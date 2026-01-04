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
            IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _httpClient = httpClientFactory.CreateClient("DodoApi");
            _cache = cache;
            _emailService = emailService;

            // Load configuration - Environment variables take priority
            _dodoApiKey = Environment.GetEnvironmentVariable("Dodo__ApiKey")
                ?? configuration["Dodo:ApiKey"]
                ?? throw new InvalidOperationException("Dodo API key not configured. Set Dodo__ApiKey environment variable.");

            _dodoWebhookSecret = Environment.GetEnvironmentVariable("Dodo__WebhookSecret")
                ?? configuration["Dodo:WebhookSecret"]
                ?? "";

            _isSandbox = bool.TryParse(Environment.GetEnvironmentVariable("Dodo__Sandbox"), out var sandbox)
                ? sandbox
                : configuration.GetValue<bool>("Dodo:Sandbox", true);

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
            _logger.LogWarning("🦤 Dodo Config Loaded:");
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
                    }
                };

                // Add success URL if provided
                if (!string.IsNullOrEmpty(request.SuccessUrl))
                {
                    dodoRequest["success_url"] = request.SuccessUrl;
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
                    ProductName = "Dodo Purchase"
                };
                
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

            if (data.Amount.HasValue)
            {
                order.AmountCents = data.Amount.Value;
            }
            if (!string.IsNullOrEmpty(data.Currency))
            {
                order.Currency = data.Currency;
            }

            // Calculate license expiry
            order.LicenseExpiresAt = DateTime.UtcNow.AddYears(order.LicenseYears);

            // ✅ GENERATE LICENSE KEYS
            var licenseKeys = GenerateLicenseKeys(order.LicenseCount);
            order.LicenseKeys = JsonSerializer.Serialize(licenseKeys);

            _logger.LogInformation("🔑 Generated {Count} license keys for OrderId={OrderId}", 
                licenseKeys.Count, order.OrderId);

            await _context.SaveChangesAsync();

            _logger.LogInformation("✅ Order updated: OrderId={OrderId}, Status={Status}",
                order.OrderId, order.Status);

            // ✅ SEND CREDENTIALS EMAIL (only for new users)
            if (userCreated && !string.IsNullOrEmpty(tempPassword))
            {
                try
                {
                    var loginUrl = Environment.GetEnvironmentVariable("FRONTEND_URL")
                        ?? _configuration["FrontendUrl"]
                        ?? "https://dashboard.dsecure.com/login";

                    // ⚠️ SECURITY: tempPassword is ONLY passed to email, NEVER logged
                    var emailSent = await _emailService.SendAccountCreatedEmailAsync(
                        customerEmail,
                        customerName,
                        tempPassword,
                        loginUrl,
                        order.ProductName,
                        order.LicenseCount,
                        order.AmountCents > 0 ? order.AmountCents / 100m : (decimal?)null,
                        licenseKeys
                    );

                    order.CredentialsEmailSent = emailSent;
                    await _context.SaveChangesAsync();

                    if (emailSent)
                    {
                        _logger.LogInformation("📧 Credentials email sent to: {Email}", customerEmail);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Failed to send credentials email to: {Email}", customerEmail);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error sending credentials email to: {Email}", customerEmail);
                }
            }
            else
            {
                // ✅ SEND PAYMENT SUCCESS EMAIL (for existing users)
                try
                {
                    var emailSent = await _emailService.SendPaymentSuccessEmailAsync(
                        customerEmail,
                        customerName,
                        order.ProductName ?? "Product",
                        order.AmountCents > 0 ? order.AmountCents / 100m : 0m,
                        order.LicenseCount,
                        licenseKeys
                    );

                    if (emailSent)
                    {
                        _logger.LogInformation("📧 Payment success email sent to: {Email}", customerEmail);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Failed to send payment success email to: {Email}", customerEmail);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error sending payment success email to: {Email}", customerEmail);
                }
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
                _logger.LogInformation("🔐 Verifying Dodo webhook signature...");
                _logger.LogDebug("   webhook-id: {Id}", webhookId);
                _logger.LogDebug("   webhook-timestamp: {Ts}", webhookTimestamp);
                _logger.LogDebug("   webhook-signature: {Sig}", webhookSignature);
                _logger.LogDebug("   payload length: {Len}", payload?.Length);

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
                
                _logger.LogDebug("   Received signature (cleaned): {Sig}", receivedSig);

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
