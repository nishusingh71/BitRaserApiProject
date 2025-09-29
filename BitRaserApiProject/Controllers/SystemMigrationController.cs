using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Migration and System Management Controller
    /// Provides endpoints to help transition from ID-based to email-based system
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SystemMigrationController : ControllerBase
    {
        private readonly MigrationUtilityService _migrationService;
        private readonly IUserDataService _userDataService;
        private readonly ILogger<SystemMigrationController> _logger;

        public SystemMigrationController(
            MigrationUtilityService migrationService,
            IUserDataService userDataService,
            ILogger<SystemMigrationController> logger)
        {
            _migrationService = migrationService;
            _userDataService = userDataService;
            _logger = logger;
        }

        /// <summary>
        /// Validate system health and configuration
        /// </summary>
        [HttpGet("validate-system")]
        [RequirePermission("FullAccess")]
        public async Task<IActionResult> ValidateSystem()
        {
            try
            {
                var rolesValidation = await _migrationService.ValidateSystemRolesAndPermissionsAsync();
                var machineValidation = await _migrationService.ValidateMachineEmailAssociationsAsync();

                var result = new
                {
                    timestamp = DateTime.UtcNow,
                    rolesAndPermissions = rolesValidation,
                    machineAssociations = new
                    {
                        totalMachines = machineValidation.TotalItems,
                        validMachines = machineValidation.SuccessfulItems,
                        invalidMachines = machineValidation.FailedItems,
                        issues = machineValidation.ErrorMessages
                    },
                    overallStatus = rolesValidation.IsValid && machineValidation.FailedItems == 0 ? "Healthy" : "Issues Found"
                };

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during system validation");
                return StatusCode(500, new { message = "System validation failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Migrate users without roles to default roles
        /// </summary>
        [HttpPost("migrate-user-roles")]
        [RequirePermission("FullAccess")]
        public async Task<IActionResult> MigrateUserRoles()
        {
            try
            {
                var result = await _migrationService.MigrateUsersToDefaultRolesAsync();
                
                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    operation = "User Role Migration",
                    totalUsers = result.TotalItems,
                    successfulMigrations = result.SuccessfulItems,
                    failedMigrations = result.FailedItems,
                    isSuccessful = result.IsSuccessful,
                    successMessages = result.SuccessMessages,
                    errorMessages = result.ErrorMessages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user role migration");
                return StatusCode(500, new { message = "User role migration failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Clean up orphaned role assignments
        /// </summary>
        [HttpPost("cleanup-orphaned-roles")]
        [RequirePermission("FullAccess")]
        public async Task<IActionResult> CleanupOrphanedRoles()
        {
            try
            {
                var result = await _migrationService.CleanupOrphanedRoleAssignmentsAsync();
                
                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    operation = "Orphaned Role Cleanup",
                    totalOrphanedAssignments = result.TotalItems,
                    cleanedAssignments = result.SuccessfulItems,
                    isSuccessful = result.IsSuccessful,
                    successMessages = result.SuccessMessages,
                    errorMessages = result.ErrorMessages
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned role cleanup");
                return StatusCode(500, new { message = "Orphaned role cleanup failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Get system statistics and health information
        /// </summary>
        [HttpGet("system-stats")]
        [RequirePermission("FullAccess")]
        public async Task<IActionResult> GetSystemStats()
        {
            try
            {
                var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                // Gather system statistics
                var stats = new
                {
                    timestamp = DateTime.UtcNow,
                    requestedBy = userEmail,
                    
                    users = new
                    {
                        totalUsers = await _userDataService.GetUserByEmailAsync(userEmail) != null ? "Available" : "Error",
                        // Add more user statistics as needed
                    },
                    
                    roles = new
                    {
                        availableRoles = (await _userDataService.GetAvailableRolesForUserAsync(userEmail))
                                        .Select(r => new { r.RoleName, r.HierarchyLevel, r.Description })
                                        .ToList()
                    },
                    
                    currentUser = new
                    {
                        email = userEmail,
                        roles = await _userDataService.GetUserRoleNamesAsync(userEmail),
                        permissions = await _userDataService.GetUserPermissionsAsync(userEmail),
                        canManageUsers = await _userDataService.HasPermissionAsync(userEmail, "UserManagement"),
                        isSystemAdmin = await _userDataService.HasPermissionAsync(userEmail, "FullAccess")
                    }
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting system statistics");
                return StatusCode(500, new { message = "Failed to get system statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Test email-based operations
        /// </summary>
        [HttpPost("test-email-operations")]
        [RequirePermission("FullAccess")]
        public async Task<IActionResult> TestEmailOperations([FromBody] TestEmailOperationsRequest request)
        {
            try
            {
                var results = new List<object>();

                // Test user lookup
                var user = await _userDataService.GetUserByEmailAsync(request.TestEmail);
                results.Add(new
                {
                    operation = "GetUserByEmail",
                    email = request.TestEmail,
                    success = user != null,
                    result = user != null ? "User found" : "User not found"
                });

                if (user != null)
                {
                    // Test role retrieval
                    var roles = await _userDataService.GetUserRoleNamesAsync(request.TestEmail);
                    results.Add(new
                    {
                        operation = "GetUserRoles",
                        email = request.TestEmail,
                        success = true,
                        result = roles.ToList()
                    });

                    // Test permission retrieval
                    var permissions = await _userDataService.GetUserPermissionsAsync(request.TestEmail);
                    results.Add(new
                    {
                        operation = "GetUserPermissions",
                        email = request.TestEmail,
                        success = true,
                        result = permissions.ToList()
                    });

                    // Test machine retrieval
                    var machines = await _userDataService.GetMachinesByUserEmailAsync(request.TestEmail);
                    results.Add(new
                    {
                        operation = "GetMachinesByUserEmail",
                        email = request.TestEmail,
                        success = true,
                        result = $"Found {machines.Count()} machines"
                    });

                    // Test subuser retrieval
                    var subusers = await _userDataService.GetSubusersByParentEmailAsync(request.TestEmail);
                    results.Add(new
                    {
                        operation = "GetSubusersByParentEmail",
                        email = request.TestEmail,
                        success = true,
                        result = $"Found {subusers.Count()} subusers"
                    });
                }

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    testEmail = request.TestEmail,
                    results = results
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during email operations test");
                return StatusCode(500, new { message = "Email operations test failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Get migration recommendations
        /// </summary>
        [HttpGet("migration-recommendations")]
        [RequirePermission("FullAccess")]
        public async Task<IActionResult> GetMigrationRecommendations()
        {
            try
            {
                var recommendations = new List<object>();

                // Check for users without roles
                var validation = await _migrationService.ValidateSystemRolesAndPermissionsAsync();
                
                if (!validation.IsValid)
                {
                    recommendations.Add(new
                    {
                        priority = "High",
                        category = "System Configuration",
                        issue = "Missing required roles or permissions",
                        recommendation = "Run system validation and ensure all default roles and permissions are created",
                        action = "POST /api/SystemMigration/validate-system"
                    });
                }

                recommendations.Add(new
                {
                    priority = "Medium",
                    category = "User Management",
                    issue = "Users without assigned roles",
                    recommendation = "Assign default roles to users who don't have any roles assigned",
                    action = "POST /api/SystemMigration/migrate-user-roles"
                });

                recommendations.Add(new
                {
                    priority = "Low",
                    category = "Data Cleanup",
                    issue = "Orphaned role assignments",
                    recommendation = "Clean up role assignments that reference deleted users or roles",
                    action = "POST /api/SystemMigration/cleanup-orphaned-roles"
                });

                recommendations.Add(new
                {
                    priority = "Info",
                    category = "API Migration",
                    issue = "Legacy ID-based endpoints",
                    recommendation = "Gradually migrate from ID-based endpoints to email-based endpoints in /api/DynamicUser",
                    action = "Update client applications to use new email-based endpoints"
                });

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    totalRecommendations = recommendations.Count,
                    recommendations = recommendations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting migration recommendations");
                return StatusCode(500, new { message = "Failed to get migration recommendations", error = ex.Message });
            }
        }

        public class TestEmailOperationsRequest
        {
            public string TestEmail { get; set; } = string.Empty;
        }
    }
}