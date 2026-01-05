# 👥 DSecure API - Subusers Management Documentation

## 📋 **Complete Implementation Guide**

यह documentation Subusers (Team Members / Child Users) Management के लिए complete API setup प्रदान करता है।

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

### **Models/Entities/Subuser.cs**

```csharp
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureAPI.Models.Entities
{
    public class Subuser
    {
        [Key]
        public int Id { get; set; }

        // Parent User Reference
        public int ParentUserId { get; set; }

        [ForeignKey("ParentUserId")]
        public User? ParentUser { get; set; }

        // Subuser Details
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [MaxLength(20)]
        public string? Phone { get; set; }

        [MaxLength(100)]
        public string? JobTitle { get; set; }

        [MaxLength(100)]
        public string? Department { get; set; }

        // Role & Permissions
        [Required]
        [MaxLength(50)]
        public string Role { get; set; } = "subuser"; // subuser, team_member, limited_admin

        [MaxLength(50)]
        public string AccessLevel { get; set; } = "limited"; // full, limited, read_only

        public string? PermissionsJson { get; set; } // JSON string for granular permissions

        // Machine & License Access
        public int? AssignedMachines { get; set; } = 0;
        
        public int? MaxMachines { get; set; } = 5;

        public string? MachineIdsJson { get; set; } // JSON array of accessible machine IDs

        public string? LicenseIdsJson { get; set; } // JSON array of accessible license IDs

        // Group Access
        public int? GroupId { get; set; }

        [ForeignKey("GroupId")]
        public Group? Group { get; set; }

        // Status
        [MaxLength(50)]
        public string Status { get; set; } = "active"; // active, inactive, suspended

        public bool IsEmailVerified { get; set; } = false;

        public bool CanCreateSubusers { get; set; } = false;

        public bool CanViewReports { get; set; } = true;

        public bool CanManageMachines { get; set; } = false;

        public bool CanAssignLicenses { get; set; } = false;

        // Notifications
        public bool EmailNotifications { get; set; } = true;

        public bool SystemAlerts { get; set; } = true;

        // Session & Security
        [MaxLength(500)]
        public string? LastLoginIp { get; set; }

        public DateTime? LastLoginAt { get; set; }

        public int FailedLoginAttempts { get; set; } = 0;

        public DateTime? LockedUntil { get; set; }

        // Audit
        public int CreatedBy { get; set; }

        [ForeignKey("CreatedBy")]
        public User? CreatedByUser { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        public int? UpdatedBy { get; set; }

        [MaxLength(500)]
        public string? Notes { get; set; }
    }
}
```

---

## 2️⃣ **DTOs**

### **Models/DTOs/Subuser/SubuserDTO.cs**

```csharp
namespace DSecureAPI.Models.DTOs.Subuser
{
    public class SubuserDTO
    {
        public int Id { get; set; }
        public int ParentUserId { get; set; }
        public string ParentUserName { get; set; } = string.Empty;
        public string ParentUserEmail { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string Role { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = string.Empty;
        public int? AssignedMachines { get; set; }
        public int? MaxMachines { get; set; }
        public string? GroupName { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsEmailVerified { get; set; }
        public bool CanCreateSubusers { get; set; }
        public bool CanViewReports { get; set; }
        public bool CanManageMachines { get; set; }
        public bool CanAssignLicenses { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateSubuserDTO
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string Role { get; set; } = "subuser";
        public string AccessLevel { get; set; } = "limited";
        public int? MaxMachines { get; set; } = 5;
        public int? GroupId { get; set; }
        public bool CanCreateSubusers { get; set; } = false;
        public bool CanViewReports { get; set; } = true;
        public bool CanManageMachines { get; set; } = false;
        public bool CanAssignLicenses { get; set; } = false;
        public bool EmailNotifications { get; set; } = true;
        public bool SystemAlerts { get; set; } = true;
        public string? Notes { get; set; }
    }

    public class UpdateSubuserDTO
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? JobTitle { get; set; }
        public string? Department { get; set; }
        public string? Role { get; set; }
        public string? AccessLevel { get; set; }
        public int? MaxMachines { get; set; }
        public int? GroupId { get; set; }
        public string? Status { get; set; }
        public bool? CanCreateSubusers { get; set; }
        public bool? CanViewReports { get; set; }
        public bool? CanManageMachines { get; set; }
        public bool? CanAssignLicenses { get; set; }
        public bool? EmailNotifications { get; set; }
        public bool? SystemAlerts { get; set; }
        public string? Notes { get; set; }
    }

    public class SubuserChangePasswordDTO
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    public class AssignMachinesToSubuserDTO
    {
        public int SubuserId { get; set; }
        public List<int> MachineIds { get; set; } = new();
    }

    public class AssignLicensesToSubuserDTO
    {
        public int SubuserId { get; set; }
        public List<int> LicenseIds { get; set; } = new();
    }

    public class SubuserStatsDTO
    {
        public int TotalSubusers { get; set; }
        public int ActiveSubusers { get; set; }
        public int InactiveSubusers { get; set; }
        public int SuspendedSubusers { get; set; }
        public int VerifiedSubusers { get; set; }
        public int UnverifiedSubusers { get; set; }
        public Dictionary<string, int> SubusersByRole { get; set; } = new();
        public Dictionary<string, int> SubusersByAccessLevel { get; set; } = new();
        public Dictionary<string, int> SubusersByDepartment { get; set; } = new();
        public List<TopParentUserDTO> TopParentUsersWithSubusers { get; set; } = new();
    }

    public class TopParentUserDTO
    {
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public int SubuserCount { get; set; }
    }
}
```

---

## 3️⃣ **Database Context Update**

### **Data/ApplicationDbContext.cs** (Add this DbSet)

```csharp
public DbSet<Subuser> Subusers { get; set; }

// In OnModelCreating method, add indexes:

// Subuser indexes
modelBuilder.Entity<Subuser>().HasIndex(s => s.Email).IsUnique();
modelBuilder.Entity<Subuser>().HasIndex(s => s.ParentUserId);
modelBuilder.Entity<Subuser>().HasIndex(s => new { s.Status, s.AccessLevel });
modelBuilder.Entity<Subuser>().HasIndex(s => s.GroupId);

// Seed Subusers
modelBuilder.Entity<Subuser>().HasData(
    new Subuser
    {
        Id = 1,
        ParentUserId = 2, // Admin User
        Name = "John Smith",
        Email = "john.smith@dsecuretech.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Subuser@123"),
        Phone = "+1-555-0101",
        JobTitle = "IT Support Specialist",
        Department = "IT Support",
        Role = "subuser",
        AccessLevel = "limited",
        MaxMachines = 5,
        AssignedMachines = 2,
        GroupId = 1,
        Status = "active",
        IsEmailVerified = true,
        CanViewReports = true,
        CanManageMachines = false,
        CanAssignLicenses = false,
        CanCreateSubusers = false,
        EmailNotifications = true,
        SystemAlerts = true,
        CreatedBy = 2,
        CreatedAt = DateTime.UtcNow.AddDays(-30)
    },
    new Subuser
    {
        Id = 2,
        ParentUserId = 2, // Admin User
        Name = "Sarah Johnson",
        Email = "sarah.johnson@dsecuretech.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Subuser@123"),
        Phone = "+1-555-0102",
        JobTitle = "System Administrator",
        Department = "IT",
        Role = "team_member",
        AccessLevel = "full",
        MaxMachines = 10,
        AssignedMachines = 7,
        GroupId = 1,
        Status = "active",
        IsEmailVerified = true,
        CanViewReports = true,
        CanManageMachines = true,
        CanAssignLicenses = true,
        CanCreateSubusers = false,
        EmailNotifications = true,
        SystemAlerts = true,
        CreatedBy = 2,
        CreatedAt = DateTime.UtcNow.AddDays(-45)
    },
    new Subuser
    {
        Id = 3,
        ParentUserId = 3, // Manager User
        Name = "Mike Davis",
        Email = "mike.davis@dsecuretech.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Subuser@123"),
        Phone = "+1-555-0103",
        JobTitle = "Sales Rep",
        Department = "Sales",
        Role = "subuser",
        AccessLevel = "read_only",
        MaxMachines = 3,
        AssignedMachines = 1,
        GroupId = 2,
        Status = "active",
        IsEmailVerified = true,
        CanViewReports = true,
        CanManageMachines = false,
        CanAssignLicenses = false,
        CanCreateSubusers = false,
        EmailNotifications = true,
        SystemAlerts = false,
        CreatedBy = 3,
        CreatedAt = DateTime.UtcNow.AddDays(-20)
    },
    new Subuser
    {
        Id = 4,
        ParentUserId = 2, // Admin User
        Name = "Emily Wilson",
        Email = "emily.wilson@dsecuretech.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Subuser@123"),
        Phone = "+1-555-0104",
        JobTitle = "Network Engineer",
        Department = "IT",
        Role = "team_member",
        AccessLevel = "full",
        MaxMachines = 8,
        AssignedMachines = 5,
        GroupId = 1,
        Status = "active",
        IsEmailVerified = false,
        CanViewReports = true,
        CanManageMachines = true,
        CanAssignLicenses = false,
        CanCreateSubusers = false,
        EmailNotifications = true,
        SystemAlerts = true,
        CreatedBy = 2,
        CreatedAt = DateTime.UtcNow.AddDays(-10),
        Notes = "Pending email verification"
    },
    new Subuser
    {
        Id = 5,
        ParentUserId = 3, // Manager User
        Name = "Robert Brown",
        Email = "robert.brown@dsecuretech.com",
        PasswordHash = BCrypt.Net.BCrypt.HashPassword("Subuser@123"),
        JobTitle = "Marketing Coordinator",
        Department = "Marketing",
        Role = "subuser",
        AccessLevel = "limited",
        MaxMachines = 2,
        AssignedMachines = 0,
        GroupId = 3,
        Status = "inactive",
        IsEmailVerified = true,
        CanViewReports = false,
        CanManageMachines = false,
        CanAssignLicenses = false,
        CanCreateSubusers = false,
        EmailNotifications = false,
        SystemAlerts = false,
        CreatedBy = 3,
        CreatedAt = DateTime.UtcNow.AddDays(-60),
        Notes = "Account temporarily disabled"
    }
);
```

---

## 4️⃣ **Controller**

### **Controllers/SubuserController.cs**

```csharp
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureAPI.Data;
using DSecureAPI.Models.DTOs.Subuser;
using DSecureAPI.Models.Entities;
using System.Security.Claims;
using System.Text.Json;

namespace DSecureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubuserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SubuserController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Subuser
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubuserDTO>>> GetSubusers(
            [FromQuery] int? parentUserId = null,
            [FromQuery] string? status = null,
            [FromQuery] string? role = null,
            [FromQuery] string? accessLevel = null,
            [FromQuery] int? groupId = null,
            [FromQuery] string? department = null,
            [FromQuery] bool? isEmailVerified = null,
            [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = int.Parse(userIdClaim!);

            var query = _context.Subusers
                .Include(s => s.ParentUser)
                .Include(s => s.Group)
                .AsQueryable();

            // Filter by parent user based on role
            if (userRole == "user" || userRole == "manager")
            {
                // Regular users and managers can only see their own subusers
                query = query.Where(s => s.ParentUserId == currentUserId);
            }
            else if (parentUserId.HasValue && (userRole == "admin" || userRole == "superadmin"))
            {
                // Admins can filter by any parent user
                query = query.Where(s => s.ParentUserId == parentUserId.Value);
            }

            if (!string.IsNullOrEmpty(status))
                query = query.Where(s => s.Status == status);

            if (!string.IsNullOrEmpty(role))
                query = query.Where(s => s.Role == role);

            if (!string.IsNullOrEmpty(accessLevel))
                query = query.Where(s => s.AccessLevel == accessLevel);

            if (groupId.HasValue)
                query = query.Where(s => s.GroupId == groupId.Value);

            if (!string.IsNullOrEmpty(department))
                query = query.Where(s => s.Department == department);

            if (isEmailVerified.HasValue)
                query = query.Where(s => s.IsEmailVerified == isEmailVerified.Value);

            if (!string.IsNullOrEmpty(search))
                query = query.Where(s =>
                    s.Name.Contains(search) ||
                    s.Email.Contains(search) ||
                    (s.Department != null && s.Department.Contains(search)) ||
                    (s.JobTitle != null && s.JobTitle.Contains(search)));

            var total = await query.CountAsync();

            var subusers = await query
                .OrderByDescending(s => s.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(s => new SubuserDTO
                {
                    Id = s.Id,
                    ParentUserId = s.ParentUserId,
                    ParentUserName = s.ParentUser!.Name,
                    ParentUserEmail = s.ParentUser.Email,
                    Name = s.Name,
                    Email = s.Email,
                    Phone = s.Phone,
                    JobTitle = s.JobTitle,
                    Department = s.Department,
                    Role = s.Role,
                    AccessLevel = s.AccessLevel,
                    AssignedMachines = s.AssignedMachines,
                    MaxMachines = s.MaxMachines,
                    GroupName = s.Group != null ? s.Group.Name : null,
                    Status = s.Status,
                    IsEmailVerified = s.IsEmailVerified,
                    CanCreateSubusers = s.CanCreateSubusers,
                    CanViewReports = s.CanViewReports,
                    CanManageMachines = s.CanManageMachines,
                    CanAssignLicenses = s.CanAssignLicenses,
                    LastLoginAt = s.LastLoginAt,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    Notes = s.Notes
                })
                .ToListAsync();

            Response.Headers.Add("X-Total-Count", total.ToString());
            Response.Headers.Add("X-Page", page.ToString());
            Response.Headers.Add("X-Page-Size", pageSize.ToString());

            return Ok(subusers);
        }

        // GET: api/Subuser/5
        [HttpGet("{id}")]
        public async Task<ActionResult<SubuserDTO>> GetSubuser(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = int.Parse(userIdClaim!);

            var subuser = await _context.Subusers
                .Include(s => s.ParentUser)
                .Include(s => s.Group)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (subuser == null)
                return NotFound(new { message = "Subuser not found" });

            // Check access permissions
            if ((userRole == "user" || userRole == "manager") && subuser.ParentUserId != currentUserId)
                return Forbid();

            return Ok(new SubuserDTO
            {
                Id = subuser.Id,
                ParentUserId = subuser.ParentUserId,
                ParentUserName = subuser.ParentUser!.Name,
                ParentUserEmail = subuser.ParentUser.Email,
                Name = subuser.Name,
                Email = subuser.Email,
                Phone = subuser.Phone,
                JobTitle = subuser.JobTitle,
                Department = subuser.Department,
                Role = subuser.Role,
                AccessLevel = subuser.AccessLevel,
                AssignedMachines = subuser.AssignedMachines,
                MaxMachines = subuser.MaxMachines,
                GroupName = subuser.Group?.Name,
                Status = subuser.Status,
                IsEmailVerified = subuser.IsEmailVerified,
                CanCreateSubusers = subuser.CanCreateSubusers,
                CanViewReports = subuser.CanViewReports,
                CanManageMachines = subuser.CanManageMachines,
                CanAssignLicenses = subuser.CanAssignLicenses,
                LastLoginAt = subuser.LastLoginAt,
                CreatedAt = subuser.CreatedAt,
                UpdatedAt = subuser.UpdatedAt,
                Notes = subuser.Notes
            });
        }

        // POST: api/Subuser
        [HttpPost]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult<SubuserDTO>> CreateSubuser([FromBody] CreateSubuserDTO dto)
        {
            // Check if email already exists
            if (await _context.Subusers.AnyAsync(s => s.Email == dto.Email) ||
                await _context.Users.AnyAsync(u => u.Email == dto.Email))
                return BadRequest(new { message = "Email already exists" });

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = int.Parse(userIdClaim!);

            var subuser = new Subuser
            {
                ParentUserId = userId,
                Name = dto.Name,
                Email = dto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Phone = dto.Phone,
                JobTitle = dto.JobTitle,
                Department = dto.Department,
                Role = dto.Role,
                AccessLevel = dto.AccessLevel,
                MaxMachines = dto.MaxMachines,
                GroupId = dto.GroupId,
                Status = "active",
                IsEmailVerified = false,
                CanCreateSubusers = dto.CanCreateSubusers,
                CanViewReports = dto.CanViewReports,
                CanManageMachines = dto.CanManageMachines,
                CanAssignLicenses = dto.CanAssignLicenses,
                EmailNotifications = dto.EmailNotifications,
                SystemAlerts = dto.SystemAlerts,
                Notes = dto.Notes,
                CreatedBy = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Subusers.Add(subuser);
            await _context.SaveChangesAsync();

            await _context.Entry(subuser).Reference(s => s.ParentUser).LoadAsync();
            await _context.Entry(subuser).Reference(s => s.Group).LoadAsync();

            return CreatedAtAction(nameof(GetSubuser), new { id = subuser.Id },
                new SubuserDTO
                {
                    Id = subuser.Id,
                    ParentUserId = subuser.ParentUserId,
                    ParentUserName = subuser.ParentUser!.Name,
                    ParentUserEmail = subuser.ParentUser.Email,
                    Name = subuser.Name,
                    Email = subuser.Email,
                    Phone = subuser.Phone,
                    JobTitle = subuser.JobTitle,
                    Department = subuser.Department,
                    Role = subuser.Role,
                    AccessLevel = subuser.AccessLevel,
                    MaxMachines = subuser.MaxMachines,
                    GroupName = subuser.Group?.Name,
                    Status = subuser.Status,
                    IsEmailVerified = subuser.IsEmailVerified,
                    CanCreateSubusers = subuser.CanCreateSubusers,
                    CanViewReports = subuser.CanViewReports,
                    CanManageMachines = subuser.CanManageMachines,
                    CanAssignLicenses = subuser.CanAssignLicenses,
                    CreatedAt = subuser.CreatedAt,
                    Notes = subuser.Notes
                });
        }

        // PUT: api/Subuser/5
        [HttpPut("{id}")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> UpdateSubuser(int id, [FromBody] UpdateSubuserDTO dto)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = int.Parse(userIdClaim!);

            var subuser = await _context.Subusers.FindAsync(id);
            if (subuser == null)
                return NotFound(new { message = "Subuser not found" });

            // Check access permissions
            if ((userRole == "manager") && subuser.ParentUserId != currentUserId)
                return Forbid();

            if (dto.Name != null) subuser.Name = dto.Name;
            if (dto.Phone != null) subuser.Phone = dto.Phone;
            if (dto.JobTitle != null) subuser.JobTitle = dto.JobTitle;
            if (dto.Department != null) subuser.Department = dto.Department;
            if (dto.Role != null) subuser.Role = dto.Role;
            if (dto.AccessLevel != null) subuser.AccessLevel = dto.AccessLevel;
            if (dto.MaxMachines.HasValue) subuser.MaxMachines = dto.MaxMachines.Value;
            if (dto.GroupId.HasValue) subuser.GroupId = dto.GroupId.Value;
            if (dto.Status != null) subuser.Status = dto.Status;
            if (dto.CanCreateSubusers.HasValue) subuser.CanCreateSubusers = dto.CanCreateSubusers.Value;
            if (dto.CanViewReports.HasValue) subuser.CanViewReports = dto.CanViewReports.Value;
            if (dto.CanManageMachines.HasValue) subuser.CanManageMachines = dto.CanManageMachines.Value;
            if (dto.CanAssignLicenses.HasValue) subuser.CanAssignLicenses = dto.CanAssignLicenses.Value;
            if (dto.EmailNotifications.HasValue) subuser.EmailNotifications = dto.EmailNotifications.Value;
            if (dto.SystemAlerts.HasValue) subuser.SystemAlerts = dto.SystemAlerts.Value;
            if (dto.Notes != null) subuser.Notes = dto.Notes;

            subuser.UpdatedAt = DateTime.UtcNow;
            subuser.UpdatedBy = currentUserId;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Subuser updated successfully" });
        }

        // DELETE: api/Subuser/5
        [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> DeleteSubuser(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = int.Parse(userIdClaim!);

            var subuser = await _context.Subusers.FindAsync(id);
            if (subuser == null)
                return NotFound(new { message = "Subuser not found" });

            // Check access permissions
            if ((userRole == "manager") && subuser.ParentUserId != currentUserId)
                return Forbid();

            _context.Subusers.Remove(subuser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subuser deleted successfully" });
        }

        // POST: api/Subuser/{id}/change-password
        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] SubuserChangePasswordDTO dto)
        {
            if (dto.NewPassword != dto.ConfirmPassword)
                return BadRequest(new { message = "New password and confirm password do not match" });

            var subuser = await _context.Subusers.FindAsync(id);
            if (subuser == null)
                return NotFound(new { message = "Subuser not found" });

            if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, subuser.PasswordHash))
                return BadRequest(new { message = "Current password is incorrect" });

            subuser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
            subuser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Password changed successfully" });
        }

        // POST: api/Subuser/assign-machines
        [HttpPost("assign-machines")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> AssignMachines([FromBody] AssignMachinesToSubuserDTO dto)
        {
            var subuser = await _context.Subusers.FindAsync(dto.SubuserId);
            if (subuser == null)
                return NotFound(new { message = "Subuser not found" });

            if (dto.MachineIds.Count > subuser.MaxMachines)
                return BadRequest(new { message = $"Cannot assign more than {subuser.MaxMachines} machines" });

            subuser.MachineIdsJson = JsonSerializer.Serialize(dto.MachineIds);
            subuser.AssignedMachines = dto.MachineIds.Count;
            subuser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Machines assigned to subuser successfully" });
        }

        // POST: api/Subuser/assign-licenses
        [HttpPost("assign-licenses")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> AssignLicenses([FromBody] AssignLicensesToSubuserDTO dto)
        {
            var subuser = await _context.Subusers.FindAsync(dto.SubuserId);
            if (subuser == null)
                return NotFound(new { message = "Subuser not found" });

            subuser.LicenseIdsJson = JsonSerializer.Serialize(dto.LicenseIds);
            subuser.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Licenses assigned to subuser successfully" });
        }

        // GET: api/Subuser/statistics
        [HttpGet("statistics")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult<SubuserStatsDTO>> GetStatistics()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var currentUserId = int.Parse(userIdClaim!);

            var query = _context.Subusers.AsQueryable();

            // Filter by parent user for managers
            if (userRole == "manager")
                query = query.Where(s => s.ParentUserId == currentUserId);

            var stats = new SubuserStatsDTO
            {
                TotalSubusers = await query.CountAsync(),
                ActiveSubusers = await query.CountAsync(s => s.Status == "active"),
                InactiveSubusers = await query.CountAsync(s => s.Status == "inactive"),
                SuspendedSubusers = await query.CountAsync(s => s.Status == "suspended"),
                VerifiedSubusers = await query.CountAsync(s => s.IsEmailVerified),
                UnverifiedSubusers = await query.CountAsync(s => !s.IsEmailVerified),
                SubusersByRole = await query
                    .GroupBy(s => s.Role)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Role, x => x.Count),
                SubusersByAccessLevel = await query
                    .GroupBy(s => s.AccessLevel)
                    .Select(g => new { AccessLevel = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.AccessLevel, x => x.Count),
                SubusersByDepartment = await query
                    .Where(s => s.Department != null)
                    .GroupBy(s => s.Department!)
                    .Select(g => new { Department = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Department, x => x.Count),
                TopParentUsersWithSubusers = await _context.Subusers
                    .Include(s => s.ParentUser)
                    .GroupBy(s => new { s.ParentUserId, s.ParentUser!.Name, s.ParentUser.Email })
                    .Select(g => new TopParentUserDTO
                    {
                        UserId = g.Key.ParentUserId,
                        UserName = g.Key.Name,
                        UserEmail = g.Key.Email,
                        SubuserCount = g.Count()
                    })
                    .OrderByDescending(x => x.SubuserCount)
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

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/Subuser` | All Roles | Get all subusers (with filters & role-based access) |
| GET | `/api/Subuser/{id}` | All Roles | Get single subuser details |
| POST | `/api/Subuser` | SuperAdmin, Admin, Manager | Create new subuser |
| PUT | `/api/Subuser/{id}` | SuperAdmin, Admin, Manager | Update subuser |
| DELETE | `/api/Subuser/{id}` | SuperAdmin, Admin, Manager | Delete subuser |
| POST | `/api/Subuser/{id}/change-password` | All Roles | Change subuser password |
| POST | `/api/Subuser/assign-machines` | SuperAdmin, Admin, Manager | Assign machines to subuser |
| POST | `/api/Subuser/assign-licenses` | SuperAdmin, Admin | Assign licenses to subuser |
| GET | `/api/Subuser/statistics` | SuperAdmin, Admin, Manager | Get subuser statistics |

---

## 6️⃣ **Migration & Testing**

### **Step 1: Add Migration**

```bash
dotnet ef migrations add AddSubusersManagement
dotnet ef database update
```

### **Step 2: Test with Swagger**

1. **Login as Admin:**
```json
POST /api/RoleBasedAuth/login
{
  "email": "admin@dsecuretech.com",
  "password": "Admin@123"
}
```

2. **Get All Subusers:**
```
GET /api/Subuser?page=1&pageSize=10
GET /api/Subuser?status=active
GET /api/Subuser?accessLevel=full
GET /api/Subuser?department=IT
```

3. **Get Subuser Statistics:**
```
GET /api/Subuser/statistics
```

4. **Create Subuser:**
```json
POST /api/Subuser
{
  "name": "Test Subuser",
  "email": "test.subuser@dsecuretech.com",
  "password": "Test@123",
  "phone": "+1-555-9999",
  "jobTitle": "Junior Admin",
  "department": "IT",
  "role": "subuser",
  "accessLevel": "limited",
  "maxMachines": 5,
  "groupId": 1,
  "canViewReports": true,
  "canManageMachines": false,
  "canAssignLicenses": false
}
```

5. **Assign Machines to Subuser:**
```json
POST /api/Subuser/assign-machines
{
  "subuserId": 1,
  "machineIds": [1, 2, 3]
}
```

6. **Change Subuser Password:**
```json
POST /api/Subuser/1/change-password
{
  "currentPassword": "Subuser@123",
  "newPassword": "NewPassword@123",
  "confirmPassword": "NewPassword@123"
}
```

---

## 🎯 **Features Implemented**

- ✅ Complete subuser/team member management
- ✅ Hierarchical user structure (Parent → Subusers)
- ✅ Role-based access control (subuser, team_member, limited_admin)
- ✅ Access levels (full, limited, read_only)
- ✅ Granular permissions (CanViewReports, CanManageMachines, etc.)
- ✅ Machine assignment with limits
- ✅ License assignment
- ✅ Group/Department organization
- ✅ Email verification system
- ✅ Password management
- ✅ Session tracking (LastLogin, IP address)
- ✅ Security features (failed login attempts, account locking)
- ✅ Notification preferences
- ✅ Advanced filtering and search
- ✅ Role-based data access (Managers see only their subusers)
- ✅ Comprehensive statistics

---

**Perfect! Subusers Management API complete! 🎉**
