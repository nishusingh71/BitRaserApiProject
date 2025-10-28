using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Group Management Controller - Complete CRUD for Groups (BitRaser Design)
    /// Features: Create, Edit, Delete Groups | Assign Permissions | Manage License Allocation
 /// </summary>
 [ApiController]
  [Route("api/[controller]")]
    [Authorize]
    public class GroupManagementController : ControllerBase
    {
    private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
      private readonly IUserDataService _userDataService;
        private readonly ILogger<GroupManagementController> _logger;

        public GroupManagementController(
  ApplicationDbContext context,
       IRoleBasedAuthService authService,
            IUserDataService userDataService,
     ILogger<GroupManagementController> logger)
        {
       _context = context;
            _authService = authService;
 _userDataService = userDataService;
      _logger = logger;
        }

        /// <summary>
        /// GET /api/GroupManagement - Get all groups with search, pagination
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<ManageGroupsResponseDto>> GetAllGroups(
      [FromQuery] string? search = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
   [FromQuery] string? sortBy = "name",
  [FromQuery] string? sortOrder = "asc")
        {
            try
            {
   var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (string.IsNullOrEmpty(userEmail))
   {
              return Unauthorized(new { message = "User not authenticated" });
       }

       var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

   // Check permissions
        if (!await _authService.HasPermissionAsync(userEmail, "VIEW_GROUPS", isSubuser) &&
  !await _authService.HasPermissionAsync(userEmail, "MANAGE_GROUPS", isSubuser))
         {
         return StatusCode(403, new { message = "Insufficient permissions to view groups" });
}

      // Using Roles as Groups (as per your database structure)
       var query = _context.Roles.AsQueryable();

    // Apply search filter
           if (!string.IsNullOrEmpty(search))
    {
        query = query.Where(r => 
      r.RoleName.Contains(search) || 
          (r.Description != null && r.Description.Contains(search)));
       }

       var totalCount = await query.CountAsync();

         // Apply sorting
        query = sortBy?.ToLower() switch
   {
              "users" => sortOrder?.ToLower() == "desc" 
 ? query.OrderByDescending(r => _context.UserRoles.Count(ur => ur.RoleId == r.RoleId))
       : query.OrderBy(r => _context.UserRoles.Count(ur => ur.RoleId == r.RoleId)),
               "licenses" => sortOrder?.ToLower() == "desc"
     ? query.OrderByDescending(r => r.RoleId)
         : query.OrderBy(r => r.RoleId),
         "created" => sortOrder?.ToLower() == "desc"
      ? query.OrderByDescending(r => r.CreatedAt)
 : query.OrderBy(r => r.CreatedAt),
       _ => sortOrder?.ToLower() == "desc"
     ? query.OrderByDescending(r => r.RoleName)
       : query.OrderBy(r => r.RoleName)
           };

    var groups = await query
     .Skip((page - 1) * pageSize)
            .Take(pageSize)
      .Select(r => new GroupCardDto
         {
 GroupId = r.RoleId,
   GroupName = r.RoleName,
        Description = r.Description ?? string.Empty,
                  UserCount = _context.UserRoles.Count(ur => ur.RoleId == r.RoleId) + 
 _context.SubuserRoles.Count(sr => sr.RoleId == r.RoleId),
     LicenseCount = CalculateLicensesForGroup(r.RoleId),
            Permissions = _context.RolePermissions
        .Where(rp => rp.RoleId == r.RoleId)
     .Select(rp => rp.Permission!.PermissionName)
       .Take(3)
  .ToList(),
   MorePermissions = _context.RolePermissions
      .Count(rp => rp.RoleId == r.RoleId) > 3 
             ? _context.RolePermissions.Count(rp => rp.RoleId == r.RoleId) - 3 
      : 0,
       CreatedDate = r.CreatedAt
      })
   .ToListAsync();

  return Ok(new ManageGroupsResponseDto
       {
            Title = "Manage Groups",
          Description = "Create and manage user groups with specific permissions",
    Groups = groups,
         TotalCount = totalCount,
         Page = page,
           PageSize = pageSize,
           Showing = $"Showing {Math.Min((page - 1) * pageSize + 1, totalCount)}-{Math.Min(page * pageSize, totalCount)} of {totalCount} groups"
         });
       }
  catch (Exception ex)
{
     _logger.LogError(ex, "Error getting groups");
 return StatusCode(500, new { message = "Error retrieving groups", error = ex.Message });
    }
        }

        /// <summary>
    /// GET /api/GroupManagement/{groupId} - Get single group details
        /// </summary>
        [HttpGet("{groupId}")]
        public async Task<ActionResult<GroupDetailDto>> GetGroup(int groupId)
        {
       try
        {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userEmail))
    {
                return Unauthorized(new { message = "User not authenticated" });
     }

    var group = await _context.Roles
           .Include(r => r.RolePermissions)
 .ThenInclude(rp => rp.Permission)
       .FirstOrDefaultAsync(r => r.RoleId == groupId);

          if (group == null)
              {
  return NotFound(new { message = "Group not found" });
             }

            var userCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == groupId) +
     await _context.SubuserRoles.CountAsync(sr => sr.RoleId == groupId);
                var licenseCount = CalculateLicensesForGroup(groupId);

  return Ok(new GroupDetailDto
                {
      GroupId = group.RoleId,
           GroupName = group.RoleName,
        Description = group.Description ?? string.Empty,
          LicenseAllocation = licenseCount,
UserCount = userCount,
      Permissions = group.RolePermissions?
         .Where(rp => rp.Permission != null)
            .Select(rp => rp.Permission!.PermissionName)
        .ToList() ?? new List<string>(),
     CreatedDate = group.CreatedAt
     });
   }
      catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting group {GroupId}", groupId);
       return StatusCode(500, new { message = "Error retrieving group details", error = ex.Message });
  }
        }

        /// <summary>
        /// POST /api/GroupManagement - Create new group
        /// </summary>
   [HttpPost]
        public async Task<ActionResult<CreateGroupResponseDto>> CreateGroup([FromBody] CreateGroupDto request)
        {
            try
     {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
    {
         return Unauthorized(new { message = "User not authenticated" });
  }

         var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

   if (!await _authService.HasPermissionAsync(userEmail, "CREATE_GROUP", isSubuser) &&
    !await _authService.HasPermissionAsync(userEmail, "MANAGE_GROUPS", isSubuser))
    {
     return StatusCode(403, new { message = "Insufficient permissions to create groups" });
     }

      // Validate
      if (string.IsNullOrEmpty(request.GroupName))
             {
         return BadRequest(new { message = "Group name is required" });
          }

       // Check if group already exists
          if (await _context.Roles.AnyAsync(r => r.RoleName == request.GroupName))
          {
         return Conflict(new { message = "Group with this name already exists" });
              }

          // Create new role (group)
           var newGroup = new Role
     {
     RoleName = request.GroupName,
      Description = request.Description ?? string.Empty,
 HierarchyLevel = 10, // Default level
        CreatedAt = DateTime.UtcNow
       };

     _context.Roles.Add(newGroup);
            await _context.SaveChangesAsync();

      // Assign permissions
       if (request.Permissions != null && request.Permissions.Any())
            {
 foreach (var permName in request.Permissions)
          {
 var permission = await _context.Permissions
      .FirstOrDefaultAsync(p => p.PermissionName == permName);

     if (permission != null)
  {
  _context.RolePermissions.Add(new RolePermission
  {
          RoleId = newGroup.RoleId,
              PermissionId = permission.PermissionId
    });
        }
              }
    await _context.SaveChangesAsync();
        }

                _logger.LogInformation("Group {GroupName} created by {Email}", request.GroupName, userEmail);

          return Ok(new CreateGroupResponseDto
       {
    Success = true,
          Message = "Group created successfully",
            GroupId = newGroup.RoleId,
             GroupName = newGroup.RoleName,
        CreatedAt = newGroup.CreatedAt
       });
         }
       catch (Exception ex)
 {
                _logger.LogError(ex, "Error creating group");
           return StatusCode(500, new { message = "Error creating group", error = ex.Message });
   }
  }

        /// <summary>
        /// PUT /api/GroupManagement/{groupId} - Update existing group
  /// </summary>
        [HttpPut("{groupId}")]
        public async Task<IActionResult> UpdateGroup(int groupId, [FromBody] UpdateGroupDto request)
  {
     try
  {
        var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     if (string.IsNullOrEmpty(userEmail))
 {
    return Unauthorized(new { message = "User not authenticated" });
    }

          var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

                if (!await _authService.HasPermissionAsync(userEmail, "UPDATE_GROUP", isSubuser) &&
          !await _authService.HasPermissionAsync(userEmail, "MANAGE_GROUPS", isSubuser))
       {
   return StatusCode(403, new { message = "Insufficient permissions to update groups" });
      }

       var group = await _context.Roles.FindAsync(groupId);
          if (group == null)
            {
   return NotFound(new { message = "Group not found" });
         }

          // Update basic info
                if (!string.IsNullOrEmpty(request.GroupName))
                {
   // Check if new name conflicts
   if (request.GroupName != group.RoleName &&
     await _context.Roles.AnyAsync(r => r.RoleName == request.GroupName))
           {
        return Conflict(new { message = "Group with this name already exists" });
         }
 group.RoleName = request.GroupName;
     }

    if (!string.IsNullOrEmpty(request.Description))
         {
   group.Description = request.Description;
    }

        // Update permissions
         if (request.Permissions != null)
                {
        // Remove existing permissions
          var existingPermissions = await _context.RolePermissions
      .Where(rp => rp.RoleId == groupId)
       .ToListAsync();
    _context.RolePermissions.RemoveRange(existingPermissions);

         // Add new permissions
        foreach (var permName in request.Permissions)
      {
  var permission = await _context.Permissions
    .FirstOrDefaultAsync(p => p.PermissionName == permName);

      if (permission != null)
   {
           _context.RolePermissions.Add(new RolePermission
    {
           RoleId = groupId,
 PermissionId = permission.PermissionId
           });
  }
        }
    }

      await _context.SaveChangesAsync();

         _logger.LogInformation("Group {GroupId} updated by {Email}", groupId, userEmail);

      return Ok(new { 
       message = "Group updated successfully",
   groupId = groupId,
              groupName = group.RoleName
       });
            }
         catch (Exception ex)
    {
          _logger.LogError(ex, "Error updating group {GroupId}", groupId);
          return StatusCode(500, new { message = "Error updating group", error = ex.Message });
            }
  }

    /// <summary>
        /// DELETE /api/GroupManagement/{groupId} - Delete group
        /// </summary>
   [HttpDelete("{groupId}")]
        public async Task<IActionResult> DeleteGroup(int groupId)
      {
 try
         {
 var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
             {
              return Unauthorized(new { message = "User not authenticated" });
          }

                var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

       if (!await _authService.HasPermissionAsync(userEmail, "DELETE_GROUP", isSubuser) &&
        !await _authService.HasPermissionAsync(userEmail, "MANAGE_GROUPS", isSubuser))
     {
           return StatusCode(403, new { message = "Insufficient permissions to delete groups" });
      }

     var group = await _context.Roles.FindAsync(groupId);
      if (group == null)
       {
        return NotFound(new { message = "Group not found" });
    }

       // Check if group has users
   var userCount = await _context.UserRoles.CountAsync(ur => ur.RoleId == groupId) +
await _context.SubuserRoles.CountAsync(sr => sr.RoleId == groupId);
        if (userCount > 0)
       {
            return BadRequest(new { 
           message = "Cannot delete group with active users",
    userCount = userCount,
         suggestion = "Remove all users from this group before deleting"
 });
                }

    // Remove permissions
    var permissions = await _context.RolePermissions
        .Where(rp => rp.RoleId == groupId)
    .ToListAsync();
     _context.RolePermissions.RemoveRange(permissions);

 // Remove group
    _context.Roles.Remove(group);
        await _context.SaveChangesAsync();

     _logger.LogInformation("Group {GroupId} deleted by {Email}", groupId, userEmail);

                return Ok(new { 
     message = "Group deleted successfully",
  groupId = groupId
       });
   }
    catch (Exception ex)
            {
         _logger.LogError(ex, "Error deleting group {GroupId}", groupId);
      return StatusCode(500, new { message = "Error deleting group", error = ex.Message });
            }
        }

      /// <summary>
        /// GET /api/GroupManagement/available-permissions - Get all permissions with categories
        /// </summary>
     [HttpGet("available-permissions")]
        public async Task<ActionResult<PermissionCategoriesDto>> GetAvailablePermissions()
        {
     try
      {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
         if (string.IsNullOrEmpty(userEmail))
      {
          return Unauthorized(new { message = "User not authenticated" });
           }

       var permissions = await _context.Permissions
      .OrderBy(p => p.PermissionName)
             .Select(p => new PermissionOptionDto
            {
       Value = p.PermissionName,
   Label = FormatPermissionLabel(p.PermissionName),
     Description = p.Description ?? string.Empty
                })
        .ToListAsync();

// Group permissions by category
           var categories = new List<PermissionCategoryDto>
         {
       new PermissionCategoryDto
           {
        CategoryName = "basic_access",
  CategoryLabel = "Basic Access",
   Permissions = permissions.Where(p => p.Value.Contains("VIEW") || p.Value.Contains("READ")).ToList()
        },
             new PermissionCategoryDto
              {
           CategoryName = "advanced_erasure",
          CategoryLabel = "Advanced Erasure",
                  Permissions = permissions.Where(p => p.Value.Contains("ERASE") || p.Value.Contains("WIPE")).ToList()
 },
      new PermissionCategoryDto
     {
            CategoryName = "report_generation",
  CategoryLabel = "Report Generation",
    Permissions = permissions.Where(p => p.Value.Contains("REPORT") || p.Value.Contains("AUDIT")).ToList()
      },
             new PermissionCategoryDto
    {
             CategoryName = "user_management",
                 CategoryLabel = "User Management",
        Permissions = permissions.Where(p => p.Value.Contains("USER") || p.Value.Contains("SUBUSER")).ToList()
        },
 new PermissionCategoryDto
     {
          CategoryName = "system_settings",
             CategoryLabel = "System Settings",
             Permissions = permissions.Where(p => p.Value.Contains("SYSTEM") || p.Value.Contains("SETTINGS")).ToList()
        },
              new PermissionCategoryDto
       {
      CategoryName = "license_management",
             CategoryLabel = "License Management",
    Permissions = permissions.Where(p => p.Value.Contains("LICENSE") || p.Value.Contains("ALLOCATION")).ToList()
             }
     };

    return Ok(new PermissionCategoriesDto
              {
                    Categories = categories
    });
            }
            catch (Exception ex)
  {
             _logger.LogError(ex, "Error getting available permissions");
        return StatusCode(500, new { message = "Error retrieving permissions", error = ex.Message });
 }
        }

        /// <summary>
   /// GET /api/GroupManagement/{groupId}/members - Get group members
        /// </summary>
        [HttpGet("{groupId}/members")]
        public async Task<ActionResult<GroupMembersDto>> GetGroupMembers(int groupId)
        {
  try
       {
 var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
           {
   return Unauthorized(new { message = "User not authenticated" });
   }

      var group = await _context.Roles.FindAsync(groupId);
           if (group == null)
     {
 return NotFound(new { message = "Group not found" });
  }

                // Get main users in this group
            var mainUsers = await _context.UserRoles
            .Where(ur => ur.RoleId == groupId)
          .Include(ur => ur.User)
            .Select(ur => new GroupMemberItemDto
         {
        UserEmail = ur.User!.user_email,
       UserName = ur.User.user_name,
               JoinedDate = ur.AssignedAt,
          Status = "active",
     UserType = "user"
         })
       .ToListAsync();

            // Get subusers in this group
     var subusers = await _context.SubuserRoles
   .Where(sr => sr.RoleId == groupId)
   .Include(sr => sr.Subuser)
      .Select(sr => new GroupMemberItemDto
              {
   UserEmail = sr.Subuser!.subuser_email,
                UserName = sr.Subuser.Name ?? sr.Subuser.subuser_email,
             JoinedDate = sr.AssignedAt,
    Status = sr.Subuser.status,
 UserType = "subuser"
     })
   .ToListAsync();

 var allMembers = mainUsers.Concat(subusers).OrderBy(m => m.UserName).ToList();

           return Ok(new GroupMembersDto
  {
     GroupId = groupId,
         GroupName = group.RoleName,
          Members = allMembers,
TotalMembers = allMembers.Count
      });
            }
     catch (Exception ex)
            {
    _logger.LogError(ex, "Error getting group members for {GroupId}", groupId);
            return StatusCode(500, new { message = "Error retrieving group members", error = ex.Message });
  }
        }

        /// <summary>
        /// POST /api/GroupManagement/{groupId}/add-users - Bulk add users to group
        /// </summary>
        [HttpPost("{groupId}/add-users")]
        public async Task<ActionResult<BulkGroupOperationResponseDto>> AddUsersToGroup(
   int groupId, 
    [FromBody] BulkAddUsersToGroupDto request)
        {
            try
    {
    var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
        {
    return Unauthorized(new { message = "User not authenticated" });
      }

    var group = await _context.Roles.FindAsync(groupId);
       if (group == null)
         {
      return NotFound(new { message = "Group not found" });
     }

        int successCount = 0;
    int failedCount = 0;
     var failedEmails = new List<string>();

              foreach (var email in request.UserEmails)
 {
    try
          {
         // Check if user exists
     var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
    if (user != null)
 {
      // Check if already in group
   if (!await _context.UserRoles.AnyAsync(ur => ur.UserId == user.user_id && ur.RoleId == groupId))
          {
      _context.UserRoles.Add(new UserRole
         {
      UserId = user.user_id,
           RoleId = groupId,
             AssignedAt = DateTime.UtcNow,
         AssignedByEmail = userEmail
                });
      successCount++;
            }
         }
      else
  {
      // Check if subuser
           var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
   if (subuser != null)
{
     if (!await _context.SubuserRoles.AnyAsync(sr => sr.SubuserId == subuser.subuser_id && sr.RoleId == groupId))
              {
     _context.SubuserRoles.Add(new SubuserRole
     {
         SubuserId = subuser.subuser_id,
 RoleId = groupId,
                AssignedAt = DateTime.UtcNow,
    AssignedByEmail = userEmail
  });
            successCount++;
       }
         }
       else
      {
     failedCount++;
 failedEmails.Add(email);
        }
         }
         }
     catch
         {
 failedCount++;
        failedEmails.Add(email);
        }
       }

   await _context.SaveChangesAsync();

    return Ok(new BulkGroupOperationResponseDto
      {
          Success = failedCount == 0,
  Message = $"Added {successCount} users to group. {failedCount} failed.",
  SuccessCount = successCount,
 FailedCount = failedCount,
  FailedEmails = failedEmails
        });
            }
  catch (Exception ex)
            {
       _logger.LogError(ex, "Error adding users to group {GroupId}", groupId);
      return StatusCode(500, new { message = "Error adding users to group", error = ex.Message });
         }
        }

    /// <summary>
        /// GET /api/GroupManagement/statistics - Get group statistics
        /// </summary>
     [HttpGet("statistics")]
        public async Task<ActionResult<GroupStatisticsDto>> GetGroupStatistics()
        {
            try
     {
 var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
         if (string.IsNullOrEmpty(userEmail))
         {
           return Unauthorized(new { message = "User not authenticated" });
             }

                var totalGroups = await _context.Roles.CountAsync();
  var totalUsers = await _context.UserRoles.Select(ur => ur.UserId).Distinct().CountAsync() +
         await _context.SubuserRoles.Select(sr => sr.SubuserId).Distinct().CountAsync();
         // Fixed: Use Machines instead of machines
 var totalLicenses = await _context.Machines.CountAsync(m => m.license_activated);

   var topGroups = await _context.Roles
        .OrderByDescending(r => _context.UserRoles.Count(ur => ur.RoleId == r.RoleId))
    .Take(5)
      .Select(r => new GroupStatsItemDto
           {
       GroupName = r.RoleName,
         UserCount = _context.UserRoles.Count(ur => ur.RoleId == r.RoleId) +
              _context.SubuserRoles.Count(sr => sr.RoleId == r.RoleId),
   LicenseCount = CalculateLicensesForGroup(r.RoleId)
})
.ToListAsync();

 return Ok(new GroupStatisticsDto
    {
       TotalGroups = totalGroups,
               TotalUsers = totalUsers,
           TotalLicenses = totalLicenses,
               AverageUsersPerGroup = totalGroups > 0 ? totalUsers / totalGroups : 0,
   TopGroups = topGroups
 });
            }
            catch (Exception ex)
            {
      _logger.LogError(ex, "Error getting group statistics");
         return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
}
        }

    #region Private Helper Methods

        private int CalculateLicensesForGroup(int roleId)
        {
       try
      {
    // Get all users in this role/group
     var userEmails = _context.UserRoles
          .Where(ur => ur.RoleId == roleId)
         .Include(ur => ur.User)
           .Select(ur => ur.User!.user_email)
   .ToList();

                var subuserEmails = _context.SubuserRoles
      .Where(sr => sr.RoleId == roleId)
      .Include(sr => sr.Subuser)
         .Select(sr => sr.Subuser!.subuser_email)
       .ToList();

   var allEmails = userEmails.Concat(subuserEmails).ToList();

         if (!allEmails.Any()) return 0;

    // Count machines/licenses for these users - Fixed: Use Machines instead of machines
         var licenseCount = _context.Machines
   .Count(m => allEmails.Contains(m.user_email) && m.license_activated);

    return licenseCount;
            }
          catch
       {
   return 0;
        }
   }

        private string FormatPermissionLabel(string permissionName)
 {
         // Convert "READ_ALL_USERS" to "Read All Users"
          return string.Join(" ", permissionName.Split('_')
   .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
  }

        #endregion
    }
}
