using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// User Activity & Reports DTOs - Based on BitRaser Dashboard Screenshots
    /// </summary>

    #region User Activity DTOs

    /// <summary>
    /// Cloud Users Activity Response
    /// </summary>
    public class CloudUsersActivityDto
    {
        public string Title { get; set; } = "Cloud Users Activity";
        public string Description { get; set; } = "Monitor user login and logout activity";
  public List<UserActivityItemDto> Activities { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
     public int PageSize { get; set; }
 public int TotalPages { get; set; }
    }

    /// <summary>
    /// Individual User Activity Item
    /// </summary>
    public class UserActivityItemDto
    {
   public string UserEmail { get; set; } = string.Empty;
        public DateTime LoginTime { get; set; }
      public DateTime? LogoutTime { get; set; }
        public string Status { get; set; } = "offline"; // "active" or "offline"
        public string IpAddress { get; set; } = string.Empty;
     public string DeviceInfo { get; set; } = string.Empty;
    }

    /// <summary>
 /// Active Users Count
    /// </summary>
    public class ActiveUsersCountDto
    {
        public int ActiveCount { get; set; }
        public int TotalUsers { get; set; }
        public int OfflineCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    #endregion

    #region Erasure Reports DTOs

    /// <summary>
    /// Erasure Reports Response
    /// </summary>
    public class ErasureReportsDto
{
        public string Title { get; set; } = "Erasure Reports";
  public string Description { get; set; } = "View and manage erasure reports";
        public List<ErasureReportItemDto> Reports { get; set; } = new();
        public int TotalCount { get; set; }
   public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    /// <summary>
    /// Individual Erasure Report Item
    /// </summary>
    public class ErasureReportItemDto
    {
        public string ReportId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
 public int Devices { get; set; }
        public string Status { get; set; } = "completed"; // "completed", "running", "failed"
   public DateTime Date { get; set; }
        public string Method { get; set; } = string.Empty;
 }

    #endregion

    #region Add New User DTOs

  /// <summary>
    /// Create New User Request (Add User Modal)
    /// </summary>
    public class CreateNewUserDto
  {
        [Required]
    public string FullName { get; set; } = string.Empty;

        [Required]
    [EmailAddress]
        public string EmailAddress { get; set; } = string.Empty;

        [Required]
        [MinLength(8)]
        public string Password { get; set; } = string.Empty;

        [Required]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required]
  public string UserRole { get; set; } = "User"; // User, Admin, Manager, SuperAdmin, Support

        public string UserGroup { get; set; } = "Default Group";

        public int LicenseAllocation { get; set; } = 5;

        public string AccountStatus { get; set; } = "Active"; // Active, Inactive
    }

    /// <summary>
    /// Create User Response
    /// </summary>
    public class CreateUserResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
   public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// Available Roles for Dropdown
    /// </summary>
 public class AvailableRolesDto
    {
        public List<RoleOptionDto> Roles { get; set; } = new();
}

    public class RoleOptionDto
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Available Groups for Dropdown
    /// </summary>
    public class AvailableGroupsDto
    {
        public List<GroupOptionDto> Groups { get; set; } = new();
    }

    public class GroupOptionDto
    {
        public string Value { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public int MemberCount { get; set; }
    }

    #endregion

    #region Dashboard Tab Navigation

    /// <summary>
    /// Dashboard Tab Configuration
    /// </summary>
    public class DashboardTabConfigDto
    {
        public List<DashboardTabItemDto> Tabs { get; set; } = new();
    }

    public class DashboardTabItemDto
  {
   public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
public int Order { get; set; }
    }

#endregion

  #region User Activity Filters

    /// <summary>
    /// User Activity Filter Request
    /// </summary>
    public class UserActivityFilterDto
    {
        public DateTime? StartDate { get; set; }
      public DateTime? EndDate { get; set; }
        public string? Status { get; set; } // "active", "offline", "all"
        public string? UserEmail { get; set; }
        public int Page { get; set; } = 1;
   public int PageSize { get; set; } = 20;
    }

    #endregion

    #region Report Filters

    /// <summary>
    /// Erasure Report Filter Request
    /// </summary>
    public class ErasureReportFilterDto
    {
        public DateTime? StartDate { get; set; }
      public DateTime? EndDate { get; set; }
        public string? Status { get; set; } // "completed", "running", "failed", "all"
        public string? Type { get; set; } // "Drive Eraser", "Mobile Diagnostics", etc.
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    #endregion

    #region Batch Operations

    /// <summary>
    /// Batch User Creation Request
    /// </summary>
public class BatchCreateUsersDto
    {
        public List<CreateNewUserDto> Users { get; set; } = new();
    public bool SendWelcomeEmail { get; set; } = true;
        public bool AssignDefaultLicenses { get; set; } = true;
    }

    /// <summary>
    /// Batch User Creation Response
    /// </summary>
    public class BatchCreateUsersResponseDto
    {
   public int TotalUsers { get; set; }
        public int SuccessCount { get; set; }
        public int FailedCount { get; set; }
   public List<string> SuccessfulEmails { get; set; } = new();
        public List<BatchUserErrorDto> Errors { get; set; } = new();
    }

    public class BatchUserErrorDto
    {
        public string Email { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
    }

    #endregion

    #region User Status Management

    /// <summary>
    /// Update User Status Request
    /// </summary>
    public class UpdateUserStatusDto
    {
    [Required]
        public string UserEmail { get; set; } = string.Empty;

        [Required]
   public string Status { get; set; } = "Active"; // Active, Inactive, Suspended

        public string? Reason { get; set; }
    }

    /// <summary>
  /// Bulk User Status Update
  /// </summary>
    public class BulkUpdateUserStatusDto
    {
        [Required]
    public List<string> UserEmails { get; set; } = new();

     [Required]
 public string Status { get; set; } = "Active";

        public string? Reason { get; set; }
    }

    #endregion

    #region Activity Analytics

    /// <summary>
    /// User Activity Analytics
    /// </summary>
    public class UserActivityAnalyticsDto
    {
        public int TotalLogins { get; set; }
     public int UniqueUsers { get; set; }
        public int CurrentlyActive { get; set; }
     public double AverageSessionDuration { get; set; }
        public List<HourlyActivityDto> HourlyActivity { get; set; } = new();
        public List<DailyActivityDto> DailyActivity { get; set; } = new();
    }

    public class HourlyActivityDto
    {
        public int Hour { get; set; }
        public int LoginCount { get; set; }
        public int ActiveUsers { get; set; }
    }

    public class DailyActivityDto
    {
        public DateTime Date { get; set; }
   public int LoginCount { get; set; }
        public int UniqueUsers { get; set; }
    }

    #endregion

    #region Report Analytics

    /// <summary>
    /// Erasure Report Analytics
    /// </summary>
    public class ErasureReportAnalyticsDto
    {
 public int TotalReports { get; set; }
        public int CompletedReports { get; set; }
        public int RunningReports { get; set; }
        public int FailedReports { get; set; }
        public int TotalDevicesErased { get; set; }
        public List<ReportTypeStatsDto> TypeBreakdown { get; set; } = new();
   public List<MethodStatsDto> MethodBreakdown { get; set; } = new();
    }

    public class ReportTypeStatsDto
    {
   public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public int DeviceCount { get; set; }
  }

    public class MethodStatsDto
    {
        public string Method { get; set; } = string.Empty;
        public int Count { get; set; }
 public double SuccessRate { get; set; }
    }

#endregion

    #region Export Options

    /// <summary>
    /// Export User Activity Request
    /// </summary>
    public class ExportUserActivityDto
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Format { get; set; } = "CSV"; // CSV, PDF, Excel
    public bool IncludeDetails { get; set; } = true;
    }

    /// <summary>
    /// Export Reports Request
    /// </summary>
    public class ExportReportsDto
    {
 public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
        public string Format { get; set; } = "PDF"; // CSV, PDF, Excel
        public bool IncludeCharts { get; set; } = true;
        public List<string>? ReportIds { get; set; }
    }

    #endregion
}
