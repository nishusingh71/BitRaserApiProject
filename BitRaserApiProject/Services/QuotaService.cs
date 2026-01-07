using Microsoft.EntityFrameworkCore;
using DSecureApi.Models;

namespace DSecureApi.Services
{
    /// <summary>
    /// Service for checking and enforcing user quotas and limits
    /// IMPORTANT: Always reads quotas from MAIN DB to prevent user tampering
    /// </summary>
    public class QuotaService : IQuotaService
    {
        private readonly ApplicationDbContext _mainContext;
        private readonly ILogger<QuotaService> _logger;

        public QuotaService(ApplicationDbContext mainContext, ILogger<QuotaService> logger)
        {
            _mainContext = mainContext;
            _logger = logger;
        }

        public async Task<QuotaCheckResult> CanCreateSubuserAsync(string userEmail)
        {
            try
            {
                // Always read from MAIN DB to prevent tampering
                var user = await _mainContext.Users
                    .Where(u => u.user_email.ToLower() == userEmail.ToLower())
                    .Select(u => new { u.max_subusers, u.used_subusers, u.license_expiry_date })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return new QuotaCheckResult
                    {
                        CanProceed = false,
                        Message = "User not found in main database"
                    };
                }

                // Check license expiry first
                if (user.license_expiry_date.HasValue && user.license_expiry_date < DateTime.UtcNow)
                {
                    return new QuotaCheckResult
                    {
                        CanProceed = false,
                        Message = "License expired. Cannot create new subusers.",
                        Limit = user.max_subusers ?? 5,
                        Used = user.used_subusers ?? 0
                    };
                }

                var maxSubusers = user.max_subusers ?? 5;
                var usedSubusers = user.used_subusers ?? 0;

                return new QuotaCheckResult
                {
                    CanProceed = usedSubusers < maxSubusers,
                    Message = usedSubusers < maxSubusers 
                        ? $"Can create subuser. {maxSubusers - usedSubusers} remaining."
                        : $"Subuser limit reached. Maximum allowed: {maxSubusers}",
                    Limit = maxSubusers,
                    Used = usedSubusers
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking subuser quota for {Email}", userEmail);
                return new QuotaCheckResult
                {
                    CanProceed = false,
                    Message = "Error checking quota"
                };
            }
        }

        public async Task<QuotaCheckResult> CanCreateGroupAsync(string userEmail)
        {
            try
            {
                var user = await _mainContext.Users
                    .Where(u => u.user_email.ToLower() == userEmail.ToLower())
                    .Select(u => new { u.max_groups, u.license_expiry_date })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return new QuotaCheckResult { CanProceed = false, Message = "User not found" };
                }

                // Check license expiry
                if (user.license_expiry_date.HasValue && user.license_expiry_date < DateTime.UtcNow)
                {
                    return new QuotaCheckResult
                    {
                        CanProceed = false,
                        Message = "License expired. Cannot create new groups."
                    };
                }

                // Count existing groups for this user
                var usedGroups = await _mainContext.Groups.CountAsync(); // Can be refined per user
                var maxGroups = user.max_groups ?? 3;

                return new QuotaCheckResult
                {
                    CanProceed = usedGroups < maxGroups,
                    Message = usedGroups < maxGroups 
                        ? $"Can create group. {maxGroups - usedGroups} remaining."
                        : $"Group limit reached. Maximum allowed: {maxGroups}",
                    Limit = maxGroups,
                    Used = usedGroups
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking group quota for {Email}", userEmail);
                return new QuotaCheckResult { CanProceed = false, Message = "Error checking quota" };
            }
        }

        public async Task<LicenseCheckResult> CheckLicenseValidAsync(string userEmail)
        {
            try
            {
                var user = await _mainContext.Users
                    .Where(u => u.user_email.ToLower() == userEmail.ToLower())
                    .Select(u => new { u.license_expiry_date, u.max_licenses, u.used_licenses })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return new LicenseCheckResult
                    {
                        IsValid = false,
                        Message = "User not found"
                    };
                }

                if (!user.license_expiry_date.HasValue)
                {
                    // No expiry set = unlimited license
                    return new LicenseCheckResult
                    {
                        IsValid = true,
                        IsExpired = false,
                        DaysRemaining = -1, // -1 means unlimited
                        Message = "License is valid (no expiry date set)"
                    };
                }

                var now = DateTime.UtcNow;
                var isExpired = user.license_expiry_date < now;
                var daysRemaining = isExpired ? 0 : (int)(user.license_expiry_date.Value - now).TotalDays;

                return new LicenseCheckResult
                {
                    IsValid = !isExpired,
                    IsExpired = isExpired,
                    ExpiryDate = user.license_expiry_date,
                    DaysRemaining = daysRemaining,
                    Message = isExpired 
                        ? "License has expired" 
                        : $"License valid for {daysRemaining} more days"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking license for {Email}", userEmail);
                return new LicenseCheckResult { IsValid = false, Message = "Error checking license" };
            }
        }

        public async Task<QuotaCheckResult> CanAllocateLicensesAsync(string userEmail, int requestedCount)
        {
            try
            {
                var user = await _mainContext.Users
                    .Where(u => u.user_email.ToLower() == userEmail.ToLower())
                    .Select(u => new { u.max_licenses, u.used_licenses, u.license_expiry_date })
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return new QuotaCheckResult { CanProceed = false, Message = "User not found" };
                }

                // Check license expiry
                if (user.license_expiry_date.HasValue && user.license_expiry_date < DateTime.UtcNow)
                {
                    return new QuotaCheckResult
                    {
                        CanProceed = false,
                        Message = "License expired. Cannot allocate licenses."
                    };
                }

                var maxLicenses = user.max_licenses ?? 10;
                var usedLicenses = user.used_licenses ?? 0;
                var canAllocate = (usedLicenses + requestedCount) <= maxLicenses;

                return new QuotaCheckResult
                {
                    CanProceed = canAllocate,
                    Message = canAllocate 
                        ? $"Can allocate {requestedCount} licenses. {maxLicenses - usedLicenses - requestedCount} will remain."
                        : $"Cannot allocate {requestedCount} licenses. Only {maxLicenses - usedLicenses} available.",
                    Limit = maxLicenses,
                    Used = usedLicenses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking license allocation for {Email}", userEmail);
                return new QuotaCheckResult { CanProceed = false, Message = "Error checking quota" };
            }
        }

        public async Task<QuotaStatus> GetQuotaStatusAsync(string userEmail)
        {
            try
            {
                var user = await _mainContext.Users
                    .Where(u => u.user_email.ToLower() == userEmail.ToLower())
                    .FirstOrDefaultAsync();

                if (user == null)
                {
                    return new QuotaStatus { UserEmail = userEmail };
                }

                var now = DateTime.UtcNow;
                var isExpired = user.license_expiry_date.HasValue && user.license_expiry_date < now;
                var daysUntilExpiry = user.license_expiry_date.HasValue 
                    ? (int)(user.license_expiry_date.Value - now).TotalDays 
                    : -1;

                // Count groups and departments
                var groupCount = await _mainContext.Groups.CountAsync();
                var departmentCount = await _mainContext.subuser
                    .Where(s => s.user_email == userEmail && s.Department != null)
                    .Select(s => s.Department)
                    .Distinct()
                    .CountAsync();

                return new QuotaStatus
                {
                    UserEmail = userEmail,
                    MaxSubusers = user.max_subusers ?? 5,
                    UsedSubusers = user.used_subusers ?? 0,
                    MaxGroups = user.max_groups ?? 3,
                    UsedGroups = groupCount,
                    MaxDepartments = user.max_departments ?? 3,
                    UsedDepartments = departmentCount,
                    MaxLicenses = user.max_licenses ?? 10,
                    UsedLicenses = user.used_licenses ?? 0,
                    HasLicenseExpiry = user.license_expiry_date.HasValue,
                    LicenseExpiryDate = user.license_expiry_date,
                    IsLicenseExpired = isExpired,
                    DaysUntilExpiry = daysUntilExpiry,
                    QuotaSyncedAt = user.quota_synced_at
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quota status for {Email}", userEmail);
                return new QuotaStatus { UserEmail = userEmail };
            }
        }

        public async Task IncrementSubuserCountAsync(string userEmail)
        {
            try
            {
                var user = await _mainContext.Users
                    .FirstOrDefaultAsync(u => u.user_email.ToLower() == userEmail.ToLower());

                if (user != null)
                {
                    user.used_subusers = (user.used_subusers ?? 0) + 1;
                    user.updated_at = DateTime.UtcNow;
                    await _mainContext.SaveChangesAsync();
                    _logger.LogInformation("Incremented subuser count for {Email} to {Count}", 
                        userEmail, user.used_subusers);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error incrementing subuser count for {Email}", userEmail);
            }
        }

        public async Task DecrementSubuserCountAsync(string userEmail)
        {
            try
            {
                var user = await _mainContext.Users
                    .FirstOrDefaultAsync(u => u.user_email.ToLower() == userEmail.ToLower());

                if (user != null && (user.used_subusers ?? 0) > 0)
                {
                    user.used_subusers = (user.used_subusers ?? 0) - 1;
                    user.updated_at = DateTime.UtcNow;
                    await _mainContext.SaveChangesAsync();
                    _logger.LogInformation("Decremented subuser count for {Email} to {Count}", 
                        userEmail, user.used_subusers);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decrementing subuser count for {Email}", userEmail);
            }
        }

        public async Task UpdateUsedLicensesAsync(string userEmail, int change)
        {
            try
            {
                var user = await _mainContext.Users
                    .FirstOrDefaultAsync(u => u.user_email.ToLower() == userEmail.ToLower());

                if (user != null)
                {
                    user.used_licenses = Math.Max(0, (user.used_licenses ?? 0) + change);
                    user.updated_at = DateTime.UtcNow;
                    await _mainContext.SaveChangesAsync();
                    _logger.LogInformation("Updated license count for {Email} by {Change} to {Count}", 
                        userEmail, change, user.used_licenses);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating license count for {Email}", userEmail);
            }
        }
    }
}
