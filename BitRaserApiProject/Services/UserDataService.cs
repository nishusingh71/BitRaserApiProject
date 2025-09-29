using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using System.Text.RegularExpressions;
using System.Security.Cryptography;
using System.Text;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Implementation of dynamic, email-based user data operations
    /// This service eliminates hardcoded IDs and provides a unified interface for all data operations
    /// </summary>
    public class UserDataService : IUserDataService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserDataService> _logger;

        public UserDataService(ApplicationDbContext context, ILogger<UserDataService> logger)
        {
            _context = context;
            _logger = logger;
        }

        #region User Operations
        
        public async Task<users?> GetUserByEmailAsync(string email)
        {
            try
            {
                return await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by email: {Email}", email);
                return null;
            }
        }

        public async Task<int?> GetUserIdByEmailAsync(string email)
        {
            try
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
                return user?.user_id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user ID by email: {Email}", email);
                return null;
            }
        }

        public async Task<bool> UserExistsAsync(string email)
        {
            try
            {
                return await _context.Users.AnyAsync(u => u.user_email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if user exists: {Email}", email);
                return false;
            }
        }

        #endregion

        #region Subuser Operations

        public async Task<subuser?> GetSubuserByEmailAsync(string subuserEmail)
        {
            try
            {
                return await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == subuserEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subuser by email: {Email}", subuserEmail);
                return null;
            }
        }

        public async Task<int?> GetSubuserIdByEmailAsync(string subuserEmail)
        {
            try
            {
                var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == subuserEmail);
                return subuser?.subuser_id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subuser ID by email: {Email}", subuserEmail);
                return null;
            }
        }

        public async Task<IEnumerable<subuser>> GetSubusersByParentEmailAsync(string parentEmail)
        {
            try
            {
                return await _context.subuser.Where(s => s.user_email == parentEmail).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subusers by parent email: {Email}", parentEmail);
                return new List<subuser>();
            }
        }

        public async Task<bool> SubuserExistsAsync(string subuserEmail)
        {
            try
            {
                return await _context.subuser.AnyAsync(s => s.subuser_email == subuserEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if subuser exists: {Email}", subuserEmail);
                return false;
            }
        }

        public async Task<bool> IsSubuserOfUserAsync(string subuserEmail, string parentEmail)
        {
            try
            {
                return await _context.subuser.AnyAsync(s => s.subuser_email == subuserEmail && s.user_email == parentEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subuser relationship: {Subuser} -> {Parent}", subuserEmail, parentEmail);
                return false;
            }
        }

        #endregion

        #region Machine Operations

        public async Task<IEnumerable<machines>> GetMachinesByUserEmailAsync(string email)
        {
            try
            {
                // Get machines for user and their subusers
                var userMachines = await _context.Machines.Where(m => m.user_email == email).ToListAsync();
                var subuserMachines = await _context.Machines.Where(m => m.subuser_email == email).ToListAsync();
                
                return userMachines.Concat(subuserMachines).Distinct().ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machines by user email: {Email}", email);
                return new List<machines>();
            }
        }

        public async Task<machines?> GetMachineByHashAsync(string hash)
        {
            try
            {
                return await _context.Machines.FirstOrDefaultAsync(m => m.fingerprint_hash == hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machine by hash: {Hash}", hash);
                return null;
            }
        }

        public async Task<machines?> GetMachineByMacAsync(string macAddress)
        {
            try
            {
                return await _context.Machines.FirstOrDefaultAsync(m => m.mac_address == macAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting machine by MAC: {MAC}", macAddress);
                return null;
            }
        }

        #endregion

        #region Audit Report Operations

        public async Task<IEnumerable<audit_reports>> GetAuditReportsByEmailAsync(string email)
        {
            try
            {
                return await _context.AuditReports.Where(r => r.client_email == email).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit reports by email: {Email}", email);
                return new List<audit_reports>();
            }
        }

        public async Task<audit_reports?> GetAuditReportByIdAsync(int reportId)
        {
            try
            {
                return await _context.AuditReports.FindAsync(reportId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit report by ID: {ID}", reportId);
                return null;
            }
        }

        #endregion

        #region Session Operations

        public async Task<IEnumerable<Sessions>> GetSessionsByEmailAsync(string email)
        {
            try
            {
                return await _context.Sessions.Where(s => s.user_email == email).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting sessions by email: {Email}", email);
                return new List<Sessions>();
            }
        }

        public async Task<Sessions?> GetSessionByIdAndEmailAsync(int sessionId, string email)
        {
            try
            {
                return await _context.Sessions.FirstOrDefaultAsync(s => s.session_id == sessionId && s.user_email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting session by ID and email: {ID}, {Email}", sessionId, email);
                return null;
            }
        }

        #endregion

        #region Log Operations

        public async Task<IEnumerable<logs>> GetLogsByEmailAsync(string email)
        {
            try
            {
                return await _context.logs.Where(l => l.user_email == email).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting logs by email: {Email}", email);
                return new List<logs>();
            }
        }

        public async Task<logs?> GetLogByIdAndEmailAsync(int logId, string email)
        {
            try
            {
                return await _context.logs.FirstOrDefaultAsync(l => l.log_id == logId && l.user_email == email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting log by ID and email: {ID}, {Email}", logId, email);
                return null;
            }
        }

        #endregion

        #region Permission and Access Control

        public async Task<bool> CanUserAccessDataAsync(string requesterEmail, string targetEmail)
        {
            try
            {
                // User can always access their own data
                if (requesterEmail == targetEmail)
                    return true;

                // Check if requester is the parent of the target subuser
                return await IsSubuserOfUserAsync(targetEmail, requesterEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking access: {Requester} -> {Target}", requesterEmail, targetEmail);
                return false;
            }
        }

        public async Task<bool> IsUserAuthorizedForOperationAsync(string userEmail, string operation, string? resourceOwner = null)
        {
            try
            {
                // Check if user has the required permission
                var hasPermission = await HasPermissionAsync(userEmail, operation);
                
                if (!hasPermission)
                    return false;

                // If resource owner is specified, check if user can access that resource
                if (!string.IsNullOrEmpty(resourceOwner))
                {
                    return await CanUserAccessDataAsync(userEmail, resourceOwner);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking authorization: {User}, {Operation}, {Resource}", userEmail, operation, resourceOwner);
                return false;
            }
        }

        #endregion

        #region Dynamic Role Operations

        public async Task<IEnumerable<string>> GetUserRoleNamesAsync(string email, bool isSubuser = false)
        {
            try
            {
                if (isSubuser)
                {
                    var subuser = await _context.subuser
                        .Include(s => s.SubuserRoles)
                        .ThenInclude(sr => sr.Role)
                        .FirstOrDefaultAsync(s => s.subuser_email == email);

                    return subuser?.SubuserRoles.Select(sr => sr.Role.RoleName).ToList() ?? new List<string>();
                }
                else
                {
                    var user = await _context.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.user_email == email);

                    if (user == null) return new List<string>();

                    // Auto-assign SuperAdmin to first user if no roles
                    if (!user.UserRoles.Any())
                    {
                        await AssignDefaultRoleIfNeededAsync(user);
                        // Reload user with roles
                        user = await _context.Users
                            .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                            .FirstOrDefaultAsync(u => u.user_email == email);
                    }

                    return user?.UserRoles.Select(ur => ur.Role.RoleName).ToList() ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user roles: {Email}", email);
                return new List<string>();
            }
        }

        public async Task<IEnumerable<string>> GetUserPermissionsAsync(string email, bool isSubuser = false)
        {
            try
            {
                if (isSubuser)
                {
                    var subuser = await _context.subuser
                        .Include(s => s.SubuserRoles)
                        .ThenInclude(sr => sr.Role)
                        .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                        .FirstOrDefaultAsync(s => s.subuser_email == email);

                    return subuser?.SubuserRoles
                        .SelectMany(sr => sr.Role.RolePermissions)
                        .Select(rp => rp.Permission.PermissionName)
                        .Distinct()
                        .ToList() ?? new List<string>();
                }
                else
                {
                    var user = await _context.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                        .FirstOrDefaultAsync(u => u.user_email == email);

                    if (user == null) return new List<string>();

                    if (!user.UserRoles.Any())
                    {
                        await AssignDefaultRoleIfNeededAsync(user);
                        user = await _context.Users
                            .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                            .FirstOrDefaultAsync(u => u.user_email == email);
                    }

                    return user?.UserRoles
                        .SelectMany(ur => ur.Role.RolePermissions)
                        .Select(rp => rp.Permission.PermissionName)
                        .Distinct()
                        .ToList() ?? new List<string>();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions: {Email}", email);
                return new List<string>();
            }
        }

        public async Task<bool> HasPermissionAsync(string email, string permission, bool isSubuser = false)
        {
            try
            {
                var permissions = await GetUserPermissionsAsync(email, isSubuser);
                return permissions.Contains(permission) || permissions.Contains("FullAccess");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission: {Email}, {Permission}", email, permission);
                return false;
            }
        }

        #endregion

        #region Role Assignment Operations

        public async Task<bool> AssignRoleByEmailAsync(string userEmail, string roleName, string assignedByEmail, bool isSubuser = false)
        {
            try
            {
                // Check if assigner has permission
                if (!await HasPermissionAsync(assignedByEmail, "UserManagement"))
                    return false;

                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                if (role == null) return false;

                if (isSubuser)
                {
                    var subuserId = await GetSubuserIdByEmailAsync(userEmail);
                    if (!subuserId.HasValue) return false;

                    // Check if already assigned
                    if (await _context.SubuserRoles.AnyAsync(sr => sr.SubuserId == subuserId.Value && sr.RoleId == role.RoleId))
                        return true;

                    var subuserRole = new SubuserRole
                    {
                        SubuserId = subuserId.Value,
                        RoleId = role.RoleId,
                        AssignedByEmail = assignedByEmail,
                        AssignedAt = DateTime.UtcNow
                    };

                    _context.SubuserRoles.Add(subuserRole);
                }
                else
                {
                    var userId = await GetUserIdByEmailAsync(userEmail);
                    if (!userId.HasValue) return false;

                    // Check if already assigned
                    if (await _context.UserRoles.AnyAsync(ur => ur.UserId == userId.Value && ur.RoleId == role.RoleId))
                        return true;

                    var userRole = new UserRole
                    {
                        UserId = userId.Value,
                        RoleId = role.RoleId,
                        AssignedByEmail = assignedByEmail,
                        AssignedAt = DateTime.UtcNow
                    };

                    _context.UserRoles.Add(userRole);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role: {User}, {Role}", userEmail, roleName);
                return false;
            }
        }

        public async Task<bool> RemoveRoleByEmailAsync(string userEmail, string roleName, bool isSubuser = false)
        {
            try
            {
                var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                if (role == null) return false;

                if (isSubuser)
                {
                    var subuserId = await GetSubuserIdByEmailAsync(userEmail);
                    if (!subuserId.HasValue) return false;

                    var subuserRole = await _context.SubuserRoles
                        .FirstOrDefaultAsync(sr => sr.SubuserId == subuserId.Value && sr.RoleId == role.RoleId);

                    if (subuserRole == null) return false;

                    _context.SubuserRoles.Remove(subuserRole);
                }
                else
                {
                    var userId = await GetUserIdByEmailAsync(userEmail);
                    if (!userId.HasValue) return false;

                    var userRole = await _context.UserRoles
                        .FirstOrDefaultAsync(ur => ur.UserId == userId.Value && ur.RoleId == role.RoleId);

                    if (userRole == null) return false;

                    _context.UserRoles.Remove(userRole);
                }

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role: {User}, {Role}", userEmail, roleName);
                return false;
            }
        }

        public async Task<IEnumerable<Role>> GetAvailableRolesForUserAsync(string requesterEmail)
        {
            try
            {
                // Get requester's hierarchy level
                var requesterRoles = await GetUserRoleNamesAsync(requesterEmail);
                
                if (requesterRoles.Contains("SuperAdmin"))
                {
                    // SuperAdmin can assign any role
                    return await _context.Roles.ToListAsync();
                }

                // Get the minimum hierarchy level of the requester
                var requesterLevel = int.MaxValue;
                foreach (var roleName in requesterRoles)
                {
                    var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                    if (role != null && role.HierarchyLevel < requesterLevel)
                    {
                        requesterLevel = role.HierarchyLevel;
                    }
                }

                // Return roles with higher hierarchy level (lower privilege)
                return await _context.Roles.Where(r => r.HierarchyLevel > requesterLevel).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available roles for user: {Email}", requesterEmail);
                return new List<Role>();
            }
        }

        #endregion

        #region Utility Methods

        public async Task<bool> ValidateEmailFormatAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                // Basic email validation regex
                var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return emailRegex.IsMatch(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating email format: {Email}", email);
                return false;
            }
        }

        public async Task<string> GenerateUniqueIdentifierAsync(string baseString)
        {
            try
            {
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(baseString + DateTime.UtcNow.Ticks));
                return Convert.ToBase64String(hash)[0..16]; // Return first 16 characters
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating unique identifier for: {Base}", baseString);
                return Guid.NewGuid().ToString("N")[0..16];
            }
        }

        #endregion

        #region Private Helper Methods

        private async Task AssignDefaultRoleIfNeededAsync(users user)
        {
            try
            {
                var userCount = await _context.Users.CountAsync();
                if (userCount == 1) // First user gets SuperAdmin
                {
                    var superAdminRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "SuperAdmin");
                    if (superAdminRole != null)
                    {
                        var userRole = new UserRole
                        {
                            UserId = user.user_id,
                            RoleId = superAdminRole.RoleId,
                            AssignedByEmail = "system",
                            AssignedAt = DateTime.UtcNow
                        };

                        _context.UserRoles.Add(userRole);
                        await _context.SaveChangesAsync();
                    }
                }
                else
                {
                    // Default users get basic "User" role
                    var userRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "User");
                    if (userRole != null)
                    {
                        var newUserRole = new UserRole
                        {
                            UserId = user.user_id,
                            RoleId = userRole.RoleId,
                            AssignedByEmail = "system",
                            AssignedAt = DateTime.UtcNow
                        };

                        _context.UserRoles.Add(newUserRole);
                        await _context.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning default role to user: {UserId}", user.user_id);
            }
        }

        #endregion
    }
}