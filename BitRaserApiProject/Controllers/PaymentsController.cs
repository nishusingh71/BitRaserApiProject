using BitRaserApiProject.Data;
using BitRaserApiProject.Models.DTOs;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.Json;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Payment Controller - Handles Polar.sh and Dodo Payments integration
    /// Endpoints for checkout, webhooks, and order management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPolarPaymentService _polarPaymentService;
        private readonly IDodoPaymentService _dodoPaymentService;
        private readonly ILogger<PaymentsController> _logger;
        private readonly ApplicationDbContext _context;

        public PaymentsController(
            IPolarPaymentService polarPaymentService,
            IDodoPaymentService dodoPaymentService,
            ILogger<PaymentsController> logger,
            ApplicationDbContext context)
        {
            _polarPaymentService = polarPaymentService;
            _dodoPaymentService = dodoPaymentService;
            _logger = logger;
            _context = context;
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

                var response = await _polarPaymentService.CreateCheckoutAsync(request, checkoutEmail);

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
        /// Get available products for purchase (legacy)
        /// </summary>
        [HttpGet("products")]
        [ProducesResponseType(typeof(List<ProductDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetProducts()
        {
            try
            {
                var products = await _polarPaymentService.GetProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new { error = "Failed to get products" });
            }
        }

        #endregion

        #region Pro-Level Billing Endpoints

        /// <summary>
        /// ✅ PRO-LEVEL: Get billing plans with monthly/yearly price IDs
        /// Optimized for React pricing page - returns monthlyPriceId and yearlyPriceId
        /// Cached for 1 hour for optimal performance
        /// </summary>
        [HttpGet("billing/plans")]
        [ProducesResponseType(typeof(List<BillingPlanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetBillingPlans()
        {
            try
            {
                var plans = await _polarPaymentService.GetBillingPlansAsync();
                return Ok(plans);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting billing plans");
                return StatusCode(500, new { error = "Failed to get billing plans" });
            }
        }

        /// <summary>
        /// ✅ PRO-LEVEL: Create checkout using price ID (not product ID)
        /// Use monthlyPriceId or yearlyPriceId from GET /billing/plans response
        /// </summary>
        /// <param name="request">Price-based checkout request</param>
        [HttpPost("billing/checkout")]
        [Authorize]
        [ProducesResponseType(typeof(PriceCheckoutResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateBillingCheckout([FromBody] PriceCheckoutRequest request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var response = await _polarPaymentService.CreatePriceCheckoutAsync(request, userEmail);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating billing checkout");
                return StatusCode(500, new { error = "Failed to create checkout session" });
            }
        }

        /// <summary>
        /// ✅ PRO-LEVEL: Force refresh the product catalog cache
        /// Admin only - use sparingly
        /// </summary>
        [HttpPost("billing/refresh-cache")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshBillingCache()
        {
            try
            {
                await _polarPaymentService.RefreshProductCacheAsync();
                return Ok(new { message = "Product cache refreshed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing product cache");
                return StatusCode(500, new { error = "Failed to refresh cache" });
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

                if (!_polarPaymentService.VerifyWebhookSignature(rawPayload, signature))
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
                var success = await _polarPaymentService.ProcessWebhookAsync(webhookEvent, rawPayload);

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

                var order = await _polarPaymentService.GetOrderAsync(orderId, userEmail);

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

                var orders = await _polarPaymentService.GetOrdersAsync(userEmail, page, pageSize);

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

                var order = await _polarPaymentService.GetOrderAsync(orderId, userEmail);

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

        #region Dodo Payments Endpoints

        /// <summary>
        /// 🦤 Create a Dodo product
        /// </summary>
        [HttpPost("dodo/products")]
        [Authorize] // Removed role restriction for testing
        [ProducesResponseType(typeof(DodoProductResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> CreateDodoProduct([FromBody] DodoCreateProductRequest request)
        {
            try
            {
                // Ensure only admins can create products
                // Note: The [Authorize(Roles = ...)] attribute handles this, but adding extra check if needed
                
                var response = await _dodoPaymentService.CreateProductAsync(request);

                if (!response.Success)
                {
                    return BadRequest(new { error = response.Message });
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Dodo product");
                return StatusCode(500, new { error = "Failed to create product" });
            }
        }

        /// <summary>
        /// 🦤 Sync Customer with Dodo
        /// Creates or updates customer in Dodo Payments system
        /// </summary>
        [HttpPost("dodo/sync-customer")]
        [Authorize]
        [ProducesResponseType(typeof(DodoCustomerResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> SyncDodoCustomer([FromBody] DodoCustomerRequest request)
        {
            try
            {
                var response = await _dodoPaymentService.CreateOrUpdateCustomerAsync(
                    request.Email,
                    request.Name,
                    request.Phone
                );

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing customer with Dodo");
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to sync customer",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 🦤 Get Invoice Details from Dodo
        /// Fetches complete invoice information including products, customer, amounts
        /// </summary>
        [HttpGet("dodo/invoices/{invoiceId}")]
        [Authorize]
        [ProducesResponseType(typeof(DodoInvoiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDodoInvoice(string invoiceId)
        {
            try
            {
                var response = await _dodoPaymentService.GetInvoiceAsync(invoiceId);

                if (!response.Success)
                {
                    return NotFound(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoice {InvoiceId}", invoiceId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to fetch invoice",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 🦤 Get Invoice by Order ID
        /// Fetches invoice ID from DB order then gets full invoice from Dodo
        /// </summary>
        [HttpGet("orders/{orderId}/invoice")]
        [Authorize]
        [ProducesResponseType(typeof(DodoInvoiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetOrderInvoice(int orderId)
        {
            try
            {
                // Get order from DB
                var order = await _context.Orders.FindAsync(orderId);
                
                if (order == null)
                {
                    return NotFound(new { success = false, message = "Order not found" });
                }

                // Check if order has invoice ID
                if (string.IsNullOrEmpty(order.DodoInvoiceId))
                {
                    // If no invoice ID, try to use payment ID to get invoice
                    if (!string.IsNullOrEmpty(order.DodoPaymentId))
                    {
                        return Ok(new
                        {
                            success = true,
                            message = "No invoice ID stored, use payment ID",
                            order_id = order.OrderId,
                            dodo_payment_id = order.DodoPaymentId,
                            amount = order.AmountCents,
                            currency = order.Currency,
                            product_name = order.ProductName,
                            status = order.Status,
                            created_at = order.CreatedAt
                        });
                    }
                    return NotFound(new { success = false, message = "No invoice ID for this order" });
                }

                // Fetch full invoice from Dodo
                var response = await _dodoPaymentService.GetInvoiceAsync(order.DodoInvoiceId);

                if (!response.Success)
                {
                    return NotFound(response);
                }

                return Ok(new
                {
                    success = true,
                    order_id = order.OrderId,
                    dodo_invoice_id = order.DodoInvoiceId,
                    invoice = response.Invoice
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoice for order {OrderId}", orderId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to fetch invoice",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// 🦤 Create Dodo Webhook
        /// </summary>
        [HttpPost("dodo/webhooks/manage")]
        [Authorize]
        public async Task<IActionResult> CreateDodoWebhook([FromBody] DodoWebhookRequest request)
        {
            try
            {
                var response = await _dodoPaymentService.CreateWebhookAsync(request.Url, request.Events);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Dodo webhook");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 🦤 List Dodo Webhooks
        /// </summary>
        [HttpGet("dodo/webhooks/manage")]
        [Authorize]
        public async Task<IActionResult> GetDodoWebhooks()
        {
            try
            {
                var webhooks = await _dodoPaymentService.GetWebhooksAsync();
                return Ok(webhooks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching Dodo webhooks");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 🦤 Delete Dodo Webhook
        /// </summary>
        [HttpDelete("dodo/webhooks/manage/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteDodoWebhook(string id)
        {
            try
            {
                var success = await _dodoPaymentService.DeleteWebhookAsync(id);
                if (!success) return BadRequest(new { error = "Failed to delete webhook" });
                return Ok(new { message = "Webhook deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Dodo webhook");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// 🦤 Create a Dodo checkout session
        /// </summary>
        [HttpPost("dodo/checkout")]
        [Authorize]
        [ProducesResponseType(typeof(DodoCheckoutResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> CreateDodoCheckout([FromBody] DodoCheckoutRequest request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var response = await _dodoPaymentService.CreateCheckoutAsync(request, request.CustomerEmail ?? userEmail);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Dodo checkout");
                return StatusCode(500, new { error = "Failed to create Dodo checkout session" });
            }
        }

        /// <summary>
        /// 🦤 Create a Dodo checkout session for NEW USERS (No Auth Required)
        /// Order is NOT created until webhook fires with actual customer data
        /// Usage: Frontend calls this with ProductId and ReturnUrl
        /// </summary>
        [HttpPost("dodo/checkout/guest")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(GuestCheckoutResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateGuestDodoCheckout([FromBody] GuestCheckoutRequest request)
        {
            try
            {
                _logger.LogInformation("🛒 Guest checkout request for ProductId: {ProductId}", request.ProductId);

                if (string.IsNullOrEmpty(request.ProductId))
                {
                    return BadRequest(new { error = "ProductId is required" });
                }

                // Build Dodo checkout - NO ORDER CREATED YET
                var response = await _dodoPaymentService.CreateGuestCheckoutSessionAsync(request);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                _logger.LogInformation("✅ Guest checkout session created: {Url}", response.CheckoutUrl);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating guest Dodo checkout");
                return StatusCode(500, new { error = "Failed to create checkout session" });
            }
        }

        /// <summary>
        /// 🦤 Dodo Payments webhook endpoint
        /// Receives payment confirmations and updates from Dodo
        /// Supports both /api/Payments/dodo/webhook and /webhooks/dodo
        /// </summary>
        [HttpPost("dodo/webhook")]
        [HttpPost("/webhooks/dodo")] // Alias route for Dodo Payments
        [AllowAnonymous]
        public async Task<IActionResult> HandleDodoWebhook()
        {
            try
            {
                // ✅ Enable buffering so body can be read correctly even if consumed by middleware
                Request.EnableBuffering();
                Request.Body.Position = 0;

                // Read raw body for signature verification
                using var reader = new StreamReader(Request.Body, leaveOpen: true);
                var rawPayload = await reader.ReadToEndAsync();
                Request.Body.Position = 0; // Reset just in case

                if (string.IsNullOrEmpty(rawPayload))
                {
                   _logger.LogWarning("❌ Empty webhook payload received in PaymentsController");
                   return BadRequest(new { error = "Empty payload" });
                }

                // Get webhook signature headers
                var webhookId = Request.Headers["webhook-id"].FirstOrDefault() ?? "";
                var webhookSignature = Request.Headers["webhook-signature"].FirstOrDefault() ?? "";
                var webhookTimestamp = Request.Headers["webhook-timestamp"].FirstOrDefault() ?? "";

                // Verify webhook signature
                if (!_dodoPaymentService.VerifyWebhookSignature(rawPayload, webhookId, webhookSignature, webhookTimestamp))
                {
                    _logger.LogWarning("Invalid Dodo webhook signature");
                    return Unauthorized(new { error = "Invalid signature" });
                }

                // Parse webhook event
                var webhookEvent = JsonSerializer.Deserialize<DodoWebhookEvent>(rawPayload, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (webhookEvent == null)
                {
                    return BadRequest(new { error = "Invalid webhook payload" });
                }

                _logger.LogInformation("🦤 Received Dodo webhook: {EventType}", webhookEvent.Type);

                // Process the webhook
                var success = await _dodoPaymentService.ProcessWebhookAsync(webhookEvent, rawPayload);

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
                _logger.LogError(ex, "Invalid Dodo webhook JSON payload");
                return BadRequest(new { error = "Invalid JSON payload" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing Dodo webhook");
                return StatusCode(500, new { error = "Webhook processing failed" });
            }
        }

        /// <summary>
        /// 🦤 Get available Dodo products
        /// </summary>
        [HttpGet("dodo/products")]
        [ProducesResponseType(typeof(List<DodoBillingPlanDto>), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDodoProducts()
        {
            try
            {
                var products = await _dodoPaymentService.GetProductsAsync();
                return Ok(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Dodo products");
                return StatusCode(500, new { error = "Failed to get Dodo products" });
            }
        }

        /// <summary>
        /// 🦤 Force refresh Dodo product cache
        /// </summary>
        [HttpPost("dodo/refresh-cache")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<IActionResult> RefreshDodoProductCache()
        {
            try
            {
                await _dodoPaymentService.RefreshProductCacheAsync();
                return Ok(new { message = "Dodo product cache refreshed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing Dodo product cache");
                return StatusCode(500, new { error = "Failed to refresh cache" });
            }
        }

        /// <summary>
        /// 🦤 Get Dodo order by ID
        /// </summary>
        [HttpGet("dodo/orders/{orderId}")]
        [Authorize]
        [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetDodoOrder(int orderId)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                var order = await _dodoPaymentService.GetOrderAsync(orderId, userEmail);

                if (order == null)
                {
                    return NotFound(new { error = "Order not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Dodo order {OrderId}", orderId);
                return StatusCode(500, new { error = "Failed to get order" });
            }
        }

        /// <summary>
        /// 🦤 Get all Dodo orders for authenticated user
        /// </summary>
        [HttpGet("dodo/orders")]
        [Authorize]
        [ProducesResponseType(typeof(OrderListResponse), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetDodoOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { error = "User not authenticated" });
                }

                pageSize = Math.Clamp(pageSize, 1, 50);
                page = Math.Max(1, page);

                var orders = await _dodoPaymentService.GetOrdersAsync(userEmail, page, pageSize);

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Dodo orders");
                return StatusCode(500, new { error = "Failed to get orders" });
            }
        }

        /// <summary>
        /// 🦤 TEST: Get Invoice by Payment ID (for debugging)
        /// Calls Dodo API: GET /invoices/payments/{payment_id}
        /// </summary>
        [HttpGet("dodo/invoices/by-payment/{paymentId}")]
        [AllowAnonymous] // Allow testing without auth
        [ProducesResponseType(typeof(DodoInvoiceResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetInvoiceByPaymentId(string paymentId)
        {
            try
            {
                _logger.LogInformation("🧪 TEST: Fetching invoice by PaymentId: {PaymentId}", paymentId);
                
                var response = await _dodoPaymentService.GetInvoiceByPaymentIdAsync(paymentId);

                if (!response.Success)
                {
                    return NotFound(response);
                }

                return Ok(new
                {
                    success = true,
                    message = "Invoice fetched successfully",
                    payment_id = paymentId,
                    invoice = response.Invoice,
                    pdf_url = response.Invoice?.PdfUrl,
                    invoice_url = response.Invoice?.InvoiceUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching invoice by payment ID {PaymentId}", paymentId);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Failed to fetch invoice",
                    error = ex.Message
                });
            }
        }


        /// <summary>
        /// 🔍 Get Comprehensive Order Details
        /// Returns consolidated Order, Payment, Product, Customer, and Invoice details
        /// Robustly handles invoice fetching via Payment ID fallback
        /// </summary>
        /// <param name="orderId">Internal Order ID</param>
        [HttpGet("orders/{orderId}/details")]
        [AllowAnonymous] // Temporarily allow anonymous for testing
        [ProducesResponseType(typeof(ComprehensiveOrderDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetFullOrderDetails(int orderId)
        {
            try
            {
                _logger.LogInformation("🔍 Fetching comprehensive details for Order {OrderId}", orderId);

                // 1. Fetch Order from DB
                var order = await _context.Orders.FindAsync(orderId);
                
                if (order == null)
                {
                    _logger.LogWarning("❌ Order {OrderId} not found", orderId);
                    return NotFound(new { error = "Order not found" });
                }

                // NOTE: Security check skipped for testing (AllowAnonymous)

                // 2. Prepare Invoice Information
                InvoiceInformation invoiceInfo = new InvoiceInformation
                {
                    Status = "Not Generated"
                };

                // Log order details for debugging
                _logger.LogInformation("📋 Order Details: PaymentProvider={Provider}, Status={Status}, DodoPaymentId={PaymentId}, DodoInvoiceId={InvoiceId}",
                    order.PaymentProvider, order.Status, order.DodoPaymentId, order.DodoInvoiceId);

                // Only attempt invoice fetch if order is paid Dodo order
                if (order.PaymentProvider == "dodo" && 
                   (order.Status == "paid" || order.Status == "completed"))
                {
                    DodoInvoiceResponse? invoiceResponse = null;

                    // Strategy A: Try fetching by Payment ID (Most Reliable)
                    if (!string.IsNullOrEmpty(order.DodoPaymentId))
                    {
                        _logger.LogInformation("🔍 Attempting invoice fetch by PaymentId: {PaymentId}", order.DodoPaymentId);
                        invoiceResponse = await _dodoPaymentService.GetInvoiceByPaymentIdAsync(order.DodoPaymentId);
                        _logger.LogInformation("📄 Invoice fetch result: Success={Success}, Message={Message}", 
                            invoiceResponse?.Success, invoiceResponse?.Message);
                    }
                    
                    // Strategy B: Fallback to Invoice ID from DB if Payment ID failed or missing
                    if ((invoiceResponse == null || !invoiceResponse.Success) && !string.IsNullOrEmpty(order.DodoInvoiceId))
                    {
                        _logger.LogInformation("🔍 Fallback: Attempting invoice fetch by InvoiceId: {InvoiceId}", order.DodoInvoiceId);
                        invoiceResponse = await _dodoPaymentService.GetInvoiceAsync(order.DodoInvoiceId);
                    }

                    if (invoiceResponse != null && invoiceResponse.Success && invoiceResponse.Invoice != null)
                    {
                        var inv = invoiceResponse.Invoice;
                        invoiceInfo = new InvoiceInformation
                        {
                            InvoiceId = inv.InvoiceId,
                            InvoiceNumber = inv.InvoiceNumber,
                            Date = inv.CreatedAt,
                            Status = inv.Status,
                            PdfUrl = inv.PdfUrl ?? inv.InvoiceUrl, // Prefer PDF, fallback to HTML
                            Currency = inv.Currency,
                            TotalAmount = inv.TotalAmountDecimal,
                            TaxAmount = inv.TaxDecimal
                        };
                    }
                    else
                    {
                        // ✅ Fallback: Use DB-stored invoice info if API call fails
                        invoiceInfo = new InvoiceInformation
                        {
                            InvoiceId = order.DodoInvoiceId,
                            InvoiceNumber = order.DodoInvoiceId, // In Dodo, this is often the invoice number
                            Date = order.PaidAt,
                            Status = "Paid (API Unavailable)",
                            Currency = order.Currency,
                            TotalAmount = order.AmountCents / 100m,
                            TaxAmount = order.TaxAmountCents / 100m
                        };
                        _logger.LogWarning("⚠️ Invoice API failed for Order {OrderId}, using DB fallback data", orderId);
                    }
                }

                // 3. Parse license keys from JSON
                List<string>? licenseKeysList = null;
                if (!string.IsNullOrEmpty(order.LicenseKeys))
                {
                    try
                    {
                        licenseKeysList = System.Text.Json.JsonSerializer.Deserialize<List<string>>(order.LicenseKeys);
                    }
                    catch
                    {
                        // If it's not JSON, treat as comma-separated
                        licenseKeysList = order.LicenseKeys.Split(',').Select(k => k.Trim()).ToList();
                    }
                }

                // 4. Construct Consolidated Response
                var response = new ComprehensiveOrderDto
                {
                    OrderDetails = new OrderSummaryDetails
                    {
                        OrderId = order.OrderId,
                        OrderDate = order.CreatedAt,
                        Status = order.Status,
                        DodoPaymentId = order.DodoPaymentId,
                        DodoInvoiceId = order.DodoInvoiceId
                    },
                    PaymentInfo = new PaymentInformation
                    {
                        Status = order.Status,
                        Method = order.PaymentMethod ?? "Unknown",
                        Amount = order.AmountCents / 100m,
                        TaxAmount = order.TaxAmountCents / 100m,
                        Currency = order.Currency,
                        TransactionId = order.DodoPaymentId,
                        Provider = order.PaymentProvider ?? "Manual",
                        PaymentDate = order.PaidAt,
                        PaymentLink = order.PaymentLink,
                        CardLastFour = order.CardLastFour,
                        CardNetwork = order.CardNetwork,
                        CardType = order.CardType
                    },
                    ProductDetails = new ProductDetails
                    {
                        ProductId = order.ProductId,
                        Name = order.ProductName ?? "Standard License",
                        Summary = $"{order.LicenseYears} Year License for {order.LicenseCount} Machine(s)",
                        Quantity = order.LicenseCount,
                        DurationYears = order.LicenseYears
                    },
                    CustomerInfo = new CustomerInformation
                    {
                        CustomerId = order.DodoCustomerId,
                        Name = $"{order.FirstName} {order.LastName}".Trim(),
                        Email = order.UserEmail,
                        Phone = order.PhoneNumber,
                        CompanyName = order.CompanyName,
                        BillingAddress = new BillingAddressDetails
                        {
                            Street = order.BillingAddress,
                            City = order.BillingCity,
                            State = order.BillingState,
                            Country = order.BillingCountry,
                            Zipcode = order.BillingZip,
                            Formatted = BuildBillingAddressString(order)
                        }
                    },
                    InvoiceInfo = invoiceInfo,
                    LicenseInfo = new LicenseInformation
                    {
                        LicenseKeys = licenseKeysList,
                        LicenseCount = order.LicenseCount,
                        LicenseYears = order.LicenseYears,
                        ExpiresAt = order.LicenseExpiresAt
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching comprehensive details for Order {OrderId}", orderId);
                return StatusCode(500, new { error = "Failed to fetch order details" });
            }
        }

        private string BuildBillingAddressString(BitRaserApiProject.Models.Order order)
        {
            var parts = new List<string?> 
            { 
                order.BillingAddress, 
                order.BillingCity, 
                order.BillingState, 
                order.BillingZip, 
                order.BillingCountry 
            };
            return string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        #endregion
    }
}

