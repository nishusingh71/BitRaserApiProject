using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DSecureApi.Models
{
    /// <summary>
    /// Email quota tracking for hybrid email providers
    /// Supports SendGrid, Microsoft Graph, and future providers
    /// </summary>
    [Table("EmailQuotas")]
    public class EmailQuota
    {
        [Key]
        public int Id { get; set; }

        /// <summary>
        /// Provider name: 'SendGrid' / 'MicrosoftGraph' / etc.
        /// </summary>
        [Required]
        [StringLength(50)]
        public string ProviderName { get; set; } = string.Empty;

        /// <summary>
        /// Account identifier (API key hash for SendGrid, email for Graph)
        /// </summary>
        [StringLength(255)]
        public string? AccountIdentifier { get; set; }

        /// <summary>
        /// Daily email limit for this provider/account
        /// </summary>
        public int DailyLimit { get; set; } = 100;

        /// <summary>
        /// Number of emails sent today
        /// </summary>
        public int DailySent { get; set; } = 0;

        /// <summary>
        /// Monthly email limit for this provider/account
        /// </summary>
        public int MonthlyLimit { get; set; } = 3000;

        /// <summary>
        /// Number of emails sent this month
        /// </summary>
        public int MonthlySent { get; set; } = 0;

        /// <summary>
        /// Last date when daily counter was reset (UTC)
        /// </summary>
        public DateTime LastDailyReset { get; set; } = DateTime.UtcNow.Date;

        /// <summary>
        /// Last date when monthly counter was reset (UTC)
        /// </summary>
        public DateTime LastMonthlyReset { get; set; } = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

        /// <summary>
        /// Whether this provider is enabled for sending
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// Priority order (1 = highest priority)
        /// </summary>
        public int Priority { get; set; } = 1;

        /// <summary>
        /// Provider health status (for circuit breaker)
        /// </summary>
        public bool IsHealthy { get; set; } = true;

        /// <summary>
        /// Last error message (if any)
        /// </summary>
        public string? LastErrorMessage { get; set; }

        /// <summary>
        /// Last successful send timestamp
        /// </summary>
        public DateTime? LastSuccessAt { get; set; }

        /// <summary>
        /// Last failure timestamp
        /// </summary>
        public DateTime? LastFailureAt { get; set; }

        /// <summary>
        /// Consecutive failure count (for circuit breaker)
        /// </summary>
        public int ConsecutiveFailures { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Helper methods
        public bool HasDailyQuota => DailySent < DailyLimit;
        public bool HasMonthlyQuota => MonthlySent < MonthlyLimit;
        public bool CanSend => IsEnabled && IsHealthy && HasDailyQuota && HasMonthlyQuota;
        public int RemainingDailyQuota => Math.Max(0, DailyLimit - DailySent);
        public int RemainingMonthlyQuota => Math.Max(0, MonthlyLimit - MonthlySent);
    }

    /// <summary>
    /// Email log for auditing and debugging
    /// </summary>
    [Table("EmailLogs")]
    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        [StringLength(50)]
        public string ProviderUsed { get; set; } = string.Empty;

        [StringLength(255)]
        public string RecipientEmail { get; set; } = string.Empty;

        [StringLength(500)]
        public string Subject { get; set; } = string.Empty;

        /// <summary>
        /// Email type: Transactional, OTP, Notification, Marketing
        /// </summary>
        [StringLength(50)]
        public string EmailType { get; set; } = "Transactional";

        /// <summary>
        /// Status: Sent, Failed, Queued, Retrying
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "Queued";

        public string? ErrorMessage { get; set; }

        public DateTime? SentAt { get; set; }

        public int? OrderId { get; set; }

        /// <summary>
        /// Time taken to send (ms)
        /// </summary>
        public int? SendDurationMs { get; set; }

        /// <summary>
        /// Number of retry attempts
        /// </summary>
        public int RetryCount { get; set; } = 0;

        /// <summary>
        /// Whether email had attachments
        /// </summary>
        public bool HasAttachments { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
