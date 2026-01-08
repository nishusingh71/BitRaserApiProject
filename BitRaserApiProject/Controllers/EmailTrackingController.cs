using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BitRaserApiProject.Services.Email;
using BitRaserApiProject.Models;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Email Tracking Controller
    /// Provides tracking pixel endpoint and email analytics
    /// </summary>
    [ApiController]
    [Route("api/email")]
    public class EmailTrackingController : ControllerBase
    {
        private readonly IEmailTrackingService _trackingService;
        private readonly ILogger<EmailTrackingController> _logger;

        // 1x1 transparent PNG pixel (68 bytes)
        private static readonly byte[] TransparentPixel = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNkYAAAAAYAAjCB0C8AAAAASUVORK5CYII=");

        public EmailTrackingController(
            IEmailTrackingService trackingService,
            ILogger<EmailTrackingController> logger)
        {
            _trackingService = trackingService;
            _logger = logger;
        }

        /// <summary>
        /// Tracking pixel endpoint - returns 1x1 transparent PNG
        /// When email client loads this image, we record the open
        /// GET /api/email/track/{trackingId}.png
        /// </summary>
        [HttpGet("track/{trackingId}.png")]
        [AllowAnonymous]
        [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
        [Produces("image/png")]
        public async Task<IActionResult> TrackEmailOpen(string trackingId)
        {
            try
            {
                // Get client info
                var ipAddress = GetClientIpAddress();
                var userAgent = Request.Headers["User-Agent"].ToString();

                // Record the open asynchronously (don't wait)
                _ = Task.Run(async () =>
                {
                    await _trackingService.RecordEmailOpenAsync(trackingId, ipAddress, userAgent);
                });

                // Return 1x1 transparent pixel
                return File(TransparentPixel, "image/png");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error tracking email open for {TrackingId}", trackingId);
                // Still return pixel to not break email display
                return File(TransparentPixel, "image/png");
            }
        }

        /// <summary>
        /// Get email statistics
        /// GET /api/email/stats
        /// </summary>
        [HttpGet("stats")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetStats()
        {
            try
            {
                var stats = await _trackingService.GetStatsAsync();
                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting email stats");
                return StatusCode(500, new { success = false, message = "Failed to get email statistics" });
            }
        }

        /// <summary>
        /// Get sent email logs with pagination
        /// GET /api/email/logs?page=1&pageSize=50&provider=MicrosoftGraph&status=Opened
        /// </summary>
        [HttpGet("logs")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetLogs(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50,
            [FromQuery] string? provider = null,
            [FromQuery] string? status = null)
        {
            try
            {
                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 50;

                var logs = await _trackingService.GetLogsAsync(page, pageSize, provider, status);
                var totalCount = await _trackingService.GetLogsCountAsync(provider, status);

                return Ok(new
                {
                    success = true,
                    data = logs,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalCount,
                        totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting email logs");
                return StatusCode(500, new { success = false, message = "Failed to get email logs" });
            }
        }

        /// <summary>
        /// Get specific email by tracking ID
        /// GET /api/email/{trackingId}
        /// </summary>
        [HttpGet("{trackingId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetEmail(string trackingId)
        {
            try
            {
                var email = await _trackingService.GetEmailByTrackingIdAsync(trackingId);

                if (email == null)
                {
                    return NotFound(new { success = false, message = "Email not found" });
                }

                return Ok(new
                {
                    success = true,
                    data = email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting email {TrackingId}", trackingId);
                return StatusCode(500, new { success = false, message = "Failed to get email" });
            }
        }

        private string? GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }
    }
}
