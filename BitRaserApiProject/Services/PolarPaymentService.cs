using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Polar.sh Payment Service Implementation
    /// ✅ PRO-LEVEL: Dynamic product sync, price interval mapping, and 1-hour caching
    /// Handles checkout sessions, webhooks, and order management
    /// </summary>
    public class PolarPaymentService : IPolarPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<PolarPaymentService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;

        // Polar API configuration
        private readonly string _polarAccessToken;
        private readonly string _polarWebhookSecret;
        private readonly string _polarBaseUrl;
        private readonly bool _isSandbox;

        // ✅ PRO-LEVEL: Cache keys
        private const string CACHE_KEY_BILLING_PLANS = "polar:billing_plans";
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromHours(1);

        public PolarPaymentService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<PolarPaymentService> logger,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("PolarApi");
            _cache = cache;

            // Load configuration - Environment variables take priority
            // ✅ Made optional - won't crash if Polar not configured (Dodo-only mode)
            _polarAccessToken = Environment.GetEnvironmentVariable("Polar__AccessToken")
                ?? configuration["Polar:AccessToken"] 
                ?? ""; // Empty string = Polar not configured
            
            _polarWebhookSecret = Environment.GetEnvironmentVariable("Polar__WebhookSecret")
                ?? configuration["Polar:WebhookSecret"] 
                ?? "";

            _isSandbox = bool.TryParse(Environment.GetEnvironmentVariable("Polar__Sandbox"), out var sandbox)
                ? sandbox
                : configuration.GetValue<bool>("Polar:Sandbox", true);
            
            _polarBaseUrl = _isSandbox 
                ? "https://sandbox-api.polar.sh/api/v1" 
                : "https://api.polar.sh/api/v1";

            // Only configure HttpClient if Polar is enabled
            if (!string.IsNullOrEmpty(_polarAccessToken))
            {
                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_polarAccessToken}");
                _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
                
                var tokenPreview = _polarAccessToken.Length > 20 
                    ? $"{_polarAccessToken.Substring(0, 20)}..." 
                    : "[SHORT TOKEN]";
                _logger.LogWarning("🐻 Polar Config Loaded:");
                _logger.LogWarning("   - Token: {TokenPreview}", tokenPreview);
                _logger.LogWarning("   - Sandbox: {IsSandbox}", _isSandbox);
                _logger.LogWarning("   - Base URL: {BaseUrl}", _polarBaseUrl);
            }
            else
            {
                _logger.LogWarning("⚠️ Polar NOT configured - running in Dodo-only mode");
            }
        }

        /// <summary>
        /// Create a checkout session with Polar.sh
        /// </summary>
        public async Task<CreateCheckoutResponse> CreateCheckoutAsync(CreateCheckoutRequest request, string userEmail)
        {
            // ✅ Guard: Return error if Polar not configured
            if (string.IsNullOrEmpty(_polarAccessToken))
            {
                return new CreateCheckoutResponse
                {
                    Success = false,
                    Message = "Polar payments not configured. Please use Dodo Payments instead."
                };
            }

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
            
            Order? order = null;
            if (orderId > 0)
            {
                order = await _context.Orders.FindAsync(orderId);
            }
            else if (checkoutId != null)
            {
                order = await _context.Orders.FirstOrDefaultAsync(o => o.PolarCheckoutId == checkoutId);
            }

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
            Order? order = null;
            if (checkoutId != null)
            {
                order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.PolarOrderId == data.Id || o.PolarCheckoutId == checkoutId);
            }
            else
            {
                order = await _context.Orders
                    .FirstOrDefaultAsync(o => o.PolarOrderId == data.Id);
            }

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

        #region Pro-Level Methods

        /// <summary>
        /// ✅ PRO-LEVEL: Get billing plans with monthly/yearly price IDs
        /// Fetches products dynamically from Polar API and caches for 1 hour
        /// </summary>
        public async Task<List<BillingPlanDto>> GetBillingPlansAsync()
        {
            // ✅ Guard: Return fallback if Polar not configured
            if (string.IsNullOrEmpty(_polarAccessToken))
            {
                _logger.LogWarning("⚠️ Polar not configured - returning empty billing plans");
                return GetFallbackBillingPlans();
            }

            // ✅ Check cache first
            if (_cache.TryGetValue(CACHE_KEY_BILLING_PLANS, out List<BillingPlanDto>? cachedPlans) && cachedPlans != null)
            {
                _logger.LogDebug("📦 Returning cached billing plans ({Count} plans)", cachedPlans.Count);
                return cachedPlans;
            }

            _logger.LogInformation("🔄 Fetching billing plans from Polar API...");

            try
            {
                // ✅ Call Polar API to get products
                var apiUrl = $"{_polarBaseUrl}/products?organization_id=2dc2e935-0587-4465-8d0a-67510f69e02f&is_archived=false&limit=100";
                _logger.LogWarning("📡 DEBUG: Calling Polar API: {Url}", apiUrl);
                _logger.LogWarning("🔑 DEBUG: Token: {Token}", _polarAccessToken.Substring(0, 30) + "...");
                _logger.LogWarning("🎯 DEBUG: Sandbox Mode: {Sandbox}", _isSandbox);
                
                var response = await _httpClient.GetAsync(apiUrl);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("📥 Polar API Response: StatusCode={StatusCode}, ContentLength={Length}", 
                    response.StatusCode, responseContent.Length);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Polar API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                    
                    // Log full error for debugging
                    _logger.LogError("Full API Error - URL: {Url}, Headers: {Headers}", apiUrl, string.Join(", ", _httpClient.DefaultRequestHeaders.Select(h => $"{h.Key}={string.Join(",", h.Value)}")));
                    
                    return GetFallbackBillingPlans();
                }
                
                // Log first 500 chars of response for debugging
                _logger.LogDebug("📄 Polar API raw response (first 500 chars): {Content}", 
                    responseContent.Length > 500 ? responseContent.Substring(0, 500) : responseContent);

                var jsonOptions = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var productList = JsonSerializer.Deserialize<PolarProductListResponse>(responseContent, jsonOptions);

                if (productList?.Items == null || !productList.Items.Any())
                {
                    _logger.LogWarning("⚠️ No products returned from Polar API");
                    return GetFallbackBillingPlans();
                }

                var billingPlans = new List<BillingPlanDto>();
                int order = 0;

                foreach (var product in productList.Items.Where(p => !p.Is_Archived))
                {
                    // ✅ PRO-LEVEL: Map monthly and yearly prices from prices array
                    var monthlyPrice = product.Prices?.FirstOrDefault(p => 
                        (p.RecurringInterval ?? p.Recurring_Interval)?.ToLower() == "month");
                    var yearlyPrice = product.Prices?.FirstOrDefault(p => 
                        (p.RecurringInterval ?? p.Recurring_Interval)?.ToLower() == "year");

                    // If no recurring prices, skip or use one-time price
                    var oneTimePrice = product.Prices?.FirstOrDefault(p => 
                        p.Type?.ToLower() == "one_time" || string.IsNullOrEmpty(p.RecurringInterval ?? p.Recurring_Interval));

                    var plan = new BillingPlanDto
                    {
                        ProductId = product.Id ?? "",
                        Name = product.Name ?? "Unknown Plan",
                        Description = product.Description,
                        MonthlyPriceId = monthlyPrice?.Id,
                        YearlyPriceId = yearlyPrice?.Id ?? oneTimePrice?.Id,
                        MonthlyAmount = ((monthlyPrice?.PriceAmount ?? monthlyPrice?.Price_Amount) ?? 0) / 100m,
                        YearlyAmount = ((yearlyPrice?.PriceAmount ?? yearlyPrice?.Price_Amount ?? oneTimePrice?.PriceAmount ?? oneTimePrice?.Price_Amount) ?? 0) / 100m,
                        Currency = (monthlyPrice?.PriceCurrency ?? monthlyPrice?.Price_Currency ?? yearlyPrice?.PriceCurrency ?? yearlyPrice?.Price_Currency ?? oneTimePrice?.PriceCurrency ?? oneTimePrice?.Price_Currency) ?? "USD",
                        IsPopular = product.Name?.Contains("Pro", StringComparison.OrdinalIgnoreCase) == true,
                        DisplayOrder = order++
                    };

                    // ✅ Only add plans that have at least one valid price
                    if (!string.IsNullOrEmpty(plan.MonthlyPriceId) || !string.IsNullOrEmpty(plan.YearlyPriceId))
                    {
                        billingPlans.Add(plan);
                    }
                }

                // ✅ Cache for 1 hour
                _cache.Set(CACHE_KEY_BILLING_PLANS, billingPlans, CACHE_DURATION);

                _logger.LogInformation("✅ Cached {Count} billing plans for 1 hour", billingPlans.Count);

                return billingPlans;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching billing plans from Polar API");
                return GetFallbackBillingPlans();
            }
        }

        /// <summary>
        /// ✅ PRO-LEVEL: Create checkout using price ID (not product ID)
        /// </summary>
        public async Task<PriceCheckoutResponse> CreatePriceCheckoutAsync(PriceCheckoutRequest request, string userEmail)
        {
            // ✅ Guard: Return error if Polar not configured
            if (string.IsNullOrEmpty(_polarAccessToken))
            {
                return new PriceCheckoutResponse
                {
                    Success = false,
                    Message = "Polar payments not configured. Please use Dodo Payments instead."
                };
            }

            try
            {
                _logger.LogInformation("🛒 Creating price-based checkout for {Email}, priceId: {PriceId}", 
                    userEmail, request.PriceId);

                // ✅ Polar checkout API payload - using correct field names
                // Polar API uses "product_price_id" or just creates checkout via price
                var checkoutPayload = new Dictionary<string, object>
                {
                    ["product_price_id"] = request.PriceId,
                    ["success_url"] = request.SuccessUrl,
                    ["customer_email"] = request.CustomerEmail ?? userEmail
                };
                
                // Only add metadata if not empty
                if (request.Metadata != null && request.Metadata.Count > 0)
                {
                    checkoutPayload["metadata"] = request.Metadata;
                }

                var jsonPayload = JsonSerializer.Serialize(checkoutPayload);
                _logger.LogInformation("📤 Checkout payload: {Payload}", jsonPayload);
                
                var jsonContent = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync($"{_polarBaseUrl}/checkouts/", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();
                
                _logger.LogInformation("📥 Checkout response: {StatusCode} - {Content}", 
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Polar checkout error: {StatusCode} - {Response}", 
                        response.StatusCode, responseContent);
                    
                    return new PriceCheckoutResponse
                    {
                        Success = false,
                        Message = $"Checkout failed: {response.StatusCode}"
                    };
                }

                var checkoutResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                var checkoutUrl = checkoutResponse.TryGetProperty("url", out var urlProp) 
                    ? urlProp.GetString() 
                    : null;
                var checkoutId = checkoutResponse.TryGetProperty("id", out var idProp) 
                    ? idProp.GetString() 
                    : null;
                DateTime? expiresAt = checkoutResponse.TryGetProperty("expires_at", out var expProp) && expProp.ValueKind != JsonValueKind.Null
                    ? DateTime.Parse(expProp.GetString()!)
                    : DateTime.UtcNow.AddHours(24);

                _logger.LogInformation("✅ Price checkout created: {CheckoutId}", checkoutId);

                return new PriceCheckoutResponse
                {
                    Success = true,
                    Message = "Checkout session created successfully",
                    CheckoutUrl = checkoutUrl,
                    CheckoutId = checkoutId,
                    ExpiresAt = expiresAt
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating price checkout for {Email}", userEmail);
                return new PriceCheckoutResponse
                {
                    Success = false,
                    Message = "An error occurred while creating checkout session"
                };
            }
        }

        /// <summary>
        /// ✅ PRO-LEVEL: Force refresh the product catalog cache
        /// </summary>
        public async Task RefreshProductCacheAsync()
        {
            _logger.LogInformation("🔄 Force refreshing product cache...");
            _cache.Remove(CACHE_KEY_BILLING_PLANS);
            await GetBillingPlansAsync(); // Re-fetch and cache
        }

        /// <summary>
        /// Fallback billing plans when API is unavailable
        /// </summary>
        private List<BillingPlanDto> GetFallbackBillingPlans()
        {
            _logger.LogWarning("⚠️ Using fallback billing plans");
            
            return new List<BillingPlanDto>
            {
                new BillingPlanDto
                {
                    ProductId = "fallback",
                    Name = "D-Secure Standard",
                    Description = "Contact sales for pricing",
                    MonthlyAmount = 0,
                    YearlyAmount = 0,
                    Currency = "USD",
                    IsPopular = false
                }
            };
        }

        #endregion
    }
}
