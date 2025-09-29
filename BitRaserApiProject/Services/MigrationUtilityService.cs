using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Migration utility service to help transition from ID-based to email-based operations
    /// This service provides helper methods to maintain backward compatibility while moving to the new system
    /// </summary>
    public class MigrationUtilityService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserDataService _userDataService;
        private readonly ILogger<MigrationUtilityService> _logger;

        public MigrationUtilityService(
            ApplicationDbContext context,
            IUserDataService userDataService,
            ILogger<MigrationUtilityService> logger)
        {
            _context = context;
            _userDataService = userDataService;
            _logger = logger;
        }

        #region ID to Email Conversion Helpers

        /// <summary>
        /// Convert user ID to email (for backward compatibility)
        /// </summary>
        public async Task<string?> GetUserEmailByIdAsync(int userId)
        {
            try
            {
                var user = await _context.Users.FindAsync(userId);
                return user?.user_email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user email by ID: {UserId}", userId);
                return null;
            }
        }

        /// <summary>
        /// Convert subuser ID to email (for backward compatibility)
        /// </summary>
        public async Task<string?> GetSubuserEmailByIdAsync(int subuserId)
        {
            try
            {
                var subuser = await _context.subuser.FindAsync(subuserId);
                return subuser?.subuser_email;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subuser email by ID: {SubuserId}", subuserId);
                return null;
            }
        }

        /// <summary>
        /// Convert role ID to role name (for backward compatibility)
        /// </summary>
        public async Task<string?> GetRoleNameByIdAsync(int roleId)
        {
            try
            {
                var role = await _context.Roles.FindAsync(roleId);
                return role?.RoleName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting role name by ID: {RoleId}", roleId);
                return null;
            }
        }

        /// <summary>
        /// Convert permission ID to permission name (for backward compatibility)
        /// </summary>
        public async Task<string?> GetPermissionNameByIdAsync(int permissionId)
        {
            try
            {
                var permission = await _context.Permissions.FindAsync(permissionId);
                return permission?.PermissionName;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permission name by ID: {PermissionId}", permissionId);
                return null;
            }
        }

        #endregion

        #region Legacy Operation Wrappers

        /// <summary>
        /// Legacy wrapper for ID-based role assignment (converts to email-based internally)
        /// </summary>
        public async Task<bool> AssignRoleToUserLegacyAsync(int userId, int roleId, string assignedByEmail)
        {
            try
            {
                var userEmail = await GetUserEmailByIdAsync(userId);
                var roleName = await GetRoleNameByIdAsync(roleId);

                if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(roleName))
                    return false;

                return await _userDataService.AssignRoleByEmailAsync(userEmail, roleName, assignedByEmail, false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in legacy role assignment: UserId={UserId}, RoleId={RoleId}", userId, roleId);
                return false;
            }
        }

        /// <summary>
        /// Legacy wrapper for ID-based subuser role assignment (converts to email-based internally)
        /// </summary>
        public async Task<bool> AssignRoleToSubuserLegacyAsync(int subuserId, int roleId, string assignedByEmail)
        {
            try
            {
                var subuserEmail = await GetSubuserEmailByIdAsync(subuserId);
                var roleName = await GetRoleNameByIdAsync(roleId);

                if (string.IsNullOrEmpty(subuserEmail) || string.IsNullOrEmpty(roleName))
                    return false;

                return await _userDataService.AssignRoleByEmailAsync(subuserEmail, roleName, assignedByEmail, true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in legacy subuser role assignment: SubuserId={SubuserId}, RoleId={RoleId}", subuserId, roleId);
                return false;
            }
        }

        #endregion

        #region Data Migration Helpers

        /// <summary>
        /// Migrate all users to have default roles if they don't have any
        /// </summary>
        public async Task<MigrationResult> MigrateUsersToDefaultRolesAsync()
        {
            var result = new MigrationResult();
            
            try
            {
                var usersWithoutRoles = await _context.Users
                    .Where(u => !u.UserRoles.Any())
                    .ToListAsync();

                result.TotalItems = usersWithoutRoles.Count;

                foreach (var user in usersWithoutRoles)
                {
                    try
                    {
                        // First user gets SuperAdmin, others get User role
                        var isFirstUser = await _context.Users.CountAsync() == 1;
                        var defaultRole = isFirstUser ? "SuperAdmin" : "User";

                        var success = await _userDataService.AssignRoleByEmailAsync(user.user_email, defaultRole, "system", false);
                        
                        if (success)
                        {
                            result.SuccessfulItems++;
                            result.SuccessMessages.Add($"Assigned '{defaultRole}' role to {user.user_email}");
                        }
                        else
                        {
                            result.FailedItems++;
                            result.ErrorMessages.Add($"Failed to assign role to {user.user_email}");
                        }
                    }
                    catch (Exception ex)
                    {
                        result.FailedItems++;
                        result.ErrorMessages.Add($"Error processing user {user.user_email}: {ex.Message}");
                        _logger.LogError(ex, "Error migrating user to default role: {Email}", user.user_email);
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"Migration failed: {ex.Message}");
                _logger.LogError(ex, "Error during user role migration");
            }

            return result;
        }

        /// <summary>
        /// Clean up orphaned role assignments (where user or role no longer exists)
        /// </summary>
        public async Task<MigrationResult> CleanupOrphanedRoleAssignmentsAsync()
        {
            var result = new MigrationResult();

            try
            {
                // Clean up UserRoles with missing users or roles
                var orphanedUserRoles = await _context.UserRoles
                    .Where(ur => _context.Users.All(u => u.user_id != ur.UserId) ||
                                _context.Roles.All(r => r.RoleId != ur.RoleId))
                    .ToListAsync();

                result.TotalItems += orphanedUserRoles.Count;
                _context.UserRoles.RemoveRange(orphanedUserRoles);
                result.SuccessfulItems += orphanedUserRoles.Count;

                // Clean up SubuserRoles with missing subusers or roles
                var orphanedSubuserRoles = await _context.SubuserRoles
                    .Where(sr => _context.subuser.All(s => s.subuser_id != sr.SubuserId) ||
                                _context.Roles.All(r => r.RoleId != sr.RoleId))
                    .ToListAsync();

                result.TotalItems += orphanedSubuserRoles.Count;
                _context.SubuserRoles.RemoveRange(orphanedSubuserRoles);
                result.SuccessfulItems += orphanedSubuserRoles.Count;

                await _context.SaveChangesAsync();

                result.SuccessMessages.Add($"Cleaned up {orphanedUserRoles.Count} orphaned user role assignments");
                result.SuccessMessages.Add($"Cleaned up {orphanedSubuserRoles.Count} orphaned subuser role assignments");
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"Cleanup failed: {ex.Message}");
                _logger.LogError(ex, "Error during orphaned role assignment cleanup");
            }

            return result;
        }

        /// <summary>
        /// Ensure all machines have proper user/subuser email associations
        /// </summary>
        public async Task<MigrationResult> ValidateMachineEmailAssociationsAsync()
        {
            var result = new MigrationResult();

            try
            {
                var allMachines = await _context.Machines.ToListAsync();
                result.TotalItems = allMachines.Count;

                foreach (var machine in allMachines)
                {
                    var issues = new List<string>();

                    // Check if user_email exists in users table
                    if (!string.IsNullOrEmpty(machine.user_email))
                    {
                        var userExists = await _userDataService.UserExistsAsync(machine.user_email);
                        if (!userExists)
                        {
                            issues.Add($"User email {machine.user_email} not found in users table");
                        }
                    }

                    // Check if subuser_email exists in subuser table
                    if (!string.IsNullOrEmpty(machine.subuser_email))
                    {
                        var subuserExists = await _userDataService.SubuserExistsAsync(machine.subuser_email);
                        if (!subuserExists)
                        {
                            issues.Add($"Subuser email {machine.subuser_email} not found in subuser table");
                        }

                        // Check if subuser belongs to the specified user
                        if (!string.IsNullOrEmpty(machine.user_email))
                        {
                            var isValidRelationship = await _userDataService.IsSubuserOfUserAsync(machine.subuser_email, machine.user_email);
                            if (!isValidRelationship)
                            {
                                issues.Add($"Subuser {machine.subuser_email} is not associated with user {machine.user_email}");
                            }
                        }
                    }

                    if (issues.Any())
                    {
                        result.FailedItems++;
                        result.ErrorMessages.Add($"Machine {machine.fingerprint_hash}: {string.Join(", ", issues)}");
                    }
                    else
                    {
                        result.SuccessfulItems++;
                    }
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessages.Add($"Validation failed: {ex.Message}");
                _logger.LogError(ex, "Error during machine email association validation");
            }

            return result;
        }

        #endregion

        #region Validation Helpers

        /// <summary>
        /// Validate that all required default roles and permissions exist
        /// </summary>
        public async Task<ValidationResult> ValidateSystemRolesAndPermissionsAsync()
        {
            var result = new ValidationResult();

            try
            {
                // Check required roles
                var requiredRoles = new[] { "SuperAdmin", "Admin", "Manager", "Support", "User" };
                var existingRoles = await _context.Roles.Select(r => r.RoleName).ToListAsync();
                var missingRoles = requiredRoles.Where(r => !existingRoles.Contains(r)).ToList();

                // Check required permissions
                var requiredPermissions = new[] { "FullAccess", "UserManagement", "ReportAccess", "MachineManagement", "ViewOnly", "LicenseManagement", "SystemLogs" };
                var existingPermissions = await _context.Permissions.Select(p => p.PermissionName).ToListAsync();
                var missingPermissions = requiredPermissions.Where(p => !existingPermissions.Contains(p)).ToList();

                result.IsValid = !missingRoles.Any() && !missingPermissions.Any();
                
                if (missingRoles.Any())
                {
                    result.ValidationMessages.Add($"Missing required roles: {string.Join(", ", missingRoles)}");
                }

                if (missingPermissions.Any())
                {
                    result.ValidationMessages.Add($"Missing required permissions: {string.Join(", ", missingPermissions)}");
                }

                if (result.IsValid)
                {
                    result.ValidationMessages.Add("All required roles and permissions exist");
                }
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ValidationMessages.Add($"Validation error: {ex.Message}");
                _logger.LogError(ex, "Error during system roles and permissions validation");
            }

            return result;
        }

        #endregion
    }

    #region Result Classes

    public class MigrationResult
    {
        public int TotalItems { get; set; }
        public int SuccessfulItems { get; set; }
        public int FailedItems { get; set; }
        public List<string> SuccessMessages { get; set; } = new List<string>();
        public List<string> ErrorMessages { get; set; } = new List<string>();
        public bool IsSuccessful => FailedItems == 0 && TotalItems > 0;
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> ValidationMessages { get; set; } = new List<string>();
    }

    #endregion
}