using BitRaserApiProject.Models.DTOs;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Dedicated Dodo Payments Webhook Controller
    /// Handles webhook events from Dodo Payments with proper security
    /// </summary>
    [ApiController]
    [Route("api/webhooks")]
    public class DodoWebhooksController : ControllerBase
    {
        private readonly IDodoPaymentService _dodoPaymentService;
        private readonly ILogger<DodoWebhooksController> _logger;

        public DodoWebhooksController(
            IDodoPaymentService dodoPaymentService,
            ILogger<DodoWebhooksController> logger)
        {
            _dodoPaymentService = dodoPaymentService;
            _logger = logger;
        }

        /// <summary>
        /// 🦤 Primary Dodo Payments Webhook Endpoint
        /// POST /api/webhooks/dodo
        /// 
        /// Security:
        /// - Reads raw body for signature verification
        /// - Verifies HMAC SHA256 signature using Dodo headers
        /// - Returns 401 on invalid signature
        /// - Implements idempotency to prevent duplicate processing
        /// </summary>
        [HttpPost("dodo")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleDodoWebhook()
        {
            string rawPayload = "";
            try
            {
                _logger.LogInformation("🦤 Dodo webhook received at /api/webhooks/dodo");

                // ✅ Enable buffering so body can be read multiple times
                Request.EnableBuffering();

                // Step 1: Read raw body (CRITICAL for signature verification)
                Request.Body.Position = 0;
                using var reader = new StreamReader(Request.Body, leaveOpen: true);
                rawPayload = await reader.ReadToEndAsync();
                Request.Body.Position = 0; // Reset for potential re-read

                _logger.LogInformation("📥 Webhook body length: {Length} chars", rawPayload?.Length ?? 0);

                if (string.IsNullOrEmpty(rawPayload))
                {
                    _logger.LogWarning("❌ Empty webhook payload received (Length: 0)");
                    return BadRequest(new { error = "Empty payload" });
                }

                // Step 2: Extract Dodo signature headers
                var webhookId = Request.Headers["webhook-id"].FirstOrDefault() ?? "";
                var webhookSignature = Request.Headers["webhook-signature"].FirstOrDefault() ?? "";
                var webhookTimestamp = Request.Headers["webhook-timestamp"].FirstOrDefault() ?? "";

                _logger.LogInformation("📥 Webhook headers - ID: {Id}, Timestamp: {Timestamp}, Sig: {Sig}",
                    webhookId,
                    webhookTimestamp,
                    string.IsNullOrEmpty(webhookSignature) ? "[MISSING]" : $"{webhookSignature.Substring(0, Math.Min(20, webhookSignature.Length))}...");

                // Step 3: Verify webhook signature (SECURITY CRITICAL)
                if (!_dodoPaymentService.VerifyWebhookSignature(rawPayload, webhookId, webhookSignature, webhookTimestamp))
                {
                    _logger.LogWarning("🔐 Invalid Dodo webhook signature - REJECTED");
                    return Unauthorized(new { error = "Invalid signature" });
                }

                _logger.LogInformation("✅ Webhook signature verified successfully");

                // Step 4: Parse webhook event
                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                DodoWebhookEvent? webhookEvent = null;
                try 
                {
                    webhookEvent = JsonSerializer.Deserialize<DodoWebhookEvent>(rawPayload, jsonOptions);
                }
                catch (Exception deserializationEx)
                {
                    _logger.LogError(deserializationEx, "❌ JSON Deserialization Exception. Payload: {Payload}", 
                        rawPayload.Length > 1000 ? rawPayload.Substring(0, 1000) + "..." : rawPayload);
                    throw; // Re-throw to catch block below
                }

                if (webhookEvent == null)
                {
                    _logger.LogWarning("❌ Failed to parse webhook event (Result is null). Payload: {Payload}", 
                        rawPayload.Length > 1000 ? rawPayload.Substring(0, 1000) + "..." : rawPayload);
                    return BadRequest(new { error = "Invalid webhook payload" });
                }
                
                // Validate essential fields
                if (string.IsNullOrEmpty(webhookEvent.Type))
                {
                     _logger.LogWarning("❌ Webhook event missing 'type'. Payload: {Payload}", 
                        rawPayload.Length > 1000 ? rawPayload.Substring(0, 1000) + "..." : rawPayload);
                     return BadRequest(new { error = "Invalid webhook payload: Missing type" });
                }

                _logger.LogInformation("🦤 Dodo Webhook Event: {EventType} | BusinessId: {BusinessId}",
                    webhookEvent.Type,
                    webhookEvent.BusinessId);

                // Step 5: Process the webhook (includes idempotency check)
                var success = await _dodoPaymentService.ProcessWebhookAsync(webhookEvent, rawPayload);

                if (success)
                {
                    _logger.LogInformation("✅ Webhook processed successfully: {EventType}", webhookEvent.Type);
                    return Ok(new { received = true, eventType = webhookEvent.Type });
                }
                else
                {
                    _logger.LogError("❌ Webhook processing failed: {EventType}", webhookEvent.Type);
                    return StatusCode(500, new { error = "Failed to process webhook" });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "❌ Invalid JSON in webhook payload");
                // Log payload specifically here too
                _logger.LogError("Invalid Payload Content: {Payload}", 
                     rawPayload?.Length > 1000 ? rawPayload.Substring(0, 1000) + "..." : rawPayload);
                return BadRequest(new { error = "Invalid JSON payload" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Unhandled error in Dodo webhook");
                // Return 500 to signal Dodo to retry
                return StatusCode(500, new { error = "Internal server error" });
            }
        }

        /// <summary>
        /// Health check endpoint for webhook testing
        /// GET /api/webhooks/dodo/health
        /// </summary>
        [HttpGet("dodo/health")]
        [AllowAnonymous]
        public IActionResult WebhookHealth()
        {
            return Ok(new
            {
                status = "healthy",
                endpoint = "/api/webhooks/dodo",
                timestamp = DateTime.UtcNow,
                message = "Dodo webhook endpoint is ready"
            });
        }

        /// <summary>
        /// GET handler for Dodo webhook verification
        /// Dodo sends GET request to verify endpoint exists before sending webhooks
        /// GET /api/webhooks/dodo
        /// </summary>
        [HttpGet("dodo")]
        [AllowAnonymous]
        public IActionResult VerifyWebhook()
        {
            _logger.LogInformation("🦤 Dodo webhook verification GET request received");
            return Ok(new
            {
                status = "ready",
                endpoint = "POST /api/webhooks/dodo",
                timestamp = DateTime.UtcNow,
                message = "Dodo webhook endpoint is ready. All events (payment.succeeded, payment.failed, etc.) are processed through this single endpoint."
            });
        }
    }
}
