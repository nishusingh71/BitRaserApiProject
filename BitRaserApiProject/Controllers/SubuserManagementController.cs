using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;
using System.Security.Claims;
using System.Text.Json;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Complete Subusers (Team Members / Child Users) Management API
    /// Based on DOTNET-SUBUSERS-API.md documentation
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubuserManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SubuserManagementController> _logger;

        public SubuserManagementController(ApplicationDbContext context, ILogger<SubuserManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/SubuserManagement
        /// Get all subusers with filtering and pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SubuserDetailedDto>>> GetSubusers(
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
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
                
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                // Get current user
                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
                if (currentUser == null)
                    return NotFound(new { message = "User not found" });

                var query = _context.subuser.AsQueryable();

                // Role-based filtering
                if (userRoleClaim == "user" || userRoleClaim == "manager")
                {
                    // Regular users and managers can only see their own subusers
                    query = query.Where(s => s.user_email == userEmail);
                }
                else if (parentUserId.HasValue && (userRoleClaim == "admin" || userRoleClaim == "superadmin"))
                {
                    // Admins can filter by any parent user
                    query = query.Where(s => s.superuser_id == parentUserId.Value);
                }

                // Apply filters
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
                        (s.Name != null && s.Name.Contains(search)) ||
                        s.subuser_email.Contains(search) ||
                        (s.Department != null && s.Department.Contains(search)) ||
                        (s.JobTitle != null && s.JobTitle.Contains(search)));

                var total = await query.CountAsync();

                var subusers = await query
                    .OrderByDescending(s => s.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // Get parent user details and group names
                var result = new List<SubuserDetailedDto>();
                foreach (var subuser in subusers)
                {
                    var parentUser = await _context.Users.FindAsync(subuser.superuser_id);
                    var group = subuser.GroupId.HasValue ? await _context.Groups.FindAsync(subuser.GroupId.Value) : null;

                    result.Add(new SubuserDetailedDto
                    {
                        Id = subuser.subuser_id,
                        ParentUserId = subuser.superuser_id,
                        ParentUserName = parentUser?.user_name ?? "Unknown",
                        ParentUserEmail = parentUser?.user_email ?? "Unknown",
                        SubuserUsername = subuser.subuser_username,
                        Name = subuser.Name ?? subuser.subuser_email,
                        Email = subuser.subuser_email,
                        Phone = subuser.Phone,
                        JobTitle = subuser.JobTitle,
                        Department = subuser.Department,
                        Role = subuser.Role,
                        AccessLevel = subuser.AccessLevel,
                        AssignedMachines = subuser.AssignedMachines,
                        MaxMachines = subuser.MaxMachines,
                        GroupId = subuser.GroupId,
                        GroupName = group?.name,
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

                Response.Headers.Add("X-Total-Count", total.ToString());
                Response.Headers.Add("X-Page", page.ToString());
                Response.Headers.Add("X-Page-Size", pageSize.ToString());

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subusers");
                return StatusCode(500, new { message = "Error retrieving subusers" });
            }
        }

        /// <summary>
        /// GET: api/SubuserManagement/{id}
        /// Get single subuser details
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SubuserDetailedDto>> GetSubuser(int id)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
                if (currentUser == null)
                    return NotFound(new { message = "User not found" });

                var subuser = await _context.subuser.FindAsync(id);
                if (subuser == null)
                    return NotFound(new { message = "Subuser not found" });

                // Check access permissions
                if ((userRoleClaim == "user" || userRoleClaim == "manager") && subuser.user_email != userEmail)
                    return Forbid();

                var parentUser = await _context.Users.FindAsync(subuser.superuser_id);
                var group = subuser.GroupId.HasValue ? await _context.Groups.FindAsync(subuser.GroupId.Value) : null;

                return Ok(new SubuserDetailedDto
                {
                    Id = subuser.subuser_id,
                    ParentUserId = subuser.superuser_id,
                    ParentUserName = parentUser?.user_name ?? "Unknown",
                    ParentUserEmail = parentUser?.user_email ?? "Unknown",
                    SubuserUsername = subuser.subuser_username,
                    Name = subuser.Name ?? subuser.subuser_email,
                    Email = subuser.subuser_email,
                    Phone = subuser.Phone,
                    JobTitle = subuser.JobTitle,
                    Department = subuser.Department,
                    Role = subuser.Role,
                    AccessLevel = subuser.AccessLevel,
                    AssignedMachines = subuser.AssignedMachines,
                    MaxMachines = subuser.MaxMachines,
                    GroupId = subuser.GroupId,
                    GroupName = group?.name,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subuser {SubuserId}", id);
                return StatusCode(500, new { message = "Error retrieving subuser" });
            }
        }

        /// <summary>
        /// POST: api/SubuserManagement
        /// Create new subuser (Only Email and Password are required, rest are optional)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult<SubuserDetailedDto>> CreateSubuser([FromBody] CreateSubuserDto dto)
        {
            try
            {
                // Check if email already exists
                if (await _context.subuser.AnyAsync(s => s.subuser_email == dto.Email) ||
                    await _context.Users.AnyAsync(u => u.user_email == dto.Email))
                    return BadRequest(new { message = "Email already exists" });

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
                if (currentUser == null)
                    return NotFound(new { message = "User not found" });

                var subuser = new subuser
                {
                    superuser_id = currentUser.user_id,
                    user_email = userEmail,
                    subuser_username = dto.SubuserUsername, // Optional
                    Name = dto.Name ?? dto.Email.Split('@')[0], // Default to email prefix if not provided
                    subuser_email = dto.Email,
                    subuser_password = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                    Phone = dto.Phone, // Optional
                    JobTitle = dto.JobTitle, // Optional
                    Department = dto.Department, // Optional
                    Role = dto.Role ?? "subuser", // Default to "subuser"
                    AccessLevel = dto.AccessLevel ?? "limited", // Default to "limited"
                    MaxMachines = dto.MaxMachines ?? 5, // Default to 5
                    GroupId = dto.GroupId, // Optional
                    Status = "active",
                    IsEmailVerified = false,
                    CanCreateSubusers = dto.CanCreateSubusers ?? false, // Default to false
                    CanViewReports = dto.CanViewReports ?? true, // Default to true
                    CanManageMachines = dto.CanManageMachines ?? false, // Default to false
                    CanAssignLicenses = dto.CanAssignLicenses ?? false, // Default to false
                    EmailNotifications = dto.EmailNotifications ?? true, // Default to true
                    SystemAlerts = dto.SystemAlerts ?? true, // Default to true
                    Notes = dto.Notes, // Optional
                    CreatedBy = currentUser.user_id,
                    CreatedAt = DateTime.UtcNow
                };

                _context.subuser.Add(subuser);
                await _context.SaveChangesAsync();

                var group = subuser.GroupId.HasValue ? await _context.Groups.FindAsync(subuser.GroupId.Value) : null;

                return CreatedAtAction(nameof(GetSubuser), new { id = subuser.subuser_id },
                    new SubuserDetailedDto
                    {
                        Id = subuser.subuser_id,
                        ParentUserId = subuser.superuser_id,
                        ParentUserName = currentUser.user_name,
                        ParentUserEmail = currentUser.user_email,
                        SubuserUsername = subuser.subuser_username,
                        Name = subuser.Name ?? subuser.subuser_email,
                        Email = subuser.subuser_email,
                        Phone = subuser.Phone,
                        JobTitle = subuser.JobTitle,
                        Department = subuser.Department,
                        Role = subuser.Role,
                        AccessLevel = subuser.AccessLevel,
                        MaxMachines = subuser.MaxMachines,
                        GroupId = subuser.GroupId,
                        GroupName = group?.name,
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subuser");
                return StatusCode(500, new { message = "Error creating subuser" });
            }
        }

        /// <summary>
        /// PUT: api/SubuserManagement/{id}
        /// Update subuser
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> UpdateSubuser(int id, [FromBody] UpdateSubuserDto dto)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
                if (currentUser == null)
                    return NotFound(new { message = "User not found" });

                var subuser = await _context.subuser.FindAsync(id);
                if (subuser == null)
                    return NotFound(new { message = "Subuser not found" });

                // Check access permissions
                if ((userRole == "manager") && subuser.user_email != userEmail)
                    return Forbid();

                // Update properties
                if (dto.SubuserUsername != null) subuser.subuser_username = dto.SubuserUsername;
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
                subuser.UpdatedBy = currentUser.user_id;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Subuser updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating subuser {SubuserId}", id);
                return StatusCode(500, new { message = "Error updating subuser" });
            }
        }

        /// <summary>
        /// PATCH: api/SubuserManagement/{id}
        /// Partially update subuser (supports partial updates)
        /// </summary>
        [HttpPatch("{id}")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> PatchSubuser(int id, [FromBody] UpdateSubuserDto dto)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
                if (currentUser == null)
                    return NotFound(new { message = "User not found" });

                var subuser = await _context.subuser.FindAsync(id);
                if (subuser == null)
                    return NotFound(new { message = "Subuser not found" });

                // Check access permissions
                if ((userRole == "manager") && subuser.user_email != userEmail)
                    return Forbid();

                // Update only provided properties (partial update)
                if (dto.SubuserUsername != null) subuser.subuser_username = dto.SubuserUsername;
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
                subuser.UpdatedBy = currentUser.user_id;

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Subuser partially updated successfully",
                    subuserId = subuser.subuser_id,
                    updatedAt = subuser.UpdatedAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error patching subuser {SubuserId}", id);
                return StatusCode(500, new { message = "Error updating subuser" });
            }
        }

        /// <summary>
        /// DELETE: api/SubuserManagement/{id}
        /// Delete subuser
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> DeleteSubuser(int id)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                var subuser = await _context.subuser.FindAsync(id);
                if (subuser == null)
                    return NotFound(new { message = "Subuser not found" });

                // Check access permissions
                if ((userRole == "manager") && subuser.user_email != userEmail)
                    return Forbid();

                _context.subuser.Remove(subuser);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Subuser deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting subuser {SubuserId}", id);
                return StatusCode(500, new { message = "Error deleting subuser" });
            }
        }

        /// <summary>
        /// POST: api/SubuserManagement/{id}/change-password
        /// Change subuser password
        /// </summary>
        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] SubuserChangePasswordDto dto)
        {
            try
            {
                if (dto.NewPassword != dto.ConfirmPassword)
                    return BadRequest(new { message = "New password and confirm password do not match" });

                var subuser = await _context.subuser.FindAsync(id);
                if (subuser == null)
                    return NotFound(new { message = "Subuser not found" });

                if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, subuser.subuser_password))
                    return BadRequest(new { message = "Current password is incorrect" });

                subuser.subuser_password = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
                subuser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for subuser {SubuserId}", id);
                return StatusCode(500, new { message = "Error changing password" });
            }
        }

        /// <summary>
        /// POST: api/SubuserManagement/assign-machines
        /// Assign machines to subuser
        /// </summary>
        [HttpPost("assign-machines")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> AssignMachines([FromBody] AssignMachinesToSubuserDto dto)
        {
            try
            {
                var subuser = await _context.subuser.FindAsync(dto.SubuserId);
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning machines to subuser");
                return StatusCode(500, new { message = "Error assigning machines" });
            }
        }

        /// <summary>
        /// POST: api/SubuserManagement/assign-licenses
        /// Assign licenses to subuser
        /// </summary>
        [HttpPost("assign-licenses")]
        [Authorize(Roles = "superadmin,admin")]
        public async Task<IActionResult> AssignLicenses([FromBody] AssignLicensesToSubuserDto dto)
        {
            try
            {
                var subuser = await _context.subuser.FindAsync(dto.SubuserId);
                if (subuser == null)
                    return NotFound(new { message = "Subuser not found" });

                subuser.LicenseIdsJson = JsonSerializer.Serialize(dto.LicenseIds);
                subuser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Licenses assigned to subuser successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning licenses to subuser");
                return StatusCode(500, new { message = "Error assigning licenses" });
            }
        }

        /// <summary>
        /// GET: api/SubuserManagement/statistics
        /// Get subuser statistics
        /// </summary>
        [HttpGet("statistics")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<ActionResult<SubuserStatsDto>> GetStatistics()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                var query = _context.subuser.AsQueryable();

                // Filter by parent user for managers
                if (userRole == "manager")
                    query = query.Where(s => s.user_email == userEmail);

                var stats = new SubuserStatsDto
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
                    TopParentUsersWithSubusers = await (from s in _context.subuser
                                                        join u in _context.Users on s.superuser_id equals u.user_id
                                                        group s by new { u.user_id, u.user_name, u.user_email } into g
                                                        select new TopParentUserDto
                                                        {
                                                            UserId = g.Key.user_id,
                                                            UserName = g.Key.user_name,
                                                            UserEmail = g.Key.user_email,
                                                            SubuserCount = g.Count()
                                                        })
                                                        .OrderByDescending(x => x.SubuserCount)
                                                        .Take(10)
                                                        .ToListAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching subuser statistics");
                return StatusCode(500, new { message = "Error retrieving statistics" });
            }
        }

        /// <summary>
 /// GET: api/SubuserManagement/by-email/{email}
        /// Get subuser by email
        /// </summary>
      [HttpGet("by-email/{email}")]
        public async Task<ActionResult<SubuserDetailedDto>> GetSubuserByEmail(string email)
    {
            try
 {
           var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
           var userRoleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

           if (string.IsNullOrEmpty(userEmail))
          return Unauthorized(new { message = "User not authenticated" });

           var subuser = await _context.subuser
            .FirstOrDefaultAsync(s => s.subuser_email == email);

    if (subuser == null)
        return NotFound(new { message = "Subuser not found" });

     // Check access permissions
   if ((userRoleClaim == "user" || userRoleClaim == "manager") && subuser.user_email != userEmail)
              return Forbid();

        var parentUser = await _context.Users.FindAsync(subuser.superuser_id);
   var group = subuser.GroupId.HasValue ? await _context.Groups.FindAsync(subuser.GroupId.Value) : null;

         return Ok(new SubuserDetailedDto
                {
        Id = subuser.subuser_id,
      ParentUserId = subuser.superuser_id,
               ParentUserName = parentUser?.user_name ?? "Unknown",
           ParentUserEmail = parentUser?.user_email ?? "Unknown",
     SubuserUsername = subuser.subuser_username,
   Name = subuser.Name ?? subuser.subuser_email,
         Email = subuser.subuser_email,
       Phone = subuser.Phone,
      JobTitle = subuser.JobTitle,
    Department = subuser.Department,
    Role = subuser.Role,
      AccessLevel = subuser.AccessLevel,
            AssignedMachines = subuser.AssignedMachines,
                MaxMachines = subuser.MaxMachines,
           GroupId = subuser.GroupId,
          GroupName = group?.name,
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
  catch (Exception ex)
 {
        _logger.LogError(ex, "Error fetching subuser by email {Email}", email);
                return StatusCode(500, new { message = "Error retrieving subuser" });
       }
     }

 /// <summary>
    /// PUT: api/SubuserManagement/by-email/{email}
    /// Update subuser by email
        /// </summary>
        [HttpPut("by-email/{email}")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> UpdateSubuserByEmail(string email, [FromBody] UpdateSubuserDto dto)
        {
         try
            {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

           if (string.IsNullOrEmpty(userEmail))
             return Unauthorized(new { message = "User not authenticated" });

     var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
     if (currentUser == null)
   return NotFound(new { message = "User not found" });

                var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
  if (subuser == null)
       return NotFound(new { message = "Subuser not found" });

       // Check access permissions
      if ((userRole == "manager") && subuser.user_email != userEmail)
  return Forbid();

        // Update properties
       if (dto.SubuserUsername != null) subuser.subuser_username = dto.SubuserUsername;
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
        subuser.UpdatedBy = currentUser.user_id;

       await _context.SaveChangesAsync();

  return Ok(new { message = "Subuser updated successfully", email = subuser.subuser_email });
   }
    catch (Exception ex)
            {
    _logger.LogError(ex, "Error updating subuser by email {Email}", email);
          return StatusCode(500, new { message = "Error updating subuser" });
        }
        }

        /// <summary>
        /// PATCH: api/SubuserManagement/by-email/{email}
        /// Partially update subuser by email (supports partial updates)
        /// </summary>
        [HttpPatch("by-email/{email}")]
        [Authorize(Roles = "superadmin,admin,manager")]
        public async Task<IActionResult> PatchSubuserByEmail(string email, [FromBody] UpdateSubuserDto dto)
     {
       try
         {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

 if (string.IsNullOrEmpty(userEmail))
         return Unauthorized(new { message = "User not authenticated" });

          var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
         if (currentUser == null)
   return NotFound(new { message = "User not found" });

      var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
     if (subuser == null)
  return NotFound(new { message = "Subuser not found" });

       // Check access permissions
   if ((userRole == "manager") && subuser.user_email != userEmail)
      return Forbid();

             // Update only provided properties (partial update)
        if (dto.SubuserUsername != null) subuser.subuser_username = dto.SubuserUsername;
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
          subuser.UpdatedBy = currentUser.user_id;

        await _context.SaveChangesAsync();

                return Ok(new { 
   message = "Subuser partially updated successfully",
             email = subuser.subuser_email,
     updatedAt = subuser.UpdatedAt
    });
        }
            catch (Exception ex)
       {
       _logger.LogError(ex, "Error patching subuser by email {Email}", email);
     return StatusCode(500, new { message = "Error updating subuser" });
            }
        }

        /// <summary>
        /// DELETE: api/SubuserManagement/by-email/{email}
      /// Delete subuser by email
        /// </summary>
        [HttpDelete("by-email/{email}")]
        [Authorize(Roles = "superadmin,admin,manager")]
     public async Task<IActionResult> DeleteSubuserByEmail(string email)
        {
            try
            {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userEmail))
             return Unauthorized(new { message = "User not authenticated" });

 var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
    if (subuser == null)
     return NotFound(new { message = "Subuser not found" });

       // Check access permissions
                if ((userRole == "manager") && subuser.user_email != userEmail)
         return Forbid();

        _context.subuser.Remove(subuser);
         await _context.SaveChangesAsync();

                return Ok(new { message = "Subuser deleted successfully", email = email });
        }
  catch (Exception ex)
      {
          _logger.LogError(ex, "Error deleting subuser by email {Email}", email);
    return StatusCode(500, new { message = "Error deleting subuser" });
    }
        }
    }
}
