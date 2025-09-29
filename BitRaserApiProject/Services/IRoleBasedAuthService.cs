using BitRaserApiProject.Models;

namespace BitRaserApiProject.Services
{
    public interface IRoleBasedAuthService
    {
        Task<bool> HasPermissionAsync(string email, string permissionName, bool isSubuser = false);
        Task<bool> CanAccessRouteAsync(string email, string routePath, string httpMethod, bool isSubuser = false);
        Task<IEnumerable<string>> GetUserPermissionsAsync(string email, bool isSubuser = false);
        Task<IEnumerable<string>> GetUserRolesAsync(string email, bool isSubuser = false);
        Task<bool> AssignRoleToUserAsync(int userId, int roleId, string assignedByEmail);
        Task<bool> AssignRoleToSubuserAsync(int subuserId, int roleId, string assignedByEmail);
        Task<bool> RemoveRoleFromUserAsync(int userId, int roleId);
        Task<bool> RemoveRoleFromSubuserAsync(int subuserId, int roleId);
        Task<bool> IsSuperAdminAsync(string email, bool isSubuser = false);
        Task<int> GetUserHierarchyLevelAsync(string email, bool isSubuser = false);
        Task<bool> CanManageUserAsync(string managerEmail, string targetUserEmail, bool isTargetSubuser = false);
    }
}