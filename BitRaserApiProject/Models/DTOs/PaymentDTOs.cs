using System.ComponentModel.DataAnnotations;

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
    /// D-Secure product for checkout
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
}
