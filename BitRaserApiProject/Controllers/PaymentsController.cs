using BitRaserApiProject.Models.DTOs;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Payment Controller - Handles Polar.sh payment integration
    /// Endpoints for checkout, webhooks, and order management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPolarPaymentService _paymentService;
        private readonly ILogger<PaymentsController> _logger;

        public PaymentsController(
            IPolarPaymentService paymentService,
            ILogger<PaymentsController> logger)
        {
            _paymentService = paymentService;
            _logger = logger;
        }

        #region Checkout Endpoints

        /// <summary>
        /// Create a checkout session for product purchase
        /// Returns Polar checkout URL for redirect
        /// </summary>
        /// <param name="request">Checkout request with product and customer info</param>
        /// <returns>Checkout URL and order details</returns>
        [HttpPost("checkout")]
        [Authorize]
        [ProducesResponseType(typeof(CreateCheckoutResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateCheckout([FromBody] CreateCheckoutRequest request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Use email from request if customer info provided, otherwise use authenticated user
                var checkoutEmail = request.Customer?.Email ?? userEmail;

                var response = await _paymentService.CreateCheckoutAsync(request, checkoutEmail);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout");
                return StatusCode(500, new { error = "Failed to create checkout session" });
            }
        }

        /// <summary>
        /// Get available products for purchase
        /// </summary>
        [HttpGet("products")]
        [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _paymentService.GetProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new { error = "Failed to get products" });
            }
        }

        #endregion

        #region Webhook Endpoint

        /// <summary>
        /// Polar.sh webhook endpoint
        /// Receives payment confirmations and updates
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> HandleWebhook()
        {
            try
            {
                // Read raw body for signature verification
                using var reader = new StreamReader(Request.Body);
                var rawPayload = await reader.ReadToEndAsync();

                // Verify webhook signature
                var signature = Request.Headers["Polar-Signature"].FirstOrDefault() 
                    ?? Request.Headers["X-Polar-Signature"].FirstOrDefault()
                    ?? "";

                if (!_paymentService.VerifyWebhookSignature(rawPayload, signature))
                {
                    _logger.LogWarning("Invalid webhook signature");
                    return Unauthorized(new { error = "Invalid signature" });
                }

                // Parse webhook event
                var webhookEvent = JsonSerializer.Deserialize<PolarWebhookEvent>(rawPayload, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (webhookEvent == null)
                {
                    return BadRequest(new { error = "Invalid webhook payload" });
                }

                _logger.LogInformation("Received Polar webhook: {EventType}", webhookEvent.Type);

                // Process the webhook
                var success = await _paymentService.ProcessWebhookAsync(webhookEvent, rawPayload);

                if (success)
                {
                    return Ok(new { received = true });
                }
                else
                {
                    return StatusCode(500, new { error = "Failed to process webhook" });
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Invalid webhook JSON payload");
                return BadRequest(new { error = "Invalid JSON payload" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook");
                return StatusCode(500, new { error = "Webhook processing failed" });
            }
        }

        #endregion

        #region Order Endpoints

        /// <summary>
        /// Get order by ID
        /// </summary>
        /// <param name="orderId">Order ID</param>
        [HttpGet("orders/{orderId}")]
        [Authorize]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrder(int orderId)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var order = await _paymentService.GetOrderAsync(orderId, userEmail);

                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", orderId);
                return StatusCode(500, new { error = "Failed to get order" });
            }
        }

        /// <summary>
        /// Get all orders for authenticated user
        /// </summary>
        /// <param name="page">Page number (default 1)</param>
        /// <param name="pageSize">Page size (default 10, max 50)</param>
        [HttpGet("orders")]
        [Authorize]
        [ProducesResponseType(typeof(OrderListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                // Clamp page size
                pageSize = Math.Clamp(pageSize, 1, 50);
                page = Math.Max(1, page);

                var orders = await _paymentService.GetOrdersAsync(userEmail, page, pageSize);

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting orders");
                return StatusCode(500, new { error = "Failed to get orders" });
            }
        }

        /// <summary>
        /// Get order status by ID (for polling after checkout)
        /// </summary>
        [HttpGet("orders/{orderId}/status")]
        [Authorize]
        [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderStatus(int orderId)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var order = await _paymentService.GetOrderAsync(orderId, userEmail);

                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                var hasKeys = order.LicenseKeys != null && order.LicenseKeys.Any();
                
                return Ok(new
                {
                    orderId = order.OrderId,
                    status = order.Status,
                    isPaid = order.Status == "paid" || order.Status == "completed",
                    paidAt = order.PaidAt,
                    licenseExpiresAt = order.LicenseExpiresAt,
                    hasLicenseKeys = hasKeys
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order status {OrderId}", orderId);
                return StatusCode(500, new { error = "Failed to get order status" });
            }
        }

        #endregion
    }
}
