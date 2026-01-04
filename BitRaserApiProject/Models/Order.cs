using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Order entity for tracking purchases
    /// Stores order information from Polar.sh payments
    /// </summary>
    [Table("orders")]
    [Index(nameof(UserEmail))]
    [Index(nameof(PolarOrderId), IsUnique = true)]
    [Index(nameof(Status))]
    public class Order
    {
        [Key]
        [Column("order_id")]
        public int OrderId { get; set; }

        /// <summary>
        /// Polar.sh Order ID (external reference)
        /// </summary>
        [Column("polar_order_id")]
        [StringLength(100)]
        public string? PolarOrderId { get; set; }

        /// <summary>
        /// Polar.sh Checkout ID
        /// </summary>
        [Column("polar_checkout_id")]
        [StringLength(100)]
        public string? PolarCheckoutId { get; set; }

        /// <summary>
        /// Dodo Payments Payment ID (external reference)
        /// </summary>
        [Column("dodo_payment_id")]
        [StringLength(100)]
        public string? DodoPaymentId { get; set; }

        /// <summary>
        /// Dodo Payments Invoice ID (for fetching invoice details)
        /// </summary>
        [Column("dodo_invoice_id")]
        [StringLength(100)]
        public string? DodoInvoiceId { get; set; }

        /// <summary>
        /// Payment provider: polar, dodo
        /// </summary>
        [Column("payment_provider")]
        [StringLength(50)]
        public string? PaymentProvider { get; set; }

        /// <summary>
        /// User email who made the purchase
        /// </summary>
        [Required]
        [Column("user_email")]
        [StringLength(255)]
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Customer first name
        /// </summary>
        [Column("first_name")]
        [StringLength(100)]
        public string? FirstName { get; set; }

        /// <summary>
        /// Customer last name
        /// </summary>
        [Column("last_name")]
        [StringLength(100)]
        public string? LastName { get; set; }

        /// <summary>
        /// Customer phone number
        /// </summary>
        [Column("phone_number")]
        [StringLength(50)]
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Company/Organization name
        /// </summary>
        [Column("company_name")]
        [StringLength(255)]
        public string? CompanyName { get; set; }

        /// <summary>
        /// Product ID purchased
        /// </summary>
        [Column("product_id")]
        [StringLength(100)]
        public string? ProductId { get; set; }

        /// <summary>
        /// Product name
        /// </summary>
        [Column("product_name")]
        [StringLength(255)]
        public string? ProductName { get; set; }

        /// <summary>
        /// Number of licenses purchased
        /// </summary>
        [Column("license_count")]
        public int LicenseCount { get; set; } = 1;

        /// <summary>
        /// License duration in years
        /// </summary>
        [Column("license_years")]
        public int LicenseYears { get; set; } = 1;

        /// <summary>
        /// Order amount in cents (e.g., $200 = 20000)
        /// </summary>
        [Column("amount_cents")]
        public int AmountCents { get; set; }

        /// <summary>
        /// Currency code (USD, EUR, etc.)
        /// </summary>
        [Column("currency")]
        [StringLength(10)]
        public string Currency { get; set; } = "USD";

        /// <summary>
        /// Order status: pending, processing, completed, failed, refunded
        /// </summary>
        [Required]
        [Column("status")]
        [StringLength(50)]
        public string Status { get; set; } = "pending";

        /// <summary>
        /// Payment method: card, paypal, wire
        /// </summary>
        [Column("payment_method")]
        [StringLength(50)]
        public string? PaymentMethod { get; set; }

        // Billing Address
        [Column("billing_country")]
        [StringLength(100)]
        public string? BillingCountry { get; set; }

        [Column("billing_address")]
        [StringLength(500)]
        public string? BillingAddress { get; set; }

        [Column("billing_city")]
        [StringLength(100)]
        public string? BillingCity { get; set; }

        [Column("billing_state")]
        [StringLength(100)]
        public string? BillingState { get; set; }

        [Column("billing_zip")]
        [StringLength(20)]
        public string? BillingZip { get; set; }

        /// <summary>
        /// License keys generated for this order (comma-separated or JSON)
        /// </summary>
        [Column("license_keys", TypeName = "TEXT")]
        public string? LicenseKeys { get; set; }

        /// <summary>
        /// Order creation timestamp
        /// </summary>
        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Order last update timestamp
        /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Payment completion timestamp
        /// </summary>
        [Column("paid_at")]
        public DateTime? PaidAt { get; set; }

        /// <summary>
        /// License expiry date
        /// </summary>
        [Column("license_expires_at")]
        public DateTime? LicenseExpiresAt { get; set; }

        /// <summary>
        /// Raw webhook payload from Polar (JSON)
        /// </summary>
        [Column("webhook_payload", TypeName = "TEXT")]
        public string? WebhookPayload { get; set; }

        /// <summary>
        /// Any notes or metadata
        /// </summary>
        [Column("notes", TypeName = "TEXT")]
        public string? Notes { get; set; }

        /// <summary>
        /// Plan ID from payment provider
        /// </summary>
        [Column("plan_id")]
        [StringLength(100)]
        public string? PlanId { get; set; }

        /// <summary>
        /// User ID linked to this order (after account creation)
        /// </summary>
        [Column("user_id")]
        public int? UserId { get; set; }

        /// <summary>
        /// Timestamp when webhook was processed (for idempotency)
        /// </summary>
        [Column("webhook_processed_at")]
        public DateTime? WebhookProcessedAt { get; set; }

        /// <summary>
        /// Whether user account was created for this order
        /// </summary>
        [Column("user_created")]
        public bool UserCreated { get; set; } = false;

        /// <summary>
        /// Whether credentials email was sent
        /// </summary>
        [Column("credentials_email_sent")]
        public bool CredentialsEmailSent { get; set; } = false;
    }
}
