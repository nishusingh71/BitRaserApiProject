using Microsoft.AspNetCore.Mvc;
using DSecureApi.Services;

namespace DSecureApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PolarTestController : ControllerBase
    {
        private readonly IPolarPaymentService _paymentService;
        private readonly ILogger<PolarTestController> _logger;
        private readonly IConfiguration _configuration;

        public PolarTestController(
            IPolarPaymentService paymentService,
            ILogger<PolarTestController> logger,
            IConfiguration configuration)
        {
            _paymentService = paymentService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Test Polar API configuration
        /// </summary>
        [HttpGet("config")]
        public IActionResult TestConfig()
        {
            var token = _configuration["Polar:AccessToken"];
            var webhook = _configuration["Polar:WebhookSecret"];
            var sandbox = _configuration.GetValue<bool>("Polar:Sandbox");

            return Ok(new
            {
                hasToken = !string.IsNullOrEmpty(token),
                tokenPreview = token?.Length > 20 ? $"{token.Substring(0, 20)}..." : "SHORT",
                hasWebhook = !string.IsNullOrEmpty(webhook),
                isSandbox = sandbox,
                baseUrl = sandbox ? "https://sandbox-api.polar.sh/api/v1" : "https://api.polar.sh/api/v1"
            });
        }

        /// <summary>
        /// Test fetch products from Polar (NO AUTH REQUIRED)
        /// </summary>
        [HttpGet("products")]
        public async Task<IActionResult> TestProducts()
        {
            try
            {
                var plans = await _paymentService.GetBillingPlansAsync();
                return Ok(new
                {
                    success = true,
                    count = plans.Count,
                    plans = plans
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing products");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Force refresh product cache
        /// </summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshCache()
        {
            try
            {
                await _paymentService.RefreshProductCacheAsync();
                return Ok(new { success = true, message = "Cache refreshed" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }
    }
}
