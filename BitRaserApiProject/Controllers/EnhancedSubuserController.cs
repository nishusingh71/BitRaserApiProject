using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Subuser management controller with email-based operations and role-based access control
    /// Supports user-friendly subuser management without strict permission requirements
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedSubuserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ILogger<EnhancedSubuserController> _logger;

        public EnhancedSubuserController(
            ApplicationDbContext context, 
            IRoleBasedAuthService authService, 
            IUserDataService userDataService,
            ILogger<EnhancedSubuserController> logger)
        {
            _context = context;
            _authService = authService;
            _userDataService = userDataService;
            _logger = logger;
        }

        /// <summary>
        /// Get all subusers with role-based filtering (email-based operations)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSubusers([FromQuery] SubuserFilterRequest? filter)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);
            
            IQueryable<subuser> query = _context.subuser;

            // Check if user has admin-level permissions
            bool hasAdminPermission = await _authService.HasPermissionAsync(
                currentUserEmail, "READ_ALL_SUBUSERS", isCurrentUserSubuser);

            if (!hasAdminPermission)
            {
                // Regular users can see their own subusers
                if (isCurrentUserSubuser)
                {
                    // Subusers cannot see any subusers by default
                    return Ok(new List<object>());
                }
                
                // Filter to only show current user's subusers
                query = query.Where(s => s.user_email == currentUserEmail);
            }

            // Apply additional filters if provided
            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.ParentUserEmail))
                    query = query.Where(s => s.user_email.Contains(filter.ParentUserEmail));

                if (!string.IsNullOrEmpty(filter.SubuserEmail))
                    query = query.Where(s => s.subuser_email.Contains(filter.SubuserEmail));
            }

            try
            {
                var subusers = await query
                    .Include(s => s.SubuserRoles)
                    .ThenInclude(sr => sr.Role)
                    .OrderByDescending(s => s.subuser_id)
                    .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                    .Take(filter?.PageSize ?? 100)
                    .Select(s => new {
                        s.subuser_email,
                        s.user_email,
                        s.subuser_id,
                        roles = s.SubuserRoles.Select(sr => sr.Role.RoleName).ToList(),
                        hasPassword = !string.IsNullOrEmpty(s.subuser_password)
                    })
                    .ToListAsync();

                return Ok(subusers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving subusers for {Email}", currentUserEmail);
                return StatusCode(500, new { 
                    message = "Error retrieving subusers", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Get subuser by email with role validation
        /// </summary>
        [HttpGet("{email}")]
        public async Task<ActionResult<object>> GetSubuser(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            var subuser = await _context.subuser
                .Include(s => s.SubuserRoles)
                .ThenInclude(sr => sr.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(s => s.subuser_email == email);
            
            if (subuser == null) return NotFound($"Subuser with email {email} not found");

            // Check if user can view this subuser
            bool canView = subuser.user_email == currentUserEmail ||
                          await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser);

            if (!canView)
            {
                return StatusCode(403, new { error = "You can only view your own subusers" });
            }

            var subuserDetails = new {
                subuser.subuser_email,
                subuser.user_email,
                subuser.subuser_id,
                roles = subuser.SubuserRoles.Select(sr => new {
                    sr.Role.RoleName,
                    sr.Role.Description,
                    sr.AssignedAt,
                    sr.AssignedByEmail
                }).ToList(),
                permissions = subuser.SubuserRoles
                    .SelectMany(sr => sr.Role.RolePermissions)
                    .Select(rp => rp.Permission.PermissionName)
                    .Distinct()
                    .ToList(),
                hasPassword = !string.IsNullOrEmpty(subuser.subuser_password)
            };

            return Ok(subuserDetails);
        }

        /// <summary>
        /// Get subusers by parent user email with management hierarchy
        /// </summary>
        [HttpGet("by-parent/{parentEmail}")]
        public async Task<ActionResult<IEnumerable<object>>> GetSubusersByParent(string parentEmail)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            // Check if user can view subusers for this parent email
            bool canView = parentEmail == currentUserEmail ||
                          await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser) ||
                          await _authService.CanManageUserAsync(currentUserEmail!, parentEmail);

            if (!canView)
            {
                return StatusCode(403, new { error = "You can only view your own subusers or subusers of users you manage" });
            }

            var subusers = await _context.subuser
                .Include(s => s.SubuserRoles)
                .ThenInclude(sr => sr.Role)
                .Where(s => s.user_email == parentEmail)
                .OrderByDescending(s => s.subuser_id)
                .Select(s => new {
                    s.subuser_email,
                    s.user_email,
                    s.subuser_id,
                    roles = s.SubuserRoles.Select(sr => sr.Role.RoleName).ToList(),
                    hasPassword = !string.IsNullOrEmpty(s.subuser_password)
                })
                .ToListAsync();

            return Ok(subusers);
        }

        /// <summary>
        /// Create a new subuser - Users can create subusers for themselves
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<object>> CreateSubuser([FromBody] SubuserCreateRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
  
            // Subusers cannot create subusers (prevent recursive subuser creation)
      if (isCurrentUserSubuser)
     {
           return StatusCode(403, new { error = "Subusers cannot create subusers" });
            }

     // Validate input - only email and password are required
            if (string.IsNullOrEmpty(request.subuser_email) || string.IsNullOrEmpty(request.subuser_password))
       return BadRequest("Subuser email and password are required");

         // Check if subuser already exists
 var existingSubuser = await _context.subuser
                .Where(s => s.subuser_email == request.subuser_email)
  .Select(s => new { s.subuser_email })  // Only select email field to avoid non-existent column errors
          .FirstOrDefaultAsync();
         
         if (existingSubuser != null)
          return Conflict($"Subuser with email {request.subuser_email} already exists");

// Check if email is already used as a main user
    var existingUser = await _context.Users
        .Where(u => u.user_email == request.subuser_email)
 .Select(u => new { u.user_email })  // Only select email field
          .FirstOrDefaultAsync();
     
   if (existingUser != null)
      return Conflict($"Email {request.subuser_email} is already used as a main user account");

 // Validate parent user email (use current user if not specified) - parentUserEmail is optional
 var parentUserEmail = request.parentUserEmail ?? currentUserEmail!;
  
            // Check if current user can create subuser for the specified parent
     if (parentUserEmail != currentUserEmail && 
        !await _authService.HasPermissionAsync(currentUserEmail!, "CREATE_SUBUSERS_FOR_OTHERS", isCurrentUserSubuser))
     {
        return StatusCode(403, new { error = "You can only create subusers for yourself" });
        }

            // Validate parent user exists
   var parentUser = await _context.Users
      .FirstOrDefaultAsync(u => u.user_email == parentUserEmail);
     if (parentUser == null)
          return BadRequest($"Parent user with email {parentUserEmail} not found");

     // Create subuser with all fields
  var newSubuser = new subuser
       {
   subuser_email = request.subuser_email,
    subuser_password = BCrypt.Net.BCrypt.HashPassword(request.subuser_password),
    user_email = parentUserEmail,
       superuser_id = parentUser.user_id,
   Name = request.subuser_name ?? request.subuser_email.Split('@')[0], // Use email prefix if name not provided
        Department = request.department, // Optional
 Role = request.role ?? "subuser", // Default to "subuser" if not provided
  Phone = request.phone, // Optional
   GroupId = !string.IsNullOrEmpty(request.subuser_group) && int.TryParse(request.subuser_group, out int groupId) ? groupId : (int?)null, // Convert string to int?
      status = "active",  // Use lowercase field to avoid duplicate column error
 IsEmailVerified = false,
    CreatedAt = DateTime.UtcNow,
       CreatedBy = parentUser.user_id
  };

    _context.subuser.Add(newSubuser);
       await _context.SaveChangesAsync();

       // ✅ Assign role to rolebasedAuth system (SubuserRoles table)
    // This ensures the role field is also added to the roles array in RBAC
       var roleToAssign = request.role ?? "SubUser";
       var roleAssigned = await AssignRoleToSubuserAsync(request.subuser_email, roleToAssign, currentUserEmail!);
       
       if (!roleAssigned)
       {
           _logger.LogWarning("Failed to assign role {RoleName} to subuser {Email} in RBAC system", roleToAssign, request.subuser_email);
       }

            var response = new {
          subuserEmail = newSubuser.subuser_email,
      subuserName = newSubuser.Name,
         department = newSubuser.Department,
      role = newSubuser.Role,
       phone = newSubuser.Phone,
  subuserGroup = newSubuser.GroupId?.ToString(), // Convert back to string for response
       parentUserEmail = newSubuser.user_email,
   subuserID = newSubuser.subuser_id,
   status = newSubuser.status,  // Use lowercase field
      createdAt = newSubuser.CreatedAt,
      roleAssignedToRBAC = roleAssigned, // Indicate if role was added to RBAC
    message = roleAssigned ? "Subuser created successfully with role assigned to RBAC" : "Subuser created successfully (role assignment to RBAC failed)"
   };

        return CreatedAtAction(nameof(GetSubuser), new { email = newSubuser.subuser_email }, response);
        }

        /// <summary>
        /// Update subuser information by email
        /// </summary>
        [HttpPut("{email}")]
        public async Task<IActionResult> UpdateSubuser(string email, [FromBody] SubuserUpdateRequest request)
        {
            if (email != request.SubuserEmail)
                return BadRequest("Email mismatch in request");

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

            // Update password if provided
            if (!string.IsNullOrEmpty(request.NewPassword))
            {
                subuser.subuser_password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            }

            // Update parent user if provided and user has permission
            if (!string.IsNullOrEmpty(request.NewParentUserEmail) && 
                request.NewParentUserEmail != subuser.user_email)
            {
                if (!await _authService.HasPermissionAsync(currentUserEmail!, "REASSIGN_SUBUSERS", isCurrentUserSubuser))
                {
                    return StatusCode(403, new { error = "Insufficient permissions to reassign subuser to different parent" });
                }

                // Validate new parent user exists
                var newParent = await _context.Users
                    .FirstOrDefaultAsync(u => u.user_email == request.NewParentUserEmail);
                if (newParent == null)
                    return BadRequest($"Parent user with email {request.NewParentUserEmail} not found");

                subuser.user_email = request.NewParentUserEmail;
                subuser.superuser_id = newParent.user_id;
            }

            _context.Entry(subuser).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Subuser updated successfully", 
                subuserEmail = email,
                updatedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// PATCH: Flexible update for subuser - Update single or multiple fields
        /// Can find subuser by parent user email OR by subuser email
        /// Supports partial updates - only fields provided will be updated
        /// </summary>
        [HttpPatch("update")]
        public async Task<IActionResult> PatchSubuser([FromBody] SubuserPatchRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          if (string.IsNullOrEmpty(currentUserEmail))
            {
  return Unauthorized(new { message = "User not authenticated" });
     }

   var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

            // Find subuser by subuser_email OR by parentUserEmail + subuser_email combination
            subuser? targetSubuser = null;

      if (!string.IsNullOrEmpty(request.subuser_email))
            {
 // First try to find by subuser_email directly
 targetSubuser = await _context.subuser
          .FirstOrDefaultAsync(s => s.subuser_email == request.subuser_email);

       // If parentUserEmail is also provided, validate it matches
          if (targetSubuser != null && !string.IsNullOrEmpty(request.parentUserEmail))
                {
    if (targetSubuser.user_email != request.parentUserEmail)
    {
       return BadRequest(new { 
  message = "Subuser email and parent user email mismatch",
              subuserEmail = request.subuser_email,
     providedParent = request.parentUserEmail,
     actualParent = targetSubuser.user_email
         });
  }
    }
            }
          else if (!string.IsNullOrEmpty(request.parentUserEmail))
    {
           // If only parent email provided, we need more context
      return BadRequest(new { 
          message = "subuser_email is required for update. Provide subuser_email to identify which subuser to update" 
          });
   }
     else
   {
   return BadRequest(new { 
        message = "Either subuser_email or both subuser_email and parentUserEmail are required" 
  });
      }

            if (targetSubuser == null)
            {
       return NotFound(new { 
  message = $"Subuser not found",
    subuser_email = request.subuser_email
          });
        }

 // Check if current user can update this subuser
    bool canUpdate = targetSubuser.user_email == currentUserEmail ||
             await _authService.HasPermissionAsync(currentUserEmail, "UPDATE_ALL_SUBUSERS", isCurrentUserSubuser);

       if (!canUpdate)
       {
        return StatusCode(403, new { 
              message = "You can only update your own subusers",
      currentUser = currentUserEmail,
      subuserParent = targetSubuser.user_email
  });
  }

   var updatedFields = new List<string>();

   // Update subuser_email if provided (email change)
      if (!string.IsNullOrEmpty(request.new_subuser_email) && 
        request.new_subuser_email != targetSubuser.subuser_email)
            {
    // Check if new email already exists
          var emailExists = await _context.subuser
        .AnyAsync(s => s.subuser_email == request.new_subuser_email && s.subuser_id != targetSubuser.subuser_id);
          
  if (emailExists)
       {
          return Conflict(new { message = $"Email {request.new_subuser_email} is already in use" });
        }

       targetSubuser.subuser_email = request.new_subuser_email;
        updatedFields.Add("subuser_email");
    }

 // Update password if provided
     if (!string.IsNullOrEmpty(request.subuser_password))
            {
    targetSubuser.subuser_password = BCrypt.Net.BCrypt.HashPassword(request.subuser_password);
       updatedFields.Add("subuser_password");
  }

            // Update subuser_name if provided
   if (request.subuser_name != null) // Allow empty string to clear name
        {
     targetSubuser.Name = string.IsNullOrEmpty(request.subuser_name) ? null : request.subuser_name;
                updatedFields.Add("subuser_name");
   }

       // Update department if provided
    if (request.department != null)
         {
                targetSubuser.Department = string.IsNullOrEmpty(request.department) ? null : request.department;
        updatedFields.Add("department");
          }

       // Update role if provided
          if (request.role != null)
            {
    targetSubuser.Role = string.IsNullOrEmpty(request.role) ? "subuser" : request.role;
  updatedFields.Add("role");
 }

            // Update phone if provided
         if (request.phone != null)
            {
        targetSubuser.Phone = string.IsNullOrEmpty(request.phone) ? null : request.phone;
       updatedFields.Add("phone");
     }

      // Update subuser_group if provided
            if (request.subuser_group != null)
{
 if (string.IsNullOrEmpty(request.subuser_group))
            {
          targetSubuser.GroupId = null;
    }
  else if (int.TryParse(request.subuser_group, out int groupId))
           {
 // Validate group exists
  var groupExists = await _context.Set<Group>().AnyAsync(g => g.group_id == groupId);
        if (!groupExists)
               {
       return BadRequest(new { message = $"Group with ID {groupId} not found" });
}
           targetSubuser.GroupId = groupId;
        }
          else
       {
         return BadRequest(new { message = $"Invalid group ID format: {request.subuser_group}" });
 }
   updatedFields.Add("subuser_group");
     }

     // Update parentUserEmail if provided (reassign subuser to different parent)
    if (!string.IsNullOrEmpty(request.new_parentUserEmail) && 
     request.new_parentUserEmail != targetSubuser.user_email)
            {
    // Check permission for reassignment
            if (!await _authService.HasPermissionAsync(currentUserEmail, "REASSIGN_SUBUSERS", isCurrentUserSubuser))
                {
           return StatusCode(403, new { 
           message = "Insufficient permissions to reassign subuser to different parent" 
            });
    }

     // Validate new parent exists
            var newParent = await _context.Users
            .FirstOrDefaultAsync(u => u.user_email == request.new_parentUserEmail);
                
   if (newParent == null)
   {
           return BadRequest(new { message = $"Parent user {request.new_parentUserEmail} not found" });
        }

   targetSubuser.user_email = request.new_parentUserEmail;
 targetSubuser.superuser_id = newParent.user_id;
             updatedFields.Add("parentUserEmail");
         }

   if (updatedFields.Count == 0)
            {
       return BadRequest(new { message = "No fields to update. Please provide at least one field to update" });
    }

            // Update timestamp
            targetSubuser.UpdatedAt = DateTime.UtcNow;
     targetSubuser.UpdatedBy = (await _context.Users.FirstOrDefaultAsync(u => u.user_email == currentUserEmail))?.user_id;

            try
    {
           _context.Entry(targetSubuser).State = EntityState.Modified;
       await _context.SaveChangesAsync();

        _logger.LogInformation("Subuser {Email} updated by {UpdatedBy}. Fields: {Fields}", 
           targetSubuser.subuser_email, currentUserEmail, string.Join(", ", updatedFields));

           return Ok(new
      {
        message = "Subuser updated successfully",
           subuser_email = targetSubuser.subuser_email,
       subuser_name = targetSubuser.Name,
              department = targetSubuser.Department,
role = targetSubuser.Role,
        phone = targetSubuser.Phone,
    subuser_group = targetSubuser.GroupId?.ToString(),
     parentUserEmail = targetSubuser.user_email,
        updatedFields = updatedFields,
       updatedAt = targetSubuser.UpdatedAt,
updatedBy = currentUserEmail
            });
   }
    catch (DbUpdateConcurrencyException ex)
    {
       _logger.LogError(ex, "Concurrency error updating subuser {Email}", targetSubuser.subuser_email);
         return StatusCode(409, new { message = "Concurrency error. The subuser was modified by another user. Please retry." });
            }
 catch (Exception ex)
         {
  _logger.LogError(ex, "Error updating subuser {Email}", targetSubuser.subuser_email);
 return StatusCode(500, new { 
        message = "Error updating subuser",
      error = ex.Message
         });
 }
        }

        /// <summary>
/// Change subuser password by email
   /// </summary>
        [HttpPatch("{email}/change-password")]
        public async Task<IActionResult> ChangeSubuserPassword(string email, [FromBody] ChangePasswordRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
            
            if (subuser == null) return NotFound($"Subuser with email {email} not found");

            // Check if user can change password for this subuser
            bool canChange = subuser.user_email == currentUserEmail ||
                           await _authService.HasPermissionAsync(currentUserEmail!, "CHANGE_ALL_SUBUSER_PASSWORDS", isCurrentUserSubuser);

            if (!canChange)
            {
                return StatusCode(403, new { error = "You can only change passwords for your own subusers" });
            }

            if (string.IsNullOrEmpty(request.NewPassword))
                return BadRequest("New password is required");

            subuser.subuser_password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Subuser password changed successfully", 
                subuserEmail = email
            });
        }

        /// <summary>
        /// Assign role to subuser by email - Users can assign roles to their subusers
        /// </summary>
        [HttpPost("{email}/assign-role")]
        public async Task<IActionResult> AssignRoleToSubuser(string email, [FromBody] AssignRoleRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
            if (subuser == null) return NotFound($"Subuser with email {email} not found");

            // Check if user can assign roles to this subuser
            bool canAssign = subuser.user_email == currentUserEmail ||
                           await _authService.HasPermissionAsync(currentUserEmail!, "ASSIGN_ALL_SUBUSER_ROLES", isCurrentUserSubuser);

            if (!canAssign)
            {
                return StatusCode(403, new { error = "You can only assign roles to your own subusers" });
            }

            await AssignRoleToSubuserAsync(email, request.RoleName, currentUserEmail!);

            return Ok(new { 
                message = $"Role {request.RoleName} assigned to subuser {email}", 
                subuserEmail = email,
                roleName = request.RoleName,
                assignedBy = currentUserEmail,
                assignedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Remove role from subuser by email
        /// </summary>
        [HttpDelete("{email}/remove-role/{roleName}")]
        public async Task<IActionResult> RemoveRoleFromSubuser(string email, string roleName)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
            if (subuser == null) return NotFound($"Subuser with email {email} not found");

            // Check permissions
            bool canRemove = subuser.user_email == currentUserEmail ||
                           await _authService.HasPermissionAsync(currentUserEmail!, "REMOVE_ALL_SUBUSER_ROLES", isCurrentUserSubuser);

            if (!canRemove)
            {
                return StatusCode(403, new { error = "You can only remove roles from your own subusers" });
            }

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role == null) return NotFound($"Role {roleName} not found");

            var subuserRole = await _context.SubuserRoles
                .FirstOrDefaultAsync(sr => sr.SubuserId == subuser.subuser_id && sr.RoleId == role.RoleId);

            if (subuserRole == null)
                return NotFound($"Role {roleName} not assigned to subuser {email}");

            _context.SubuserRoles.Remove(subuserRole);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Role {roleName} removed from subuser {email}",
                subuserEmail = email,
                roleName = roleName,
                removedBy = currentUserEmail,
                removedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Delete subuser by email
        /// </summary>
        [HttpDelete("{email}")]
        public async Task<IActionResult> DeleteSubuser(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
            
            if (subuser == null) return NotFound($"Subuser with email {email} not found");

            // Check if user can delete this subuser
            bool canDelete = subuser.user_email == currentUserEmail ||
                           await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_ALL_SUBUSERS", isCurrentUserSubuser);

            if (!canDelete)
            {
                return StatusCode(403, new { error = "You can only delete your own subusers" });
            }

            _context.subuser.Remove(subuser);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Subuser deleted successfully", 
                subuserEmail = email,
                deletedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get subuser statistics by parent user email
        /// </summary>
        [HttpGet("statistics/{parentEmail}")]
        public async Task<ActionResult<object>> GetSubuserStatistics(string parentEmail)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            // Check if user can view statistics for this parent email
            bool canView = parentEmail == currentUserEmail ||
                          await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSER_STATISTICS", isCurrentUserSubuser);

            if (!canView)
            {
                return StatusCode(403, new { error = "You can only view statistics for your own subusers" });
            }

            var stats = new {
                ParentUserEmail = parentEmail,
                TotalSubusers = await _context.subuser.CountAsync(s => s.user_email == parentEmail),
                SubusersWithRoles = await _context.subuser
                    .Where(s => s.user_email == parentEmail)
                    .CountAsync(s => s.SubuserRoles.Any()),
                SubusersWithoutRoles = await _context.subuser
                    .Where(s => s.user_email == parentEmail)
                    .CountAsync(s => !s.SubuserRoles.Any()),
                RoleDistribution = await _context.SubuserRoles
                    .Join(_context.subuser, sr => sr.SubuserId, s => s.subuser_id, (sr, s) => new { sr, s })
                    .Where(joined => joined.s.user_email == parentEmail)
                    .Join(_context.Roles, joined => joined.sr.RoleId, r => r.RoleId, (joined, r) => r.RoleName)
                    .GroupBy(roleName => roleName)
                    .Select(g => new { RoleName = g.Key, Count = g.Count() })
                    .ToListAsync()
            };

            return Ok(stats);
        }

        #region Private Helper Methods

        private async Task<bool> AssignRoleToSubuserAsync(string subuserEmail, string roleName, string assignedByEmail)
        {
            try
            {
                var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == subuserEmail);
                if (subuser == null)
                {
                    _logger.LogWarning("Subuser {Email} not found for role assignment", subuserEmail);
                    return false;
                }

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                if (role == null)
                {
                    _logger.LogWarning("Role {RoleName} not found, creating default User role instead", roleName);
                    
                    // Fallback to default role if specified role doesn't exist
                    role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
                    if (role == null)
                    {
                        _logger.LogError("No default role found in system");
                        return false;
                    }
                }

                // Check if role already assigned
                var existingRole = await _context.SubuserRoles
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

                    _context.SubuserRoles.Add(subuserRole);
                    await _context.SaveChangesAsync();
                    
                    _logger.LogInformation("Role {RoleName} assigned to subuser {Email}", role.RoleName, subuserEmail);
                    return true;
                }

                return true; // Already assigned
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleName} to subuser {Email}", roleName, subuserEmail);
                return false;
            }
        }

        #endregion
    }

    #region Request Models

    /// <summary>
    /// Subuser filter request model
    /// </summary>
  public class SubuserFilterRequest
    {
      public string? ParentUserEmail { get; set; }
  public string? SubuserEmail { get; set; }
    public DateTime? CreatedFrom { get; set; }
   public DateTime? CreatedTo { get; set; }
        public int Page { get; set; } = 0;
   public int PageSize { get; set; } = 100;
    }

    /// <summary>
    /// Subuser creation request model
    /// </summary>
    public class SubuserCreateRequest
    {
  [Required(ErrorMessage = "Subuser email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
        [MaxLength(255)]
        public string subuser_email { get; set; } = string.Empty;
   
        [Required(ErrorMessage = "Password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string subuser_password { get; set; } = string.Empty;
        
     [MaxLength(100)]
 public string? subuser_name { get; set; }
        
        [MaxLength(100)]
     public string? department { get; set; }
      
        [MaxLength(50)]
   public string? role { get; set; }
        
    [MaxLength(20)]
        public string? phone { get; set; }
     
      [MaxLength(100)]
        public string? subuser_group { get; set; }  // Changed from int? to string?
        
        [EmailAddress(ErrorMessage = "Invalid parent email format")]
   public string? parentUserEmail { get; set; } // Optional - If null, uses current user
    }

    /// <summary>
    /// Subuser update request model
    /// </summary>
    public class SubuserUpdateRequest
    {
      public string SubuserEmail { get; set; } = string.Empty;
        public string? NewPassword { get; set; }
public string? NewParentUserEmail { get; set; }
    }

    /// <summary>
    /// Flexible PATCH request model for partial subuser updates
    /// All fields are optional - only provided fields will be updated
    /// </summary>
    public class SubuserPatchRequest
    {
   /// <summary>
        /// Current subuser email - Required to identify which subuser to update
    /// </summary>
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string? subuser_email { get; set; }

        /// <summary>
      /// Parent user email - Optional, used for additional validation
        /// If provided, will verify subuser belongs to this parent
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid parent email format")]
   public string? parentUserEmail { get; set; }

        /// <summary>
        /// New subuser email - Optional, use to change subuser's email address
        /// </summary>
        [EmailAddress(ErrorMessage = "Invalid new email format")]
        public string? new_subuser_email { get; set; }

  /// <summary>
        /// New password - Optional, provide to change password
        /// </summary>
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string? subuser_password { get; set; }

      /// <summary>
        /// Subuser name - Optional
        /// </summary>
      [MaxLength(100)]
        public string? subuser_name { get; set; }

    /// <summary>
        /// Department - Optional
   /// </summary>
        [MaxLength(100)]
        public string? department { get; set; }

 /// <summary>
        /// Role - Optional
        /// </summary>
   [MaxLength(50)]
     public string? role { get; set; }

        /// <summary>
        /// Phone number - Optional
        /// </summary>
    [MaxLength(20)]
  public string? phone { get; set; }

        /// <summary>
    /// Subuser group (as string, will be converted to int internally) - Optional
        /// </summary>
        [MaxLength(100)]
        public string? subuser_group { get; set; }

        /// <summary>
        /// New parent user email - Optional, use to reassign subuser to different parent
        /// Requires REASSIGN_SUBUSERS permission
      /// </summary>
        [EmailAddress(ErrorMessage = "Invalid new parent email format")]
        public string? new_parentUserEmail { get; set; }
    }

    /// <summary>
    /// Change password request model
    /// </summary>
    public class ChangePasswordRequest
    {
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
 /// Assign role request model
    /// </summary>
    public class AssignRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
    }

    #endregion
}