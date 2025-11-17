namespace BitRaserApiProject.BackgroundServices
{
    /// <summary>
    /// Background service to prevent Render.com free tier from spinning down
    /// Pings the health endpoint every 10 minutes to keep the service alive
    /// </summary>
    public class KeepAliveBackgroundService : BackgroundService
    {
 private readonly ILogger<KeepAliveBackgroundService> _logger;
        private readonly IConfiguration _configuration;
        private readonly TimeSpan _pingInterval = TimeSpan.FromMinutes(10);  // Ping every 10 minutes
        private readonly HttpClient _httpClient;

        public KeepAliveBackgroundService(
            ILogger<KeepAliveBackgroundService> logger,
          IConfiguration configuration,
            IHttpClientFactory httpClientFactory)
        {
     _logger = logger;
          _configuration = configuration;
        _httpClient = httpClientFactory.CreateClient();
 _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Only run on Render.com (not in development)
   var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") 
          ?? "Production";

            if (environment == "Development")
            {
             _logger.LogInformation("⏸️ Keep-Alive service disabled in Development mode");
    return;
            }

    _logger.LogInformation("💓 Keep-Alive Background Service started (Render.com protection)");
      
       // Wait 2 minutes before first ping to let app fully start
            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
     {
           try
                {
   await PingSelfAsync(stoppingToken);
        }
      catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error during keep-alive ping");
                }

        // Wait for next ping cycle
       try
     {
         await Task.Delay(_pingInterval, stoppingToken);
       }
         catch (OperationCanceledException)
   {
     _logger.LogInformation("🛑 Keep-Alive service stopping");
             break;
   }
 }

     _logger.LogInformation("🛑 Keep-Alive Background Service stopped");
        }

        private async Task PingSelfAsync(CancellationToken cancellationToken)
        {
            try
   {
      // Get the app's own URL (Render.com URL or configured base URL)
              var appUrl = Environment.GetEnvironmentVariable("RENDER_EXTERNAL_URL")
          ?? _configuration["AppSettings:BaseUrl"]
    ?? "http://localhost:4000";

      // Ping the health endpoint
       var healthUrl = $"{appUrl}/api/health";

   _logger.LogInformation("💓 Sending keep-alive ping to {Url}", healthUrl);

      var response = await _httpClient.GetAsync(healthUrl, cancellationToken);

        if (response.IsSuccessStatusCode)
 {
            _logger.LogInformation("✅ Keep-alive ping successful - Service staying awake");
                }
  else
      {
      _logger.LogWarning("⚠️ Keep-alive ping returned status {Status}", 
                 response.StatusCode);
                }
     }
            catch (HttpRequestException ex)
    {
           _logger.LogWarning(ex, "⚠️ Keep-alive ping failed (network issue)");
            }
         catch (TaskCanceledException)
            {
           _logger.LogWarning("⚠️ Keep-alive ping timeout");
        }
   }

        public override async Task StopAsync(CancellationToken cancellationToken)
 {
            _logger.LogInformation("🛑 Keep-Alive service is stopping...");
_httpClient.Dispose();
      await base.StopAsync(cancellationToken);
        }
    }
}
