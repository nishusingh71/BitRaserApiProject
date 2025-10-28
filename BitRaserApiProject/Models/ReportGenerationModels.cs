using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Report Generation Models - Based on BitRaser Generate Report UI
    /// Complete report configuration and generation
    /// </summary>

    #region Report Configuration

    /// <summary>
    /// Complete report generation request
    /// Matches the "Generate Report" form from screenshot
    /// </summary>
    public class GenerateReportRequestDto
    {
 // Basic Information
        [Required]
   [MaxLength(200)]
        public string ReportTitle { get; set; } = string.Empty;

 [Required]
    public string ReportType { get; set; } = "Comprehensive Report"; // Comprehensive, Summary, Detailed

        // Date Range
        [Required]
        public DateTime FromDate { get; set; }

        [Required]
 public DateTime ToDate { get; set; }

        // Device Types
 public bool AllDevices { get; set; } = true;
        public bool WindowsDevices { get; set; } = false;
        public bool LinuxDevices { get; set; } = false;
        public bool MacDevices { get; set; } = false;
        public bool MobileDevices { get; set; } = false;
        public bool ServerDevices { get; set; } = false;

        // Report Options
        public string ExportFormat { get; set; } = "PDF"; // PDF, Excel, CSV
     public bool IncludeChartsAndGraphs { get; set; } = true;
     public bool IncludeComplianceCertificates { get; set; } = true;
        public bool IncludeDetailedStatistics { get; set; } = true;

        // Report Customization - Erasure Person
        [MaxLength(100)]
        public string? ErasurePersonName { get; set; }

        [MaxLength(100)]
   public string? ErasurePersonDepartment { get; set; }

        // Validator Person
   [MaxLength(100)]
        public string? ValidatorPersonName { get; set; }

        [MaxLength(100)]
        public string? ValidatorPersonDepartment { get; set; }

   // Signature Settings
        public string? TechnicianSignature { get; set; } // Base64 or file path
        public string? ValidatorSignature { get; set; } // Base64 or file path

        // Image Settings
        public string? TopLogo { get; set; } // Base64 or file path
        public string? WatermarkImage { get; set; } // Base64 or file path

        // Header Settings
     [MaxLength(200)]
        public string? HeaderText { get; set; } = "Data Erasure Report";

        // Generation Options
     public bool ScheduleReportGeneration { get; set; } = false;
        public DateTime? ScheduledDateTime { get; set; }

 // Additional filters
  public string? UserEmail { get; set; }
        public List<string>? MachineIds { get; set; }
    public List<string>? ErasureMethods { get; set; }
    }

    #endregion

    #region Report Types

    /// <summary>
    /// Available report types
    /// </summary>
    public class ReportTypeDto
    {
 public string Value { get; set; } = string.Empty;
   public string Label { get; set; } = string.Empty;
      public string Description { get; set; } = string.Empty;
 }

 /// <summary>
    /// Report types response
    /// </summary>
    public class ReportTypesResponseDto
    {
        public List<ReportTypeDto> ReportTypes { get; set; } = new()
  {
       new ReportTypeDto 
      { 
     Value = "Comprehensive Report", 
    Label = "Comprehensive Report",
            Description = "Complete overview of all erasure activities"
       },
  new ReportTypeDto 
            { 
      Value = "Summary Report", 
    Label = "Summary Report",
        Description = "Quick summary of key metrics"
       },
            new ReportTypeDto 
     { 
     Value = "Detailed Report", 
   Label = "Detailed Report",
      Description = "Detailed analysis with all data points"
  },
     new ReportTypeDto 
       { 
     Value = "Compliance Report", 
    Label = "Compliance Report",
  Description = "Compliance and certification focused"
            },
            new ReportTypeDto 
            { 
        Value = "Audit Report", 
     Label = "Audit Report",
    Description = "Audit trail and history"
   }
        };
    }

    #endregion

    #region Export Formats

    /// <summary>
    /// Available export formats
    /// </summary>
    public class ExportFormatDto
    {
 public string Value { get; set; } = string.Empty;
public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
    }

    /// <summary>
    /// Export formats response
    /// </summary>
    public class ExportFormatsResponseDto
    {
   public List<ExportFormatDto> ExportFormats { get; set; } = new()
        {
   new ExportFormatDto { Value = "PDF", Label = "PDF Document", Icon = "pdf" },
        new ExportFormatDto { Value = "Excel", Label = "Excel Spreadsheet", Icon = "excel" },
      new ExportFormatDto { Value = "CSV", Label = "CSV File", Icon = "csv" },
  new ExportFormatDto { Value = "HTML", Label = "HTML Report", Icon = "html" }
        };
    }

    #endregion

  #region Report Generation Response

    /// <summary>
 /// Report generation response
/// </summary>
    public class GenerateReportResponseDto
    {
    public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
   public string? ReportId { get; set; }
        public string? DownloadUrl { get; set; }
      public string? FileName { get; set; }
        public long? FileSizeBytes { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string? Format { get; set; }
    }

    #endregion

    #region Report History

    /// <summary>
    /// Report history item
    /// </summary>
    public class ReportHistoryItemDto
    {
        public string ReportId { get; set; } = string.Empty;
 public string ReportTitle { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
        public DateTime GeneratedAt { get; set; }
        public string GeneratedBy { get; set; } = string.Empty;
      public string Format { get; set; } = string.Empty;
   public long FileSizeBytes { get; set; }
        public string Status { get; set; } = "completed"; // pending, processing, completed, failed
        public string? DownloadUrl { get; set; }
  }

    /// <summary>
    /// Report history response
    /// </summary>
    public class ReportHistoryResponseDto
    {
        public List<ReportHistoryItemDto> Reports { get; set; } = new();
        public int TotalCount { get; set; }
      public int Page { get; set; }
  public int PageSize { get; set; }
    }

    #endregion

    #region Report Templates

    /// <summary>
    /// Report template
 /// </summary>
    public class ReportTemplateDto
    {
        public int TemplateId { get; set; }
        public string TemplateName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
        public string? ConfigurationJson { get; set; }
   public bool IsDefault { get; set; } = false;
  public DateTime CreatedAt { get; set; }
 public string CreatedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// Report templates response
    /// </summary>
    public class ReportTemplatesResponseDto
    {
        public List<ReportTemplateDto> Templates { get; set; } = new();
    }

 #endregion

    #region Scheduled Reports

    /// <summary>
    /// Scheduled report
    /// </summary>
 public class ScheduledReportDto
    {
      public int ScheduleId { get; set; }
      public string ReportTitle { get; set; } = string.Empty;
        public string ReportType { get; set; } = string.Empty;
   public string Frequency { get; set; } = "daily"; // daily, weekly, monthly
        public DateTime NextRunDate { get; set; }
   public string RecipientEmails { get; set; } = string.Empty;
 public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
 /// Scheduled reports response
    /// </summary>
    public class ScheduledReportsResponseDto
    {
 public List<ScheduledReportDto> ScheduledReports { get; set; } = new();
    }

    #endregion

    #region Report Statistics

    /// <summary>
    /// Report generation statistics
    /// </summary>
    public class ReportStatisticsDto
    {
        public int TotalReportsGenerated { get; set; }
        public int ReportsThisMonth { get; set; }
        public int ReportsThisWeek { get; set; }
        public int ReportsToday { get; set; }
 public long TotalStorageUsedBytes { get; set; }
        public Dictionary<string, int> ReportsByType { get; set; } = new();
   public Dictionary<string, int> ReportsByFormat { get; set; } = new();
        public List<ReportHistoryItemDto> RecentReports { get; set; } = new();
    }

    #endregion

    #region Database Entity

    /// <summary>
    /// Generated report entity for database
    /// </summary>
    public class GeneratedReport
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string ReportId { get; set; } = Guid.NewGuid().ToString();

        [Required]
  [MaxLength(200)]
 public string ReportTitle { get; set; } = string.Empty;

 [Required]
        [MaxLength(100)]
        public string ReportType { get; set; } = string.Empty;

        [Required]
    public DateTime FromDate { get; set; }

        [Required]
        public DateTime ToDate { get; set; }

    [Required]
     [MaxLength(50)]
 public string Format { get; set; } = "PDF";

        [Required]
        public string ConfigurationJson { get; set; } = "{}";

        [MaxLength(500)]
        public string? FilePath { get; set; }

        public long FileSizeBytes { get; set; }

        [Required]
        [MaxLength(255)]
    public string GeneratedBy { get; set; } = string.Empty;

        public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(50)]
   public string Status { get; set; } = "completed"; // pending, processing, completed, failed

[MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        public bool IsScheduled { get; set; } = false;
      public int? ScheduleId { get; set; }

      public DateTime? ExpiresAt { get; set; }
    public bool IsDeleted { get; set; } = false;
    }

    #endregion

    #region Report Configuration Database Entity

    /// <summary>
    /// Report template configuration entity
    /// </summary>
    public class ReportTemplate
    {
      [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string TemplateName { get; set; } = string.Empty;

 [MaxLength(500)]
        public string? Description { get; set; }

        [Required]
     [MaxLength(100)]
        public string ReportType { get; set; } = string.Empty;

        [Required]
        public string ConfigurationJson { get; set; } = "{}";

        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;

        [MaxLength(255)]
        public string CreatedBy { get; set; } = string.Empty;

     public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    #endregion

    #region Scheduled Report Entity

    /// <summary>
    /// Scheduled report configuration entity
    /// </summary>
    public class ScheduledReport
    {
      [Key]
    public int Id { get; set; }

        [Required]
[MaxLength(200)]
        public string ReportTitle { get; set; } = string.Empty;

        [Required]
   [MaxLength(100)]
        public string ReportType { get; set; } = string.Empty;

        [Required]
    public string ConfigurationJson { get; set; } = "{}";

        [Required]
        [MaxLength(50)]
        public string Frequency { get; set; } = "daily"; // daily, weekly, monthly

   public int DayOfWeek { get; set; } = 1; // 0-6 for weekly
      public int DayOfMonth { get; set; } = 1; // 1-31 for monthly
   public int HourOfDay { get; set; } = 9; // 0-23

        [Required]
        public DateTime NextRunDate { get; set; }

        public DateTime? LastRunDate { get; set; }

[Required]
  public string RecipientEmails { get; set; } = string.Empty; // Comma-separated

        public bool IsActive { get; set; } = true;

        [MaxLength(255)]
        public string CreatedBy { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
   public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
 }

    #endregion
}
