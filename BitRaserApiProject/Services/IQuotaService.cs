using DSecureApi.Models;

namespace DSecureApi.Services
{
    /// <summary>
    /// Service for checking and enforcing user quotas and limits
    /// </summary>
    public interface IQuotaService
    {
        /// <summary>
        /// Check if user can create more subusers
        /// </summary>
        Task<QuotaCheckResult> CanCreateSubuserAsync(string userEmail);

        /// <summary>
        /// Check if user can create more groups
        /// </summary>
        Task<QuotaCheckResult> CanCreateGroupAsync(string userEmail);

        /// <summary>
        /// Check if user has valid (non-expired) license
        /// </summary>
        Task<LicenseCheckResult> CheckLicenseValidAsync(string userEmail);

        /// <summary>
        /// Check if user can allocate more licenses
        /// </summary>
        Task<QuotaCheckResult> CanAllocateLicensesAsync(string userEmail, int requestedCount);

        /// <summary>
        /// Get full quota status for a user
        /// </summary>
        Task<QuotaStatus> GetQuotaStatusAsync(string userEmail);

        /// <summary>
        /// Increment used subuser count after creating subuser
        /// </summary>
        Task IncrementSubuserCountAsync(string userEmail);

        /// <summary>
        /// Decrement used subuser count after deleting subuser
        /// </summary>
        Task DecrementSubuserCountAsync(string userEmail);

        /// <summary>
        /// Update used license count
        /// </summary>
        Task UpdateUsedLicensesAsync(string userEmail, int change);
    }

    /// <summary>
    /// Result of quota check
    /// </summary>
    public class QuotaCheckResult
    {
        public bool CanProceed { get; set; }
        public string Message { get; set; } = string.Empty;
        public int Limit { get; set; }
        public int Used { get; set; }
        public int Remaining => Limit - Used;
    }

    /// <summary>
    /// Result of license validation
    /// </summary>
    public class LicenseCheckResult
    {
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int DaysRemaining { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Full quota status for a user
    /// </summary>
    public class QuotaStatus
    {
        public string UserEmail { get; set; } = string.Empty;
        
        // Subusers
        public int MaxSubusers { get; set; }
        public int UsedSubusers { get; set; }
        public int RemainingSubusers => MaxSubusers - UsedSubusers;
        public bool CanCreateSubuser => UsedSubusers < MaxSubusers;

        // Groups
        public int MaxGroups { get; set; }
        public int UsedGroups { get; set; }
        public int RemainingGroups => MaxGroups - UsedGroups;
        public bool CanCreateGroup => UsedGroups < MaxGroups;

        // Departments
        public int MaxDepartments { get; set; }
        public int UsedDepartments { get; set; }

        // Licenses
        public int MaxLicenses { get; set; }
        public int UsedLicenses { get; set; }
        public int RemainingLicenses => MaxLicenses - UsedLicenses;
        public bool CanAllocateLicenses => UsedLicenses < MaxLicenses;

        // License Expiry
        public bool HasLicenseExpiry { get; set; }
        public DateTime? LicenseExpiryDate { get; set; }
        public bool IsLicenseExpired { get; set; }
        public int DaysUntilExpiry { get; set; }

        // Sync Status
        public DateTime? QuotaSyncedAt { get; set; }
    }
}
