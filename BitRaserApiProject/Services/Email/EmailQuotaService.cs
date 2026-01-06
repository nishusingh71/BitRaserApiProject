using BitRaserApiProject.Data;
using BitRaserApiProject.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BitRaserApiProject.Services.Email
{
    /// <summary>
    /// Email quota management service
    /// Tracks daily/monthly limits per provider, handles resets, circuit breaker
    /// </summary>
    public class EmailQuotaService : IEmailQuotaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailQuotaService> _logger;
        private readonly SemaphoreSlim _lock = new(1, 1);

        // Default limits
        private const int DEFAULT_SENDGRID_DAILY = 100;
        private const int DEFAULT_SENDGRID_MONTHLY = 3000;
        private const int DEFAULT_GRAPH_DAILY = 10000;
        private const int DEFAULT_GRAPH_MONTHLY = 300000;
        private const int CIRCUIT_BREAKER_THRESHOLD = 5;

        public EmailQuotaService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<EmailQuotaService> logger)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Initialize quotas in database if not present
        /// </summary>
        public async Task InitializeQuotasAsync()
        {
            await _lock.WaitAsync();
            try
            {
                // Initialize SendGrid quota
                var sendGridQuota = await _context.Set<EmailQuota>()
                    .FirstOrDefaultAsync(q => q.ProviderName == "SendGrid");

                if (sendGridQuota == null)
                {
                    var dailyLimit = int.TryParse(
                        Environment.GetEnvironmentVariable("SendGrid__DailyLimit") ?? 
                        _configuration["SendGrid:DailyLimit"], 
                        out var dl) ? dl : DEFAULT_SENDGRID_DAILY;

                    var monthlyLimit = int.TryParse(
                        Environment.GetEnvironmentVariable("SendGrid__MonthlyLimit") ?? 
                        _configuration["SendGrid:MonthlyLimit"], 
                        out var ml) ? ml : DEFAULT_SENDGRID_MONTHLY;

                    sendGridQuota = new EmailQuota
                    {
                        ProviderName = "SendGrid",
                        AccountIdentifier = "default",
                        DailyLimit = dailyLimit,
                        MonthlyLimit = monthlyLimit,
                        Priority = 2,
                        IsEnabled = true
                    };
                    _context.Set<EmailQuota>().Add(sendGridQuota);
                    _logger.LogInformation("📧 Created SendGrid quota: Daily={Daily}, Monthly={Monthly}", 
                        dailyLimit, monthlyLimit);
                }

                // Initialize Microsoft Graph quota
                var graphQuota = await _context.Set<EmailQuota>()
                    .FirstOrDefaultAsync(q => q.ProviderName == "MicrosoftGraph");

                if (graphQuota == null)
                {
                    var dailyLimit = int.TryParse(
                        Environment.GetEnvironmentVariable("MsGraph__DailyLimit") ?? 
                        _configuration["MsGraph:DailyLimit"], 
                        out var dl) ? dl : DEFAULT_GRAPH_DAILY;

                    var monthlyLimit = int.TryParse(
                        Environment.GetEnvironmentVariable("MsGraph__MonthlyLimit") ?? 
                        _configuration["MsGraph:MonthlyLimit"], 
                        out var ml) ? ml : DEFAULT_GRAPH_MONTHLY;

                    var senderEmail = Environment.GetEnvironmentVariable("MsGraph__SenderEmail") 
                        ?? _configuration["MsGraph:SenderEmail"];

                    graphQuota = new EmailQuota
                    {
                        ProviderName = "MicrosoftGraph",
                        AccountIdentifier = senderEmail ?? "default",
                        DailyLimit = dailyLimit,
                        MonthlyLimit = monthlyLimit,
                        Priority = 1,
                        IsEnabled = !string.IsNullOrEmpty(senderEmail)
                    };
                    _context.Set<EmailQuota>().Add(graphQuota);
                    _logger.LogInformation("📧 Created MicrosoftGraph quota: Daily={Daily}, Monthly={Monthly}", 
                        dailyLimit, monthlyLimit);
                }

                await _context.SaveChangesAsync();
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task<bool> HasAvailableQuotaAsync(string providerName)
        {
            var quota = await GetQuotaAsync(providerName);
            if (quota == null) return false;

            // Check if quotas need reset
            await CheckAndResetQuotasAsync(quota);

            return quota.CanSend;
        }

        public async Task<int> GetRemainingQuotaAsync(string providerName)
        {
            var quota = await GetQuotaAsync(providerName);
            if (quota == null) return 0;

            // Check if quotas need reset
            await CheckAndResetQuotasAsync(quota);

            return Math.Min(quota.RemainingDailyQuota, quota.RemainingMonthlyQuota);
        }

        public async Task IncrementUsageAsync(string providerName)
        {
            await _lock.WaitAsync();
            try
            {
                var quota = await GetQuotaAsync(providerName);
                if (quota == null) return;

                quota.DailySent++;
                quota.MonthlySent++;
                quota.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogDebug("📊 {Provider} usage: {Daily}/{DailyLimit} daily, {Monthly}/{MonthlyLimit} monthly",
                    providerName, quota.DailySent, quota.DailyLimit, quota.MonthlySent, quota.MonthlyLimit);
            }
            finally
            {
                _lock.Release();
            }
        }

        public async Task RecordSuccessAsync(string providerName)
        {
            var quota = await GetQuotaAsync(providerName);
            if (quota == null) return;

            quota.LastSuccessAt = DateTime.UtcNow;
            quota.ConsecutiveFailures = 0;
            quota.IsHealthy = true;
            quota.LastErrorMessage = null;
            quota.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
        }

        public async Task RecordFailureAsync(string providerName, string errorMessage)
        {
            var quota = await GetQuotaAsync(providerName);
            if (quota == null) return;

            quota.LastFailureAt = DateTime.UtcNow;
            quota.ConsecutiveFailures++;
            quota.LastErrorMessage = errorMessage;
            quota.UpdatedAt = DateTime.UtcNow;

            // Circuit breaker: disable provider after threshold failures
            if (quota.ConsecutiveFailures >= CIRCUIT_BREAKER_THRESHOLD)
            {
                quota.IsHealthy = false;
                _logger.LogWarning("🔴 Circuit breaker triggered for {Provider} after {Failures} failures",
                    providerName, quota.ConsecutiveFailures);
            }

            await _context.SaveChangesAsync();
        }

        public async Task ResetDailyQuotasAsync()
        {
            _logger.LogInformation("🔄 Resetting daily email quotas...");
            
            var quotas = await _context.Set<EmailQuota>().ToListAsync();
            var today = DateTime.UtcNow.Date;

            foreach (var quota in quotas)
            {
                if (quota.LastDailyReset.Date < today)
                {
                    quota.DailySent = 0;
                    quota.LastDailyReset = today;
                    quota.UpdatedAt = DateTime.UtcNow;
                    
                    // Reset circuit breaker on daily reset
                    if (!quota.IsHealthy)
                    {
                        quota.IsHealthy = true;
                        quota.ConsecutiveFailures = 0;
                        _logger.LogInformation("✅ Circuit breaker reset for {Provider}", quota.ProviderName);
                    }
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Daily quotas reset for {Count} providers", quotas.Count);
        }

        public async Task ResetMonthlyQuotasAsync()
        {
            _logger.LogInformation("🔄 Resetting monthly email quotas...");

            var quotas = await _context.Set<EmailQuota>().ToListAsync();
            var firstOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);

            foreach (var quota in quotas)
            {
                if (quota.LastMonthlyReset < firstOfMonth)
                {
                    quota.MonthlySent = 0;
                    quota.LastMonthlyReset = firstOfMonth;
                    quota.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("✅ Monthly quotas reset for {Count} providers", quotas.Count);
        }

        public async Task<List<EmailQuota>> GetAllQuotasAsync()
        {
            return await _context.Set<EmailQuota>()
                .OrderBy(q => q.Priority)
                .ToListAsync();
        }

        private async Task<EmailQuota?> GetQuotaAsync(string providerName)
        {
            return await _context.Set<EmailQuota>()
                .FirstOrDefaultAsync(q => q.ProviderName == providerName);
        }

        private async Task CheckAndResetQuotasAsync(EmailQuota quota)
        {
            var today = DateTime.UtcNow.Date;
            var firstOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
            var needsSave = false;

            if (quota.LastDailyReset.Date < today)
            {
                quota.DailySent = 0;
                quota.LastDailyReset = today;
                quota.UpdatedAt = DateTime.UtcNow;
                needsSave = true;
                _logger.LogInformation("🔄 Auto-reset daily quota for {Provider}", quota.ProviderName);
            }

            if (quota.LastMonthlyReset < firstOfMonth)
            {
                quota.MonthlySent = 0;
                quota.LastMonthlyReset = firstOfMonth;
                quota.UpdatedAt = DateTime.UtcNow;
                needsSave = true;
                _logger.LogInformation("🔄 Auto-reset monthly quota for {Provider}", quota.ProviderName);
            }

            if (needsSave)
            {
                await _context.SaveChangesAsync();
            }
        }
    }
}
