using BitRaserApiProject.Models;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Service for logging user activities in Sessions table
    /// </summary>
    public interface IActivityLogService
    {
        /// <summary>
        /// Log a user activity
        /// </summary>
        Task LogActivityAsync(
            string userEmail, 
            string activityType, 
            string? resourceId = null, 
            string? resourceType = null, 
            object? details = null,
            string? ipAddress = null);

        /// <summary>
        /// Get recent activities for a user
        /// </summary>
        Task<List<Sessions>> GetUserActivitiesAsync(string userEmail, int limit = 50);

        /// <summary>
        /// Get activities by type
        /// </summary>
        Task<List<Sessions>> GetActivitiesByTypeAsync(string activityType, int limit = 50);
    }

    /// <summary>
    /// Activity types for logging
    /// </summary>
    public static class ActivityTypes
    {
        // Authentication
        public const string LOGIN = "LOGIN";
        public const string LOGOUT = "LOGOUT";
        public const string LOGIN_FAILED = "LOGIN_FAILED";

        // Audit Reports
        public const string REPORT_DOWNLOAD = "REPORT_DOWNLOAD";
        public const string REPORT_VIEW = "REPORT_VIEW";
        public const string REPORT_CREATE = "REPORT_CREATE";
        public const string REPORT_DELETE = "REPORT_DELETE";

        // Machines
        public const string MACHINE_CREATE = "MACHINE_CREATE";
        public const string MACHINE_UPDATE = "MACHINE_UPDATE";
        public const string MACHINE_DELETE = "MACHINE_DELETE";
        public const string MACHINE_ACTIVATE = "MACHINE_ACTIVATE";

        // Subusers
        public const string SUBUSER_CREATE = "SUBUSER_CREATE";
        public const string SUBUSER_UPDATE = "SUBUSER_UPDATE";
        public const string SUBUSER_DELETE = "SUBUSER_DELETE";
        public const string SUBUSER_PASSWORD_RESET = "SUBUSER_PASSWORD_RESET";

        // Groups & Departments
        public const string GROUP_CREATE = "GROUP_CREATE";
        public const string GROUP_UPDATE = "GROUP_UPDATE";
        public const string DEPARTMENT_ASSIGN = "DEPARTMENT_ASSIGN";
        public const string DEPARTMENT_REASSIGN = "DEPARTMENT_REASSIGN";

        // Licenses
        public const string LICENSE_ALLOCATE = "LICENSE_ALLOCATE";
        public const string LICENSE_TRANSFER = "LICENSE_TRANSFER";
        public const string LICENSE_REVOKE = "LICENSE_REVOKE";

        // Quota
        public const string QUOTA_VIEW = "QUOTA_VIEW";
        public const string QUOTA_UPDATE = "QUOTA_UPDATE";
    }
}
