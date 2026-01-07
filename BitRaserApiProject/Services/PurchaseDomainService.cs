using DSecureApi.Data;
using DSecureApi.Models;
using DSecureApi.Models.DTOs;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DSecureApi.Services
{
    #region DTOs

    /// <summary>
    /// Request to create a purchase order
    /// </summary>
    public class CreatePurchaseOrderRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public int LicenseQuantity { get; set; } = 1;
        public int LicenseDurationDays { get; set; } = 365; // Default 1 year
        public string? RedirectUrl { get; set; }
        public int? Amount { get; set; } // In cents, optional - will fetch from product
        public string Currency { get; set; } = "USD";
    }

    /// <summary>
    /// Response after creating purchase order
    /// </summary>
    public class CreatePurchaseOrderResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid? OrderId { get; set; }
        public string? PaymentUrl { get; set; }
        public string? RedirectUrl { get; set; }
        public string? Status { get; set; }
        public int? Amount { get; set; }
        public string? Currency { get; set; }
    }

    /// <summary>
    /// Response for user's licenses
    /// </summary>
    public class UserLicenseDto
    {
        public Guid LicenseId { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public string ProductId { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int DaysRemaining { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsActivated { get; set; }
    }

    #endregion

    /// <summary>
    /// Interface for Purchase Domain Service
    /// </summary>
    public interface IPurchaseDomainService
    {
        Task<CreatePurchaseOrderResponse> CreateOrderAsync(CreatePurchaseOrderRequest request, int userId, string? userEmail);
        Task<PurchaseOrder?> GetOrderAsync(Guid orderId);
        Task<PurchaseOrder?> GetOrderByIdempotencyKeyAsync(string idempotencyKey);
        Task<bool> ProcessPaymentSuccessAsync(Guid orderId, string gatewayPaymentId, int amount, string currency, string? rawPayload);
        Task<bool> ProcessPaymentFailureAsync(Guid orderId, string reason, string? rawPayload);
        Task<List<UserLicenseDto>> GetUserLicensesAsync(int userId);
        Task<List<PurchasedLicense>> GenerateLicensesAsync(PurchaseOrder order);
        bool ValidateRedirectUrl(string url);
        string GenerateIdempotencyKey(int userId, string productId);
    }

    /// <summary>
    /// Purchase Domain Service
    /// Handles order creation, payment processing, and license generation
    /// </summary>
    public class PurchaseDomainService : IPurchaseDomainService
    {
        private readonly ApplicationDbContext _context;
        private readonly IDodoPaymentService _dodoPaymentService;
        private readonly ILicenseKeyGenerator _keyGenerator;
        private readonly ILogger<PurchaseDomainService> _logger;
        private readonly IConfiguration _configuration;

        // Allowed redirect URL domains
        private readonly HashSet<string> _allowedRedirectDomains;

        public PurchaseDomainService(
            ApplicationDbContext context,
            IDodoPaymentService dodoPaymentService,
            ILicenseKeyGenerator keyGenerator,
            ILogger<PurchaseDomainService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _dodoPaymentService = dodoPaymentService;
            _keyGenerator = keyGenerator;
            _logger = logger;
            _configuration = configuration;

            // Load allowed redirect domains from config
            _allowedRedirectDomains = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "dsecuretech.com",
                "dsecure.com",
                "localhost",
                "127.0.0.1"
            };

            var configDomains = configuration.GetSection("AllowedRedirectDomains").Get<string[]>();
            if (configDomains != null)
            {
                foreach (var domain in configDomains)
                {
                    _allowedRedirectDomains.Add(domain);
                }
            }
        }

        /// <summary>
        /// Create a new purchase order
        /// </summary>
        public async Task<CreatePurchaseOrderResponse> CreateOrderAsync(
            CreatePurchaseOrderRequest request, 
            int userId, 
            string? userEmail)
        {
            try
            {
                _logger.LogInformation("📦 Creating order for user {UserId}, product {ProductId}", 
                    userId, request.ProductId);

                // Validate redirect URL
                if (!string.IsNullOrEmpty(request.RedirectUrl) && !ValidateRedirectUrl(request.RedirectUrl))
                {
                    return new CreatePurchaseOrderResponse
                    {
                        Success = false,
                        Message = "Invalid redirect URL domain"
                    };
                }

                // Generate idempotency key
                var idempotencyKey = GenerateIdempotencyKey(userId, request.ProductId);

                // Check for existing pending order with same idempotency key
                var existingOrder = await GetOrderByIdempotencyKeyAsync(idempotencyKey);
                if (existingOrder != null && existingOrder.Status == "PendingPayment")
                {
                    _logger.LogInformation("📦 Returning existing pending order {OrderId}", existingOrder.Id);
                    return new CreatePurchaseOrderResponse
                    {
                        Success = true,
                        Message = "Existing order found",
                        OrderId = existingOrder.Id,
                        PaymentUrl = existingOrder.PaymentUrl,
                        RedirectUrl = existingOrder.RedirectUrl,
                        Status = existingOrder.Status,
                        Amount = existingOrder.Amount,
                        Currency = existingOrder.Currency
                    };
                }

                // Create new order
                var order = new PurchaseOrder
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    UserEmail = userEmail,
                    ProductId = request.ProductId,
                    ProductName = request.ProductName,
                    LicenseQuantity = request.LicenseQuantity > 0 ? request.LicenseQuantity : 1,
                    LicenseDurationDays = request.LicenseDurationDays > 0 ? request.LicenseDurationDays : 365,
                    IsRecurring = false,
                    Amount = request.Amount ?? 0,
                    Currency = request.Currency ?? "USD",
                    Status = "PendingPayment",
                    RedirectUrl = request.RedirectUrl,
                    RedirectUrlHash = HashString(request.RedirectUrl ?? ""),
                    IdempotencyKey = idempotencyKey,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.Set<PurchaseOrder>().Add(order);
                await _context.SaveChangesAsync();

                // Create checkout with Dodo
                var checkoutResponse = await _dodoPaymentService.CreateCheckoutAsync(
                    new DodoCheckoutRequest
                    {
                        ProductId = request.ProductId,
                        SuccessUrl = request.RedirectUrl ?? $"{_configuration["App:BaseUrl"]}/payment/success",
                        CustomerEmail = userEmail,
                        Metadata = new Dictionary<string, string>
                        {
                            { "order_id", order.Id.ToString() },
                            { "user_id", userId.ToString() },
                            { "license_quantity", request.LicenseQuantity.ToString() },
                            { "license_duration_days", request.LicenseDurationDays.ToString() }
                        }
                    },
                    userEmail ?? "guest@dsecure.com" // userEmail parameter required
                );

                if (checkoutResponse.Success && !string.IsNullOrEmpty(checkoutResponse.CheckoutUrl))
                {
                    // Update order with payment URL
                    order.PaymentUrl = checkoutResponse.CheckoutUrl;
                    order.GatewayOrderId = checkoutResponse.PaymentId;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("✅ Order created: {OrderId}, PaymentUrl: {Url}", 
                        order.Id, order.PaymentUrl);

                    return new CreatePurchaseOrderResponse
                    {
                        Success = true,
                        Message = "Order created successfully",
                        OrderId = order.Id,
                        PaymentUrl = order.PaymentUrl,
                        RedirectUrl = order.RedirectUrl,
                        Status = order.Status,
                        Amount = order.Amount,
                        Currency = order.Currency
                    };
                }
                else
                {
                    // Mark order as failed
                    order.Status = "Failed";
                    await _context.SaveChangesAsync();

                    return new CreatePurchaseOrderResponse
                    {
                        Success = false,
                        Message = checkoutResponse.Message ?? "Failed to create payment checkout"
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating order");
                return new CreatePurchaseOrderResponse
                {
                    Success = false,
                    Message = $"Error creating order: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Get order by ID
        /// </summary>
        public async Task<PurchaseOrder?> GetOrderAsync(Guid orderId)
        {
            return await _context.Set<PurchaseOrder>()
                .Include(o => o.Licenses)
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        /// <summary>
        /// Get order by idempotency key
        /// </summary>
        public async Task<PurchaseOrder?> GetOrderByIdempotencyKeyAsync(string idempotencyKey)
        {
            return await _context.Set<PurchaseOrder>()
                .FirstOrDefaultAsync(o => o.IdempotencyKey == idempotencyKey);
        }

        /// <summary>
        /// Process successful payment - generate licenses
        /// </summary>
        public async Task<bool> ProcessPaymentSuccessAsync(
            Guid orderId, 
            string gatewayPaymentId, 
            int amount, 
            string currency,
            string? rawPayload)
        {
            using var transaction = await _context.Database.BeginTransactionAsync(
                System.Data.IsolationLevel.RepeatableRead);

            try
            {
                _logger.LogInformation("💳 Processing payment success for order {OrderId}", orderId);

                // Get and lock order
                var order = await _context.Set<PurchaseOrder>()
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null)
                {
                    _logger.LogWarning("❌ Order not found: {OrderId}", orderId);
                    return false;
                }

                // Idempotency check - already completed
                if (order.Status == "Completed")
                {
                    _logger.LogInformation("⏭️ Order already completed (idempotent): {OrderId}", orderId);
                    return true;
                }

                // Validate order is pending
                if (order.Status != "PendingPayment" && order.Status != "Processing")
                {
                    _logger.LogWarning("❌ Invalid order status for payment: {Status}", order.Status);
                    return false;
                }

                // Record payment
                var paymentRecord = new PaymentRecord
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    GatewayReference = gatewayPaymentId,
                    GatewayType = "dodo",
                    Amount = amount,
                    Currency = currency,
                    Status = "succeeded",
                    EventType = "payment.succeeded",
                    RawPayload = rawPayload,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.Set<PaymentRecord>().Add(paymentRecord);

                // Generate licenses
                var licenses = await GenerateLicensesAsync(order);

                // Update order status
                order.Status = "Completed";
                order.GatewayPaymentId = gatewayPaymentId;
                order.CompletedAtUtc = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation("✅ Payment processed, {Count} licenses generated for order {OrderId}",
                    licenses.Count, orderId);

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "❌ Error processing payment for order {OrderId}", orderId);
                throw;
            }
        }

        /// <summary>
        /// Process payment failure
        /// </summary>
        public async Task<bool> ProcessPaymentFailureAsync(Guid orderId, string reason, string? rawPayload)
        {
            try
            {
                var order = await _context.Set<PurchaseOrder>()
                    .FirstOrDefaultAsync(o => o.Id == orderId);

                if (order == null) return false;

                // Record failed payment
                var paymentRecord = new PaymentRecord
                {
                    Id = Guid.NewGuid(),
                    OrderId = orderId,
                    GatewayType = "dodo",
                    Status = "failed",
                    EventType = "payment.failed",
                    RawPayload = rawPayload,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.Set<PaymentRecord>().Add(paymentRecord);

                // Update order
                order.Status = "Failed";
                await _context.SaveChangesAsync();

                _logger.LogWarning("❌ Payment failed for order {OrderId}: {Reason}", orderId, reason);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing payment failure");
                return false;
            }
        }

        /// <summary>
        /// Generate licenses for completed order
        /// </summary>
        public async Task<List<PurchasedLicense>> GenerateLicensesAsync(PurchaseOrder order)
        {
            var licenses = new List<PurchasedLicense>();
            var startDate = DateTime.UtcNow;
            var endDate = startDate.AddDays(order.LicenseDurationDays);

            for (int i = 0; i < order.LicenseQuantity; i++)
            {
                var license = new PurchasedLicense
                {
                    Id = Guid.NewGuid(),
                    OrderId = order.Id,
                    ProductId = order.ProductId,
                    UserId = order.UserId,
                    LicenseKey = _keyGenerator.Generate(), // 16-digit key
                    StartDateUtc = startDate,
                    EndDateUtc = endDate,
                    Status = "Active",
                    IsActivated = false,
                    CreatedAtUtc = DateTime.UtcNow
                };

                _context.Set<PurchasedLicense>().Add(license);
                licenses.Add(license);
            }

            _logger.LogInformation("🔑 Generated {Count} licenses for order {OrderId}", 
                licenses.Count, order.Id);

            return licenses;
        }

        /// <summary>
        /// Get user's purchased licenses
        /// </summary>
        public async Task<List<UserLicenseDto>> GetUserLicensesAsync(int userId)
        {
            var licenses = await _context.Set<PurchasedLicense>()
                .Include(l => l.Order)
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAtUtc)
                .ToListAsync();

            return licenses.Select(l => new UserLicenseDto
            {
                LicenseId = l.Id,
                LicenseKey = l.LicenseKey,
                ProductId = l.ProductId,
                ProductName = l.Order?.ProductName,
                StartDate = l.StartDateUtc,
                EndDate = l.EndDateUtc,
                DaysRemaining = Math.Max(0, (l.EndDateUtc - DateTime.UtcNow).Days),
                Status = l.EndDateUtc < DateTime.UtcNow ? "Expired" : l.Status,
                IsActivated = l.IsActivated
            }).ToList();
        }

        /// <summary>
        /// Validate redirect URL is in allowed domains
        /// </summary>
        public bool ValidateRedirectUrl(string url)
        {
            try
            {
                if (string.IsNullOrEmpty(url)) return true;

                var uri = new Uri(url);
                
                // Must be HTTPS (except localhost)
                if (uri.Scheme != "https" && uri.Host != "localhost" && uri.Host != "127.0.0.1")
                {
                    return false;
                }

                // Check domain is allowed
                return _allowedRedirectDomains.Any(d => 
                    uri.Host.Equals(d, StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.EndsWith($".{d}", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Generate idempotency key for order
        /// </summary>
        public string GenerateIdempotencyKey(int userId, string productId)
        {
            var data = $"{userId}:{productId}:{DateTime.UtcNow:yyyyMMddHH}";
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToHexString(hash)[..32];
        }

        /// <summary>
        /// Hash string for storage
        /// </summary>
        private static string HashString(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(hash);
        }
    }
}
