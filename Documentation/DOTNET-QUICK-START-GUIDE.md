# 🚀 DSecure .NET API - Quick Start Guide

## 📋 **Complete Setup in 10 Steps**

### **Step 1: Install .NET SDK**
```bash
# Download from: https://dotnet.microsoft.com/download
# Verify installation:
dotnet --version
# Should show: 8.0.x or higher
```

---

### **Step 2: Create New Project**
```bash
# Create project
dotnet new webapi -n DSecureAPI
cd DSecureAPI

# Install NuGet packages
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 8.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 8.0.0
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer --version 8.0.0
dotnet add package System.IdentityModel.Tokens.Jwt --version 7.0.0
dotnet add package BCrypt.Net-Next --version 4.0.3
dotnet add package Swashbuckle.AspNetCore --version 6.5.0
```

---

### **Step 3: Update appsettings.json**

Copy this content to `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=DSecureDB;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true"
  },
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyThatIsAtLeast32CharactersLong!@#$%",
    "Issuer": "https://api.dsecuretech.com",
    "Audience": "https://dsecuretech.com",
    "ExpirationInMinutes": 1440
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Cors": {
    "AllowedOrigins": [
      "http://localhost:5173",
      "http://localhost:5177"
    ]
  }
}
```

---

### **Step 4: Create Folder Structure**

```bash
# Create folders
mkdir Models
mkdir Models/Entities
mkdir Models/DTOs
mkdir Models/DTOs/Auth
mkdir Models/DTOs/Dashboard
mkdir Models/DTOs/User
mkdir Models/DTOs/Group
mkdir Models/DTOs/License
mkdir Data
mkdir Controllers
mkdir Helpers
```

---

### **Step 5: Copy All Files**

अब मैं आपको सभी files की content दूंगा। बस copy-paste करते जाओ:

#### **5.1 Helpers/JwtHelper.cs**
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
            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JwtSettings:Secret"]!));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["JwtSettings:Issuer"],
                audience: _configuration["JwtSettings:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(Convert.ToDouble(_configuration["JwtSettings:ExpirationInMinutes"])),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
```

#### **5.2 Models/Entities/User.cs**
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
        public string Role { get; set; } = "user";

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginAt { get; set; }
    }
}
```

#### **5.3 Models/DTOs/Auth/LoginRequestDTO.cs**
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
        public string Password { get; set; } = string.Empty;
    }
}
```

#### **5.4 Models/DTOs/Auth/LoginResponseDTO.cs**
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
    }
}
```

#### **5.5 Data/ApplicationDbContext.cs**
```csharp
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Models.Entities;

namespace DSecureAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // Seed users
            modelBuilder.Entity<User>().HasData(
                new User
                {
                    Id = 1,
                    Name = "Super Admin",
                    Email = "superadmin@dsecuretech.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "superadmin",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 2,
                    Name = "Admin User",
                    Email = "admin@dsecuretech.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "admin",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 3,
                    Name = "Manager User",
                    Email = "manager@dsecuretech.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Manager@123"),
                    Role = "manager",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                },
                new User
                {
                    Id = 4,
                    Name = "Regular User",
                    Email = "user@dsecuretech.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                    Role = "user",
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                }
            );
        }
    }
}
```

#### **5.6 Controllers/RoleBasedAuthController.cs**
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

        public RoleBasedAuthController(ApplicationDbContext context, JwtHelper jwtHelper)
        {
            _context = context;
            _jwtHelper = jwtHelper;
        }

        [HttpPost("login")]
        public async Task<ActionResult<LoginResponseDTO>> Login([FromBody] LoginRequestDTO request)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                return Unauthorized(new { message = "Invalid email or password" });
            }

            if (user.Status != "active")
            {
                return Unauthorized(new { message = "Account is not active" });
            }

            user.LastLoginAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var token = _jwtHelper.GenerateToken(user);

            return Ok(new LoginResponseDTO
            {
                Token = token,
                Role = user.Role,
                ExpiresAt = DateTime.UtcNow.AddMinutes(1440),
                User = new UserTokenDTO
                {
                    Id = user.Id,
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role
                }
            });
        }
    }
}
```

#### **5.7 Program.cs** (Replace entire file)
```csharp
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using DSecureAPI.Data;
using DSecureAPI.Helpers;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

var jwtSecret = builder.Configuration["JwtSettings:Secret"]!;
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

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:5177")
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

builder.Services.AddScoped<JwtHelper>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DSecure API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization",
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
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowReactApp");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    context.Database.Migrate();
}

app.Run();
```

---

### **Step 6: Create Database Migration**

```bash
# Add migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

---

### **Step 7: Run the API**

```bash
dotnet run
```

**API will start at:**
- HTTPS: `https://localhost:7XXX`
- HTTP: `http://localhost:5XXX`
- Swagger: `https://localhost:7XXX/swagger`

---

### **Step 8: Test Login with Swagger**

1. Open: `https://localhost:7XXX/swagger`
2. Find: `POST /api/RoleBasedAuth/login`
3. Click "Try it out"
4. Use credentials:
```json
{
  "email": "admin@dsecuretech.com",
  "password": "Admin@123"
}
```
5. Click "Execute"
6. Copy the `token` from response

---

### **Step 9: Update Frontend API URL**

In your React app, update `.env`:

```env
VITE_API_BASE_URL=https://localhost:7XXX
```

---

### **Step 10: Test from Frontend**

अब आपका React app API से connect हो जाएगा! 🎉

---

## 📊 **Default Login Credentials**

| Role | Email | Password |
|------|-------|----------|
| SuperAdmin | superadmin@dsecuretech.com | Admin@123 |
| Admin | admin@dsecuretech.com | Admin@123 |
| Manager | manager@dsecuretech.com | Manager@123 |
| User | user@dsecuretech.com | User@123 |

---

## 🔧 **Troubleshooting**

### **Problem: Database connection error**
```bash
# Solution: Check SQL Server is running
# Update connection string in appsettings.json
```

### **Problem: Migration error**
```bash
# Solution: Install EF Core tools globally
dotnet tool install --global dotnet-ef

# Then retry migration
dotnet ef migrations add InitialCreate
```

### **Problem: CORS error**
```bash
# Solution: Add your frontend URL to appsettings.json
"Cors": {
  "AllowedOrigins": ["http://localhost:5173"]
}
```

---

## ✅ **Verification Checklist**

- [ ] .NET SDK 8.0+ installed
- [ ] SQL Server running
- [ ] All NuGet packages installed
- [ ] Database created and migrated
- [ ] API running on localhost
- [ ] Swagger UI accessible
- [ ] Login endpoint working
- [ ] JWT token generated
- [ ] Frontend connected to API
- [ ] CORS configured properly

---

## 🚀 **Next Steps**

1. **Add More Entities:**
   - Copy Group, License, Report models from Part 1
   - Add corresponding controllers

2. **Add Dashboard Endpoints:**
   - Copy AdminDashboardController
   - Test stats, user-activity endpoints

3. **Implement Role-Based Access:**
   - Add `[Authorize(Roles = "admin")]` attributes
   - Test different role permissions

4. **Deploy to Production:**
   - Configure production connection string
   - Update CORS for production URL
   - Deploy to Azure/AWS

---

## 📚 **Complete Documentation**

Full documentation available in:
- `DOTNET-API-COMPLETE-SETUP.md` - Complete setup with entities & DTOs
- `DOTNET-API-SETUP-PART2.md` - All controllers (User, Group, License, Reports)
- `DOTNET-PERFORMANCE-AUDIT-API.md` - Performance & Audit Reports complete API
- `DOTNET-MACHINES-API.md` - **NEW!** Machines Management API
- `DOTNET-LOGS-API.md` - **NEW!** Logs Management API
- `DOTNET-SUBUSERS-API.md` - **NEW!** Subusers/Team Members API

---

## 🆕 **Additional Features Available**

### **Performance & Audit Reports API**
- ✅ CPU, Memory, Disk, Network monitoring
- ✅ Real-time metrics tracking
- ✅ Historical performance data
- ✅ License compliance audits
- ✅ User access & security audits
- ✅ Compliance scoring (0-100)
- ✅ Finding categorization & recommendations

### **Machines Management API** (NEW!)
- ✅ Complete machine inventory management
- ✅ Multi-OS support (Windows, Linux, macOS)
- ✅ Hardware specs tracking (CPU, RAM, Disk)
- ✅ Network information (IP, MAC address)
- ✅ License assignment to machines
- ✅ Real-time connection status
- ✅ User & Group assignment
- ✅ Machine statistics dashboard

### **Logs Management API** (NEW!)
- ✅ Machine logs (System, Security, Application, Network)
- ✅ System logs (API, Authentication, Database)
- ✅ Log type classification (info, warning, error, critical)
- ✅ Severity levels & status management
- ✅ Acknowledgement system
- ✅ Advanced filtering & search
- ✅ Performance monitoring (execution time)
- ✅ Comprehensive statistics

### **Subusers/Team Members API** (NEW!)
- ✅ Hierarchical user management (Parent → Subusers)
- ✅ Role-based access (subuser, team_member, limited_admin)
- ✅ Access levels (full, limited, read_only)
- ✅ Granular permissions system
- ✅ Machine & license assignment with limits
- ✅ Email verification system
- ✅ Password management & security
- ✅ Session tracking & account locking
- ✅ Notification preferences

**📖 See respective documentation files for complete implementation!**

---

**Perfect! Ab aapka complete .NET API ready hai! 🎉**

**Questions? Check Swagger documentation or refer to complete setup files! 🚀**
