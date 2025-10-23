# 🎯 DTO Quick Reference Guide

## 📦 **Which DTO Should I Use?**

### **Dashboard Controllers** (`/api/Dashboard*`)

Use these DTOs from `Models\DashboardModels.cs`:

| DTO Name | Purpose | Properties |
|----------|---------|------------|
| `DashboardLoginRequestDto` | Login request | `Email`, `Password` (PascalCase) |
| `DashboardLoginResponseDto` | Login response | `Token`, `RefreshToken`, `User`, `ExpiresAt` |
| `DashboardUserDto` | User profile | `Id`, `Name`, `Email`, `Role`, `TimeZone`, `Department`, `LastLogin` |
| `AdminUserDto` | Admin user management | `Id`, `Name`, `Email`, `Department`, `Role`, `Status`, etc. |
| `CreateUserDto` | Create user | `Name`, `Email`, `Password`, `Department`, `Role` |
| `DashboardOverviewDto` | Dashboard stats | `TotalUsers`, `ActiveUsers`, `TotalLicenses`, etc. |
| `ActivityDto` | Activity logs | `Id`, `Type`, `Description`, `User`, `Timestamp` |
| `AdminLicenseDto` | License management | `Id`, `Type`, `AssignedTo`, `Status`, `ExpiryDate` |

### **Role-Based Auth Controllers** (`/api/RoleBasedAuth`, `/api/Enhanced*`)

Use these DTOs from `Models\DTOs\UserDtos.cs`:

| DTO Name | Purpose | Properties |
|----------|---------|------------|
| `LoginRequestDto` | Login request | `email`, `password` (camelCase) |
| `LoginResponseDto` | Login response | `token`, `userType`, `email`, `roles`, `permissions` |
| `UserDto` | User profile | `user_id`, `user_name`, `user_email`, `roles`, `permissions` |
| `UserCreateDto` | Create user | `user_name`, `user_email`, `user_password`, `initialRoles` |
| `UserUpdateDto` | Update user | `user_name`, `phone_number`, `is_private_cloud`, etc. |
| `SubuserDto` | Subuser profile | `subuser_id`, `subuser_email`, `user_email`, `roles` |
| `UserRoleInfoDto` | Role information | `userEmail`, `userType`, `roles`, `permissions` |

## 🔄 **Property Naming Conventions**

### **Dashboard DTOs** (PascalCase)
```csharp
var request = new DashboardLoginRequestDto
{
    Email = "user@example.com",      // ✅ PascalCase
    Password = "password123"          // ✅ PascalCase
};
```

### **Role-Based Auth DTOs** (camelCase for JSON, snake_case for DB fields)
```csharp
var request = new LoginRequestDto
{
    email = "user@example.com",      // ✅ camelCase (JSON)
    password = "password123"          // ✅ camelCase (JSON)
};

var user = new UserDto
{
    user_id = 1,                     // ✅ snake_case (DB field)
    user_name = "John Doe",          // ✅ snake_case (DB field)
    user_email = "john@example.com"  // ✅ snake_case (DB field)
};
```

## 🌐 **API Examples**

### **Dashboard Login** (PascalCase)
```http
POST /api/DashboardAuth/login
Content-Type: application/json

{
  "Email": "admin@example.com",
  "Password": "password123"
}
```

**Response:**
```json
{
  "Token": "eyJhbGciOiJIUzI1NiIs...",
  "RefreshToken": "550e8400-e29b-41d4-a716-446655440000",
  "User": {
    "Id": "1",
    "Name": "Admin User",
    "Email": "admin@example.com",
    "Role": "Admin",
    "TimeZone": "UTC",
    "Department": "",
    "LastLogin": "2024-01-01T12:00:00Z"
  },
  "ExpiresAt": "2024-01-02T12:00:00Z"
}
```

### **Role-Based Auth Login** (camelCase)
```http
POST /api/RoleBasedAuth/login
Content-Type: application/json

{
  "email": "admin@example.com",
  "password": "password123"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "userType": "user",
  "email": "admin@example.com",
  "roles": ["Admin", "Manager"],
  "permissions": ["FullAccess", "UserManagement"],
  "expiresAt": "2024-01-02T12:00:00Z"
}
```

## 🎨 **Controller Usage Examples**

### **Dashboard Controller**
```csharp
[ApiController]
[Route("api/[controller]")]
public class DashboardAuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<DashboardLoginResponseDto>> Login(
        [FromBody] DashboardLoginRequestDto request)  // ✅ Dashboard DTO
    {
        return Ok(new DashboardLoginResponseDto
        {
            Token = token,
            User = new DashboardUserDto { ... }  // ✅ Dashboard DTO
        });
    }
}
```

### **Role-Based Auth Controller**
```csharp
[ApiController]
[Route("api/[controller]")]
public class RoleBasedAuthController : ControllerBase
{
    [HttpPost("login")]
    public async Task<ActionResult<LoginResponseDto>> Login(
        [FromBody] LoginRequestDto request)  // ✅ Role-Based DTO
    {
        return Ok(new LoginResponseDto
        {
            token = token,
            userType = "user"  // ✅ camelCase
        });
    }
}
```

## 📋 **Quick Decision Tree**

```
Are you working with Dashboard endpoints?
│
├─ YES → Use DashboardModels.cs DTOs
│         - DashboardLoginRequestDto
│         - DashboardLoginResponseDto
│         - DashboardUserDto
│         - AdminUserDto
│         - Properties: PascalCase
│
└─ NO → Are you working with Enhanced/RoleBasedAuth endpoints?
         │
         └─ YES → Use DTOs/UserDtos.cs DTOs
                  - LoginRequestDto
                  - LoginResponseDto
                  - UserDto
                  - UserCreateDto
                  - Properties: camelCase (JSON), snake_case (DB)
```

## 🔍 **How to Find Which DTO to Use**

### **Method 1: Check the Controller Route**
```csharp
// If route starts with /api/Dashboard*
[Route("api/[controller]")]  // e.g., api/DashboardAuth
public class DashboardAuthController
// → Use DashboardModels.cs DTOs

// If route is /api/RoleBasedAuth or /api/Enhanced*
[Route("api/[controller]")]  // e.g., api/RoleBasedAuth
public class RoleBasedAuthController
// → Use DTOs/UserDtos.cs DTOs
```

### **Method 2: Check the Method Signature**
```csharp
// Dashboard DTO
public async Task<ActionResult<DashboardLoginResponseDto>> Login(...)

// Role-Based Auth DTO
public async Task<ActionResult<LoginResponseDto>> Login(...)
```

### **Method 3: Look at the Import Statements**
```csharp
// Dashboard controllers typically use:
using BitRaserApiProject.Models;  // DashboardModels.cs

// Enhanced controllers typically use:
using BitRaserApiProject.Models.DTOs;  // UserDtos.cs
```

## 📚 **Related Files**

| Category | File | Purpose |
|----------|------|---------|
| **Models** | `Models\DashboardModels.cs` | Dashboard-specific DTOs |
| **Models** | `Models\DTOs\UserDtos.cs` | Role-based auth DTOs |
| **Controllers** | `Controllers\DashboardController.cs` | Dashboard endpoints |
| **Controllers** | `Controllers\RoleBasedAuthController.cs` | Role-based auth endpoints |
| **Controllers** | `Controllers\Enhanced*.cs` | Enhanced feature endpoints |

## 🎯 **Best Practices**

1. ✅ **Consistent Naming**: Stick to the naming convention of the endpoint you're working with
2. ✅ **Type Safety**: Use the correct DTO type for method signatures
3. ✅ **Documentation**: Add XML comments to clarify which DTO to use
4. ✅ **Testing**: Test with correct property casing in API requests

## ⚠️ **Common Mistakes to Avoid**

❌ **Don't mix DTOs:**
```csharp
// WRONG - Mixing Dashboard and Role-Based DTOs
public async Task<ActionResult<LoginResponseDto>> DashboardLogin(
    [FromBody] DashboardLoginRequestDto request)  // ❌ Mismatch!
```

✅ **Do use matching DTOs:**
```csharp
// CORRECT - Matching DTOs
public async Task<ActionResult<DashboardLoginResponseDto>> DashboardLogin(
    [FromBody] DashboardLoginRequestDto request)  // ✅ Perfect!
```

---

**Quick Reference**: When in doubt, check the controller namespace and route to determine which DTO set to use!
