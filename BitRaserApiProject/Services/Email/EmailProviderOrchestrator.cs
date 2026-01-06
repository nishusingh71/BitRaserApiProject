using BitRaserApiProject.Data;
using BitRaserApiProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BitRaserApiProject.Services.Email
{
    /// <summary>
    /// Email provider orchestrator - Smart decision engine for hybrid email delivery
    /// Manages multiple providers, handles failover, logs all sends
    /// </summary>
    public class EmailProviderOrchestrator : IEmailOrchestrator
    {
        private readonly IEnumerable<IEmailProvider> _providers;
        private readonly IEmailQuotaService _quotaService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailProviderOrchestrator> _logger;

        public EmailProviderOrchestrator(
            IEnumerable<IEmailProvider> providers,
            IEmailQuotaService quotaService,
            ApplicationDbContext context,
            ILogger<EmailProviderOrchestrator> logger)
        {
            _providers = providers;
            _quotaService = quotaService;
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Send email using best available provider
        /// Automatically fails over to next provider on error
        /// </summary>
        public async Task<EmailSendResult> SendEmailAsync(EmailSendRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            var log = new EmailLog
            {
                RecipientEmail = request.ToEmail,
                Subject = request.Subject,
                EmailType = request.Type.ToString(),
                OrderId = request.OrderId,
                HasAttachments = request.Attachments?.Count > 0,
                Status = "Queued"
            };

            try
            {
                _logger.LogInformation("📧 Orchestrator: Processing email to {Email} - Type: {Type}", 
                    request.ToEmail, request.Type);

                // Get available providers sorted by priority
                var availableProviders = await GetAvailableProvidersAsync(request.Type);

                if (!availableProviders.Any())
                {
                    _logger.LogError("❌ No email providers available! All quotas exhausted or providers unhealthy.");
                    
                    log.Status = "Failed";
                    log.ErrorMessage = "No providers available - all quotas exhausted";
                    await SaveLogAsync(log);
                    
                    return EmailSendResult.Failed("No email providers available. All quotas exhausted or providers are unhealthy.");
                }

                _logger.LogInformation("📊 Available providers: {Providers}", 
                    string.Join(", ", availableProviders.Select(p => $"{p.ProviderName}({p.Priority})")));

                // Try each provider in order
                foreach (var provider in availableProviders)
                {
                    _logger.LogInformation("🔄 Trying provider: {Provider}", provider.ProviderName);
                    
                    var result = await provider.SendEmailAsync(request);

                    if (result.Success)
                    {
                        stopwatch.Stop();
                        
                        log.ProviderUsed = provider.ProviderName;
                        log.Status = "Sent";
                        log.SentAt = DateTime.UtcNow;
                        log.SendDurationMs = (int)stopwatch.ElapsedMilliseconds;
                        await SaveLogAsync(log);

                        _logger.LogInformation("✅ Email sent successfully via {Provider} in {Duration}ms",
                            provider.ProviderName, stopwatch.ElapsedMilliseconds);

                        return result;
                    }

                    _logger.LogWarning("⚠️ Provider {Provider} failed: {Error}. Trying next...",
                        provider.ProviderName, result.Message);
                    
                    log.RetryCount++;
                }

                // All providers failed
                stopwatch.Stop();
                
                log.Status = "Failed";
                log.ErrorMessage = "All providers failed";
                log.SendDurationMs = (int)stopwatch.ElapsedMilliseconds;
                await SaveLogAsync(log);

                _logger.LogError("❌ All email providers failed for {Email}", request.ToEmail);
                return EmailSendResult.Failed("All email providers failed. Please try again later.");
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                
                log.Status = "Failed";
                log.ErrorMessage = ex.Message;
                log.SendDurationMs = (int)stopwatch.ElapsedMilliseconds;
                await SaveLogAsync(log);

                _logger.LogError(ex, "❌ Orchestrator exception while sending email to {Email}", request.ToEmail);
                return EmailSendResult.Failed(ex.Message, exception: ex);
            }
        }

        /// <summary>
        /// Get available providers for a specific email type
        /// Providers are sorted by priority, filtered by availability
        /// </summary>
        public async Task<List<IEmailProvider>> GetAvailableProvidersAsync(EmailType emailType)
        {
            var available = new List<IEmailProvider>();

            foreach (var provider in _providers.OrderBy(p => p.Priority))
            {
                // Initialize provider if not already initialized
                try
                {
                    await provider.InitializeAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning("⚠️ Failed to initialize {Provider}: {Error}", 
                        provider.ProviderName, ex.Message);
                    continue;
                }

                if (await provider.IsAvailableAsync())
                {
                    var remaining = await provider.GetRemainingQuotaAsync();
                    _logger.LogInformation("📊 {Provider}: Available, {Remaining} quota remaining, Priority={Priority}",
                        provider.ProviderName, remaining, provider.Priority);
                    available.Add(provider);
                }
                else
                {
                    _logger.LogInformation("📊 {Provider}: Not available (quota exhausted or unhealthy)",
                        provider.ProviderName);
                }
            }

            // For OTP/high-priority emails, prioritize SendGrid
            if (emailType == EmailType.OTP)
            {
                available = available
                    .OrderBy(p => p.ProviderName == "SendGrid" ? 0 : 1)
                    .ThenBy(p => p.Priority)
                    .ToList();
            }

            return available;
        }

        /// <summary>
        /// Get quota status for all providers
        /// </summary>
        public async Task<List<ProviderStatus>> GetProviderStatusAsync()
        {
            var statuses = new List<ProviderStatus>();

            foreach (var provider in _providers)
            {
                var isAvailable = await provider.IsAvailableAsync();
                var remaining = await provider.GetRemainingQuotaAsync();

                statuses.Add(new ProviderStatus
                {
                    ProviderName = provider.ProviderName,
                    Priority = provider.Priority,
                    IsAvailable = isAvailable,
                    RemainingQuota = remaining
                });
            }

            return statuses.OrderBy(s => s.Priority).ToList();
        }

        private async Task SaveLogAsync(EmailLog log)
        {
            try
            {
                _context.Set<EmailLog>().Add(log);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save email log");
            }
        }
    }

    /// <summary>
    /// Provider status for monitoring
    /// </summary>
    public class ProviderStatus
    {
        public string ProviderName { get; set; } = string.Empty;
        public int Priority { get; set; }
        public bool IsAvailable { get; set; }
        public int RemainingQuota { get; set; }
    }
}
