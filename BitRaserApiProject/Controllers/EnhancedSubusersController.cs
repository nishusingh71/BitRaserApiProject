using System.Security.Claims;
using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Subusers management controller with comprehensive user name and role information
    /// Supports email-based operations and role-based access control
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedSubusersController : ControllerBase
    {
  private readonly ApplicationDbContext _context;
   private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;

        public EnhancedSubusersController(
    ApplicationDbContext context,
         IRoleBasedAuthService authService,
      IUserDataService userDataService)
     {
_context = context;
    _authService = authService;
       _userDataService = userDataService;
        }

        /// <summary>
        /// Get all subusers with user name and roles
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllSubusers(
      [FromQuery] string? userEmail = null,
            [FromQuery] string? subuserEmail = null,
       [FromQuery] string? name = null,
[FromQuery] string? status = null,
            [FromQuery] string? role = null,
          [FromQuery] int page = 0,
            [FromQuery] int pageSize = 100)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
       
      IQueryable<subuser> query = _context.subuser
      .Include(s => s.SubuserRoles)
           .ThenInclude(sr => sr.Role);

            // Apply role-based filtering
 if (!await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser))
            {
     // Users can only see their own subusers
         query = query.Where(s => s.user_email == currentUserEmail);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(userEmail))
        query = query.Where(s => s.user_email.Contains(userEmail));

 if (!string.IsNullOrEmpty(subuserEmail))
        query = query.Where(s => s.subuser_email.Contains(subuserEmail));

   if (!string.IsNullOrEmpty(name))
       query = query.Where(s => s.Name != null && s.Name.Contains(name));

       if (!string.IsNullOrEmpty(status))
       query = query.Where(s => s.status != null && s.status.Contains(status));

  if (!string.IsNullOrEmpty(role))
     query = query.Where(s => s.Role.Contains(role));

    var subusers = await query
 .OrderByDescending(s => s.CreatedAt)
      .Skip(page * pageSize)
        .Take(pageSize)
           .ToListAsync();

     var subuserDetails = subusers.Select(s => new {
   s.subuser_id,
 s.subuser_email,
  s.user_email, // Parent user email
    name = s.Name ?? "N/A",
phone = s.Phone ?? "N/A",
      department = s.Department ?? "N/A",
      role = s.Role ?? "N/A",
    status = s.status ?? "active",
    last_login = s.last_login,
  subuser_group = s.GroupId.HasValue ? 
  _context.Set<Group>().Where(g => g.group_id == s.GroupId.Value).Select(g => g.name).FirstOrDefault() ?? "No Group" 
    : "No Group",
isEmailVerified = s.IsEmailVerified,
  assignedMachines = s.AssignedMachines ?? 0,
maxMachines = s.MaxMachines ?? 5,
     // Roles from SubuserRoles relationship
     roles = s.SubuserRoles.Select(sr => new {
   roleId = sr.RoleId,
   roleName = sr.Role.RoleName,
description = sr.Role.Description,
  hierarchyLevel = sr.Role.HierarchyLevel,
 assignedAt = sr.AssignedAt,
assignedBy = sr.AssignedByEmail
}).ToList(),
createdAt = s.CreatedAt,
        updatedAt = s.UpdatedAt,
 lastLoginIp = s.LastLoginIp ?? "N/A"
     }).ToList();

     return Ok(subuserDetails);
        }

        /// <summary>
    /// Get subuser by email with full details including name and roles
        /// </summary>
      [HttpGet("by-email/{email}")]
        public async Task<ActionResult<object>> GetSubuserByEmail(string email)
  {
     var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
    
      var subuser = await _context.subuser
       .Include(s => s.SubuserRoles)
    .ThenInclude(sr => sr.Role)
    .ThenInclude(r => r.RolePermissions)
    .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(s => s.subuser_email == email);
     
       if (subuser == null) 
                return NotFound($"Subuser with email {email} not found");

            // Check if user can view this subuser
bool canView = subuser.user_email == currentUserEmail ||
     await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser);

       if (!canView)
    {
                return StatusCode(403, new { error = "You can only view your own subusers" });
            }

     var subuserDetails = new {
subuser.subuser_id,
  subuser.subuser_email,
     subuser.user_email,
   name = subuser.Name ?? "N/A",
   phone = subuser.Phone ?? "N/A",
   department = subuser.Department ?? "N/A",
    role = subuser.Role ?? "N/A",
    status = subuser.status ?? "active",
    last_login = subuser.last_login,
    subuser_group = subuser.GroupId.HasValue ? 
 _context.Set<Group>().Where(g => g.group_id == subuser.GroupId.Value).Select(g => g.name).FirstOrDefault() ?? "No Group"
: "No Group",
       isEmailVerified = subuser.IsEmailVerified,
    // Detailed roles information
  roles = subuser.SubuserRoles.Select(sr => new {
    roleId = sr.RoleId,
    roleName = sr.Role.RoleName,
description = sr.Role.Description,
 hierarchyLevel = sr.Role.HierarchyLevel,
 assignedAt = sr.AssignedAt,
    assignedBy = sr.AssignedByEmail
  }).ToList(),
// Permissions from roles
   permissions = subuser.SubuserRoles
    .SelectMany(sr => sr.Role.RolePermissions)
   .Select(rp => rp.Permission.PermissionName)
  .Distinct()
        .ToList(),
  // Machine and license info
   assignedMachines = subuser.AssignedMachines ?? 0,
          maxMachines = subuser.MaxMachines ?? 5,
  groupId = subuser.GroupId,
// Permissions flags
    canCreateSubusers = subuser.CanCreateSubusers,
     canViewReports = subuser.CanViewReports,
canManageMachines = subuser.CanManageMachines,
   canAssignLicenses = subuser.CanAssignLicenses,
// Notifications
emailNotifications = subuser.EmailNotifications,
  systemAlerts = subuser.SystemAlerts,
     // Security info
   lastLoginIp = subuser.LastLoginIp ?? "N/A",
  failedLoginAttempts = subuser.FailedLoginAttempts,
    lockedUntil = subuser.LockedUntil,
  // Audit info
   createdAt = subuser.CreatedAt,
   createdBy = subuser.CreatedBy,
   updatedAt = subuser.UpdatedAt,
   updatedBy = subuser.UpdatedBy,
 notes = subuser.Notes ?? ""
  };

    return Ok(subuserDetails);
      }

    /// <summary>
        /// Get subusers by parent user email
   /// </summary>
        [HttpGet("by-parent/{parentEmail}")]
        public async Task<ActionResult<IEnumerable<object>>> GetSubusersByParent(string parentEmail)
    {
  var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
     
       // Check if user can view subusers for this parent
    bool canView = parentEmail == currentUserEmail ||
          await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser);

   if (!canView)
         {
   return StatusCode(403, new { error = "You can only view your own subusers" });
    }

            var subusers = await _context.subuser
      .Include(s => s.SubuserRoles)
       .ThenInclude(sr => sr.Role)
         .Where(s => s.user_email == parentEmail)
   .OrderByDescending(s => s.CreatedAt)
       .ToListAsync();

            var subuserDetails = subusers.Select(s => new {
    s.subuser_id,
  s.subuser_email,
    name = s.Name ?? "N/A",
  phone = s.Phone ?? "N/A",
  department = s.Department ?? "N/A",
  role = s.Role ?? "N/A",
       status = s.status ?? "active",
 last_login = s.last_login,
   subuser_group = s.GroupId.HasValue ? 
 _context.Set<Group>().Where(g => g.group_id == s.GroupId.Value).Select(g => g.name).FirstOrDefault() ?? "No Group"
        : "No Group",
 // Roles information
       roles = s.SubuserRoles.Select(sr => new {
  roleId = sr.RoleId,
    roleName = sr.Role.RoleName,
 hierarchyLevel = sr.Role.HierarchyLevel
 }).ToList(),
   assignedMachines = s.AssignedMachines ?? 0,
  maxMachines = s.MaxMachines ?? 5,
  isEmailVerified = s.IsEmailVerified,
   createdAt = s.CreatedAt
 }).ToList();

   return Ok(subuserDetails);
        }

    /// <summary>
        /// Create new subuser with name and role assignment
        /// </summary>
        [HttpPost]
      [RequirePermission("CREATE_SUBUSER")]
        public async Task<ActionResult<object>> CreateSubuser([FromBody] CreateSubuserDto request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
         if (!await _authService.HasPermissionAsync(currentUserEmail!, "CREATE_SUBUSER", isCurrentUserSubuser))
       return StatusCode(403, new { error = "Insufficient permissions to create subusers" });

       // Check if subuser already exists
     var existingSubuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == request.Email);
        if (existingSubuser != null)
     return Conflict($"Subuser with email {request.Email} already exists");

            // Get parent user
            var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == currentUserEmail);
      if (parentUser == null)
           return BadRequest("Parent user not found");

            // Create subuser with name
 var newSubuser = new subuser
            {
  subuser_email = request.Email,
  subuser_password = BCrypt.Net.BCrypt.HashPassword(request.Password),
      user_email = currentUserEmail!,
   superuser_id = parentUser.user_id,
      Name = request.Name,
  Phone = request.Phone ?? "",
    Department = request.Department ?? "",
  Role = request.Role ?? "subuser",
  status = "active",
    IsEmailVerified = false,
     MaxMachines = request.MaxMachines ?? 5,
   GroupId = request.GroupId,
      CanCreateSubusers = request.CanCreateSubusers ?? false,
 CanViewReports = request.CanViewReports ?? true,
        CanManageMachines = request.CanManageMachines ?? false,
    CanAssignLicenses = request.CanAssignLicenses ?? false,
    EmailNotifications = request.EmailNotifications ?? true,
     SystemAlerts = request.SystemAlerts ?? true,
       CreatedBy = parentUser.user_id,
       CreatedAt = DateTime.UtcNow,
      Notes = request.Notes
    };

   _context.subuser.Add(newSubuser);
            await _context.SaveChangesAsync();

       // Assign default SubUser role
   await AssignRoleToSubuserAsync(newSubuser.subuser_email, "SubUser", currentUserEmail!);

            // Reload with roles
            var createdSubuser = await _context.subuser
   .Include(s => s.SubuserRoles)
                .ThenInclude(sr => sr.Role)
       .FirstOrDefaultAsync(s => s.subuser_id == newSubuser.subuser_id);

          var response = new {
      subuser_id = createdSubuser!.subuser_id,
    subuser_email = createdSubuser.subuser_email,
         name = createdSubuser.Name,
       phone = createdSubuser.Phone,
              department = createdSubuser.Department,
      role = createdSubuser.Role,
          roles = createdSubuser.SubuserRoles.Select(sr => new {
    roleName = sr.Role.RoleName,
                    hierarchyLevel = sr.Role.HierarchyLevel
                }).ToList(),
        createdAt = createdSubuser.CreatedAt,
       message = "Subuser created successfully"
 };

          return CreatedAtAction(nameof(GetSubuserByEmail), new { email = newSubuser.subuser_email }, response);
        }

   /// <summary>
        /// Update subuser details including name
      /// </summary>
        [HttpPut("{email}")]
        [RequirePermission("UPDATE_SUBUSER")]
        public async Task<IActionResult> UpdateSubuser(string email, [FromBody] UpdateSubuserDto request)
    {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
     
 if (subuser == null) return NotFound($"Subuser with email {email} not found");

    // Check if user can update this subuser
 bool canUpdate = subuser.user_email == currentUserEmail ||
      await _authService.HasPermissionAsync(currentUserEmail!, "UPDATE_ALL_SUBUSERS", isCurrentUserSubuser);

       if (!canUpdate)
{
             return StatusCode(403, new { error = "You can only update your own subusers" });
     }

     // Update subuser information
            if (!string.IsNullOrEmpty(request.Name))
    subuser.Name = request.Name;

     if (!string.IsNullOrEmpty(request.Phone))
        subuser.Phone = request.Phone;

      if (!string.IsNullOrEmpty(request.Department))
     subuser.Department = request.Department;

 if (!string.IsNullOrEmpty(request.Status))
    subuser.status = request.Status;

        if (request.MaxMachines.HasValue)
           subuser.MaxMachines = request.MaxMachines.Value;

      if (request.CanViewReports.HasValue)
     subuser.CanViewReports = request.CanViewReports.Value;

          if (request.CanManageMachines.HasValue)
       subuser.CanManageMachines = request.CanManageMachines.Value;

       if (request.CanAssignLicenses.HasValue)
       subuser.CanAssignLicenses = request.CanAssignLicenses.Value;

   if (!string.IsNullOrEmpty(request.Notes))
      subuser.Notes = request.Notes;

        var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == currentUserEmail);
  if (parentUser != null)
            {
        subuser.UpdatedBy = parentUser.user_id;
      }
            subuser.UpdatedAt = DateTime.UtcNow;

     _context.Entry(subuser).State = EntityState.Modified;
         await _context.SaveChangesAsync();

        return Ok(new { 
         message = "Subuser updated successfully", 
     subuser_email = email,
      name = subuser.Name,
  updatedAt = subuser.UpdatedAt
 });
   }

        /// <summary>
        /// Assign role to subuser
  /// </summary>
        [HttpPost("{email}/assign-role")]
        [RequirePermission("ASSIGN_ROLES")]
 public async Task<IActionResult> AssignRole(string email, [FromBody] SubuserRoleAssignDto roleDto)
        {
          var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
      if (!await _authService.HasPermissionAsync(currentUserEmail!, "ASSIGN_ROLES"))
     return StatusCode(403, new { error = "Insufficient permissions to assign roles" });

      var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
          if (subuser == null) return NotFound($"Subuser with email {email} not found");

      // Check if this is user's subuser
     if (subuser.user_email != currentUserEmail && 
        !await _authService.HasPermissionAsync(currentUserEmail!, "ASSIGN_ROLES_TO_ALL"))
        {
        return StatusCode(403, new { error = "You can only assign roles to your own subusers" });
       }

            await AssignRoleToSubuserAsync(email, roleDto.RoleName, currentUserEmail!);

     return Ok(new { 
       message = $"Role {roleDto.RoleName} assigned to subuser {email}", 
            subuser_email = email,
                roleName = roleDto.RoleName,
              assignedBy = currentUserEmail,
            assignedAt = DateTime.UtcNow
    });
        }

      /// <summary>
  /// Remove role from subuser
  /// </summary>
    [HttpDelete("{email}/remove-role/{roleName}")]
      [RequirePermission("ASSIGN_ROLES")]
        public async Task<IActionResult> RemoveRole(string email, string roleName)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
            if (subuser == null) return NotFound($"Subuser with email {email} not found");

            // Check if this is user's subuser
            if (subuser.user_email != currentUserEmail && 
  !await _authService.HasPermissionAsync(currentUserEmail!, "ASSIGN_ROLES_TO_ALL"))
            {
     return StatusCode(403, new { error = "You can only remove roles from your own subusers" });
    }

         var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role == null) return NotFound($"Role {roleName} not found");

 var subuserRole = await _context.Set<SubuserRole>()
       .FirstOrDefaultAsync(sr => sr.SubuserId == subuser.subuser_id && sr.RoleId == role.RoleId);

            if (subuserRole == null)
     return NotFound($"Role {roleName} not assigned to subuser {email}");

  _context.Set<SubuserRole>().Remove(subuserRole);
        await _context.SaveChangesAsync();

            return Ok(new { 
message = $"Role {roleName} removed from subuser {email}",
   subuser_email = email,
       roleName = roleName,
      removedBy = currentUserEmail,
        removedAt = DateTime.UtcNow
   });
        }

        /// <summary>
        /// Get subuser statistics including role distribution
        /// </summary>
[HttpGet("statistics")]
        public async Task<ActionResult<object>> GetSubuserStatistics([FromQuery] string? parentEmail)
     {
     var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
        
      IQueryable<subuser> query = _context.subuser
     .Include(s => s.SubuserRoles)
.ThenInclude(sr => sr.Role);

          // Apply role-based filtering
if (!await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSER_STATISTICS", isCurrentUserSubuser))
        {
     // Users can only see their own subuser statistics
     parentEmail = currentUserEmail;
            }

      if (!string.IsNullOrEmpty(parentEmail))
       query = query.Where(s => s.user_email == parentEmail);

            var stats = new {
     TotalSubusers = await query.CountAsync(),
  ActiveSubusers = await query.CountAsync(s => s.status == "active"),
     InactiveSubusers = await query.CountAsync(s => s.status == "inactive"),
 SuspendedSubusers = await query.CountAsync(s => s.status == "suspended"),
           VerifiedEmails = await query.CountAsync(s => s.IsEmailVerified),
   UnverifiedEmails = await query.CountAsync(s => !s.IsEmailVerified),
    SubusersCreatedToday = await query.CountAsync(s => s.CreatedAt.Date == DateTime.UtcNow.Date),
SubusersCreatedThisWeek = await query.CountAsync(s => s.CreatedAt >= DateTime.UtcNow.AddDays(-7)),
    SubusersCreatedThisMonth = await query.CountAsync(s => s.CreatedAt.Month == DateTime.UtcNow.Month),
     // Role distribution
     RoleDistribution = await query
  .SelectMany(s => s.SubuserRoles)
        .GroupBy(sr => sr.Role.RoleName)
      .Select(g => new { RoleName = g.Key, Count = g.Count() })
    .ToListAsync(),
 // Department distribution
  DepartmentDistribution = await query
        .Where(s => !string.IsNullOrEmpty(s.Department))
     .GroupBy(s => s.Department)
  .Select(g => new { Department = g.Key, Count = g.Count() })
  .OrderByDescending(x => x.Count)
     .Take(10)
      .ToListAsync(),
     // Recent subusers with roles
            RecentSubusers = await query
        .OrderByDescending(s => s.CreatedAt)
          .Take(5)
     .Select(s => new {
        s.subuser_email,
          name = s.Name ?? "N/A",
           roles = s.SubuserRoles.Select(sr => sr.Role.RoleName).ToList(),
   s.CreatedAt
    })
   .ToListAsync()
    };

  return Ok(stats);
        }

        /// <summary>
        /// Delete subuser
        /// </summary>
        [HttpDelete("{email}")]
        [RequirePermission("DELETE_SUBUSER")]
        public async Task<IActionResult> DeleteSubuser(string email)
        {
  var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
   var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
    
            if (subuser == null) return NotFound($"Subuser with email {email} not found");

            // Check permissions
  bool canDelete = subuser.user_email == currentUserEmail ||
           await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_ALL_SUBUSERS", isCurrentUserSubuser);

  if (!canDelete)
          {
  return StatusCode(403, new { error = "You can only delete your own subusers" });
        }

   var subuserName = subuser.Name;
 _context.subuser.Remove(subuser);
     await _context.SaveChangesAsync();

            return Ok(new { 
     message = "Subuser deleted successfully", 
    subuser_email = email,
             name = subuserName,
         deletedAt = DateTime.UtcNow
    });
        }

        #region Private Helper Methods

        private async Task AssignRoleToSubuserAsync(string subuserEmail, string roleName, string assignedByEmail)
        {
var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == subuserEmail);
         var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);

       if (subuser != null && role != null)
   {
     // Check if role already assigned
      var existingRole = await _context.Set<SubuserRole>()
         .FirstOrDefaultAsync(sr => sr.SubuserId == subuser.subuser_id && sr.RoleId == role.RoleId);

    if (existingRole == null)
         {
            var subuserRole = new SubuserRole
        {
     SubuserId = subuser.subuser_id,
       RoleId = role.RoleId,
  AssignedAt = DateTime.UtcNow,
    AssignedByEmail = assignedByEmail
         };

      _context.Set<SubuserRole>().Add(subuserRole);
           await _context.SaveChangesAsync();
       }
   }
        }

        #endregion
    }

    #region Local DTOs

    /// <summary>
    /// DTO for assigning role to subuser
    /// </summary>
    public class SubuserRoleAssignDto
    {
        [System.ComponentModel.DataAnnotations.Required]
  public string RoleName { get; set; } = string.Empty;
    }

    #endregion
}
