using System.Text.Json.Serialization;

namespace DSecureApi.Models.DTOs
{
    /// <summary>
    /// Consolidated API response for detailed order information
    /// Includes Order, Payment, Customer, Product, and Invoice details
    /// </summary>
    public class ComprehensiveOrderDto
    {
        [JsonPropertyName("order_details")]
        public OrderSummaryDetails OrderDetails { get; set; }

        [JsonPropertyName("payment_info")]
        public PaymentInformation PaymentInfo { get; set; }

        [JsonPropertyName("product_details")]
        public ProductDetails ProductDetails { get; set; }

        [JsonPropertyName("customer_info")]
        public CustomerInformation CustomerInfo { get; set; }

        [JsonPropertyName("invoice_info")]
        public InvoiceInformation InvoiceInfo { get; set; }

        [JsonPropertyName("license_info")]
        public LicenseInformation LicenseInfo { get; set; }
    }

    public class OrderSummaryDetails
    {
        [JsonPropertyName("order_id")]
        public int OrderId { get; set; }

        [JsonPropertyName("order_date")]
        public DateTime OrderDate { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("dodo_payment_id")]
        public string? DodoPaymentId { get; set; }

        [JsonPropertyName("dodo_invoice_id")]
        public string? DodoInvoiceId { get; set; }
    }

    public class PaymentInformation
    {
        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("method")]
        public string Method { get; set; }

        [JsonPropertyName("amount")]
        public decimal Amount { get; set; }

        [JsonPropertyName("tax_amount")]
        public decimal TaxAmount { get; set; }

        [JsonPropertyName("currency")]
        public string Currency { get; set; }

        [JsonPropertyName("transaction_id")]
        public string? TransactionId { get; set; }

        [JsonPropertyName("provider")]
        public string Provider { get; set; }

        [JsonPropertyName("payment_date")]
        public DateTime? PaymentDate { get; set; }

        [JsonPropertyName("payment_link")]
        public string? PaymentLink { get; set; }

        // Card Details
        [JsonPropertyName("card_last_four")]
        public string? CardLastFour { get; set; }

        [JsonPropertyName("card_network")]
        public string? CardNetwork { get; set; }

        [JsonPropertyName("card_type")]
        public string? CardType { get; set; }
    }

    public class ProductDetails
    {
        [JsonPropertyName("product_id")]
        public string? ProductId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("summary")]
        public string Summary { get; set; }

        [JsonPropertyName("quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("duration_years")]
        public int DurationYears { get; set; }
    }

    public class CustomerInformation
    {
        [JsonPropertyName("customer_id")]
        public string? CustomerId { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("email")]
        public string Email { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("company_name")]
        public string? CompanyName { get; set; }

        [JsonPropertyName("billing_address")]
        public BillingAddressDetails? BillingAddress { get; set; }
    }

    public class BillingAddressDetails
    {
        [JsonPropertyName("street")]
        public string? Street { get; set; }

        [JsonPropertyName("city")]
        public string? City { get; set; }

        [JsonPropertyName("state")]
        public string? State { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("zipcode")]
        public string? Zipcode { get; set; }

        [JsonPropertyName("formatted")]
        public string? Formatted { get; set; }
    }

    public class InvoiceInformation
    {
        [JsonPropertyName("invoice_id")]
        public string? InvoiceId { get; set; }

        [JsonPropertyName("invoice_number")]
        public string? InvoiceNumber { get; set; }

        [JsonPropertyName("date")]
        public DateTime? Date { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; }

        [JsonPropertyName("pdf_url")]
        public string? PdfUrl { get; set; }

        [JsonPropertyName("currency")]
        public string? Currency { get; set; }

        [JsonPropertyName("total_amount")]
        public decimal? TotalAmount { get; set; }
        
        [JsonPropertyName("tax_amount")]
        public decimal? TaxAmount { get; set; }
    }

    public class LicenseInformation
    {
        [JsonPropertyName("license_keys")]
        public List<string>? LicenseKeys { get; set; }

        [JsonPropertyName("license_count")]
        public int LicenseCount { get; set; }

        [JsonPropertyName("license_years")]
        public int LicenseYears { get; set; }

        [JsonPropertyName("expires_at")]
        public DateTime? ExpiresAt { get; set; }
    }
}
