# 🖥️ DSecure API - Machines Management Documentation

## 📋 **Complete Implementation Guide**

यह documentation Machines Management के लिए complete API setup प्रदान करता है।

---

## 📁 **Table of Contents**

1. [Entity Models](#entity-models)
2. [DTOs](#dtos)
3. [Database Context Update](#database-context-update)
4. [Controller](#controller)
5. [API Endpoints](#api-endpoints)
6. [Testing Guide](#testing-guide)

---

## 1️⃣ **Entity Models**

### **Models/Entities/Machine.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureAPI.Models.Entities
{
    public class Machine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string MachineName { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string MachineId { get; set; } = string.Empty; // Unique identifier

        [MaxLength(100)]
        public string? Hostname { get; set; }

        [MaxLength(50)]
        public string? IpAddress { get; set; }

        [MaxLength(50)]
        public string? MacAddress { get; set; }

        [Required]
        [MaxLength(50)]
        public string OperatingSystem { get; set; } = string.Empty; // Windows, Linux, macOS

        [MaxLength(50)]
        public string? OsVersion { get; set; }

        [MaxLength(100)]
        public string? Processor { get; set; }

        public int RamGB { get; set; } = 0;

        public int DiskGB { get; set; } = 0;

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, inactive, maintenance, decommissioned

        [MaxLength(50)]
        public string ConnectionStatus { get; set; } = "offline"; // online, offline

        public DateTime? LastSeen { get; set; }

        // License
        public int? LicenseId { get; set; }

        [ForeignKey("LicenseId")]
        public License? License { get; set; }

        [MaxLength(100)]
        public string? LicenseKey { get; set; }

        public DateTime? LicenseAssignedAt { get; set; }

        // User Assignment
        public int? AssignedToUserId { get; set; }

        [ForeignKey("AssignedToUserId")]
        public User? AssignedToUser { get; set; }

        // Group/Department
        public int? GroupId { get; set; }

        [ForeignKey("GroupId")]
        public Group? Group { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        [MaxLength(100)]
        public string? Location { get; set; }

        // Software Information
        public string? InstalledSoftwareJson { get; set; } // JSON array of installed software

        // Metadata
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int? CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public User? CreatedByUser { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }

        // Navigation
        public ICollection<MachineLog>? MachineLogs { get; set; }
    }
}
```

### **Models/Entities/License.cs** (If not exists)

```csharp
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
        public string LicenseKey { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string ProductName { get; set; } = string.Empty;

        [MaxLength(50)]
        public string LicenseType { get; set; } = "standard"; // standard, premium, enterprise

        public DateTime? ExpiryDate { get; set; }

        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, expired, revoked

        public int MaxMachines { get; set; } = 1;

        public int AssignedMachines { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Machine>? Machines { get; set; }
    }
}
```

### **Models/Entities/Group.cs** (If not exists)

```csharp
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

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Machine>? Machines { get; set; }
    }
}
```

---

## 2️⃣ **DTOs**

### **Models/DTOs/Machine/MachineDTO.cs**

```csharp
namespace DSecureAPI.Models.DTOs.Machine
{
    public class MachineDTO
    {
        public int Id { get; set; }
        public string MachineName { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string? Hostname { get; set; }
        public string? IpAddress { get; set; }
        public string? MacAddress { get; set; }
        public string OperatingSystem { get; set; } = string.Empty;
        public string? OsVersion { get; set; }
        public string? Processor { get; set; }
        public int RamGB { get; set; }
        public int DiskGB { get; set; }
        public string Status { get; set; } = string.Empty;
        public string ConnectionStatus { get; set; } = string.Empty;
        public DateTime? LastSeen { get; set; }
        public string? LicenseKey { get; set; }
        public DateTime? LicenseAssignedAt { get; set; }
        public string? AssignedToUserName { get; set; }
        public string? AssignedToUserEmail { get; set; }
        public string? GroupName { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateMachineDTO
    {
        public string MachineName { get; set; } = string.Empty;
        public string MachineId { get; set; } = string.Empty;
        public string? Hostname { get; set; }
        public string? IpAddress { get; set; }
        public string? MacAddress { get; set; }
        public string OperatingSystem { get; set; } = string.Empty;
        public string? OsVersion { get; set; }
        public string? Processor { get; set; }
        public int RamGB { get; set; }
        public int DiskGB { get; set; }
        public int? AssignedToUserId { get; set; }
        public int? GroupId { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateMachineDTO
    {
        public string? MachineName { get; set; }
        public string? Hostname { get; set; }
        public string? IpAddress { get; set; }
        public string? MacAddress { get; set; }
        public string? OsVersion { get; set; }
        public string? Processor { get; set; }
        public int? RamGB { get; set; }
        public int? DiskGB { get; set; }
        public string? Status { get; set; }
        public int? AssignedToUserId { get; set; }
        public int? GroupId { get; set; }
        public string? Department { get; set; }
        public string? Location { get; set; }
        public string? Notes { get; set; }
    }

    public class AssignLicenseToMachineDTO
    {
        public int MachineId { get; set; }
        public int LicenseId { get; set; }
    }

    public class MachineStatsDTO
    {
        public int TotalMachines { get; set; }
        public int OnlineMachines { get; set; }
        public int OfflineMachines { get; set; }
        public int ActiveMachines { get; set; }
        public int InactiveMachines { get; set; }
        public int LicensedMachines { get; set; }
        public int UnlicensedMachines { get; set; }
        public Dictionary<string, int> MachinesByOS { get; set; } = new();
        public Dictionary<string, int> MachinesByStatus { get; set; } = new();
        public Dictionary<string, int> MachinesByDepartment { get; set; } = new();
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
        public DbSet<Machine> Machines { get; set; }
        public DbSet<License> Licenses { get; set; }
        public DbSet<Group> Groups { get; set; }
        public DbSet<PerformanceReport> PerformanceReports { get; set; }
        public DbSet<AuditReport> AuditReports { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User indexes
            modelBuilder.Entity<User>().HasIndex(u => u.Email).IsUnique();

            // Machine indexes
            modelBuilder.Entity<Machine>().HasIndex(m => m.MachineId).IsUnique();
            modelBuilder.Entity<Machine>().HasIndex(m => new { m.Status, m.ConnectionStatus });
            modelBuilder.Entity<Machine>().HasIndex(m => m.AssignedToUserId);
            modelBuilder.Entity<Machine>().HasIndex(m => m.GroupId);

            // License indexes
            modelBuilder.Entity<License>().HasIndex(l => l.LicenseKey).IsUnique();
            modelBuilder.Entity<License>().HasIndex(l => l.Status);

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

            // Seed Groups
            modelBuilder.Entity<Group>().HasData(
                new Group { Id = 1, Name = "IT Department", Description = "IT Team Machines", CreatedAt = DateTime.UtcNow },
                new Group { Id = 2, Name = "Sales Department", Description = "Sales Team Machines", CreatedAt = DateTime.UtcNow },
                new Group { Id = 3, Name = "Marketing", Description = "Marketing Team Machines", CreatedAt = DateTime.UtcNow }
            );

            // Seed Licenses
            modelBuilder.Entity<License>().HasData(
                new License
                {
                    Id = 1,
                    LicenseKey = "DSEC-WIN-2024-A1B2C3",
                    ProductName = "DSecure Enterprise",
                    LicenseType = "enterprise",
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                    Status = "active",
                    MaxMachines = 10,
                    AssignedMachines = 3,
                    CreatedAt = DateTime.UtcNow
                },
                new License
                {
                    Id = 2,
                    LicenseKey = "DSEC-PRO-2024-X9Y8Z7",
                    ProductName = "DSecure Professional",
                    LicenseType = "premium",
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                    Status = "active",
                    MaxMachines = 5,
                    AssignedMachines = 2,
                    CreatedAt = DateTime.UtcNow
                },
                new License
                {
                    Id = 3,
                    LicenseKey = "DSEC-STD-2024-P5Q6R7",
                    ProductName = "DSecure Standard",
                    LicenseType = "standard",
                    ExpiryDate = DateTime.UtcNow.AddMonths(6),
                    Status = "active",
                    MaxMachines = 3,
                    AssignedMachines = 1,
                    CreatedAt = DateTime.UtcNow
                }
            );

            // Seed Machines
            modelBuilder.Entity<Machine>().HasData(
                new Machine
                {
                    Id = 1,
                    MachineName = "WIN-WS-001",
                    MachineId = "MACH-001-WIN-2024",
                    Hostname = "desktop-it-001",
                    IpAddress = "192.168.1.101",
                    MacAddress = "00:1B:63:84:45:E6",
                    OperatingSystem = "Windows",
                    OsVersion = "Windows 11 Pro",
                    Processor = "Intel Core i7-12700K",
                    RamGB = 32,
                    DiskGB = 512,
                    Status = "active",
                    ConnectionStatus = "online",
                    LastSeen = DateTime.UtcNow.AddMinutes(-5),
                    LicenseId = 1,
                    LicenseKey = "DSEC-WIN-2024-A1B2C3",
                    LicenseAssignedAt = DateTime.UtcNow.AddDays(-30),
                    AssignedToUserId = 2,
                    GroupId = 1,
                    Department = "IT",
                    Location = "Office Floor 3",
                    CreatedAt = DateTime.UtcNow.AddDays(-90),
                    CreatedBy = 1
                },
                new Machine
                {
                    Id = 2,
                    MachineName = "MAC-WS-002",
                    MachineId = "MACH-002-MAC-2024",
                    Hostname = "macbook-sales-001",
                    IpAddress = "192.168.1.102",
                    MacAddress = "A4:83:E7:2F:1A:BC",
                    OperatingSystem = "macOS",
                    OsVersion = "macOS Sonoma 14.5",
                    Processor = "Apple M2 Pro",
                    RamGB = 16,
                    DiskGB = 512,
                    Status = "active",
                    ConnectionStatus = "online",
                    LastSeen = DateTime.UtcNow.AddMinutes(-2),
                    LicenseId = 1,
                    LicenseKey = "DSEC-WIN-2024-A1B2C3",
                    LicenseAssignedAt = DateTime.UtcNow.AddDays(-45),
                    AssignedToUserId = 3,
                    GroupId = 2,
                    Department = "Sales",
                    Location = "Office Floor 2",
                    CreatedAt = DateTime.UtcNow.AddDays(-60),
                    CreatedBy = 1
                },
                new Machine
                {
                    Id = 3,
                    MachineName = "LINUX-SRV-001",
                    MachineId = "MACH-003-LNX-2024",
                    Hostname = "ubuntu-server-001",
                    IpAddress = "192.168.1.50",
                    MacAddress = "52:54:00:12:34:56",
                    OperatingSystem = "Linux",
                    OsVersion = "Ubuntu 22.04 LTS",
                    Processor = "AMD EPYC 7763",
                    RamGB = 64,
                    DiskGB = 1024,
                    Status = "active",
                    ConnectionStatus = "online",
                    LastSeen = DateTime.UtcNow.AddMinutes(-1),
                    LicenseId = 2,
                    LicenseKey = "DSEC-PRO-2024-X9Y8Z7",
                    LicenseAssignedAt = DateTime.UtcNow.AddDays(-20),
                    AssignedToUserId = 1,
                    GroupId = 1,
                    Department = "IT",
                    Location = "Data Center",
                    CreatedAt = DateTime.UtcNow.AddDays(-120),
                    CreatedBy = 1
                },
                new Machine
                {
                    Id = 4,
                    MachineName = "WIN-WS-004",
                    MachineId = "MACH-004-WIN-2024",
                    Hostname = "desktop-mkt-001",
                    IpAddress = "192.168.1.103",
                    MacAddress = "F8:BC:12:A4:D5:9E",
                    OperatingSystem = "Windows",
                    OsVersion = "Windows 10 Pro",
                    Processor = "Intel Core i5-11400",
                    RamGB = 16,
                    DiskGB = 256,
                    Status = "active",
                    ConnectionStatus = "offline",
                    LastSeen = DateTime.UtcNow.AddHours(-3),
                    LicenseId = 3,
                    LicenseKey = "DSEC-STD-2024-P5Q6R7",
                    LicenseAssignedAt = DateTime.UtcNow.AddDays(-15),
                    AssignedToUserId = 4,
                    GroupId = 3,
                    Department = "Marketing",
                    Location = "Office Floor 1",
                    CreatedAt = DateTime.UtcNow.AddDays(-45),
                    CreatedBy = 2
                },
                new Machine
                {
                    Id = 5,
                    MachineName = "WIN-WS-005",
                    MachineId = "MACH-005-WIN-2024",
                    Hostname = "laptop-sales-002",
                    IpAddress = "192.168.1.104",
                    MacAddress = "2C:F0:5D:8A:3B:1F",
                    OperatingSystem = "Windows",
                    OsVersion = "Windows 11 Home",
                    Processor = "Intel Core i7-1165G7",
                    RamGB = 16,
                    DiskGB = 512,
                    Status = "inactive",
                    ConnectionStatus = "offline",
                    LastSeen = DateTime.UtcNow.AddDays(-7),
                    AssignedToUserId = null,
                    GroupId = 2,
                    Department = "Sales",
                    Location = "Storage Room",
                    CreatedAt = DateTime.UtcNow.AddDays(-180),
                    CreatedBy = 1,
                    Notes = "Machine pending reassignment"
                }
            );
        }
    }
}
```

---

## 4️⃣ **Controller**

### **Controllers/MachineController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Machine;
using DSecureAPI.Models.Entities;
using System.Security.Claims;

namespace DSecureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MachineController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MachineController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Machine
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MachineDTO>>> GetMachines(
            [FromQuery] string? status = null,
            [FromQuery] string? connectionStatus = null,
            [FromQuery] string? operatingSystem = null,
            [FromQuery] int? groupId = null,
            [FromQuery] int? assignedToUserId = null,
            [FromQuery] string? department = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var query = _context.Machines
                .Include(m => m.AssignedToUser)
                .Include(m => m.Group)
                .Include(m => m.License)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
                query = query.Where(m => m.Status == status);

            if (!string.IsNullOrEmpty(connectionStatus))
                query = query.Where(m => m.ConnectionStatus == connectionStatus);

            if (!string.IsNullOrEmpty(operatingSystem))
                query = query.Where(m => m.OperatingSystem == operatingSystem);

            if (groupId.HasValue)
                query = query.Where(m => m.GroupId == groupId.Value);

            if (assignedToUserId.HasValue)
                query = query.Where(m => m.AssignedToUserId == assignedToUserId.Value);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(m => m.Department == department);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(m => 
                    m.MachineName.Contains(search) ||
                    m.MachineId.Contains(search) ||
                    (m.Hostname != null && m.Hostname.Contains(search)) ||
                    (m.IpAddress != null && m.IpAddress.Contains(search)));

            var total = await query.CountAsync();

            var machines = await query
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new MachineDTO
                {
                    Id = m.Id,
                    MachineName = m.MachineName,
                    MachineId = m.MachineId,
                    Hostname = m.Hostname,
                    IpAddress = m.IpAddress,
                    MacAddress = m.MacAddress,
                    OperatingSystem = m.OperatingSystem,
                    OsVersion = m.OsVersion,
                    Processor = m.Processor,
                    RamGB = m.RamGB,
                    DiskGB = m.DiskGB,
                    Status = m.Status,
                    ConnectionStatus = m.ConnectionStatus,
                    LastSeen = m.LastSeen,
                    LicenseKey = m.LicenseKey,
                    LicenseAssignedAt = m.LicenseAssignedAt,
                    AssignedToUserName = m.AssignedToUser != null ? m.AssignedToUser.Name : null,
                    AssignedToUserEmail = m.AssignedToUser != null ? m.AssignedToUser.Email : null,
                    GroupName = m.Group != null ? m.Group.Name : null,
                    Department = m.Department,
                    Location = m.Location,
                    CreatedAt = m.CreatedAt,
                    UpdatedAt = m.UpdatedAt,
                    Notes = m.Notes
                })
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", total.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(machines);
        }

        // GET: api/Machine/5
        [HttpGet("{id}")]
        public async Task<ActionResult<MachineDTO>> GetMachine(int id)
        {
            var machine = await _context.Machines
                .Include(m => m.AssignedToUser)
                .Include(m => m.Group)
                .Include(m => m.License)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (machine == null)
                return NotFound(new { message = "Machine not found" });

            return Ok(new MachineDTO
            {
                Id = machine.Id,
                MachineName = machine.MachineName,
                MachineId = machine.MachineId,
                Hostname = machine.Hostname,
                IpAddress = machine.IpAddress,
                MacAddress = machine.MacAddress,
                OperatingSystem = machine.OperatingSystem,
                OsVersion = machine.OsVersion,
                Processor = machine.Processor,
                RamGB = machine.RamGB,
                DiskGB = machine.DiskGB,
                Status = machine.Status,
                ConnectionStatus = machine.ConnectionStatus,
                LastSeen = machine.LastSeen,
                LicenseKey = machine.LicenseKey,
                LicenseAssignedAt = machine.LicenseAssignedAt,
                AssignedToUserName = machine.AssignedToUser?.Name,
                AssignedToUserEmail = machine.AssignedToUser?.Email,
                GroupName = machine.Group?.Name,
                Department = machine.Department,
                Location = machine.Location,
                CreatedAt = machine.CreatedAt,
                UpdatedAt = machine.UpdatedAt,
                Notes = machine.Notes
            });
        }

        // POST: api/Machine
        [HttpPost]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult<MachineDTO>> CreateMachine([FromBody] CreateMachineDTO dto)
        {
            // Check if MachineId already exists
            if (await _context.Machines.AnyAsync(m => m.MachineId == dto.MachineId))
                return BadRequest(new { message = "Machine ID already exists" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdClaim!);

            var machine = new Machine
            {
                MachineName = dto.MachineName,
                MachineId = dto.MachineId,
                Hostname = dto.Hostname,
                IpAddress = dto.IpAddress,
                MacAddress = dto.MacAddress,
                OperatingSystem = dto.OperatingSystem,
                OsVersion = dto.OsVersion,
                Processor = dto.Processor,
                RamGB = dto.RamGB,
                DiskGB = dto.DiskGB,
                Status = "active",
                ConnectionStatus = "offline",
                AssignedToUserId = dto.AssignedToUserId,
                GroupId = dto.GroupId,
                Department = dto.Department,
                Location = dto.Location,
                Notes = dto.Notes,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = userId
            };

            _context.Machines.Add(machine);
            await _context.SaveChangesAsync();

            await _context.Entry(machine).Reference(m => m.AssignedToUser).LoadAsync();
            await _context.Entry(machine).Reference(m => m.Group).LoadAsync();

            return CreatedAtAction(nameof(GetMachine), new { id = machine.Id },
                new MachineDTO
                {
                    Id = machine.Id,
                    MachineName = machine.MachineName,
                    MachineId = machine.MachineId,
                    Hostname = machine.Hostname,
                    IpAddress = machine.IpAddress,
                    MacAddress = machine.MacAddress,
                    OperatingSystem = machine.OperatingSystem,
                    OsVersion = machine.OsVersion,
                    Processor = machine.Processor,
                    RamGB = machine.RamGB,
                    DiskGB = machine.DiskGB,
                    Status = machine.Status,
                    ConnectionStatus = machine.ConnectionStatus,
                    AssignedToUserName = machine.AssignedToUser?.Name,
                    GroupName = machine.Group?.Name,
                    Department = machine.Department,
                    Location = machine.Location,
                    CreatedAt = machine.CreatedAt,
                    Notes = machine.Notes
                });
        }

        // PUT: api/Machine/5
        [HttpPut("{id}")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> UpdateMachine(int id, [FromBody] UpdateMachineDTO dto)
        {
            var machine = await _context.Machines.FindAsync(id);
            if (machine == null)
                return NotFound(new { message = "Machine not found" });

            if (dto.MachineName != null) machine.MachineName = dto.MachineName;
            if (dto.Hostname != null) machine.Hostname = dto.Hostname;
            if (dto.IpAddress != null) machine.IpAddress = dto.IpAddress;
            if (dto.MacAddress != null) machine.MacAddress = dto.MacAddress;
            if (dto.OsVersion != null) machine.OsVersion = dto.OsVersion;
            if (dto.Processor != null) machine.Processor = dto.Processor;
            if (dto.RamGB.HasValue) machine.RamGB = dto.RamGB.Value;
            if (dto.DiskGB.HasValue) machine.DiskGB = dto.DiskGB.Value;
            if (dto.Status != null) machine.Status = dto.Status;
            if (dto.AssignedToUserId.HasValue) machine.AssignedToUserId = dto.AssignedToUserId.Value;
            if (dto.GroupId.HasValue) machine.GroupId = dto.GroupId.Value;
            if (dto.Department != null) machine.Department = dto.Department;
            if (dto.Location != null) machine.Location = dto.Location;
            if (dto.Notes != null) machine.Notes = dto.Notes;

            machine.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Machine updated successfully" });
        }

        // DELETE: api/Machine/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> DeleteMachine(int id)
        {
            var machine = await _context.Machines.FindAsync(id);
            if (machine == null)
                return NotFound(new { message = "Machine not found" });

            _context.Machines.Remove(machine);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Machine deleted successfully" });
        }

        // POST: api/Machine/assign-license
        [HttpPost("assign-license")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> AssignLicense([FromBody] AssignLicenseToMachineDTO dto)
        {
            var machine = await _context.Machines.FindAsync(dto.MachineId);
            if (machine == null)
                return NotFound(new { message = "Machine not found" });

            var license = await _context.Licenses.FindAsync(dto.LicenseId);
            if (license == null)
                return NotFound(new { message = "License not found" });

            if (license.Status != "active")
                return BadRequest(new { message = "License is not active" });

            if (license.AssignedMachines >= license.MaxMachines)
                return BadRequest(new { message = "License has reached maximum machine limit" });

            // Remove old license assignment
            if (machine.LicenseId.HasValue)
            {
                var oldLicense = await _context.Licenses.FindAsync(machine.LicenseId.Value);
                if (oldLicense != null)
                    oldLicense.AssignedMachines--;
            }

            machine.LicenseId = dto.LicenseId;
            machine.LicenseKey = license.LicenseKey;
            machine.LicenseAssignedAt = DateTime.UtcNow;
            machine.UpdatedAt = DateTime.UtcNow;

            license.AssignedMachines++;

            await _context.SaveChangesAsync();

            return Ok(new { message = "License assigned to machine successfully" });
        }

        // POST: api/Machine/{id}/update-connection-status
        [HttpPost("{id}/update-connection-status")]
        public async Task<IActionResult> UpdateConnectionStatus(int id, [FromBody] string connectionStatus)
        {
            var machine = await _context.Machines.FindAsync(id);
            if (machine == null)
                return NotFound(new { message = "Machine not found" });

            machine.ConnectionStatus = connectionStatus;
            machine.LastSeen = DateTime.UtcNow;
            machine.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Connection status updated successfully" });
        }

        // GET: api/Machine/statistics
        [HttpGet("statistics")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult<MachineStatsDTO>> GetStatistics()
        {
            var stats = new MachineStatsDTO
            {
                TotalMachines = await _context.Machines.CountAsync(),
                OnlineMachines = await _context.Machines.CountAsync(m => m.ConnectionStatus == "online"),
                OfflineMachines = await _context.Machines.CountAsync(m => m.ConnectionStatus == "offline"),
                ActiveMachines = await _context.Machines.CountAsync(m => m.Status == "active"),
                InactiveMachines = await _context.Machines.CountAsync(m => m.Status == "inactive"),
                LicensedMachines = await _context.Machines.CountAsync(m => m.LicenseId != null),
                UnlicensedMachines = await _context.Machines.CountAsync(m => m.LicenseId == null),
                MachinesByOS = await _context.Machines
                    .GroupBy(m => m.OperatingSystem)
                    .Select(g => new { OS = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.OS, x => x.Count),
                MachinesByStatus = await _context.Machines
                    .GroupBy(m => m.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count),
                MachinesByDepartment = await _context.Machines
                    .Where(m => m.Department != null)
                    .GroupBy(m => m.Department!)
                    .Select(g => new { Department = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x, x => x.Count)
            };

            return Ok(stats);
        }
    }
}
```

---

## 5️⃣ **API Endpoints Summary**

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Machine` | All Roles | Get all machines (with filters) |
| GET | `/api/Machine/{id}` | All Roles | Get single machine details |
| POST | `/api/Machine` | SuperAdmin, Admin, Manager | Create new machine |
| PUT | `/api/Machine/{id}` | SuperAdmin, Admin, Manager | Update machine |
| DELETE | `/api/Machine/{id}` | SuperAdmin, Admin | Delete machine |
| POST | `/api/Machine/assign-license` | SuperAdmin, Admin | Assign license to machine |
| POST | `/api/Machine/{id}/update-connection-status` | All Roles | Update connection status |
| GET | `/api/Machine/statistics` | SuperAdmin, Admin, Manager | Get machine statistics |

---

## 6️⃣ **Migration & Testing**

### **Step 1: Add Migration**

```bash
dotnet ef migrations add AddMachinesManagement
dotnet ef database update
```

### **Step 2: Test with Swagger**

1. **Login:**
```json
POST /api/RoleBasedAuth/login
{
  "email": "admin@dsecuretech.com",
  "password": "Admin@123"
}
```

2. **Get All Machines:**
```
GET /api/Machine?page=1&pageSize=10
GET /api/Machine?status=active
GET /api/Machine?connectionStatus=online
GET /api/Machine?operatingSystem=Windows
```

3. **Get Machine Statistics:**
```
GET /api/Machine/statistics
```

4. **Create Machine:**
```json
POST /api/Machine
{
  "machineName": "WIN-WS-NEW",
  "machineId": "MACH-NEW-001",
  "hostname": "new-desktop-001",
  "ipAddress": "192.168.1.105",
  "macAddress": "AA:BB:CC:DD:EE:FF",
  "operatingSystem": "Windows",
  "osVersion": "Windows 11 Pro",
  "processor": "Intel Core i5",
  "ramGB": 16,
  "diskGB": 512,
  "assignedToUserId": 4,
  "groupId": 1,
  "department": "IT",
  "location": "Office Floor 2"
}
```

5. **Assign License:**
```json
POST /api/Machine/assign-license
{
  "machineId": 1,
  "licenseId": 1
}
```

6. **Update Connection Status:**
```json
POST /api/Machine/1/update-connection-status
"online"
```

---

## 🎯 **Features Implemented**

- ✅ Complete machine inventory management
- ✅ Multi-OS support (Windows, Linux, macOS)
- ✅ Hardware specs tracking (CPU, RAM, Disk)
- ✅ Network information (IP, MAC address)
- ✅ License assignment and tracking
- ✅ User assignment
- ✅ Group/Department organization
- ✅ Real-time connection status
- ✅ Last seen tracking
- ✅ Advanced filtering and search
- ✅ Machine statistics dashboard
- ✅ Role-based access control

---

**Perfect! Machine Management API complete! 🎉**
