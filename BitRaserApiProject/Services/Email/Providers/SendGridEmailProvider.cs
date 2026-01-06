using SendGrid;
using SendGrid.Helpers.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BitRaserApiProject.Services.Email.Providers
{
    /// <summary>
    /// SendGrid email provider implementation
    /// Supports free plan limits with quota management
    /// </summary>
    public class SendGridEmailProvider : IEmailProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SendGridEmailProvider> _logger;
        private readonly IEmailQuotaService _quotaService;
        
        private string? _apiKey;
        private string _fromEmail = "noreply@dsecuretech.com";
        private string _fromName = "DSecure";
        private bool _isInitialized = false;

        public string ProviderName => "SendGrid";
        public int Priority { get; private set; } = 2;

        public SendGridEmailProvider(
            IConfiguration configuration,
            ILogger<SendGridEmailProvider> logger,
            IEmailQuotaService quotaService)
        {
            _configuration = configuration;
            _logger = logger;
            _quotaService = quotaService;
        }

        public Task InitializeAsync()
        {
            // Load from environment variables first, then config
            _apiKey = Environment.GetEnvironmentVariable("SendGrid__ApiKey")
                ?? Environment.GetEnvironmentVariable("SENDGRID_API_KEY")
                ?? _configuration["SendGrid:ApiKey"];

            _fromEmail = Environment.GetEnvironmentVariable("SendGrid__FromEmail")
                ?? Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL")
                ?? _configuration["SendGrid:FromEmail"]
                ?? "noreply@dsecuretech.com";

            _fromName = Environment.GetEnvironmentVariable("SendGrid__FromName")
                ?? Environment.GetEnvironmentVariable("SENDGRID_FROM_NAME")
                ?? _configuration["SendGrid:FromName"]
                ?? "DSecure";

            var priorityStr = Environment.GetEnvironmentVariable("SendGrid__Priority")
                ?? _configuration["SendGrid:Priority"];
            
            if (int.TryParse(priorityStr, out var priority))
            {
                Priority = priority;
            }

            _isInitialized = !string.IsNullOrEmpty(_apiKey);

            if (_isInitialized)
            {
                _logger.LogInformation("✅ SendGrid provider initialized - FromEmail: {FromEmail}", _fromEmail);
            }
            else
            {
                _logger.LogWarning("⚠️ SendGrid provider not configured - API key missing");
            }

            return Task.CompletedTask;
        }

        public async Task<bool> IsAvailableAsync()
        {
            if (!_isInitialized || string.IsNullOrEmpty(_apiKey))
                return false;

            return await _quotaService.HasAvailableQuotaAsync(ProviderName);
        }

        public async Task<int> GetRemainingQuotaAsync()
        {
            return await _quotaService.GetRemainingQuotaAsync(ProviderName);
        }

        public async Task<EmailSendResult> SendEmailAsync(EmailSendRequest request)
        {
            if (string.IsNullOrEmpty(_apiKey))
            {
                return EmailSendResult.Failed("SendGrid API key not configured", ProviderName);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("📧 SendGrid: Sending email to {Email} - Subject: {Subject}", 
                    request.ToEmail, request.Subject);

                var client = new SendGridClient(_apiKey);
                
                var from = new EmailAddress(
                    request.FromEmail ?? _fromEmail, 
                    request.FromName ?? _fromName
                );
                var to = new EmailAddress(request.ToEmail, request.ToName);
                
                var msg = MailHelper.CreateSingleEmail(
                    from, 
                    to, 
                    request.Subject, 
                    request.PlainTextBody, 
                    request.HtmlBody
                );

                // Add attachments if present
                if (request.Attachments != null && request.Attachments.Count > 0)
                {
                    foreach (var attachment in request.Attachments)
                    {
                        msg.AddAttachment(
                            attachment.FileName,
                            Convert.ToBase64String(attachment.Content),
                            attachment.ContentType
                        );
                        _logger.LogDebug("📎 Attached: {FileName} ({Size} bytes)", 
                            attachment.FileName, attachment.Content.Length);
                    }
                }

                // Add Reply-To if specified
                if (!string.IsNullOrEmpty(request.ReplyToEmail))
                {
                    msg.SetReplyTo(new EmailAddress(request.ReplyToEmail));
                }

                var response = await client.SendEmailAsync(msg);
                stopwatch.Stop();

                if (response.IsSuccessStatusCode)
                {
                    await _quotaService.IncrementUsageAsync(ProviderName);
                    await _quotaService.RecordSuccessAsync(ProviderName);
                    
                    _logger.LogInformation("✅ SendGrid: Email sent successfully to {Email} in {Duration}ms", 
                        request.ToEmail, stopwatch.ElapsedMilliseconds);
                    
                    return EmailSendResult.Succeeded(
                        ProviderName, 
                        response.Headers?.GetValues("X-Message-Id")?.FirstOrDefault(),
                        (int)stopwatch.ElapsedMilliseconds
                    );
                }
                else
                {
                    var body = await response.Body.ReadAsStringAsync();
                    var errorMessage = $"SendGrid returned {response.StatusCode}: {body}";
                    
                    await _quotaService.RecordFailureAsync(ProviderName, errorMessage);
                    _logger.LogError("❌ SendGrid: Failed to send email - {Error}", errorMessage);
                    
                    return EmailSendResult.Failed(errorMessage, ProviderName);
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _quotaService.RecordFailureAsync(ProviderName, ex.Message);
                
                _logger.LogError(ex, "❌ SendGrid: Exception while sending email to {Email}", request.ToEmail);
                return EmailSendResult.Failed(ex.Message, ProviderName, ex);
            }
        }
    }
}
