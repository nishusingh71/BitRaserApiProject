using DSecureApi.Models;
using DSecureApi.Models.DTOs;

namespace DSecureApi.Services
{
    /// <summary>
    /// Interface for Dodo Payments service
    /// Provides payment integration with Dodo Payments platform
    /// </summary>
    public interface IDodoPaymentService
    {
        /// <summary>
        /// Create a checkout session with Dodo Payments
        /// </summary>
        /// <param name="request">Checkout request with product and customer info</param>
        /// <param name="userEmail">Authenticated user's email</param>
        /// <returns>Checkout response with payment URL</returns>
        Task<DodoCheckoutResponse> CreateCheckoutAsync(DodoCheckoutRequest request, string userEmail);

        /// <summary>
        /// Process webhook event from Dodo Payments
        /// </summary>
        /// <param name="webhookEvent">Webhook event payload</param>
        /// <param name="rawPayload">Raw JSON payload for signature verification</param>
        /// <returns>True if processed successfully</returns>
        Task<bool> ProcessWebhookAsync(DodoWebhookEvent webhookEvent, string rawPayload);

        /// <summary>
        /// Get order by ID
        /// </summary>
        Task<OrderDto?> GetOrderAsync(int orderId, string userEmail);

        /// <summary>
        /// Get orders for a user
        /// </summary>
        Task<OrderListResponse> GetOrdersAsync(string userEmail, int page = 1, int pageSize = 10);

        /// <summary>
        /// Verify webhook signature from Dodo Payments
        /// Uses HMAC SHA256 with webhook-id, webhook-signature, webhook-timestamp headers
        /// </summary>
        /// <param name="payload">Raw request payload</param>
        /// <param name="webhookId">webhook-id header value</param>
        /// <param name="webhookSignature">webhook-signature header value</param>
        /// <param name="webhookTimestamp">webhook-timestamp header value</param>
        /// <returns>True if signature is valid</returns>
        bool VerifyWebhookSignature(string payload, string webhookId, string webhookSignature, string webhookTimestamp);

        /// <summary>
        /// Get available products from Dodo
        /// </summary>
        Task<List<DodoBillingPlanDto>> GetProductsAsync();

        /// <summary>
        /// Force refresh the product catalog cache
        /// </summary>
        Task RefreshProductCacheAsync();

        /// <summary>
        /// Create a new webhook endpoint
        /// </summary>
        Task<DodoWebhookResponse> CreateWebhookAsync(string url, List<string> events);

        /// <summary>
        /// Get list of configured webhooks
        /// </summary>
        Task<List<DodoWebhookResponse>> GetWebhooksAsync();

        /// <summary>
        /// Delete a webhook endpoint
        /// </summary>
        Task<bool> DeleteWebhookAsync(string webhookId);

        /// <summary>
        /// Create a new product in Dodo Payments
        /// </summary>
        /// <param name="request">Product creation request</param>
        /// <returns>Created product details</returns>
        Task<DodoProductResponse> CreateProductAsync(DodoCreateProductRequest request);

        /// <summary>
        /// Create or update a customer in Dodo Payments
        /// </summary>
        /// <param name="email">Customer email</param>
        /// <param name="name">Customer name</param>
        /// <param name="phone">Optional phone number</param>
        /// <returns>Customer response with Dodo customer_id</returns>
        Task<DodoCustomerResponse> CreateOrUpdateCustomerAsync(string email, string name, string? phone = null);

        /// <summary>
        /// Get invoice details from Dodo Payments
        /// </summary>
        /// <param name="invoiceId">Invoice ID from Dodo</param>
        /// <returns>Full invoice details</returns>
        Task<DodoInvoiceResponse> GetInvoiceAsync(string invoiceId);

        /// <summary>
        /// Get invoice details from Dodo Payments using Payment ID
        /// More reliable than Invoice ID as it avoids timing issues
        /// </summary>
        /// <param name="paymentId">Payment ID from Dodo</param>
        /// <returns>Full invoice details</returns>
        Task<DodoInvoiceResponse> GetInvoiceByPaymentIdAsync(string paymentId);

        /// <summary>
        /// 🆕 Create a guest checkout session (No Auth Required)
        /// Order is NOT created until webhook fires
        /// </summary>
        /// <param name="request">Guest checkout request</param>
        /// <returns>Checkout URL and session ID</returns>
        Task<GuestCheckoutResponse> CreateGuestCheckoutSessionAsync(GuestCheckoutRequest request);
    }
}
