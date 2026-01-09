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
        private readonly ICacheService _cacheService;

        public GroupManagementController(
            ApplicationDbContext context,
            IRoleBasedAuthService authService,
            IUserDataService userDataService,
            ILogger<GroupManagementController> logger,
            ICacheService cacheService)
        {
            _context = context;
            _authService = authService;
            _userDataService = userDataService;
            _logger = logger;
            _cacheService = cacheService;
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
                var query = _context.Roles.AsNoTracking().AsQueryable();  // ✅ RENDER OPTIMIZATION

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
                        LicenseCount = 0, // Calculated client-side below
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

                // Fix: Calculate licenses client-side to avoid memory leak in LINQ projection
                foreach (var group in groups)
                {
                    group.LicenseCount = CalculateLicensesForGroup(group.GroupId);
                }

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
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
                    .Include(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .AsSplitQuery()  // ✅ Prevent cartesian explosion
                    .Where(r => r.RoleId == groupId).FirstOrDefaultAsync();

                if (group == null)
                {
                    return NotFound(new { message = "Group not found" });
                }

                var userCount = await _context.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == groupId) +
                                await _context.SubuserRoles.AsNoTracking().CountAsync(sr => sr.RoleId == groupId);
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

                // Assign permissions (in-memory before save for atomic transaction)
                if (request.Permissions != null && request.Permissions.Any())
                {
                    // Dedup permissions to avoid Unique Constraint violations
                    var distinctPermissions = request.Permissions.Distinct().ToList();

                    foreach (var permName in distinctPermissions)
                    {
                        var permission = await _context.Permissions
                            .Where(p => p.PermissionName == permName).FirstOrDefaultAsync();

                        if (permission != null)
                        {
                            newGroup.RolePermissions!.Add(new RolePermission
                            {
                                PermissionId = permission.PermissionId
                            });
                        }
                    }
                }

                _context.Roles.Add(newGroup);
                
                try 
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "Database error creating group: {Message} | Inner: {Inner}", dbEx.Message, dbEx.InnerException?.Message);
                    // Return detailed error for debugging
                    return StatusCode(500, new { message = "Error saving group to database", error = dbEx.InnerException?.Message ?? dbEx.Message });
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
                    var distinctPermissions = request.Permissions.Distinct().ToList();
                    
                    foreach (var permName in distinctPermissions)
                    {
                        var permission = await _context.Permissions
                            .Where(p => p.PermissionName == permName).FirstOrDefaultAsync();

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
                var userCount = await _context.UserRoles.AsNoTracking().CountAsync(ur => ur.RoleId == groupId) +
                                await _context.SubuserRoles.AsNoTracking().CountAsync(sr => sr.RoleId == groupId);
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
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
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
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
                    .Where(ur => ur.RoleId == groupId)
                    .Include(ur => ur.User)
                    .AsSplitQuery()  // ✅ Prevent cartesian explosion
                    .Select(ur => new GroupMemberItemDto
                    {
                        UserEmail = ur.User!.user_email,
                        UserName = ur.User.user_name,
                        JoinedDate = ur.AssignedAt,
                        Status = ur.User.status ?? "active",
                        UserType = "user",
                        Department = ur.User.department ?? "General",
                        Profile = ur.User.user_role ?? "Member"
                    })
                    .ToListAsync();

                // Get subusers in this group
                var subusers = await _context.SubuserRoles
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
                    .Where(sr => sr.RoleId == groupId)
                    .Include(sr => sr.Subuser)
                    .AsSplitQuery()  // ✅ Prevent cartesian explosion
                    .Select(sr => new GroupMemberItemDto
                    {
                        UserEmail = sr.Subuser!.subuser_email,
                        UserName = sr.Subuser.Name ?? sr.Subuser.subuser_email,
                        JoinedDate = sr.AssignedAt,
                        Status = sr.Subuser.status ?? "active",
                        UserType = "subuser",
                        Department = sr.Subuser.Department ?? "General",
                        Profile = sr.Subuser.Role ?? "Subuser"
                    })
                    .ToListAsync();

                var allMembers = mainUsers.Concat(subusers).OrderBy(m => m.UserName).ToList();
                var memberEmails = allMembers.Select(m => m.UserEmail).Distinct().ToList();

                // Efficiently fetch License Types from Machines
                // We take the "highest" available license for each user if they have multiple
                var userLicenses = await _context.Machines
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
                    .Where(m => memberEmails.Contains(m.user_email) && m.license_activated)
                    .Select(m => new { m.user_email, m.license_details_json }) 
                    .ToListAsync();

                // Map license info
                 foreach (var member in allMembers)
                {
                    // Simple logic: if they have any active machine, call it "Pro", explicitly check JSON if needed
                    // For now, if they have an active machine, we assign "Professional", else "Standard"
                    // If you have a specific column for 'Edition', use that. Based on models, we rely on details or existence.
                    var hasMachine = userLicenses.Any(ul => ul.user_email == member.UserEmail);
                    member.LicenseType = hasMachine ? "Professional" : "Standard";
                    
                    // If we had an Edition field in Machines table (which we saw in License model but not Machines), we would use it.
                    // Assuming 'Enterprise' for admins/special roles if needed.
                    if (member.UserType == "user" && (member.Profile?.Contains("Admin") == true || member.Profile?.Contains("Manager") == true))
                    {
                         member.LicenseType = "Enterprise";
                    }
                }

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
                        var user = await _context.Users.Where(u => u.user_email == email).FirstOrDefaultAsync();
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
                            var subuser = await _context.subuser.Where(s => s.subuser_email == email).FirstOrDefaultAsync();
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

                var totalGroups = await _context.Roles.AsNoTracking().CountAsync();
                var totalUsers = await _context.UserRoles.AsNoTracking().Select(ur => ur.UserId).Distinct().CountAsync() +
                                 await _context.SubuserRoles.AsNoTracking().Select(sr => sr.SubuserId).Distinct().CountAsync();
                // Fixed: Use Machines instead of machines
                var totalLicenses = await _context.Machines.AsNoTracking().CountAsync(m => m.license_activated);

                var topGroups = await _context.Roles
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
                    .OrderByDescending(r => _context.UserRoles.Count(ur => ur.RoleId == r.RoleId))
                    .Take(5)
                    .Select(r => new GroupStatsItemDto
                    {
                        GroupName = r.RoleName,
                        UserCount = _context.UserRoles.Count(ur => ur.RoleId == r.RoleId) +
                                    _context.SubuserRoles.Count(sr => sr.RoleId == r.RoleId),
                        LicenseCount = 0 // Calculated client-side below
                    })
                    .ToListAsync();

                // Fix: Calculate licenses client-side
                foreach (var group in topGroups)
                {
                    group.LicenseCount = CalculateLicensesForGroup(await _context.Roles.AsNoTracking().Where(r => r.RoleName == group.GroupName).Select(r => r.RoleId).FirstOrDefaultAsync());
                }

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

        #region Group Admin Resource Management

        /// <summary>
        /// GET /api/GroupManagement/{groupId}/resources
        /// Get all group members' resources overview (licenses, machines, reports)
        /// Only accessible by Group Admin
        /// </summary>
        [HttpGet("{groupId}/resources")]
        public async Task<IActionResult> GetGroupResources(int groupId)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                // Check if user is group admin
                if (!await IsGroupAdminAsync(groupId, currentUserEmail))
                    return StatusCode(403, new { success = false, message = "Only Group Admin can view group resources" });

                var group = await _context.Groups
                    .Include(g => g.GroupMembers)
                    .FirstOrDefaultAsync(g => g.group_id == groupId);

                if (group == null)
                    return NotFound(new { success = false, message = "Group not found" });

                // Get all member emails
                var memberEmails = group.GroupMembers?.Select(m => m.UserEmail).Where(e => e != null).ToList() ?? new List<string?>();

                // Get resource counts for each member
                var memberResources = new List<object>();
                foreach (var email in memberEmails.Where(e => !string.IsNullOrEmpty(e)))
                {
                    var licenseCount = await _context.Machines
                        .CountAsync(m => m.user_email == email && m.license_activated);
                    var machineCount = await _context.Machines
                        .CountAsync(m => m.user_email == email);
                    var reportCount = await _context.AuditReports
                        .CountAsync(r => r.client_email == email);
                    var subuserCount = await _context.Set<subuser>()
                        .CountAsync(s => s.user_email == email);

                    var member = group.GroupMembers?.FirstOrDefault(m => m.UserEmail == email);
                    memberResources.Add(new
                    {
                        email,
                        name = member?.UserName ?? email,
                        role = member?.Role ?? "member",
                        department = member?.Department,
                        licenses = licenseCount,
                        machines = machineCount,
                        reports = reportCount,
                        subusers = subuserCount
                    });
                }

                var totalLicenses = group.license_allocation;
                var usedLicenses = memberResources.Sum(m => (int)((dynamic)m).licenses);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        groupId = group.group_id,
                        groupName = group.name,
                        totalLicenses,
                        usedLicenses,
                        availableLicenses = totalLicenses - usedLicenses,
                        memberCount = memberEmails.Count,
                        members = memberResources
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting group resources for group {GroupId}", groupId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/GroupManagement/{groupId}/members/{email}/resources
        /// Get specific member's detailed resources
        /// </summary>
        [HttpGet("{groupId}/members/{email}/resources")]
        public async Task<IActionResult> GetMemberResources(int groupId, string email)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!await IsGroupAdminAsync(groupId, currentUserEmail!))
                    return StatusCode(403, new { success = false, message = "Only Group Admin can view member resources" });

                var decodedEmail = DecodeEmail(email);

                // Get member's machines
                var machines = await _context.Machines
                    .Where(m => m.user_email == decodedEmail)
                    .Select(m => new
                    {
                        id = m.fingerprint_hash,
                        macAddress = m.mac_address,
                        hostname = m.os_version,
                        licenseActivated = m.license_activated,
                        createdAt = m.created_at
                    })
                    .ToListAsync();

                // Get member's reports count
                var reportCount = await _context.AuditReports
                    .CountAsync(r => r.client_email == decodedEmail);

                // Get member's subusers
                var subusers = await _context.Set<subuser>()
                    .Where(s => s.user_email == decodedEmail)
                    .Select(s => new
                    {
                        email = s.subuser_email,
                        name = s.Name,
                        role = s.Role,
                        status = s.status
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        email = decodedEmail,
                        machines,
                        machineCount = machines.Count,
                        activeLicenses = machines.Count(m => m.licenseActivated),
                        reportCount,
                        subusers,
                        subuserCount = subusers.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting member resources for {Email}", email);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/GroupManagement/{groupId}/transfer-license
        /// Transfer licenses from one group member to another
        /// </summary>
        [HttpPost("{groupId}/transfer-license")]
        public async Task<IActionResult> TransferLicense(int groupId, [FromBody] TransferLicenseRequestDto request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!await IsGroupAdminAsync(groupId, currentUserEmail!))
                    return StatusCode(403, new { success = false, message = "Only Group Admin can transfer licenses" });

                // Verify both users are in the group
                var group = await _context.Groups
                    .Include(g => g.GroupMembers)
                    .FirstOrDefaultAsync(g => g.group_id == groupId);

                if (group == null)
                    return NotFound(new { success = false, message = "Group not found" });

                var memberEmails = group.GroupMembers?.Select(m => m.UserEmail?.ToLower()).ToList() ?? new List<string?>();
                if (!memberEmails.Contains(request.FromEmail?.ToLower()) || !memberEmails.Contains(request.ToEmail?.ToLower()))
                    return BadRequest(new { success = false, message = "Both users must be group members" });

                // Get machines to transfer
                var machinesToTransfer = await _context.Machines
                    .Where(m => m.user_email == request.FromEmail && m.license_activated)
                    .Take(request.LicenseCount)
                    .ToListAsync();

                if (machinesToTransfer.Count < request.LicenseCount)
                    return BadRequest(new { success = false, message = $"Source user only has {machinesToTransfer.Count} active licenses" });

                // Transfer ownership
                foreach (var machine in machinesToTransfer)
                {
                    machine.user_email = request.ToEmail;
                    machine.updated_at = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Transferred {Count} licenses from {From} to {To} in group {GroupId}",
                    request.LicenseCount, request.FromEmail, request.ToEmail, groupId);

                return Ok(new
                {
                    success = true,
                    message = $"Transferred {request.LicenseCount} licenses successfully",
                    data = new
                    {
                        transferred = machinesToTransfer.Count,
                        fromEmail = request.FromEmail,
                        toEmail = request.ToEmail
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error transferring licenses in group {GroupId}", groupId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/GroupManagement/{groupId}/distribute-resources
        /// Distribute a leaving user's resources to other group members
        /// </summary>
        [HttpPost("{groupId}/distribute-resources")]
        public async Task<IActionResult> DistributeResources(int groupId, [FromBody] DistributeResourcesRequestDto request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!await IsGroupAdminAsync(groupId, currentUserEmail!))
                    return StatusCode(403, new { success = false, message = "Only Group Admin can distribute resources" });

                var group = await _context.Groups
                    .Include(g => g.GroupMembers)
                    .FirstOrDefaultAsync(g => g.group_id == groupId);

                if (group == null)
                    return NotFound(new { success = false, message = "Group not found" });

                var results = new List<object>();

                foreach (var distribution in request.Distribution)
                {
                    // Transfer machines
                    if (distribution.MachineIds?.Any() == true)
                    {
                        var machines = await _context.Machines
                            .Where(m => distribution.MachineIds.Contains(m.fingerprint_hash) && 
                                        m.user_email == request.LeavingUserEmail)
                            .ToListAsync();

                        foreach (var machine in machines)
                        {
                            machine.user_email = distribution.ToEmail;
                            machine.updated_at = DateTime.UtcNow;
                        }

                        results.Add(new { toEmail = distribution.ToEmail, machines = machines.Count });
                    }

                    // Transfer reports
                    if (distribution.TransferReports)
                    {
                        var reports = await _context.AuditReports
                            .Where(r => r.client_email == request.LeavingUserEmail)
                            .ToListAsync();

                        foreach (var report in reports)
                        {
                            report.client_email = distribution.ToEmail;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Distributed resources from {LeavingUser} in group {GroupId}",
                    request.LeavingUserEmail, groupId);

                return Ok(new
                {
                    success = true,
                    message = "Resources distributed successfully",
                    data = new
                    {
                        leavingUser = request.LeavingUserEmail,
                        distributions = results
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error distributing resources in group {GroupId}", groupId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST /api/GroupManagement/{groupId}/members/{email}/permissions
        /// Update member's permissions (toggle-based)
        /// </summary>
        [HttpPost("{groupId}/members/{email}/permissions")]
        public async Task<IActionResult> UpdateMemberPermissions(int groupId, string email, [FromBody] UpdateMemberPermissionsRequestDto request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!await IsGroupAdminAsync(groupId, currentUserEmail!))
                    return StatusCode(403, new { success = false, message = "Only Group Admin can update permissions" });

                var decodedEmail = DecodeEmail(email);

                // Check if member is a subuser
                var subuser = await _context.Set<subuser>()
                    .FirstOrDefaultAsync(s => s.subuser_email == decodedEmail);

                if (subuser == null)
                {
                    // Check if main user exists
                    var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == decodedEmail);
                    if (user == null)
                        return NotFound(new { success = false, message = "User not found" });
                }

                // Get or create role for this user (via subuser's ID)
                SubuserRole? existingRole = null;
                if (subuser != null)
                {
                    existingRole = await _context.SubuserRoles
                        .FirstOrDefaultAsync(sr => sr.SubuserId == subuser.subuser_id);
                }

                foreach (var perm in request.Permissions)
                {
                    var permission = await _context.Permissions
                        .FirstOrDefaultAsync(p => p.PermissionName == perm.Key);

                    if (permission == null) continue;

                    if (perm.Value)
                    {
                        // Grant permission - find or create role permission
                        if (existingRole != null)
                        {
                            var rolePermExists = await _context.RolePermissions
                                .AnyAsync(rp => rp.RoleId == existingRole.RoleId && rp.PermissionId == permission.PermissionId);

                            if (!rolePermExists)
                            {
                                _context.RolePermissions.Add(new RolePermission
                                {
                                    RoleId = existingRole.RoleId,
                                    PermissionId = permission.PermissionId
                                });
                            }
                        }
                    }
                    else
                    {
                        // Revoke permission
                        if (existingRole != null)
                        {
                            var rolePermToRemove = await _context.RolePermissions
                                .FirstOrDefaultAsync(rp => rp.RoleId == existingRole.RoleId && rp.PermissionId == permission.PermissionId);

                            if (rolePermToRemove != null)
                            {
                                _context.RolePermissions.Remove(rolePermToRemove);
                            }
                        }
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Updated permissions for {Email} in group {GroupId}", decodedEmail, groupId);

                return Ok(new
                {
                    success = true,
                    message = "Permissions updated successfully",
                    data = new
                    {
                        email = decodedEmail,
                        updatedPermissions = request.Permissions
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating permissions for {Email}", email);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// DELETE /api/GroupManagement/{groupId}/members/{email}/remove-with-transfer
        /// Remove member from group and transfer all their resources to another member
        /// </summary>
        [HttpDelete("{groupId}/members/{email}/remove-with-transfer")]
        public async Task<IActionResult> RemoveMemberWithTransfer(int groupId, string email, [FromQuery] string transferTo)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!await IsGroupAdminAsync(groupId, currentUserEmail!))
                    return StatusCode(403, new { success = false, message = "Only Group Admin can remove members" });

                var decodedEmail = DecodeEmail(email);
                var decodedTransferTo = DecodeEmail(transferTo);

                var group = await _context.Groups
                    .Include(g => g.GroupMembers)
                    .FirstOrDefaultAsync(g => g.group_id == groupId);

                if (group == null)
                    return NotFound(new { success = false, message = "Group not found" });

                // Verify target user is in group
                var targetMember = group.GroupMembers?.FirstOrDefault(m => m.UserEmail?.ToLower() == decodedTransferTo.ToLower());
                if (targetMember == null)
                    return BadRequest(new { success = false, message = "Transfer target must be a group member" });

                // Transfer all machines
                var machines = await _context.Machines
                    .Where(m => m.user_email == decodedEmail)
                    .ToListAsync();

                foreach (var machine in machines)
                {
                    machine.user_email = decodedTransferTo;
                    machine.updated_at = DateTime.UtcNow;
                }

                // Transfer all reports
                var reports = await _context.AuditReports
                    .Where(r => r.client_email == decodedEmail)
                    .ToListAsync();

                foreach (var report in reports)
                {
                    report.client_email = decodedTransferTo;
                }

                // Remove member from group
                var memberToRemove = group.GroupMembers?.FirstOrDefault(m => m.UserEmail?.ToLower() == decodedEmail.ToLower());
                if (memberToRemove != null)
                {
                    _context.GroupMembers.Remove(memberToRemove);
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Removed {Email} from group {GroupId}, transferred {Machines} machines and {Reports} reports to {TransferTo}",
                    decodedEmail, groupId, machines.Count, reports.Count, decodedTransferTo);

                return Ok(new
                {
                    success = true,
                    message = "Member removed and resources transferred",
                    data = new
                    {
                        removedMember = decodedEmail,
                        transferredTo = decodedTransferTo,
                        machinesTransferred = machines.Count,
                        reportsTransferred = reports.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error removing member {Email} from group {GroupId}", email, groupId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Check if current user is admin of the specified group
        /// </summary>
        private async Task<bool> IsGroupAdminAsync(int groupId, string userEmail)
        {
            // SuperAdmin can do anything
            var isSubuser = await _context.Set<subuser>().AnyAsync(s => s.subuser_email == userEmail);
            if (await _authService.HasPermissionAsync(userEmail, "MANAGE_ALL_GROUPS", isSubuser))
                return true;

            // Check if user is group admin
            var group = await _context.Groups
                .Include(g => g.GroupMembers)
                .FirstOrDefaultAsync(g => g.group_id == groupId);

            if (group == null) return false;

            // Check admin_user_id
            if (group.admin_user_id == userEmail)
                return true;

            // Check GroupMember role
            var member = group.GroupMembers?.FirstOrDefault(m => m.UserEmail?.ToLower() == userEmail.ToLower());
            return member?.Role?.ToLower() == "admin";
        }

        private static string DecodeEmail(string base64Email)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64Email);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return base64Email;
            }
        }

        #endregion
    }

    #region Group Admin DTOs

    public class TransferLicenseRequestDto
    {
        public string FromEmail { get; set; } = string.Empty;
        public string ToEmail { get; set; } = string.Empty;
        public int LicenseCount { get; set; }
    }

    public class DistributeResourcesRequestDto
    {
        public string LeavingUserEmail { get; set; } = string.Empty;
        public List<ResourceDistributionDto> Distribution { get; set; } = new();
    }

    public class ResourceDistributionDto
    {
        public string ToEmail { get; set; } = string.Empty;
        public List<string>? MachineIds { get; set; }
        public bool TransferReports { get; set; } = false;
    }

    public class UpdateMemberPermissionsRequestDto
    {
        public Dictionary<string, bool> Permissions { get; set; } = new();
    }

    #endregion
}
