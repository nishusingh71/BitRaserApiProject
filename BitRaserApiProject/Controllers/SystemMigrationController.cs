using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Migration and System Management Controller
    /// Provides endpoints to help transition from ID-based to email-based system
    /// Supports both users and subusers with comprehensive migration capabilities
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class SystemMigrationController : ControllerBase
    {
        private readonly MigrationUtilityService _migrationService;
        private readonly IUserDataService _userDataService;
        private readonly IRoleBasedAuthService _authService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SystemMigrationController> _logger;

        public SystemMigrationController(
            MigrationUtilityService migrationService,
            IUserDataService userDataService,
            IRoleBasedAuthService authService,
            ApplicationDbContext context,
            ILogger<SystemMigrationController> logger)
        {
            _migrationService = migrationService;
            _userDataService = userDataService;
            _authService = authService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Validate system health and configuration (includes subuser validation)
        /// </summary>
        [HttpGet("validate-system")]
        public async Task<IActionResult> ValidateSystem()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "FullAccess", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions for system validation" });
            }

            try
            {
                _logger.LogInformation("System validation initiated by {User} ({UserType})", currentUserEmail, isSubuser ? "Subuser" : "User");

                var rolesValidation = await _migrationService.ValidateSystemRolesAndPermissionsAsync();
                var machineValidation = await _migrationService.ValidateMachineEmailAssociationsAsync();
                var subuserValidation = await ValidateSubuserIntegrityAsync();

                var result = new
                {
                    timestamp = DateTime.UtcNow,
                    validatedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    rolesAndPermissions = rolesValidation,
                    machineAssociations = new
                    {
                        totalMachines = machineValidation.TotalItems,
                        validMachines = machineValidation.SuccessfulItems,
                        invalidMachines = machineValidation.FailedItems,
                        issues = machineValidation.ErrorMessages
                    },
                    subuserValidation = new
                    {
                        totalSubusers = subuserValidation.TotalSubusers,
                        subusersWithValidParents = subuserValidation.ValidSubusers,
                        orphanedSubusers = subuserValidation.OrphanedSubusers,
                        subusersWithRoles = subuserValidation.SubusersWithRoles,
                        subusersWithoutRoles = subuserValidation.SubusersWithoutRoles,
                        issues = subuserValidation.Issues
                    },
                    overallStatus = rolesValidation.IsValid && 
                                   machineValidation.FailedItems == 0 && 
                                   subuserValidation.OrphanedSubusers == 0 ? "Healthy" : "Issues Found",
                    recommendations = GenerateValidationRecommendations(rolesValidation, machineValidation, subuserValidation)
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
        /// Migrate users and subusers without roles to default roles
        /// </summary>
        [HttpPost("migrate-user-roles")]
        public async Task<IActionResult> MigrateUserRoles()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "FullAccess", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions for role migration" });
            }

            try
            {
                _logger.LogInformation("User role migration initiated by {User} ({UserType})", currentUserEmail, isSubuser ? "Subuser" : "User");

                var userMigrationResult = await _migrationService.MigrateUsersToDefaultRolesAsync();
                var subuserMigrationResult = await MigrateSubusersToDefaultRolesAsync();
                
                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    operation = "User and Subuser Role Migration",
                    migratedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    userMigration = new
                    {
                        totalUsers = userMigrationResult.TotalItems,
                        successfulMigrations = userMigrationResult.SuccessfulItems,
                        failedMigrations = userMigrationResult.FailedItems,
                        isSuccessful = userMigrationResult.IsSuccessful,
                        successMessages = userMigrationResult.SuccessMessages,
                        errorMessages = userMigrationResult.ErrorMessages
                    },
                    subuserMigration = new
                    {
                        totalSubusers = subuserMigrationResult.TotalItems,
                        successfulMigrations = subuserMigrationResult.SuccessfulItems,
                        failedMigrations = subuserMigrationResult.FailedItems,
                        isSuccessful = subuserMigrationResult.IsSuccessful,
                        successMessages = subuserMigrationResult.SuccessMessages,
                        errorMessages = subuserMigrationResult.ErrorMessages
                    },
                    overallSuccess = userMigrationResult.IsSuccessful && subuserMigrationResult.IsSuccessful
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during user role migration");
                return StatusCode(500, new { message = "User role migration failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Clean up orphaned role assignments (includes subuser roles)
        /// </summary>
        [HttpPost("cleanup-orphaned-roles")]
        public async Task<IActionResult> CleanupOrphanedRoles()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "FullAccess", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions for role cleanup" });
            }

            try
            {
                _logger.LogInformation("Orphaned role cleanup initiated by {User} ({UserType})", currentUserEmail, isSubuser ? "Subuser" : "User");

                var userRoleCleanupResult = await _migrationService.CleanupOrphanedRoleAssignmentsAsync();
                var subuserRoleCleanupResult = await CleanupOrphanedSubuserRolesAsync();
                
                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    operation = "Orphaned Role Cleanup",
                    cleanedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    userRoleCleanup = new
                    {
                        totalOrphanedAssignments = userRoleCleanupResult.TotalItems,
                        cleanedAssignments = userRoleCleanupResult.SuccessfulItems,
                        isSuccessful = userRoleCleanupResult.IsSuccessful,
                        successMessages = userRoleCleanupResult.SuccessMessages,
                        errorMessages = userRoleCleanupResult.ErrorMessages
                    },
                    subuserRoleCleanup = new
                    {
                        totalOrphanedAssignments = subuserRoleCleanupResult.TotalItems,
                        cleanedAssignments = subuserRoleCleanupResult.SuccessfulItems,
                        isSuccessful = subuserRoleCleanupResult.IsSuccessful,
                        successMessages = subuserRoleCleanupResult.SuccessMessages,
                        errorMessages = subuserRoleCleanupResult.ErrorMessages
                    },
                    overallSuccess = userRoleCleanupResult.IsSuccessful && subuserRoleCleanupResult.IsSuccessful
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during orphaned role cleanup");
                return StatusCode(500, new { message = "Orphaned role cleanup failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Fix subuser data integrity issues
        /// </summary>
        [HttpPost("fix-subuser-integrity")]
        public async Task<IActionResult> FixSubuserIntegrity()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "FullAccess", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions for subuser integrity fix" });
            }

            try
            {
                _logger.LogInformation("Subuser integrity fix initiated by {User} ({UserType})", currentUserEmail, isSubuser ? "Subuser" : "User");

                var issuesFixed = 0;
                var errors = new List<string>();

                // Fix orphaned subusers (subusers whose parent users don't exist)
                try
                {
                    var orphanedSubusers = await _context.subuser
                        .Where(s => !_context.Users.Any(u => u.user_email == s.user_email))
                        .ToListAsync();

                    if (orphanedSubusers.Any())
                    {
                        _context.subuser.RemoveRange(orphanedSubusers);
                        await _context.SaveChangesAsync();
                        issuesFixed += orphanedSubusers.Count;
                        _logger.LogInformation("Removed {Count} orphaned subusers", orphanedSubusers.Count);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Orphaned subuser cleanup failed: {ex.Message}");
                    _logger.LogError(ex, "Failed to clean up orphaned subusers");
                }

                // Fix subusers with invalid superuser_id references
                try
                {
                    var invalidSubusers = await _context.subuser
                        .Where(s => !_context.Users.Any(u => u.user_id == s.superuser_id))
                        .ToListAsync();

                    foreach (var invalidSubuser in invalidSubusers)
                    {
                        var parentUser = await _context.Users.Where(u => u.user_email == invalidSubuser.user_email).FirstOrDefaultAsync();
                        if (parentUser != null)
                        {
                            invalidSubuser.superuser_id = parentUser.user_id;
                            issuesFixed++;
                        }
                    }

                    if (invalidSubusers.Any())
                    {
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("Fixed {Count} subusers with invalid superuser_id", invalidSubusers.Count);
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Invalid superuser_id fix failed: {ex.Message}");
                    _logger.LogError(ex, "Failed to fix invalid superuser_id references");
                }

                // Assign default roles to subusers without roles
                try
                {
                    var subusersWithoutRoles = await _context.subuser
                        .Where(s => !s.SubuserRoles.Any())
                        .ToListAsync();

                    var defaultRole = await _context.Roles.Where(r => r.RoleName == "SubUser").FirstOrDefaultAsync();
                    if (defaultRole != null)
                    {
                        foreach (var subuser in subusersWithoutRoles)
                        {
                            var subuserRole = new SubuserRole
                            {
                                SubuserId = subuser.subuser_id,
                                RoleId = defaultRole.RoleId,
                                AssignedAt = DateTime.UtcNow,
                                AssignedByEmail = currentUserEmail ?? "System"
                            };
                            _context.SubuserRoles.Add(subuserRole);
                            issuesFixed++;
                        }

                        if (subusersWithoutRoles.Any())
                        {
                            await _context.SaveChangesAsync();
                            _logger.LogInformation("Assigned default roles to {Count} subusers", subusersWithoutRoles.Count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"Default role assignment failed: {ex.Message}");
                    _logger.LogError(ex, "Failed to assign default roles to subusers");
                }

                return Ok(new
                {
                    success = true,
                    message = "Subuser integrity fix completed",
                    timestamp = DateTime.UtcNow,
                    fixedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    issuesFixed = issuesFixed,
                    errors = errors,
                    recommendation = errors.Any() ? 
                        "Some issues could not be automatically fixed. Manual intervention may be required." :
                        "All detected subuser integrity issues have been fixed successfully."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during subuser integrity fix");
                return StatusCode(500, new { message = "Subuser integrity fix failed", error = ex.Message });
            }
        }

        /// <summary>
        /// Get system statistics and health information (includes subuser statistics)
        /// </summary>
        [HttpGet("system-stats")]
        public async Task<IActionResult> GetSystemStats()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "FullAccess", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions to view system statistics" });
            }

            try
            {
                var stats = new
                {
                    timestamp = DateTime.UtcNow,
                    requestedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    
                    users = new
                    {
                        totalUsers = await _context.Users.CountAsync(),
                        usersWithRoles = await _context.UserRoles.Select(ur => ur.UserId).Distinct().CountAsync(),
                        usersWithoutRoles = await _context.Users.CountAsync() - await _context.UserRoles.Select(ur => ur.UserId).Distinct().CountAsync(),
                        recentUsers = await _context.Users.Where(u => u.created_at >= DateTime.UtcNow.AddDays(-30)).CountAsync()
                    },

                    subusers = new
                    {
                        totalSubusers = await _context.subuser.CountAsync(),
                        subusersWithRoles = await _context.SubuserRoles.Select(sr => sr.SubuserId).Distinct().CountAsync(),
                        subusersWithoutRoles = await _context.subuser.CountAsync() - await _context.SubuserRoles.Select(sr => sr.SubuserId).Distinct().CountAsync(),
                        orphanedSubusers = await _context.subuser.Where(s => !_context.Users.Any(u => u.user_email == s.user_email)).CountAsync(),
                        topParentUsers = await _context.subuser
                            .GroupBy(s => s.user_email)
                            .Select(g => new { ParentEmail = g.Key, SubuserCount = g.Count() })
                            .OrderByDescending(x => x.SubuserCount)
                            .Take(5)
                            .ToListAsync()
                    },
                    
                    roles = new
                    {
                        availableRoles = (await _userDataService.GetAvailableRolesForUserAsync(currentUserEmail!))
                                        .Select(r => new { r.RoleName, r.HierarchyLevel, r.Description })
                                        .ToList(),
                        userRoleDistribution = await _context.UserRoles
                            .Include(ur => ur.Role)
                            .GroupBy(ur => ur.Role.RoleName)
                            .Select(g => new { RoleName = g.Key, UserCount = g.Count() })
                            .ToListAsync(),
                        subuserRoleDistribution = await _context.SubuserRoles
                            .Include(sr => sr.Role)
                            .GroupBy(sr => sr.Role.RoleName)
                            .Select(g => new { RoleName = g.Key, SubuserCount = g.Count() })
                            .ToListAsync()
                    },
                    
                    currentUser = new
                    {
                        email = currentUserEmail,
                        userType = isSubuser ? "Subuser" : "User",
                        roles = await _userDataService.GetUserRoleNamesAsync(currentUserEmail!, isSubuser),
                        permissions = await _userDataService.GetUserPermissionsAsync(currentUserEmail!, isSubuser),
                        canManageUsers = await _userDataService.HasPermissionAsync(currentUserEmail!, "UserManagement", isSubuser),
                        isSystemAdmin = await _userDataService.HasPermissionAsync(currentUserEmail!, "FullAccess", isSubuser)
                    },

                    dataIntegrity = new
                    {
                        machinesWithoutUserEmail = await _context.Machines.CountAsync(m => string.IsNullOrEmpty(m.user_email)),
                        reportsWithoutClientEmail = await _context.AuditReports.CountAsync(r => string.IsNullOrEmpty(r.client_email)),
                        sessionsWithoutUserEmail = await _context.Sessions.CountAsync(s => string.IsNullOrEmpty(s.user_email)),
                        logsWithoutUserEmail = await _context.logs.CountAsync(l => string.IsNullOrEmpty(l.user_email))
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
        /// Test email-based operations (includes subuser operations)
        /// </summary>
        [HttpPost("test-email-operations")]
        public async Task<IActionResult> TestEmailOperations([FromBody] TestEmailOperationsRequest request)
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "FullAccess", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions for email operations test" });
            }

            try
            {
                var results = new List<object>();

                // Test if the email is a user or subuser
                var isTestEmailSubuser = await _userDataService.SubuserExistsAsync(request.TestEmail);
                var isTestEmailUser = await _userDataService.UserExistsAsync(request.TestEmail);

                results.Add(new
                {
                    operation = "EmailTypeCheck",
                    email = request.TestEmail,
                    success = true,
                    result = new
                    {
                        IsUser = isTestEmailUser,
                        IsSubuser = isTestEmailSubuser,
                        EmailType = isTestEmailUser ? "User" : (isTestEmailSubuser ? "Subuser" : "Not Found")
                    }
                });

                if (isTestEmailUser)
                {
                    // Test user operations
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
                        var roles = await _userDataService.GetUserRoleNamesAsync(request.TestEmail, false);
                        results.Add(new
                        {
                            operation = "GetUserRoles",
                            email = request.TestEmail,
                            success = true,
                            result = roles.ToList()
                        });

                        // Test permission retrieval
                        var permissions = await _userDataService.GetUserPermissionsAsync(request.TestEmail, false);
                        results.Add(new
                        {
                            operation = "GetUserPermissions",
                            email = request.TestEmail,
                            success = true,
                            result = permissions.ToList()
                        });

                        // Test subuser retrieval
                        var subusers = await _userDataService.GetSubusersByParentEmailAsync(request.TestEmail);
                        results.Add(new
                        {
                            operation = "GetSubusersByParentEmail",
                            email = request.TestEmail,
                            success = true,
                            result = $"Found {subusers.Count()} subusers: {string.Join(", ", subusers.Select(s => s.subuser_email))}"
                        });
                    }
                }
                else if (isTestEmailSubuser)
                {
                    // Test subuser operations
                    var subuser = await _userDataService.GetSubuserByEmailAsync(request.TestEmail);
                    results.Add(new
                    {
                        operation = "GetSubuserByEmail",
                        email = request.TestEmail,
                        success = subuser != null,
                        result = subuser != null ? $"Subuser found, parent: {subuser.user_email}" : "Subuser not found"
                    });

                    if (subuser != null)
                    {
                        // Test subuser role retrieval
                        var roles = await _userDataService.GetUserRoleNamesAsync(request.TestEmail, true);
                        results.Add(new
                        {
                            operation = "GetSubuserRoles",
                            email = request.TestEmail,
                            success = true,
                            result = roles.ToList()
                        });

                        // Test subuser permission retrieval
                        var permissions = await _userDataService.GetUserPermissionsAsync(request.TestEmail, true);
                        results.Add(new
                        {
                            operation = "GetSubuserPermissions",
                            email = request.TestEmail,
                            success = true,
                            result = permissions.ToList()
                        });
                    }
                }

                // Test common operations regardless of user type
                var machines = await _userDataService.GetMachinesByUserEmailAsync(request.TestEmail);
                results.Add(new
                {
                    operation = "GetMachinesByUserEmail",
                    email = request.TestEmail,
                    success = true,
                    result = $"Found {machines.Count()} machines"
                });

                var reports = await _userDataService.GetAuditReportsByEmailAsync(request.TestEmail);
                results.Add(new
                {
                    operation = "GetAuditReportsByEmail",
                    email = request.TestEmail,
                    success = true,
                    result = $"Found {reports.Count()} reports"
                });

                var sessions = await _userDataService.GetSessionsByEmailAsync(request.TestEmail);
                results.Add(new
                {
                    operation = "GetSessionsByEmail",
                    email = request.TestEmail,
                    success = true,
                    result = $"Found {sessions.Count()} sessions"
                });

                var logs = await _userDataService.GetLogsByEmailAsync(request.TestEmail);
                results.Add(new
                {
                    operation = "GetLogsByEmail",
                    email = request.TestEmail,
                    success = true,
                    result = $"Found {logs.Count()} logs"
                });

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    testEmail = request.TestEmail,
                    testedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    totalOperations = results.Count,
                    successfulOperations = results.Count(r => (bool)r.GetType().GetProperty("success")!.GetValue(r)!),
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
        /// Get migration recommendations (includes subuser-specific recommendations)
        /// </summary>
        [HttpGet("migration-recommendations")]
        public async Task<IActionResult> GetMigrationRecommendations()
        {
            var currentUserEmail = GetCurrentUserEmail();
            var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "FullAccess", isSubuser))
            {
                return StatusCode(403, new { error = "Insufficient permissions to view migration recommendations" });
            }

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

                // Check for users without roles
                var usersWithoutRoles = await _context.Users.CountAsync() - await _context.UserRoles.Select(ur => ur.UserId).Distinct().CountAsync();
                if (usersWithoutRoles > 0)
                {
                    recommendations.Add(new
                    {
                        priority = "High",
                        category = "User Management",
                        issue = $"{usersWithoutRoles} users without assigned roles",
                        recommendation = "Assign default roles to users who don't have any roles assigned",
                        action = "POST /api/SystemMigration/migrate-user-roles"
                    });
                }

                // Check for subusers without roles
                var subusersWithoutRoles = await _context.subuser.CountAsync() - await _context.SubuserRoles.Select(sr => sr.SubuserId).Distinct().CountAsync();
                if (subusersWithoutRoles > 0)
                {
                    recommendations.Add(new
                    {
                        priority = "High",
                        category = "Subuser Management",
                        issue = $"{subusersWithoutRoles} subusers without assigned roles",
                        recommendation = "Assign default roles to subusers who don't have any roles assigned",
                        action = "POST /api/SystemMigration/migrate-user-roles"
                    });
                }

                // Check for orphaned subusers
                var orphanedSubusers = await _context.subuser.CountAsync(s => !_context.Users.Any(u => u.user_email == s.user_email));
                if (orphanedSubusers > 0)
                {
                    recommendations.Add(new
                    {
                        priority = "High",
                        category = "Data Integrity",
                        issue = $"{orphanedSubusers} orphaned subusers without valid parent users",
                        recommendation = "Clean up orphaned subuser records or reassign to valid parent users",
                        action = "POST /api/SystemMigration/fix-subuser-integrity"
                    });
                }

                recommendations.Add(new
                {
                    priority = "Medium",
                    category = "Data Cleanup",
                    issue = "Orphaned role assignments",
                    recommendation = "Clean up role assignments that reference deleted users, subusers, or roles",
                    action = "POST /api/SystemMigration/cleanup-orphaned-roles"
                });

                recommendations.Add(new
                {
                    priority = "Low",
                    category = "API Migration",
                    issue = "Legacy ID-based endpoints",
                    recommendation = "Gradually migrate from ID-based endpoints to email-based endpoints in /api/DynamicUser",
                    action = "Update client applications to use new email-based endpoints"
                });

                recommendations.Add(new
                {
                    priority = "Info",
                    category = "Subuser Management",
                    issue = "Subuser access optimization",
                    recommendation = "Review subuser permissions and ensure they have appropriate access levels",
                    action = "Use Enhanced Subuser Controller to review and optimize subuser roles"
                });

                return Ok(new
                {
                    timestamp = DateTime.UtcNow,
                    generatedBy = new
                    {
                        Email = currentUserEmail,
                        UserType = isSubuser ? "Subuser" : "User"
                    },
                    totalRecommendations = recommendations.Count,
                    highPriority = recommendations.Count(r => r.GetType().GetProperty("priority")!.GetValue(r)!.ToString() == "High"),
                    mediumPriority = recommendations.Count(r => r.GetType().GetProperty("priority")!.GetValue(r)!.ToString() == "Medium"),
                    lowPriority = recommendations.Count(r => r.GetType().GetProperty("priority")!.GetValue(r)!.ToString() == "Low"),
                    recommendations = recommendations
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting migration recommendations");
                return StatusCode(500, new { message = "Failed to get migration recommendations", error = ex.Message });
            }
        }

        #region Private Helper Methods

        private string? GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        private async Task<SubuserValidationResult> ValidateSubuserIntegrityAsync()
        {
            var totalSubusers = await _context.subuser.CountAsync();
            var orphanedSubusers = await _context.subuser.CountAsync(s => !_context.Users.Any(u => u.user_email == s.user_email));
            var subusersWithRoles = await _context.SubuserRoles.Select(sr => sr.SubuserId).Distinct().CountAsync();
            var subusersWithoutRoles = totalSubusers - subusersWithRoles;
            var validSubusers = totalSubusers - orphanedSubusers;

            var issues = new List<string>();
            if (orphanedSubusers > 0)
                issues.Add($"{orphanedSubusers} subusers have invalid parent user references");
            if (subusersWithoutRoles > 0)
                issues.Add($"{subusersWithoutRoles} subusers don't have assigned roles");

            return new SubuserValidationResult
            {
                TotalSubusers = totalSubusers,
                ValidSubusers = validSubusers,
                OrphanedSubusers = orphanedSubusers,
                SubusersWithRoles = subusersWithRoles,
                SubusersWithoutRoles = subusersWithoutRoles,
                Issues = issues
            };
        }

        private async Task<MigrationResult> MigrateSubusersToDefaultRolesAsync()
        {
            var result = new MigrationResult();
            
            try
            {
                var subusersWithoutRoles = await _context.subuser
                    .Where(s => !s.SubuserRoles.Any())
                    .ToListAsync();

                result.TotalItems = subusersWithoutRoles.Count;

                var defaultRole = await _context.Roles.Where(r => r.RoleName == "SubUser").FirstOrDefaultAsync();
                if (defaultRole == null)
                {
                    result.ErrorMessages.Add("Default 'SubUser' role not found. Create it first.");
                    return result;
                }

                foreach (var subuser in subusersWithoutRoles)
                {
                    try
                    {
                        var subuserRole = new SubuserRole
                        {
                            SubuserId = subuser.subuser_id,
                            RoleId = defaultRole.RoleId,
                            AssignedAt = DateTime.UtcNow,
                            AssignedByEmail = "System Migration"
                        };

                        _context.SubuserRoles.Add(subuserRole);
                        result.SuccessfulItems++;
                        result.SuccessMessages.Add($"Assigned default role to subuser {subuser.subuser_email}");
                    }
                    catch (Exception ex)
                    {
                        result.FailedItems++;
                        result.ErrorMessages.Add($"Failed to assign role to subuser {subuser.subuser_email}: {ex.Message}");
                    }
                }

                if (result.SuccessfulItems > 0)
                {
                    await _context.SaveChangesAsync();
                }

                result.IsSuccessful = result.FailedItems == 0;
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"Migration failed: {ex.Message}");
                result.IsSuccessful = false;
            }

            return result;
        }

        private async Task<MigrationResult> CleanupOrphanedSubuserRolesAsync()
        {
            var result = new MigrationResult();
            
            try
            {
                var orphanedRoles = await _context.SubuserRoles
                    .Where(sr => !_context.subuser.Any(s => s.subuser_id == sr.SubuserId) ||
                                !_context.Roles.Any(r => r.RoleId == sr.RoleId))
                    .ToListAsync();

                result.TotalItems = orphanedRoles.Count;

                foreach (var orphanedRole in orphanedRoles)
                {
                    try
                    {
                        _context.SubuserRoles.Remove(orphanedRole);
                        result.SuccessfulItems++;
                        result.SuccessMessages.Add($"Removed orphaned subuser role assignment (SubuserId: {orphanedRole.SubuserId}, RoleId: {orphanedRole.RoleId})");
                    }
                    catch (Exception ex)
                    {
                        result.FailedItems++;
                        result.ErrorMessages.Add($"Failed to remove orphaned role assignment: {ex.Message}");
                    }
                }

                if (result.SuccessfulItems > 0)
                {
                    await _context.SaveChangesAsync();
                }

                result.IsSuccessful = result.FailedItems == 0;
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"Cleanup failed: {ex.Message}");
                result.IsSuccessful = false;
            }

            return result;
        }

        private List<object> GenerateValidationRecommendations(dynamic rolesValidation, dynamic machineValidation, SubuserValidationResult subuserValidation)
        {
            var recommendations = new List<object>();

            if (!rolesValidation.IsValid)
            {
                recommendations.Add(new { priority = "High", issue = "Invalid roles/permissions configuration", action = "Fix role and permission setup" });
            }

            if (machineValidation.FailedItems > 0)
            {
                recommendations.Add(new { priority = "Medium", issue = "Invalid machine email associations", action = "Review machine-user associations" });
            }

            if (subuserValidation.OrphanedSubusers > 0)
            {
                recommendations.Add(new { priority = "High", issue = "Orphaned subusers detected", action = "Run subuser integrity fix" });
            }

            if (subuserValidation.SubusersWithoutRoles > 0)
            {
                recommendations.Add(new { priority = "Medium", issue = "Subusers without roles", action = "Assign default roles to subusers" });
            }

            return recommendations;
        }

        #endregion

        #region Data Models

        private class SubuserValidationResult
        {
            public int TotalSubusers { get; set; }
            public int ValidSubusers { get; set; }
            public int OrphanedSubusers { get; set; }
            public int SubusersWithRoles { get; set; }
            public int SubusersWithoutRoles { get; set; }
            public List<string> Issues { get; set; } = new();
        }

        private class MigrationResult
        {
            public int TotalItems { get; set; }
            public int SuccessfulItems { get; set; }
            public int FailedItems { get; set; }
            public bool IsSuccessful { get; set; }
            public List<string> SuccessMessages { get; set; } = new();
            public List<string> ErrorMessages { get; set; } = new();
        }

        public class TestEmailOperationsRequest
        {
            public string TestEmail { get; set; } = string.Empty;
        }

        #endregion
    }
}