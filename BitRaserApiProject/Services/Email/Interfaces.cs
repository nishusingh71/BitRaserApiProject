namespace BitRaserApiProject.Services.Email
{
    /// <summary>
    /// Email type for priority-based provider selection
    /// </summary>
    public enum EmailType
    {
        /// <summary>
        /// Highest priority - OTP, password reset, security alerts
        /// </summary>
        OTP = 1,

        /// <summary>
        /// High priority - Payment confirmations, credentials
        /// </summary>
        Transactional = 2,

        /// <summary>
        /// Medium priority - General notifications
        /// </summary>
        Notification = 3,

        /// <summary>
        /// Low priority - Marketing, newsletters
        /// </summary>
        Marketing = 4
    }

    /// <summary>
    /// Email attachment model
    /// </summary>
    public class EmailAttachment
    {
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public byte[] Content { get; set; }

        public EmailAttachment(string fileName, string contentType, byte[] content)
        {
            FileName = fileName;
            ContentType = contentType;
            Content = content;
        }
    }

    /// <summary>
    /// Email send request with all required information
    /// </summary>
    public class EmailSendRequest
    {
        public string ToEmail { get; set; } = string.Empty;
        public string ToName { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string HtmlBody { get; set; } = string.Empty;
        public string? PlainTextBody { get; set; }
        public EmailType Type { get; set; } = EmailType.Transactional;
        public List<EmailAttachment>? Attachments { get; set; }
        public int? OrderId { get; set; }
        public string? FromEmail { get; set; }
        public string? FromName { get; set; }
        public string? ReplyToEmail { get; set; }
        public List<string>? CcEmails { get; set; }
        public Dictionary<string, string>? CustomHeaders { get; set; }
        
        /// <summary>
        /// If true, use only primary provider (MS Graph) and don't fallback to secondary providers
        /// </summary>
        public bool ForcePrimaryProvider { get; set; } = false;
    }

    /// <summary>
    /// Email send result
    /// </summary>
    public class EmailSendResult
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ProviderUsed { get; set; }
        public string? MessageId { get; set; }
        public int DurationMs { get; set; }
        public Exception? Exception { get; set; }

        public static EmailSendResult Succeeded(string providerName, string? messageId = null, int durationMs = 0)
        {
            return new EmailSendResult
            {
                Success = true,
                ProviderUsed = providerName,
                MessageId = messageId,
                DurationMs = durationMs,
                Message = "Email sent successfully"
            };
        }

        public static EmailSendResult Failed(string message, string? providerName = null, Exception? exception = null)
        {
            return new EmailSendResult
            {
                Success = false,
                Message = message,
                ProviderUsed = providerName,
                Exception = exception
            };
        }
    }

    /// <summary>
    /// Interface for pluggable email providers
    /// </summary>
    public interface IEmailProvider
    {
        /// <summary>
        /// Provider name for logging and quota tracking
        /// </summary>
        string ProviderName { get; }

        /// <summary>
        /// Provider priority (1 = highest)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// Send email via this provider
        /// </summary>
        Task<EmailSendResult> SendEmailAsync(EmailSendRequest request);

        /// <summary>
        /// Check if provider is available (quota + health)
        /// </summary>
        Task<bool> IsAvailableAsync();

        /// <summary>
        /// Get remaining quota for this provider
        /// </summary>
        Task<int> GetRemainingQuotaAsync();

        /// <summary>
        /// Initialize provider (load config, validate credentials)
        /// </summary>
        Task InitializeAsync();
    }

    /// <summary>
    /// Interface for quota management service
    /// </summary>
    public interface IEmailQuotaService
    {
        Task<bool> HasAvailableQuotaAsync(string providerName);
        Task IncrementUsageAsync(string providerName);
        Task<int> GetRemainingQuotaAsync(string providerName);
        Task ResetDailyQuotasAsync();
        Task ResetMonthlyQuotasAsync();
        Task RecordSuccessAsync(string providerName);
        Task RecordFailureAsync(string providerName, string errorMessage);
        Task<List<Models.EmailQuota>> GetAllQuotasAsync();
        Task InitializeQuotasAsync();
    }

    /// <summary>
    /// Interface for email orchestrator
    /// </summary>
    public interface IEmailOrchestrator
    {
        Task<EmailSendResult> SendEmailAsync(EmailSendRequest request);
        Task<List<IEmailProvider>> GetAvailableProvidersAsync(EmailType emailType);
    }
}
