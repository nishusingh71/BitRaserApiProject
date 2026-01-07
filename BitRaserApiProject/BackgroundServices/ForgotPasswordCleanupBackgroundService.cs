using DSecureApi.Services;

namespace DSecureApi.BackgroundServices
{
    /// <summary>
    /// Background service to automatically cleanup expired password reset requests
    /// Runs every 24 hours to delete expired and used requests (optimized for TiDB RU)
    /// </summary>
    public class ForgotPasswordCleanupBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ForgotPasswordCleanupBackgroundService> _logger;
        private readonly TimeSpan _cleanupInterval = TimeSpan.FromHours(24);  // ✅ Optimized: 1 day

        public ForgotPasswordCleanupBackgroundService(
 IServiceProvider serviceProvider,
        ILogger<ForgotPasswordCleanupBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
      }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
      {
            _logger.LogInformation("🧹 Forgot Password Cleanup Background Service started");

 // Wait 1 minute before first cleanup
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

  while (!stoppingToken.IsCancellationRequested)
   {
         try
       {
            await CleanupExpiredRequestsAsync(stoppingToken);
  }
 catch (Exception ex)
     {
      _logger.LogError(ex, "❌ Error during forgot password cleanup");
   }

       // Wait for next cleanup cycle
     try
         {
  await Task.Delay(_cleanupInterval, stoppingToken);
     }
      catch (OperationCanceledException)
       {
         // Service is stopping
         _logger.LogInformation("🛑 Forgot Password Cleanup Background Service stopping");
    break;
                }
    }

            _logger.LogInformation("🛑 Forgot Password Cleanup Background Service stopped");
        }

    private async Task CleanupExpiredRequestsAsync(CancellationToken cancellationToken)
        {
       using var scope = _serviceProvider.CreateScope();
          var forgotPasswordService = scope.ServiceProvider.GetRequiredService<IForgotPasswordService>();

            _logger.LogInformation("🧹 Starting automatic cleanup of expired password reset requests...");

            await forgotPasswordService.CleanupExpiredRequestsAsync();

            _logger.LogInformation("✅ Automatic cleanup completed");
 }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
        _logger.LogInformation("🛑 Forgot Password Cleanup Background Service is stopping...");
            await base.StopAsync(cancellationToken);
        }
    }
}
