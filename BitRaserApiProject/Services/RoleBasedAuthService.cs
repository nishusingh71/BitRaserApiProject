using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;

namespace BitRaserApiProject.Services
{
    public class RoleBasedAuthService : IRoleBasedAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<RoleBasedAuthService> _logger;

        public RoleBasedAuthService(ApplicationDbContext context, ILogger<RoleBasedAuthService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> HasPermissionAsync(string email, string permissionName, bool isSubuser = false)
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

                    if (subuser == null) return false;

                    return subuser.SubuserRoles
                        .SelectMany(sr => sr.Role.RolePermissions)
                        .Any(rp => rp.Permission.PermissionName == permissionName || rp.Permission.PermissionName == "FullAccess");
                }
                else
                {
                    var user = await _context.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .ThenInclude(r => r.RolePermissions)
                        .ThenInclude(rp => rp.Permission)
                        .FirstOrDefaultAsync(u => u.user_email == email);

                    if (user == null) return false;

                    // If no roles assigned, treat as SuperAdmin (first user created gets SuperAdmin by default)
                    if (!user.UserRoles.Any())
                    {
                        await AssignDefaultSuperAdminRoleAsync(user);
                        user = await _context.Users
                            .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                            .FirstOrDefaultAsync(u => u.user_email == email);
                    }

                    return user.UserRoles
                        .SelectMany(ur => ur.Role.RolePermissions)
                        .Any(rp => rp.Permission.PermissionName == permissionName || rp.Permission.PermissionName == "FullAccess");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for user {Email}", permissionName, email);
                return false;
            }
        }

        public async Task<bool> CanAccessRouteAsync(string email, string routePath, string httpMethod, bool isSubuser = false)
        {
            try
            {
                // Get user permissions
                var permissions = await GetUserPermissionsAsync(email, isSubuser);
                
                // Check if user has FullAccess
                if (permissions.Contains("FullAccess"))
                    return true;

                // Check specific route permissions
                var route = await _context.Routes
                    .Include(r => r.PermissionRoutes)
                    .ThenInclude(pr => pr.Permission)
                    .FirstOrDefaultAsync(r => r.RoutePath == routePath && r.HttpMethod.ToUpper() == httpMethod.ToUpper());

                if (route == null)
                {
                    // If route is not configured, allow only SuperAdmins
                    return await IsSuperAdminAsync(email, isSubuser);
                }

                // Check if user has any of the required permissions for this route
                var requiredPermissions = route.PermissionRoutes.Select(pr => pr.Permission.PermissionName);
                return requiredPermissions.Any(rp => permissions.Contains(rp));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking route access for user {Email} on {Method} {Route}", email, httpMethod, routePath);
                return false;
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

                    if (subuser == null) return new List<string>();

                    return subuser.SubuserRoles
                        .SelectMany(sr => sr.Role.RolePermissions)
                        .Select(rp => rp.Permission.PermissionName)
                        .Distinct()
                        .ToList();
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

                    // If no roles assigned, assign SuperAdmin to first user
                    if (!user.UserRoles.Any())
                    {
                        await AssignDefaultSuperAdminRoleAsync(user);
                        user = await _context.Users
                            .Include(u => u.UserRoles)
                            .ThenInclude(ur => ur.Role)
                            .ThenInclude(r => r.RolePermissions)
                            .ThenInclude(rp => rp.Permission)
                            .FirstOrDefaultAsync(u => u.user_email == email);
                    }

                    return user.UserRoles
                        .SelectMany(ur => ur.Role.RolePermissions)
                        .Select(rp => rp.Permission.PermissionName)
                        .Distinct()
                        .ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for user {Email}", email);
                return new List<string>();
            }
        }

        public async Task<IEnumerable<string>> GetUserRolesAsync(string email, bool isSubuser = false)
        {
            try
            {
                if (isSubuser)
                {
                    var subuser = await _context.subuser
                        .Include(s => s.SubuserRoles)
                        .ThenInclude(sr => sr.Role)
                        .FirstOrDefaultAsync(s => s.subuser_email == email);

                    if (subuser == null) return new List<string>();

                    return subuser.SubuserRoles.Select(sr => sr.Role.RoleName).ToList();
                }
                else
                {
                    var user = await _context.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.user_email == email);

                    if (user == null) return new List<string>();

                    return user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting roles for user {Email}", email);
                return new List<string>();
            }
        }

        public async Task<bool> AssignRoleToUserAsync(int userId, int roleId, string assignedByEmail)
        {
            try
            {
                // Check if the assigner has permission to assign roles
                var canAssign = await HasPermissionAsync(assignedByEmail, "UserManagement");
                if (!canAssign) return false;

                // Check if role assignment already exists
                var existingAssignment = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (existingAssignment != null) return true; // Already assigned

                var userRole = new UserRole
                {
                    UserId = userId,
                    RoleId = roleId,
                    AssignedByEmail = assignedByEmail,
                    AssignedAt = DateTime.UtcNow
                };

                _context.UserRoles.Add(userRole);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to user {UserId}", roleId, userId);
                return false;
            }
        }

        public async Task<bool> AssignRoleToSubuserAsync(int subuserId, int roleId, string assignedByEmail)
        {
            try
            {
                // Check if the assigner has permission and can manage this subuser
                var canAssign = await HasPermissionAsync(assignedByEmail, "UserManagement");
                if (!canAssign) return false;

                // Check if role assignment already exists
                var existingAssignment = await _context.SubuserRoles
                    .FirstOrDefaultAsync(sr => sr.SubuserId == subuserId && sr.RoleId == roleId);

                if (existingAssignment != null) return true; // Already assigned

                var subuserRole = new SubuserRole
                {
                    SubuserId = subuserId,
                    RoleId = roleId,
                    AssignedByEmail = assignedByEmail,
                    AssignedAt = DateTime.UtcNow
                };

                _context.SubuserRoles.Add(subuserRole);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role {RoleId} to subuser {SubuserId}", roleId, subuserId);
                return false;
            }
        }

        public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
        {
            try
            {
                var userRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId);

                if (userRole == null) return false;

                _context.UserRoles.Remove(userRole);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from user {UserId}", roleId, userId);
                return false;
            }
        }

        public async Task<bool> RemoveRoleFromSubuserAsync(int subuserId, int roleId)
        {
            try
            {
                var subuserRole = await _context.SubuserRoles
                    .FirstOrDefaultAsync(sr => sr.SubuserId == subuserId && sr.RoleId == roleId);

                if (subuserRole == null) return false;

                _context.SubuserRoles.Remove(subuserRole);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role {RoleId} from subuser {SubuserId}", roleId, subuserId);
                return false;
            }
        }

        public async Task<bool> IsSuperAdminAsync(string email, bool isSubuser = false)
        {
            var roles = await GetUserRolesAsync(email, isSubuser);
            return roles.Contains("SuperAdmin");
        }

        public async Task<int> GetUserHierarchyLevelAsync(string email, bool isSubuser = false)
        {
            try
            {
                if (isSubuser)
                {
                    var subuser = await _context.subuser
                        .Include(s => s.SubuserRoles)
                        .ThenInclude(sr => sr.Role)
                        .FirstOrDefaultAsync(s => s.subuser_email == email);

                    if (subuser == null || !subuser.SubuserRoles.Any()) return int.MaxValue;

                    return subuser.SubuserRoles.Min(sr => sr.Role.HierarchyLevel);
                }
                else
                {
                    var user = await _context.Users
                        .Include(u => u.UserRoles)
                        .ThenInclude(ur => ur.Role)
                        .FirstOrDefaultAsync(u => u.user_email == email);

                    if (user == null || !user.UserRoles.Any()) return 1; // Default SuperAdmin for first user

                    return user.UserRoles.Min(ur => ur.Role.HierarchyLevel);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting hierarchy level for user {Email}", email);
                return int.MaxValue;
            }
        }

        public async Task<bool> CanManageUserAsync(string managerEmail, string targetUserEmail, bool isTargetSubuser = false)
        {
            try
            {
                var managerLevel = await GetUserHierarchyLevelAsync(managerEmail, false);
                var targetLevel = await GetUserHierarchyLevelAsync(targetUserEmail, isTargetSubuser);

                // Manager can only manage users with higher hierarchy level (lower privilege)
                return managerLevel < targetLevel;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if {Manager} can manage {Target}", managerEmail, targetUserEmail);
                return false;
            }
        }

        private async Task AssignDefaultSuperAdminRoleAsync(users user)
        {
            try
            {
                // Check if this is the first user in the system
                var userCount = await _context.Users.CountAsync();
                if (userCount == 1) // This is the first user
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
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning default SuperAdmin role to user {UserId}", user.user_id);
            }
        }
    }
}