using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Tracks every email sent through the system for analytics and open tracking
    /// </summary>
    [Table("email_sent_logs")]
    public class EmailSentLog
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        /// <summary>
        /// Unique tracking GUID for this email (used in tracking pixel URL)
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("tracking_id")]
        public string TrackingId { get; set; } = Guid.NewGuid().ToString("N");

        /// <summary>
        /// Recipient email address
        /// </summary>
        [Required]
        [StringLength(255)]
        [Column("recipient_email")]
        public string RecipientEmail { get; set; } = string.Empty;

        /// <summary>
        /// Recipient name (if available)
        /// </summary>
        [StringLength(255)]
        [Column("recipient_name")]
        public string? RecipientName { get; set; }

        /// <summary>
        /// Email subject line
        /// </summary>
        [StringLength(500)]
        [Column("subject")]
        public string? Subject { get; set; }

        /// <summary>
        /// Email provider used (MicrosoftGraph, SendGrid)
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("provider")]
        public string Provider { get; set; } = string.Empty;

        /// <summary>
        /// Provider's message ID (for reference)
        /// </summary>
        [StringLength(255)]
        [Column("message_id")]
        public string? MessageId { get; set; }

        /// <summary>
        /// Email type (OTP, Transactional, Notification, Marketing)
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("email_type")]
        public string EmailType { get; set; } = "Transactional";

        /// <summary>
        /// Current status (Sent, Opened, Bounced, Failed)
        /// </summary>
        [Required]
        [StringLength(50)]
        [Column("status")]
        public string Status { get; set; } = "Sent";

        /// <summary>
        /// When the email was sent
        /// </summary>
        [Required]
        [Column("sent_at")]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When the email was first opened (tracking pixel loaded)
        /// </summary>
        [Column("opened_at")]
        public DateTime? OpenedAt { get; set; }

        /// <summary>
        /// Number of times the email was opened
        /// </summary>
        [Column("open_count")]
        public int OpenCount { get; set; } = 0;

        /// <summary>
        /// Last time the email was opened
        /// </summary>
        [Column("last_opened_at")]
        public DateTime? LastOpenedAt { get; set; }

        /// <summary>
        /// IP address of the opener (from tracking pixel request)
        /// </summary>
        [StringLength(50)]
        [Column("opener_ip")]
        public string? OpenerIp { get; set; }

        /// <summary>
        /// User agent of the opener's email client
        /// </summary>
        [StringLength(500)]
        [Column("opener_user_agent")]
        public string? OpenerUserAgent { get; set; }

        /// <summary>
        /// Failure reason if email failed to send
        /// </summary>
        [StringLength(1000)]
        [Column("failure_reason")]
        public string? FailureReason { get; set; }

        /// <summary>
        /// Related order ID (if applicable)
        /// </summary>
        [Column("order_id")]
        public int? OrderId { get; set; }

        /// <summary>
        /// Related user email (sender context)
        /// </summary>
        [StringLength(255)]
        [Column("sender_email")]
        public string? SenderEmail { get; set; }

        /// <summary>
        /// Duration in milliseconds to send the email
        /// </summary>
        [Column("send_duration_ms")]
        public int SendDurationMs { get; set; } = 0;

        /// <summary>
        /// Additional metadata (JSON)
        /// </summary>
        [Column("metadata")]
        public string? Metadata { get; set; }

        /// <summary>
        /// Created timestamp
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Updated timestamp
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Email statistics response DTO
    /// </summary>
    public class EmailStatsResponse
    {
        public int TotalSent { get; set; }
        public int TotalOpened { get; set; }
        public int TotalFailed { get; set; }
        public double OpenRate { get; set; }
        public int TodaySent { get; set; }
        public int TodayOpened { get; set; }
        public int WeekSent { get; set; }
        public int MonthSent { get; set; }
        public Dictionary<string, int> ByProvider { get; set; } = new();
        public Dictionary<string, int> ByType { get; set; } = new();
        public Dictionary<string, int> ByStatus { get; set; } = new();
    }
}
