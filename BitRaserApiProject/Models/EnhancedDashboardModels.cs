using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Enhanced Dashboard DTOs - Based on BitRaser Admin Dashboard Design
    /// </summary>

    // Enhanced Dashboard Overview Response
    public class EnhancedDashboardOverviewDto
    {
        public string WelcomeMessage { get; set; } = string.Empty;
public DashboardMetricsDto Metrics { get; set; } = new();
public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
  public int TotalMachines { get; set; }
  public int ActiveMachines { get; set; }
    }

    // Dashboard Metrics Container
    public class DashboardMetricsDto
    {
        public MetricDto TotalLicenses { get; set; } = new();
  public MetricDto ActiveUsers { get; set; } = new();
      public MetricDto AvailableLicenses { get; set; } = new();
        public MetricDto SuccessRate { get; set; } = new();
    }

    // Individual Metric
    public class MetricDto
{
        public int Value { get; set; }
        public string Label { get; set; } = string.Empty;
        public double ChangePercent { get; set; }
public string ChangeDirection { get; set; } = "up"; // "up" or "down"
        public string Icon { get; set; } = string.Empty;
        public string Unit { get; set; } = string.Empty;
    }

    // Groups and Users Overview
    public class GroupsUsersOverviewDto
    {
        public List<DashboardGroupDto> Groups { get; set; } = new();
        public int TotalGroups { get; set; }
    }

    // Dashboard Group
    public class DashboardGroupDto
    {
     public string GroupName { get; set; } = string.Empty;
      public string Description { get; set; } = string.Empty;
        public int Licenses { get; set; }
     public string DateCreated { get; set; } = string.Empty;
    }

    // Recent Report
    public class RecentReportDto
    {
     public string ReportId { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
  public string ErasureMethod { get; set; } = string.Empty;
        public DateTime ReportDate { get; set; }
        public string Day { get; set; } = string.Empty;
    public int DeviceCount { get; set; }
    }

    // License Detail
    public class LicenseDetailDto
    {
        public string Product { get; set; } = string.Empty;
   public int TotalAvailable { get; set; }
        public int TotalConsumed { get; set; }
        public int Usage { get; set; }
     public string UsageColor { get; set; } = "#10B981"; // Green, Yellow, Red based on usage
    }

    // Quick Action
    public class QuickActionDto
    {
     public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string IconColor { get; set; } = "#4F46E5";
        public string Route { get; set; } = string.Empty;
        public bool Enabled { get; set; } = true;
    }

    // License Management
    public class LicenseManagementDto
  {
        public BulkAssignmentDto BulkAssignment { get; set; } = new();
        public LicenseAuditDto LicenseAudit { get; set; } = new();
    }

    // Bulk Assignment
    public class BulkAssignmentDto
    {
     public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
  public string Status { get; set; } = string.Empty;
   public string ProcessingStatus { get; set; } = string.Empty;
        public int TotalLicenses { get; set; }
      public int AssignedLicenses { get; set; }
    }

    // License Audit
    public class LicenseAuditDto
    {
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string AnalysisStatus { get; set; } = string.Empty;
        public int TotalLicenses { get; set; }
public int OptimizationScore { get; set; }
    }

    // User Activity
    public class UserActivityDto
    {
        public string Id { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
     public string Activity { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty;
  public DateTime Timestamp { get; set; }
     public string Icon { get; set; } = string.Empty;
        public string Color { get; set; } = "#6B7280";
    }

    // Dashboard Statistics Summary
    public class DashboardStatisticsDto
    {
  public int TotalUsers { get; set; }
   public int TotalSubusers { get; set; }
        public int TotalMachines { get; set; }
     public int TotalReports { get; set; }
        public int TotalSessions { get; set; }
   public int ActiveSessions { get; set; }
        public int TotalLogs { get; set; }
        public int TotalRoles { get; set; }
        public int TotalPermissions { get; set; }
    }

    // Dashboard Tab Navigation
    public class DashboardTabDto
    {
      public string Id { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public bool Active { get; set; }
   public string Route { get; set; } = string.Empty;
    }

    // User Group Assignment
  public class UserGroupAssignmentDto
{
        [Required]
        public List<string> UserIds { get; set; } = new();
  
  [Required]
     public string GroupName { get; set; } = string.Empty;
        
   public string AssignedBy { get; set; } = string.Empty;
  }

    // License Assignment Request
    public class LicenseAssignmentRequestDto
    {
        [Required]
  public List<string> UserEmails { get; set; } = new();
        
        [Required]
     public string LicenseType { get; set; } = string.Empty;
        
        public int ValidityDays { get; set; } = 365;
        public string AssignedBy { get; set; } = string.Empty;
    }

    // Report Generation Request
    public class ReportGenerationRequestDto
    {
        [Required]
        public string ReportType { get; set; } = string.Empty;

     public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
      public List<string> UserEmails { get; set; } = new();
 public string Format { get; set; } = "PDF"; // PDF, CSV, Excel
    }

    // System Settings Update
    public class SystemSettingsUpdateDto
    {
        public Dictionary<string, string> Settings { get; set; } = new();
        public string UpdatedBy { get; set; } = string.Empty;
    }

    // Dashboard Filter Options
    public class DashboardFilterDto
    {
        public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
        public string? UserEmail { get; set; }
        public string? Status { get; set; }
        public string? GroupName { get; set; }
      public int Page { get; set; } = 1;
 public int PageSize { get; set; } = 20;
}

    // Dashboard Chart Data
    public class DashboardChartDataDto
    {
        public string ChartType { get; set; } = string.Empty; // "line", "bar", "pie", "doughnut"
  public List<string> Labels { get; set; } = new();
public List<DashboardDatasetDto> Datasets { get; set; } = new();
    }

    // Dashboard Dataset
    public class DashboardDatasetDto
    {
   public string Label { get; set; } = string.Empty;
     public List<double> Data { get; set; } = new();
        public string BackgroundColor { get; set; } = string.Empty;
     public string BorderColor { get; set; } = string.Empty;
    }

    // Dashboard Notification
    public class DashboardNotificationDto
    {
     public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = "info"; // "info", "success", "warning", "error"
        public DateTime Timestamp { get; set; }
        public bool Read { get; set; }
    public string ActionUrl { get; set; } = string.Empty;
    }

    // Dashboard Widget
    public class DashboardWidgetDto
    {
        public string Id { get; set; } = string.Empty;
 public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // "metric", "chart", "table", "list"
        public int Position { get; set; }
        public object Data { get; set; } = new();
        public bool Visible { get; set; } = true;
    }

    // Dashboard Breadcrumb
    public class DashboardBreadcrumbDto
    {
     public string Label { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public bool Active { get; set; }
    }

    // Dashboard Search Result
    public class DashboardSearchResultDto
    {
        public string Type { get; set; } = string.Empty; // "user", "machine", "report", "group"
    public string Id { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
     public string Description { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
   public string Route { get; set; } = string.Empty;
    }

    // Dashboard Export Options
    public class DashboardExportOptionsDto
    {
   public string Format { get; set; } = "PDF"; // "PDF", "CSV", "Excel", "JSON"
        public string FileName { get; set; } = string.Empty;
        public List<string> Columns { get; set; } = new();
        public DashboardFilterDto Filters { get; set; } = new();
    }
}
