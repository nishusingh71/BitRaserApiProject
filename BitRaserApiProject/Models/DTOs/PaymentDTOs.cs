using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace BitRaserApiProject.Models.DTOs
{
    #region Checkout Request/Response

    /// <summary>
    /// Request to create a checkout session
    /// </summary>
    public class CreateCheckoutRequest
    {
        /// <summary>
        /// Product ID from Polar.sh
        /// </summary>
        [Required]
        public string ProductId { get; set; } = string.Empty;

        /// <summary>
        /// Number of licenses to purchase
        /// </summary>
        [Range(1, 1000)]
        public int LicenseCount { get; set; } = 1;

        /// <summary>
        /// License duration in years
        /// </summary>
        [Range(1, 10)]
        public int LicenseYears { get; set; } = 1;

        /// <summary>
        /// Customer information
        /// </summary>
        public CustomerInfo? Customer { get; set; }

        /// <summary>
        /// Billing address
        /// </summary>
        public BillingAddress? BillingAddress { get; set; }

        /// <summary>
        /// URL to redirect after successful payment
        /// </summary>
        public string? SuccessUrl { get; set; }

        /// <summary>
        /// URL to redirect after cancelled payment
        /// </summary>
        public string? CancelUrl { get; set; }
    }

    /// <summary>
    /// Customer information for checkout
    /// </summary>
    public class CustomerInfo
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? CompanyName { get; set; }
    }

    /// <summary>
    /// Billing address for checkout
    /// </summary>
    public class BillingAddress
    {
        public string? Country { get; set; }
        public string? StreetAddress { get; set; }
        public string? City { get; set; }
        public string? State { get; set; }
        public string? ZipCode { get; set; }
    }

    /// <summary>
    /// Response from checkout session creation
    /// </summary>
    public class CreateCheckoutResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        
        /// <summary>
        /// Checkout URL to redirect user to Polar
        /// </summary>
        public string? CheckoutUrl { get; set; }
        
        /// <summary>
        /// Internal order ID
        /// </summary>
        public int? OrderId { get; set; }
        
        /// <summary>
        /// Polar checkout session ID
        /// </summary>
        public string? CheckoutId { get; set; }
        
        /// <summary>
        /// Expires at (checkout session expiry)
        /// </summary>
        public DateTime? ExpiresAt { get; set; }
    }

    #endregion

    #region Order DTOs

    /// <summary>
    /// Order summary for API responses
    /// </summary>
    public class OrderDto
    {
        public int OrderId { get; set; }
        public string? PolarOrderId { get; set; }
        public string UserEmail { get; set; } = string.Empty;
        public string? ProductName { get; set; }
        public int LicenseCount { get; set; }
        public int LicenseYears { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
        public string Status { get; set; } = "pending";
        public string? PaymentMethod { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
        public DateTime? LicenseExpiresAt { get; set; }
        public List<string>? LicenseKeys { get; set; }
    }

    /// <summary>
    /// Order list response with pagination
    /// </summary>
    public class OrderListResponse
    {
        public List<OrderDto> Orders { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
    }

    #endregion

    #region Polar Webhook DTOs

    /// <summary>
    /// Polar.sh webhook event payload
    /// </summary>
    public class PolarWebhookEvent
    {
        public string Type { get; set; } = string.Empty;
        public PolarWebhookData? Data { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Polar webhook data object
    /// </summary>
    public class PolarWebhookData
    {
        public string? Id { get; set; }
        public string? Status { get; set; }
        public PolarCustomer? Customer { get; set; }
        public PolarProduct? Product { get; set; }
        public PolarAmount? Amount { get; set; }
        public PolarCheckout? Checkout { get; set; }
        public string? PaymentMethod { get; set; }
        public List<PolarLicenseKey>? LicenseKeys { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    public class PolarCustomer
    {
        public string? Id { get; set; }
        public string? Email { get; set; }
        public string? Name { get; set; }
    }

    public class PolarProduct
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
    }

    public class PolarAmount
    {
        public int? Amount { get; set; }
        public string? Currency { get; set; }
    }

    public class PolarCheckout
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        public string? Status { get; set; }
    }

    public class PolarLicenseKey
    {
        public string? Id { get; set; }
        public string? Key { get; set; }
        public string? Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    #endregion

    #region Polar API Request/Response DTOs

    /// <summary>
    /// Request to Polar API to create checkout
    /// </summary>
    public class PolarCreateCheckoutRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public string? SuccessUrl { get; set; }
        public string? CustomerEmail { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Response from Polar API checkout creation
    /// </summary>
    public class PolarCheckoutResponse
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        public string? Status { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public PolarCustomer? Customer { get; set; }
        public PolarProduct? Product { get; set; }
    }

    #endregion

    #region Product DTOs

    /// <summary>
    /// D-Secure product for checkout (legacy)
    /// </summary>
    public class ProductDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int PricePerLicenseCents { get; set; }
        public string Currency { get; set; } = "USD";
        public List<string>? Features { get; set; }
    }

    #endregion

    #region Pro-Level Billing DTOs

    /// <summary>
    /// ✅ PRO-LEVEL: Simplified billing plan DTO for React frontend
    /// Contains both monthly and yearly price IDs for direct checkout
    /// </summary>
    public class BillingPlanDto
    {
        /// <summary>Polar Product ID</summary>
        public string ProductId { get; set; } = string.Empty;
        
        /// <summary>Plan name (e.g., "Pro", "Enterprise")</summary>
        public string Name { get; set; } = string.Empty;
        
        /// <summary>Plan description</summary>
        public string? Description { get; set; }
        
        /// <summary>✅ Monthly price ID for checkout - use this in POST /checkout</summary>
        public string? MonthlyPriceId { get; set; }
        
        /// <summary>✅ Yearly price ID for checkout - use this in POST /checkout</summary>
        public string? YearlyPriceId { get; set; }
        
        /// <summary>Monthly amount in dollars (e.g., 19.99)</summary>
        public decimal MonthlyAmount { get; set; }
        
        /// <summary>Yearly amount in dollars (e.g., 199.99)</summary>
        public decimal YearlyAmount { get; set; }
        
        /// <summary>Currency code (e.g., "USD")</summary>
        public string Currency { get; set; } = "USD";
        
        /// <summary>List of features included in this plan</summary>
        public List<string>? Features { get; set; }
        
        /// <summary>Whether this is the most popular/recommended plan</summary>
        public bool IsPopular { get; set; }
        
        /// <summary>Sort order for display</summary>
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// ✅ PRO-LEVEL: Price-based checkout request (uses priceId, not productId)
    /// </summary>
    public class PriceCheckoutRequest
    {
        /// <summary>
        /// ✅ Price ID from Polar (monthlyPriceId or yearlyPriceId from BillingPlanDto)
        /// </summary>
        [Required]
        public string PriceId { get; set; } = string.Empty;
        
        /// <summary>
        /// URL to redirect after successful payment
        /// </summary>
        [Required]
        public string SuccessUrl { get; set; } = string.Empty;
        
        /// <summary>
        /// Optional: Customer email (uses authenticated user if not provided)
        /// </summary>
        public string? CustomerEmail { get; set; }
        
        /// <summary>
        /// Optional: Additional metadata to store with the order
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// ✅ PRO-LEVEL: Response from price-based checkout
    /// </summary>
    public class PriceCheckoutResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        
        /// <summary>Checkout URL to redirect user to Polar payment page</summary>
        public string? CheckoutUrl { get; set; }
        
        /// <summary>Polar checkout session ID</summary>
        public string? CheckoutId { get; set; }
        
        /// <summary>When the checkout session expires</summary>
        public DateTime? ExpiresAt { get; set; }
    }

    #endregion

    #region Polar API Response Models (Internal)

    /// <summary>
    /// Polar API product list response
    /// </summary>
    public class PolarProductListResponse
    {
        public List<PolarProductItem>? Items { get; set; }
        public PolarPagination? Pagination { get; set; }
    }

    public class PolarProductItem
    {
        public string? Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool Is_Archived { get; set; }
        public bool Is_Recurring { get; set; }
        public List<PolarPrice>? Prices { get; set; }
    }

    public class PolarPrice
    {
        public string? Id { get; set; }
        public string? Type { get; set; }  // "recurring" or "one_time"
        public string? Recurring_Interval { get; set; }  // "month" or "year"
        
        [System.Text.Json.Serialization.JsonPropertyName("recurring_interval")]
        public string? RecurringInterval { get; set; }  // Alias for snake_case
        
        public int? Amount_Type { get; set; }
        public int? Price_Amount { get; set; }  // in cents
        
        [System.Text.Json.Serialization.JsonPropertyName("price_amount")]
        public int? PriceAmount { get; set; }  // Alias for snake_case
        
        public string? Price_Currency { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("price_currency")]
        public string? PriceCurrency { get; set; }  // Alias for snake_case
    }

    public class PolarPagination
    {
        public int Total_Count { get; set; }
        public int Max_Page { get; set; }
    }

    /// <summary>
    /// Polar checkout session response
    /// </summary>
    public class PolarCheckoutSessionResponse
    {
        public string? Id { get; set; }
        public string? Url { get; set; }
        public string? Status { get; set; }
        public DateTime? Expires_At { get; set; }
    }

    #endregion

    #region Dodo Payments DTOs

    /// <summary>
    /// Dodo Payments webhook event payload
    /// </summary>
    public class DodoWebhookEvent
    {
        [JsonPropertyName("business_id")]
        public string BusinessId { get; set; } = string.Empty;
        
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; }
        
        [JsonPropertyName("data")]
        public DodoWebhookData? Data { get; set; }
    }

    /// <summary>
    /// Dodo webhook data object - matches actual Dodo API format
    /// </summary>
    public class DodoWebhookData
    {
        [JsonPropertyName("payload_type")]
        public string PayloadType { get; set; } = string.Empty; // Payment, Subscription, Refund, Dispute, LicenseKey
        
        [JsonPropertyName("payment_id")]
        public string? PaymentId { get; set; }
        
        [JsonPropertyName("status")]
        public string? Status { get; set; }
        
        [JsonPropertyName("total_amount")]
        public int? TotalAmount { get; set; }
        
        /// <summary>
        /// Backwards-compatible alias for TotalAmount
        /// </summary>
        [JsonIgnore]
        public int? Amount => TotalAmount;
        
        [JsonPropertyName("settlement_amount")]
        public int? SettlementAmount { get; set; }
        
        [JsonPropertyName("currency")]
        public string? Currency { get; set; }
        
        [JsonPropertyName("settlement_currency")]
        public string? SettlementCurrency { get; set; }
        
        [JsonPropertyName("customer")]
        public DodoCustomer? Customer { get; set; }
        
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
        
        [JsonPropertyName("product_cart")]
        public List<DodoProductCartItem>? ProductCart { get; set; }
        
        [JsonPropertyName("checkout_session_id")]
        public string? CheckoutSessionId { get; set; }
        
        [JsonPropertyName("subscription_id")]
        public string? SubscriptionId { get; set; }
        
        [JsonPropertyName("invoice_id")]
        public string? InvoiceId { get; set; }
        
        [JsonPropertyName("refund_id")]
        public string? RefundId { get; set; }
        
        [JsonPropertyName("license_key")]
        public string? LicenseKey { get; set; }
        
        [JsonPropertyName("payment_method")]
        public string? PaymentMethod { get; set; }
        
        [JsonPropertyName("payment_link")]
        public string? PaymentLink { get; set; }
        
        [JsonPropertyName("card_last_four")]
        public string? CardLastFour { get; set; }
        
        [JsonPropertyName("card_network")]
        public string? CardNetwork { get; set; }
        
        [JsonPropertyName("card_type")]
        public string? CardType { get; set; }
        
        [JsonPropertyName("card_issuing_country")]
        public string? CardIssuingCountry { get; set; }
        
        [JsonPropertyName("business_id")]
        public string? BusinessId { get; set; }
        
        [JsonPropertyName("brand_id")]
        public string? BrandId { get; set; }
        
        [JsonPropertyName("billing")]
        public DodoBillingAddress? Billing { get; set; }
        
        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }
        
        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }
        
        [JsonPropertyName("digital_products_delivered")]
        public bool? DigitalProductsDelivered { get; set; }
        
        [JsonPropertyName("error_code")]
        public string? ErrorCode { get; set; }
        
        [JsonPropertyName("error_message")]
        public string? ErrorMessage { get; set; }
        
        [JsonPropertyName("disputes")]
        public List<object>? Disputes { get; set; }
        
        [JsonPropertyName("refunds")]
        public List<object>? Refunds { get; set; }
        
        [JsonPropertyName("tax")]
        public int? Tax { get; set; }
        
        [JsonPropertyName("settlement_tax")]
        public int? SettlementTax { get; set; }
        
        [JsonPropertyName("discount_id")]
        public string? DiscountId { get; set; }

        // Helper properties for easy access to customer info
        [JsonIgnore]
        public string? CustomerEmail => Customer?.Email;

        [JsonIgnore]
        public string? CustomerName => Customer?.Name;
    }

    /// <summary>
    /// Product cart item in Dodo webhook
    /// </summary>
    public class DodoProductCartItem
    {
        [JsonPropertyName("product_id")]
        public string? ProductId { get; set; }
        
        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }
    }

    /// <summary>
    /// Billing address in Dodo webhook
    /// </summary>
    public class DodoBillingAddress
    {
        [JsonPropertyName("city")]
        public string? City { get; set; }
        
        [JsonPropertyName("country")]
        public string? Country { get; set; }
        
        [JsonPropertyName("state")]
        public string? State { get; set; }
        
        [JsonPropertyName("street")]
        public string? Street { get; set; }
        
        [JsonPropertyName("zipcode")]
        public string? Zipcode { get; set; }
    }

    /// <summary>
    /// Customer object in Dodo webhook - matches actual API format
    /// </summary>
    public class DodoCustomer
    {
        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }
        
        [JsonPropertyName("email")]
        public string? Email { get; set; }
        
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        
        [JsonPropertyName("phone_number")]
        public string? PhoneNumber { get; set; }
        
        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }
    }

    /// <summary>
    /// Request to create a new webhook endpoint
    /// </summary>
    public class DodoWebhookRequest
    {
        [Required]
        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("events")]
        public List<string> Events { get; set; } = new List<string>();

        [JsonPropertyName("description")]
        public string? Description { get; set; }
    }

    /// <summary>
    /// Response when creating/retrieving webhooks
    /// </summary>
    public class DodoWebhookResponse
    {
        [JsonPropertyName("webhook_id")]
        public string WebhookId { get; set; } = string.Empty;

        [JsonPropertyName("url")]
        public string Url { get; set; } = string.Empty;

        [JsonPropertyName("events")]
        public List<string> Events { get; set; } = new List<string>();

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("created_at")]
        public DateTime CreatedAt { get; set; }
        
        [JsonPropertyName("secret")]
        public string? Secret { get; set; }
    }

    /// <summary>
    /// Dodo checkout request
    /// </summary>
    public class DodoCheckoutRequest
    {
        [Required]
        public string ProductId { get; set; } = string.Empty;
        
        public string SuccessUrl { get; set; } = string.Empty;
        
        public string? CancelUrl { get; set; } // URL to redirect when payment fails/cancelled
        
        public string? DiscountCode { get; set; } // Discount/coupon code to apply automatically
        
        /// <summary>
        /// Customer email (optional, uses authenticated user if not provided)
        /// </summary>
        public string? CustomerEmail { get; set; }
        
        /// <summary>
        /// Customer name (required by Dodo API)
        /// </summary>
        public string? CustomerName { get; set; }
        
        /// <summary>
        /// Billing country code, e.g., "US", "IN", "GB" (required by Dodo API)
        /// </summary>
        public string? BillingCountry { get; set; }
        
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// Dodo checkout response
    /// </summary>
    public class DodoCheckoutResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int? OrderId { get; set; } // ✅ Internal order ID for frontend tracking
        public string? PaymentId { get; set; }
        public string? CheckoutUrl { get; set; }
        public DateTime? ExpiresAt { get; set; }
    }

    /// <summary>
    /// 🆕 Guest checkout request - No authentication required
    /// Order created ONLY when webhook fires with actual customer data
    /// </summary>
    public class GuestCheckoutRequest
    {
        [Required]
        [JsonPropertyName("product_id")]
        public string ProductId { get; set; } = string.Empty;

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; } = 1;

        [JsonPropertyName("return_url")]
        public string? ReturnUrl { get; set; }

        [JsonPropertyName("cancel_url")]
        public string? CancelUrl { get; set; }

        [JsonPropertyName("discount_code")]
        public string? DiscountCode { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// 🆕 Guest checkout response
    /// </summary>
    public class GuestCheckoutResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? SessionId { get; set; }
        // Note: No OrderId until payment completes via webhook
    }

    /// <summary>
    /// Request to create/update a Dodo customer
    /// </summary>
    public class DodoCustomerRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        
        [Required]
        public string Name { get; set; } = string.Empty;
        
        public string? Phone { get; set; }
    }

    /// <summary>
    /// Response from Dodo customer creation/update
    /// </summary>
    public class DodoCustomerResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? CustomerId { get; set; } // Dodo customer_id
        public string? Email { get; set; }
        public string? Name { get; set; }
    }

    /// <summary>
    /// Dodo billing plan DTO for product listing
    /// </summary>
    public class DodoBillingPlanDto
    {
        public string ProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public string Currency { get; set; } = "USD";
        public bool IsRecurring { get; set; }
        public string? RecurringInterval { get; set; } // "month", "year"
        public int DisplayOrder { get; set; }
    }

    /// <summary>
    /// Dodo API product list response
    /// </summary>
    public class DodoProductListResponse
    {
        public List<DodoProductItem>? Items { get; set; }
    }

    public class DodoProductItem
    {
        public string? ProductId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Price { get; set; } // in cents
        public string? Currency { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurringInterval { get; set; }
    }

    /// <summary>
    /// Request to create a new product in Dodo Payments
    /// </summary>
    public class DodoCreateProductRequest
    {
        /// <summary>
        /// Product name (required)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Product description
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// Price in cents (e.g., 9900 = $99.00)
        /// </summary>
        public int Price { get; set; }

        /// <summary>
        /// Currency code (e.g., "USD", "EUR", "INR")
        /// </summary>
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Whether this is a recurring subscription product
        /// </summary>
        public bool IsRecurring { get; set; } = false;

        /// <summary>
        /// Recurring interval: "day", "week", "month", "year" (required if IsRecurring is true)
        /// </summary>
        public string? RecurringInterval { get; set; }

        /// <summary>
        /// Product image URL
        /// </summary>
        public string? ImageUrl { get; set; }

        /// <summary>
        /// Custom metadata
        /// </summary>
        public Dictionary<string, string>? Metadata { get; set; }
    }

    /// <summary>
    /// Response from Dodo Payments product creation
    /// </summary>
    public class DodoProductResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ProductId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public int? Price { get; set; }
        public string? Currency { get; set; }
        public bool IsRecurring { get; set; }
        public string? RecurringInterval { get; set; }
        public DateTime? CreatedAt { get; set; }
    }

    /// <summary>
    /// Dodo invoice response DTO
    /// </summary>
    public class DodoInvoiceResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public DodoInvoice? Invoice { get; set; }
    }

    /// <summary>
    /// Dodo invoice details
    /// </summary>
    public class DodoInvoice
    {
        [JsonPropertyName("invoice_id")]
        public string InvoiceId { get; set; } = string.Empty;

        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("payment_id")]
        public string? PaymentId { get; set; }

        [JsonPropertyName("customer")]
        public DodoCustomer? Customer { get; set; }

        [JsonPropertyName("total_amount")]
        public int TotalAmount { get; set; }

        [JsonPropertyName("tax")]
        public int? Tax { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; } = "USD";

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("product_cart")]
        public List<DodoProductCartItem>? Products { get; set; }

        [JsonPropertyName("metadata")]
        public Dictionary<string, object>? Metadata { get; set; }

        [JsonPropertyName("billing")]
        public DodoBillingAddress? BillingAddress { get; set; }

        [JsonPropertyName("created_at")]
        public DateTime? CreatedAt { get; set; }

        [JsonPropertyName("updated_at")]
        public DateTime? UpdatedAt { get; set; }

        [JsonPropertyName("paid_at")]
        public DateTime? PaidAt { get; set; }

        [JsonPropertyName("invoice_url")]
        public string? InvoiceUrl { get; set; }

        [JsonPropertyName("pdf_url")]
        public string? PdfUrl { get; set; }

        // Computed properties for easy access
        [JsonIgnore]
        public decimal TotalAmountDecimal => TotalAmount / 100m;

        [JsonIgnore]
        public decimal? TaxDecimal => Tax.HasValue ? Tax.Value / 100m : null;

        [JsonIgnore]
        public string CustomerEmail => Customer?.Email ?? "";

        [JsonIgnore]
        public string CustomerName => Customer?.Name ?? "";
    }

    #endregion
}

