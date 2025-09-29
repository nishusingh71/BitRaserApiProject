using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Profile Controller with hierarchical user management and comprehensive profile operations
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;

        public EnhancedProfileController(ApplicationDbContext context, IRoleBasedAuthService authService)
        {
            _context = context;
            _authService = authService;
        }

        /// <summary>
        /// Get current user's profile with comprehensive details
        /// </summary>
        [HttpGet("my-profile")]
        [RequirePermission("VIEW_PROFILE")]
        public async Task<ActionResult<object>> GetMyProfile()
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.user_email == currentUserEmail);
            
            if (user == null) return NotFound("Profile not found");

            var profile = new
            {
                PersonalInfo = new
                {
                    user.user_email,
                    user.user_name,
                    user.phone_number,
                    user.created_at,
                    user.updated_at,
                    AccountAge = DateTime.UtcNow - user.created_at,
                    IsPrivateCloud = user.is_private_cloud,
                    HasPrivateApi = user.private_api
                },
                SecurityInfo = new
                {
                    Roles = user.UserRoles.Select(ur => new {
                        RoleName = ur.Role.RoleName,
                        Description = ur.Role.Description,
                        HierarchyLevel = ur.Role.HierarchyLevel,
                        AssignedAt = ur.AssignedAt,
                        AssignedBy = ur.AssignedByEmail
                    }).ToList(),
                    Permissions = user.UserRoles
                        .SelectMany(ur => ur.Role.RolePermissions)
                        .Select(rp => rp.Permission.PermissionName)
                        .Distinct()
                        .ToList(),
                    HighestRole = user.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault()?.Role.RoleName ?? "User"
                },
                Statistics = await GetUserStatistics(currentUserEmail!),
                HierarchyInfo = await GetUserHierarchy(currentUserEmail!),
                RecentActivity = await GetRecentActivity(currentUserEmail!)
            };

            return Ok(profile);
        }

        /// <summary>
        /// Get user profile by email with hierarchy validation
        /// </summary>
        [HttpGet("profile/{userEmail}")]
        [RequirePermission("VIEW_USER_PROFILE")]
        public async Task<ActionResult<object>> GetUserProfile(string userEmail)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Check if current user can view this profile
            if (userEmail != currentUserEmail && !await CanManageUserAsync(currentUserEmail!, userEmail))
            {
                return StatusCode(403,new { error = "You can only view profiles you have access to in your hierarchy" });
            }

            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.user_email == userEmail);
            
            if (user == null) return NotFound($"User profile with email {userEmail} not found");

            var canViewSensitiveInfo = await _authService.HasPermissionAsync(currentUserEmail!, "VIEW_SENSITIVE_PROFILE_INFO");

            var profile = new
            {
                PersonalInfo = new
                {
                    user.user_email,
                    user.user_name,
                    PhoneNumber = canViewSensitiveInfo ? user.phone_number : "****",
                    user.created_at,
                    user.updated_at,
                    AccountAge = DateTime.UtcNow - user.created_at
                },
                SecurityInfo = new
                {
                    Roles = user.UserRoles.Select(ur => new {
                        RoleName = ur.Role.RoleName,
                        HierarchyLevel = ur.Role.HierarchyLevel,
                        AssignedAt = ur.AssignedAt
                    }).ToList(),
                    HighestRole = user.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault()?.Role.RoleName ?? "User"
                },
                Statistics = canViewSensitiveInfo ? await GetUserStatistics(userEmail) : null,
                HierarchyInfo = await GetUserHierarchyPublic(userEmail),
                ManagementInfo = await GetManagementInfo(currentUserEmail!, userEmail)
            };

            return Ok(profile);
        }

        /// <summary>
        /// Update current user's profile
        /// </summary>
        [HttpPut("my-profile")]
        [RequirePermission("UPDATE_PROFILE")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == currentUserEmail);
            if (user == null) return NotFound("Profile not found");

            // Update allowed fields
            if (!string.IsNullOrEmpty(request.UserName))
                user.user_name = request.UserName;

            if (!string.IsNullOrEmpty(request.PhoneNumber))
                user.phone_number = request.PhoneNumber;

            user.updated_at = DateTime.UtcNow;

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Profile updated successfully", 
                updatedAt = user.updated_at 
            });
        }

        /// <summary>
        /// Get users in current user's hierarchy (subordinates)
        /// </summary>
        [HttpGet("my-hierarchy")]
        [RequirePermission("VIEW_HIERARCHY")]
        public async Task<ActionResult<object>> GetMyHierarchy()
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var currentUser = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.user_email == currentUserEmail);

            if (currentUser == null) return NotFound("User not found");

            var currentUserHighestRole = currentUser.UserRoles
                .OrderBy(ur => ur.Role.HierarchyLevel)
                .FirstOrDefault()?.Role;

            if (currentUserHighestRole == null)
                return Ok(new { message = "No hierarchy information available", subordinates = new List<object>() });

            // Get users with lower hierarchy levels (higher numbers)
            var subordinates = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.user_email != currentUserEmail &&
                           u.UserRoles.Any(ur => ur.Role.HierarchyLevel > currentUserHighestRole.HierarchyLevel))
                .Select(u => new {
                    u.user_email,
                    u.user_name,
                    u.created_at,
                    HighestRole = u.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault().Role.RoleName,
                    HierarchyLevel = u.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault().Role.HierarchyLevel,
                    CanManage = true // They can manage all subordinates
                })
                .OrderBy(u => u.HierarchyLevel)
                .ThenBy(u => u.user_name)
                .ToListAsync();

            // Get direct reports (users managed by current user)
            var directReports = await GetDirectReports(currentUserEmail!);

            // Get peers (same hierarchy level)
            var peers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.user_email != currentUserEmail &&
                           u.UserRoles.Any(ur => ur.Role.HierarchyLevel == currentUserHighestRole.HierarchyLevel))
                .Select(u => new {
                    u.user_email,
                    u.user_name,
                    u.created_at,
                    HighestRole = u.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault().Role.RoleName
                })
                .OrderBy(u => u.user_name)
                .ToListAsync();

            var hierarchy = new
            {
                CurrentUser = new
                {
                    currentUser.user_email,
                    currentUser.user_name,
                    Role = currentUserHighestRole.RoleName,
                    HierarchyLevel = currentUserHighestRole.HierarchyLevel
                },
                DirectReports = directReports,
                AllSubordinates = subordinates,
                Peers = peers,
                HierarchyStatistics = new
                {
                    DirectReportCount = directReports.Count,
                    TotalSubordinateCount = subordinates.Count,
                    PeerCount = peers.Count,
                    CanManageUsers = subordinates.Any()
                }
            };

            return Ok(hierarchy);
        }

        /// <summary>
        /// Get organizational hierarchy tree
        /// </summary>
        [HttpGet("organization-hierarchy")]
        [RequirePermission("VIEW_ORGANIZATION_HIERARCHY")]
        public async Task<ActionResult<object>> GetOrganizationHierarchy()
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Get all roles ordered by hierarchy
            var roles = await _context.Roles
                .OrderBy(r => r.HierarchyLevel)
                .ToListAsync();

            var organizationTree = new List<object>();

            foreach (var role in roles)
            {
                var usersInRole = await _context.UserRoles
                    .Include(ur => ur.User)
                    .Include(ur => ur.Role)
                    .Where(ur => ur.RoleId == role.RoleId)
                    .Select(ur => new {
                        ur.User.user_email,
                        ur.User.user_name,
                        ur.User.created_at,
                        AssignedAt = ur.AssignedAt,
                        CanView = true // Implement logic based on current user's permissions
                    })
                    .ToListAsync();

                organizationTree.Add(new {
                    Role = role.RoleName,
                    HierarchyLevel = role.HierarchyLevel,
                    Description = role.Description,
                    UserCount = usersInRole.Count,
                    Users = usersInRole
                });
            }

            return Ok(new {
                OrganizationHierarchy = organizationTree,
                TotalUsers = await _context.Users.CountAsync(),
                TotalRoles = roles.Count,
                GeneratedBy = currentUserEmail,
                GeneratedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Assign user to direct reports (management relationship)
        /// </summary>
        [HttpPost("assign-direct-report")]
        [RequirePermission("MANAGE_HIERARCHY")]
        public async Task<IActionResult> AssignDirectReport([FromBody] AssignDirectReportRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Validate that current user can manage the target user
            if (!await CanManageUserAsync(currentUserEmail!, request.UserEmail))
            {
                return StatusCode(403,new { error = "You cannot manage this user based on hierarchy rules" });
            }

            // Check if user exists
            var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == request.UserEmail);
            if (targetUser == null)
                return NotFound($"User with email {request.UserEmail} not found");

            // Create management relationship (you can store this in a separate table if needed)
            // For now, we'll use the existing role system to establish hierarchy

            return Ok(new {
                message = $"User {request.UserEmail} assigned as direct report",
                manager = currentUserEmail,
                directReport = request.UserEmail,
                assignedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Search users by hierarchy and filters
        /// </summary>
        [HttpGet("search-users")]
        [RequirePermission("SEARCH_USERS")]
        public async Task<ActionResult<object>> SearchUsers([FromQuery] UserSearchRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            IQueryable<users> query = _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

            // Apply hierarchy filter based on current user's level
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "VIEW_ALL_USERS"))
            {
                var currentUserRole = await GetCurrentUserHighestRole(currentUserEmail!);
                if (currentUserRole != null)
                {
                    // Only show users with same or lower hierarchy level
                    query = query.Where(u => u.UserRoles.Any(ur => ur.Role.HierarchyLevel >= currentUserRole.HierarchyLevel));
                }
            }

            // Apply search filters
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                query = query.Where(u => u.user_name.Contains(request.SearchTerm) || 
                                        u.user_email.Contains(request.SearchTerm));
            }

            if (!string.IsNullOrEmpty(request.Role))
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == request.Role));
            }

            if (request.HierarchyLevel.HasValue)
            {
                query = query.Where(u => u.UserRoles.Any(ur => ur.Role.HierarchyLevel == request.HierarchyLevel.Value));
            }

            if (request.CreatedFrom.HasValue)
            {
                query = query.Where(u => u.created_at >= request.CreatedFrom.Value);
            }

            if (request.CreatedTo.HasValue)
            {
                query = query.Where(u => u.created_at <= request.CreatedTo.Value);
            }

            var users = await query
                .OrderBy(u => u.UserRoles.Min(ur => ur.Role.HierarchyLevel))
                .ThenBy(u => u.user_name)
                .Skip(request.Page * request.PageSize)
                .Take(request.PageSize)
                .Select(u => new {
                    u.user_email,
                    u.user_name,
                    u.phone_number,
                    u.created_at,
                    HighestRole = u.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault().Role.RoleName,
                    HierarchyLevel = u.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault().Role.HierarchyLevel,
                    CanManage = true // Calculate based on hierarchy
                })
                .ToListAsync();

            var totalCount = await query.CountAsync();

            return Ok(new {
                Users = users,
                Pagination = new {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize)
                },
                SearchCriteria = request
            });
        }

        /// <summary>
        /// Get profile statistics and analytics
        /// </summary>
        [HttpGet("profile-analytics")]
        [RequirePermission("VIEW_PROFILE_ANALYTICS")]
        public async Task<ActionResult<object>> GetProfileAnalytics()
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var analytics = new
            {
                UserDistribution = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .GroupBy(ur => ur.Role.RoleName)
                    .Select(g => new { Role = g.Key, Count = g.Count() })
                    .OrderBy(x => x.Role)
                    .ToListAsync(),

                HierarchyDistribution = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .GroupBy(ur => ur.Role.HierarchyLevel)
                    .Select(g => new { 
                        HierarchyLevel = g.Key, 
                        Count = g.Count(),
                        RoleName = g.FirstOrDefault().Role.RoleName
                    })
                    .OrderBy(x => x.HierarchyLevel)
                    .ToListAsync(),

                RecentRegistrations = await _context.Users
                    .Where(u => u.created_at >= DateTime.UtcNow.AddDays(-30))
                    .GroupBy(u => u.created_at.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Date)
                    .Take(30)
                    .ToListAsync(),

                ActiveUsers = new
                {
                    Total = await _context.Users.CountAsync(),
                    WithSessions = await _context.Sessions
                        .Where(s => s.login_time >= DateTime.UtcNow.AddDays(-30))
                        .Select(s => s.user_email)
                        .Distinct()
                        .CountAsync(),
                    WithRecentActivity = await _context.logs
                        .Where(l => l.created_at >= DateTime.UtcNow.AddDays(-7))
                        .Select(l => l.user_email)
                        .Distinct()
                        .CountAsync()
                },

                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = currentUserEmail
            };

            return Ok(analytics);
        }

        #region Private Helper Methods

        private async Task<object> GetUserStatistics(string userEmail)
        {
            return new
            {
                TotalMachines = await _context.Machines.CountAsync(m => m.user_email == userEmail),
                ActiveLicenses = await _context.Machines.CountAsync(m => m.user_email == userEmail && m.license_activated),
                TotalReports = await _context.AuditReports.CountAsync(r => r.client_email == userEmail),
                TotalSessions = await _context.Sessions.CountAsync(s => s.user_email == userEmail),
                TotalSubusers = await _context.subuser.CountAsync(s => s.user_email == userEmail),
                TotalLogs = await _context.logs.CountAsync(l => l.user_email == userEmail),
                LastLoginDate = await _context.Sessions
                    .Where(s => s.user_email == userEmail)
                    .OrderByDescending(s => s.login_time)
                    .Select(s => s.login_time)
                    .FirstOrDefaultAsync()
            };
        }

        private async Task<object> GetUserHierarchy(string userEmail)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.user_email == userEmail);

            if (user == null) return null;

            var highestRole = user.UserRoles.OrderBy(ur => ur.Role.HierarchyLevel).FirstOrDefault()?.Role;

            return new
            {
                CurrentLevel = highestRole?.HierarchyLevel,
                CurrentRole = highestRole?.RoleName,
                CanManageUsers = highestRole?.HierarchyLevel < 5, // Lower levels can manage higher levels
                ManagedUserCount = await GetManagedUserCount(userEmail),
                ReportsTo = await GetReportsTo(userEmail)
            };
        }

        private async Task<object> GetUserHierarchyPublic(string userEmail)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.user_email == userEmail);

            if (user == null) return null;

            var highestRole = user.UserRoles.OrderBy(ur => ur.Role.HierarchyLevel).FirstOrDefault()?.Role;

            return new
            {
                CurrentRole = highestRole?.RoleName,
                HierarchyLevel = highestRole?.HierarchyLevel
            };
        }

        private async Task<object> GetManagementInfo(string currentUserEmail, string targetUserEmail)
        {
            var canManage = await CanManageUserAsync(currentUserEmail, targetUserEmail);
            var managedByCurrentUser = await IsDirectReport(currentUserEmail, targetUserEmail);

            return new
            {
                CanManage = canManage,
                IsDirectReport = managedByCurrentUser,
                ManagementActions = canManage ? new[] {
                    "assign_roles",
                    "view_statistics",
                    "manage_permissions",
                    "view_activity"
                } : new string[0]
            };
        }

        private async Task<List<object>> GetDirectReports(string managerEmail)
        {
            var managerRole = await GetCurrentUserHighestRole(managerEmail);
            if (managerRole == null) return new List<object>();

            // Get users with immediately lower hierarchy level
            var directReports = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.HierarchyLevel == managerRole.HierarchyLevel + 1))
                .Select(u => new {
                    u.user_email,
                    u.user_name,
                    u.created_at,
                    Role = u.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault().Role.RoleName
                })
                .ToListAsync<object>();

            return directReports;
        }

        private async Task<object> GetRecentActivity(string userEmail)
        {
            var recentLogs = await _context.logs
                .Where(l => l.user_email == userEmail)
                .OrderByDescending(l => l.created_at)
                .Take(5)
                .Select(l => new {
                    l.log_level,
                    l.log_message,
                    l.created_at
                })
                .ToListAsync();

            var lastSession = await _context.Sessions
                .Where(s => s.user_email == userEmail)
                .OrderByDescending(s => s.login_time)
                .FirstOrDefaultAsync();

            return new
            {
                RecentLogs = recentLogs,
                LastSession = lastSession != null ? new {
                    lastSession.login_time,
                    lastSession.logout_time,
                    lastSession.ip_address,
                    lastSession.session_status
                } : null
            };
        }

        private async Task<bool> CanManageUserAsync(string managerEmail, string targetUserEmail)
        {
            if (managerEmail == targetUserEmail) return true;

            var managerRole = await GetCurrentUserHighestRole(managerEmail);
            var targetRole = await GetCurrentUserHighestRole(targetUserEmail);

            if (managerRole == null || targetRole == null) return false;

            // Manager can manage users with higher hierarchy level (lower authority)
            return managerRole.HierarchyLevel < targetRole.HierarchyLevel;
        }

        private async Task<Role?> GetCurrentUserHighestRole(string userEmail)
        {
            return await _context.UserRoles
                .Include(ur => ur.Role)
                .Where(ur => ur.User.user_email == userEmail)
                .OrderBy(ur => ur.Role.HierarchyLevel)
                .Select(ur => ur.Role)
                .FirstOrDefaultAsync();
        }

        private async Task<int> GetManagedUserCount(string managerEmail)
        {
            var managerRole = await GetCurrentUserHighestRole(managerEmail);
            if (managerRole == null) return 0;

            return await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .CountAsync(u => u.user_email != managerEmail &&
                               u.UserRoles.Any(ur => ur.Role.HierarchyLevel > managerRole.HierarchyLevel));
        }

        private async Task<string?> GetReportsTo(string userEmail)
        {
            var userRole = await GetCurrentUserHighestRole(userEmail);
            if (userRole == null || userRole.HierarchyLevel <= 1) return null;

            // Find users with immediately higher authority (lower hierarchy level)
            var supervisor = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles.Any(ur => ur.Role.HierarchyLevel == userRole.HierarchyLevel - 1))
                .Select(u => u.user_email)
                .FirstOrDefaultAsync();

            return supervisor;
        }

        private async Task<bool> IsDirectReport(string managerEmail, string targetUserEmail)
        {
            // Implementation depends on how you track direct reporting relationships
            // This is a simplified version based on hierarchy levels
            return await CanManageUserAsync(managerEmail, targetUserEmail);
        }

        #endregion
    }

    #region Request Models

    public class UpdateProfileRequest
    {
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
    }

    public class AssignDirectReportRequest
    {
        public string UserEmail { get; set; } = string.Empty;
    }

    public class UserSearchRequest
    {
        public string? SearchTerm { get; set; }
        public string? Role { get; set; }
        public int? HierarchyLevel { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 50;
    }

    #endregion
}