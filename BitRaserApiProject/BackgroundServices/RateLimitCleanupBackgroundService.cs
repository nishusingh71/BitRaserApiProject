using DSecureApi.Middleware;

namespace DSecureApi.BackgroundServices
{
    /// <summary>
    /// Background service to periodically cleanup expired rate limit entries
    /// Runs every 5 minutes to prevent memory bloat
    /// </summary>
    public class RateLimitCleanupBackgroundService : BackgroundService
    {
        private readonly ILogger<RateLimitCleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5);

        public RateLimitCleanupBackgroundService(ILogger<RateLimitCleanupBackgroundService> logger)
        {
        _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("✅ Rate Limit Cleanup Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
              try
       {
     await Task.Delay(_cleanupInterval, stoppingToken);
    
         RateLimitingMiddleware.CleanupExpiredEntries();
     _logger.LogDebug("🧹 Rate limit entries cleanup completed");
                }
         catch (OperationCanceledException)
        {
        // Service is stopping
         break;
   }
     catch (Exception ex)
  {
       _logger.LogError(ex, "❌ Error during rate limit cleanup");
                }
      }

  _logger.LogInformation("⏹️ Rate Limit Cleanup Service stopped");
        }
    }
}
