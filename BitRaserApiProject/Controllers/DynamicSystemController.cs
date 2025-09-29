using Microsoft.AspNetCore.Mvc;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using BitRaserApiProject.Attributes;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Dynamic System Management Controller
    /// Provides endpoints for managing permissions, roles, and routes dynamically
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicSystemController : ControllerBase
    {
        private readonly IDynamicPermissionService _permissionService;
        private readonly DynamicRouteService _routeService;
        private readonly ILogger<DynamicSystemController> _logger;
        private readonly ApplicationDbContext _context; // Assuming you're using EF Core and have a DbContext

        public DynamicSystemController(
            IDynamicPermissionService permissionService, 
            DynamicRouteService routeService,
            ILogger<DynamicSystemController> logger,
            ApplicationDbContext context)
        {
            _permissionService = permissionService;
            _routeService = routeService;
            _logger = logger;
            _context = context;
        }

        /// <summary>
        /// Initialize the dynamic system - create permissions, roles, and routes
        /// </summary>
        [HttpPost("initialize")]
        [RequirePermission("SYSTEM_ADMIN")]
        public async Task<ActionResult> InitializeSystem()
        {
            try
            {
                _logger.LogInformation("Starting dynamic system initialization");

                var results = new
                {
                    Timestamp = DateTime.UtcNow,
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

                _logger.LogInformation("Dynamic system initialization completed successfully");

                return Ok(new
                {
                    success = true,
                    message = "Dynamic system initialization completed successfully",
                    timestamp = DateTime.UtcNow,
                    steps = new object[] { step1, step2, step3, step4 },
                    summary = new
                    {
                        PermissionsCreated = permissionResult.PermissionsCreated,
                        MappingsCreated = roleMappingResult.MappingsCreated,
                        RoutesDiscovered = routeResult.RoutesDiscovered,
                        ControllersProcessed = routeResult.ControllersProcessed,
                        OrphanedRoutesRemoved = cleanupResult.OrphanedRoutesRemoved
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
                    timestamp = DateTime.UtcNow
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
        /// Get system status - Overview of current permissions, roles, and routes
        /// </summary>
        [HttpGet("status")]
        [RequirePermission("SYSTEM_ADMIN")]
        public async Task<ActionResult> GetSystemStatus()
        {
            try
            {
                _logger.LogInformation("Getting system status");

                // This would require additional methods in the services
                // For now, return a basic status
                return Ok(new
                {
                    success = true,
                    message = "System status retrieved successfully",
                    status = "Operational",
                    timestamp = DateTime.UtcNow,
                    services = new
                    {
                        PermissionService = "Running",
                        RouteService = "Running"
                    }
                });
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
        /// Get performance metrics of the dynamic system
        /// </summary>
        [HttpGet("metrics")]
        [RequirePermission("SYSTEM_ADMIN")]
        public async Task<ActionResult> GetSystemMetrics()
        {
            try
            {
                return Ok(new
                {
                    success = true,
                    message = "System metrics retrieved successfully",
                    metrics = new
                    {
                        Timestamp = DateTime.UtcNow,
                        SystemUptime = TimeSpan.FromMilliseconds(Environment.TickCount64),
                        MemoryUsage = new
                        {
                            WorkingSet = GC.GetTotalMemory(false),
                            Gen0Collections = GC.CollectionCount(0),
                            Gen1Collections = GC.CollectionCount(1),
                            Gen2Collections = GC.CollectionCount(2)
                        }
                    }
                });
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
        /// Full system reinitialize - Use with caution in production
        /// </summary>
        [HttpPost("reinitialize")]
        [RequirePermission("SYSTEM_ADMIN")]
        public async Task<ActionResult> ReinitializeSystem([FromBody] ReinitializeRequest request)
        {
            try
            {
                if (!request.ConfirmReinitialize)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Reinitialization requires explicit confirmation",
                        requiredField = "confirmReinitialize: true"
                    });
                }

                _logger.LogWarning("System reinitialization requested by user");

                // Perform full system reinitialization
                var initResult = await InitializeSystem();

                _logger.LogInformation("System reinitialization completed");

                return Ok(new
                {
                    success = true,
                    message = "System successfully reinitialized",
                    timestamp = DateTime.UtcNow,
                    warning = "System has been fully reinitialized",
                    initializationResult = initResult
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reinitialize system");
                return StatusCode(500, new
                {
                    success = false,
                    message = "System reinitialization failed",
                    error = ex.Message,
                    timestamp = DateTime.UtcNow
                });
            }
        }

        /// <summary>
        /// Fix database data integrity issues - JSON columns, NULL values, etc.
        /// </summary>
        [HttpPost("fix-database-integrity")]
        [RequirePermission("SYSTEM_ADMIN")]
        public async Task<ActionResult> FixDatabaseIntegrity()
        {
            try
            {
                _logger.LogInformation("Starting database integrity fix");

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

                _logger.LogInformation("Database integrity fix completed");

                return Ok(new
                {
                    success = true,
                    message = "Database integrity fix completed",
                    issuesFixed = issuesFixed,
                    errors = errors,
                    timestamp = DateTime.UtcNow,
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