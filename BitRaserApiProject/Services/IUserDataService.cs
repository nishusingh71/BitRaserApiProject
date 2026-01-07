using DSecureApi.Models;

namespace DSecureApi.Services
{
    /// <summary>
    /// Service interface for dynamic, email-based user data operations
    /// Eliminates the need for hardcoded IDs by using email as the primary identifier
    /// </summary>
    public interface IUserDataService
    {
        // User operations
        Task<users?> GetUserByEmailAsync(string email);
        Task<int?> GetUserIdByEmailAsync(string email);
        Task<bool> UserExistsAsync(string email);
        
        // Subuser operations
        Task<subuser?> GetSubuserByEmailAsync(string subuserEmail);
        Task<int?> GetSubuserIdByEmailAsync(string subuserEmail);
        Task<IEnumerable<subuser>> GetSubusersByParentEmailAsync(string parentEmail);
        Task<bool> SubuserExistsAsync(string subuserEmail);
        Task<bool> IsSubuserOfUserAsync(string subuserEmail, string parentEmail);
        
        // Machine operations
        Task<IEnumerable<machines>> GetMachinesByUserEmailAsync(string email);
        Task<machines?> GetMachineByHashAsync(string hash);
        Task<machines?> GetMachineByMacAsync(string macAddress);
        
        // Audit report operations
        Task<IEnumerable<audit_reports>> GetAuditReportsByEmailAsync(string email);
        Task<audit_reports?> GetAuditReportByIdAsync(int reportId);
        
        // Session operations
        Task<IEnumerable<Sessions>> GetSessionsByEmailAsync(string email);
        Task<Sessions?> GetSessionByIdAndEmailAsync(int sessionId, string email);
        
        // Log operations
        Task<IEnumerable<logs>> GetLogsByEmailAsync(string email);
        Task<logs?> GetLogByIdAndEmailAsync(int logId, string email);
        
        // Permission and access control
        Task<bool> CanUserAccessDataAsync(string requesterEmail, string targetEmail);
        Task<bool> IsUserAuthorizedForOperationAsync(string userEmail, string operation, string? resourceOwner = null);
        
        // Dynamic role operations
        Task<IEnumerable<string>> GetUserRoleNamesAsync(string email, bool isSubuser = false);
        Task<IEnumerable<string>> GetUserPermissionsAsync(string email, bool isSubuser = false);
        Task<bool> HasPermissionAsync(string email, string permission, bool isSubuser = false);
        
        // Role assignment operations
        Task<bool> AssignRoleByEmailAsync(string userEmail, string roleName, string assignedByEmail, bool isSubuser = false);
        Task<bool> RemoveRoleByEmailAsync(string userEmail, string roleName, bool isSubuser = false);
        Task<IEnumerable<Role>> GetAvailableRolesForUserAsync(string requesterEmail);
        
        // Utility methods
        Task<bool> ValidateEmailFormatAsync(string email);
        Task<string> GenerateUniqueIdentifierAsync(string baseString);
    }
}