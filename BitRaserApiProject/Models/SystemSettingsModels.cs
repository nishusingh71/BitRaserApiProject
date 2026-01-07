using System.ComponentModel.DataAnnotations;

namespace DSecureApi.Models
{
    /// <summary>
    /// System Settings Models - Based on BitRaser System Settings UI
    /// Includes General, Security, Notifications, and License settings
    /// </summary>

    #region General Settings

    /// <summary>
    /// General system settings configuration
    /// </summary>
    public class GeneralSettingsDto
    {
        [Required]
     [MaxLength(100)]
        public string SiteName { get; set; } = "DSecureTech";

        [MaxLength(500)]
      public string SiteDescription { get; set; } = "Professional Data Erasure Solutions";

        [Required]
        [MaxLength(10)]
  public string DefaultLanguage { get; set; } = "English";

        [Required]
        [MaxLength(50)]
        public string Timezone { get; set; } = "UTC";

     public bool EnableMaintenanceMode { get; set; } = false;

        // Additional settings
     [MaxLength(100)]
     public string? CompanyName { get; set; }

        [EmailAddress]
        [MaxLength(255)]
 public string? SupportEmail { get; set; }

        [MaxLength(20)]
        public string? SupportPhone { get; set; }

        [MaxLength(10)]
        public string? DateFormat { get; set; } = "dd/MM/yyyy";

        [MaxLength(10)]
        public string? TimeFormat { get; set; } = "24h";

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
 }

    #endregion

    #region Security Settings

    /// <summary>
    /// Security and authentication settings
    /// </summary>
    public class SecuritySettingsDto
    {
        [Range(6, 128)]
     public int PasswordMinimumLength { get; set; } = 8;

     [Range(1, 1440)]
        public int SessionTimeoutMinutes { get; set; } = 30;

   [Range(1, 10)]
        public int MaxLoginAttempts { get; set; } = 5;

     public bool RequireSpecialCharactersInPasswords { get; set; } = true;

        public bool EnableTwoFactorAuthentication { get; set; } = false;

  // Additional security settings
        public bool RequireUppercase { get; set; } = true;
        public bool RequireLowercase { get; set; } = true;
        public bool RequireNumbers { get; set; } = true;
 
        [Range(0, 365)]
      public int PasswordExpiryDays { get; set; } = 90;

        public bool EnablePasswordHistory { get; set; } = true;
        
[Range(1, 10)]
    public int PasswordHistoryCount { get; set; } = 5;

        public bool EnableAccountLockout { get; set; } = true;
   
        [Range(1, 1440)]
        public int LockoutDurationMinutes { get; set; } = 30;

        public bool EnableIpWhitelist { get; set; } = false;
        public string? IpWhitelistJson { get; set; }

  public bool EnableAuditLogging { get; set; } = true;
   public bool EnableLoginNotifications { get; set; } = true;

   public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    #endregion

    #region Notification Settings

    /// <summary>
    /// Notification preferences and settings
    /// </summary>
    public class NotificationSettingsDto
    {
        // Email notifications
        public bool EnableEmailNotifications { get; set; } = true;
      public bool NotifyOnNewUser { get; set; } = true;
        public bool NotifyOnLicenseExpiry { get; set; } = true;
    public bool NotifyOnSystemErrors { get; set; } = true;
  public bool NotifyOnSecurityEvents { get; set; } = true;

  // SMS Notifications (From Screenshot 3)
        public bool EnableSmsNotifications { get; set; } = false;

        // Report Generation Notifications (From Screenshot 3)
        public bool NotifyOnReportGeneration { get; set; } = true;

        // System Alerts (From Screenshot 3)
        public bool EnableSystemAlerts { get; set; } = true;

     // User Registration Notifications (From Screenshot 3)
      public bool NotifyOnUserRegistration { get; set; } = true;

        // System notifications
        public bool EnableMaintenanceAlerts { get; set; } = true;
        public bool EnablePerformanceAlerts { get; set; } = false;

        // User notifications
  public bool NotifyUsersOnPasswordChange { get; set; } = true;
        public bool NotifyUsersOnLoginFromNewDevice { get; set; } = true;
        public bool NotifyAdminsOnFailedLogins { get; set; } = true;

        // Notification channels
   [EmailAddress]
        [MaxLength(255)]
        public string? AdminNotificationEmail { get; set; }

        [MaxLength(20)]
  public string? AdminNotificationPhone { get; set; }

        // Notification frequency
[MaxLength(50)]
        public string NotificationFrequency { get; set; } = "immediate"; // immediate, hourly, daily

        [Range(0, 23)]
        public int DailyReportHour { get; set; } = 9;

     public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    #endregion

    #region License Settings

    /// <summary>
  /// License management and allocation settings
  /// </summary>
    public class LicenseSettingsDto
    {
        [Required]
   [MaxLength(100)]
        public string LicenseType { get; set; } = "Enterprise";

      public int TotalLicenses { get; set; } = 0;
        public int UsedLicenses { get; set; } = 0;
        public int AvailableLicenses => TotalLicenses - UsedLicenses;

        public DateTime? LicenseExpiryDate { get; set; }
 public int DaysUntilExpiry => LicenseExpiryDate.HasValue 
            ? Math.Max(0, (LicenseExpiryDate.Value - DateTime.UtcNow).Days) 
     : 0;

    public bool AutoRenew { get; set; } = false;
        public bool SendExpiryReminders { get; set; } = true;

        [Range(1, 90)]
        public int ReminderDaysBeforeExpiry { get; set; } = 30;

        // License restrictions
        public int MaxUsers { get; set; } = 0;
   public int MaxMachines { get; set; } = 0;
        public int MaxSubusers { get; set; } = 0;

        public bool AllowTrialExtension { get; set; } = false;
        public int TrialDaysRemaining { get; set; } = 0;

        [MaxLength(500)]
        public string? LicenseKey { get; set; }

        [MaxLength(100)]
        public string? LicensedTo { get; set; }

        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedBy { get; set; }
    }

    /// <summary>
    /// Bulk License Assignment Request (From Screenshot 2)
    /// </summary>
    public class BulkLicenseAssignmentRequest
    {
        [Required]
   [Range(1, 10000)]
        public int NumberOfUsers { get; set; }

        [Required]
        [Range(1, 100)]
        public int LicensesPerUser { get; set; }

    public int TotalUsers => NumberOfUsers;
        public int TotalLicensesRequired => NumberOfUsers * LicensesPerUser;

      public List<string>? UserEmails { get; set; }
public string? GroupId { get; set; }
      public DateTime? ExpiryDate { get; set; }
    }

/// <summary>
 /// Bulk License Assignment Response
    /// </summary>
    public class BulkLicenseAssignmentResponse
    {
      public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
   public int UsersProcessed { get; set; }
        public int LicensesAssigned { get; set; }
        public int FailedAssignments { get; set; }
      public List<string> Errors { get; set; } = new();
        public DateTime AssignedAt { get; set; }
    }

    /// <summary>
    /// License Audit Report
    /// </summary>
    public class LicenseAuditReportDto
    {
        public int TotalLicenses { get; set; }
        public int UsedLicenses { get; set; }
        public int AvailableLicenses { get; set; }
        public int ExpiringWithin30Days { get; set; }
        public int ExpiredLicenses { get; set; }
        public Dictionary<string, int> LicensesByUser { get; set; } = new();
  public List<LicenseUsageEntry> RecentActivity { get; set; } = new();
    }

    /// <summary>
    /// License Usage Entry
    /// </summary>
    public class LicenseUsageEntry
    {
        public string UserEmail { get; set; } = string.Empty;
        public int LicensesAssigned { get; set; }
    public DateTime AssignedAt { get; set; }
        public string AssignedBy { get; set; } = string.Empty;
    }

    #endregion

#region Complete System Settings

    /// <summary>
    /// Complete system settings response
    /// </summary>
    public class SystemSettingsResponseDto
    {
        public GeneralSettingsDto General { get; set; } = new();
   public SecuritySettingsDto Security { get; set; } = new();
public NotificationSettingsDto Notifications { get; set; } = new();
        public LicenseSettingsDto License { get; set; } = new();
    }

    #endregion

    #region Update Requests

    /// <summary>
    /// Update general settings request
    /// </summary>
    public class UpdateGeneralSettingsRequest
    {
        [MaxLength(100)]
        public string? SiteName { get; set; }

        [MaxLength(500)]
 public string? SiteDescription { get; set; }

        [MaxLength(10)]
        public string? DefaultLanguage { get; set; }

        [MaxLength(50)]
  public string? Timezone { get; set; }

        public bool? EnableMaintenanceMode { get; set; }
    }

  /// <summary>
    /// Update security settings request
    /// </summary>
    public class UpdateSecuritySettingsRequest
    {
        [Range(6, 128)]
     public int? PasswordMinimumLength { get; set; }

[Range(1, 1440)]
     public int? SessionTimeoutMinutes { get; set; }

        [Range(1, 10)]
        public int? MaxLoginAttempts { get; set; }

        public bool? RequireSpecialCharactersInPasswords { get; set; }

        public bool? EnableTwoFactorAuthentication { get; set; }
    }

    /// <summary>
    /// Update notification settings request
    /// </summary>
    public class UpdateNotificationSettingsRequest
    {
        public bool? EnableEmailNotifications { get; set; }
        public bool? NotifyOnNewUser { get; set; }
        public bool? NotifyOnLicenseExpiry { get; set; }
        public bool? NotifyOnSystemErrors { get; set; }

   [EmailAddress]
public string? AdminNotificationEmail { get; set; }

    // New fields from Screenshot 3
      public bool? EnableSmsNotifications { get; set; }
        public bool? NotifyOnReportGeneration { get; set; }
        public bool? EnableSystemAlerts { get; set; }
        public bool? NotifyOnUserRegistration { get; set; }
    }

    /// <summary>
    /// Update auto-renewal request
    /// </summary>
 public class UpdateAutoRenewRequest
    {
        [Required]
        public bool AutoRenew { get; set; }
    }

    /// <summary>
  /// Update license expiry date request
  /// </summary>
    public class UpdateLicenseExpiryRequest
    {
        [Required]
        public DateTime ExpiryDate { get; set; }
    }

    #endregion

    #region System Settings Storage Entity

    /// <summary>
    /// Database entity for storing system settings
    /// </summary>
    public class SystemSetting
    {
        [Key]
  public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string SettingKey { get; set; } = string.Empty;

   [Required]
public string SettingValue { get; set; } = string.Empty;

  [MaxLength(50)]
   public string Category { get; set; } = "General"; // General, Security, Notifications, License

        [MaxLength(500)]
        public string? Description { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
     public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

   [MaxLength(255)]
  public string? UpdatedBy { get; set; }

        public bool IsEncrypted { get; set; } = false;
 public bool IsSystemSetting { get; set; } = true;
    }

    #endregion

 #region Available Options

  /// <summary>
    /// Available language options
 /// </summary>
    public class LanguageOption
    {
    public string Code { get; set; } = string.Empty;
public string Name { get; set; } = string.Empty;
        public string NativeName { get; set; } = string.Empty;
    }

    /// <summary>
    /// Available timezone options
    /// </summary>
    public class TimezoneOption
    {
     public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
   public string Offset { get; set; } = string.Empty;
  }

    /// <summary>
    /// System settings options response
    /// </summary>
    public class SystemSettingsOptionsDto
  {
 public List<LanguageOption> Languages { get; set; } = new();
 public List<TimezoneOption> Timezones { get; set; } = new();
 public List<string> DateFormats { get; set; } = new();
public List<string> TimeFormats { get; set; } = new();
    }

    #endregion

 #region Settings Validation

    /// <summary>
    /// Settings validation result
    /// </summary>
    public class SettingsValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new();
   public Dictionary<string, List<string>> FieldErrors { get; set; } = new();
    }

    #endregion
}
