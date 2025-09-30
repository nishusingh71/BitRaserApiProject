using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// CORS Test Controller for frontend connection validation
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class CorsTestController : ControllerBase
    {
        /// <summary>
        /// Test CORS configuration - No authentication required
        /// </summary>
        [HttpGet("test")]
        [AllowAnonymous]
        public ActionResult TestCors()
        {
            var response = new
            {
                message = "CORS is working! ✅",
                timestamp = DateTime.UtcNow,
                origin = Request.Headers["Origin"].FirstOrDefault() ?? "No origin header",
                userAgent = Request.Headers["User-Agent"].FirstOrDefault() ?? "No user agent",
                method = Request.Method,
                headers = Request.Headers.Keys.ToList(),
                corsEnabled = true,
                serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                serverPort = Request.Host.Port?.ToString() ?? "Unknown"
            };

            return Ok(response);
        }

        /// <summary>
        /// Test CORS with authentication required
        /// </summary>
        [HttpGet("test-auth")]
        [Authorize]
        public ActionResult TestCorsWithAuth()
        {
            var userEmail = User.Identity?.Name ?? "Unknown";
            var response = new
            {
                message = "CORS with Authentication is working! 🔒✅",
                userEmail = userEmail,
                timestamp = DateTime.UtcNow,
                authenticated = User.Identity?.IsAuthenticated ?? false,
                claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList()
            };

            return Ok(response);
        }

        /// <summary>
        /// Test POST request with CORS
        /// </summary>
        [HttpPost("test-post")]
        [AllowAnonymous]
        public ActionResult TestCorsPost([FromBody] object? data)
        {
            var response = new
            {
                message = "CORS POST request is working! 📝✅",
                timestamp = DateTime.UtcNow,
                receivedData = data,
                contentType = Request.ContentType ?? "No content type",
                contentLength = Request.ContentLength?.ToString() ?? "Unknown"
            };

            return Ok(response);
        }

        /// <summary>
        /// OPTIONS preflight request handler
        /// </summary>
        [HttpOptions]
        [AllowAnonymous]
        public ActionResult PreflightHandler()
        {
            return Ok(new
            {
                message = "Preflight request handled successfully! ✈️✅",
                timestamp = DateTime.UtcNow,
                allowedMethods = "GET, POST, PUT, DELETE, PATCH, OPTIONS",
                allowedHeaders = "Authorization, Content-Type, Accept, Origin, X-Requested-With"
            });
        }

        /// <summary>
        /// Get CORS configuration info
        /// </summary>
        [HttpGet("config")]
        [AllowAnonymous]
        public ActionResult GetCorsConfig()
        {
            var config = new
            {
                message = "CORS Configuration Info 📋",
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                serverTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                requestOrigin = Request.Headers["Origin"].FirstOrDefault() ?? "No origin",
                supportedPorts = new[]
                {
                    "3000 (React)",
                    "3001 (React Alt)",
                    "4200 (Angular)",
                    "5173 (Vite)",
                    "8080 (Vue)",
                    "8081 (Vue Alt)"
                },
                tips = new[]
                {
                    "Make sure your frontend is running on one of the supported ports",
                    "Check that your API calls include proper headers",
                    "For authenticated requests, include Authorization header",
                    "Test with /api/corstest/test endpoint first"
                }
            };

            return Ok(config);
        }
    }
}