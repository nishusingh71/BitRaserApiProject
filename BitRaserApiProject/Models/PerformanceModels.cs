using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Performance Metrics Models - Based on D-Secure Performance Dashboard UI
    /// Screenshot 1: Performance page with Monthly Growth, Avg Duration, Uptime, and Throughput
    /// </summary>

    #region Performance Dashboard

    /// <summary>
    /// Complete performance dashboard response
    /// </summary>
    public class PerformanceDashboardDto
    {
public MonthlyGrowthDto MonthlyGrowth { get; set; } = new();
        public AverageDurationDto AverageDuration { get; set; } = new();
      public UptimeDto Uptime { get; set; } = new();
  public ThroughputDto Throughput { get; set; } = new();
        public List<PerformanceTimeSeriesData> MonthlyGrowthChart { get; set; } = new();
        public List<PerformanceTimeSeriesData> AverageDurationChart { get; set; } = new();
        public List<PerformanceTimeSeriesData> UptimeChart { get; set; } = new();
  public List<ThroughputBarData> ThroughputChart { get; set; } = new();
    }

    /// <summary>
    /// Monthly Growth metrics
    /// </summary>
    public class MonthlyGrowthDto
    {
        public int TotalRecords { get; set; }
        public double PercentageChange { get; set; }
  public bool IsPositive { get; set; }
        public int PreviousMonthRecords { get; set; }
     public int CurrentMonthRecords { get; set; }
    }

    /// <summary>
    /// Average Duration metrics
    /// </summary>
    public class AverageDurationDto
    {
        public string Duration { get; set; } = "0m 0s";
        public int TotalMinutes { get; set; }
        public int TotalSeconds { get; set; }
        public List<string> RecentDurations { get; set; } = new();
    }

  /// <summary>
    /// Uptime metrics
    /// </summary>
    public class UptimeDto
    {
 public double UptimePercentage { get; set; }
    public int TotalUpMinutes { get; set; }
     public int TotalDownMinutes { get; set; }
   public DateTime LastDowntime { get; set; }
        public string Status { get; set; } = "Operational";
    }

    /// <summary>
    /// Throughput metrics
    /// </summary>
    public class ThroughputDto
    {
        public int TotalOperations { get; set; }
        public double OperationsPerHour { get; set; }
        public double OperationsPerDay { get; set; }
   public string PeakHour { get; set; } = "12:00 PM";
    }

    /// <summary>
    /// Time series data for line charts
    /// </summary>
    public class PerformanceTimeSeriesData
    {
        public DateTime Date { get; set; }
   public double Value { get; set; }
        public string Label { get; set; } = string.Empty;
    }

    /// <summary>
  /// Bar chart data for throughput
    /// </summary>
    public class ThroughputBarData
    {
        public string Month { get; set; } = string.Empty;
   public int Operations { get; set; }
  public string Color { get; set; } = "#4A90E2";
    }

    #endregion

    #region Performance Statistics

    /// <summary>
    /// System performance statistics
    /// </summary>
    public class SystemPerformanceStatsDto
    {
        public int TotalMachines { get; set; }
        public int ActiveMachines { get; set; }
 public int TotalUsers { get; set; }
   public int ActiveUsers { get; set; }
        public int TotalReports { get; set; }
  public int ReportsThisMonth { get; set; }
        public double AverageReportTime { get; set; }
        public double SystemUptime { get; set; }
        public long TotalDataProcessed { get; set; }
     public DateTime LastUpdated { get; set; }
    }

    /// <summary>
    /// Performance trends over time
    /// </summary>
    public class PerformanceTrendsDto
    {
        public List<MonthlyTrendData> MonthlyTrends { get; set; } = new();
   public List<DailyTrendData> DailyTrends { get; set; } = new();
        public List<HourlyTrendData> HourlyTrends { get; set; } = new();
    }

    public class MonthlyTrendData
    {
  public int Year { get; set; }
        public int Month { get; set; }
      public string MonthName { get; set; } = string.Empty;
    public int TotalOperations { get; set; }
      public double AverageDuration { get; set; }
   public double UptimePercentage { get; set; }
    }

    public class DailyTrendData
    {
        public DateTime Date { get; set; }
public int TotalOperations { get; set; }
      public double AverageDuration { get; set; }
        public int Errors { get; set; }
    }

    public class HourlyTrendData
    {
  public int Hour { get; set; }
     public int Operations { get; set; }
public double AverageResponseTime { get; set; }
    }

    #endregion

    #region Performance Filters

    /// <summary>
    /// Performance query filters
    /// </summary>
    public class PerformanceFilterDto
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? TimeRange { get; set; } // today, week, month, year
 public string? MetricType { get; set; } // growth, duration, uptime, throughput
        public string? GroupBy { get; set; } // hour, day, week, month
    }

    #endregion
}
