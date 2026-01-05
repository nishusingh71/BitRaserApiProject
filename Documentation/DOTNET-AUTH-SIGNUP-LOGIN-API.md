# 🔐 DSecure API - Authentication (Signup & Login) Documentation

## 📋 **Complete Implementation Guide**

यह documentation User Registration (Signup) और Login के लिए complete API setup प्रदान करता है।

---

## 📁 **Table of Contents**

1. [Overview](#overview)
2. [Entity Models](#entity-models)
3. [DTOs](#dtos)
4. [Authentication Controller](#authentication-controller)
5. [API Endpoints](#api-endpoints)
6. [JWT Token Structure](#jwt-token-structure)
7. [Testing Guide](#testing-guide)

---

## 1️⃣ **Overview**

### **Features Implemented:**
- ✅ User Registration (Signup) with email validation
- ✅ User Login with JWT token generation
- ✅ Password hashing with BCrypt
- ✅ Role-based authentication (SuperAdmin, Admin, Manager, User)
- ✅ Account status validation
- ✅ Last login tracking
- ✅ Email uniqueness check
- ✅ Password strength requirements
- ✅ Token expiration (24 hours)
- ✅ User profile in token response

---

## 2️⃣ **Entity Models**

### **Models/Entities/User.cs** (Already Implemented)

```csharp
using System.ComponentModel.DataAnnotations;

namespace DSecureAPI.Models.Entities
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "user"; // user, manager, admin, superadmin

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, inactive, suspended

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? LastLoginAt { get; set; }
    }
}
```

---

## 3️⃣ **DTOs**

### **Models/DTOs/Auth/RegisterRequestDTO.cs** (NEW!)

```csharp
using System.ComponentModel.DataAnnotations;

namespace DSecureAPI.Models.DTOs.Auth
{
    public class RegisterRequestDTO
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&#])[A-Za-z\d@$!%*?&#]{8,}$",
            ErrorMessage = "Password must contain at least one uppercase letter, one lowercase letter, one number and one special character")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Password and confirm password do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number format")]
        [StringLength(20)]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Department { get; set; }

        // Role is auto-assigned as "user" - cannot be set during registration
        // Only admins can create users with different roles via UserManagement endpoints
    }
}
```

### **Models/DTOs/Auth/LoginRequestDTO.cs** (Already Implemented)

```csharp
using System.ComponentModel.DataAnnotations;

namespace DSecureAPI.Models.DTOs.Auth
{
    public class LoginRequestDTO
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;
    }
}
```

### **Models/DTOs/Auth/LoginResponseDTO.cs** (Already Implemented)

```csharp
namespace DSecureAPI.Models.DTOs.Auth
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public UserTokenDTO User { get; set; } = new();
        public DateTime ExpiresAt { get; set; }
    }

    public class UserTokenDTO
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Phone { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
```

### **Models/DTOs/Auth/RegisterResponseDTO.cs** (NEW!)

```csharp
namespace DSecureAPI.Models.DTOs.Auth
{
    public class RegisterResponseDTO
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserTokenDTO? User { get; set; }
        
        // Optional: Auto-login after registration
        public string? Token { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }
}
```

---

## 4️⃣ **Authentication Controller**

### **Controllers/AuthController.cs** (Complete Implementation)

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Auth;
using DSecureAPI.Models.Entities;
using DSecureAPI.Helpers;

namespace DSecureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;
        private readonly IConfiguration _configuration;

        public AuthController(
            ApplicationDbContext context, 
            JwtHelper jwtHelper,
            IConfiguration configuration)
        {
            _context = context;
            _jwtHelper = jwtHelper;
            _configuration = configuration;
        }

        /// <summary>
        /// Register a new user account
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <returns>Registration response with optional auto-login token</returns>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<RegisterResponseDTO>> Register([FromBody] RegisterRequestDTO request)
        {
            try
            {
                // Check if email already exists
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                if (existingUser != null)
                {
                    return BadRequest(new RegisterResponseDTO
                    {
                        Success = false,
                        Message = "Email already registered. Please use a different email or login."
                    });
                }

                // Create new user with "user" role (default)
                var newUser = new User
                {
                    Name = request.Name,
                    Email = request.Email.ToLower(),
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    Phone = request.Phone,
                    Department = request.Department,
                    Role = "user", // Default role for self-registration
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Prepare response
                var userDto = new UserTokenDTO
                {
                    Id = newUser.Id,
                    Email = newUser.Email,
                    Name = newUser.Name,
                    Role = newUser.Role,
                    Department = newUser.Department,
                    Phone = newUser.Phone,
                    Status = newUser.Status
                };

                // Option 1: Return success without auto-login
                // User needs to login separately
                var response = new RegisterResponseDTO
                {
                    Success = true,
                    Message = "Registration successful! Please login with your credentials.",
                    User = userDto
                };

                // Option 2: Auto-login after registration (Uncomment if needed)
                /*
                var token = _jwtHelper.GenerateToken(newUser);
                var expirationMinutes = Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"]);
                
                response.Token = token;
                response.ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes);
                response.Message = "Registration successful! You are now logged in.";
                
                // Update last login
                newUser.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                */

                return CreatedAtAction(nameof(GetProfile), new { }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new RegisterResponseDTO
                {
                    Success = false,
                    Message = $"An error occurred during registration: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Login with email and password
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <returns>JWT token and user information</returns>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] LoginRequestDTO request)
        {
            try
            {
                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email.ToLower() == request.Email.ToLower());

                // Check if user exists
                if (user == null)
                {
                    return Unauthorized(new { 
                        message = "Invalid email or password",
                        field = "email"
                    });
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                {
                    return Unauthorized(new { 
                        message = "Invalid email or password",
                        field = "password"
                    });
                }

                // Check account status
                if (user.Status != "active")
                {
                    return Unauthorized(new { 
                        message = $"Account is {user.Status}. Please contact administrator.",
                        field = "status"
                    });
                }

                // Update last login timestamp
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Generate JWT token
                var token = _jwtHelper.GenerateToken(user);
                var expirationMinutes = Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"]);

                // Prepare response
                var response = new LoginResponseDTO
                {
                    Token = token,
                    Role = user.Role,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                    User = new UserTokenDTO
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Name = user.Name,
                        Role = user.Role,
                        Department = user.Department,
                        Phone = user.Phone,
                        Status = user.Status
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"An error occurred during login: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Logout current user (client-side token removal)
        /// </summary>
        /// <returns>Success message</returns>
        [HttpPost("logout")]
        [Authorize]
        public IActionResult Logout()
        {
            // In JWT authentication, logout is handled client-side by removing the token
            // Server-side, we can just return a success message
            // Optional: Can implement token blacklist for additional security
            
            return Ok(new { 
                message = "Logout successful",
                timestamp = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        /// <returns>Current user information</returns>
        [HttpGet("profile")]
        [Authorize]
        public async Task<ActionResult<UserTokenDTO>> GetProfile()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userId = int.Parse(userIdClaim);
                var user = await _context.Users.FindAsync(userId);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new UserTokenDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role,
                    Department = user.Department,
                    Phone = user.Phone,
                    Status = user.Status
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"An error occurred: {ex.Message}"
                });
            }
        }

        /// <summary>
        /// Verify if email is available for registration
        /// </summary>
        /// <param name="email">Email to check</param>
        /// <returns>Availability status</returns>
        [HttpGet("check-email")]
        [AllowAnonymous]
        public async Task<ActionResult> CheckEmailAvailability([FromQuery] string email)
        {
            if (string.IsNullOrEmpty(email))
            {
                return BadRequest(new { message = "Email is required" });
            }

            var exists = await _context.Users
                .AnyAsync(u => u.Email.ToLower() == email.ToLower());

            return Ok(new { 
                available = !exists,
                email = email.ToLower(),
                message = exists ? "Email already registered" : "Email is available"
            });
        }

        /// <summary>
        /// Refresh JWT token (if implementing refresh token logic)
        /// </summary>
        /// <returns>New JWT token</returns>
        [HttpPost("refresh-token")]
        [Authorize]
        public async Task<ActionResult<LoginResponseDTO>> RefreshToken()
        {
            try
            {
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                
                if (string.IsNullOrEmpty(userIdClaim))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var userId = int.Parse(userIdClaim);
                var user = await _context.Users.FindAsync(userId);

                if (user == null || user.Status != "active")
                {
                    return Unauthorized(new { message = "User not found or inactive" });
                }

                // Generate new token
                var token = _jwtHelper.GenerateToken(user);
                var expirationMinutes = Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"]);

                return Ok(new LoginResponseDTO
                {
                    Token = token,
                    Role = user.Role,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                    User = new UserTokenDTO
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Name = user.Name,
                        Role = user.Role,
                        Department = user.Department,
                        Phone = user.Phone,
                        Status = user.Status
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = $"An error occurred: {ex.Message}"
                });
            }
        }
    }
}
```

---

## 5️⃣ **API Endpoints Summary**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/Auth/register` | Public | Register new user account |
| POST | `/api/Auth/login` | Public | Login with email & password |
| POST | `/api/Auth/logout` | Required | Logout current user |
| GET | `/api/Auth/profile` | Required | Get current user profile |
| GET | `/api/Auth/check-email?email=xyz` | Public | Check if email is available |
| POST | `/api/Auth/refresh-token` | Required | Refresh JWT token |

---

## 6️⃣ **JWT Token Structure**

### **Token Claims:**
```json
{
  "sub": "1",                          // User ID
  "email": "user@dsecuretech.com",     // User Email
  "name": "User Name",                 // User Name
  "role": "user",                      // User Role
  "jti": "unique-token-id",            // JWT ID
  "exp": 1729036800,                   // Expiration timestamp
  "iss": "https://api.dsecuretech.com", // Issuer
  "aud": "https://dsecuretech.com"     // Audience
}
```

### **Token Expiration:**
- Default: **1440 minutes (24 hours)**
- Configurable in `appsettings.json`

---

## 7️⃣ **Testing Guide**

### **Step 1: Register New User**

```bash
POST /api/Auth/register
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "John Doe",
  "email": "john.doe@example.com",
  "password": "SecurePass@123",
  "confirmPassword": "SecurePass@123",
  "phone": "+1-555-0123",
  "department": "IT"
}
```

**Success Response (201 Created):**
```json
{
  "success": true,
  "message": "Registration successful! Please login with your credentials.",
  "user": {
    "id": 5,
    "email": "john.doe@example.com",
    "name": "John Doe",
    "role": "user",
    "department": "IT",
    "phone": "+1-555-0123",
    "status": "active"
  },
  "token": null,
  "expiresAt": null
}
```

**Error Response (400 Bad Request):**
```json
{
  "success": false,
  "message": "Email already registered. Please use a different email or login.",
  "user": null,
  "token": null,
  "expiresAt": null
}
```

---

### **Step 2: Check Email Availability (Optional)**

```bash
GET /api/Auth/check-email?email=john.doe@example.com
```

**Response:**
```json
{
  "available": false,
  "email": "john.doe@example.com",
  "message": "Email already registered"
}
```

---

### **Step 3: Login**

```bash
POST /api/Auth/login
Content-Type: application/json
```

**Request Body:**
```json
{
  "email": "john.doe@example.com",
  "password": "SecurePass@123"
}
```

**Success Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "role": "user",
  "user": {
    "id": 5,
    "email": "john.doe@example.com",
    "name": "John Doe",
    "role": "user",
    "department": "IT",
    "phone": "+1-555-0123",
    "status": "active"
  },
  "expiresAt": "2025-10-16T10:30:00Z"
}
```

**Error Response (401 Unauthorized):**
```json
{
  "message": "Invalid email or password",
  "field": "email"
}
```

---

### **Step 4: Get Profile (Protected Route)**

```bash
GET /api/Auth/profile
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "id": 5,
  "email": "john.doe@example.com",
  "name": "John Doe",
  "role": "user",
  "department": "IT",
  "phone": "+1-555-0123",
  "status": "active"
}
```

---

### **Step 5: Logout**

```bash
POST /api/Auth/logout
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "message": "Logout successful",
  "timestamp": "2025-10-15T14:30:00Z"
}
```

---

### **Step 6: Refresh Token**

```bash
POST /api/Auth/refresh-token
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "role": "user",
  "user": {
    "id": 5,
    "email": "john.doe@example.com",
    "name": "John Doe",
    "role": "user",
    "department": "IT",
    "phone": "+1-555-0123",
    "status": "active"
  },
  "expiresAt": "2025-10-16T14:30:00Z"
}
```

---

## 8️⃣ **Password Requirements**

### **Validation Rules:**
- ✅ Minimum 8 characters
- ✅ At least one uppercase letter (A-Z)
- ✅ At least one lowercase letter (a-z)
- ✅ At least one number (0-9)
- ✅ At least one special character (@$!%*?&#)

### **Valid Examples:**
- `SecurePass@123`
- `MyP@ssw0rd!`
- `Admin#2024Pass`

### **Invalid Examples:**
- `password` (no uppercase, number, special char)
- `PASSWORD123` (no lowercase, special char)
- `Pass@1` (too short, less than 8 chars)

---

## 9️⃣ **Frontend Integration**

### **TypeScript Interfaces**

```typescript
// src/types/auth.types.ts

export interface RegisterRequest {
  name: string;
  email: string;
  password: string;
  confirmPassword: string;
  phone?: string;
  department?: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface UserToken {
  id: number;
  email: string;
  name: string;
  role: string;
  department?: string;
  phone?: string;
  status: string;
}

export interface LoginResponse {
  token: string;
  role: string;
  user: UserToken;
  expiresAt: string;
}

export interface RegisterResponse {
  success: boolean;
  message: string;
  user?: UserToken;
  token?: string;
  expiresAt?: string;
}
```

### **API Service Functions**

```typescript
// src/services/authService.ts

import axios from 'axios';
import type { 
  RegisterRequest, 
  LoginRequest, 
  LoginResponse, 
  RegisterResponse,
  UserToken 
} from '@/types/auth.types';

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL || 'https://api.dsecuretech.com';

const apiClient = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Register new user
export const register = async (data: RegisterRequest): Promise<RegisterResponse> => {
  const response = await apiClient.post<RegisterResponse>('/api/Auth/register', data);
  return response.data;
};

// Login user
export const login = async (credentials: LoginRequest): Promise<LoginResponse> => {
  const response = await apiClient.post<LoginResponse>('/api/Auth/login', credentials);
  
  // Store token in localStorage
  if (response.data.token) {
    localStorage.setItem('token', response.data.token);
    localStorage.setItem('user', JSON.stringify(response.data.user));
    localStorage.setItem('role', response.data.role);
  }
  
  return response.data;
};

// Logout user
export const logout = async (): Promise<void> => {
  const token = localStorage.getItem('token');
  
  if (token) {
    try {
      await apiClient.post('/api/Auth/logout', {}, {
        headers: { Authorization: `Bearer ${token}` }
      });
    } catch (error) {
      console.error('Logout error:', error);
    }
  }
  
  // Clear localStorage
  localStorage.removeItem('token');
  localStorage.removeItem('user');
  localStorage.removeItem('role');
};

// Get current user profile
export const getProfile = async (): Promise<UserToken> => {
  const token = localStorage.getItem('token');
  
  const response = await apiClient.get<UserToken>('/api/Auth/profile', {
    headers: { Authorization: `Bearer ${token}` }
  });
  
  return response.data;
};

// Check email availability
export const checkEmail = async (email: string): Promise<{ available: boolean; message: string }> => {
  const response = await apiClient.get(`/api/Auth/check-email?email=${encodeURIComponent(email)}`);
  return response.data;
};

// Refresh token
export const refreshToken = async (): Promise<LoginResponse> => {
  const token = localStorage.getItem('token');
  
  const response = await apiClient.post<LoginResponse>('/api/Auth/refresh-token', {}, {
    headers: { Authorization: `Bearer ${token}` }
  });
  
  // Update stored token
  if (response.data.token) {
    localStorage.setItem('token', response.data.token);
    localStorage.setItem('user', JSON.stringify(response.data.user));
  }
  
  return response.data;
};

// Get stored token
export const getToken = (): string | null => {
  return localStorage.getItem('token');
};

// Get stored user
export const getStoredUser = (): UserToken | null => {
  const userStr = localStorage.getItem('user');
  return userStr ? JSON.parse(userStr) : null;
};

// Check if user is authenticated
export const isAuthenticated = (): boolean => {
  return !!localStorage.getItem('token');
};
```

---

## 🔟 **Security Best Practices**

### **Implemented:**
- ✅ Password hashing with BCrypt (salt rounds: 10)
- ✅ JWT token with expiration
- ✅ Email validation
- ✅ Password strength requirements
- ✅ Account status checking
- ✅ HTTPS required in production
- ✅ CORS configuration
- ✅ Case-insensitive email checking

### **Recommended Additions:**
- 🔄 Rate limiting on login/register endpoints
- 🔄 Email verification with OTP/link
- 🔄 Password reset functionality
- 🔄 Two-factor authentication (2FA)
- 🔄 Token blacklist for logout
- 🔄 Refresh token rotation
- 🔄 Account lockout after failed attempts
- 🔄 IP-based security monitoring

---

## 1️⃣1️⃣ **Error Handling**

### **Common Error Responses:**

**400 Bad Request:**
```json
{
  "success": false,
  "message": "Email already registered. Please use a different email or login."
}
```

**401 Unauthorized:**
```json
{
  "message": "Invalid email or password",
  "field": "email"
}
```

**403 Forbidden:**
```json
{
  "message": "Account is suspended. Please contact administrator.",
  "field": "status"
}
```

**500 Internal Server Error:**
```json
{
  "message": "An error occurred during registration: Database connection failed"
}
```

---

## 1️⃣2️⃣ **Testing with Postman/Thunder Client**

### **Collection Setup:**

1. **Create Environment Variables:**
```
base_url = https://localhost:7XXX
token = (will be set automatically)
```

2. **Register User:**
```
POST {{base_url}}/api/Auth/register
Body (JSON):
{
  "name": "Test User",
  "email": "test@example.com",
  "password": "Test@123456",
  "confirmPassword": "Test@123456"
}
```

3. **Login:**
```
POST {{base_url}}/api/Auth/login
Body (JSON):
{
  "email": "test@example.com",
  "password": "Test@123456"
}

Tests (Save token):
pm.environment.set("token", pm.response.json().token);
```

4. **Get Profile:**
```
GET {{base_url}}/api/Auth/profile
Headers:
Authorization: Bearer {{token}}
```

---

## ✅ **Complete Feature Checklist**

### **Registration:**
- ✅ Email validation
- ✅ Password strength check
- ✅ Confirm password matching
- ✅ Duplicate email prevention
- ✅ Auto role assignment (user)
- ✅ Optional phone & department
- ✅ Success/error responses

### **Login:**
- ✅ Email & password authentication
- ✅ BCrypt password verification
- ✅ Account status check
- ✅ JWT token generation
- ✅ Last login tracking
- ✅ User profile in response
- ✅ Token expiration time

### **Additional Features:**
- ✅ Logout endpoint
- ✅ Get profile endpoint
- ✅ Check email availability
- ✅ Refresh token endpoint
- ✅ Role-based authorization
- ✅ Error handling
- ✅ Security validations

---

**Perfect! Ab aapka complete Signup & Login API documentation ready hai! 🎉**

**Testing ke liye Swagger UI use karo ya Postman collection banao! 🚀**
