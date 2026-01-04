using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Purchase Order Status Enum
    /// </summary>
    public enum PurchaseOrderStatus
    {
        PendingPayment,
        Processing,
        Completed,
        Failed,
        Refunded,
        Cancelled
    }

    /// <summary>
    /// Purchase Order Entity
    /// Tracks product purchases with license generation
    /// </summary>
    [Table("purchase_orders")]
    public class PurchaseOrder
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("product_id")]
        [MaxLength(100)]
        public string ProductId { get; set; } = string.Empty;

        [Column("product_name")]
        [MaxLength(255)]
        public string? ProductName { get; set; }

        [Required]
        [Column("license_quantity")]
        public int LicenseQuantity { get; set; } = 1;

        [Required]
        [Column("license_duration_days")]
        public int LicenseDurationDays { get; set; } = 365; // Default 1 year

        [Column("is_recurring")]
        public bool IsRecurring { get; set; } = false;

        [Column("amount")]
        public int Amount { get; set; } // In cents

        [Column("currency")]
        [MaxLength(10)]
        public string Currency { get; set; } = "USD";

        [Required]
        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "PendingPayment";

        [Column("redirect_url")]
        [MaxLength(500)]
        public string? RedirectUrl { get; set; }

        [Column("redirect_url_hash")]
        [MaxLength(64)]
        public string? RedirectUrlHash { get; set; }

        [Column("payment_url")]
        [MaxLength(500)]
        public string? PaymentUrl { get; set; }

        [Column("idempotency_key")]
        [MaxLength(64)]
        public string? IdempotencyKey { get; set; }

        [Column("gateway_order_id")]
        [MaxLength(100)]
        public string? GatewayOrderId { get; set; }

        [Column("gateway_payment_id")]
        [MaxLength(100)]
        public string? GatewayPaymentId { get; set; }

        [Column("created_at_utc")]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        [Column("completed_at_utc")]
        public DateTime? CompletedAtUtc { get; set; }

        [Column("user_email")]
        [MaxLength(255)]
        public string? UserEmail { get; set; }

        /// <summary>
        /// Navigation: Licenses generated from this order
        /// </summary>
        public virtual ICollection<PurchasedLicense>? Licenses { get; set; }

        /// <summary>
        /// Navigation: Payment records for this order
        /// </summary>
        public virtual ICollection<PaymentRecord>? PaymentRecords { get; set; }
    }

    /// <summary>
    /// Purchased License Entity
    /// License generated after successful payment
    /// </summary>
    [Table("purchased_licenses")]
    public class PurchasedLicense
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("order_id")]
        public Guid OrderId { get; set; }

        [Required]
        [Column("product_id")]
        [MaxLength(100)]
        public string ProductId { get; set; } = string.Empty;

        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        [Required]
        [Column("license_key")]
        [MaxLength(64)]
        public string LicenseKey { get; set; } = string.Empty;

        [Required]
        [Column("start_date_utc")]
        public DateTime StartDateUtc { get; set; }

        [Required]
        [Column("end_date_utc")]
        public DateTime EndDateUtc { get; set; }

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "Active";

        [Column("is_activated")]
        public bool IsActivated { get; set; } = false;

        [Column("activated_at_utc")]
        public DateTime? ActivatedAtUtc { get; set; }

        [Column("hwid")]
        [MaxLength(255)]
        public string? Hwid { get; set; }

        [Column("created_at_utc")]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation: Parent order
        /// </summary>
        [ForeignKey("OrderId")]
        public virtual PurchaseOrder? Order { get; set; }
    }

    /// <summary>
    /// Payment Record Entity
    /// Audit trail for all payment events
    /// </summary>
    [Table("payment_records")]
    public class PaymentRecord
    {
        [Key]
        [Column("id")]
        public Guid Id { get; set; } = Guid.NewGuid();

        [Required]
        [Column("order_id")]
        public Guid OrderId { get; set; }

        [Column("gateway_reference")]
        [MaxLength(100)]
        public string? GatewayReference { get; set; }

        [Column("gateway_type")]
        [MaxLength(50)]
        public string? GatewayType { get; set; } // dodo, polar

        [Column("amount")]
        public int Amount { get; set; }

        [Column("currency")]
        [MaxLength(10)]
        public string Currency { get; set; } = "USD";

        [Column("status")]
        [MaxLength(20)]
        public string Status { get; set; } = "pending";

        [Column("event_type")]
        [MaxLength(50)]
        public string? EventType { get; set; }

        [Column("raw_payload")]
        public string? RawPayload { get; set; } // JSON

        [Column("created_at_utc")]
        public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Navigation: Parent order
        /// </summary>
        [ForeignKey("OrderId")]
        public virtual PurchaseOrder? Order { get; set; }
    }
}
