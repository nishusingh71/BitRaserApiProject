# 🚀 Complete .NET API Setup for Admin Dashboard

## 📋 **Project Structure**

```
DSecureAPI/
├── Controllers/
│   ├── RoleBasedAuthController.cs
│   ├── AdminDashboardController.cs
│   ├── UserManagementController.cs
│   ├── GroupManagementController.cs
│   ├── LicenseManagementController.cs
│   ├── ReportsController.cs
│   ├── SystemSettingsController.cs
│   └── ProfileController.cs
├── Models/
│   ├── DTOs/
│   │   ├── Auth/
│   │   │   ├── LoginRequestDTO.cs
│   │   │   ├── LoginResponseDTO.cs
│   │   │   └── UserTokenDTO.cs
│   │   ├── Dashboard/
│   │   │   ├── DashboardStatsDTO.cs
│   │   │   ├── UserActivityDTO.cs
│   │   │   └── RecentReportDTO.cs
│   │   ├── User/
│   │   │   ├── CreateUserDTO.cs
│   │   │   ├── UpdateUserDTO.cs
│   │   │   └── UserResponseDTO.cs
│   │   ├── Group/
│   │   │   ├── CreateGroupDTO.cs
│   │   │   └── GroupResponseDTO.cs
│   │   └── License/
│   │       ├── AssignLicenseDTO.cs
│   │       └── LicenseDataDTO.cs
│   └── Entities/
│       ├── User.cs
│       ├── Group.cs
│       ├── License.cs
│       ├── Report.cs
│       ├── UserActivity.cs
│       └── SystemSettings.cs
├── Services/
│   ├── Interfaces/
│   │   ├── IAuthService.cs
│   │   ├── IUserService.cs
│   │   ├── IGroupService.cs
│   │   ├── ILicenseService.cs
│   │   └── IReportService.cs
│   └── Implementations/
│       ├── AuthService.cs
│       ├── UserService.cs
│       ├── GroupService.cs
│       ├── LicenseService.cs
│       └── ReportService.cs
├── Data/
│   ├── ApplicationDbContext.cs
│   └── Migrations/
├── Middleware/
│   ├── JwtMiddleware.cs
│   └── RoleAuthorizationMiddleware.cs
├── Helpers/
│   ├── JwtHelper.cs
│   ├── PasswordHasher.cs
│   └── RolePermissions.cs
├── appsettings.json
├── appsettings.Development.json
├── Program.cs
└── DSecureAPI.csproj
```

---

## 🔧 **Step 1: Create .NET Web API Project**

### **Commands:**
```bash
# Create new Web API project
dotnet new webapi -n DSecureAPI
cd DSecureAPI

# Add required NuGet packages
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 8.0.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.0.0
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
dotnet add package AutoMapper --version 12.0.1
dotnet add package AutoMapper.Extensions.Microsoft.DependencyInjection --version 12.0.1
```

---

## 📝 **Step 2: Configure appsettings.json**

### **appsettings.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DSecureDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!@#$%",
    "Issuer": "https://api.dsecuretech.com",
    "Audience": "https://dsecuretech.com",
    "ExpirationInMinutes": 1440,
    "RefreshTokenExpirationInDays": 7
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:5177",
      "https://dsecuretech.com",
      "https://www.dsecuretech.com"
    ]
  }
}
```

### **appsettings.Development.json**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DSecureDB_Dev;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Debug",
      "Microsoft.AspNetCore": "Debug",
      "Microsoft.EntityFrameworkCore": "Information"
    }
  }
}
```

---

## 🗄️ **Step 3: Entity Models**

### **Models/Entities/User.cs**
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string Role { get; set; } = "user"; // superadmin, admin, manager, user

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, inactive, suspended

        [MaxLength(50)]
        public string? Timezone { get; set; }

        [MaxLength(255)]
        public string? Avatar { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastLoginAt { get; set; }

        // Navigation properties
        public virtual ICollection<License> Licenses { get; set; } = new List<License>();
        public virtual ICollection<UserActivity> Activities { get; set; } = new List<UserActivity>();
        public virtual ICollection<Report> Reports { get; set; } = new List<Report>();
    }
}
```

### **Models/Entities/Group.cs**
```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace DSecureAPI.Models.Entities
{
    public class Group
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int TotalUsers { get; set; } = 0;
        public int TotalLicenses { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "active";

        // Navigation properties
        public virtual ICollection<User> Users { get; set; } = new List<User>();
        public virtual ICollection<License> Licenses { get; set; } = new List<License>();
    }
}
```

### **Models/Entities/License.cs**
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureAPI.Models.Entities
{
    public class License
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [MaxLength(255)]
        public string LicenseKey { get; set; } = string.Empty;

        [MaxLength(50)]
        public string LicenseType { get; set; } = "basic"; // basic, pro, enterprise

        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, expired, revoked

        public DateTime? ExpiryDate { get; set; }
        public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RevokedAt { get; set; }

        // Foreign Keys
        public int? UserId { get; set; }
        public int? GroupId { get; set; }

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [ForeignKey("GroupId")]
        public virtual Group? Group { get; set; }
    }
}
```

### **Models/Entities/Report.cs**
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureAPI.Models.Entities
{
    public class Report
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Type { get; set; } = string.Empty; // erasure, license-audit, user-activity

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "pending"; // pending, running, completed, failed

        public int DeviceCount { get; set; } = 0;

        [MaxLength(100)]
        public string? Method { get; set; }

        [MaxLength(500)]
        public string? FilePath { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Foreign Key
        public int GeneratedBy { get; set; }

        // Navigation property
        [ForeignKey("GeneratedBy")]
        public virtual User? GeneratedByUser { get; set; }
    }
}
```

### **Models/Entities/UserActivity.cs**
```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureAPI.Models.Entities
{
    public class UserActivity
    {
        [Key]
        public int Id { get; set; }

        public int UserId { get; set; }

        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // login, logout, create_user, etc.

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(255)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User? User { get; set; }
    }
}
```

### **Models/Entities/SystemSettings.cs**
```csharp
using System;
using System.ComponentModel.DataAnnotations;

namespace DSecureAPI.Models.Entities
{
    public class SystemSettings
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SettingKey { get; set; } = string.Empty;

        [Required]
        public string SettingValue { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? Description { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

---

## 📊 **Step 4: DTOs (Data Transfer Objects)**

### **Models/DTOs/Auth/LoginRequestDTO.cs**
```csharp
using System.ComponentModel.DataAnnotations;

namespace DSecureAPI.Models.DTOs.Auth
{
    public class LoginRequestDTO
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;
    }
}
```

### **Models/DTOs/Auth/LoginResponseDTO.cs**
```csharp
namespace DSecureAPI.Models.DTOs.Auth
{
    public class LoginResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public UserTokenDTO User { get; set; } = new();
        public string Role { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
    }
}
```

### **Models/DTOs/Auth/UserTokenDTO.cs**
```csharp
namespace DSecureAPI.Models.DTOs.Auth
{
    public class UserTokenDTO
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Department { get; set; }
        public string? Avatar { get; set; }
    }
}
```

### **Models/DTOs/Dashboard/DashboardStatsDTO.cs**
```csharp
namespace DSecureAPI.Models.DTOs.Dashboard
{
    public class DashboardStatsDTO
    {
        public string TotalLicenses { get; set; } = "0";
        public string ActiveUsers { get; set; } = "0";
        public string AvailableLicenses { get; set; } = "0";
        public string SuccessRate { get; set; } = "0%";
        public int? TotalUsers { get; set; } // For Manager role
        public StatsChanges Changes { get; set; } = new();
    }

    public class StatsChanges
    {
        public StatChange TotalLicenses { get; set; } = new();
        public StatChange ActiveUsers { get; set; } = new();
        public StatChange AvailableLicenses { get; set; } = new();
        public StatChange SuccessRate { get; set; } = new();
    }

    public class StatChange
    {
        public string Value { get; set; } = "0%";
        public string Trend { get; set; } = "up"; // up or down
    }
}
```

### **Models/DTOs/Dashboard/UserActivityDTO.cs**
```csharp
namespace DSecureAPI.Models.DTOs.Dashboard
{
    public class UserActivityDTO
    {
        public string Email { get; set; } = string.Empty;
        public string LoginTime { get; set; } = string.Empty;
        public string LogoutTime { get; set; } = string.Empty;
        public string Status { get; set; } = "offline"; // active or offline
    }
}
```

### **Models/DTOs/User/CreateUserDTO.cs**
```csharp
using System.ComponentModel.DataAnnotations;

namespace DSecureAPI.Models.DTOs.User
{
    public class CreateUserDTO
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public string Role { get; set; } = "user"; // superadmin, admin, manager, user

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        public string Status { get; set; } = "active";
    }
}
```

### **Models/DTOs/Group/CreateGroupDTO.cs**
```csharp
using System.ComponentModel.DataAnnotations;

namespace DSecureAPI.Models.DTOs.Group
{
    public class CreateGroupDTO
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? Description { get; set; }

        public int TotalLicenses { get; set; } = 0;
    }
}
```

### **Models/DTOs/License/AssignLicenseDTO.cs**
```csharp
using System.ComponentModel.DataAnnotations;

namespace DSecureAPI.Models.DTOs.License
{
    public class AssignLicenseDTO
    {
        [Required]
        public int? UserId { get; set; }

        public int? GroupId { get; set; }

        [Required]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        public string LicenseType { get; set; } = "basic";

        public DateTime? ExpiryDate { get; set; }

        public int LicenseCount { get; set; } = 1;
    }
}
```

---

## 🗃️ **Step 5: Database Context**

### **Data/ApplicationDbContext.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Models.Entities;

namespace DSecureAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<UserActivity> UserActivities { get; set; }
        public DbSet<SystemSettings> SystemSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure relationships
            modelBuilder.Entity<User>()
                .HasMany(u => u.Licenses)
                .WithOne(l => l.User)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Activities)
                .WithOne(a => a.User)
                .HasForeignKey(a => a.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<User>()
                .HasMany(u => u.Reports)
                .WithOne(r => r.GeneratedByUser)
                .HasForeignKey(r => r.GeneratedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Group>()
                .HasMany(g => g.Licenses)
                .WithOne(l => l.Group)
                .HasForeignKey(l => l.GroupId)
                .OnDelete(DeleteBehavior.SetNull);

            // Indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<License>()
                .HasIndex(l => l.LicenseKey)
                .IsUnique();

            // Seed data
            SeedData(modelBuilder);
        }

        private void SeedData(ModelBuilder modelBuilder)
        {
            // Seed SuperAdmin user
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "Super Admin",
                    Email = "superadmin@dsecuretech.com",
                    // Password: Admin@123
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "superadmin",
                    Department = "Administration",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 2,
                    Name = "Admin User",
                    Email = "admin@dsecuretech.com",
                    // Password: Admin@123
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "admin",
                    Department = "IT",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 3,
                    Name = "Manager User",
                    Email = "manager@dsecuretech.com",
                    // Password: Manager@123
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                    Role = "manager",
                    Department = "Operations",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 4,
                    Name = "Regular User",
                    Email = "user@dsecuretech.com",
                    // Password: User@123
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                    Role = "user",
                    Department = "Support",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
```

---

## 🔐 **Step 6: JWT Helper**

### **Helpers/JwtHelper.cs**
```csharp
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using DSecureAPI.Models.Entities;

namespace DSecureAPI.Helpers
{
    public class JwtHelper
    {
        private readonly IConfiguration _configuration;

        public JwtHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(User user)
        {
            var securityKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"))
            );
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("department", user.Department ?? ""),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"] ?? "1440")
                ),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public string GenerateRefreshToken()
        {
            return Guid.NewGuid().ToString() + Guid.NewGuid().ToString();
        }

        public ClaimsPrincipal? ValidateToken(string token)
        {
            try
            {
                var tokenHandler = new JwtSecurityTokenHandler();
                var key = Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"] ?? throw new InvalidOperationException("JWT Secret not configured"));

                var validationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _configuration["JwtSettings:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = _configuration["JwtSettings:Audience"],
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                return tokenHandler.ValidateToken(token, validationParameters, out _);
            }
            catch
            {
                return null;
            }
        }
    }
}
```

---

## 🎯 **Step 7: Controllers**

### **Controllers/RoleBasedAuthController.cs**
```csharp
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Auth;
using DSecureAPI.Helpers;

namespace DSecureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RoleBasedAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly JwtHelper _jwtHelper;
        private readonly ILogger<RoleBasedAuthController> _logger;

        public RoleBasedAuthController(
            ApplicationDbContext context,
            JwtHelper jwtHelper,
            ILogger<RoleBasedAuthController> logger)
        {
            _context = context;
            _jwtHelper = jwtHelper;
            _logger = logger;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] LoginRequestDTO loginRequest)
        {
            try
            {
                // Find user by email
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == loginRequest.Email);

                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Verify password
                if (!BCrypt.Net.BCrypt.Verify(loginRequest.Password, user.PasswordHash))
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Check user status
                if (user.Status != "active")
                {
                    return Unauthorized(new { message = "Account is not active" });
                }

                // Generate JWT token
                var token = _jwtHelper.GenerateToken(user);
                var refreshToken = _jwtHelper.GenerateRefreshToken();

                // Update last login
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Log activity
                var activity = new Models.Entities.UserActivity
                {
                    UserId = user.Id,
                    Action = "login",
                    Description = "User logged in",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                var response = new LoginResponseDTO
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    Role = user.Role,
                    ExpiresAt = DateTime.UtcNow.AddMinutes(1440),
                    User = new UserTokenDTO
                    {
                        Id = user.Id,
                        Email = user.Email,
                        Name = user.Name,
                        Role = user.Role,
                        Department = user.Department,
                        Avatar = user.Avatar
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // Log activity
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                var activity = new Models.Entities.UserActivity
                {
                    UserId = userId,
                    Action = "logout",
                    Description = "User logged out",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = "Logged out successfully" });
        }
    }
}
```

### **Controllers/AdminDashboardController.cs**
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Dashboard;

namespace DSecureAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AdminDashboardController> _logger;

        public AdminDashboardController(
            ApplicationDbContext context,
            ILogger<AdminDashboardController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("stats")]
        public async Task<ActionResult<DashboardStatsDTO>> GetDashboardStats()
        {
            try
            {
                var totalUsers = await _context.Users.CountAsync();
                var activeUsers = await _context.Users.CountAsync(u => u.Status == "active");
                var totalLicenses = await _context.Licenses.CountAsync();
                var availableLicenses = await _context.Licenses.CountAsync(l => l.Status == "active" && l.UserId == null);
                var completedReports = await _context.Reports.CountAsync(r => r.Status == "completed");
                var totalReports = await _context.Reports.CountAsync();

                var successRate = totalReports > 0 
                    ? $"{(completedReports * 100.0 / totalReports):F1}%" 
                    : "0%";

                var stats = new DashboardStatsDTO
                {
                    TotalLicenses = totalLicenses.ToString(),
                    ActiveUsers = activeUsers.ToString(),
                    AvailableLicenses = availableLicenses.ToString(),
                    SuccessRate = successRate,
                    TotalUsers = totalUsers,
                    Changes = new StatsChanges
                    {
                        TotalLicenses = new StatChange { Value = "+12%", Trend = "up" },
                        ActiveUsers = new StatChange { Value = "+5%", Trend = "up" },
                        AvailableLicenses = new StatChange { Value = "-3%", Trend = "down" },
                        SuccessRate = new StatChange { Value = "+2%", Trend = "up" }
                    }
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching dashboard stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("user-activity")]
        public async Task<ActionResult<List<UserActivityDTO>>> GetUserActivity()
        {
            try
            {
                var activities = await _context.UserActivities
                    .Include(a => a.User)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(10)
                    .Select(a => new UserActivityDTO
                    {
                        Email = a.User != null ? a.User.Email : "Unknown",
                        LoginTime = a.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                        LogoutTime = "", // Calculate from logout activities
                        Status = a.Action == "login" ? "active" : "offline"
                    })
                    .ToListAsync();

                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user activity");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("groups")]
        public async Task<ActionResult> GetGroups()
        {
            try
            {
                var groups = await _context.Groups
                    .Select(g => new
                    {
                        g.Id,
                        g.Name,
                        g.Description,
                        Users = g.TotalUsers,
                        Licenses = g.TotalLicenses,
                        Date = g.CreatedAt.ToString("MMM dd, yyyy")
                    })
                    .ToListAsync();

                return Ok(groups);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching groups");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("licenses")]
        public async Task<ActionResult> GetLicenseData()
        {
            try
            {
                var licenses = await _context.Licenses
                    .GroupBy(l => l.ProductName)
                    .Select(g => new
                    {
                        Product = g.Key,
                        Total = g.Count(),
                        Consumed = g.Count(l => l.UserId != null),
                        Available = g.Count(l => l.UserId == null)
                    })
                    .ToListAsync();

                return Ok(licenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching license data");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        [HttpGet("recent-reports")]
        public async Task<ActionResult> GetRecentReports()
        {
            try
            {
                var reports = await _context.Reports
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(10)
                    .Select(r => new
                    {
                        r.Id,
                        r.Type,
                        Devices = r.DeviceCount,
                        r.Status,
                        Date = r.CreatedAt.ToString("MMM dd, yyyy"),
                        r.Method
                    })
                    .ToListAsync();

                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recent reports");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
```

---

**[Continuing in next part due to length...]**

Would you like me to continue with:
1. User Management Controller
2. Group Management Controller
3. License Management Controller
4. Reports Controller
5. System Settings Controller
6. Program.cs configuration
7. Migration commands
8. Complete API endpoint documentation?