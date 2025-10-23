# 📋 DSecure API - Logs Management Documentation

## 📋 **Complete Implementation Guide**

यह documentation System Logs & Machine Logs के लिए complete API setup प्रदान करता है।

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

### **Models/Entities/MachineLog.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureAPI.Models.Entities
{
    public class MachineLog
    {
        [Key]
        public int Id { get; set; }

        // Machine Reference
        public int MachineId { get; set; }

        [ForeignKey("MachineId")]
        public Machine? Machine { get; set; }

        [Required]
        [MaxLength(100)]
        public string MachineName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string MachineIdentifier { get; set; } = string.Empty; // MachineId or IP

        // Log Details
        [Required]
        [MaxLength(50)]
        public string LogType { get; set; } = "info"; // info, warning, error, critical, security

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "system"; // system, security, application, network, performance

        [Required]
        [MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EventCode { get; set; }

        [MaxLength(50)]
        public string Severity { get; set; } = "low"; // low, medium, high, critical

        // Source Information
        [MaxLength(100)]
        public string? Source { get; set; }

        [MaxLength(100)]
        public string? ProcessName { get; set; }

        public int? ProcessId { get; set; }

        // User Information
        [MaxLength(100)]
        public string? Username { get; set; }

        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        // Network Information
        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(100)]
        public string? Hostname { get; set; }

        // Additional Data
        public string? AdditionalDataJson { get; set; } // JSON string for extra details

        public string? StackTrace { get; set; }

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "unread"; // unread, read, resolved, ignored

        public bool IsAcknowledged { get; set; } = false;

        public int? AcknowledgedBy { get; set; }

        [ForeignKey("AcknowledgedBy")]
        public User? AcknowledgedByUser { get; set; }

        public DateTime? AcknowledgedAt { get; set; }

        // Timestamps
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

### **Models/Entities/SystemLog.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureAPI.Models.Entities
{
    public class SystemLog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string LogType { get; set; } = "info"; // info, warning, error, critical, audit

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "system"; // system, api, database, authentication, authorization

        [Required]
        [MaxLength(200)]
        public string EventName { get; set; } = string.Empty;

        [Required]
        public string Message { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? EventCode { get; set; }

        [MaxLength(50)]
        public string Severity { get; set; } = "low"; // low, medium, high, critical

        // User Information
        public int? UserId { get; set; }

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [MaxLength(100)]
        public string? Username { get; set; }

        [MaxLength(100)]
        public string? UserEmail { get; set; }

        [MaxLength(50)]
        public string? UserRole { get; set; }

        // Request Information
        [MaxLength(10)]
        public string? HttpMethod { get; set; } // GET, POST, PUT, DELETE

        [MaxLength(500)]
        public string? RequestPath { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(500)]
        public string? UserAgent { get; set; }

        public int? StatusCode { get; set; }

        // Additional Data
        public string? RequestBody { get; set; }

        public string? ResponseBody { get; set; }

        public string? AdditionalDataJson { get; set; } // JSON string for extra details

        public string? StackTrace { get; set; }

        public string? ExceptionType { get; set; }

        // Performance
        public long? ExecutionTimeMs { get; set; }

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "unread"; // unread, read, resolved, investigating

        public bool IsAcknowledged { get; set; } = false;

        public int? AcknowledgedBy { get; set; }

        [ForeignKey("AcknowledgedBy")]
        public User? AcknowledgedByUser { get; set; }

        public DateTime? AcknowledgedAt { get; set; }

        // Timestamps
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
```

---

## 2️⃣ **DTOs**

### **Models/DTOs/Log/MachineLogDTO.cs**

```csharp
namespace DSecureAPI.Models.DTOs.Log
{
    public class MachineLogDTO
    {
        public int Id { get; set; }
        public int MachineId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string MachineIdentifier { get; set; } = string.Empty;
        public string LogType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? EventCode { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string? Source { get; set; }
        public string? ProcessName { get; set; }
        public int? ProcessId { get; set; }
        public string? Username { get; set; }
        public string? IpAddress { get; set; }
        public string? Hostname { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsAcknowledged { get; set; }
        public string? AcknowledgedByName { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateMachineLogDTO
    {
        public int MachineId { get; set; }
        public string LogType { get; set; } = "info";
        public string Category { get; set; } = "system";
        public string EventName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? EventCode { get; set; }
        public string Severity { get; set; } = "low";
        public string? Source { get; set; }
        public string? ProcessName { get; set; }
        public int? ProcessId { get; set; }
        public string? Username { get; set; }
        public string? AdditionalDataJson { get; set; }
    }

    public class MachineLogStatsDTO
    {
        public int TotalLogs { get; set; }
        public int UnreadLogs { get; set; }
        public int CriticalLogs { get; set; }
        public int ErrorLogs { get; set; }
        public int WarningLogs { get; set; }
        public Dictionary<string, int> LogsByType { get; set; } = new();
        public Dictionary<string, int> LogsByCategory { get; set; } = new();
        public Dictionary<string, int> LogsBySeverity { get; set; } = new();
        public List<TopMachineLogsDTO> TopMachinesWithLogs { get; set; } = new();
    }

    public class TopMachineLogsDTO
    {
        public int MachineId { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public int LogCount { get; set; }
        public int CriticalCount { get; set; }
    }
}
```

### **Models/DTOs/Log/SystemLogDTO.cs**

```csharp
namespace DSecureAPI.Models.DTOs.Log
{
    public class SystemLogDTO
    {
        public int Id { get; set; }
        public string LogType { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? EventCode { get; set; }
        public string Severity { get; set; } = string.Empty;
        public string? Username { get; set; }
        public string? UserEmail { get; set; }
        public string? UserRole { get; set; }
        public string? HttpMethod { get; set; }
        public string? RequestPath { get; set; }
        public string? IpAddress { get; set; }
        public int? StatusCode { get; set; }
        public long? ExecutionTimeMs { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsAcknowledged { get; set; }
        public string? AcknowledgedByName { get; set; }
        public DateTime? AcknowledgedAt { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateSystemLogDTO
    {
        public string LogType { get; set; } = "info";
        public string Category { get; set; } = "system";
        public string EventName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string? EventCode { get; set; }
        public string Severity { get; set; } = "low";
        public string? HttpMethod { get; set; }
        public string? RequestPath { get; set; }
        public int? StatusCode { get; set; }
        public string? AdditionalDataJson { get; set; }
    }

    public class SystemLogStatsDTO
    {
        public int TotalLogs { get; set; }
        public int UnreadLogs { get; set; }
        public int CriticalLogs { get; set; }
        public int ErrorLogs { get; set; }
        public int WarningLogs { get; set; }
        public int ApiLogs { get; set; }
        public int AuthenticationLogs { get; set; }
        public Dictionary<string, int> LogsByType { get; set; } = new();
        public Dictionary<string, int> LogsByCategory { get; set; } = new();
        public Dictionary<string, int> LogsBySeverity { get; set; } = new();
        public Dictionary<int, int> LogsByStatusCode { get; set; } = new();
        public double? AverageExecutionTimeMs { get; set; }
    }
}
```

---

## 3️⃣ **Database Context Update**

### **Data/ApplicationDbContext.cs** (Add these DbSets)

```csharp
public DbSet<MachineLog> MachineLogs { get; set; }
public DbSet<SystemLog> SystemLogs { get; set; }

// In OnModelCreating method, add indexes:

// MachineLog indexes
modelBuilder.Entity<MachineLog>()
    .HasIndex(m => new { m.MachineId, m.Timestamp });
modelBuilder.Entity<MachineLog>()
    .HasIndex(m => new { m.LogType, m.Severity, m.Status });
modelBuilder.Entity<MachineLog>()
    .HasIndex(m => m.Timestamp);

// SystemLog indexes
modelBuilder.Entity<SystemLog>()
    .HasIndex(s => new { s.Category, s.Timestamp });
modelBuilder.Entity<SystemLog>()
    .HasIndex(s => new { s.LogType, s.Severity, s.Status });
modelBuilder.Entity<SystemLog>()
    .HasIndex(s => s.UserId);
modelBuilder.Entity<SystemLog>()
    .HasIndex(s => s.Timestamp);

// Seed Machine Logs
modelBuilder.Entity<MachineLog>().HasData(
    new MachineLog
    {
        Id = 1,
        MachineId = 1,
        MachineName = "WIN-WS-001",
        MachineIdentifier = "MACH-001-WIN-2024",
        LogType = "info",
        Category = "system",
        EventName = "System Startup",
        Message = "System started successfully",
        EventCode = "SYS-1000",
        Severity = "low",
        Source = "Windows Event Log",
        Username = "Admin User",
        IpAddress = "192.168.1.101",
        Hostname = "desktop-it-001",
        Status = "read",
        Timestamp = DateTime.UtcNow.AddHours(-2),
        CreatedAt = DateTime.UtcNow.AddHours(-2)
    },
    new MachineLog
    {
        Id = 2,
        MachineId = 1,
        MachineName = "WIN-WS-001",
        MachineIdentifier = "MACH-001-WIN-2024",
        LogType = "warning",
        Category = "security",
        EventName = "Failed Login Attempt",
        Message = "Multiple failed login attempts detected from user account",
        EventCode = "SEC-4625",
        Severity = "medium",
        Source = "Windows Security",
        Username = "unknown_user",
        IpAddress = "192.168.1.101",
        Status = "unread",
        Timestamp = DateTime.UtcNow.AddMinutes(-30),
        CreatedAt = DateTime.UtcNow.AddMinutes(-30)
    },
    new MachineLog
    {
        Id = 3,
        MachineId = 2,
        MachineName = "MAC-WS-002",
        MachineIdentifier = "MACH-002-MAC-2024",
        LogType = "error",
        Category = "application",
        EventName = "Application Crash",
        Message = "Application 'DSecure Agent' terminated unexpectedly",
        EventCode = "APP-500",
        Severity = "high",
        Source = "macOS Console",
        ProcessName = "DSecureAgent",
        ProcessId = 12345,
        Username = "Manager User",
        IpAddress = "192.168.1.102",
        Status = "unread",
        Timestamp = DateTime.UtcNow.AddMinutes(-15),
        CreatedAt = DateTime.UtcNow.AddMinutes(-15)
    },
    new MachineLog
    {
        Id = 4,
        MachineId = 3,
        MachineName = "LINUX-SRV-001",
        MachineIdentifier = "MACH-003-LNX-2024",
        LogType = "critical",
        Category = "performance",
        EventName = "High CPU Usage",
        Message = "CPU usage exceeded 95% for more than 10 minutes",
        EventCode = "PERF-001",
        Severity = "critical",
        Source = "System Monitor",
        IpAddress = "192.168.1.50",
        Hostname = "ubuntu-server-001",
        Status = "unread",
        Timestamp = DateTime.UtcNow.AddMinutes(-5),
        CreatedAt = DateTime.UtcNow.AddMinutes(-5)
    }
);

// Seed System Logs
modelBuilder.Entity<SystemLog>().HasData(
    new SystemLog
    {
        Id = 1,
        LogType = "info",
        Category = "authentication",
        EventName = "User Login",
        Message = "User successfully logged in",
        EventCode = "AUTH-1000",
        Severity = "low",
        UserId = 2,
        Username = "Admin User",
        UserEmail = "admin@dsecuretech.com",
        UserRole = "admin",
        HttpMethod = "POST",
        RequestPath = "/api/RoleBasedAuth/login",
        IpAddress = "192.168.1.10",
        StatusCode = 200,
        ExecutionTimeMs = 145,
        Status = "read",
        Timestamp = DateTime.UtcNow.AddHours(-1),
        CreatedAt = DateTime.UtcNow.AddHours(-1)
    },
    new SystemLog
    {
        Id = 2,
        LogType = "warning",
        Category = "authorization",
        EventName = "Unauthorized Access Attempt",
        Message = "User attempted to access restricted resource",
        EventCode = "AUTH-403",
        Severity = "medium",
        UserId = 4,
        Username = "Regular User",
        UserEmail = "user@dsecuretech.com",
        UserRole = "user",
        HttpMethod = "GET",
        RequestPath = "/api/UserManagement/all",
        IpAddress = "192.168.1.25",
        StatusCode = 403,
        ExecutionTimeMs = 23,
        Status = "unread",
        Timestamp = DateTime.UtcNow.AddMinutes(-45),
        CreatedAt = DateTime.UtcNow.AddMinutes(-45)
    },
    new SystemLog
    {
        Id = 3,
        LogType = "error",
        Category = "database",
        EventName = "Database Connection Failed",
        Message = "Failed to connect to database server",
        EventCode = "DB-500",
        Severity = "high",
        HttpMethod = "GET",
        RequestPath = "/api/Machine",
        IpAddress = "192.168.1.15",
        StatusCode = 500,
        ExecutionTimeMs = 30000,
        ExceptionType = "SqlException",
        Status = "unread",
        Timestamp = DateTime.UtcNow.AddMinutes(-20),
        CreatedAt = DateTime.UtcNow.AddMinutes(-20)
    },
    new SystemLog
    {
        Id = 4,
        LogType = "critical",
        Category = "system",
        EventName = "System Overload",
        Message = "Server resources critically low, system performance degraded",
        EventCode = "SYS-999",
        Severity = "critical",
        StatusCode = 503,
        Status = "unread",
        Timestamp = DateTime.UtcNow.AddMinutes(-10),
        CreatedAt = DateTime.UtcNow.AddMinutes(-10)
    },
    new SystemLog
    {
        Id = 5,
        LogType = "audit",
        Category = "api",
        EventName = "License Assigned",
        Message = "License assigned to machine successfully",
        EventCode = "AUD-2000",
        Severity = "low",
        UserId = 1,
        Username = "Super Admin",
        UserEmail = "superadmin@dsecuretech.com",
        UserRole = "superadmin",
        HttpMethod = "POST",
        RequestPath = "/api/Machine/assign-license",
        IpAddress = "192.168.1.5",
        StatusCode = 200,
        ExecutionTimeMs = 89,
        Status = "read",
        Timestamp = DateTime.UtcNow.AddMinutes(-35),
        CreatedAt = DateTime.UtcNow.AddMinutes(-35)
    }
);
```

---

## 4️⃣ **Controllers**

### **4.1 Controllers/MachineLogController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Log;
using DSecureAPI.Models.Entities;
using System.Security.Claims;

namespace DSecureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MachineLogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MachineLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/MachineLog
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MachineLogDTO>>> GetMachineLogs(
            [FromQuery] int? machineId = null,
            [FromQuery] string? logType = null,
            [FromQuery] string? category = null,
            [FromQuery] string? severity = null,
            [FromQuery] string? status = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.MachineLogs
                .Include(m => m.Machine)
                .Include(m => m.AcknowledgedByUser)
                .AsQueryable();

            if (machineId.HasValue)
                query = query.Where(m => m.MachineId == machineId.Value);

            if (!string.IsNullOrEmpty(logType))
                query = query.Where(m => m.LogType == logType);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(m => m.Category == category);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(m => m.Severity == severity);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(m => m.Status == status);

            if (startDate.HasValue)
                query = query.Where(m => m.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(m => m.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(m =>
                    m.EventName.Contains(search) ||
                    m.Message.Contains(search) ||
                    m.MachineName.Contains(search));

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(m => m.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MachineLogDTO
                {
                    Id = m.Id,
                    MachineId = m.MachineId,
                    MachineName = m.MachineName,
                    MachineIdentifier = m.MachineIdentifier,
                    LogType = m.LogType,
                    Category = m.Category,
                    EventName = m.EventName,
                    Message = m.Message,
                    EventCode = m.EventCode,
                    Severity = m.Severity,
                    Source = m.Source,
                    ProcessName = m.ProcessName,
                    ProcessId = m.ProcessId,
                    Username = m.Username,
                    IpAddress = m.IpAddress,
                    Hostname = m.Hostname,
                    Status = m.Status,
                    IsAcknowledged = m.IsAcknowledged,
                    AcknowledgedByName = m.AcknowledgedByUser != null ? m.AcknowledgedByUser.Name : null,
                    AcknowledgedAt = m.AcknowledgedAt,
                    Timestamp = m.Timestamp,
                    CreatedAt = m.CreatedAt
                })
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", total.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(logs);
        }

        // GET: api/MachineLog/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MachineLog>> GetMachineLog(int id)
        {
            var log = await _context.MachineLogs
                .Include(m => m.Machine)
                .Include(m => m.AcknowledgedByUser)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (log == null)
                return NotFound(new { message = "Machine log not found" });

            return Ok(log);
        }

        // POST: api/MachineLog
        [HttpPost]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult<MachineLogDTO>> CreateMachineLog([FromBody] CreateMachineLogDTO dto)
        {
            var machine = await _context.Machines.FindAsync(dto.MachineId);
            if (machine == null)
                return NotFound(new { message = "Machine not found" });

            var log = new MachineLog
            {
                MachineId = dto.MachineId,
                MachineName = machine.MachineName,
                MachineIdentifier = machine.MachineId,
                LogType = dto.LogType,
                Category = dto.Category,
                EventName = dto.EventName,
                Message = dto.Message,
                EventCode = dto.EventCode,
                Severity = dto.Severity,
                Source = dto.Source,
                ProcessName = dto.ProcessName,
                ProcessId = dto.ProcessId,
                Username = dto.Username,
                IpAddress = machine.IpAddress,
                Hostname = machine.Hostname,
                AdditionalDataJson = dto.AdditionalDataJson,
                Status = "unread",
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.MachineLogs.Add(log);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMachineLog), new { id = log.Id }, log);
        }

        // PUT: api/MachineLog/5/acknowledge
        [HttpPut("{id}/acknowledge")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> AcknowledgeMachineLog(int id)
        {
            var log = await _context.MachineLogs.FindAsync(id);
            if (log == null)
                return NotFound(new { message = "Machine log not found" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdClaim!);

            log.IsAcknowledged = true;
            log.AcknowledgedBy = userId;
            log.AcknowledgedAt = DateTime.UtcNow;
            log.Status = "read";

            await _context.SaveChangesAsync();

            return Ok(new { message = "Machine log acknowledged successfully" });
        }

        // PUT: api/MachineLog/5/status
        [HttpPut("{id}/status")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> UpdateMachineLogStatus(int id, [FromBody] string status)
        {
            var log = await _context.MachineLogs.FindAsync(id);
            if (log == null)
                return NotFound(new { message = "Machine log not found" });

            log.Status = status;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Machine log status updated successfully" });
        }

        // DELETE: api/MachineLog/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> DeleteMachineLog(int id)
        {
            var log = await _context.MachineLogs.FindAsync(id);
            if (log == null)
                return NotFound(new { message = "Machine log not found" });

            _context.MachineLogs.Remove(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Machine log deleted successfully" });
        }

        // GET: api/MachineLog/statistics
        [HttpGet("statistics")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult<MachineLogStatsDTO>> GetStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.MachineLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(m => m.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(m => m.Timestamp <= endDate.Value);

            var stats = new MachineLogStatsDTO
            {
                TotalLogs = await query.CountAsync(),
                UnreadLogs = await query.CountAsync(m => m.Status == "unread"),
                CriticalLogs = await query.CountAsync(m => m.LogType == "critical"),
                ErrorLogs = await query.CountAsync(m => m.LogType == "error"),
                WarningLogs = await query.CountAsync(m => m.LogType == "warning"),
                LogsByType = await query
                    .GroupBy(m => m.LogType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type, x => x.Count),
                LogsByCategory = await query
                    .GroupBy(m => m.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count),
                LogsBySeverity = await query
                    .GroupBy(m => m.Severity)
                    .Select(g => new { Severity = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Severity, x => x.Count),
                TopMachinesWithLogs = await query
                    .GroupBy(m => new { m.MachineId, m.MachineName })
                    .Select(g => new TopMachineLogsDTO
                    {
                        MachineId = g.Key.MachineId,
                        MachineName = g.Key.MachineName,
                        LogCount = g.Count(),
                        CriticalCount = g.Count(m => m.LogType == "critical" || m.LogType == "error")
                    })
                    .OrderByDescending(x => x.LogCount)
                    .Take(10)
                    .ToListAsync()
            };

            return Ok(stats);
        }
    }
}
```

### **4.2 Controllers/SystemLogController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Log;
using DSecureAPI.Models.Entities;
using System.Security.Claims;

namespace DSecureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "superadmin,admin")]
    public class SystemLogController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SystemLogController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/SystemLog
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SystemLogDTO>>> GetSystemLogs(
            [FromQuery] string? logType = null,
            [FromQuery] string? category = null,
            [FromQuery] string? severity = null,
            [FromQuery] string? status = null,
            [FromQuery] int? userId = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var query = _context.SystemLogs
                .Include(s => s.User)
                .Include(s => s.AcknowledgedByUser)
                .AsQueryable();

            if (!string.IsNullOrEmpty(logType))
                query = query.Where(s => s.LogType == logType);

            if (!string.IsNullOrEmpty(category))
                query = query.Where(s => s.Category == category);

            if (!string.IsNullOrEmpty(severity))
                query = query.Where(s => s.Severity == severity);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            if (userId.HasValue)
                query = query.Where(s => s.UserId == userId.Value);

            if (startDate.HasValue)
                query = query.Where(s => s.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.Timestamp <= endDate.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s =>
                    s.EventName.Contains(search) ||
                    s.Message.Contains(search) ||
                    (s.RequestPath != null && s.RequestPath.Contains(search)));

            var total = await query.CountAsync();

            var logs = await query
                .OrderByDescending(s => s.Timestamp)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SystemLogDTO
                {
                    Id = s.Id,
                    LogType = s.LogType,
                    Category = s.Category,
                    EventName = s.EventName,
                    Message = s.Message,
                    EventCode = s.EventCode,
                    Severity = s.Severity,
                    Username = s.Username,
                    UserEmail = s.UserEmail,
                    UserRole = s.UserRole,
                    HttpMethod = s.HttpMethod,
                    RequestPath = s.RequestPath,
                    IpAddress = s.IpAddress,
                    StatusCode = s.StatusCode,
                    ExecutionTimeMs = s.ExecutionTimeMs,
                    Status = s.Status,
                    IsAcknowledged = s.IsAcknowledged,
                    AcknowledgedByName = s.AcknowledgedByUser != null ? s.AcknowledgedByUser.Name : null,
                    AcknowledgedAt = s.AcknowledgedAt,
                    Timestamp = s.Timestamp,
                    CreatedAt = s.CreatedAt
                })
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", total.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(logs);
        }

        // GET: api/SystemLog/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SystemLog>> GetSystemLog(int id)
        {
            var log = await _context.SystemLogs
                .Include(s => s.User)
                .Include(s => s.AcknowledgedByUser)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (log == null)
                return NotFound(new { message = "System log not found" });

            return Ok(log);
        }

        // POST: api/SystemLog
        [HttpPost]
        public async Task<ActionResult<SystemLogDTO>> CreateSystemLog([FromBody] CreateSystemLogDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int? userId = null;
            if (!string.IsNullOrEmpty(userIdClaim))
                userId = int.Parse(userIdClaim);

            User? user = null;
            if (userId.HasValue)
                user = await _context.Users.FindAsync(userId.Value);

            var log = new SystemLog
            {
                LogType = dto.LogType,
                Category = dto.Category,
                EventName = dto.EventName,
                Message = dto.Message,
                EventCode = dto.EventCode,
                Severity = dto.Severity,
                UserId = userId,
                Username = user?.Name,
                UserEmail = user?.Email,
                UserRole = user?.Role,
                HttpMethod = dto.HttpMethod,
                RequestPath = dto.RequestPath,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                StatusCode = dto.StatusCode,
                AdditionalDataJson = dto.AdditionalDataJson,
                Status = "unread",
                Timestamp = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _context.SystemLogs.Add(log);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSystemLog), new { id = log.Id }, log);
        }

        // PUT: api/SystemLog/5/acknowledge
        [HttpPut("{id}/acknowledge")]
        public async Task<IActionResult> AcknowledgeSystemLog(int id)
        {
            var log = await _context.SystemLogs.FindAsync(id);
            if (log == null)
                return NotFound(new { message = "System log not found" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdClaim!);

            log.IsAcknowledged = true;
            log.AcknowledgedBy = userId;
            log.AcknowledgedAt = DateTime.UtcNow;
            log.Status = "read";

            await _context.SaveChangesAsync();

            return Ok(new { message = "System log acknowledged successfully" });
        }

        // DELETE: api/SystemLog/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin")]
        public async Task<IActionResult> DeleteSystemLog(int id)
        {
            var log = await _context.SystemLogs.FindAsync(id);
            if (log == null)
                return NotFound(new { message = "System log not found" });

            _context.SystemLogs.Remove(log);
            await _context.SaveChangesAsync();

            return Ok(new { message = "System log deleted successfully" });
        }

        // GET: api/SystemLog/statistics
        [HttpGet("statistics")]
        public async Task<ActionResult<SystemLogStatsDTO>> GetStatistics(
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null)
        {
            var query = _context.SystemLogs.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.Timestamp >= startDate.Value);

            if (endDate.HasValue)
                query = query.Where(s => s.Timestamp <= endDate.Value);

            var stats = new SystemLogStatsDTO
            {
                TotalLogs = await query.CountAsync(),
                UnreadLogs = await query.CountAsync(s => s.Status == "unread"),
                CriticalLogs = await query.CountAsync(s => s.LogType == "critical"),
                ErrorLogs = await query.CountAsync(s => s.LogType == "error"),
                WarningLogs = await query.CountAsync(s => s.LogType == "warning"),
                ApiLogs = await query.CountAsync(s => s.Category == "api"),
                AuthenticationLogs = await query.CountAsync(s => s.Category == "authentication"),
                LogsByType = await query
                    .GroupBy(s => s.LogType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type, x => x.Count),
                LogsByCategory = await query
                    .GroupBy(s => s.Category)
                    .Select(g => new { Category = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Category, x => x.Count),
                LogsBySeverity = await query
                    .GroupBy(s => s.Severity)
                    .Select(g => new { Severity = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Severity, x => x.Count),
                LogsByStatusCode = await query
                    .Where(s => s.StatusCode.HasValue)
                    .GroupBy(s => s.StatusCode!.Value)
                    .Select(g => new { StatusCode = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.StatusCode, x => x.Count),
                AverageExecutionTimeMs = await query
                    .Where(s => s.ExecutionTimeMs.HasValue)
                    .AverageAsync(s => (double?)s.ExecutionTimeMs)
            };

            return Ok(stats);
        }
    }
}
```

---

## 5️⃣ **API Endpoints Summary**

### **Machine Logs**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/MachineLog` | All Roles | Get all machine logs (with filters) |
| GET | `/api/MachineLog/{id}` | All Roles | Get single machine log |
| POST | `/api/MachineLog` | SuperAdmin, Admin, Manager | Create new machine log |
| PUT | `/api/MachineLog/{id}/acknowledge` | SuperAdmin, Admin, Manager | Acknowledge machine log |
| PUT | `/api/MachineLog/{id}/status` | SuperAdmin, Admin, Manager | Update log status |
| DELETE | `/api/MachineLog/{id}` | SuperAdmin, Admin | Delete machine log |
| GET | `/api/MachineLog/statistics` | SuperAdmin, Admin, Manager | Get machine log statistics |

### **System Logs**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/SystemLog` | SuperAdmin, Admin | Get all system logs (with filters) |
| GET | `/api/SystemLog/{id}` | SuperAdmin, Admin | Get single system log |
| POST | `/api/SystemLog` | SuperAdmin, Admin | Create new system log |
| PUT | `/api/SystemLog/{id}/acknowledge` | SuperAdmin, Admin | Acknowledge system log |
| DELETE | `/api/SystemLog/{id}` | SuperAdmin | Delete system log |
| GET | `/api/SystemLog/statistics` | SuperAdmin, Admin | Get system log statistics |

---

## 6️⃣ **Migration & Testing**

### **Step 1: Add Migration**

```bash
dotnet ef migrations add AddLogsManagement
dotnet ef database update
```

### **Step 2: Test with Swagger**

1. **Get Machine Logs:**
```
GET /api/MachineLog?page=1&pageSize=20
GET /api/MachineLog?machineId=1
GET /api/MachineLog?severity=critical&status=unread
GET /api/MachineLog/statistics
```

2. **Get System Logs:**
```
GET /api/SystemLog?page=1&pageSize=20
GET /api/SystemLog?category=authentication
GET /api/SystemLog?severity=high&status=unread
GET /api/SystemLog/statistics?startDate=2025-10-01
```

3. **Create Machine Log:**
```json
POST /api/MachineLog
{
  "machineId": 1,
  "logType": "warning",
  "category": "security",
  "eventName": "Suspicious Activity",
  "message": "Unusual network traffic detected",
  "eventCode": "SEC-1001",
  "severity": "medium",
  "source": "Network Monitor"
}
```

4. **Acknowledge Log:**
```
PUT /api/MachineLog/1/acknowledge
PUT /api/SystemLog/1/acknowledge
```

---

## 🎯 **Features Implemented**

### **Machine Logs**
- ✅ System, Security, Application, Network, Performance logs
- ✅ Log type classification (info, warning, error, critical)
- ✅ Severity levels (low, medium, high, critical)
- ✅ Process and user tracking
- ✅ Status management (unread, read, resolved, ignored)
- ✅ Acknowledgement system
- ✅ Advanced filtering by date, severity, status
- ✅ Top machines with most logs
- ✅ Comprehensive statistics

### **System Logs**
- ✅ API, Authentication, Database, System logs
- ✅ HTTP request/response tracking
- ✅ Performance monitoring (execution time)
- ✅ User action tracking
- ✅ IP address and user agent logging
- ✅ Status code tracking
- ✅ Exception and stack trace capture
- ✅ Acknowledgement workflow
- ✅ Advanced analytics and statistics

---

**Perfect! Logs Management API complete! 🎉**
