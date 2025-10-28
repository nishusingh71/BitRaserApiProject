using System;
using System.Collections.Generic;

namespace BitRaserApiProject.Models.DTOs
{
    /// <summary>
    /// DTO for user activity tracking with hierarchical access
    /// </summary>
    public class UserActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserType { get; set; } = string.Empty; // "user" or "subuser"
        public DateTime LoginTime { get; set; }
     public DateTime? LogoutTime { get; set; }
        public string Status { get; set; } = string.Empty; // "online", "offline", "idle"
        public string IpAddress { get; set; } = string.Empty;
      public string DeviceInfo { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public TimeSpan? SessionDuration { get; set; }
    public string Activity { get; set; } = string.Empty;
    public string ActivityType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
     public string Icon { get; set; } = string.Empty;
 public string Color { get; set; } = string.Empty;
     public int SessionId { get; set; }
  }

    /// <summary>
    /// Request DTO for getting user activity by email
    /// </summary>
    public class GetUserActivityRequest
  {
        public string UserEmail { get; set; } = string.Empty;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Status { get; set; } = string.Empty; // Filter by status
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Response DTO for user activity
    /// </summary>
    public class UserActivityResponse
    {
     public List<UserActivityDto> Activities { get; set; } = new();
public int TotalCount { get; set; }
        public int Page { get; set; }
public int PageSize { get; set; }
        public int TotalPages { get; set; }
  public UserActivitySummary Summary { get; set; } = new();
    }

    /// <summary>
    /// Summary of user activity
    /// </summary>
  public class UserActivitySummary
    {
        public int TotalSessions { get; set; }
   public int ActiveSessions { get; set; }
  public int OfflineSessions { get; set; }
        public TimeSpan AverageSessionDuration { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime? LastLogout { get; set; }
    public string CurrentStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Hierarchical activity tracking - for managers to see their team
    /// </summary>
    public class HierarchicalActivityRequest
    {
    public bool IncludeSubusers { get; set; } = true;
    public bool IncludeManagedUsers { get; set; } = true;
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string StatusFilter { get; set; } = string.Empty; // "online", "offline", "all"
    public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>
    /// Hierarchical activity response
    /// </summary>
    public class HierarchicalActivityResponse
    {
        public string ManagerEmail { get; set; } = string.Empty;
        public List<UserActivityDto> DirectUsers { get; set; } = new();
    public List<UserActivityDto> Subusers { get; set; } = new();
   public List<UserActivityDto> ManagedUsers { get; set; } = new();
        public ActivityStatistics Statistics { get; set; } = new();
        public int TotalCount { get; set; }
  public int Page { get; set; }
  public int PageSize { get; set; }
        public int TotalPages { get; set; }
  }

    /// <summary>
    /// Activity statistics
    /// </summary>
    public class ActivityStatistics
    {
      public int TotalUsers { get; set; }
        public int OnlineUsers { get; set; }
        public int OfflineUsers { get; set; }
      public int IdleUsers { get; set; }
        public int TotalSubusers { get; set; }
    public int OnlineSubusers { get; set; }
     public int OfflineSubusers { get; set; }
        public double OnlinePercentage { get; set; }
      public TimeSpan AverageSessionDuration { get; set; }
        public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// User status update DTO
    /// </summary>
    public class UserStatusDto
    {
        public string UserEmail { get; set; } = string.Empty;
   public string Status { get; set; } = string.Empty; // "online", "offline", "idle"
        public DateTime LastActivity { get; set; }
        public int ActiveSessionsCount { get; set; }
    }

    /// <summary>
    /// Bulk user activity export request
    /// </summary>
    public class ExportUserActivityRequest
    {
        public List<string> UserEmails { get; set; } = new();
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Format { get; set; } = "csv"; // "csv", "excel", "pdf"
        public bool IncludeDetails { get; set; } = true;
    }

    /// <summary>
    /// Real-time activity notification DTO
    /// </summary>
    public class ActivityNotificationDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty; // "login", "logout", "idle", "active"
  public DateTime Timestamp { get; set; }
        public string IpAddress { get; set; } = string.Empty;
        public string DeviceInfo { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Activity analytics DTO
/// </summary>
    public class ActivityAnalyticsDto
    {
        public string UserEmail { get; set; } = string.Empty;
        public int TotalLogins { get; set; }
   public int TotalLogouts { get; set; }
        public TimeSpan TotalActiveTime { get; set; }
        public TimeSpan AverageSessionDuration { get; set; }
        public DateTime? FirstLogin { get; set; }
        public DateTime? LastLogin { get; set; }
   public List<DailyActivityDto> DailyActivity { get; set; } = new();
public List<HourlyActivityDto> HourlyActivity { get; set; } = new();
  public List<DeviceUsageDto> DeviceBreakdown { get; set; } = new();
    }

    /// <summary>
    /// Daily activity breakdown
    /// </summary>
    public class DailyActivityDto
    {
        public DateTime Date { get; set; }
        public int LoginCount { get; set; }
   public int UniqueUsers { get; set; }
        public TimeSpan TotalActiveTime { get; set; }
    }

    /// <summary>
    /// Hourly activity breakdown
    /// </summary>
    public class HourlyActivityDto
    {
    public int Hour { get; set; }
  public int LoginCount { get; set; }
        public int ActiveUsers { get; set; }
    }

    /// <summary>
    /// Device usage statistics
    /// </summary>
    public class DeviceUsageDto
    {
        public string DeviceType { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    /// <summary>
    /// Live activity dashboard DTO
    /// </summary>
    public class LiveActivityDashboardDto
    {
        public int TotalOnlineUsers { get; set; }
        public int TotalOnlineSubusers { get; set; }
 public List<UserActivityDto> RecentLogins { get; set; } = new();
        public List<UserActivityDto> RecentLogouts { get; set; } = new();
     public List<UserActivityDto> CurrentlyActive { get; set; } = new();
        public ActivityStatistics Statistics { get; set; } = new();
        public DateTime LastRefreshed { get; set; }
    }

    // Existing DTOs from UserActivityController
    public class CloudUsersActivityDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<UserActivityItemDto> Activities { get; set; } = new();
        public int TotalCount { get; set; }
      public int Page { get; set; }
        public int PageSize { get; set; }
public int TotalPages { get; set; }
    }

    public class UserActivityItemDto
    {
        public string UserEmail { get; set; } = string.Empty;
    public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public string Status { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
     public string DeviceInfo { get; set; } = string.Empty;
 }

    public class ActiveUsersCountDto
    {
   public int ActiveCount { get; set; }
        public int TotalUsers { get; set; }
        public int OfflineCount { get; set; }
   public DateTime LastUpdated { get; set; }
    }

    public class ErasureReportsDto
    {
        public string Title { get; set; } = string.Empty;
   public string Description { get; set; } = string.Empty;
        public List<ErasureReportItemDto> Reports { get; set; } = new();
        public int TotalCount { get; set; }
  public int Page { get; set; }
      public int PageSize { get; set; }
        public int TotalPages { get; set; }
}

    public class ErasureReportItemDto
    {
        public string ReportId { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Devices { get; set; }
   public string Status { get; set; } = string.Empty;
        public DateTime Date { get; set; }
      public string Method { get; set; } = string.Empty;
    }

    public class CreateNewUserDto
    {
   public string FullName { get; set; } = string.Empty;
      public string EmailAddress { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
 public string ConfirmPassword { get; set; } = string.Empty;
        public string UserRole { get; set; } = "User";
        public string UserGroup { get; set; } = string.Empty;
   public int LicenseAllocation { get; set; } = 0;
        public string AccountStatus { get; set; } = "Active";
    }

    public class CreateUserResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
 public string UserEmail { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    }

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

 public class UserActivityAnalyticsDto
    {
      public int TotalLogins { get; set; }
        public int UniqueUsers { get; set; }
        public int CurrentlyActive { get; set; }
        public double AverageSessionDuration { get; set; }
        public List<DailyActivityDto> DailyActivity { get; set; } = new();
  public List<HourlyActivityDto> HourlyActivity { get; set; } = new();
    }

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
    }

    public class UpdateUserStatusDto
    {
        public string UserEmail { get; set; } = string.Empty;
      public string Status { get; set; } = string.Empty;
    }

    public class BulkUpdateUserStatusDto
    {
        public List<string> UserEmails { get; set; } = new();
        public string Status { get; set; } = string.Empty;
    }
}
