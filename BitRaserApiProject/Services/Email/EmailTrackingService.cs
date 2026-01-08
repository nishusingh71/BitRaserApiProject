using BitRaserApiProject.Models;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Services.Email
{
    /// <summary>
    /// Interface for email tracking service
    /// Tracks sent emails, opens, and provides analytics
    /// </summary>
    public interface IEmailTrackingService
    {
        /// <summary>
        /// Log a sent email and get tracking ID
        /// </summary>
        Task<string> LogEmailSentAsync(EmailSendRequest request, EmailSendResult result, string provider);

        /// <summary>
        /// Record an email open (tracking pixel was loaded)
        /// </summary>
        Task<bool> RecordEmailOpenAsync(string trackingId, string? ipAddress, string? userAgent);

        /// <summary>
        /// Get email by tracking ID
        /// </summary>
        Task<EmailSentLog?> GetEmailByTrackingIdAsync(string trackingId);

        /// <summary>
        /// Get email statistics
        /// </summary>
        Task<EmailStatsResponse> GetStatsAsync();

        /// <summary>
        /// Get sent email logs with pagination
        /// </summary>
        Task<List<EmailSentLog>> GetLogsAsync(int page = 1, int pageSize = 50, string? provider = null, string? status = null);

        /// <summary>
        /// Get total count for pagination
        /// </summary>
        Task<int> GetLogsCountAsync(string? provider = null, string? status = null);
    }

    /// <summary>
    /// Email tracking service implementation
    /// </summary>
    public class EmailTrackingService : IEmailTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<EmailTrackingService> _logger;

        public EmailTrackingService(ApplicationDbContext context, ILogger<EmailTrackingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<string> LogEmailSentAsync(EmailSendRequest request, EmailSendResult result, string provider)
        {
            try
            {
                var log = new EmailSentLog
                {
                    TrackingId = Guid.NewGuid().ToString("N"),
                    RecipientEmail = request.ToEmail,
                    RecipientName = request.ToName,
                    Subject = request.Subject,
                    Provider = provider,
                    MessageId = result.MessageId,
                    EmailType = request.Type.ToString(),
                    Status = result.Success ? "Sent" : "Failed",
                    SentAt = DateTime.UtcNow,
                    FailureReason = result.Success ? null : result.Message,
                    OrderId = request.OrderId,
                    SendDurationMs = result.DurationMs
                };

                _context.EmailSentLogs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation("📧 Email logged: {TrackingId} to {Email} via {Provider} - {Status}",
                    log.TrackingId, log.RecipientEmail, provider, log.Status);

                return log.TrackingId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to log email to {Email}", request.ToEmail);
                return string.Empty;
            }
        }

        public async Task<bool> RecordEmailOpenAsync(string trackingId, string? ipAddress, string? userAgent)
        {
            try
            {
                var log = await _context.EmailSentLogs
                    .FirstOrDefaultAsync(e => e.TrackingId == trackingId);

                if (log == null)
                {
                    _logger.LogWarning("⚠️ Email open tracking: Unknown tracking ID {TrackingId}", trackingId);
                    return false;
                }

                // Update open tracking
                if (log.OpenedAt == null)
                {
                    log.OpenedAt = DateTime.UtcNow;
                    log.Status = "Opened";
                }

                log.OpenCount++;
                log.LastOpenedAt = DateTime.UtcNow;
                log.OpenerIp = ipAddress;
                log.OpenerUserAgent = userAgent;
                log.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("👁️ Email opened: {TrackingId} - Count: {Count}", trackingId, log.OpenCount);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to record email open for {TrackingId}", trackingId);
                return false;
            }
        }

        public async Task<EmailSentLog?> GetEmailByTrackingIdAsync(string trackingId)
        {
            return await _context.EmailSentLogs
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.TrackingId == trackingId);
        }

        public async Task<EmailStatsResponse> GetStatsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var weekAgo = today.AddDays(-7);
            var monthAgo = today.AddMonths(-1);

            var stats = new EmailStatsResponse
            {
                TotalSent = await _context.EmailSentLogs.CountAsync(),
                TotalOpened = await _context.EmailSentLogs.CountAsync(e => e.OpenedAt != null),
                TotalFailed = await _context.EmailSentLogs.CountAsync(e => e.Status == "Failed"),
                TodaySent = await _context.EmailSentLogs.CountAsync(e => e.SentAt >= today),
                TodayOpened = await _context.EmailSentLogs.CountAsync(e => e.OpenedAt >= today),
                WeekSent = await _context.EmailSentLogs.CountAsync(e => e.SentAt >= weekAgo),
                MonthSent = await _context.EmailSentLogs.CountAsync(e => e.SentAt >= monthAgo),
                ByProvider = await _context.EmailSentLogs
                    .GroupBy(e => e.Provider)
                    .Select(g => new { Provider = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Provider, x => x.Count),
                ByType = await _context.EmailSentLogs
                    .GroupBy(e => e.EmailType)
                    .Select(g => new { Type = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Type, x => x.Count),
                ByStatus = await _context.EmailSentLogs
                    .GroupBy(e => e.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.Status, x => x.Count)
            };

            stats.OpenRate = stats.TotalSent > 0 
                ? Math.Round((double)stats.TotalOpened / stats.TotalSent * 100, 2) 
                : 0;

            return stats;
        }

        public async Task<List<EmailSentLog>> GetLogsAsync(int page = 1, int pageSize = 50, string? provider = null, string? status = null)
        {
            var query = _context.EmailSentLogs.AsNoTracking();

            if (!string.IsNullOrEmpty(provider))
                query = query.Where(e => e.Provider == provider);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(e => e.Status == status);

            return await query
                .OrderByDescending(e => e.SentAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<int> GetLogsCountAsync(string? provider = null, string? status = null)
        {
            var query = _context.EmailSentLogs.AsNoTracking();

            if (!string.IsNullOrEmpty(provider))
                query = query.Where(e => e.Provider == provider);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(e => e.Status == status);

            return await query.CountAsync();
        }
    }
}
