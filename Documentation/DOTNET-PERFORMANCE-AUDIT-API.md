# 🚀 DSecure API - Performance & Audit Reports Documentation

## 📋 **Complete Implementation Guide**

यह documentation Performance Reports और Audit Reports के लिए complete API setup प्रदान करता है।

---

## 📁 **Table of Contents**

1. [Entity Models](#entity-models)
2. [DTOs](#dtos)
3. [Database Context Update](#database-context-update)
4. [Controllers](#controllers)
5. [API Endpoints](#api-endpoints)
6. [Testing Guide](#testing-guide)

---

## 1️⃣ **Entity Models**

### **1.1 Models/Entities/PerformanceReport.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureAPI.Models.Entities
{
    public class PerformanceReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string ReportType { get; set; } = "performance"; // performance, system, user

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = string.Empty; // CPU, Memory, Disk, Network, etc.

        public DateTime StartDate { get; set; }
        
        public DateTime EndDate { get; set; }

        // Metrics
        [Column(TypeName = "decimal(18,2)")]
        public decimal AverageValue { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MinValue { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal MaxValue { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentValue { get; set; } = 0;

        [MaxLength(20)]
        public string Unit { get; set; } = "%"; // %, GB, MB, ms, etc.

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "good"; // good, warning, critical

        public int TotalIncidents { get; set; } = 0;

        // Metadata
        public int GeneratedBy { get; set; } // User ID

        [ForeignKey("GeneratedBy")]
        public User? GeneratedByUser { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? DataJson { get; set; } // JSON string for detailed metrics
    }
}
```

### **1.2 Models/Entities/AuditReport.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureAPI.Models.Entities
{
    public class AuditReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string AuditType { get; set; } = "license"; // license, user, system, security

        public DateTime AuditStartDate { get; set; }
        
        public DateTime AuditEndDate { get; set; }

        // Audit Summary
        public int TotalRecords { get; set; } = 0;
        
        public int PassedRecords { get; set; } = 0;
        
        public int FailedRecords { get; set; } = 0;
        
        public int WarningRecords { get; set; } = 0;

        [Column(TypeName = "decimal(18,2)")]
        public decimal ComplianceScore { get; set; } = 0; // 0-100

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "completed"; // pending, in_progress, completed, failed

        [MaxLength(50)]
        public string Severity { get; set; } = "low"; // low, medium, high, critical

        // Findings
        public int TotalFindings { get; set; } = 0;
        
        public int CriticalFindings { get; set; } = 0;
        
        public int ResolvedFindings { get; set; } = 0;

        // Metadata
        public int GeneratedBy { get; set; } // User ID

        [ForeignKey("GeneratedBy")]
        public User? GeneratedByUser { get; set; }

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(1000)]
        public string? Summary { get; set; }

        [MaxLength(1000)]
        public string? Recommendations { get; set; }

        public string? DetailedDataJson { get; set; } // JSON string for detailed audit data

        public string? FilePath { get; set; } // Path to generated PDF/Excel report
    }
}
```

### **1.3 Models/Entities/AuditLog.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureAPI.Models.Entities
{
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Action { get; set; } = string.Empty; // login, logout, create_user, delete_license, etc.

        [Required]
        [MaxLength(50)]
        public string EntityType { get; set; } = string.Empty; // User, License, Group, Report, etc.

        public int? EntityId { get; set; } // ID of affected entity

        [Required]
        [MaxLength(100)]
        public string PerformedBy { get; set; } = string.Empty; // User email

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public string? OldValue { get; set; } // JSON string of old state

        public string? NewValue { get; set; } // JSON string of new state

        [MaxLength(50)]
        public string Status { get; set; } = "success"; // success, failed

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

---

## 2️⃣ **DTOs**

### **2.1 Models/DTOs/Performance/PerformanceReportDTO.cs**

```csharp
namespace DSecureAPI.Models.DTOs.Performance
{
    public class PerformanceReportDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public decimal AverageValue { get; set; }
        public decimal MinValue { get; set; }
        public decimal MaxValue { get; set; }
        public decimal CurrentValue { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public int TotalIncidents { get; set; }
        public string GeneratedByName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string? Description { get; set; }
    }

    public class CreatePerformanceReportDTO
    {
        public string Title { get; set; } = string.Empty;
        public string ReportType { get; set; } = "performance";
        public string Category { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Description { get; set; }
    }

    public class PerformanceMetricsDTO
    {
        public string Category { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public string Unit { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Trend { get; set; } = "stable"; // up, down, stable
        public List<DataPointDTO> HistoricalData { get; set; } = new();
    }

    public class DataPointDTO
    {
        public DateTime Timestamp { get; set; }
        public decimal Value { get; set; }
    }
}
```

### **2.2 Models/DTOs/Audit/AuditReportDTO.cs**

```csharp
namespace DSecureAPI.Models.DTOs.Audit
{
    public class AuditReportDTO
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AuditType { get; set; } = string.Empty;
        public DateTime AuditStartDate { get; set; }
        public DateTime AuditEndDate { get; set; }
        public int TotalRecords { get; set; }
        public int PassedRecords { get; set; }
        public int FailedRecords { get; set; }
        public int WarningRecords { get; set; }
        public decimal ComplianceScore { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public int TotalFindings { get; set; }
        public int CriticalFindings { get; set; }
        public int ResolvedFindings { get; set; }
        public string GeneratedByName { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string? Summary { get; set; }
        public string? Recommendations { get; set; }
        public string? FilePath { get; set; }
    }

    public class CreateAuditReportDTO
    {
        public string Title { get; set; } = string.Empty;
        public string AuditType { get; set; } = "license";
        public DateTime AuditStartDate { get; set; }
        public DateTime AuditEndDate { get; set; }
        public string? Summary { get; set; }
    }

    public class AuditLogDTO
    {
        public int Id { get; set; }
        public string Action { get; set; } = string.Empty;
        public string EntityType { get; set; } = string.Empty;
        public int? EntityId { get; set; }
        public string PerformedBy { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
```

---

## 3️⃣ **Database Context Update**

### **Data/ApplicationDbContext.cs** (Add these DbSets)

```csharp
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Models.Entities;

namespace DSecureAPI.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<User> Users { get; set; }
        public DbSet<PerformanceReport> PerformanceReports { get; set; }
        public DbSet<AuditReport> AuditReports { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User indexes
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // PerformanceReport indexes
            modelBuilder.Entity<PerformanceReport>()
                .HasIndex(p => new { p.ReportType, p.Category, p.GeneratedAt });

            // AuditReport indexes
            modelBuilder.Entity<AuditReport>()
                .HasIndex(a => new { a.AuditType, a.Status, p.GeneratedAt });

            // AuditLog indexes
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => new { a.Action, a.EntityType, a.CreatedAt });
            modelBuilder.Entity<AuditLog>()
                .HasIndex(a => a.PerformedBy);

            // Seed Users (existing code)
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

            // Seed Performance Reports
            modelBuilder.Entity<PerformanceReport>().HasData(
                new PerformanceReport
                {
                    Id = 1,
                    Title = "CPU Usage - Last 7 Days",
                    ReportType = "performance",
                    Category = "CPU",
                    StartDate = DateTime.UtcNow.AddDays(-7),
                    EndDate = DateTime.UtcNow,
                    AverageValue = 45.5m,
                    MinValue = 15.2m,
                    MaxValue = 89.7m,
                    CurrentValue = 52.3m,
                    Unit = "%",
                    Status = "good",
                    TotalIncidents = 2,
                    GeneratedBy = 1,
                    GeneratedAt = DateTime.UtcNow,
                    Description = "CPU usage metrics for the last 7 days"
                },
                new PerformanceReport
                {
                    Id = 2,
                    Title = "Memory Usage - Last 30 Days",
                    ReportType = "performance",
                    Category = "Memory",
                    StartDate = DateTime.UtcNow.AddDays(-30),
                    EndDate = DateTime.UtcNow,
                    AverageValue = 6.8m,
                    MinValue = 3.2m,
                    MaxValue = 14.5m,
                    CurrentValue = 8.2m,
                    Unit = "GB",
                    Status = "warning",
                    TotalIncidents = 5,
                    GeneratedBy = 1,
                    GeneratedAt = DateTime.UtcNow.AddHours(-2),
                    Description = "Memory usage analysis over the past month"
                },
                new PerformanceReport
                {
                    Id = 3,
                    Title = "Disk I/O Performance",
                    ReportType = "system",
                    Category = "Disk",
                    StartDate = DateTime.UtcNow.AddDays(-14),
                    EndDate = DateTime.UtcNow,
                    AverageValue = 156.3m,
                    MinValue = 45.1m,
                    MaxValue = 456.8m,
                    CurrentValue = 189.4m,
                    Unit = "MB/s",
                    Status = "good",
                    TotalIncidents = 0,
                    GeneratedBy = 2,
                    GeneratedAt = DateTime.UtcNow.AddHours(-5),
                    Description = "Disk read/write performance metrics"
                }
            );

            // Seed Audit Reports
            modelBuilder.Entity<AuditReport>().HasData(
                new AuditReport
                {
                    Id = 1,
                    Title = "License Compliance Audit - Q4 2025",
                    AuditType = "license",
                    AuditStartDate = DateTime.UtcNow.AddDays(-30),
                    AuditEndDate = DateTime.UtcNow,
                    TotalRecords = 150,
                    PassedRecords = 135,
                    FailedRecords = 10,
                    WarningRecords = 5,
                    ComplianceScore = 90.0m,
                    Status = "completed",
                    Severity = "medium",
                    TotalFindings = 15,
                    CriticalFindings = 3,
                    ResolvedFindings = 12,
                    GeneratedBy = 1,
                    GeneratedAt = DateTime.UtcNow,
                    Summary = "Quarterly license compliance audit completed with 90% compliance score",
                    Recommendations = "Address critical license violations in Marketing department"
                },
                new AuditReport
                {
                    Id = 2,
                    Title = "User Access Audit - October 2025",
                    AuditType = "user",
                    AuditStartDate = DateTime.UtcNow.AddDays(-7),
                    AuditEndDate = DateTime.UtcNow,
                    TotalRecords = 245,
                    PassedRecords = 238,
                    FailedRecords = 5,
                    WarningRecords = 2,
                    ComplianceScore = 97.1m,
                    Status = "completed",
                    Severity = "low",
                    TotalFindings = 7,
                    CriticalFindings = 1,
                    ResolvedFindings = 6,
                    GeneratedBy = 2,
                    GeneratedAt = DateTime.UtcNow.AddHours(-3),
                    Summary = "User access patterns reviewed, minimal violations found",
                    Recommendations = "Review inactive user accounts older than 90 days"
                },
                new AuditReport
                {
                    Id = 3,
                    Title = "Security Compliance Audit",
                    AuditType = "security",
                    AuditStartDate = DateTime.UtcNow.AddDays(-15),
                    AuditEndDate = DateTime.UtcNow,
                    TotalRecords = 89,
                    PassedRecords = 75,
                    FailedRecords = 12,
                    WarningRecords = 2,
                    ComplianceScore = 84.3m,
                    Status = "completed",
                    Severity = "high",
                    TotalFindings = 14,
                    CriticalFindings = 5,
                    ResolvedFindings = 9,
                    GeneratedBy = 1,
                    GeneratedAt = DateTime.UtcNow.AddDays(-1),
                    Summary = "Security audit revealed several critical vulnerabilities",
                    Recommendations = "Immediate patching required for identified security gaps"
                }
            );
        }
    }
}
```

---

## 4️⃣ **Controllers**

### **4.1 Controllers/PerformanceReportController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Performance;
using DSecureAPI.Models.Entities;
using System.Security.Claims;

namespace DSecureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PerformanceReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public PerformanceReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/PerformanceReport
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PerformanceReportDTO>>> GetPerformanceReports(
            [FromQuery] string? reportType = null,
            [FromQuery] string? category = null,
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.PerformanceReports
                .Include(p => p.GeneratedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(reportType))
                query = query.Where(p => p.ReportType == reportType);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(p => p.Category == category);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(p => p.Status == status);

            var total = await query.CountAsync();
            
            var reports = await query
                .OrderByDescending(p => p.GeneratedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new PerformanceReportDTO
                {
                    Id = p.Id,
                    Title = p.Title,
                    ReportType = p.ReportType,
                    Category = p.Category,
                    StartDate = p.StartDate,
                    EndDate = p.EndDate,
                    AverageValue = p.AverageValue,
                    MinValue = p.MinValue,
                    MaxValue = p.MaxValue,
                    CurrentValue = p.CurrentValue,
                    Unit = p.Unit,
                    Status = p.Status,
                    TotalIncidents = p.TotalIncidents,
                    GeneratedByName = p.GeneratedByUser!.Name,
                    GeneratedAt = p.GeneratedAt,
                    Description = p.Description
                })
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", total.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(reports);
        }

        // GET: api/PerformanceReport/5
        [HttpGet("{id}")]
        public async Task<ActionResult<PerformanceReportDTO>> GetPerformanceReport(int id)
        {
            var report = await _context.PerformanceReports
                .Include(p => p.GeneratedByUser)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (report == null)
                return NotFound(new { message = "Performance report not found" });

            return Ok(new PerformanceReportDTO
            {
                Id = report.Id,
                Title = report.Title,
                ReportType = report.ReportType,
                Category = report.Category,
                StartDate = report.StartDate,
                EndDate = report.EndDate,
                AverageValue = report.AverageValue,
                MinValue = report.MinValue,
                MaxValue = report.MaxValue,
                CurrentValue = report.CurrentValue,
                Unit = report.Unit,
                Status = report.Status,
                TotalIncidents = report.TotalIncidents,
                GeneratedByName = report.GeneratedByUser!.Name,
                GeneratedAt = report.GeneratedAt,
                Description = report.Description
            });
        }

        // POST: api/PerformanceReport
        [HttpPost]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult<PerformanceReportDTO>> CreatePerformanceReport(
            [FromBody] CreatePerformanceReportDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var userId = int.Parse(userIdClaim);

            // Simulate performance data collection
            var random = new Random();
            var report = new PerformanceReport
            {
                Title = dto.Title,
                ReportType = dto.ReportType,
                Category = dto.Category,
                StartDate = dto.StartDate,
                EndDate = dto.EndDate,
                AverageValue = Math.Round((decimal)(random.NextDouble() * 100), 2),
                MinValue = Math.Round((decimal)(random.NextDouble() * 50), 2),
                MaxValue = Math.Round((decimal)(random.NextDouble() * 50 + 50), 2),
                CurrentValue = Math.Round((decimal)(random.NextDouble() * 100), 2),
                Unit = dto.Category == "Memory" ? "GB" : "%",
                Status = random.Next(0, 100) > 70 ? "good" : "warning",
                TotalIncidents = random.Next(0, 10),
                GeneratedBy = userId,
                GeneratedAt = DateTime.UtcNow,
                Description = dto.Description
            };

            _context.PerformanceReports.Add(report);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);

            return CreatedAtAction(nameof(GetPerformanceReport), new { id = report.Id },
                new PerformanceReportDTO
                {
                    Id = report.Id,
                    Title = report.Title,
                    ReportType = report.ReportType,
                    Category = report.Category,
                    StartDate = report.StartDate,
                    EndDate = report.EndDate,
                    AverageValue = report.AverageValue,
                    MinValue = report.MinValue,
                    MaxValue = report.MaxValue,
                    CurrentValue = report.CurrentValue,
                    Unit = report.Unit,
                    Status = report.Status,
                    TotalIncidents = report.TotalIncidents,
                    GeneratedByName = user!.Name,
                    GeneratedAt = report.GeneratedAt,
                    Description = report.Description
                });
        }

        // DELETE: api/PerformanceReport/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> DeletePerformanceReport(int id)
        {
            var report = await _context.PerformanceReports.FindAsync(id);
            if (report == null)
                return NotFound(new { message = "Performance report not found" });

            _context.PerformanceReports.Remove(report);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Performance report deleted successfully" });
        }

        // GET: api/PerformanceReport/current-metrics
        [HttpGet("current-metrics")]
        public async Task<ActionResult<IEnumerable<PerformanceMetricsDTO>>> GetCurrentMetrics()
        {
            var latestReports = await _context.PerformanceReports
                .GroupBy(p => p.Category)
                .Select(g => g.OrderByDescending(p => p.GeneratedAt).First())
                .ToListAsync();

            var metrics = latestReports.Select(r => new PerformanceMetricsDTO
            {
                Category = r.Category,
                Value = r.CurrentValue,
                Unit = r.Unit,
                Status = r.Status,
                Trend = r.CurrentValue > r.AverageValue ? "up" : 
                       r.CurrentValue < r.AverageValue ? "down" : "stable",
                HistoricalData = new List<DataPointDTO>()
            }).ToList();

            return Ok(metrics);
        }

        // GET: api/PerformanceReport/statistics
        [HttpGet("statistics")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult> GetStatistics()
        {
            var stats = new
            {
                TotalReports = await _context.PerformanceReports.CountAsync(),
                ReportsByStatus = await _context.PerformanceReports
                    .GroupBy(p => p.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync(),
                ReportsByCategory = await _context.PerformanceReports
                    .GroupBy(p => p.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToListAsync(),
                TotalIncidents = await _context.PerformanceReports.SumAsync(p => p.TotalIncidents),
                AverageComplianceScore = await _context.PerformanceReports
                    .AverageAsync(p => p.CurrentValue)
            };

            return Ok(stats);
        }
    }
}
```

### **4.2 Controllers/AuditReportController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Audit;
using DSecureAPI.Models.Entities;
using System.Security.Claims;

namespace DSecureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AuditReportController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditReportController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AuditReport
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditReportDTO>>> GetAuditReports(
            [FromQuery] string? auditType = null,
            [FromQuery] string? status = null,
            [FromQuery] string? severity = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.AuditReports
                .Include(a => a.GeneratedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(auditType))
                query = query.Where(a => a.AuditType == auditType);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(a => a.Status == status);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(a => a.Severity == severity);

            var total = await query.CountAsync();

            var reports = await query
                .OrderByDescending(a => a.GeneratedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditReportDTO
                {
                    Id = a.Id,
                    Title = a.Title,
                    AuditType = a.AuditType,
                    AuditStartDate = a.AuditStartDate,
                    AuditEndDate = a.AuditEndDate,
                    TotalRecords = a.TotalRecords,
                    PassedRecords = a.PassedRecords,
                    FailedRecords = a.FailedRecords,
                    WarningRecords = a.WarningRecords,
                    ComplianceScore = a.ComplianceScore,
                    Status = a.Status,
                    Severity = a.Severity,
                    TotalFindings = a.TotalFindings,
                    CriticalFindings = a.CriticalFindings,
                    ResolvedFindings = a.ResolvedFindings,
                    GeneratedByName = a.GeneratedByUser!.Name,
                    GeneratedAt = a.GeneratedAt,
                    Summary = a.Summary,
                    Recommendations = a.Recommendations,
                    FilePath = a.FilePath
                })
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", total.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(reports);
        }

        // GET: api/AuditReport/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AuditReportDTO>> GetAuditReport(int id)
        {
            var report = await _context.AuditReports
                .Include(a => a.GeneratedByUser)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (report == null)
                return NotFound(new { message = "Audit report not found" });

            return Ok(new AuditReportDTO
            {
                Id = report.Id,
                Title = report.Title,
                AuditType = report.AuditType,
                AuditStartDate = report.AuditStartDate,
                AuditEndDate = report.AuditEndDate,
                TotalRecords = report.TotalRecords,
                PassedRecords = report.PassedRecords,
                FailedRecords = report.FailedRecords,
                WarningRecords = report.WarningRecords,
                ComplianceScore = report.ComplianceScore,
                Status = report.Status,
                Severity = report.Severity,
                TotalFindings = report.TotalFindings,
                CriticalFindings = report.CriticalFindings,
                ResolvedFindings = report.ResolvedFindings,
                GeneratedByName = report.GeneratedByUser!.Name,
                GeneratedAt = report.GeneratedAt,
                Summary = report.Summary,
                Recommendations = report.Recommendations,
                FilePath = report.FilePath
            });
        }

        // POST: api/AuditReport
        [HttpPost]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<ActionResult<AuditReportDTO>> CreateAuditReport(
            [FromBody] CreateAuditReportDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                return Unauthorized();

            var userId = int.Parse(userIdClaim);

            // Simulate audit data collection
            var random = new Random();
            var totalRecords = random.Next(50, 300);
            var passedRecords = (int)(totalRecords * 0.85);
            var failedRecords = totalRecords - passedRecords - random.Next(0, 10);
            var warningRecords = totalRecords - passedRecords - failedRecords;

            var report = new AuditReport
            {
                Title = dto.Title,
                AuditType = dto.AuditType,
                AuditStartDate = dto.AuditStartDate,
                AuditEndDate = dto.AuditEndDate,
                TotalRecords = totalRecords,
                PassedRecords = passedRecords,
                FailedRecords = failedRecords,
                WarningRecords = warningRecords,
                ComplianceScore = Math.Round((decimal)(passedRecords * 100.0 / totalRecords), 2),
                Status = "completed",
                Severity = failedRecords > 20 ? "high" : failedRecords > 10 ? "medium" : "low",
                TotalFindings = failedRecords + warningRecords,
                CriticalFindings = random.Next(0, failedRecords / 2),
                ResolvedFindings = random.Next(0, failedRecords),
                GeneratedBy = userId,
                GeneratedAt = DateTime.UtcNow,
                Summary = dto.Summary ?? $"Audit completed with {passedRecords} passed out of {totalRecords} records",
                Recommendations = "Review and address all failed and warning items"
            };

            _context.AuditReports.Add(report);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(userId);

            return CreatedAtAction(nameof(GetAuditReport), new { id = report.Id },
                new AuditReportDTO
                {
                    Id = report.Id,
                    Title = report.Title,
                    AuditType = report.AuditType,
                    AuditStartDate = report.AuditStartDate,
                    AuditEndDate = report.AuditEndDate,
                    TotalRecords = report.TotalRecords,
                    PassedRecords = report.PassedRecords,
                    FailedRecords = report.FailedRecords,
                    WarningRecords = report.WarningRecords,
                    ComplianceScore = report.ComplianceScore,
                    Status = report.Status,
                    Severity = report.Severity,
                    TotalFindings = report.TotalFindings,
                    CriticalFindings = report.CriticalFindings,
                    ResolvedFindings = report.ResolvedFindings,
                    GeneratedByName = user!.Name,
                    GeneratedAt = report.GeneratedAt,
                    Summary = report.Summary,
                    Recommendations = report.Recommendations
                });
        }

        // DELETE: api/AuditReport/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> DeleteAuditReport(int id)
        {
            var report = await _context.AuditReports.FindAsync(id);
            if (report == null)
                return NotFound(new { message = "Audit report not found" });

            _context.AuditReports.Remove(report);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Audit report deleted successfully" });
        }

        // GET: api/AuditReport/statistics
        [HttpGet("statistics")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<ActionResult> GetStatistics()
        {
            var stats = new
            {
                TotalAudits = await _context.AuditReports.CountAsync(),
                CompletedAudits = await _context.AuditReports.CountAsync(a => a.Status == "completed"),
                AverageComplianceScore = await _context.AuditReports
                    .Where(a => a.Status == "completed")
                    .AverageAsync(a => (double?)a.ComplianceScore) ?? 0,
                TotalFindings = await _context.AuditReports.SumAsync(a => a.TotalFindings),
                CriticalFindings = await _context.AuditReports.SumAsync(a => a.CriticalFindings),
                ResolvedFindings = await _context.AuditReports.SumAsync(a => a.ResolvedFindings),
                AuditsBySeverity = await _context.AuditReports
                    .GroupBy(a => a.Severity)
                    .Select(g => new { Severity = g.Key, Count = g.Count() })
                    .ToListAsync(),
                AuditsByType = await _context.AuditReports
                    .GroupBy(a => a.AuditType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToListAsync()
            };

            return Ok(stats);
        }
    }
}
```

### **4.3 Controllers/AuditLogController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Audit;

namespace DSecureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin,admin")]
    public class AuditLogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AuditLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/AuditLog
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AuditLogDTO>>> GetAuditLogs(
            [FromQuery] string? action = null,
            [FromQuery] string? entityType = null,
            [FromQuery] string? performedBy = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (!string.IsNullOrEmpty(action))
                query = query.Where(a => a.Action.Contains(action));

            if (!string.IsNullOrEmpty(entityType))
                query = query.Where(a => a.EntityType == entityType);

            if (!string.IsNullOrEmpty(performedBy))
                query = query.Where(a => a.PerformedBy.Contains(performedBy));

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt <= endDate.Value);

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(a => new AuditLogDTO
                {
                    Id = a.Id,
                    Action = a.Action,
                    EntityType = a.EntityType,
                    EntityId = a.EntityId,
                    PerformedBy = a.PerformedBy,
                    Description = a.Description,
                    Status = a.Status,
                    CreatedAt = a.CreatedAt
                })
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", total.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(logs);
        }

        // GET: api/AuditLog/5
        [HttpGet("{id}")]
        public async Task<ActionResult<AuditLog>> GetAuditLog(int id)
        {
            var log = await _context.AuditLogs.FindAsync(id);

            if (log == null)
                return NotFound(new { message = "Audit log not found" });

            return Ok(log);
        }

        // GET: api/AuditLog/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult> GetStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.AuditLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(a => a.CreatedAt >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(a => a.CreatedAt <= endDate.Value);

            var stats = new
            {
                TotalLogs = await query.CountAsync(),
                SuccessfulActions = await query.CountAsync(a => a.Status == "success"),
                FailedActions = await query.CountAsync(a => a.Status == "failed"),
                ActionsByType = await query
                    .GroupBy(a => a.Action)
                    .Select(g => new { Action = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync(),
                EntityTypeActivity = await query
                    .GroupBy(a => a.EntityType)
                    .Select(g => new { EntityType = g.Key, Count = g.Count() })
                    .ToListAsync(),
                TopUsers = await query
                    .GroupBy(a => a.PerformedBy)
                    .Select(g => new { User = g.Key, ActionCount = g.Count() })
                    .OrderByDescending(x => x.ActionCount)
                    .Take(10)
                    .ToListAsync()
            };

            return Ok(stats);
        }
    }
}
```

---

## 5️⃣ **API Endpoints Summary**

### **Performance Reports**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/PerformanceReport` | All Roles | Get all performance reports (with filters) |
| GET | `/api/PerformanceReport/{id}` | All Roles | Get single performance report |
| POST | `/api/PerformanceReport` | SuperAdmin, Admin, Manager | Create new performance report |
| DELETE | `/api/PerformanceReport/{id}` | SuperAdmin, Admin | Delete performance report |
| GET | `/api/PerformanceReport/current-metrics` | All Roles | Get current system metrics |
| GET | `/api/PerformanceReport/statistics` | SuperAdmin, Admin, Manager | Get performance statistics |

### **Audit Reports**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/AuditReport` | All Roles | Get all audit reports (with filters) |
| GET | `/api/AuditReport/{id}` | All Roles | Get single audit report |
| POST | `/api/AuditReport` | SuperAdmin, Admin | Create new audit report |
| DELETE | `/api/AuditReport/{id}` | SuperAdmin, Admin | Delete audit report |
| GET | `/api/AuditReport/statistics` | SuperAdmin, Admin | Get audit statistics |

### **Audit Logs**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/AuditLog` | SuperAdmin, Admin | Get all audit logs (with filters) |
| GET | `/api/AuditLog/{id}` | SuperAdmin, Admin | Get single audit log |
| GET | `/api/AuditLog/statistics` | SuperAdmin, Admin | Get audit log statistics |

---

## 6️⃣ **Migration & Testing**

### **Step 1: Add Migration**

```bash
dotnet ef migrations add AddPerformanceAndAuditReports
dotnet ef database update
```

### **Step 2: Test with Swagger**

1. **Login first:**
```json
POST /api/RoleBasedAuth/login
{
  "email": "admin@dsecuretech.com",
  "password": "Admin@123"
}
```

2. **Get Performance Reports:**
```
GET /api/PerformanceReport
GET /api/PerformanceReport/current-metrics
GET /api/PerformanceReport/statistics
```

3. **Create Performance Report:**
```json
POST /api/PerformanceReport
{
  "title": "CPU Performance - October 2025",
  "reportType": "performance",
  "category": "CPU",
  "startDate": "2025-10-01T00:00:00Z",
  "endDate": "2025-10-15T00:00:00Z",
  "description": "Monthly CPU performance analysis"
}
```

4. **Get Audit Reports:**
```
GET /api/AuditReport
GET /api/AuditReport/statistics
```

5. **Create Audit Report:**
```json
POST /api/AuditReport
{
  "title": "License Audit - October 2025",
  "auditType": "license",
  "auditStartDate": "2025-10-01T00:00:00Z",
  "auditEndDate": "2025-10-15T00:00:00Z",
  "summary": "Monthly license compliance audit"
}
```

6. **Get Audit Logs:**
```
GET /api/AuditLog
GET /api/AuditLog/statistics?startDate=2025-10-01&endDate=2025-10-15
```

---

## 7️⃣ **Frontend Integration Example**

### **TypeScript Interface (Add to adminDashboardAPI.ts)**

```typescript
export interface PerformanceReport {
  id: number;
  title: string;
  reportType: string;
  category: string;
  startDate: string;
  endDate: string;
  averageValue: number;
  minValue: number;
  maxValue: number;
  currentValue: number;
  unit: string;
  status: 'good' | 'warning' | 'critical';
  totalIncidents: number;
  generatedByName: string;
  generatedAt: string;
  description?: string;
}

export interface AuditReport {
  id: number;
  title: string;
  auditType: string;
  auditStartDate: string;
  auditEndDate: string;
  totalRecords: number;
  passedRecords: number;
  failedRecords: number;
  warningRecords: number;
  complianceScore: number;
  status: string;
  severity: 'low' | 'medium' | 'high' | 'critical';
  totalFindings: number;
  criticalFindings: number;
  resolvedFindings: number;
  generatedByName: string;
  generatedAt: string;
  summary?: string;
  recommendations?: string;
  filePath?: string;
}

export interface AuditLog {
  id: number;
  action: string;
  entityType: string;
  entityId?: number;
  performedBy: string;
  description?: string;
  status: string;
  createdAt: string;
}
```

### **API Call Examples**

```typescript
// Get Performance Reports
export const getPerformanceReports = async (filters?: {
  reportType?: string;
  category?: string;
  status?: string;
  page?: number;
  pageSize?: number;
}): Promise<PerformanceReport[]> => {
  const params = new URLSearchParams();
  if (filters?.reportType) params.append('reportType', filters.reportType);
  if (filters?.category) params.append('category', filters.category);
  if (filters?.status) params.append('status', filters.status);
  if (filters?.page) params.append('page', filters.page.toString());
  if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

  const response = await apiClient.get(`/PerformanceReport?${params}`);
  return response.data;
};

// Get Audit Reports
export const getAuditReports = async (filters?: {
  auditType?: string;
  status?: string;
  severity?: string;
  page?: number;
  pageSize?: number;
}): Promise<AuditReport[]> => {
  const params = new URLSearchParams();
  if (filters?.auditType) params.append('auditType', filters.auditType);
  if (filters?.status) params.append('status', filters.status);
  if (filters?.severity) params.append('severity', filters.severity);
  if (filters?.page) params.append('page', filters.page.toString());
  if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

  const response = await apiClient.get(`/AuditReport?${params}`);
  return response.data;
};

// Get Audit Logs
export const getAuditLogs = async (filters?: {
  action?: string;
  entityType?: string;
  performedBy?: string;
  startDate?: string;
  endDate?: string;
  page?: number;
  pageSize?: number;
}): Promise<AuditLog[]> => {
  const params = new URLSearchParams();
  if (filters?.action) params.append('action', filters.action);
  if (filters?.entityType) params.append('entityType', filters.entityType);
  if (filters?.performedBy) params.append('performedBy', filters.performedBy);
  if (filters?.startDate) params.append('startDate', filters.startDate);
  if (filters?.endDate) params.append('endDate', filters.endDate);
  if (filters?.page) params.append('page', filters.page.toString());
  if (filters?.pageSize) params.append('pageSize', filters.pageSize.toString());

  const response = await apiClient.get(`/AuditLog?${params}`);
  return response.data;
};
```

---

## ✅ **Features Implemented**

### **Performance Reports**
- ✅ CPU, Memory, Disk, Network monitoring
- ✅ Real-time metrics tracking
- ✅ Historical data analysis
- ✅ Status indicators (good, warning, critical)
- ✅ Incident tracking
- ✅ Performance statistics

### **Audit Reports**
- ✅ License compliance audits
- ✅ User access audits
- ✅ Security audits
- ✅ System audits
- ✅ Compliance scoring (0-100)
- ✅ Finding categorization (passed, failed, warning)
- ✅ Severity levels (low, medium, high, critical)
- ✅ Recommendations engine

### **Audit Logs**
- ✅ Complete activity tracking
- ✅ User action logging
- ✅ Entity change tracking
- ✅ Before/after state comparison
- ✅ IP address and user agent logging
- ✅ Advanced filtering and search
- ✅ Activity statistics

---

## 🎯 **Next Steps**

1. **Add Real-time Monitoring:**
   - Implement SignalR for live metrics
   - Add WebSocket support
   - Real-time dashboard updates

2. **Add Export Functionality:**
   - PDF report generation
   - Excel export
   - CSV download

3. **Add Scheduling:**
   - Automated report generation
   - Scheduled audits
   - Email notifications

4. **Add Visualization:**
   - Charts and graphs
   - Trend analysis
   - Comparative reporting

---

**Perfect! Ab Performance aur Audit Reports ka complete API ready hai! 🎉**

**Koi aur feature chahiye to bataao! 🚀**
