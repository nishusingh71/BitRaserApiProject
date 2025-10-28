using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// License Audit Report Models - Based on BitRaser License Audit Report Modal UI
    /// Screenshot 3: License Audit Report modal with utilization overview and product breakdown
    /// </summary>

    #region License Audit Report

    /// <summary>
  /// Complete license audit report response
    /// </summary>
    public class LicenseAuditReportResponseDto
    {
   public LicenseSummaryDto Summary { get; set; } = new();
        public LicenseUtilizationDto Utilization { get; set; } = new();
     public List<ProductLicenseBreakdownDto> ProductBreakdown { get; set; } = new();
        public DateTime GeneratedAt { get; set; }
   public string GeneratedBy { get; set; } = string.Empty;
    }

    /// <summary>
    /// License summary (4 cards at top of modal)
  /// </summary>
    public class LicenseSummaryDto
    {
      public int TotalLicenses { get; set; }
        public int ActiveLicenses { get; set; }
        public int AvailableLicenses { get; set; }
     public int ExpiredLicenses { get; set; }
    }

    /// <summary>
  /// License utilization overview
    /// </summary>
    public class LicenseUtilizationDto
    {
        public double OverallUtilizationPercentage { get; set; }
public int UsedLicenses { get; set; }
 public double UsedPercentage { get; set; }
  public int AvailableLicenses { get; set; }
 public double AvailablePercentage { get; set; }
 public int OptimizationPotential { get; set; }
        public double OptimizationPercentage { get; set; }
     public string UtilizationStatus { get; set; } = "Optimal"; // Optimal, Under-utilized, Over-utilized
 }

    /// <summary>
    /// Product license breakdown (table rows)
    /// </summary>
    public class ProductLicenseBreakdownDto
    {
      public string ProductName { get; set; } = string.Empty;
     public int TotalLicenses { get; set; }
   public int UsedLicenses { get; set; }
    public int AvailableLicenses { get; set; }
public double UtilizationPercentage { get; set; }
   public string UtilizationColor { get; set; } = "#4CAF50"; // Green, Orange, Blue
     public string Status { get; set; } = "Normal"; // High Usage, Low Usage, Normal
    }

    #endregion

    #region License Audit Report Options

    /// <summary>
    /// License audit report generation request
    /// </summary>
    public class GenerateLicenseAuditRequest
    {
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
 public bool IncludeProductBreakdown { get; set; } = true;
        public bool IncludeUtilizationDetails { get; set; } = true;
  public bool IncludeHistoricalData { get; set; } = false;
   public string? Department { get; set; }
     public string? UserGroup { get; set; }
    }

    #endregion

    #region License Utilization Details

    /// <summary>
    /// Detailed license utilization by category
  /// </summary>
    public class LicenseUtilizationDetailsDto
    {
   public Dictionary<string, int> LicensesByProduct { get; set; } = new();
        public Dictionary<string, int> LicensesByDepartment { get; set; } = new();
        public Dictionary<string, int> LicensesByUser { get; set; } = new();
   public List<LicenseExpiryAlert> ExpiringLicenses { get; set; } = new();
        public List<LicenseUsageAlert> UnderutilizedProducts { get; set; } = new();
    }

    public class LicenseExpiryAlert
    {
   public string ProductName { get; set; } = string.Empty;
     public int LicenseCount { get; set; }
   public DateTime ExpiryDate { get; set; }
 public int DaysUntilExpiry { get; set; }
public string Severity { get; set; } = "Info"; // Critical, Warning, Info
    }

    public class LicenseUsageAlert
    {
     public string ProductName { get; set; } = string.Empty;
  public int TotalLicenses { get; set; }
   public int UsedLicenses { get; set; }
public double UtilizationPercentage { get; set; }
  public string Recommendation { get; set; } = string.Empty;
  }

    #endregion

    #region License Historical Data

    /// <summary>
 /// Historical license usage trends
    /// </summary>
    public class LicenseHistoricalDataDto
    {
   public List<LicenseHistoryPoint> MonthlyHistory { get; set; } = new();
 public List<LicenseHistoryPoint> WeeklyHistory { get; set; } = new();
    }

    public class LicenseHistoryPoint
    {
      public DateTime Date { get; set; }
   public int TotalLicenses { get; set; }
        public int UsedLicenses { get; set; }
    public double UtilizationPercentage { get; set; }
    }

    #endregion

    #region Export Options

    /// <summary>
    /// Export license audit report request
    /// </summary>
  public class ExportLicenseAuditRequest
    {
  public string ExportType { get; set; } = "Detailed"; // Detailed, Optimization

        public string Format { get; set; } = "PDF"; // PDF, Excel, CSV

 public bool IncludeCharts { get; set; } = true;

      public bool IncludeRecommendations { get; set; } = true;

    public GenerateLicenseAuditRequest? Filters { get; set; }
    }

    #endregion

    #region Optimization Recommendations

    /// <summary>
 /// License optimization recommendations
    /// </summary>
    public class LicenseOptimizationDto
    {
  public int TotalSavingsPotential { get; set; }
public List<OptimizationRecommendation> Recommendations { get; set; } = new();
   public double EstimatedCostSavings { get; set; }
    }

    public class OptimizationRecommendation
    {
 public string ProductName { get; set; } = string.Empty;
        public string RecommendationType { get; set; } = string.Empty; // Reduce, Reallocate, Upgrade
   public int CurrentLicenses { get; set; }
        public int RecommendedLicenses { get; set; }
   public double PotentialSavings { get; set; }
   public string Reason { get; set; } = string.Empty;
    public string Priority { get; set; } = "Medium"; // High, Medium, Low
    }

    #endregion
}
