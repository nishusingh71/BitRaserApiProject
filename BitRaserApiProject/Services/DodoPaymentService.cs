using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using BCrypt.Net;
using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Dodo Payments Service Implementation
    /// Handles checkout sessions, webhooks, and order management with Dodo Payments
    /// </summary>
    public class DodoPaymentService : IDodoPaymentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DodoPaymentService> _logger;
        private readonly HttpClient _httpClient;
        private readonly IMemoryCache _cache;
        private readonly IEmailService _emailService;
        private readonly Email.IEmailOrchestrator? _emailOrchestrator;
        private readonly Email.ExcelExportService? _excelExportService;

        // Dodo API configuration
        private readonly string _dodoApiKey;
        private readonly string _dodoWebhookSecret;
        private readonly string _dodoBaseUrl;
        private readonly bool _isSandbox;

        // Cache keys
        private const string CACHE_KEY_DODO_PRODUCTS = "dodo:products";
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromHours(1);

        public DodoPaymentService(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<DodoPaymentService> logger,
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache,
            IEmailService emailService,
            Email.IEmailOrchestrator? emailOrchestrator = null,
            Email.ExcelExportService? excelExportService = null)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("DodoApi");
            _cache = cache;
            _emailService = emailService;
            _emailOrchestrator = emailOrchestrator;
            _excelExportService = excelExportService;

            // Load configuration - Environment variables take priority
            _dodoApiKey = Environment.GetEnvironmentVariable("Dodo__ApiKey")
                ?? configuration["Dodo:ApiKey"]
                ?? throw new InvalidOperationException("Dodo API key not configured. Set Dodo__ApiKey environment variable.");

            _dodoWebhookSecret = Environment.GetEnvironmentVariable("Dodo__WebhookSecret")
                ?? configuration["Dodo:WebhookSecret"]
                ?? "";

            _isSandbox = bool.TryParse(Environment.GetEnvironmentVariable("Dodo__Sandbox"), out var sandbox)
                ? sandbox
                : configuration.GetValue<bool>("Dodo:Sandbox", false);

            _dodoBaseUrl = _isSandbox
                ? "https://test.dodopayments.com"
                : "https://live.dodopayments.com";

            // Configure HttpClient
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_dodoApiKey}");
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            // Debug logging (mask key for security)
            var keyPreview = _dodoApiKey.Length > 20
                ? $"{_dodoApiKey.Substring(0, 20)}..."
                : "[SHORT KEY]";
            
            // 🔍 PRODUCTION DEBUG: Clearly identify LIVE vs TEST mode
            var modeLabel = _isSandbox ? "TEST (SANDBOX)" : "LIVE (PRODUCTION)";
            _logger.LogWarning("🦤 Dodo Config Loaded:");
            _logger.LogWarning("   - MODE: {Mode} ⚠️", modeLabel);
            _logger.LogWarning("   - Key: {KeyPreview}", keyPreview);
            _logger.LogWarning("   - Sandbox: {IsSandbox}", _isSandbox);
            _logger.LogWarning("   - Base URL: {BaseUrl}", _dodoBaseUrl);
        }

        /// <summary>
        /// Create a checkout session with Dodo Payments
        /// </summary>
        public async Task<DodoCheckoutResponse> CreateCheckoutAsync(DodoCheckoutRequest request, string userEmail)
        {
            try
            {
                _logger.LogInformation("🛒 Creating Dodo checkout for user {Email}, product {ProductId}",
                    userEmail, request.ProductId);

                // Create order in database first (pending status)
                var order = new Order
                {
                    UserEmail = userEmail,
                    ProductId = request.ProductId,
                    LicenseCount = 1,
                    LicenseYears = 1,
                    Status = "pending",
                    PaymentProvider = "dodo",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Build Dodo checkout request - using product_cart format per Dodo API docs
                var dodoRequest = new Dictionary<string, object>
                {
                    // Product Cart - required by Dodo
                    ["product_cart"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["product_id"] = request.ProductId,
                            ["quantity"] = 1
                        }
                    },
                    // ✅ Enable tax calculation by default
                    ["tax_enabled"] = true
                };

                // Add success/return URL if provided - Dodo uses return_url for redirects
                // ✅ Automatically append order_id so frontend can fetch order details
                if (!string.IsNullOrEmpty(request.SuccessUrl))
                {
                    var separator = request.SuccessUrl.Contains('?') ? '&' : '?';
                    var successUrlWithOrderId = $"{request.SuccessUrl}{separator}order_id={order.OrderId}";
                    
                    dodoRequest["return_url"] = successUrlWithOrderId;  // Primary redirect URL
                    dodoRequest["success_url"] = successUrlWithOrderId; // Fallback
                    dodoRequest["redirect_url"] = successUrlWithOrderId; // Alternative format
                    
                    _logger.LogInformation("✅ Success URL with OrderId: {Url}", successUrlWithOrderId);
                }

                // Add cancel URL if provided (for failed/cancelled payments)
                if (!string.IsNullOrEmpty(request.CancelUrl))
                {
                    dodoRequest["cancel_url"] = request.CancelUrl;
                }

                // Add discount code if provided (automatically applies during checkout)
                if (!string.IsNullOrEmpty(request.DiscountCode))
                {
                    dodoRequest["discount_code"] = request.DiscountCode;
                }

                // Add metadata
                var metadata = request.Metadata ?? new Dictionary<string, string>();
                metadata["order_id"] = order.OrderId.ToString();
                dodoRequest["metadata"] = metadata;

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(dodoRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                _logger.LogInformation("📤 Dodo checkout payload: {Payload}", JsonSerializer.Serialize(dodoRequest));
                _logger.LogInformation("📡 Posting to: {Url}", $"{_dodoBaseUrl}/checkouts");

                var response = await _httpClient.PostAsync($"{_dodoBaseUrl}/checkouts", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("📥 Dodo API Response: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Dodo API error: {StatusCode} - {Response}",
                        response.StatusCode, responseContent);

                    // Update order status to failed
                    order.Status = "failed";
                    order.Notes = $"Dodo API error: {responseContent}";
                    await _context.SaveChangesAsync();

                    return new DodoCheckoutResponse
                    {
                        Success = false,
                        Message = "Failed to create checkout session. Please try again."
                    };
                }

                var dodoResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Extract session ID and checkout URL from /checkouts response
                var sessionId = dodoResponse.TryGetProperty("session_id", out var sidProp)
                    ? sidProp.GetString()
                    : dodoResponse.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

                var checkoutUrl = dodoResponse.TryGetProperty("checkout_url", out var urlProp)
                    ? urlProp.GetString()
                    : dodoResponse.TryGetProperty("url", out var altUrlProp) ? altUrlProp.GetString() : null;

                _logger.LogInformation("📥 Parsed response: SessionId={SessionId}, CheckoutUrl={Url}", 
                    sessionId, checkoutUrl);

                // Update order with Dodo session ID
                order.DodoPaymentId = sessionId;
                order.Status = "checkout_created";
                order.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Dodo checkout created: OrderId={OrderId}, SessionId={SessionId}",
                    order.OrderId, sessionId);

                return new DodoCheckoutResponse
                {
                    Success = true,
                    Message = "Checkout session created successfully",
                    OrderId = order.OrderId, // ✅ Return OrderId for frontend
                    PaymentId = sessionId,
                    CheckoutUrl = checkoutUrl,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating Dodo checkout for {Email}", userEmail);
                return new DodoCheckoutResponse
                {
                    Success = false,
                    Message = "An error occurred while creating checkout session"
                };
            }
        }

        /// <summary>
        /// 🆕 Create a GUEST checkout session (No Auth Required)
        /// NO ORDER IS CREATED until webhook fires with actual customer data
        /// This allows new users to buy without logging in first
        /// </summary>
        public async Task<GuestCheckoutResponse> CreateGuestCheckoutSessionAsync(GuestCheckoutRequest request)
        {
            try
            {
                _logger.LogInformation("🛒 Creating GUEST Dodo checkout for ProductId: {ProductId}", request.ProductId);

                // Build Dodo checkout request - using product_cart format per Dodo API docs
                var dodoRequest = new Dictionary<string, object>
                {
                    // Product Cart - required by Dodo
                    ["product_cart"] = new[]
                    {
                        new Dictionary<string, object>
                        {
                            ["product_id"] = request.ProductId,
                            ["quantity"] = request.Quantity > 0 ? request.Quantity : 1
                        }
                    },
                    // ✅ Tax calculation from request (default: true)
                    ["tax_enabled"] = request.TaxEnabled
                };

                // ✅ Default success URL from FRONTEND_URL env or dsecuretech.com
                var frontendBaseUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                    ?? _configuration["FrontendUrl"]
                    ?? "https://dsecuretech.com";
                var defaultSuccessUrl = $"{frontendBaseUrl.TrimEnd('/')}/order-success";
                
                // Add return URL - use provided or default
                var successUrl = !string.IsNullOrEmpty(request.ReturnUrl) 
                    ? request.ReturnUrl 
                    : defaultSuccessUrl;
                    
                dodoRequest["return_url"] = successUrl;
                dodoRequest["success_url"] = successUrl;
                dodoRequest["redirect_url"] = successUrl;

                // Add cancel URL if provided (default to success URL)
                dodoRequest["cancel_url"] = !string.IsNullOrEmpty(request.CancelUrl) 
                    ? request.CancelUrl 
                    : defaultSuccessUrl;

                // Add discount code if provided
                if (!string.IsNullOrEmpty(request.DiscountCode))
                {
                    dodoRequest["discount_code"] = request.DiscountCode;
                }

                // ========== TAX RELATED FIELDS (Dodo SDK) ==========

                // Add Tax ID for B2B invoicing
                if (!string.IsNullOrEmpty(request.TaxId))
                {
                    dodoRequest["tax_id"] = request.TaxId;
                }

                // Add billing address for country-based tax calculation
                if (request.BillingAddress != null && !string.IsNullOrEmpty(request.BillingAddress.Country))
                {
                    dodoRequest["billing"] = new Dictionary<string, object?>
                    {
                        ["country"] = request.BillingAddress.Country,
                        ["state"] = request.BillingAddress.State,
                        ["city"] = request.BillingAddress.City,
                        ["street"] = request.BillingAddress.Street,
                        ["zipcode"] = request.BillingAddress.Zipcode
                    }.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);
                    
                    _logger.LogInformation("🌍 Billing country for tax: {Country}", request.BillingAddress.Country);
                }

                // Add customer info to pre-fill checkout form
                if (request.Customer != null && 
                    (!string.IsNullOrEmpty(request.Customer.Email) || 
                     !string.IsNullOrEmpty(request.Customer.Name)))
                {
                    dodoRequest["customer"] = new Dictionary<string, object?>
                    {
                        ["email"] = request.Customer.Email,
                        ["name"] = request.Customer.Name,
                        ["phone_number"] = request.Customer.PhoneNumber
                    }.Where(kv => kv.Value != null).ToDictionary(kv => kv.Key, kv => kv.Value);
                }

                // Add metadata if provided
                if (request.Metadata != null && request.Metadata.Count > 0)
                {
                    dodoRequest["metadata"] = request.Metadata;
                }

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(dodoRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                _logger.LogInformation("📤 Guest checkout payload: {Payload}", JsonSerializer.Serialize(dodoRequest));
                _logger.LogInformation("📡 Posting to: {Url}", $"{_dodoBaseUrl}/checkouts");

                var response = await _httpClient.PostAsync($"{_dodoBaseUrl}/checkouts", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("📥 Dodo API Response: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Dodo API error: {StatusCode} - {Response}",
                        response.StatusCode, responseContent);

                    return new GuestCheckoutResponse
                    {
                        Success = false,
                        Message = "Failed to create checkout session. Please try again."
                    };
                }

                var dodoResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Extract session ID and checkout URL
                var sessionId = dodoResponse.TryGetProperty("session_id", out var sidProp)
                    ? sidProp.GetString()
                    : dodoResponse.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

                var checkoutUrl = dodoResponse.TryGetProperty("checkout_url", out var urlProp)
                    ? urlProp.GetString()
                    : dodoResponse.TryGetProperty("url", out var altUrlProp) ? altUrlProp.GetString() : null;

                _logger.LogInformation("✅ Guest checkout created: SessionId={SessionId}, Url={Url}",
                    sessionId, checkoutUrl);

                // NOTE: NO ORDER CREATED HERE! Order will be created when webhook fires
                // with the actual customer data from the Dodo checkout form

                return new GuestCheckoutResponse
                {
                    Success = true,
                    Message = "Checkout session created successfully",
                    SessionId = sessionId,
                    CheckoutUrl = checkoutUrl
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating guest Dodo checkout");
                return new GuestCheckoutResponse
                {
                    Success = false,
                    Message = "An error occurred while creating checkout session"
                };
            }
        }

        /// <summary>
        /// Process webhook event from Dodo Payments
        /// </summary>
        public async Task<bool> ProcessWebhookAsync(DodoWebhookEvent webhookEvent, string rawPayload)
        {
            try
            {
                _logger.LogInformation("🦤 Processing Dodo webhook: {EventType}", webhookEvent.Type);

                switch (webhookEvent.Type)
                {
                    case "payment.succeeded":
                        await HandlePaymentSucceededAsync(webhookEvent.Data, rawPayload);
                        break;

                    case "payment.failed":
                        await HandlePaymentFailedAsync(webhookEvent.Data, rawPayload);
                        break;

                    case "subscription.created":
                    case "subscription.updated":
                        await HandleSubscriptionEventAsync(webhookEvent.Data, rawPayload);
                        break;

                    case "subscription.cancelled":
                        await HandleSubscriptionCancelledAsync(webhookEvent.Data, rawPayload);
                        break;

                    case "refund.succeeded":
                        await HandleRefundSucceededAsync(webhookEvent.Data, rawPayload);
                        break;

                    case "license_key.created":
                        await HandleLicenseKeyCreatedAsync(webhookEvent.Data, rawPayload);
                        break;

                    default:
                        _logger.LogInformation("Unhandled Dodo webhook event type: {EventType}", webhookEvent.Type);
                        break;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing Dodo webhook: {EventType}", webhookEvent.Type);
                return false;
            }
        }

        /// <summary>
        /// Handle payment.succeeded webhook event
        /// 1. Find or create user account
        /// 2. Generate secure password & hash with BCrypt
        /// 3. Link user to order, mark as PAID
        /// 4. Send credentials email
        /// ⚠️ SECURITY: Password ONLY used in email, NEVER logged
        /// </summary>
        private async Task HandlePaymentSucceededAsync(DodoWebhookData? data, string rawPayload)
        {
            if (data?.PaymentId == null) return;

            _logger.LogInformation("💳 Processing payment.succeeded for PaymentId: {PaymentId}", data.PaymentId);
            
            // 🔍 PRODUCTION DEBUG: Log full payload structure for LIVE vs TEST comparison
            _logger.LogWarning("🔍 LIVE PAYLOAD ANALYSIS - PaymentId: {PaymentId}", data.PaymentId);
            _logger.LogWarning("  Amount: {Amount}", data.Amount ?? 0);
            _logger.LogWarning("  TotalAmount: {TotalAmount}", data.TotalAmount ?? 0);
            _logger.LogWarning("  SettlementAmount: {SettlementAmount}", data.SettlementAmount ?? 0);
            _logger.LogWarning("  Tax: {Tax}", data.Tax ?? 0);
            _logger.LogWarning("  Currency: {Currency}", data.Currency ?? "NULL");
            _logger.LogWarning("  SettlementCurrency: {SettlementCurrency}", data.SettlementCurrency ?? "NULL");
            _logger.LogWarning("  InvoiceId: {InvoiceId}", data.InvoiceId ?? "NULL");
            _logger.LogWarning("  CardLastFour: {CardLastFour}", data.CardLastFour ?? "NULL");
            _logger.LogWarning("  CardNetwork: {CardNetwork}", data.CardNetwork ?? "NULL");
            _logger.LogWarning("  CardType: {CardType}", data.CardType ?? "NULL");
            _logger.LogWarning("  PaymentMethod: {PaymentMethod}", data.PaymentMethod ?? "NULL");
            _logger.LogWarning("  PaymentLink: {PaymentLink}", data.PaymentLink ?? "NULL");
            _logger.LogWarning("  Customer.Name: {Name}", data.Customer?.Name ?? "NULL");
            _logger.LogWarning("  Customer.Email: {Email}", data.Customer?.Email ?? "NULL");
            _logger.LogWarning("  Customer.CustomerId: {CustomerId}", data.Customer?.CustomerId ?? "NULL");
            _logger.LogWarning("  Customer.PhoneNumber: {Phone}", data.Customer?.PhoneNumber ?? "NULL");
            _logger.LogWarning("  Billing.Street: {Street}", data.Billing?.Street ?? "NULL");
            _logger.LogWarning("  Billing.City: {City}", data.Billing?.City ?? "NULL");
            _logger.LogWarning("  Billing.State: {State}", data.Billing?.State ?? "NULL");
            _logger.LogWarning("  Billing.Country: {Country}", data.Billing?.Country ?? "NULL");
            _logger.LogWarning("  Billing.Zipcode: {Zip}", data.Billing?.Zipcode ?? "NULL");
            _logger.LogWarning("  ProductCart Count: {Count}", data.ProductCart?.Count ?? 0);
            _logger.LogInformation("🔍 DEBUG: Webhook InvoiceId = '{InvoiceId}'", data.InvoiceId ?? "NULL");

            // Find the order
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.DodoPaymentId == data.PaymentId);

            if (order == null)
            {
                // Try to find by metadata order_id
                if (data.Metadata?.TryGetValue("order_id", out var orderIdObj) == true)
                {
                    var orderId = int.Parse(orderIdObj?.ToString() ?? "0");
                    order = await _context.Orders.FindAsync(orderId);
                }
            }

            if (order == null)
            {
                _logger.LogInformation("🆕 Dodo payment {PaymentId} not found in DB - Creating new order", data.PaymentId);
                
                // Create new order for direct Dodo purchases (e.g. Payment Links)
                order = new Order
                {
                    DodoPaymentId = data.PaymentId,
                    DodoInvoiceId = data.InvoiceId, // ✅ Store Invoice ID
                    PaymentProvider = "dodo",
                    UserEmail = data.Customer?.Email ?? "unknown@dodo.com",
                    Status = "processing", // Will be updated to paid below
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    AmountCents = data.TotalAmount ?? data.SettlementAmount ?? 0,
                    Currency = data.Currency ?? data.SettlementCurrency ?? "USD",
                    // Extract product info from cart
                    ProductId = data.ProductCart?.FirstOrDefault()?.ProductId,
                    LicenseCount = data.ProductCart?.Sum(p => p.Quantity) ?? 1,
                    LicenseYears = 1, 
                    ProductName = "Dodo Purchase" // Default, will be updated below
                };
                
                // ✅ Fetch actual product name from Dodo API
                var productId = data.ProductCart?.FirstOrDefault()?.ProductId;
                if (!string.IsNullOrEmpty(productId))
                {
                    try
                    {
                        var products = await GetProductsAsync();
                        var product = products.FirstOrDefault(p => p.ProductId == productId);
                        if (product != null)
                        {
                            order.ProductName = product.Name;
                            _logger.LogInformation("📦 Product name fetched: {ProductName} for ProductId: {ProductId}", 
                                product.Name, productId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Failed to fetch product name for ProductId: {ProductId}", productId);
                    }
                }
                
                _context.Orders.Add(order);
                await _context.SaveChangesAsync();
            }

            // ✅ IDEMPOTENCY CHECK - Prevent duplicate processing
            if (order.WebhookProcessedAt.HasValue)
            {
                _logger.LogInformation("⚠️ Webhook already processed for OrderId: {OrderId} at {ProcessedAt}",
                    order.OrderId, order.WebhookProcessedAt);
                return;
            }

            // Extract customer info
            var customerEmail = data.Customer?.Email ?? order.UserEmail;
            var customerName = data.Customer?.Name ?? customerEmail.Split('@')[0];

            _logger.LogInformation("👤 Customer: {Email}, Name: {Name}", customerEmail, customerName);

            // ✅ Update product name if still default
            if (string.IsNullOrEmpty(order.ProductName) || order.ProductName == "Dodo Purchase" || order.ProductName == "Product")
            {
                var productId = data.ProductCart?.FirstOrDefault()?.ProductId ?? order.ProductId;
                if (!string.IsNullOrEmpty(productId))
                {
                    try
                    {
                        var products = await GetProductsAsync();
                        var product = products.FirstOrDefault(p => p.ProductId == productId);
                        if (product != null)
                        {
                            order.ProductName = product.Name;
                            _logger.LogInformation("📦 Updated product name: {ProductName}", product.Name);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "⚠️ Failed to update product name for ProductId: {ProductId}", productId);
                    }
                }
            }

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.user_email == customerEmail);

            string? tempPassword = null;
            bool userCreated = false;

            if (existingUser == null)
            {
                // ✅ CREATE NEW USER
                _logger.LogInformation("👤 Creating new user account for: {Email}", customerEmail);

                // ✅ Use default temporary password as requested
                tempPassword = "Admin@1234";

                // Hash password with BCrypt
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(tempPassword, workFactor: 12);

                // Prepare payment detail object
                var paymentDetail = new
                {
                    payment_date = DateTime.UtcNow,
                    amount = order.AmountCents,
                    currency = order.Currency,
                    order_id = order.OrderId,
                    dodo_payment_id = data.PaymentId,
                    status = "paid",
                    product_name = order.ProductName
                };

                var paymentDetailsList = new List<object> { paymentDetail };

                var newUser = new users
                {
                    user_name = customerName,
                    user_email = customerEmail,
                    user_password = tempPassword, // Store plaintext for reference
                    hash_password = hashedPassword,
                    user_role = "SuperAdmin", // Dodo payment customers get SuperAdmin role
                    status = "active",
                    is_private_cloud = false,
                    private_api = false,
                    is_domain_admin = false, // Explicitly set to false to avoid type mismatch
                    license_expiry_date = DateTime.UtcNow.AddYears(order.LicenseYears),
                    max_licenses = order.LicenseCount,
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow,
                    // ✅ Store payment history
                    payment_details_json = JsonSerializer.Serialize(paymentDetailsList)
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // Link user to order
                order.UserId = newUser.user_id;
                userCreated = true;

                _logger.LogInformation("✅ User created: UserId={UserId}, Email={Email}", 
                    newUser.user_id, customerEmail);

                // ✅ ASSIGN SUPERADMIN ROLE with all permissions
                try
                {
                    var superAdminRole = await _context.Roles
                        .FirstOrDefaultAsync(r => r.RoleName == "SuperAdmin");

                    if (superAdminRole != null)
                    {
                        var userRole = new UserRole
                        {
                            UserId = newUser.user_id,
                            RoleId = superAdminRole.RoleId,
                            AssignedByEmail = "system@dodo",
                            AssignedAt = DateTime.UtcNow
                        };

                        _context.UserRoles.Add(userRole);
                        await _context.SaveChangesAsync();

                        _logger.LogInformation("✅ SuperAdmin role assigned to UserId={UserId} with full permissions", 
                            newUser.user_id);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ SuperAdmin role not found in database - user created without role assignment");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error assigning SuperAdmin role to UserId={UserId}", newUser.user_id);
                }
            }
            else
            {
                // User already exists - link to order
                order.UserId = existingUser.user_id;
                _logger.LogInformation("👤 Existing user found: UserId={UserId}", existingUser.user_id);

                // ✅ Update payment_details_json for existing user
                try
                {
                    var paymentDetail = new
                    {
                        payment_date = DateTime.UtcNow,
                        amount = order.AmountCents,
                        currency = order.Currency,
                        order_id = order.OrderId,
                        dodo_payment_id = data.PaymentId,
                        status = "paid",
                        product_name = order.ProductName
                    };

                    var currentDetails = new List<object>();
                    if (!string.IsNullOrEmpty(existingUser.payment_details_json))
                    {
                        try
                        {
                            currentDetails = JsonSerializer.Deserialize<List<object>>(existingUser.payment_details_json) 
                                             ?? new List<object>();
                        }
                        catch
                        {
                            // If invalid JSON, start fresh but maybe log warning
                            _logger.LogWarning("⚠️ Invalid payment_details_json for user {UserId}, overwriting.", existingUser.user_id);
                        }
                    }

                    currentDetails.Add(paymentDetail);
                    existingUser.payment_details_json = JsonSerializer.Serialize(currentDetails);
                    existingUser.updated_at = DateTime.UtcNow;
                    
                    // Also extend license if needed (cumulative)
                    // existingUser.license_expiry_date = (existingUser.license_expiry_date ?? DateTime.UtcNow).AddYears(order.LicenseYears);
                    // _logger.LogInformation("Extended license for user {UserId}", existingUser.user_id);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to update payment_details_json for user {UserId}", existingUser.user_id);
                }
            }

            // Update order status
            order.Status = "paid";
            order.PaidAt = DateTime.UtcNow;
            order.UpdatedAt = DateTime.UtcNow;
            order.WebhookPayload = rawPayload;
            order.WebhookProcessedAt = DateTime.UtcNow;
            order.UserCreated = userCreated;

            // ✅ CRITICAL: Always update UserEmail from Dodo Customer to use checkout form email
            if (!string.IsNullOrEmpty(data.Customer?.Email))
            {
                order.UserEmail = data.Customer.Email;
                _logger.LogInformation("📧 Order email updated from Dodo checkout: {Email}", data.Customer.Email);
            }

            // ✅ Update DodoInvoiceId from webhook data (important for existing orders)
            if (!string.IsNullOrEmpty(data.InvoiceId))
            {
                order.DodoInvoiceId = data.InvoiceId;
                _logger.LogInformation("📄 Invoice ID captured from webhook: {InvoiceId}", data.InvoiceId);
            }

            if (data.Amount.HasValue)
            {
                order.AmountCents = data.Amount.Value;
            }
            if (!string.IsNullOrEmpty(data.Currency))
            {
                order.Currency = data.Currency;
            }

            // ✅ NEW: Save card payment details
            if (!string.IsNullOrEmpty(data.CardLastFour))
            {
                order.CardLastFour = data.CardLastFour;
            }
            if (!string.IsNullOrEmpty(data.CardNetwork))
            {
                order.CardNetwork = data.CardNetwork;
            }
            if (!string.IsNullOrEmpty(data.CardType))
            {
                order.CardType = data.CardType;
            }
            if (!string.IsNullOrEmpty(data.PaymentLink))
            {
                order.PaymentLink = data.PaymentLink;
            }
            if (!string.IsNullOrEmpty(data.PaymentMethod))
            {
                order.PaymentMethod = data.PaymentMethod;
            }

            // ✅ NEW: Save tax amount
            if (data.Tax.HasValue)
            {
                order.TaxAmountCents = data.Tax.Value;
            }

            // ✅ NEW: Save customer ID
            if (data.Customer != null && !string.IsNullOrEmpty(data.Customer.CustomerId))
            {
                order.DodoCustomerId = data.Customer.CustomerId;
            }

            // ✅ NEW: Save customer name and phone
            if (data.Customer != null)
            {
                if (!string.IsNullOrEmpty(data.Customer.Name))
                {
                    // Split name into first and last
                    var nameParts = data.Customer.Name.Split(' ', 2);
                    order.FirstName = nameParts[0];
                    order.LastName = nameParts.Length > 1 ? nameParts[1] : "";
                }
                if (!string.IsNullOrEmpty(data.Customer.PhoneNumber))
                {
                    order.PhoneNumber = data.Customer.PhoneNumber;
                }
            }

            // ✅ NEW: Save billing address from webhook
            if (data.Billing != null)
            {
                order.BillingAddress = data.Billing.Street;
                order.BillingCity = data.Billing.City;
                order.BillingState = data.Billing.State;
                order.BillingCountry = data.Billing.Country;
                order.BillingZip = data.Billing.Zipcode;
                _logger.LogInformation("📍 Billing address saved: {City}, {Country}", data.Billing.City, data.Billing.Country);
            }

            // Calculate license expiry
            order.LicenseExpiresAt = DateTime.UtcNow.AddYears(order.LicenseYears);

            // ✅ GENERATE LICENSE KEYS
            var licenseKeys = GenerateLicenseKeys(order.LicenseCount);
            order.LicenseKeys = JsonSerializer.Serialize(licenseKeys);

            _logger.LogInformation("🔑 Generated {Count} license keys for OrderId={OrderId}", 
                licenseKeys.Count, order.OrderId);

            // 🔍 LOG ORDER ENTITY BEFORE SAVE (PRODUCTION DEBUG)
            _logger.LogWarning("💾 SAVING ORDER TO DB - OrderId: {OrderId}, DodoPaymentId: {PaymentId}", 
                order.OrderId, order.DodoPaymentId);
            _logger.LogWarning("  Status: {Status}, PaidAt: {PaidAt}", order.Status, order.PaidAt);
            _logger.LogWarning("  AmountCents: {Amount}, Currency: {Currency}", order.AmountCents, order.Currency);
            _logger.LogWarning("  TaxAmountCents: {Tax}", order.TaxAmountCents);
            _logger.LogWarning("  DodoInvoiceId: {InvoiceId}", order.DodoInvoiceId ?? "NULL");
            _logger.LogWarning("  CardLastFour: {Card}, CardNetwork: {Network}, CardType: {Type}", 
                order.CardLastFour ?? "NULL", order.CardNetwork ?? "NULL", order.CardType ?? "NULL");
            _logger.LogWarning("  PaymentMethod: {Method}, PaymentLink: {Link}", 
                order.PaymentMethod ?? "NULL", order.PaymentLink ?? "NULL");
            _logger.LogWarning("  CustomerName: {First} {Last}", order.FirstName ?? "NULL", order.LastName ?? "NULL");
            _logger.LogWarning("  CustomerEmail: {Email}, Phone: {Phone}", order.UserEmail, order.PhoneNumber ?? "NULL");
            _logger.LogWarning("  DodoCustomerId: {CustomerId}", order.DodoCustomerId ?? "NULL");
            _logger.LogWarning("  BillingAddress: {Address}", order.BillingAddress ?? "NULL");
            _logger.LogWarning("  BillingCity: {City}, State: {State}, Country: {Country}, Zip: {Zip}", 
                order.BillingCity ?? "NULL", order.BillingState ?? "NULL", 
                order.BillingCountry ?? "NULL", order.BillingZip ?? "NULL");
            _logger.LogWarning("  ProductId: {ProductId}, ProductName: {ProductName}", 
                order.ProductId ?? "NULL", order.ProductName ?? "NULL");
            _logger.LogWarning("  LicenseCount: {Count}, LicenseYears: {Years}, ExpiresAt: {Expires}", 
                order.LicenseCount, order.LicenseYears, order.LicenseExpiresAt);

            // Save to database with enhanced error handling
            try
            {
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ DB SAVE SUCCESS - OrderId: {OrderId}", order.OrderId);

                // 🔍 POST-SAVE VERIFICATION (PRODUCTION DEBUG)
                var savedOrder = await _context.Orders.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);
                
                if (savedOrder == null)
                {
                    _logger.LogError("🔴 CRITICAL: Order {OrderId} saved but not found in DB on verification!", order.OrderId);
                }
                else
                {
                    _logger.LogInformation("✅ POST-SAVE VERIFICATION: Order {OrderId} confirmed in DB", order.OrderId);
                    
                    // Verify critical fields persisted correctly
                    if (savedOrder.CardLastFour != order.CardLastFour)
                    {
                        _logger.LogWarning("⚠️ DATA MISMATCH: CardLastFour - Expected '{Expected}' but got '{Actual}'", 
                            order.CardLastFour, savedOrder.CardLastFour);
                    }
                    if (savedOrder.TaxAmountCents != order.TaxAmountCents)
                    {
                        _logger.LogWarning("⚠️ DATA MISMATCH: TaxAmountCents - Expected {Expected} but got {Actual}", 
                            order.TaxAmountCents, savedOrder.TaxAmountCents);
                    }
                }
            }
            catch (DbUpdateException dbEx)
            {
                _logger.LogError(dbEx, "💥 DATABASE SAVE FAILED for OrderId {OrderId}: {Message}", 
                    order.OrderId, dbEx.InnerException?.Message ?? dbEx.Message);
                _logger.LogError("DB Error Details: {Details}", dbEx.ToString());
                throw; // Re-throw to ensure webhook returns error
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ UNEXPECTED ERROR during DB save for OrderId {OrderId}", order.OrderId);
                throw;
            }

            // ✅ GENERATE INVOICE DOWNLOAD URL (Using direct Dodo format)
            // Format: https://live.dodopayments.com/invoices/payments/{payment_id}
            string? invoiceUrl = null;
            
            if (!string.IsNullOrEmpty(data.PaymentId))
            {
                // Determine if sandbox or live mode
                var dodoBaseUrl = _isSandbox 
                    ? "https://test.dodopayments.com" 
                    : "https://live.dodopayments.com";
                    
                invoiceUrl = $"{dodoBaseUrl}/invoices/payments/{data.PaymentId}";
                _logger.LogInformation("📄 Invoice Download URL generated: {InvoiceUrl}", invoiceUrl);
            }
            else
            {
                _logger.LogWarning("⚠️ PaymentId is null, cannot generate invoice URL.");
            }

            // ✅ SEND ORDER EMAIL VIA HYBRID SYSTEM (MS Graph → SendGrid fallback)
            // Uses orchestrator with Excel attachment, replaces old email logic
            if (_emailOrchestrator != null && _excelExportService != null)
            {
                try
                {
                    var loginUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                        ?? _configuration["FrontendUrl"]
                        ?? "https://dsecuretech.com";
                    
                    _logger.LogInformation("📧 Sending order email via hybrid system to: {Email}", customerEmail);
                    
                    // Generate Excel with order details and license keys
                    var excelBytes = _excelExportService.GenerateOrderDetailsExcel(order, licenseKeys);
                    
                    // Build email request with minimal theme
                    var emailRequest = new Email.EmailSendRequest
                    {
                        ToEmail = customerEmail,
                        ToName = customerName,
                        Subject = userCreated 
                            ? $"Welcome to DSecure - Order #{order.OrderId}" 
                            : $"Order Confirmed - #{order.OrderId}",
                        HtmlBody = GenerateMinimalOrderEmailHtml(order, customerName, userCreated, tempPassword, loginUrl, licenseKeys),
                        Type = Email.EmailType.Transactional,
                        OrderId = order.OrderId,
                        Attachments = new List<Email.EmailAttachment>
                        {
                            new Email.EmailAttachment(
                                $"DSecure_Order_{order.OrderId}.xlsx",
                                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                                excelBytes
                            )
                        }
                    };
                    
                    var result = await _emailOrchestrator.SendEmailAsync(emailRequest);
                    
                    if (result.Success)
                    {
                        order.CredentialsEmailSent = true;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("✅ Order email sent via {Provider} to: {Email}", 
                            result.ProviderUsed, customerEmail);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Hybrid email failed, falling back to old system: {Message}", result.Message);
                        // Fallback to old email system if hybrid fails
                        await SendFallbackEmail(customerEmail, customerName, order, userCreated, tempPassword, licenseKeys, invoiceUrl);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Hybrid email error, using fallback for: {Email}", customerEmail);
                    // Fallback to old email system on exception
                    await SendFallbackEmail(customerEmail, customerName, order, userCreated, tempPassword, licenseKeys, invoiceUrl);
                }
            }
            else
            {
                // Fallback: use old email system if orchestrator not available
                await SendFallbackEmail(customerEmail, customerName, order, userCreated, tempPassword, licenseKeys, invoiceUrl);
            }

            _logger.LogInformation("✅ Dodo payment succeeded: OrderId={OrderId}, PaymentId={PaymentId}, UserCreated={UserCreated}",
                order.OrderId, data.PaymentId, userCreated);
        }

        /// <summary>
        /// Generate a cryptographically secure random password
        /// </summary>
        private static string GenerateSecurePassword(int length)
        {
            const string chars = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ23456789!@#$%";
            var password = new char[length];
            
            using var rng = RandomNumberGenerator.Create();
            var data = new byte[length];
            rng.GetBytes(data);
            
            for (int i = 0; i < length; i++)
            {
                password[i] = chars[data[i] % chars.Length];
            }
            
            return new string(password);
        }

        /// <summary>
        /// Fallback to old email system if hybrid fails
        /// </summary>
        private async Task SendFallbackEmail(string customerEmail, string customerName, Order order, bool userCreated, string? tempPassword, List<string>? licenseKeys, string? invoiceUrl)
        {
            try
            {
                var loginUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                    ?? _configuration["FrontendUrl"]
                    ?? "https://dsecuretech.com/login";

                if (userCreated && !string.IsNullOrEmpty(tempPassword))
                {
                    var emailSent = await _emailService.SendAccountCreatedEmailAsync(
                        customerEmail, customerName, tempPassword, loginUrl,
                        order.ProductName, order.LicenseCount,
                        order.AmountCents > 0 ? order.AmountCents / 100m : (decimal?)null,
                        licenseKeys, invoiceUrl);
                    
                    order.CredentialsEmailSent = emailSent;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("📧 Fallback credentials email sent to: {Email}", customerEmail);
                }
                else
                {
                    await _emailService.SendPaymentSuccessEmailAsync(
                        customerEmail, customerName, order.ProductName ?? "Product",
                        order.AmountCents > 0 ? order.AmountCents / 100m : 0m,
                        order.LicenseCount, licenseKeys, "Paid", invoiceUrl);
                    _logger.LogInformation("📧 Fallback payment email sent to: {Email}", customerEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Fallback email also failed for: {Email}", customerEmail);
            }
        }

        /// <summary>
        /// Generate minimal theme HTML email for order confirmation
        /// Clean, professional, no invoice button - details in Excel attachment
        /// </summary>
        private string GenerateMinimalOrderEmailHtml(Order order, string customerName, bool isNewUser, string? tempPassword, string loginUrl, List<string>? licenseKeys)
        {
            var amount = order.AmountCents / 100m;
            var tax = order.TaxAmountCents / 100m;
            var total = amount + tax;
            var currency = order.Currency ?? "USD";
            
            var credentialsHtml = "";
            if (isNewUser && !string.IsNullOrEmpty(tempPassword))
            {
                credentialsHtml = $@"
<tr><td style='padding:20px;background:#f8f9fa;border-radius:8px;margin:15px 0;'>
<p style='margin:0 0 10px 0;font-weight:600;color:#1a1a2e;'>🔐 Your Login Credentials</p>
<p style='margin:5px 0;'><strong>Email:</strong> {order.UserEmail}</p>
<p style='margin:5px 0;'><strong>Password:</strong> {tempPassword}</p>
<p style='margin:10px 0 0 0;font-size:12px;color:#666;'>Please change your password after first login.</p>
</td></tr>
<tr><td style='padding:15px 0;text-align:center;'>
<a href='{loginUrl}/login' style='display:inline-block;background:#1a1a2e;color:#fff;padding:12px 30px;text-decoration:none;border-radius:6px;font-weight:600;'>Login to Dashboard</a>
</td></tr>";
            }

            var licenseHtml = "";
            if (licenseKeys != null && licenseKeys.Count > 0)
            {
                licenseHtml = $@"
<tr><td style='padding:15px 0;'>
<p style='margin:0;color:#666;'>📋 <strong>{licenseKeys.Count} License Key(s)</strong> included in attached Excel file.</p>
</td></tr>";
            }

            return $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width,initial-scale=1'></head>
<body style='margin:0;padding:20px;background:#f5f5f5;font-family:Segoe UI,Arial,sans-serif;'>
<table width='100%' cellpadding='0' cellspacing='0' style='max-width:550px;margin:0 auto;background:#fff;border-radius:10px;overflow:hidden;'>
<tr><td style='background:#1a1a2e;color:#fff;padding:25px;text-align:center;'>
<h1 style='margin:0;font-size:22px;'>{(isNewUser ? "Welcome to DSecure!" : "Order Confirmed")}</h1>
<p style='margin:8px 0 0 0;opacity:0.9;'>Order #{order.OrderId}</p>
</td></tr>
<tr><td style='padding:25px;'>
<p style='margin:0 0 20px 0;'>Hi {customerName},</p>
<p style='margin:0 0 20px 0;color:#444;'>{(isNewUser ? "Thank you for your purchase! Your account is ready." : "Thank you for your order!")}</p>

<table width='100%' cellpadding='8' cellspacing='0' style='background:#f8f9fa;border-radius:8px;margin:15px 0;'>
<tr><td style='color:#666;'>Product</td><td style='text-align:right;font-weight:600;'>{order.ProductName ?? "DSecure"}</td></tr>
<tr><td style='color:#666;'>Licenses</td><td style='text-align:right;font-weight:600;'>{order.LicenseCount}</td></tr>
<tr><td style='color:#666;'>Duration</td><td style='text-align:right;font-weight:600;'>{order.LicenseYears} Year(s)</td></tr>
{(tax > 0 ? $"<tr><td style='color:#666;'>Tax</td><td style='text-align:right;'>{currency} {tax:N2}</td></tr>" : "")}
<tr style='border-top:1px solid #ddd;'><td style='color:#1a1a2e;font-weight:600;padding-top:12px;'>Total</td><td style='text-align:right;font-weight:700;font-size:18px;color:#1a1a2e;padding-top:12px;'>{currency} {total:N2}</td></tr>
</table>

{credentialsHtml}
{licenseHtml}

<tr><td style='padding:20px 0 0 0;border-top:1px solid #eee;'>
<p style='margin:0;font-size:13px;color:#666;'>📎 Complete order details and license keys are in the attached Excel file.</p>
</td></tr>
</td></tr>
<tr><td style='background:#f8f9fa;padding:20px;text-align:center;font-size:12px;color:#888;'>
<p style='margin:0;'>DSecure Technologies • support@dsecuretech.com</p>
</td></tr>
</table>
</body>
</html>";
        }

        /// <summary>
        /// Generate HTML email body for order confirmation (used by hybrid email system)
        /// </summary>
        private string GenerateOrderConfirmationHtml(Order order, string customerName, bool isNewUser, string? tempPassword, string? invoiceUrl, List<string>? licenseKeys)
        {
            var amount = order.AmountCents / 100m;
            var tax = order.TaxAmountCents / 100m;
            
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f5f5f5; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); color: white; padding: 30px; text-align: center; }}
        .header h1 {{ margin: 0; font-size: 24px; }}
        .content {{ padding: 30px; }}
        .order-box {{ background: #f8f9fa; border-radius: 8px; padding: 20px; margin: 20px 0; }}
        .row {{ display: flex; justify-content: space-between; padding: 10px 0; border-bottom: 1px solid #eee; }}
        .row:last-child {{ border-bottom: none; }}
        .label {{ color: #666; }}
        .value {{ font-weight: 600; color: #333; }}
        .total {{ font-size: 20px; color: #1a1a2e; }}
        .cta-button {{ display: inline-block; background: #4CAF50; color: white; padding: 15px 30px; text-decoration: none; border-radius: 8px; margin: 20px 0; }}
        .alert {{ background: #e3f2fd; border-left: 4px solid #2196F3; padding: 15px; margin: 20px 0; border-radius: 4px; }}
        .license-box {{ background: #f0f4c3; border-radius: 8px; padding: 15px; margin: 15px 0; }}
        .footer {{ background: #f5f5f5; padding: 20px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>🎉 {(isNewUser ? "Welcome to DSecure!" : "Payment Confirmed!")}</h1>
            <p>Order #{order.OrderId}</p>
        </div>
        <div class='content'>
            <p>Dear {customerName},</p>
            <p>{(isNewUser ? "Thank you for your purchase! Your account has been created and is ready to use." : "Thank you for your continued trust in DSecure!")}</p>
            
            <div class='order-box'>
                <h3 style='margin-top: 0;'>Order Summary</h3>
                <div class='row'><span class='label'>Product</span><span class='value'>{order.ProductName ?? "DSecure Product"}</span></div>
                <div class='row'><span class='label'>Licenses</span><span class='value'>{order.LicenseCount}</span></div>
                <div class='row'><span class='label'>Duration</span><span class='value'>{order.LicenseYears} Year(s)</span></div>
                <div class='row'><span class='label'>Subtotal</span><span class='value'>{order.Currency ?? "USD"} {amount:N2}</span></div>
                {(tax > 0 ? $"<div class='row'><span class='label'>Tax</span><span class='value'>{order.Currency ?? "USD"} {tax:N2}</span></div>" : "")}
                <div class='row'><span class='label'>Total</span><span class='value total'>{order.Currency ?? "USD"} {amount + tax:N2}</span></div>
            </div>";

            // Add credentials for new users
            if (isNewUser && !string.IsNullOrEmpty(tempPassword))
            {
                html += $@"
            <div class='alert'>
                <strong>🔐 Your Login Credentials</strong><br><br>
                <strong>Email:</strong> {order.UserEmail}<br>
                <strong>Password:</strong> {tempPassword}<br><br>
                <em>Please change your password after first login.</em>
            </div>
            <a href='https://dsecuretech.com/login' class='cta-button'>Login to Dashboard</a>";
            }

            // Add license keys summary
            if (licenseKeys != null && licenseKeys.Count > 0)
            {
                html += $@"
            <div class='license-box'>
                <strong>📋 License Keys ({licenseKeys.Count})</strong><br>
                <em>Complete license details are in the attached Excel file.</em>
            </div>";
            }

            // Add invoice link
            // if (!string.IsNullOrEmpty(invoiceUrl))
            // {
            //     html += $@"
            // <p><a href='{invoiceUrl}' style='color: #1a1a2e;'>📄 Download Invoice (PDF)</a></p>";
            // }

            html += @"
            <p style='margin-top: 30px;'><strong>📎 Attachment:</strong> Complete order details and license keys are in the attached Excel file.</p>
        </div>
        <div class='footer'>
            <p>DSecure Technologies • support@dsecuretech.com</p>
            <p>© 2026 DSecure. All rights reserved.</p>
        </div>
    </div>
</body>
</html>";

            return html;
        }

        private async Task HandlePaymentFailedAsync(DodoWebhookData? data, string rawPayload)
        {
            if (data?.PaymentId == null) return;

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.DodoPaymentId == data.PaymentId);

            if (order != null)
            {
                order.Status = "failed";
                order.UpdatedAt = DateTime.UtcNow;
                order.Notes = "Payment failed";
                order.WebhookPayload = rawPayload;
                await _context.SaveChangesAsync();

                _logger.LogWarning("❌ Dodo payment failed: OrderId={OrderId}", order.OrderId);

                // Send payment failure email to customer
                try
                {
                    var customerEmail = order.UserEmail ?? data.CustomerEmail;
                    var customerName = data.CustomerName ?? "Customer";
                    var productName = order.ProductName ?? "Product";
                    var amount = order.AmountCents > 0 ? order.AmountCents / 100m : (decimal?)null;

                    if (!string.IsNullOrEmpty(customerEmail))
                    {
                        var emailSent = await _emailService.SendPaymentFailedEmailAsync(
                            customerEmail,
                            customerName,
                            productName,
                            amount
                        );

                        _logger.LogInformation(emailSent
                            ? "✅ Payment failure email sent to {Email}"
                            : "⚠️ Failed to send payment failure email to {Email}",
                            customerEmail);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error sending payment failure email for OrderId={OrderId}", order.OrderId);
                }
            }
        }

        private async Task HandleSubscriptionEventAsync(DodoWebhookData? data, string rawPayload)
        {
            _logger.LogInformation("📅 Dodo subscription event received: {PayloadType}", data?.PayloadType);
            // Handle subscription creation/update logic here
            await Task.CompletedTask;
        }

        private async Task HandleSubscriptionCancelledAsync(DodoWebhookData? data, string rawPayload)
        {
            _logger.LogInformation("🚫 Dodo subscription cancelled");
            // Handle subscription cancellation logic here
            await Task.CompletedTask;
        }

        private async Task HandleRefundSucceededAsync(DodoWebhookData? data, string rawPayload)
        {
            if (data?.PaymentId == null) return;

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.DodoPaymentId == data.PaymentId);

            if (order != null)
            {
                order.Status = "refunded";
                order.UpdatedAt = DateTime.UtcNow;
                order.Notes = $"Refunded at {DateTime.UtcNow:u}";
                await _context.SaveChangesAsync();

                _logger.LogInformation("💸 Dodo refund succeeded: OrderId={OrderId}", order.OrderId);
            }
        }

        private async Task HandleLicenseKeyCreatedAsync(DodoWebhookData? data, string rawPayload)
        {
            _logger.LogInformation("🔑 Dodo license key created");
            // Handle license key creation logic here
            await Task.CompletedTask;
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        public async Task<OrderDto?> GetOrderAsync(int orderId, string userEmail)
        {
            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderId == orderId && o.UserEmail == userEmail && o.PaymentProvider == "dodo");

            if (order == null) return null;

            return MapToDto(order);
        }

        /// <summary>
        /// Get orders for a user (Dodo payments only)
        /// </summary>
        public async Task<OrderListResponse> GetOrdersAsync(string userEmail, int page = 1, int pageSize = 10)
        {
            var query = _context.Orders
                .Where(o => o.UserEmail == userEmail && o.PaymentProvider == "dodo")
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
        /// Verify webhook signature from Dodo Payments
        /// Per Dodo documentation:
        /// 1. Secret format: whsec_{base64_encoded_key}
        /// 2. Signed message: "{webhook-id}.{webhook-timestamp}.{payload}"
        /// 3. Signature header format: "v1,{base64_signature}" or "v1={base64_signature}"
        /// </summary>
        public bool VerifyWebhookSignature(string payload, string webhookId, string webhookSignature, string webhookTimestamp)
        {
            // Skip verification if no secret configured (development only)
            if (string.IsNullOrEmpty(_dodoWebhookSecret))
            {
                _logger.LogWarning("⚠️ Dodo webhook secret not configured - skipping verification (INSECURE!)");
                return true;
            }

            try
            {
                _logger.LogWarning("🔐 Verifying Dodo webhook signature...");
                _logger.LogWarning("   webhook-id: {Id}", webhookId);
                _logger.LogWarning("   webhook-timestamp: {Ts}", webhookTimestamp);
                _logger.LogWarning("   webhook-signature (first 30): {Sig}", webhookSignature?.Length > 30 ? webhookSignature.Substring(0, 30) : webhookSignature);
                _logger.LogWarning("   payload length: {Len}", payload?.Length);
                _logger.LogWarning("   Secret configured (first 15): {Sec}", _dodoWebhookSecret?.Length > 15 ? _dodoWebhookSecret.Substring(0, 15) : "[SHORT]");

                // Step 1: Extract the actual secret key
                // Dodo secrets are prefixed with "whsec_" followed by Base64 encoded key
                var secretKey = _dodoWebhookSecret;
                if (secretKey.StartsWith("whsec_"))
                {
                    secretKey = secretKey.Substring(6); // Remove "whsec_" prefix
                }

                // Step 2: Build the signed message: "webhook-id.webhook-timestamp.payload"
                var signedMessage = $"{webhookId}.{webhookTimestamp}.{payload}";
                
                // Extract actual signature from webhook-signature header
                // Format: "v1,{base64_signature}"
                string receivedSig = webhookSignature;
                if (webhookSignature.StartsWith("v1,"))
                {
                    receivedSig = webhookSignature.Substring(3);
                }
                
                _logger.LogWarning("   Received signature (cleaned, first 30): {Sig}", receivedSig?.Length > 30 ? receivedSig.Substring(0, 30) : receivedSig);

                // Step 3: Try BOTH secret formats - Base64 decoded AND raw UTF8
                // This handles different Dodo environments/configurations
                
                // Try 1: Base64 decoded secret (Standard Webhooks spec)
                try
                {
                    var secretBytes = Convert.FromBase64String(secretKey);
                    _logger.LogDebug("   Trying Base64 decoded secret: {Len} bytes", secretBytes.Length);
                    
                    using var hmac = new HMACSHA256(secretBytes);
                    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedMessage));
                    var expectedSignature = Convert.ToBase64String(hash);
                    
                    _logger.LogDebug("   Expected (Base64 secret): {Sig}", expectedSignature);
                    
                    if (CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(expectedSignature),
                        Encoding.UTF8.GetBytes(receivedSig)))
                    {
                        _logger.LogInformation("✅ Dodo webhook signature verified (Base64 secret)");
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug("   Base64 decode failed: {Msg}", ex.Message);
                }
                
                // Try 2: Raw UTF8 secret (fallback for some Dodo configurations)
                {
                    var secretBytes = Encoding.UTF8.GetBytes(secretKey);
                    _logger.LogDebug("   Trying UTF8 raw secret: {Len} bytes", secretBytes.Length);
                    
                    using var hmac = new HMACSHA256(secretBytes);
                    var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(signedMessage));
                    var expectedSignature = Convert.ToBase64String(hash);
                    
                    _logger.LogDebug("   Expected (UTF8 secret): {Sig}", expectedSignature);
                    
                    if (CryptographicOperations.FixedTimeEquals(
                        Encoding.UTF8.GetBytes(expectedSignature),
                        Encoding.UTF8.GetBytes(receivedSig)))
                    {
                        _logger.LogInformation("✅ Dodo webhook signature verified (UTF8 secret)");
                        return true;
                    }
                }

                // If we reach here, neither Base64 nor UTF8 secret matched
                _logger.LogWarning("❌ Dodo webhook signature mismatch.");
                _logger.LogWarning("   Received: {Received}", receivedSig);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying Dodo webhook signature");
                return false;
            }
        }

        /// <summary>
        /// Get available products from Dodo
        /// </summary>
        public async Task<List<DodoBillingPlanDto>> GetProductsAsync()
        {
            // Check cache first
            if (_cache.TryGetValue(CACHE_KEY_DODO_PRODUCTS, out List<DodoBillingPlanDto>? cachedProducts) && cachedProducts != null)
            {
                _logger.LogDebug("📦 Returning cached Dodo products ({Count} products)", cachedProducts.Count);
                return cachedProducts;
            }

            _logger.LogInformation("🔄 Fetching products from Dodo API...");
            _logger.LogInformation("📡 API URL: {Url}", $"{_dodoBaseUrl}/products");

            try
            {
                var response = await _httpClient.GetAsync($"{_dodoBaseUrl}/products");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("📥 Dodo Products Response: StatusCode={StatusCode}, ContentLength={Length}",
                    response.StatusCode, responseContent.Length);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Dodo products API error: {StatusCode}", response.StatusCode);
                    _logger.LogError("❌ Response: {Response}", responseContent);
                    return GetFallbackProducts();
                }

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
                };

                var productList = JsonSerializer.Deserialize<DodoProductListResponse>(responseContent, jsonOptions);

                if (productList?.Items == null || !productList.Items.Any())
                {
                    _logger.LogWarning("⚠️ No products returned from Dodo API");
                    return GetFallbackProducts();
                }

                var products = productList.Items.Select((p, index) => new DodoBillingPlanDto
                {
                    ProductId = p.ProductId ?? "",
                    Name = p.Name ?? "Unknown Plan",
                    Description = p.Description,
                    Price = (p.Price ?? 0) / 100m, // Convert cents to dollars
                    Currency = p.Currency ?? "USD",
                    IsRecurring = p.IsRecurring,
                    RecurringInterval = p.RecurringInterval,
                    DisplayOrder = index
                }).ToList();

                // Cache for 1 hour
                _cache.Set(CACHE_KEY_DODO_PRODUCTS, products, CACHE_DURATION);

                _logger.LogInformation("✅ Cached {Count} Dodo products for 1 hour", products.Count);

                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching products from Dodo API");
                return GetFallbackProducts();
            }
        }

        /// <summary>
        /// Force refresh the product catalog cache
        /// </summary>
        public async Task RefreshProductCacheAsync()
        {
            _logger.LogInformation("🔄 Force refreshing Dodo product cache...");
            _cache.Remove(CACHE_KEY_DODO_PRODUCTS);
            await GetProductsAsync();
        }

        /// <summary>
        /// Create a new product in Dodo Payments
        /// POST /products
        /// </summary>
        public async Task<DodoProductResponse> CreateProductAsync(DodoCreateProductRequest request)
        {
            _logger.LogInformation("📦 Creating Dodo product: {Name}, Price: {Price} {Currency}",
                request.Name, request.Price, request.Currency);

            try
            {
                // Construct price object (Internally Tagged Enum)
                var priceObject = new Dictionary<string, object>
                {
                    ["price"] = request.Price, // Field name MUST be 'price'
                    ["currency"] = request.Currency.ToUpperInvariant(),
                    ["type"] = request.IsRecurring ? "recurring" : "one_time_price",
                    ["discount"] = 0, // Required field
                    ["purchasing_power_parity"] = false, // Required field based on error message
                    ["tax_inclusive"] = true // User requested tax inclusive pricing
                };

                // Add recurring details to price object if needed
                if (request.IsRecurring && !string.IsNullOrEmpty(request.RecurringInterval))
                {
                    priceObject["interval"] = request.RecurringInterval;
                }

                var dodoRequest = new Dictionary<string, object>
                {
                    ["name"] = request.Name,
                    ["price"] = priceObject,
                    ["tax_category"] = "digital_products"
                };

                // Add optional fields
                if (!string.IsNullOrEmpty(request.Description))
                    dodoRequest["description"] = request.Description;

                if (!string.IsNullOrEmpty(request.ImageUrl))
                    dodoRequest["image"] = request.ImageUrl;

                if (request.Metadata != null && request.Metadata.Count > 0)
                    dodoRequest["metadata"] = request.Metadata;

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(dodoRequest),
                    Encoding.UTF8,
                    "application/json"
                );

                _logger.LogInformation("📤 Dodo create product payload: {Payload}", JsonSerializer.Serialize(dodoRequest));
                _logger.LogInformation("📡 Posting to: {Url}", $"{_dodoBaseUrl}/products");

                var response = await _httpClient.PostAsync($"{_dodoBaseUrl}/products", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("📥 Dodo Create Product Response: {StatusCode} - {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Dodo create product failed: {StatusCode} - {Response}",
                        response.StatusCode, responseContent);

                    return new DodoProductResponse
                    {
                        Success = false,
                        Message = $"Failed to create product: {responseContent}"
                    };
                }

                var dodoResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Parse response
                var productId = dodoResponse.TryGetProperty("product_id", out var pidProp)
                    ? pidProp.GetString()
                    : dodoResponse.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;

                // Clear product cache to include new product
                _cache.Remove(CACHE_KEY_DODO_PRODUCTS);

                _logger.LogInformation("✅ Dodo product created: ProductId={ProductId}, Name={Name}",
                    productId, request.Name);

                return new DodoProductResponse
                {
                    Success = true,
                    Message = "Product created successfully",
                    ProductId = productId,
                    Name = request.Name,
                    Description = request.Description,
                    Price = request.Price,
                    Currency = request.Currency,
                    IsRecurring = request.IsRecurring,
                    RecurringInterval = request.RecurringInterval,
                    CreatedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating Dodo product: {Name}", request.Name);
                return new DodoProductResponse
                {
                    Success = false,
                    Message = $"Error creating product: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Create or update a customer in Dodo Payments
        /// POST /customers
        /// </summary>
        public async Task<DodoCustomerResponse> CreateOrUpdateCustomerAsync(string email, string name, string? phone = null)
        {
            try
            {
                _logger.LogInformation("👤 Creating/updating Dodo customer: {Email}", email);

                var requestBody = new
                {
                    email,
                    name,
                    phone = phone ?? ""
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync($"{_dodoBaseUrl}/customers", jsonContent);
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("📥 Dodo Customer API Response: {Status} - {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Dodo customer creation failed: {Status} - {Response}",
                        response.StatusCode, responseContent);

                    return new DodoCustomerResponse
                    {
                        Success = false,
                        Message = $"Failed to create customer: {responseContent}"
                    };
                }

                var dodoResponse = JsonSerializer.Deserialize<JsonElement>(responseContent);

                // Extract customer ID from response
                var customerId = dodoResponse.TryGetProperty("id", out var idProp)
                    ? idProp.GetString()
                    : dodoResponse.TryGetProperty("customer_id", out var custIdProp)
                        ? custIdProp.GetString()
                        : null;

                _logger.LogInformation("✅ Dodo customer created: {CustomerId} for {Email}",
                    customerId, email);

                return new DodoCustomerResponse
                {
                    Success = true,
                    Message = "Customer synced successfully",
                    CustomerId = customerId,
                    Email = email,
                    Name = name
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating Dodo customer for {Email}", email);
                return new DodoCustomerResponse
                {
                    Success = false,
                    Message = $"Error creating customer: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get invoice details from Dodo Payments API
        /// GET /invoices/{invoice_id}
        /// </summary>
        public async Task<DodoInvoiceResponse> GetInvoiceAsync(string invoiceId)
        {
            try
            {
                _logger.LogInformation("📄 Fetching invoice: {InvoiceId}", invoiceId);

                var response = await _httpClient.GetAsync($"{_dodoBaseUrl}/invoices/{invoiceId}");
                var responseContent = await response.Content.ReadAsStringAsync();

                _logger.LogInformation("📥 Dodo Invoice API Response: {Status} - {Content}",
                    response.StatusCode, responseContent);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("❌ Dodo invoice fetch failed: {Status} - {Response}",
                        response.StatusCode, responseContent);

                    return new DodoInvoiceResponse
                    {
                        Success = false,
                        Message = $"Failed to fetch invoice: {responseContent}"
                    };
                }

                var invoice = JsonSerializer.Deserialize<DodoInvoice>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                _logger.LogInformation("✅ Invoice fetched: {InvoiceId}, Amount: {Amount}",
                    invoice?.InvoiceId, invoice?.TotalAmountDecimal);

                return new DodoInvoiceResponse
                {
                    Success = true,
                    Message = "Invoice fetched successfully",
                    Invoice = invoice
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching invoice {InvoiceId}", invoiceId);
                return new DodoInvoiceResponse
                {
                    Success = false,
                    Message = $"Error fetching invoice: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get invoice by Payment ID from Dodo Payments API
        /// GET /invoices/payments/{payment_id}
        /// This is more reliable than invoice_id as payment_id is always available
        /// Includes retry logic for timing issues (invoice may not be immediately available)
        /// </summary>
        public async Task<DodoInvoiceResponse> GetInvoiceByPaymentIdAsync(string paymentId)
        {
            const int maxRetries = 3;
            int[] delaysMs = { 0, 2000, 4000 }; // First attempt immediately, then 2s, 4s delays
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    if (delaysMs[attempt] > 0)
                    {
                        _logger.LogInformation("⏳ Waiting {Delay}ms before retry attempt {Attempt}/{Max}...", 
                            delaysMs[attempt], attempt + 1, maxRetries);
                        await Task.Delay(delaysMs[attempt]);
                    }
                    
                    _logger.LogInformation("📄 Fetching invoice by PaymentId: {PaymentId} (Attempt {Attempt}/{Max})", 
                        paymentId, attempt + 1, maxRetries);

                    var requestUrl = $"{_dodoBaseUrl}/invoices/payments/{paymentId}";
                    _logger.LogInformation("🌐 Request URL: {Url}", requestUrl);
                    
                    var response = await _httpClient.GetAsync(requestUrl);
                    var responseContent = await response.Content.ReadAsStringAsync();

                    _logger.LogInformation("📥 Dodo Invoice API Response: {Status} - {Content}",
                        response.StatusCode, responseContent.Length > 200 ? responseContent.Substring(0, 200) + "..." : responseContent);

                    if (response.IsSuccessStatusCode)
                    {
                        // ✅ Check if response is actually JSON before parsing
                        if (string.IsNullOrEmpty(responseContent) || 
                            responseContent.TrimStart().StartsWith('<') || 
                            responseContent.TrimStart().StartsWith('%'))
                        {
                            _logger.LogError("❌ Dodo API returned non-JSON response: {Content}", 
                                responseContent.Length > 100 ? responseContent.Substring(0, 100) : responseContent);
                            return new DodoInvoiceResponse
                            {
                                Success = false,
                                Message = "Dodo API returned non-JSON response (possibly HTML error page)"
                            };
                        }

                        var invoice = JsonSerializer.Deserialize<DodoInvoice>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        _logger.LogInformation("✅ Invoice fetched successfully: InvoiceId={InvoiceId}, PdfUrl={PdfUrl}",
                            invoice?.InvoiceId, invoice?.PdfUrl ?? "NULL");

                        return new DodoInvoiceResponse
                        {
                            Success = true,
                            Message = "Invoice fetched successfully",
                            Invoice = invoice
                        };
                    }
                    
                    // Check if it's a NOT_FOUND error (retry-able)
                    if (response.StatusCode == System.Net.HttpStatusCode.NotFound && attempt < maxRetries - 1)
                    {
                        _logger.LogWarning("⚠️ Invoice not found (attempt {Attempt}), will retry - Dodo may still be generating...", attempt + 1);
                        continue; // Retry
                    }
                    
                    // Non-retryable error or last attempt
                    _logger.LogError("❌ Invoice fetch failed after {Attempt} attempts: {Status} - {Response}",
                        attempt + 1, response.StatusCode, responseContent);

                    return new DodoInvoiceResponse
                    {
                        Success = false,
                        Message = $"Failed to fetch invoice: {responseContent}"
                    };
                }
                catch (Exception ex)
                {
                    if (attempt < maxRetries - 1)
                    {
                        _logger.LogWarning(ex, "⚠️ Exception on attempt {Attempt}, will retry...", attempt + 1);
                        continue;
                    }
                    
                    _logger.LogError(ex, "❌ Error fetching invoice by PaymentId {PaymentId} after all retries", paymentId);
                    return new DodoInvoiceResponse
                    {
                        Success = false,
                        Message = $"Error fetching invoice: {ex.Message}"
                    };
                }
            }
            
            // Should not reach here, but safety fallback
            return new DodoInvoiceResponse
            {
                Success = false,
                Message = "Invoice fetch failed after all retry attempts"
            };
        }

        /// <summary>
        /// Create a new webhook endpoint in Dodo
        /// </summary>
        public async Task<DodoWebhookResponse> CreateWebhookAsync(string url, List<string> events)
        {
            try
            {
                var payload = new DodoWebhookRequest
                {
                    Url = url,
                    Events = events,
                    Description = "BitRaser API Webhook"
                };

                var jsonOptions = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                
                var content = new StringContent(JsonSerializer.Serialize(payload, jsonOptions), Encoding.UTF8, "application/json");
                _logger.LogInformation("Creating Dodo webhook: {Url} for events {Events}", url, string.Join(",", events));

                var response = await _httpClient.PostAsync($"{_dodoBaseUrl}/webhooks", content);
                var responseStr = await response.Content.ReadAsStringAsync();

                // ✅ Log raw response for debugging
                _logger.LogInformation("📥 Dodo API Raw Response: {Response}", responseStr);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to create webhook. Status: {Status}, Content: {Content}", response.StatusCode, responseStr);
                    throw new HttpRequestException($"Failed to create webhook: {responseStr}");
                }

                _logger.LogInformation("✅ Webhook created successfully");
                
                // ✅ Use proper deserialization options
                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                
                var result = JsonSerializer.Deserialize<DodoWebhookResponse>(responseStr, deserializeOptions);
                
                // ✅ Log parsed result for debugging
                _logger.LogInformation("📥 Parsed Webhook Response: WebhookId={Id}, Url={Url}, Status={Status}, Events={Events}", 
                    result?.WebhookId, result?.Url, result?.Status, result?.Events?.Count);
                
                return result ?? new DodoWebhookResponse { Url = url, CreatedAt = DateTime.UtcNow };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating Dodo webhook");
                throw;
            }
        }

        /// <summary>
        /// Get all configured webhooks from Dodo
        /// </summary>
        public async Task<List<DodoWebhookResponse>> GetWebhooksAsync()
        {
            try
            {
                _logger.LogInformation("Fetching Dodo webhooks list...");
                var response = await _httpClient.GetAsync($"{_dodoBaseUrl}/webhooks");
                var responseStr = await response.Content.ReadAsStringAsync();

                // ✅ Log raw response for debugging
                _logger.LogInformation("📥 Dodo Webhooks Raw Response: {Response}", responseStr);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Failed to list webhooks. Status: {Status}, Content: {Content}", response.StatusCode, responseStr);
                    return new List<DodoWebhookResponse>();
                }

                var deserializeOptions = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Dodo may return a list or a wrapped list. Try both formats.
                try
                {
                    // Try direct list first
                    var list = JsonSerializer.Deserialize<List<DodoWebhookResponse>>(responseStr, deserializeOptions);
                    if (list != null && list.Count > 0)
                    {
                        _logger.LogInformation("📥 Parsed {Count} webhooks from Dodo", list.Count);
                        return list;
                    }
                }
                catch
                {
                    // Ignore and try wrapper format
                }

                // Try wrapped format { "webhooks": [...] } or { "items": [...] }
                try
                {
                    using var doc = JsonDocument.Parse(responseStr);
                    JsonElement? items = null;
                    
                    if (doc.RootElement.TryGetProperty("webhooks", out var webhooksElement))
                        items = webhooksElement;
                    else if (doc.RootElement.TryGetProperty("items", out var itemsElement))
                        items = itemsElement;
                    else if (doc.RootElement.TryGetProperty("data", out var dataElement))
                        items = dataElement;
                    
                    if (items.HasValue)
                    {
                        var list = JsonSerializer.Deserialize<List<DodoWebhookResponse>>(items.Value.GetRawText(), deserializeOptions);
                        if (list != null)
                        {
                            _logger.LogInformation("📥 Parsed {Count} webhooks from wrapped response", list.Count);
                            return list;
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not parse wrapped webhook response");
                }

                _logger.LogWarning("⚠️ Could not parse webhook list, returning empty");
                return new List<DodoWebhookResponse>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting Dodo webhooks");
                return new List<DodoWebhookResponse>();
            }
        }

        /// <summary>
        /// Delete a webhook endpoint
        /// </summary>
        public async Task<bool> DeleteWebhookAsync(string webhookId)
        {
            try
            {
                _logger.LogInformation("Deleting Dodo webhook: {WebhookId}", webhookId);
                var response = await _httpClient.DeleteAsync($"{_dodoBaseUrl}/webhooks/{webhookId}");

                if (response.IsSuccessStatusCode)
                {
                   _logger.LogInformation("✅ Webhook {WebhookId} deleted successfully", webhookId);
                   return true;
                }

                var error = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to delete webhook {Id}. Status: {Status}, Content: {Content}", webhookId, response.StatusCode, error);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting Dodo webhook");
                return false;
            }
        }

        private List<DodoBillingPlanDto> GetFallbackProducts()
        {
            _logger.LogWarning("⚠️ Dodo API failed - returning empty product list (no fallback products)");
            return new List<DodoBillingPlanDto>();
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
                PolarOrderId = order.DodoPaymentId, // Reuse field for now
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

        /// <summary>
        /// Generate multiple license keys based on quantity
        /// </summary>
        private List<string> GenerateLicenseKeys(int count, string prefix = "BITR")
        {
            var keys = new List<string>();
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            string GenerateSegment() => new string(Enumerable.Range(0, 4)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());

            for (int i = 0; i < count; i++)
            {
                string key;
                int attempts = 0;
                do
                {
                    key = $"{prefix}-{GenerateSegment()}-{GenerateSegment()}-{GenerateSegment()}";
                    attempts++;
                } while (keys.Contains(key) && attempts < 10); // Ensure uniqueness

                keys.Add(key);
            }

            return keys;
        }
    }
}
