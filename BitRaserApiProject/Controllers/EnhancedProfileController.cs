using System.Security.Claims;
using DSecureApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureApi.Services;
using DSecureApi.Attributes;

namespace DSecureApi.Controllers
{
    /// <summary>
    /// Enhanced Profile Controller with hierarchical user management and comprehensive profile operations
    /// Supports both users and subusers with appropriate access levels
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ICacheService _cacheService;

        public EnhancedProfileController(ApplicationDbContext context, IRoleBasedAuthService authService, IUserDataService userDataService, ICacheService cacheService)
        {
            _context = context;
            _authService = authService;
            _userDataService = userDataService;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Get current user's profile with comprehensive details (supports both users and subusers)
        /// </summary>
        [HttpGet("my-profile")]
        public async Task<ActionResult<object>> GetMyProfile()
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            if (isCurrentUserSubuser)
            {
                return await GetSubuserProfile(currentUserEmail!);
            }
            else
            {
                return await GetUserProfile(currentUserEmail!);
            }
        }

        /// <summary>
        /// Get user or subuser profile by email with hierarchy validation
        /// </summary>
        [HttpGet("profile/{userEmail}")]
        public async Task<ActionResult<object>> GetUserProfileByEmail(string userEmail)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            var isTargetSubuser = await _userDataService.SubuserExistsAsync(userEmail);
            
            // Check if current user can view this profile
            bool canView = userEmail == currentUserEmail ||
                          await _authService.HasPermissionAsync(currentUserEmail!, "VIEW_USER_PROFILE", isCurrentUserSubuser) ||
                          await CanManageUserAsync(currentUserEmail!, userEmail);

            if (!canView)
            {
                return StatusCode(403, new { error = "You can only view profiles you have access to in your hierarchy" });
            }

            if (isTargetSubuser)
            {
                return await GetSubuserProfile(userEmail);
            }
            else
            {
                return await GetUserProfile(userEmail);
            }
        }

        /// <summary>
        /// Update current user's profile (supports both users and subusers)
        /// </summary>
        [HttpPut("my-profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            if (isCurrentUserSubuser)
            {
                return await UpdateSubuserProfile(currentUserEmail!, request);
            }
            else
            {
                return await UpdateUserProfile(currentUserEmail!, request);
            }
        }

        /// <summary>
        /// Get users in current user's hierarchy (subordinates) - includes subusers
        /// </summary>
        [HttpGet("my-hierarchy")]
        public async Task<ActionResult<object>> GetMyHierarchy()
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            if (isCurrentUserSubuser)
            {
                // Subusers cannot manage hierarchy
                return StatusCode(403, new { error = "Subusers do not have hierarchy management access" });
            }

            var currentUser = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.user_email == currentUserEmail).FirstOrDefaultAsync();

            if (currentUser == null) return NotFound("User not found");

            var currentUserHighestRole = currentUser.UserRoles
                .OrderBy(ur => ur.Role.HierarchyLevel)
                .FirstOrDefault()?.Role;

            if (currentUserHighestRole == null)
                return Ok(new { message = "No hierarchy information available", subordinates = new List<object>() });

            // Get subordinate users
            var subordinateUsers = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.user_email != currentUserEmail &&
                           u.UserRoles.Any(ur => ur.Role.HierarchyLevel > currentUserHighestRole.HierarchyLevel))
                .Select(u => new {
                    u.user_email,
                    u.user_name,
                    u.created_at,
                    UserType = "User",
                    HighestRole = u.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault().Role.RoleName,
                    HierarchyLevel = u.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault().Role.HierarchyLevel,
                    CanManage = true
                })
                .OrderBy(u => u.HierarchyLevel)
                .ThenBy(u => u.user_name)
                .ToListAsync();

            // Get subusers managed by current user
            var managedSubusers = await _context.subuser
                .Include(s => s.SubuserRoles)
                .ThenInclude(sr => sr.Role)
                .Where(s => s.user_email == currentUserEmail)
                .Select(s => new {
                    user_email = s.subuser_email,
                    user_name = $"Subuser: {s.subuser_email}",
                    created_at = DateTime.MinValue, // Subuser table doesn't have created_at
                    UserType = "Subuser",
                    HighestRole = s.SubuserRoles
                        .OrderBy(sr => sr.Role.HierarchyLevel)
                        .FirstOrDefault() != null 
                            ? s.SubuserRoles.OrderBy(sr => sr.Role.HierarchyLevel).FirstOrDefault().Role.RoleName 
                            : "SubUser",
                    HierarchyLevel = s.SubuserRoles
                        .OrderBy(sr => sr.Role.HierarchyLevel)
                        .FirstOrDefault() != null 
                            ? s.SubuserRoles.OrderBy(sr => sr.Role.HierarchyLevel).FirstOrDefault().Role.HierarchyLevel 
                            : 10, // Default low priority
                    CanManage = true
                })
                .ToListAsync();

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
                    UserType = "User",
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
                    UserType = "User",
                    Role = currentUserHighestRole.RoleName,
                    HierarchyLevel = currentUserHighestRole.HierarchyLevel
                },
                DirectReports = subordinateUsers.Where(u => u.HierarchyLevel == currentUserHighestRole.HierarchyLevel + 1).ToList(),
                ManagedSubusers = managedSubusers,
                AllSubordinates = subordinateUsers,
                Peers = peers,
                HierarchyStatistics = new
                {
                    DirectReportCount = subordinateUsers.Count(u => u.HierarchyLevel == currentUserHighestRole.HierarchyLevel + 1),
                    TotalSubordinateCount = subordinateUsers.Count,
                    ManagedSubuserCount = managedSubusers.Count,
                    PeerCount = peers.Count,
                    CanManageUsers = subordinateUsers.Any() || managedSubusers.Any()
                }
            };

            return Ok(hierarchy);
        }

        /// <summary>
        /// Search users and subusers by hierarchy and filters
        /// </summary>
        [HttpGet("search-users")]
        public async Task<ActionResult<object>> SearchUsers([FromQuery] UserSearchRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            if (isCurrentUserSubuser)
            {
                return StatusCode(403, new { error = "Subusers cannot search other users" });
            }

            var users = new List<object>();
            var subusers = new List<object>();

            // Search users
            IQueryable<users> userQuery = _context.Users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);

            // Apply hierarchy filter based on current user's level
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "VIEW_ALL_USERS", isCurrentUserSubuser))
            {
                var currentUserRole = await GetCurrentUserHighestRole(currentUserEmail!);
                if (currentUserRole != null)
                {
                    // Only show users with same or lower hierarchy level
                    userQuery = userQuery.Where(u => u.UserRoles.Any(ur => ur.Role.HierarchyLevel >= currentUserRole.HierarchyLevel));
                }
            }

            // Apply search filters for users
            if (!string.IsNullOrEmpty(request.SearchTerm))
            {
                userQuery = userQuery.Where(u => u.user_name.Contains(request.SearchTerm) || 
                                               u.user_email.Contains(request.SearchTerm));
            }

            if (!string.IsNullOrEmpty(request.Role))
            {
                userQuery = userQuery.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == request.Role));
            }

            if (request.HierarchyLevel.HasValue)
            {
                userQuery = userQuery.Where(u => u.UserRoles.Any(ur => ur.Role.HierarchyLevel == request.HierarchyLevel.Value));
            }

            if (request.CreatedFrom.HasValue)
            {
                userQuery = userQuery.Where(u => u.created_at >= request.CreatedFrom.Value);
            }

            if (request.CreatedTo.HasValue)
            {
                userQuery = userQuery.Where(u => u.created_at <= request.CreatedTo.Value);
            }

            users = await userQuery
                .OrderBy(u => u.UserRoles.Min(ur => ur.Role.HierarchyLevel))
                .ThenBy(u => u.user_name)
                .Skip(request.Page * request.PageSize / 2) // Split page size between users and subusers
                .Take(request.PageSize / 2)
                .Select(u => new {
                    u.user_email,
                    u.user_name,
                    u.phone_number,
                    u.created_at,
                    UserType = "User",
                    HighestRole = u.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault().Role.RoleName,
                    HierarchyLevel = u.UserRoles
                        .OrderBy(ur => ur.Role.HierarchyLevel)
                        .FirstOrDefault().Role.HierarchyLevel,
                    CanManage = true
                })
                .ToListAsync<object>();

            // Search subusers (only if user can manage subusers)
            if (await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SUBUSERS", isCurrentUserSubuser))
            {
                IQueryable<subuser> subuserQuery = _context.subuser.Include(s => s.SubuserRoles).ThenInclude(sr => sr.Role);

                // Apply search filters for subusers
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    subuserQuery = subuserQuery.Where(s => s.subuser_email.Contains(request.SearchTerm));
                }

                subusers = await subuserQuery
                    .OrderBy(s => s.subuser_email)
                    .Skip(request.Page * request.PageSize / 2)
                    .Take(request.PageSize / 2)
                    .Select(s => new {
                        user_email = s.subuser_email,
                        user_name = $"Subuser: {s.subuser_email}",
                        created_at = DateTime.MinValue,
                        UserType = "Subuser",
                        HighestRole = s.SubuserRoles
                            .OrderBy(sr => sr.Role.HierarchyLevel)
                            .Select(sr => sr.Role.RoleName)
                            .FirstOrDefault() ?? "SubUser",
                        HierarchyLevel = s.SubuserRoles
                            .OrderBy(sr => sr.Role.HierarchyLevel)
                            .Select(sr => sr.Role.HierarchyLevel)
                            .FirstOrDefault(),
                        CanManage = true,
                        ParentUser = s.user_email
                    })
                    .ToListAsync<object>();
            }

            var allResults = users.Concat(subusers).ToList();
            var totalCount = await userQuery.CountAsync() + (subusers.Any() ? await _context.subuser.CountAsync() : 0);

            return Ok(new {
                Users = allResults,
                Pagination = new {
                    Page = request.Page,
                    PageSize = request.PageSize,
                    TotalCount = totalCount,
                    TotalPages = (int)Math.Ceiling((double)totalCount / request.PageSize),
                    UserCount = users.Count,
                    SubuserCount = subusers.Count
                },
                SearchCriteria = request
            });
        }

        /// <summary>
        /// Get profile statistics and analytics (includes subuser data)
        /// </summary>
        [HttpGet("profile-analytics")]
        public async Task<ActionResult<object>> GetProfileAnalytics()
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (isCurrentUserSubuser || !await _authService.HasPermissionAsync(currentUserEmail!, "VIEW_PROFILE_ANALYTICS", isCurrentUserSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions to view profile analytics" });
            }

            var analytics = new
            {
                UserDistribution = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .GroupBy(ur => ur.Role.RoleName)
                    .Select(g => new { Role = g.Key, Count = g.Count(), UserType = "User" })
                    .OrderBy(x => x.Role)
                    .ToListAsync(),

                SubuserDistribution = await _context.SubuserRoles
                    .Include(sr => sr.Role)
                    .GroupBy(sr => sr.Role.RoleName)
                    .Select(g => new { Role = g.Key, Count = g.Count(), UserType = "Subuser" })
                    .OrderBy(x => x.Role)
                    .ToListAsync(),

                HierarchyDistribution = await _context.UserRoles
                    .Include(ur => ur.Role)
                    .GroupBy(ur => ur.Role.HierarchyLevel)
                    .Select(g => new { 
                        HierarchyLevel = g.Key, 
                        Count = g.Count(),
                        RoleName = g.FirstOrDefault().Role.RoleName,
                        UserType = "User"
                    })
                    .OrderBy(x => x.HierarchyLevel)
                    .ToListAsync(),

                RecentRegistrations = await _context.Users
                    .Where(u => u.created_at >= DateTime.UtcNow.AddDays(-30))
                    .GroupBy(u => u.created_at.Date)
                    .Select(g => new { Date = g.Key, Count = g.Count(), UserType = "User" })
                    .OrderByDescending(x => x.Date)
                    .Take(30)
                    .ToListAsync(),

                ActiveUsers = new
                {
                    TotalUsers = await _context.Users.CountAsync(),
                    TotalSubusers = await _context.subuser.CountAsync(),
                    UsersWithSessions = await _context.Sessions
                        .Where(s => s.login_time >= DateTime.UtcNow.AddDays(-30))
                        .Join(_context.Users, s => s.user_email, u => u.user_email, (s, u) => s.user_email)
                        .Distinct()
                        .CountAsync(),
                    SubusersWithSessions = await _context.Sessions
                        .Where(s => s.login_time >= DateTime.UtcNow.AddDays(-30))
                        .Join(_context.subuser, s => s.user_email, su => su.subuser_email, (s, su) => s.user_email)
                        .Distinct()
                        .CountAsync(),
                    UsersWithRecentActivity = await _context.logs
                        .Where(l => l.created_at >= DateTime.UtcNow.AddDays(-7))
                        .Join(_context.Users, l => l.user_email, u => u.user_email, (l, u) => l.user_email)
                        .Distinct()
                        .CountAsync(),
                    SubusersWithRecentActivity = await _context.logs
                        .Where(l => l.created_at >= DateTime.UtcNow.AddDays(-7))
                        .Join(_context.subuser, l => l.user_email, su => su.subuser_email, (l, su) => l.user_email)
                        .Distinct()
                        .CountAsync()
                },

                TopSubuserParents = await _context.subuser
                    .GroupBy(s => s.user_email)
                    .Select(g => new { ParentEmail = g.Key, SubuserCount = g.Count() })
                    .OrderByDescending(x => x.SubuserCount)
                    .Take(10)
                    .ToListAsync(),

                GeneratedAt = DateTime.UtcNow,
                GeneratedBy = currentUserEmail
            };

            return Ok(analytics);
        }

        #region Private Helper Methods

        private async Task<ActionResult<object>> GetUserProfile(string userEmail)
        {
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
            
            if (user == null) return NotFound("User profile not found");

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var canViewSensitiveInfo = userEmail == currentUserEmail || 
                                      await _authService.HasPermissionAsync(currentUserEmail!, "VIEW_SENSITIVE_PROFILE_INFO");

            var profile = new
            {
                UserType = "User",
                PersonalInfo = new
                {
                    user.user_email,
                    user.user_name,
                    PhoneNumber = canViewSensitiveInfo ? user.phone_number : "****",
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
                Statistics = canViewSensitiveInfo ? await GetUserStatistics(userEmail) : null,
                HierarchyInfo = await GetUserHierarchy(userEmail),
                RecentActivity = canViewSensitiveInfo ? await GetRecentActivity(userEmail) : null
            };

            return Ok(profile);
        }

        private async Task<ActionResult<object>> GetSubuserProfile(string subuserEmail)
        {
            var subuser = await _context.subuser
                .Include(s => s.SubuserRoles)
                .ThenInclude(sr => sr.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .Where(s => s.subuser_email == subuserEmail).FirstOrDefaultAsync();
            
            if (subuser == null) return NotFound("Subuser profile not found");

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var canViewSensitiveInfo = subuserEmail == currentUserEmail || 
                                      subuser.user_email == currentUserEmail ||
                                      await _authService.HasPermissionAsync(currentUserEmail!, "VIEW_SENSITIVE_PROFILE_INFO");

            var profile = new
            {
                UserType = "Subuser",
                PersonalInfo = new
                {
                    subuser_email = subuser.subuser_email,
                    parent_user_email = subuser.user_email,
                    subuser_id = subuser.subuser_id,
                    HasPassword = !string.IsNullOrEmpty(subuser.subuser_password),
                    created_at = DateTime.MinValue, // Subuser table doesn't have created_at
                    updated_at = DateTime.MinValue  // Subuser table doesn't have updated_at
                },
                SecurityInfo = new
                {
                    Roles = subuser.SubuserRoles.Select(sr => new {
                        RoleName = sr.Role.RoleName,
                        Description = sr.Role.Description,
                        HierarchyLevel = sr.Role.HierarchyLevel,
                        AssignedAt = sr.AssignedAt,
                        AssignedBy = sr.AssignedByEmail
                    }).ToList(),
                    Permissions = subuser.SubuserRoles
                        .SelectMany(sr => sr.Role.RolePermissions)
                        .Select(rp => rp.Permission.PermissionName)
                        .Distinct()
                        .ToList(),
                    HighestRole = subuser.SubuserRoles
                        .OrderBy(sr => sr.Role.HierarchyLevel)
                        .FirstOrDefault()?.Role.RoleName ?? "SubUser"
                },
                Statistics = canViewSensitiveInfo ? await GetSubuserStatistics(subuserEmail) : null,
                RecentActivity = canViewSensitiveInfo ? await GetRecentActivity(subuserEmail) : null
            };

            return Ok(profile);
        }

        private async Task<IActionResult> UpdateUserProfile(string userEmail, UpdateProfileRequest request)
        {
            var user = await _context.Users.Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
            if (user == null) return NotFound("User profile not found");

            // Update allowed fields
            if (!string.IsNullOrEmpty(request.UserName))
                user.user_name = request.UserName;

            if (!string.IsNullOrEmpty(request.PhoneNumber))
                user.phone_number = request.PhoneNumber;

            user.updated_at = DateTime.UtcNow;

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "User profile updated successfully", 
                userType = "User",
                updatedAt = user.updated_at 
            });
        }

        private async Task<IActionResult> UpdateSubuserProfile(string subuserEmail, UpdateProfileRequest request)
        {
            var subuser = await _context.subuser.Where(s => s.subuser_email == subuserEmail).FirstOrDefaultAsync();
            if (subuser == null) return NotFound("Subuser profile not found");

            // For subusers, we can only update limited information
            // Most profile updates should be done through the Enhanced Subuser Controller

            return Ok(new { 
                message = "Subuser profile information is limited. Use Enhanced Subuser Controller for full management.",
                userType = "Subuser",
                subuserEmail = subuserEmail,
                parentUserEmail = subuser.user_email,
                note = "Contact your parent user for profile updates or use /api/EnhancedSubuser endpoints"
            });
        }

        private async Task<object> GetSubuserStatistics(string subuserEmail)
        {
            return new
            {
                TotalMachines = await _context.Machines.CountAsync(m => m.subuser_email == subuserEmail),
                ActiveLicenses = await _context.Machines.CountAsync(m => m.subuser_email == subuserEmail && m.license_activated),
                TotalReports = await _context.AuditReports.CountAsync(r => r.client_email == subuserEmail),
                TotalSessions = await _context.Sessions.CountAsync(s => s.user_email == subuserEmail),
                TotalLogs = await _context.logs.CountAsync(l => l.user_email == subuserEmail),
                LastLoginDate = await _context.Sessions
                    .Where(s => s.user_email == subuserEmail)
                    .OrderByDescending(s => s.login_time)
                    .Select(s => s.login_time)
                    .FirstOrDefaultAsync()
            };
        }

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
            var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);
            
            if (isSubuser)
            {
                var subuser = await _context.subuser
                    .Include(s => s.SubuserRoles)
                    .ThenInclude(sr => sr.Role)
                    .Where(s => s.subuser_email == userEmail).FirstOrDefaultAsync();

                if (subuser == null) return null;

                var highestRole = subuser.SubuserRoles.OrderBy(sr => sr.Role.HierarchyLevel).FirstOrDefault()?.Role;

                return new
                {
                    UserType = "Subuser",
                    CurrentLevel = highestRole?.HierarchyLevel ?? 10,
                    CurrentRole = highestRole?.RoleName ?? "SubUser",
                    ParentUser = subuser.user_email,
                    CanManageUsers = false, // Subusers cannot manage other users
                    ManagedUserCount = 0,
                    ReportsTo = subuser.user_email
                };
            }
            else
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .Where(u => u.user_email == userEmail).FirstOrDefaultAsync();

                if (user == null) return null;

                var highestRole = user.UserRoles.OrderBy(ur => ur.Role.HierarchyLevel).FirstOrDefault()?.Role;

                return new
                {
                    UserType = "User",
                    CurrentLevel = highestRole?.HierarchyLevel,
                    CurrentRole = highestRole?.RoleName,
                    CanManageUsers = highestRole?.HierarchyLevel < 5, // Lower levels can manage higher levels
                    ManagedUserCount = await GetManagedUserCount(userEmail),
                    ManagedSubuserCount = await _context.subuser.CountAsync(s => s.user_email == userEmail),
                    ReportsTo = await GetReportsTo(userEmail)
                };
            }
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

            var isManagerSubuser = await _userDataService.SubuserExistsAsync(managerEmail);
            var isTargetSubuser = await _userDataService.SubuserExistsAsync(targetUserEmail);

            // Subusers cannot manage anyone
            if (isManagerSubuser) return false;

            // Users can manage their own subusers
            if (isTargetSubuser)
            {
                var subuser = await _context.subuser.Where(s => s.subuser_email == targetUserEmail).FirstOrDefaultAsync();
                return subuser?.user_email == managerEmail;
            }

            // Regular user hierarchy management
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

            var managerHierarchyLevel = managerRole.HierarchyLevel;

            return await _context.Users
                .Include(u => u.UserRoles!)
                .ThenInclude(ur => ur.Role)
                .CountAsync(u => u.user_email != managerEmail &&
                               u.UserRoles != null && u.UserRoles.Any(ur => ur.Role.HierarchyLevel > managerHierarchyLevel));
        }

        private async Task<string?> GetReportsTo(string userEmail)
        {
            var userRole = await GetCurrentUserHighestRole(userEmail);
            if (userRole == null || userRole.HierarchyLevel <= 1) return null;

            var targetHierarchyLevel = userRole.HierarchyLevel - 1;

            // Find users with immediately higher authority (lower hierarchy level)
            var supervisor = await _context.Users
                .Include(u => u.UserRoles!)
                .ThenInclude(ur => ur.Role)
                .Where(u => u.UserRoles != null && u.UserRoles.Any(ur => ur.Role.HierarchyLevel == targetHierarchyLevel))
                .Select(u => u.user_email)
                .FirstOrDefaultAsync();

            return supervisor;
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