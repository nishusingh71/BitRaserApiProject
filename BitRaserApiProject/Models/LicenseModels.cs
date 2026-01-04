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

        /// <summary>
        /// Maximum devices allowed for cloud activation (enterprise feature)
        /// Note: Add column to DB: ALTER TABLE licenses ADD COLUMN max_devices INT DEFAULT 1;
        /// </summary>
        [NotMapped] // Temporarily disabled until DB column exists
        public int MaxDevices { get; set; } = 1;

        /// <summary>
        /// Navigation property for associated devices (cloud activation)
        /// </summary>
        [NotMapped] // Temporarily disabled until license_devices table exists
        public virtual ICollection<LicenseDevice>? Devices { get; set; }
    }

    /// <summary>
    /// License Device entity for multi-device cloud activation
    /// Tracks all devices activated under a license with enhanced hardware details
    /// </summary>
    [Table("license_devices")]
    public class LicenseDevice
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("license_id")]
        public int LicenseId { get; set; }

        [Required]
        [MaxLength(255)]
        [Column("hwid")]
        public string Hwid { get; set; } = string.Empty;

        [MaxLength(255)]
        [Column("hwid_hash")]
        public string? HwidHash { get; set; } // SHA-256 hash for security

        [MaxLength(255)]
        [Column("hardware_fingerprint")]
        public string? HardwareFingerprint { get; set; } // Combined hardware hash

        [MaxLength(255)]
        [Column("machine_name")]
        public string? MachineName { get; set; }

        [MaxLength(255)]
        [Column("os_info")]
        public string? OsInfo { get; set; }

        [MaxLength(100)]
        [Column("os_build")]
        public string? OsBuild { get; set; }

        // Enhanced Hardware Details
        [MaxLength(255)]
        [Column("cpu_id")]
        public string? CpuId { get; set; }

        [MaxLength(255)]
        [Column("cpu_name")]
        public string? CpuName { get; set; }

        [MaxLength(50)]
        [Column("mac_address")]
        public string? MacAddress { get; set; }

        [MaxLength(255)]
        [Column("motherboard_serial")]
        public string? MotherboardSerial { get; set; }

        [MaxLength(255)]
        [Column("disk_serial")]
        public string? DiskSerial { get; set; }

        [MaxLength(255)]
        [Column("gpu_info")]
        public string? GpuInfo { get; set; }

        [Column("ram_gb")]
        public int? RamGb { get; set; }

        [MaxLength(100)]
        [Column("timezone")]
        public string? Timezone { get; set; }

        [MaxLength(45)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [Column("activated_at")]
        public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;

        [Column("last_seen")]
        public DateTime? LastSeen { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // RSA Token
        [Column("activation_token")]
        public string? ActivationToken { get; set; }

        /// <summary>
        /// Navigation property to parent license
        /// </summary>
        [ForeignKey("LicenseId")]
        public virtual LicenseActivation? License { get; set; }
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
