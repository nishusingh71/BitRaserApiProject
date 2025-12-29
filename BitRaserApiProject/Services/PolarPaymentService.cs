using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Polar.sh Payment Service Implementation
    /// Handles checkout sessions, webhooks, and order management
    /// </summary>
    public class PolarPaymentService : IPolarPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PolarPaymentService> _logger;
        private readonly HttpClient _httpClient;

        // Polar API configuration
        private readonly string _polarAccessToken;
        private readonly string _polarWebhookSecret;
        private readonly string _polarBaseUrl;
        private readonly bool _isSandbox;

        public PolarPaymentService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<PolarPaymentService> logger,
            IHttpClientFactory httpClientFactory)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("PolarApi");

            // Load configuration
            _polarAccessToken = configuration["Polar:AccessToken"] 
                ?? Environment.GetEnvironmentVariable("POLAR_ACCESS_TOKEN") 
                ?? throw new InvalidOperationException("Polar access token not configured");
            
            _polarWebhookSecret = configuration["Polar:WebhookSecret"] 
                ?? Environment.GetEnvironmentVariable("POLAR_WEBHOOK_SECRET") 
                ?? "";

            _isSandbox = configuration.GetValue<bool>("Polar:Sandbox", true);
            _polarBaseUrl = _isSandbox 
                ? "https://sandbox-api.polar.sh/v1" 
                : "https://api.polar.sh/v1";

            // Configure HttpClient
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_polarAccessToken}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        }

        /// <summary>
        /// Create a checkout session with Polar.sh
        /// </summary>
        public async Task<CreateCheckoutResponse> CreateCheckoutAsync(CreateCheckoutRequest request, string userEmail)
        {
            try
            {
                _logger.LogInformation("Creating checkout for user {Email}, product {ProductId}", 
                    userEmail, request.ProductId);

                // Create order in database first (pending status)
                var order = new Order
                {
                    UserEmail = userEmail,
                    ProductId = request.ProductId,
                    LicenseCount = request.LicenseCount,
                    LicenseYears = request.LicenseYears,
                    Status = "pending",
                    FirstName = request.Customer?.FirstName,
                    LastName = request.Customer?.LastName,
                    PhoneNumber = request.Customer?.PhoneNumber,
                    CompanyName = request.Customer?.CompanyName,
                    BillingCountry = request.BillingAddress?.Country,
                    BillingAddress = request.BillingAddress?.StreetAddress,
                    BillingCity = request.BillingAddress?.City,
                    BillingState = request.BillingAddress?.State,
                    BillingZip = request.BillingAddress?.ZipCode,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Create checkout with Polar API
                var polarRequest = new
                {
                    product_id = request.ProductId,
                    success_url = request.SuccessUrl ?? $"{_configuration["AppSettings:FrontendUrl"]}/checkout/success?order_id={order.OrderId}",
                    cancel_url = request.CancelUrl ?? $"{_configuration["AppSettings:FrontendUrl"]}/checkout/cancel",
                    customer_email = userEmail,
                    metadata = new Dictionary<string, object>
                    {
                        { "order_id", order.OrderId.ToString() },
                        { "license_count", request.LicenseCount },
                        { "license_years", request.LicenseYears }
                    }
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(polarRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_polarBaseUrl}/checkouts/", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Polar API error: {StatusCode} - {Response}", 
                        response.StatusCode, responseContent);
                    
                    // Update order status to failed
                    order.Status = "failed";
                    order.Notes = $"Polar API error: {responseContent}";
                    await _context.SaveChangesAsync();

                    return new CreateCheckoutResponse
                    {
                        Success = false,
                        Message = "Failed to create checkout session. Please try again.",
                        OrderId = order.OrderId
                    };
                }

                var polarResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Update order with Polar checkout ID
                order.PolarCheckoutId = polarResponse.GetProperty("id").GetString();
                order.Status = "checkout_created";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                var checkoutUrl = polarResponse.GetProperty("url").GetString();
                var expiresAt = polarResponse.TryGetProperty("expires_at", out var exp) 
                    ? DateTime.Parse(exp.GetString()!) 
                    : DateTime.UtcNow.AddHours(24);

                _logger.LogInformation("Checkout created successfully: OrderId={OrderId}, CheckoutId={CheckoutId}", 
                    order.OrderId, order.PolarCheckoutId);

                return new CreateCheckoutResponse
                {
                    Success = true,
                    Message = "Checkout session created successfully",
                    CheckoutUrl = checkoutUrl,
                    OrderId = order.OrderId,
                    CheckoutId = order.PolarCheckoutId,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating checkout for {Email}", userEmail);
                return new CreateCheckoutResponse
                {
                    Success = false,
                    Message = "An error occurred while creating checkout session"
                };
            }
        }

        /// <summary>
        /// Process webhook event from Polar.sh
        /// </summary>
        public async Task<bool> ProcessWebhookAsync(PolarWebhookEvent webhookEvent, string rawPayload)
        {
            try
            {
                _logger.LogInformation("Processing Polar webhook: {EventType}", webhookEvent.Type);

                switch (webhookEvent.Type)
                {
                    case "checkout.completed":
                        await HandleCheckoutCompletedAsync(webhookEvent.Data, rawPayload);
                        break;

                    case "order.created":
                        await HandleOrderCreatedAsync(webhookEvent.Data, rawPayload);
                        break;

                    case "order.paid":
                        await HandleOrderPaidAsync(webhookEvent.Data, rawPayload);
                        break;

                    case "order.refunded":
                        await HandleOrderRefundedAsync(webhookEvent.Data, rawPayload);
                        break;

                    case "license_key.created":
                        await HandleLicenseKeyCreatedAsync(webhookEvent.Data, rawPayload);
                        break;

                    default:
                        _logger.LogInformation("Unhandled webhook event type: {EventType}", webhookEvent.Type);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing webhook: {EventType}", webhookEvent.Type);
                return false;
            }
        }

        private async Task HandleCheckoutCompletedAsync(PolarWebhookData? data, string rawPayload)
        {
            if (data?.Checkout?.Id == null) return;

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PolarCheckoutId == data.Checkout.Id);

            if (order == null)
            {
                _logger.LogWarning("Order not found for checkout: {CheckoutId}", data.Checkout.Id);
                return;
            }

            order.Status = "completed";
            order.PaidAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.WebhookPayload = rawPayload;

            if (data.Amount != null)
            {
                order.AmountCents = data.Amount.Amount ?? 0;
                order.Currency = data.Amount.Currency ?? "USD";
            }

            order.PaymentMethod = data.PaymentMethod;

            // Calculate license expiry
            order.LicenseExpiresAt = DateTime.UtcNow.AddYears(order.LicenseYears);

            await _context.SaveChangesAsync();

            _logger.LogInformation("Checkout completed: OrderId={OrderId}", order.OrderId);
        }

        private async Task HandleOrderCreatedAsync(PolarWebhookData? data, string rawPayload)
        {
            if (data?.Id == null) return;

            // Try to find existing order by metadata
            var orderId = data.Metadata?.TryGetValue("order_id", out var oid) == true 
                ? int.Parse(oid?.ToString() ?? "0") 
                : 0;

            // Extract checkout ID to avoid null propagation in lambda
            var checkoutId = data.Checkout?.Id;
            
            var order = orderId > 0 
                ? await _context.Orders.FindAsync(orderId)
                : await _context.Orders.FirstOrDefaultAsync(o => o.PolarCheckoutId == checkoutId);

            if (order != null)
            {
                order.PolarOrderId = data.Id;
                order.Status = "processing";
                order.UpdatedAt = DateTime.UtcNow;
                order.WebhookPayload = rawPayload;
                await _context.SaveChangesAsync();
            }

            _logger.LogInformation("Order created in Polar: {PolarOrderId}", data.Id);
        }

        private async Task HandleOrderPaidAsync(PolarWebhookData? data, string rawPayload)
        {
            if (data?.Id == null) return;

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PolarOrderId == data.Id);

            if (order == null)
            {
                _logger.LogWarning("Order not found for Polar order: {PolarOrderId}", data.Id);
                return;
            }

            order.Status = "paid";
            order.PaidAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.LicenseExpiresAt = DateTime.UtcNow.AddYears(order.LicenseYears);

            // Store license keys if provided
            if (data.LicenseKeys?.Any() == true)
            {
                order.LicenseKeys = JsonSerializer.Serialize(data.LicenseKeys.Select(k => k.Key));
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation("Order paid: OrderId={OrderId}, PolarOrderId={PolarOrderId}", 
                order.OrderId, data.Id);
        }

        private async Task HandleOrderRefundedAsync(PolarWebhookData? data, string rawPayload)
        {
            if (data?.Id == null) return;

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PolarOrderId == data.Id);

            if (order != null)
            {
                order.Status = "refunded";
                order.UpdatedAt = DateTime.UtcNow;
                order.Notes = $"Refunded at {DateTime.UtcNow:u}";
                await _context.SaveChangesAsync();

                _logger.LogInformation("Order refunded: OrderId={OrderId}", order.OrderId);
            }
        }

        private async Task HandleLicenseKeyCreatedAsync(PolarWebhookData? data, string rawPayload)
        {
            if (data?.LicenseKeys == null) return;

            // Find the order by checkout or order ID
            // Extract checkout ID to avoid null propagation in lambda
            var checkoutId = data.Checkout?.Id;
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.PolarOrderId == data.Id || o.PolarCheckoutId == checkoutId);

            if (order != null && data.LicenseKeys.Any())
            {
                var keys = data.LicenseKeys.Select(k => k.Key).ToList();
                order.LicenseKeys = JsonSerializer.Serialize(keys);
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("License keys added to order: OrderId={OrderId}, KeyCount={Count}", 
                    order.OrderId, keys.Count);
            }
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        public async Task<OrderDto?> GetOrderAsync(int orderId, string userEmail)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserEmail == userEmail);

            if (order == null) return null;

            return MapToDto(order);
        }

        /// <summary>
        /// Get orders for a user
        /// </summary>
        public async Task<OrderListResponse> GetOrdersAsync(string userEmail, int page = 1, int pageSize = 10)
        {
            var query = _context.Orders
                .Where(o => o.UserEmail == userEmail)
                .OrderByDescending(o => o.CreatedAt);

            var totalCount = await query.CountAsync();
            var orders = await query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return new OrderListResponse
            {
                Orders = orders.Select(MapToDto).ToList(),
                TotalCount = totalCount,
                Page = page,
                PageSize = pageSize
            };
        }

        /// <summary>
        /// Verify webhook signature from Polar
        /// </summary>
        public bool VerifyWebhookSignature(string payload, string signature)
        {
            if (string.IsNullOrEmpty(_polarWebhookSecret))
            {
                _logger.LogWarning("Webhook secret not configured - skipping verification");
                return true; // Allow in development
            }

            try
            {
                using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_polarWebhookSecret));
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
                var expectedSignature = Convert.ToBase64String(hash);

                return signature == expectedSignature || signature == $"sha256={expectedSignature}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying webhook signature");
                return false;
            }
        }

        /// <summary>
        /// Get available products (configured in appsettings)
        /// </summary>
        public Task<List<ProductDto>> GetProductsAsync()
        {
            var products = _configuration.GetSection("Polar:Products").Get<List<ProductDto>>() 
                ?? new List<ProductDto>
                {
                    new ProductDto
                    {
                        ProductId = "default",
                        Name = "D-Secure Drive Eraser",
                        Description = "Enterprise data erasure solution",
                        PricePerLicenseCents = 2000, // $20 per license
                        Currency = "USD",
                        Features = new List<string>
                        {
                            "Military-grade data erasure",
                            "Compliance reporting",
                            "24/7 support"
                        }
                    }
                };

            return Task.FromResult(products);
        }

        private OrderDto MapToDto(Order order)
        {
            List<string>? licenseKeys = null;
            if (!string.IsNullOrEmpty(order.LicenseKeys))
            {
                try
                {
                    licenseKeys = JsonSerializer.Deserialize<List<string>>(order.LicenseKeys);
                }
                catch { }
            }

            return new OrderDto
            {
                OrderId = order.OrderId,
                PolarOrderId = order.PolarOrderId,
                UserEmail = order.UserEmail,
                ProductName = order.ProductName,
                LicenseCount = order.LicenseCount,
                LicenseYears = order.LicenseYears,
                Amount = order.AmountCents / 100m,
                Currency = order.Currency,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                CreatedAt = order.CreatedAt,
                PaidAt = order.PaidAt,
                LicenseExpiresAt = order.LicenseExpiresAt,
                LicenseKeys = licenseKeys
            };
        }
    }
}
