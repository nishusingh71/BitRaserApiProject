using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// System Logs Models - Based on D-Secure System Logs UI
    /// Screenshot 1: System Logs page with level, category, date filters and search
    /// </summary>

    #region System Logs List

    /// <summary>
    /// System logs list response
    /// </summary>
    public class SystemLogsListDto
    {
        public List<SystemLogItemDto> Logs { get; set; } = new();
  public int TotalCount { get; set; }
        public int Page { get; set; }
    public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>
    /// Individual system log item
    /// </summary>
    public class SystemLogItemDto
    {
        public int LogId { get; set; }
        public string Level { get; set; } = string.Empty; // INFO, SUCCESS, WARNING, ERROR, CRITICAL
        public string EventType { get; set; } = string.Empty; // API, Data Erasure, Performance, etc.
        public DateTime Timestamp { get; set; }
      public string UserEmail { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty; // API Gateway, Data Engine, etc.
  public bool CanViewDetails { get; set; } = true;
    }

    #endregion

    #region System Logs Filters

    /// <summary>
    /// System logs filters (from Screenshot 1)
    /// </summary>
    public class SystemLogsFiltersDto
    {
        [MaxLength(200)]
        public string? Search { get; set; }

        public string? Level { get; set; } // All Levels, INFO, SUCCESS, WARNING, ERROR, CRITICAL

        public string? Category { get; set; } // All Categories, API, Data Erasure, Performance, etc.

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }

        [MaxLength(50)]
      public string? SortBy { get; set; } = "Timestamp"; // Timestamp, Level, Category

        public int SortDirection { get; set; } = -1; // 1 = Ascending, -1 = Descending

public int Page { get; set; } = 1;

  public int PageSize { get; set; } = 10;
 }

    #endregion

    #region System Log Details

    /// <summary>
    /// Detailed system log entry
    /// </summary>
    public class SystemLogDetailDto
    {
        public int LogId { get; set; }
        public string Level { get; set; } = string.Empty;
        public string EventType { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public string UserEmail { get; set; } = string.Empty;
  public string Message { get; set; } = string.Empty;
        public string Source { get; set; } = string.Empty;
  public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
  public Dictionary<string, object> DetailsJson { get; set; } = new();
        public string? StackTrace { get; set; }
        public string? RequestUrl { get; set; }
        public string? HttpMethod { get; set; }
        public int? ResponseCode { get; set; }
    }

    #endregion

    #region Log Statistics

    /// <summary>
    /// System logs statistics
    /// </summary>
    public class SystemLogsStatisticsDto
    {
   public int TotalLogs { get; set; }
        public int InfoLogs { get; set; }
        public int SuccessLogs { get; set; }
      public int WarningLogs { get; set; }
        public int ErrorLogs { get; set; }
        public int CriticalLogs { get; set; }
   public int LogsToday { get; set; }
        public int LogsThisWeek { get; set; }
        public Dictionary<string, int> LogsByCategory { get; set; } = new();
        public Dictionary<string, int> LogsByLevel { get; set; } = new();
        public List<SystemLogItemDto> RecentLogs { get; set; } = new();
    }

    #endregion

  #region Export Logs

    /// <summary>
    /// Export system logs request
    /// </summary>
    public class ExportSystemLogsRequest
    {
        public List<int>? LogIds { get; set; }

        public string ExportFormat { get; set; } = "CSV"; // CSV, Excel, JSON

        public bool ExportAll { get; set; } = false;

        public SystemLogsFiltersDto? Filters { get; set; }
    }

    /// <summary>
    /// Export response
    /// </summary>
    public class ExportSystemLogsResponse
    {
      public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? DownloadUrl { get; set; }
        public string? FileName { get; set; }
        public long FileSizeBytes { get; set; }
        public DateTime ExportedAt { get; set; }
    }

    #endregion

    #region Filter Options

    /// <summary>
    /// Available filter options
  /// </summary>
    public class SystemLogsFilterOptionsDto
    {
 public List<string> Levels { get; set; } = new() 
        { 
            "All Levels", 
    "INFO", 
    "SUCCESS", 
          "WARNING", 
 "ERROR", 
       "CRITICAL" 
        };

   public List<string> Categories { get; set; } = new() 
        { 
 "All Categories",
         "API",
"Data Erasure",
  "Performance",
            "Authentication",
  "Authorization",
            "System",
        "Database",
 "Network"
        };

   public List<string> SortOptions { get; set; } = new() 
        { 
            "Timestamp", 
       "Level", 
            "Category",
            "User"
  };
    }

    #endregion

    #region Log Actions

    /// <summary>
    /// Clear logs request
    /// </summary>
    public class ClearLogsRequest
    {
   public DateTime? OlderThan { get; set; }
        public string? Level { get; set; }
     public string? Category { get; set; }
    }

    /// <summary>
    /// Clear logs response
    /// </summary>
    public class ClearLogsResponse
    {
   public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    public int LogsCleared { get; set; }
    }

    #endregion
}
