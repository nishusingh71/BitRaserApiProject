using Microsoft.AspNetCore.Mvc;
using BitRaserApiProject.Services;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Health check endpoint for monitoring and keep-alive
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly ILogger<HealthController> _logger;
        private readonly ApplicationDbContext _context;
        private static DateTime _startTime = DateTime.UtcNow;
        private static int _pingCount = 0;

    public HealthController(
   ILogger<HealthController> logger,
     ApplicationDbContext context)
   {
 _logger = logger;
            _context = context;
 }

        /// <summary>
        /// Simple health check endpoint
      /// Returns 200 OK if service is running
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(object), 200)]
      public IActionResult GetHealth()
        {
  _pingCount++;
   
   var uptime = DateTime.UtcNow - _startTime;
 
     var health = new
            {
      status = "healthy",
  timestamp = DateTime.UtcNow,
                uptime = new
    {
      days = uptime.Days,
   hours = uptime.Hours,
          minutes = uptime.Minutes,
         totalMinutes = (int)uptime.TotalMinutes
       },
   pingCount = _pingCount,
      environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
      version = "2.0",
             service = "BitRaser API"
   };

  _logger.LogDebug("💓 Health check ping #{Count} - Uptime: {Minutes} minutes", 
      _pingCount, (int)uptime.TotalMinutes);

 return Ok(health);
        }

        /// <summary>
        /// Database health check
        /// Verifies database connection is working
        /// </summary>
        [HttpGet("database")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(503)]
        public async Task<IActionResult> GetDatabaseHealth()
        {
  try
    {
       // Try to query database
       var canConnect = await _context.Database.CanConnectAsync();
     
             if (canConnect)
    {
     return Ok(new
    {
     status = "healthy",
        database = "connected",
        timestamp = DateTime.UtcNow
         });
  }
     else
                {
         return StatusCode(503, new
   {
         status = "unhealthy",
      database = "disconnected",
    timestamp = DateTime.UtcNow
       });
     }
    }
            catch (Exception ex)
            {
           _logger.LogError(ex, "Database health check failed");
         
           return StatusCode(503, new
      {
 status = "unhealthy",
         database = "error",
    error = ex.Message,
       timestamp = DateTime.UtcNow
    });
            }
   }

        /// <summary>
 /// Detailed health check with all service status
        /// </summary>
        [HttpGet("detailed")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetDetailedHealth()
 {
 var uptime = DateTime.UtcNow - _startTime;
   
     // Check database
  bool dbHealthy = false;
            try
     {
     dbHealthy = await _context.Database.CanConnectAsync();
      }
        catch
       {
         dbHealthy = false;
  }

   var health = new
     {
     status = dbHealthy ? "healthy" : "degraded",
              timestamp = DateTime.UtcNow,
       uptime = new
       {
           days = uptime.Days,
         hours = uptime.Hours,
   minutes = uptime.Minutes,
               seconds = uptime.Seconds,
       totalMinutes = (int)uptime.TotalMinutes
      },
             services = new
             {
       api = "healthy",
       database = dbHealthy ? "healthy" : "unhealthy",
              keepAlive = "active"
    },
       metrics = new
                {
 totalPings = _pingCount,
startTime = _startTime,
   environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production",
  platform = Environment.GetEnvironmentVariable("RENDER") != null ? "Render.com" : "Local"
  }
            };

            return Ok(health);
 }
    }
}
