using BitRaserApiProject.Models;

namespace BitRaserApiProject.Services
{
    public interface IRoleBasedAuthService
    {
        Task<bool> HasPermissionAsync(string email, string permissionName, bool isSubuser = false);
        Task<bool> CanAccessRouteAsync(string email, string routePath, string httpMethod, bool isSubuser = false);
        Task<IEnumerable<string>> GetUserPermissionsAsync(string email, bool isSubuser = false, string? parentUserEmail = null);
        Task<IEnumerable<string>> GetUserRolesAsync(string email, bool isSubuser = false, string? parentUserEmail = null);
        Task<bool> AssignRoleToUserAsync(int userId, int roleId, string assignedByEmail);
        Task<bool> AssignRoleToSubuserAsync(int subuserId, int roleId, string assignedByEmail);
        Task<bool> RemoveRoleFromUserAsync(int userId, int roleId);
        Task<bool> RemoveRoleFromSubuserAsync(int subuserId, int roleId);
        Task<bool> IsSuperAdminAsync(string email, bool isSubuser = false);
        Task<int> GetUserHierarchyLevelAsync(string email, bool isSubuser = false);
        Task<bool> CanManageUserAsync(string managerEmail, string targetUserEmail, bool isTargetSubuser = false);
        Task<bool> CanAssignRoleAsync(string assignerEmail, string roleName);
        Task<bool> CanCreateSubusersAsync(string userEmail);
        Task<List<string>> GetManagedUserEmailsAsync(string managerEmail);

        // ✅ NEW: Permission Management Methods
        /// <summary>
        /// Add permission to a role (SuperAdmin/Admin only)
        /// </summary>
        Task<bool> AddPermissionToRoleAsync(string roleName, string permissionName, string modifiedByEmail);

        /// <summary>
        /// Remove permission from a role (SuperAdmin/Admin only)
        /// </summary>
        Task<bool> RemovePermissionFromRoleAsync(string roleName, string permissionName, string modifiedByEmail);

        /// <summary>
        /// Get all permissions for a specific role
        /// </summary>
        Task<IEnumerable<string>> GetRolePermissionsAsync(string roleName);

        /// <summary>
        /// Get all available permissions in the system
        /// </summary>
        Task<IEnumerable<Permission>> GetAllPermissionsAsync();

        /// <summary>
        /// Check if user can modify permissions for a specific role
        /// </summary>
        Task<bool> CanModifyRolePermissionsAsync(string userEmail, string targetRoleName);

        /// <summary>
        /// Update all permissions for a role (replace existing with new set)
        /// </summary>
        Task<bool> UpdateRolePermissionsAsync(string roleName, List<string> permissionNames, string modifiedByEmail);
    }
}