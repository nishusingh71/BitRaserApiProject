using System.ComponentModel.DataAnnotations;

namespace DSecureApi.Models
{
  /// <summary>
    /// Audit Reports Models - Based on BitRaser Audit Reports UI
    /// Screenshot 2: Audit Reports page with filters, search, and data table
    /// </summary>

    #region Audit Reports List

    /// <summary>
    /// Audit reports list response
    /// </summary>
    public class AuditReportsListDto
    {
   public List<AuditReportItemDto> Reports { get; set; } = new();
        public int TotalCount { get; set; }
 public int Page { get; set; }
    public int PageSize { get; set; }
  public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>
    /// Individual audit report item
    /// </summary>
  public class AuditReportItemDto
    {
        public string ReportId { get; set; } = string.Empty;
   public DateTime Date { get; set; }
        public int Devices { get; set; }
  public string Status { get; set; } = string.Empty;
   public string Department { get; set; } = string.Empty;
        public bool CanView { get; set; } = true;
      public bool CanDownload { get; set; } = true;
     public bool CanShare { get; set; } = true;
    }

    #endregion

    #region Audit Report Filters

    /// <summary>
    /// Audit report filters (from Screenshot 2)
    /// </summary>
    public class AuditReportFiltersDto
    {
      [MaxLength(200)]
        public string? Search { get; set; }

        public string? Status { get; set; } // All Statuses, Completed, Pending, Failed

public string? Month { get; set; } // All Months, January, February, etc.

 public string? DeviceRange { get; set; } // All Ranges, 0-10, 10-50, 50-100, 100+

   public bool ShowUniqueRecordsOnly { get; set; } = false;

     [MaxLength(50)]
    public string? SortBy { get; set; } = "Report ID"; // Report ID, Date, Devices, Status

        public int SortDirection { get; set; } = 1; // 1 = Ascending, -1 = Descending

  public int Page { get; set; } = 1;

 public int PageSize { get; set; } = 5;
    }

    #endregion

    #region Audit Report Export

    /// <summary>
    /// Export audit reports request
    /// </summary>
    public class ExportAuditReportsRequest
    {
  public List<string>? ReportIds { get; set; }

      public string ExportFormat { get; set; } = "PDF"; // PDF, Excel, CSV

  public bool ExportAll { get; set; } = false;

   public AuditReportFiltersDto? Filters { get; set; }
 }

    /// <summary>
 /// Export response
    /// </summary>
    public class ExportAuditReportsResponse
    {
        public bool Success { get; set; }
   public string Message { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }
        public string? FileName { get; set; }
   public long FileSizeBytes { get; set; }
        public DateTime ExportedAt { get; set; }
    }

    #endregion

    #region Audit Report Details

    /// <summary>
 /// Detailed audit report
    /// </summary>
    public class AuditReportDetailDto
    {
        public string ReportId { get; set; } = string.Empty;
   public DateTime Date { get; set; }
      public string ClientEmail { get; set; } = string.Empty;
     public string ReportName { get; set; } = string.Empty;
        public string ErasureMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
  public string Department { get; set; } = string.Empty;
  public int TotalDevices { get; set; }
     public List<DeviceErasureDetail> Devices { get; set; } = new();
        public Dictionary<string, object> ReportDetailsJson { get; set; } = new();
        public bool Synced { get; set; }
  public DateTime CreatedAt { get; set; }
    }

    public class DeviceErasureDetail
    {
  public string DeviceId { get; set; } = string.Empty;
 public string DeviceName { get; set; } = string.Empty;
public string ErasureMethod { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
   public int DurationMinutes { get; set; }
    }

    #endregion

    #region Audit Report Statistics

    /// <summary>
    /// Audit report statistics
    /// </summary>
    public class AuditReportStatisticsDto
    {
        public int TotalReports { get; set; }
  public int CompletedReports { get; set; }
     public int PendingReports { get; set; }
   public int FailedReports { get; set; }
    public int ReportsThisMonth { get; set; }
  public int ReportsThisWeek { get; set; }
   public int TotalDevicesErased { get; set; }
    public Dictionary<string, int> ReportsByDepartment { get; set; } = new();
        public Dictionary<string, int> ReportsByStatus { get; set; } = new();
 public Dictionary<string, int> ReportsByMonth { get; set; } = new();
public List<AuditReportItemDto> RecentReports { get; set; } = new();
    }

    #endregion

    #region Status and Month Options

    /// <summary>
    /// Available filter options
    /// </summary>
    public class AuditReportFilterOptionsDto
    {
public List<string> Statuses { get; set; } = new() 
        { 
   "All Statuses", 
            "Completed", 
       "Pending", 
 "Failed", 
   "In Progress" 
};

  public List<string> Months { get; set; } = new() 
{
            "All Months",
  "January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December"
      };

        public List<string> DeviceRanges { get; set; } = new() 
   { 
          "All Ranges", 
        "0-10", 
    "10-50", 
        "50-100", 
            "100+" 
 };

public List<string> SortOptions { get; set; } = new() 
 { 
   "Report ID", 
    "Date", 
        "Devices", 
      "Status", 
     "Department" 
        };
    }

    #endregion
}
