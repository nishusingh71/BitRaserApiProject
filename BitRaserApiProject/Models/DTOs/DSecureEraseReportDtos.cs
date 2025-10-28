using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
 /// Request model for generating BitRaserErase PDF reports
 /// </summary>
    public class DSecureEraseReportRequest
    {
        [Required]
        public string ReportId { get; set; } = string.Empty;

    [Required]
     public string ReportDate { get; set; } = string.Empty;

[Required]
      public string Software { get; set; } = "BitRaserErase 1.0.0.0";

     public string? DigitalIdentifier { get; set; }

 [Required]
        public string Os { get; set; } = string.Empty;

 [Required]
        public string Computer { get; set; } = string.Empty;

     [Required]
        public string Mac { get; set; } = string.Empty;

        public string? Manufacturer { get; set; }

     [Required]
  public string StartTime { get; set; } = string.Empty;

        [Required]
        public string EndTime { get; set; } = string.Empty;

        [Required]
        public string Method { get; set; } = string.Empty;

        [Required]
    public string Verification { get; set; } = string.Empty;

        [Required]
  public string ErasedBy { get; set; } = string.Empty;

      [Required]
     public string ValidatedBy { get; set; } = string.Empty;

        public List<ErasureLogItemDto>? Log { get; set; }
    }

    /// <summary>
    /// Erasure log item for the annexure table
    /// </summary>
    public class ErasureLogItemDto
    {
        [Required]
     public string Volume { get; set; } = string.Empty;

        [Required]
      public string Capacity { get; set; } = string.Empty;

    [Required]
  public string TotalSectors { get; set; } = string.Empty;

     [Required]
    public string SectorsErased { get; set; } = string.Empty;

      [Required]
        public string Status { get; set; } = "Completed";
    }

    /// <summary>
    /// Request model for branded report generation with file uploads
    /// </summary>
    public class BrandedReportRequest
    {
        [Required]
        public string ReportId { get; set; } = string.Empty;

     [Required]
      public string ReportDate { get; set; } = string.Empty;

        [Required]
        public string Software { get; set; } = "BitRaserErase 1.0.0.0";

        public string? DigitalIdentifier { get; set; }

        [Required]
        public string Os { get; set; } = string.Empty;

  [Required]
    public string Computer { get; set; } = string.Empty;

        [Required]
  public string Mac { get; set; } = string.Empty;

        public string? Manufacturer { get; set; }

        [Required]
   public string StartTime { get; set; } = string.Empty;

    [Required]
        public string EndTime { get; set; } = string.Empty;

      [Required]
   public string Method { get; set; } = string.Empty;

        [Required]
        public string Verification { get; set; } = string.Empty;

        [Required]
    public string ErasedBy { get; set; } = string.Empty;

        [Required]
        public string ValidatedBy { get; set; } = string.Empty;

        public List<ErasureLogItemDto>? Log { get; set; }

        // Branding options
        public string? ReportTitle { get; set; }
        public string? HeaderText { get; set; }
  public IFormFile? HeaderLeftLogo { get; set; }
  public IFormFile? HeaderRightLogo { get; set; }
        public IFormFile? WatermarkImage { get; set; }
    }

    /// <summary>
/// Response model for successful report generation
    /// </summary>
    public class DSecureEraseReportResponse
    {
  public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
 public string ReportId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
        public long FileSizeBytes { get; set; }
        public DateTime GeneratedAt { get; set; }
    }
}
