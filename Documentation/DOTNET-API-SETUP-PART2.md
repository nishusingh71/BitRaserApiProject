# 🚀 .NET API Setup - Part 2: Controllers & Configuration

## 📋 **Controllers (Continued)**

### **Controllers/UserManagementController.cs**
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.User;
using DSecureAPI.Models.Entities;

namespace DSecureAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(
            ApplicationDbContext context,
            ILogger<UserManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/UserManagement
        [HttpGet]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult> GetAllUsers()
        {
            try
            {
                var users = await _context.Users
                    .Select(u => new
                    {
                        u.Id,
                        u.Name,
                        u.Email,
                        u.Role,
                        u.Department,
                        u.Phone,
                        u.Status,
                        u.CreatedAt,
                        LastLogin = u.LastLoginAt,
                        LicenseCount = u.Licenses.Count
                    })
                    .ToListAsync();

                return Ok(users);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching users");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/UserManagement/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult> GetUser(int id)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.Licenses)
                    .FirstOrDefaultAsync(u => u.Id == id);

                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var userDto = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                    user.Department,
                    user.Phone,
                    user.Status,
                    user.Timezone,
                    user.Avatar,
                    user.CreatedAt,
                    LastLogin = user.LastLoginAt,
                    Licenses = user.Licenses.Select(l => new
                    {
                        l.Id,
                        l.ProductName,
                        l.LicenseType,
                        l.Status,
                        l.ExpiryDate
                    })
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/UserManagement
        [HttpPost]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult> CreateUser([FromBody] CreateUserDTO createUserDto)
        {
            try
            {
                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == createUserDto.Email))
                {
                    return BadRequest(new { message = "Email already exists" });
                }

                // Create new user
                var user = new User
                {
                    Name = createUserDto.Name,
                    Email = createUserDto.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(createUserDto.Password),
                    Role = createUserDto.Role,
                    Department = createUserDto.Department,
                    Phone = createUserDto.Phone,
                    Status = createUserDto.Status,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "create_user",
                    Description = $"Created user: {user.Email}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                    user.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: api/UserManagement/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult> UpdateUser(int id, [FromBody] CreateUserDTO updateUserDto)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Update user properties
                user.Name = updateUserDto.Name;
                user.Role = updateUserDto.Role;
                user.Department = updateUserDto.Department;
                user.Phone = updateUserDto.Phone;
                user.Status = updateUserDto.Status;
                user.UpdatedAt = DateTime.UtcNow;

                // Update password if provided
                if (!string.IsNullOrEmpty(updateUserDto.Password))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updateUserDto.Password);
                }

                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "update_user",
                    Description = $"Updated user: {user.Email}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/UserManagement/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<ActionResult> DeleteUser(int id)
        {
            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Don't allow deleting superadmin
                if (user.Role == "superadmin")
                {
                    return BadRequest(new { message = "Cannot delete superadmin user" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "delete_user",
                    Description = $"Deleted user: {user.Email}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
```

### **Controllers/GroupManagementController.cs**
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Group;
using DSecureAPI.Models.Entities;

namespace DSecureAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class GroupManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<GroupManagementController> _logger;

        public GroupManagementController(
            ApplicationDbContext context,
            ILogger<GroupManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/GroupManagement
        [HttpGet]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult> GetAllGroups()
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
                        g.Status,
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

        // GET: api/GroupManagement/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult> GetGroup(int id)
        {
            try
            {
                var group = await _context.Groups
                    .Include(g => g.Users)
                    .Include(g => g.Licenses)
                    .FirstOrDefaultAsync(g => g.Id == id);

                if (group == null)
                {
                    return NotFound(new { message = "Group not found" });
                }

                return Ok(group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching group");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/GroupManagement
        [HttpPost]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult> CreateGroup([FromBody] CreateGroupDTO createGroupDto)
        {
            try
            {
                var group = new Group
                {
                    Name = createGroupDto.Name,
                    Description = createGroupDto.Description,
                    TotalLicenses = createGroupDto.TotalLicenses,
                    TotalUsers = 0,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Groups.Add(group);
                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "create_group",
                    Description = $"Created group: {group.Name}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetGroup), new { id = group.Id }, group);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating group");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // PUT: api/GroupManagement/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult> UpdateGroup(int id, [FromBody] CreateGroupDTO updateGroupDto)
        {
            try
            {
                var group = await _context.Groups.FindAsync(id);
                if (group == null)
                {
                    return NotFound(new { message = "Group not found" });
                }

                group.Name = updateGroupDto.Name;
                group.Description = updateGroupDto.Description;
                group.TotalLicenses = updateGroupDto.TotalLicenses;
                group.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "update_group",
                    Description = $"Updated group: {group.Name}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Group updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating group");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/GroupManagement/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<ActionResult> DeleteGroup(int id)
        {
            try
            {
                var group = await _context.Groups.FindAsync(id);
                if (group == null)
                {
                    return NotFound(new { message = "Group not found" });
                }

                _context.Groups.Remove(group);
                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "delete_group",
                    Description = $"Deleted group: {group.Name}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Group deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting group");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
```

### **Controllers/LicenseManagementController.cs**
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.License;
using DSecureAPI.Models.Entities;

namespace DSecureAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class LicenseManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LicenseManagementController> _logger;

        public LicenseManagementController(
            ApplicationDbContext context,
            ILogger<LicenseManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/LicenseManagement
        [HttpGet]
        public async Task<ActionResult> GetAllLicenses()
        {
            try
            {
                var licenses = await _context.Licenses
                    .Include(l => l.User)
                    .Include(l => l.Group)
                    .Select(l => new
                    {
                        l.Id,
                        l.ProductName,
                        l.LicenseKey,
                        l.LicenseType,
                        l.Status,
                        l.ExpiryDate,
                        l.IssuedAt,
                        UserEmail = l.User != null ? l.User.Email : null,
                        GroupName = l.Group != null ? l.Group.Name : null
                    })
                    .ToListAsync();

                return Ok(licenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching licenses");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/LicenseManagement/assign
        [HttpPost("assign")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult> AssignLicense([FromBody] AssignLicenseDTO assignLicenseDto)
        {
            try
            {
                var licenses = new List<License>();

                for (int i = 0; i < assignLicenseDto.LicenseCount; i++)
                {
                    var license = new License
                    {
                        ProductName = assignLicenseDto.ProductName,
                        LicenseKey = Guid.NewGuid().ToString().Replace("-", "").ToUpper().Substring(0, 25),
                        LicenseType = assignLicenseDto.LicenseType,
                        Status = "active",
                        ExpiryDate = assignLicenseDto.ExpiryDate,
                        IssuedAt = DateTime.UtcNow,
                        UserId = assignLicenseDto.UserId,
                        GroupId = assignLicenseDto.GroupId
                    };

                    licenses.Add(license);
                }

                _context.Licenses.AddRange(licenses);
                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "assign_license",
                    Description = $"Assigned {assignLicenseDto.LicenseCount} license(s) for {assignLicenseDto.ProductName}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"{assignLicenseDto.LicenseCount} license(s) assigned successfully",
                    licenses = licenses.Select(l => new { l.Id, l.LicenseKey })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning license");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/LicenseManagement/bulk-assign
        [HttpPost("bulk-assign")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<ActionResult> BulkAssignLicenses([FromBody] List<AssignLicenseDTO> bulkAssignDto)
        {
            try
            {
                var allLicenses = new List<License>();

                foreach (var dto in bulkAssignDto)
                {
                    for (int i = 0; i < dto.LicenseCount; i++)
                    {
                        var license = new License
                        {
                            ProductName = dto.ProductName,
                            LicenseKey = Guid.NewGuid().ToString().Replace("-", "").ToUpper().Substring(0, 25),
                            LicenseType = dto.LicenseType,
                            Status = "active",
                            ExpiryDate = dto.ExpiryDate,
                            IssuedAt = DateTime.UtcNow,
                            UserId = dto.UserId,
                            GroupId = dto.GroupId
                        };

                        allLicenses.Add(license);
                    }
                }

                _context.Licenses.AddRange(allLicenses);
                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "bulk_assign_licenses",
                    Description = $"Bulk assigned {allLicenses.Count} license(s)",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"{allLicenses.Count} license(s) assigned successfully",
                    totalLicenses = allLicenses.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in bulk license assignment");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/LicenseManagement/{id}/revoke
        [HttpDelete("{id}/revoke")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<ActionResult> RevokeLicense(int id)
        {
            try
            {
                var license = await _context.Licenses.FindAsync(id);
                if (license == null)
                {
                    return NotFound(new { message = "License not found" });
                }

                license.Status = "revoked";
                license.RevokedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "revoke_license",
                    Description = $"Revoked license: {license.LicenseKey}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "License revoked successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking license");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // GET: api/LicenseManagement/audit
        [HttpGet("audit")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult> GetLicenseAudit()
        {
            try
            {
                var audit = new
                {
                    TotalLicenses = await _context.Licenses.CountAsync(),
                    ActiveLicenses = await _context.Licenses.CountAsync(l => l.Status == "active"),
                    ExpiredLicenses = await _context.Licenses.CountAsync(l => l.Status == "expired"),
                    RevokedLicenses = await _context.Licenses.CountAsync(l => l.Status == "revoked"),
                    AssignedLicenses = await _context.Licenses.CountAsync(l => l.UserId != null),
                    UnassignedLicenses = await _context.Licenses.CountAsync(l => l.UserId == null),
                    LicensesByProduct = await _context.Licenses
                        .GroupBy(l => l.ProductName)
                        .Select(g => new
                        {
                            Product = g.Key,
                            Total = g.Count(),
                            Active = g.Count(l => l.Status == "active"),
                            Expired = g.Count(l => l.Status == "expired")
                        })
                        .ToListAsync()
                };

                return Ok(audit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating license audit");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
```

### **Controllers/ReportsController.cs**
```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.Entities;

namespace DSecureAPI.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportsController> _logger;

        public ReportsController(
            ApplicationDbContext context,
            ILogger<ReportsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Reports
        [HttpGet]
        public async Task<ActionResult> GetAllReports()
        {
            try
            {
                var reports = await _context.Reports
                    .Include(r => r.GeneratedByUser)
                    .OrderByDescending(r => r.CreatedAt)
                    .Select(r => new
                    {
                        r.Id,
                        r.Type,
                        r.Status,
                        DeviceCount = r.DeviceCount,
                        r.Method,
                        CreatedAt = r.CreatedAt.ToString("MMM dd, yyyy HH:mm"),
                        CompletedAt = r.CompletedAt != null ? r.CompletedAt.Value.ToString("MMM dd, yyyy HH:mm") : null,
                        GeneratedBy = r.GeneratedByUser != null ? r.GeneratedByUser.Name : "System"
                    })
                    .ToListAsync();

                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching reports");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // POST: api/Reports/generate
        [HttpPost("generate")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult> GenerateReport([FromBody] dynamic reportData)
        {
            try
            {
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");

                var report = new Report
                {
                    Type = reportData.type,
                    Status = "pending",
                    DeviceCount = reportData.deviceCount ?? 0,
                    Method = reportData.method,
                    GeneratedBy = currentUserId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Reports.Add(report);
                await _context.SaveChangesAsync();

                // Simulate report generation (in real app, this would be a background job)
                _ = Task.Run(async () =>
                {
                    await Task.Delay(5000); // Simulate processing
                    report.Status = "completed";
                    report.CompletedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                });

                // Log activity
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "generate_report",
                    Description = $"Generated report: {report.Type}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = "Report generation started",
                    reportId = report.Id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating report");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        // DELETE: api/Reports/{id}
        [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<ActionResult> DeleteReport(int id)
        {
            try
            {
                var report = await _context.Reports.FindAsync(id);
                if (report == null)
                {
                    return NotFound(new { message = "Report not found" });
                }

                _context.Reports.Remove(report);
                await _context.SaveChangesAsync();

                // Log activity
                var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var activity = new UserActivity
                {
                    UserId = currentUserId,
                    Action = "delete_report",
                    Description = $"Deleted report ID: {id}",
                    CreatedAt = DateTime.UtcNow
                };
                _context.UserActivities.Add(activity);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Report deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting report");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }
    }
}
```

---

## ⚙️ **Program.cs Configuration**

### **Program.cs**
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DSecureAPI.Data;
using DSecureAPI.Helpers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    )
);

// Configure JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"] 
    ?? throw new InvalidOperationException("JWT Secret not configured");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"],
            ValidAudience = builder.Configuration["JwtSettings:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
            ClockSkew = TimeSpan.Zero
        };
    });

// Add Authorization
builder.Services.AddAuthorization();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
            ?? new[] { "http://localhost:5173" };

        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// Register Services
builder.Services.AddScoped<JwtHelper>();

// Configure Swagger/OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DSecure API",
        Version = "v1",
        Description = "Admin Dashboard API for DSecure Tech"
    });

    // Add JWT Authentication to Swagger
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: \"Bearer {token}\"",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DSecure API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();

app.UseCors("AllowReactApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Create database if it doesn't exist and run migrations
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        context.Database.Migrate();
        Console.WriteLine("✅ Database migrated successfully!");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "❌ An error occurred while migrating the database.");
    }
}

app.Run();
```

---

## 🗄️ **Database Migration Commands**

### **Create Initial Migration:**
```bash
# Add migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

### **If you need to reset:**
```bash
# Drop database
dotnet ef database drop --force

# Remove migrations
Remove-Item -Recurse -Force .\Migrations\

# Create new migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

---

## 🚀 **Running the API**

### **Development Mode:**
```bash
dotnet run
```

### **Production Mode:**
```bash
dotnet run --configuration Release
```

### **Watch Mode (auto-reload):**
```bash
dotnet watch run
```

---

## 📡 **API Endpoints Summary**

### **Authentication:**
- `POST /api/RoleBasedAuth/login` - Login
- `POST /api/RoleBasedAuth/logout` - Logout

### **Dashboard:**
- `GET /api/AdminDashboard/stats` - Dashboard stats
- `GET /api/AdminDashboard/user-activity` - User activity
- `GET /api/AdminDashboard/groups` - Groups list
- `GET /api/AdminDashboard/licenses` - License data
- `GET /api/AdminDashboard/recent-reports` - Recent reports

### **User Management:**
- `GET /api/UserManagement` - Get all users
- `GET /api/UserManagement/{id}` - Get user by ID
- `POST /api/UserManagement` - Create user
- `PUT /api/UserManagement/{id}` - Update user
- `DELETE /api/UserManagement/{id}` - Delete user

### **Group Management:**
- `GET /api/GroupManagement` - Get all groups
- `GET /api/GroupManagement/{id}` - Get group by ID
- `POST /api/GroupManagement` - Create group
- `PUT /api/GroupManagement/{id}` - Update group
- `DELETE /api/GroupManagement/{id}` - Delete group

### **License Management:**
- `GET /api/LicenseManagement` - Get all licenses
- `POST /api/LicenseManagement/assign` - Assign license
- `POST /api/LicenseManagement/bulk-assign` - Bulk assign licenses
- `DELETE /api/LicenseManagement/{id}/revoke` - Revoke license
- `GET /api/LicenseManagement/audit` - License audit report

### **Reports:**
- `GET /api/Reports` - Get all reports
- `POST /api/Reports/generate` - Generate new report
- `DELETE /api/Reports/{id}` - Delete report

---

## 🔐 **Default Login Credentials**

```
SuperAdmin:
Email: superadmin@dsecuretech.com
Password: Admin@123

Admin:
Email: admin@dsecuretech.com
Password: Admin@123

Manager:
Email: manager@dsecuretech.com
Password: Manager@123

User:
Email: user@dsecuretech.com
Password: User@123
```

---

## ✅ **Testing with Swagger**

1. Run the API: `dotnet run`
2. Open browser: `https://localhost:7XXX/swagger`
3. Test login endpoint
4. Copy JWT token
5. Click "Authorize" button in Swagger
6. Enter: `Bearer {your-token-here}`
7. Test protected endpoints

---

**Perfect! Complete .NET API setup ready! 🚀**