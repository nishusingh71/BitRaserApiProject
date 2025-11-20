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
                // SuperAdmin can manage everyone
                if (await IsSuperAdminAsync(managerEmail, false))
                    return true;

                var managerLevel = await GetUserHierarchyLevelAsync(managerEmail, false);
                var targetLevel = await GetUserHierarchyLevelAsync(targetUserEmail, isTargetSubuser);

                 // ✅ STRICT HIERARCHY: Manager can ONLY manage users with HIGHER hierarchy level (LOWER privilege)
               // Users CANNOT manage users at the same level or higher privilege
                // Example: Admin (level 2) can manage Manager (level 3), Support (level 4), User (level 5)
                //         Admin (level 2) CANNOT manage another Admin (level 2) or SuperAdmin (level 1)
                 if (managerLevel >= targetLevel)
                 return false;

                // ✅ If target is a subuser, verify it belongs to the manager or manager can access its parent
                if (isTargetSubuser)
                {
                     var subuser = await _context.subuser
                   .FirstOrDefaultAsync(s => s.subuser_email == targetUserEmail);
                 
                    if (subuser == null) return false;

                 // Manager can manage subuser if:
                // 1. Subuser belongs to manager directly
               // 2. Manager can manage the parent user
               if (subuser.user_email == managerEmail)
                  return true;

                return await CanManageUserAsync(managerEmail, subuser.user_email, false);
                }

            return true;
            }
            catch (Exception ex)
            {
          _logger.LogError(ex, "Error checking if {Manager} can manage {Target}", managerEmail, targetUserEmail);
    return false;
   }
        }

  /// <summary>
        /// Check if user can create a user/subuser with specific role
        /// </summary>
        public async Task<bool> CanAssignRoleAsync(string assignerEmail, string roleName)
        {
            try
     {
       // SuperAdmin can assign any role
           if (await IsSuperAdminAsync(assignerEmail, false))
    return true;

            var assignerLevel = await GetUserHierarchyLevelAsync(assignerEmail, false);
 var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
        
 if (role == null) return false;

                // ✅ Can only assign roles with HIGHER hierarchy level (LOWER privilege)
       // Example: Admin (level 2) can assign Manager (3), Support (4), User (5)
     //         Admin (level 2) CANNOT assign Admin (2) or SuperAdmin (1)
            return assignerLevel < role.HierarchyLevel;
            }
      catch (Exception ex)
       {
             _logger.LogError(ex, "Error checking if {Assigner} can assign role {Role}", assignerEmail, roleName);
       return false;
    }
   }

/// <summary>
        /// Check if user can create subusers (User role cannot)
      /// </summary>
     public async Task<bool> CanCreateSubusersAsync(string userEmail)
        {
        try
       {
   // ✅ First check if this is a subuser
 var isSubuser = await _context.subuser.AnyAsync(s => s.subuser_email == userEmail);
        
      // ✅ Get roles based on user type
        var roles = await GetUserRolesAsync(userEmail, isSubuser);
        
        // ✅ "User" role cannot create subusers (both for Users and Subusers)
    // If ONLY "User" role is assigned, deny permission
     if (roles.Contains("User") && !roles.Any(r => r != "User"))
            return false;
        
        // ✅ All other roles can create subusers (Manager, Support, Admin, SuperAdmin, etc.)
        // Check if they have the required permission
        return await HasPermissionAsync(userEmail, "UserManagement", isSubuser) ||
       await HasPermissionAsync(userEmail, "CREATE_SUBUSER", isSubuser);
 }
            catch (Exception ex)
 {
          _logger.LogError(ex, "Error checking if {User} can create subusers", userEmail);
return false;
            }
        }

        /// <summary>
        /// Get all users/subusers that a manager can access
        /// </summary>
        public async Task<List<string>> GetManagedUserEmailsAsync(string managerEmail)
        {
  try
        {
    var managedEmails = new List<string>();
        
// SuperAdmin can access all users
         if (await IsSuperAdminAsync(managerEmail, false))
                {
        var allUsers = await _context.Users.Select(u => u.user_email).ToListAsync();
        var allSubusers = await _context.subuser.Select(s => s.subuser_email).ToListAsync();
          managedEmails.AddRange(allUsers);
        managedEmails.AddRange(allSubusers);
return managedEmails;
            }

      var managerLevel = await GetUserHierarchyLevelAsync(managerEmail, false);

       // Get all users with lower privilege (higher hierarchy level)
      var managedUsers = await _context.Users
            .Include(u => u.UserRoles)
               .ThenInclude(ur => ur.Role)
           .Where(u => u.UserRoles.Any() && u.UserRoles.Min(ur => ur.Role.HierarchyLevel) > managerLevel)
  .Select(u => u.user_email)
  .ToListAsync();

        managedEmails.AddRange(managedUsers);

   // Get direct subusers
     var directSubusers = await _context.subuser
        .Where(s => s.user_email == managerEmail)
        .Select(s => s.subuser_email)
                .ToListAsync();

                managedEmails.AddRange(directSubusers);

    // Get subusers of managed users
         var managedUsersSubusers = await _context.subuser
         .Where(s => managedUsers.Contains(s.user_email))
   .Select(s => s.subuser_email)
        .ToListAsync();

    managedEmails.AddRange(managedUsersSubusers);

         return managedEmails.Distinct().ToList();
   }
     catch (Exception ex)
      {
      _logger.LogError(ex, "Error getting managed users for {Manager}", managerEmail);
  return new List<string>();
   }
   }

        // ✅ NEW: Permission Management Methods

        /// <summary>
        /// Add permission to a role (SuperAdmin/Admin can modify lower-level roles)
     /// </summary>
        public async Task<bool> AddPermissionToRoleAsync(string roleName, string permissionName, string modifiedByEmail)
        {
            try
 {
        // Check if modifier can modify this role's permissions
             if (!await CanModifyRolePermissionsAsync(modifiedByEmail, roleName))
    {
        _logger.LogWarning("User {Email} attempted to modify role {Role} without permission", modifiedByEmail, roleName);
    return false;
   }

        var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.PermissionName == permissionName);

   if (role == null || permission == null)
     {
   _logger.LogWarning("Role {Role} or Permission {Permission} not found", roleName, permissionName);
 return false;
                }

         // Check if permission already exists
              var existingRolePermission = await _context.RolePermissions
    .FirstOrDefaultAsync(rp => rp.RoleId == role.RoleId && rp.PermissionId == permission.PermissionId);

        if (existingRolePermission != null)
    {
          _logger.LogInformation("Permission {Permission} already exists for role {Role}", permissionName, roleName);
    return true; // Already exists
     }

      // Add new permission to role
          var rolePermission = new RolePermission
           {
        RoleId = role.RoleId,
             PermissionId = permission.PermissionId
    };

     _context.RolePermissions.Add(rolePermission);
     await _context.SaveChangesAsync();

    _logger.LogInformation("Permission {Permission} added to role {Role} by {User}", permissionName, roleName, modifiedByEmail);
  return true;
     }
        catch (Exception ex)
            {
     _logger.LogError(ex, "Error adding permission {Permission} to role {Role}", permissionName, roleName);
        return false;
        }
      }

        /// <summary>
 /// Remove permission from a role
        /// </summary>
        public async Task<bool> RemovePermissionFromRoleAsync(string roleName, string permissionName, string modifiedByEmail)
{
 try
            {
 // Check if modifier can modify this role's permissions
         if (!await CanModifyRolePermissionsAsync(modifiedByEmail, roleName))
              {
            _logger.LogWarning("User {Email} attempted to modify role {Role} without permission", modifiedByEmail, roleName);
  return false;
     }

       var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
                var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.PermissionName == permissionName);

       if (role == null || permission == null)
    {
        _logger.LogWarning("Role {Role} or Permission {Permission} not found", roleName, permissionName);
  return false;
}

       // Find and remove the role-permission mapping
                var rolePermission = await _context.RolePermissions
    .FirstOrDefaultAsync(rp => rp.RoleId == role.RoleId && rp.PermissionId == permission.PermissionId);

  if (rolePermission == null)
           {
     _logger.LogInformation("Permission {Permission} not found for role {Role}", permissionName, roleName);
        return false;
    }

  _context.RolePermissions.Remove(rolePermission);
     await _context.SaveChangesAsync();

          _logger.LogInformation("Permission {Permission} removed from role {Role} by {User}", permissionName, roleName, modifiedByEmail);
           return true;
          }
 catch (Exception ex)
  {
       _logger.LogError(ex, "Error removing permission {Permission} from role {Role}", permissionName, roleName);
           return false;
   }
        }

        /// <summary>
        /// Get all permissions for a specific role
        /// </summary>
      public async Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName)
        {
 try
    {
                var role = await _context.Roles
        .Include(r => r.RolePermissions)
    .ThenInclude(rp => rp.Permission)
         .FirstOrDefaultAsync(r => r.RoleName == roleName);

     if (role == null)
            return new List<string>();

    return role.RolePermissions
   .Select(rp => rp.Permission.PermissionName)
    .Distinct()
                    .ToList();
    }
     catch (Exception ex)
            {
     _logger.LogError(ex, "Error getting permissions for role {Role}", roleName);
            return new List<string>();
      }
        }

     /// <summary>
        /// Get all available permissions in the system
        /// </summary>
     public async Task<IEnumerable<Permission>> GetAllPermissionsAsync()
        {
   try
 {
                return await _context.Permissions
                 .OrderBy(p => p.PermissionName)
   .ToListAsync();
      }
            catch (Exception ex)
     {
         _logger.LogError(ex, "Error getting all permissions");
return new List<Permission>();
            }
        }

        /// <summary>
        /// Check if user can modify permissions for a specific role
        /// Rules:
        /// - SuperAdmin can modify any role's permissions
    /// - Admin can modify Manager, Support, User, SubUser permissions (NOT SuperAdmin)
   /// - Others cannot modify permissions
        /// </summary>
        public async Task<bool> CanModifyRolePermissionsAsync(string userEmail, string targetRoleName)
        {
            try
  {
       // SuperAdmin can modify any role's permissions
    if (await IsSuperAdminAsync(userEmail, false))
           return true;

      var userLevel = await GetUserHierarchyLevelAsync(userEmail, false);
    
            // Get target role's hierarchy level
              var targetRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == targetRoleName);
          if (targetRole == null)
          return false;

       // User can modify permissions of roles with HIGHER hierarchy level (LOWER privilege)
          // Example: Admin (level 2) can modify Manager (3), Support (4), User (5)
         //         Admin (level 2) CANNOT modify SuperAdmin (1) or Admin (2)
      
 // Special rule: Only SuperAdmin and Admin can modify permissions
     if (userLevel > 2) // If not SuperAdmin or Admin
       return false;

      return userLevel < targetRole.HierarchyLevel;
      }
   catch (Exception ex)
     {
           _logger.LogError(ex, "Error checking if {User} can modify permissions for role {Role}", userEmail, targetRoleName);
return false;
    }
        }

        /// <summary>
        /// Update all permissions for a role (replace existing with new set)
        /// </summary>
      public async Task<bool> UpdateRolePermissionsAsync(string roleName, List<string> permissionNames, string modifiedByEmail)
    {
   try
            {
    // Check if modifier can modify this role's permissions
                if (!await CanModifyRolePermissionsAsync(modifiedByEmail, roleName))
   {
         _logger.LogWarning("User {Email} attempted to modify role {Role} without permission", modifiedByEmail, roleName);
      return false;
            }

          var role = await _context.Roles
       .Include(r => r.RolePermissions)
          .FirstOrDefaultAsync(r => r.RoleName == roleName);

         if (role == null)
     {
           _logger.LogWarning("Role {Role} not found", roleName);
            return false;
           }

   // Remove all existing permissions
     _context.RolePermissions.RemoveRange(role.RolePermissions);

      // Add new permissions
       foreach (var permissionName in permissionNames)
                {
var permission = await _context.Permissions.FirstOrDefaultAsync(p => p.PermissionName == permissionName);
        if (permission != null)
        {
       var rolePermission = new RolePermission
                 {
            RoleId = role.RoleId,
     PermissionId = permission.PermissionId
     };
          _context.RolePermissions.Add(rolePermission);
            }
    else
    {
         _logger.LogWarning("Permission {Permission} not found", permissionName);
      }
             }

        await _context.SaveChangesAsync();

  _logger.LogInformation("Permissions updated for role {Role} by {User}. New permissions: {Permissions}", 
        roleName, modifiedByEmail, string.Join(", ", permissionNames));
          return true;
            }
     catch (Exception ex)
       {
 _logger.LogError(ex, "Error updating permissions for role {Role}", roleName);
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