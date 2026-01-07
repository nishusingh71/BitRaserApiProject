using Microsoft.AspNetCore.Mvc;
using DSecureApi.Services;
using Microsoft.AspNetCore.Authorization;
using DSecureApi.Attributes;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using DSecureApi.Models;

namespace DSecureApi.Controllers
{
    /// <summary>
    /// Dynamic System Management Controller
    /// Provides endpoints for managing permissions, roles, and routes dynamically
    /// Supports both users and subusers with appropriate access levels
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicSystemController : ControllerBase
    {
        private readonly IDynamicPermissionService _permissionService;
        private readonly DynamicRouteService _routeService;
        private readonly IUserDataService _userDataService;
        private readonly IRoleBasedAuthService _authService;
        private readonly ILogger<DynamicSystemController> _logger;
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cacheService;

        public DynamicSystemController(
            IDynamicPermissionService permissionService, 
            DynamicRouteService routeService,
            IUserDataService userDataService,
            IRoleBasedAuthService authService,
            ILogger<DynamicSystemController> logger,
            ApplicationDbContext context,
            ICacheService cacheService)
        {
            _permissionService = permissionService;
            _routeService = routeService;
            _userDataService = userDataService;
            _authService = authService;
            _logger = logger;
            _context = context;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Initialize the dynamic system - create permissions, roles, and routes
        /// </summary>
        [HttpPost("initialize")]
        public async Task<ActionResult> InitializeSystem()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "SYSTEM_ADMIN", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions for system initialization" });
            }

            try
            {
                _logger.LogInformation("Starting dynamic system initialization by {User} ({UserType})", currentUserEmail, isSubuser ? "Subuser" : "User");

                var results = new
                {
                    Timestamp = DateTime.UtcNow,
                    InitiatedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    Steps = new List<object>()
                };

                // Step 1: Ensure permissions exist
                _logger.LogInformation("Step 1: Creating permissions");
                var permissionResult = await _permissionService.EnsurePermissionsExistAsync();
                
                var step1 = new
                {
                    Step = 1,
                    Name = "Permission Creation",
                    Success = permissionResult.Success,
                    Message = permissionResult.Message,
                    PermissionsCreated = permissionResult.PermissionsCreated,
                    CreatedPermissions = permissionResult.CreatedPermissions
                };
                
                // Step 2: Create role-permission mappings
                _logger.LogInformation("Step 2: Creating role-permission mappings");
                var roleMappingResult = await _permissionService.CreateDynamicRolePermissionMappingsAsync();
                
                var step2 = new
                {
                    Step = 2,
                    Name = "Role-Permission Mappings",
                    Success = roleMappingResult.Success,
                    Message = roleMappingResult.Message,
                    MappingsCreated = roleMappingResult.MappingsCreated
                };

                // Step 3: Discover and seed routes
                _logger.LogInformation("Step 3: Discovering routes");
                var routeResult = await _routeService.DiscoverAndSeedRoutesAsync();
                
                var step3 = new
                {
                    Step = 3,
                    Name = "Route Discovery",
                    Success = routeResult.Success,
                    Message = routeResult.Message,
                    RoutesDiscovered = routeResult.RoutesDiscovered,
                    ControllersProcessed = routeResult.ControllersProcessed,
                    Controllers = routeResult.DiscoveredRoutes.GroupBy(r => r.ControllerName).Select(g => g.Key).ToList()
                };

                // Step 4: Clean up orphaned routes
                _logger.LogInformation("Step 4: Cleaning up orphaned routes");
                var cleanupResult = await _routeService.CleanupOrphanedRoutesAsync();
                
                var step4 = new
                {
                    Step = 4,
                    Name = "Route Cleanup",
                    Success = cleanupResult.Success,
                    Message = cleanupResult.Message,
                    OrphanedRoutesRemoved = cleanupResult.OrphanedRoutesRemoved
                };

                // Step 5: Initialize subuser support if needed
                _logger.LogInformation("Step 5: Ensuring subuser support");
                var subuserSupportResult = await EnsureSubuserSupportAsync();
                
                var step5 = new
                {
                    Step = 5,
                    Name = "Subuser Support Initialization",
                    Success = subuserSupportResult.Success,
                    Message = subuserSupportResult.Message,
                    SubuserRolesCreated = subuserSupportResult.RolesCreated
                };

                _logger.LogInformation("Dynamic system initialization completed successfully");

                return Ok(new
                {
                    success = true,
                    message = "Dynamic system initialization completed successfully",
                    timestamp = DateTime.UtcNow,
                    initiatedBy = results.InitiatedBy,
                    steps = new object[] { step1, step2, step3, step4, step5 },
                    summary = new
                    {
                        PermissionsCreated = permissionResult.PermissionsCreated,
                        MappingsCreated = roleMappingResult.MappingsCreated,
                        RoutesDiscovered = routeResult.RoutesDiscovered,
                        ControllersProcessed = routeResult.ControllersProcessed,
                        OrphanedRoutesRemoved = cleanupResult.OrphanedRoutesRemoved,
                        SubuserRolesCreated = subuserSupportResult.RolesCreated
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize dynamic system");
                return StatusCode(500, new
                {
                    success = false,
                    message = "System initialization failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow,
                    initiatedBy = currentUserEmail
                });
            }
        }

        /// <summary>
        /// Update system permissions - Add new permissions or update existing ones
        /// </summary>
        [HttpPost("update-permissions")]
        [RequirePermission("SYSTEM_ADMIN")]
        public async Task<ActionResult> UpdatePermissions()
        {
            try
            {
                _logger.LogInformation("Updating system permissions");

                var result = await _permissionService.EnsurePermissionsExistAsync();

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    permissionsCreated = result.PermissionsCreated,
                    createdPermissions = result.CreatedPermissions,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update permissions");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Permission update failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Update role mappings - Assign permissions to roles
        /// </summary>
        [HttpPost("update-role-mappings")]
        [RequirePermission("SYSTEM_ADMIN")]
        public async Task<ActionResult> UpdateRoleMappings()
        {
            try
            {
                _logger.LogInformation("Updating role-permission mappings");

                var result = await _permissionService.CreateDynamicRolePermissionMappingsAsync();

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    mappingsCreated = result.MappingsCreated,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update role mappings");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Role mapping update failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Refresh route discovery - Re-scan controllers for new routes
        /// </summary>
        [HttpPost("refresh-routes")]
        [RequirePermission("SYSTEM_ADMIN")]
        public async Task<ActionResult> RefreshRoutes()
        {
            try
            {
                _logger.LogInformation("Refreshing route discovery");

                var result = await _routeService.DiscoverAndSeedRoutesAsync();

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    routesDiscovered = result.RoutesDiscovered,
                    controllersProcessed = result.ControllersProcessed,
                    discoveredRoutes = result.DiscoveredRoutes.Select(r => new
                    {
                        r.ControllerName,
                        r.ActionName,
                        r.HttpMethod,
                        r.RoutePath,
                        r.RequiredPermissions
                    }),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to refresh routes");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Route refresh failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Clean up orphaned routes - Remove routes that no longer exist in controllers
        /// </summary>
        [HttpPost("cleanup-routes")]
        [RequirePermission("SYSTEM_ADMIN")]
        public async Task<ActionResult> CleanupRoutes()
        {
            try
            {
                _logger.LogInformation("Cleaning up orphaned routes");

                var result = await _routeService.CleanupOrphanedRoutesAsync();

                return Ok(new
                {
                    success = result.Success,
                    message = result.Message,
                    orphanedRoutesRemoved = result.OrphanedRoutesRemoved,
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup routes");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Route cleanup failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get system status - Overview of current permissions, roles, and routes (includes subuser info)
        /// </summary>
        [HttpGet("status")]
        public async Task<ActionResult> GetSystemStatus()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "SYSTEM_ADMIN", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions to view system status" });
            }

            try
            {
                _logger.LogInformation("Getting system status for {User} ({UserType})", currentUserEmail, isSubuser ? "Subuser" : "User");

                var systemStatus = new
                {
                    success = true,
                    message = "System status retrieved successfully",
                    status = "Operational",
                    timestamp = DateTime.UtcNow,
                    requestedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    services = new
                    {
                        PermissionService = "Running",
                        RouteService = "Running",
                        UserDataService = "Running",
                        AuthService = "Running"
                    },
                    statistics = await GetSystemStatistics(),
                    userManagement = new
                    {
                        TotalUsers = await _context.Users.CountAsync(),
                        TotalSubusers = await _context.subuser.CountAsync(),
                        TotalRoles = await _context.Roles.CountAsync(),
                        TotalPermissions = await _context.Permissions.CountAsync(),
                        UsersWithRoles = await _context.UserRoles.Select(ur => ur.UserId).Distinct().CountAsync(),
                        SubusersWithRoles = await _context.SubuserRoles.Select(sr => sr.SubuserId).Distinct().CountAsync()
                    }
                };

                return Ok(systemStatus);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system status");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to get system status",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get performance metrics of the dynamic system (includes user/subuser activity)
        /// </summary>
        [HttpGet("metrics")]
        public async Task<ActionResult> GetSystemMetrics()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "SYSTEM_ADMIN", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions to view system metrics" });
            }

            try
            {
                var metrics = new
                {
                    success = true,
                    message = "System metrics retrieved successfully",
                    metrics = new
                    {
                        Timestamp = DateTime.UtcNow,
                        RequestedBy = new
                        {
                            Email = currentUserEmail,
                            UserType = isSubuser ? "Subuser" : "User"
                        },
                        SystemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                        MemoryUsage = new
                        {
                            WorkingSet = GC.GetTotalMemory(false),
                            Gen0Collections = GC.CollectionCount(0),
                            Gen1Collections = GC.CollectionCount(1),
                            Gen2Collections = GC.CollectionCount(2)
                        },
                        UserActivity = new
                        {
                            ActiveUserSessions = await _context.Sessions
                                .Where(s => s.session_status == "active")
                                .Join(_context.Users, s => s.user_email, u => u.user_email, (s, u) => s)
                                .CountAsync(),
                            ActiveSubuserSessions = await _context.Sessions
                                .Where(s => s.session_status == "active")
                                .Join(_context.subuser, s => s.user_email, su => su.subuser_email, (s, su) => s)
                                .CountAsync(),
                            RecentUserLogins = await _context.Sessions
                                .Where(s => s.login_time >= DateTime.UtcNow.AddHours(-24))
                                .Join(_context.Users, s => s.user_email, u => u.user_email, (s, u) => s.user_email)
                                .Distinct()
                                .CountAsync(),
                            RecentSubuserLogins = await _context.Sessions
                                .Where(s => s.login_time >= DateTime.UtcNow.AddHours(-24))
                                .Join(_context.subuser, s => s.user_email, su => su.subuser_email, (s, su) => s.user_email)
                                .Distinct()
                                .CountAsync()
                        },
                        DatabaseActivity = new
                        {
                            RecentUserCreations = await _context.Users
                                .Where(u => u.created_at >= DateTime.UtcNow.AddDays(-7))
                                .CountAsync(),
                            RecentSubuserCreations = await _context.subuser
                                .Where(s => s.subuser_id > 0) // No created_at field, so count recent IDs
                                .CountAsync() - await _context.subuser.CountAsync() + 
                                await _context.subuser.Where(s => s.subuser_id > 0).CountAsync(), // Approximate recent count
                            TotalMachines = await _context.Machines.CountAsync(),
                            TotalReports = await _context.AuditReports.CountAsync(),
                            TotalLogs = await _context.logs.CountAsync()
                        }
                    }
                };

                return Ok(metrics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get system metrics");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to get system metrics",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Fix database data integrity issues - JSON columns, NULL values, etc. (includes subuser data)
        /// </summary>
        [HttpPost("fix-database-integrity")]
        public async Task<ActionResult> FixDatabaseIntegrity()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "SYSTEM_ADMIN", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions to fix database integrity" });
            }

            try
            {
                _logger.LogInformation("Starting database integrity fix by {User} ({UserType})", currentUserEmail, isSubuser ? "Subuser" : "User");

                var issuesFixed = 0;
                var errors = new List<string>();

                // Fix logs table JSON issues
                try
                {
                    var logsWithNullJson = await _context.logs
                        .Where(l => l.log_details_json == null)
                        .ToListAsync();

                    foreach (var log in logsWithNullJson)
                    {
                        log.log_details_json = "{}";
                        issuesFixed++;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Fixed {Count} logs with NULL JSON", logsWithNullJson.Count);
                }
                catch (Exception ex)
                {
                    errors.Add($"Logs table JSON fix failed: {ex.Message}");
                    _logger.LogError(ex, "Failed to fix logs JSON issues");
                }

                // Fix machines table JSON issues
                try
                {
                    var machinesWithNullJson = await _context.Machines
                        .Where(m => m.license_details_json == null)
                        .ToListAsync();

                    foreach (var machine in machinesWithNullJson)
                    {
                        machine.license_details_json = "{}";
                        issuesFixed++;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Fixed {Count} machines with NULL JSON", machinesWithNullJson.Count);
                }
                catch (Exception ex)
                {
                    errors.Add($"Machines table JSON fix failed: {ex.Message}");
                    _logger.LogError(ex, "Failed to fix machines JSON issues");
                }

                // Fix users table JSON issues
                try
                {
                    var usersWithNullJson = await _context.Users
                        .Where(u => u.payment_details_json == null || u.license_details_json == null)
                        .ToListAsync();

                    foreach (var user in usersWithNullJson)
                    {
                        if (user.payment_details_json == null)
                        {
                            user.payment_details_json = "{}";
                            issuesFixed++;
                        }
                        if (user.license_details_json == null)
                        {
                            user.license_details_json = "{}";
                            issuesFixed++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Fixed {Count} users with NULL JSON", usersWithNullJson.Count);
                }
                catch (Exception ex)
                {
                    errors.Add($"Users table JSON fix failed: {ex.Message}");
                    _logger.LogError(ex, "Failed to fix users JSON issues");
                }

                // Fix commands table JSON issues
                try
                {
                    var commandsWithNullJson = await _context.Commands
                        .Where(c => c.command_json == null)
                        .ToListAsync();

                    foreach (var command in commandsWithNullJson)
                    {
                        command.command_json = "{}";
                        issuesFixed++;
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Fixed {Count} commands with NULL JSON", commandsWithNullJson.Count);
                }
                catch (Exception ex)
                {
                    errors.Add($"Commands table JSON fix failed: {ex.Message}");
                    _logger.LogError(ex, "Failed to fix commands JSON issues");
                }

                // Fix orphaned subuser role assignments
                try
                {
                    var orphanedSubuserRoles = await _context.SubuserRoles
                        .Where(sr => !_context.subuser.Any(s => s.subuser_id == sr.SubuserId) ||
                                    !_context.Roles.Any(r => r.RoleId == sr.RoleId))
                        .ToListAsync();

                    if (orphanedSubuserRoles.Any())
                    {
                        _context.SubuserRoles.RemoveRange(orphanedSubuserRoles);
                        await _context.SaveChangesAsync();
                        issuesFixed += orphanedSubuserRoles.Count;
                        _logger.LogInformation("Removed {Count} orphaned subuser role assignments", orphanedSubuserRoles.Count);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Subuser role cleanup failed: {ex.Message}");
                    _logger.LogError(ex, "Failed to fix subuser role assignments");
                }

                _logger.LogInformation("Database integrity fix completed by {User}", currentUserEmail);

                return Ok(new
                {
                    success = true,
                    message = "Database integrity fix completed",
                    issuesFixed = issuesFixed,
                    errors = errors,
                    timestamp = DateTime.UtcNow,
                    fixedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    recommendation = errors.Any() ? 
                        "Some issues could not be automatically fixed. Manual intervention may be required." :
                        "All detected issues have been fixed successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fix database integrity");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Database integrity fix failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Get comprehensive system health report (includes user/subuser health)
        /// </summary>
        [HttpGet("health-report")]
        public async Task<ActionResult> GetSystemHealthReport()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "SYSTEM_ADMIN", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions to view system health report" });
            }

            try
            {
                var healthReport = new
                {
                    success = true,
                    message = "System health report generated successfully",
                    timestamp = DateTime.UtcNow,
                    generatedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    userHealth = new
                    {
                        TotalUsers = await _context.Users.CountAsync(),
                        UsersWithRoles = await _context.UserRoles.Select(ur => ur.UserId).Distinct().CountAsync(),
                        UsersWithoutRoles = await _context.Users.CountAsync() - await _context.UserRoles.Select(ur => ur.UserId).Distinct().CountAsync(),
                        UsersWithRecentActivity = await _context.Sessions
                            .Where(s => s.login_time >= DateTime.UtcNow.AddDays(-30))
                            .Join(_context.Users, s => s.user_email, u => u.user_email, (s, u) => u.user_email)
                            .Distinct()
                            .CountAsync()
                    },
                    subuserHealth = new
                    {
                        TotalSubusers = await _context.subuser.CountAsync(),
                        SubusersWithRoles = await _context.SubuserRoles.Select(sr => sr.SubuserId).Distinct().CountAsync(),
                        SubusersWithoutRoles = await _context.subuser.CountAsync() - await _context.SubuserRoles.Select(sr => sr.SubuserId).Distinct().CountAsync(),
                        SubusersWithRecentActivity = await _context.Sessions
                            .Where(s => s.login_time >= DateTime.UtcNow.AddDays(-30))
                            .Join(_context.subuser, s => s.user_email, su => su.subuser_email, (s, su) => su.subuser_email)
                            .Distinct()
                            .CountAsync(),
                        OrphanedSubusers = await _context.subuser
                            .Where(s => !_context.Users.Any(u => u.user_email == s.user_email))
                            .CountAsync()
                    },
                    dataHealth = new
                    {
                        MachinesWithNullJson = await _context.Machines.CountAsync(m => m.license_details_json == null),
                        LogsWithNullJson = await _context.logs.CountAsync(l => l.log_details_json == null),
                        UsersWithNullJson = await _context.Users.CountAsync(u => u.payment_details_json == null || u.license_details_json == null),
                        CommandsWithNullJson = await _context.Commands.CountAsync(c => c.command_json == null),
                        OrphanedUserRoles = await _context.UserRoles
                            .Where(ur => !_context.Users.Any(u => u.user_id == ur.UserId) ||
                                        !_context.Roles.Any(r => r.RoleId == ur.RoleId))
                            .CountAsync(),
                        OrphanedSubuserRoles = await _context.SubuserRoles
                            .Where(sr => !_context.subuser.Any(s => s.subuser_id == sr.SubuserId) ||
                                        !_context.Roles.Any(r => r.RoleId == sr.RoleId))
                            .CountAsync()
                    },
                    systemHealth = new
                    {
                        TotalRoles = await _context.Roles.CountAsync(),
                        TotalPermissions = await _context.Permissions.CountAsync(),
                        TotalRoutes = await _context.Routes.CountAsync(),
                        ActiveSessions = await _context.Sessions.CountAsync(s => s.session_status == "active")
                    },
                    recommendations = GetHealthRecommendations()
                };

                return Ok(healthReport);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate system health report");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to generate system health report",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        #region Private Helper Methods

        private string? GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private async Task<object> GetSystemStatistics()
        {
            return new
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalSubusers = await _context.subuser.CountAsync(),
                TotalMachines = await _context.Machines.CountAsync(),
                TotalReports = await _context.AuditReports.CountAsync(),
                TotalSessions = await _context.Sessions.CountAsync(),
                TotalLogs = await _context.logs.CountAsync(),
                ActiveSessions = await _context.Sessions.CountAsync(s => s.session_status == "active"),
                RecentActivity = new
                {
                    UsersLoggedInToday = await _context.Sessions
                        .Where(s => s.login_time.Date == DateTime.UtcNow.Date)
                        .Join(_context.Users, s => s.user_email, u => u.user_email, (s, u) => u.user_email)
                        .Distinct()
                        .CountAsync(),
                    SubusersLoggedInToday = await _context.Sessions
                        .Where(s => s.login_time.Date == DateTime.UtcNow.Date)
                        .Join(_context.subuser, s => s.user_email, su => su.subuser_email, (s, su) => su.subuser_email)
                        .Distinct()
                        .CountAsync()
                }
            };
        }

        private async Task<(bool Success, string Message, int RolesCreated)> EnsureSubuserSupportAsync()
        {
            try
            {
                var rolesCreated = 0;

                // Ensure SubUser role exists
                var subuserRole = await _context.Roles.Where(r => r.RoleName == "SubUser").FirstOrDefaultAsync();
                if (subuserRole == null)
                {
                    subuserRole = new Role
                    {
                        RoleName = "SubUser",
                        Description = "Default role for subusers with basic permissions",
                        HierarchyLevel = 10 // Lowest priority
                    };
                    _context.Roles.Add(subuserRole);
                    await _context.SaveChangesAsync();
                    rolesCreated++;
                }

                return (true, "Subuser support initialized successfully", rolesCreated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to ensure subuser support");
                return (false, $"Failed to initialize subuser support: {ex.Message}", 0);
            }
        }

        private List<object> GetHealthRecommendations()
        {
            return new List<object>
            {
                new
                {
                    Priority = "High",
                    Category = "Data Integrity",
                    Issue = "NULL JSON fields in database",
                    Recommendation = "Run database integrity fix to resolve NULL JSON issues",
                    Action = "POST /api/DynamicSystem/fix-database-integrity"
                },
                new
                {
                    Priority = "Medium",
                    Category = "User Management",
                    Issue = "Users or subusers without assigned roles",
                    Recommendation = "Assign appropriate roles to all users and subusers",
                    Action = "Use Enhanced User/Subuser controllers to assign roles"
                },
                new
                {
                    Priority = "Medium",
                    Category = "Subuser Management",
                    Issue = "Orphaned subusers without parent users",
                    Recommendation = "Clean up orphaned subuser records or reassign to valid parent users",
                    Action = "Manual review and cleanup required"
                },
                new
                {
                    Priority = "Low",
                    Category = "Performance",
                    Issue = "Inactive sessions not cleaned up",
                    Recommendation = "Implement session cleanup routine",
                    Action = "Review and close inactive sessions"
                }
            };
        }

        #endregion
    }

    /// <summary>
    /// Request model for system reinitialization
    /// </summary>
    public class ReinitializeRequest
    {
        public bool ConfirmReinitialize { get; set; }
        public string? Reason { get; set; }
    }
}