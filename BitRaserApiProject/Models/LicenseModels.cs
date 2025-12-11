using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// License Activation System Models
    /// Separate from existing License Management
    /// Matches Python controller expectations
    /// </summary>

    /// <summary>
    /// License entity for activation system
    /// Stored in 'licenses' table
    /// </summary>
    [Table("licenses")]
    public class LicenseActivation
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        [Column("license_key")]
        public string LicenseKey { get; set; } = string.Empty;

        [MaxLength(128)]
        [Column("hwid")]
        public string? Hwid { get; set; }

        [Required]
        [Column("expiry_days")]
        public int ExpiryDays { get; set; } // ✅ Changed from expiry_date to expiry_days

        [Required]
        [MaxLength(32)]
        [Column("edition")]
        public string Edition { get; set; } = "BASIC"; // BASIC / PRO / ENTERPRISE

        [Required]
        [MaxLength(16)]
        [Column("status")]
        public string Status { get; set; } = "ACTIVE"; // ACTIVE / REVOKED / EXPIRED

        [Column("server_revision")]
        public int ServerRevision { get; set; } = 1;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_seen")]
        public DateTime? LastSeen { get; set; }

        [MaxLength(255)]
        [Column("user_email")]
        public string? UserEmail { get; set; }

        [MaxLength(500)]
        [Column("notes")]
        public string? Notes { get; set; }

        /// <summary>
        /// Calculate expiry date from activation date
        /// </summary>
        [NotMapped]
        public DateTime? ExpiryDate => CreatedAt.AddDays(ExpiryDays);

        /// <summary>
        /// Check if license is expired
        /// </summary>
        [NotMapped]
        public bool IsExpired => ExpiryDate.HasValue && ExpiryDate.Value.Date < DateTime.UtcNow.Date;

        /// <summary>
        /// Remaining days until expiry
        /// </summary>
        [NotMapped]
        public int RemainingDays
        {
            get
            {
                if (!ExpiryDate.HasValue) return 0;
                var remaining = (ExpiryDate.Value.Date - DateTime.UtcNow.Date).Days;
                return remaining > 0 ? remaining : 0;
            }
        }
    }

    /// <summary>
    /// License usage history/logs
    /// </summary>
    [Table("license_usage_logs")]
    public class LicenseUsageLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [MaxLength(64)]
        [Column("license_key")]
        public string LicenseKey { get; set; } = string.Empty;

        [MaxLength(128)]
        [Column("hwid")]
        public string? Hwid { get; set; }

        [Required]
        [MaxLength(50)]
        [Column("action")]
        public string Action { get; set; } = string.Empty; // ACTIVATE / RENEW / UPGRADE / SYNC / REVOKE

        [MaxLength(255)]
        [Column("user_email")]
        public string? UserEmail { get; set; }

        [Column("old_edition")]
        public string? OldEdition { get; set; }

        [Column("new_edition")]
        public string? NewEdition { get; set; }

        [Column("old_expiry_days")]
        public int? OldExpiryDays { get; set; }

        [Column("new_expiry_days")]
        public int? NewExpiryDays { get; set; }

        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("user_agent")]
        public string? UserAgent { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        [Column("notes")]
        public string? Notes { get; set; }
    }
}
