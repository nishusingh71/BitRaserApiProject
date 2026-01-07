using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace DSecureApi.Models
{
    /// <summary>
    /// Download entity for tracking software download statistics
    /// </summary>
    [Table("downloads")]
    public class Download
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        [Column("product_name")]
        public string ProductName { get; set; } = string.Empty; // "File Eraser", "Drive Eraser", "Network Eraser"

        [StringLength(50)]
        [Column("version")]
        public string? Version { get; set; }

        [Required]
        [StringLength(50)]
        [Column("platform")]
        public string Platform { get; set; } = string.Empty; // "Windows", "macOS", "Linux"

        [StringLength(50)]
        [Column("architecture")]
        public string? Architecture { get; set; } // "x64", "ARM64", "Intel", "Apple Silicon", "DEB", "RPM"

        [StringLength(255)]
        [Column("user_id")]
        public string? UserId { get; set; } // Nullable for anonymous downloads

        [StringLength(255)]
        [Column("user_email")]
        public string? UserEmail { get; set; }

        [StringLength(50)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        [Column("user_agent")]
        public string? UserAgent { get; set; }

        [Column("downloaded_at")]
        public DateTime DownloadedAt { get; set; } = DateTime.UtcNow;

        [Column("file_size")]
        public long FileSize { get; set; } = 0; // In bytes

        [Column("download_completed")]
        public bool DownloadCompleted { get; set; } = true;

        [StringLength(100)]
        [Column("country")]
        public string? Country { get; set; }

        [StringLength(100)]
        [Column("city")]
        public string? City { get; set; }

        [StringLength(100)]
        [Column("referrer")]
        public string? Referrer { get; set; }

        [StringLength(500)]
        [Column("download_source")]
        public string? DownloadSource { get; set; } // "website", "api", "partner"
    }

    #region DTOs

    /// <summary>
    /// Overall download statistics
    /// </summary>
    public class DownloadStatsDto
    {
        [JsonPropertyName("totalDownloads")]
        public int TotalDownloads { get; set; }

        [JsonPropertyName("windowsDownloads")]
        public int WindowsDownloads { get; set; }

        [JsonPropertyName("macOsDownloads")]
        public int MacOsDownloads { get; set; }

        [JsonPropertyName("linuxDownloads")]
        public int LinuxDownloads { get; set; }

        [JsonPropertyName("todayDownloads")]
        public int TodayDownloads { get; set; }

        [JsonPropertyName("thisWeekDownloads")]
        public int ThisWeekDownloads { get; set; }

        [JsonPropertyName("thisMonthDownloads")]
        public int ThisMonthDownloads { get; set; }
    }

    /// <summary>
    /// Download stats per product
    /// </summary>
    public class ProductDownloadDto
    {
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("totalDownloads")]
        public int TotalDownloads { get; set; }

        [JsonPropertyName("platformBreakdown")]
        public Dictionary<string, int> PlatformBreakdown { get; set; } = new();

        [JsonPropertyName("architectureBreakdown")]
        public Dictionary<string, int> ArchitectureBreakdown { get; set; } = new();

        [JsonPropertyName("lastDownloadDate")]
        public string? LastDownloadDate { get; set; }
    }

    /// <summary>
    /// Request to record a new download
    /// </summary>
    public class RecordDownloadDto
    {
        [Required(ErrorMessage = "Product name is required")]
        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [Required(ErrorMessage = "Platform is required")]
        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;

        [JsonPropertyName("architecture")]
        public string? Architecture { get; set; }

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; } = 0;

        [JsonPropertyName("downloadSource")]
        public string? DownloadSource { get; set; }
    }

    /// <summary>
    /// User download history item
    /// </summary>
    public class DownloadHistoryDto
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("productName")]
        public string ProductName { get; set; } = string.Empty;

        [JsonPropertyName("version")]
        public string? Version { get; set; }

        [JsonPropertyName("platform")]
        public string Platform { get; set; } = string.Empty;

        [JsonPropertyName("architecture")]
        public string? Architecture { get; set; }

        [JsonPropertyName("downloadedAt")]
        public DateTime DownloadedAt { get; set; }

        [JsonPropertyName("fileSize")]
        public long FileSize { get; set; }

        [JsonPropertyName("downloadCompleted")]
        public bool DownloadCompleted { get; set; }
    }

    /// <summary>
    /// Response for recording a download
    /// </summary>
    public class RecordDownloadResponseDto
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("downloadId")]
        public int? DownloadId { get; set; }
    }

    /// <summary>
    /// Daily download trend data
    /// </summary>
    public class DownloadTrendDto
    {
        [JsonPropertyName("date")]
        public string Date { get; set; } = string.Empty;

        [JsonPropertyName("count")]
        public int Count { get; set; }
    }

    #endregion
}
