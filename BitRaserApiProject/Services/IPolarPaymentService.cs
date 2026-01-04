using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Interface for Polar.sh payment service
    /// </summary>
    public interface IPolarPaymentService
    {
        /// <summary>
        /// Create a checkout session with Polar.sh
        /// </summary>
        Task<CreateCheckoutResponse> CreateCheckoutAsync(CreateCheckoutRequest request, string userEmail);

        /// <summary>
        /// Process webhook event from Polar.sh
        /// </summary>
        Task<bool> ProcessWebhookAsync(PolarWebhookEvent webhookEvent, string rawPayload);

        /// <summary>
        /// Get order by ID
        /// </summary>
        Task<OrderDto?> GetOrderAsync(int orderId, string userEmail);

        /// <summary>
        /// Get orders for a user
        /// </summary>
        Task<OrderListResponse> GetOrdersAsync(string userEmail, int page = 1, int pageSize = 10);

        /// <summary>
        /// Verify webhook signature
        /// </summary>
        bool VerifyWebhookSignature(string payload, string signature);

        /// <summary>
        /// Get available products (legacy)
        /// </summary>
        Task<List<ProductDto>> GetProductsAsync();

        #region Pro-Level Methods

        /// <summary>
        /// ✅ PRO-LEVEL: Get billing plans with monthly/yearly price IDs
        /// Fetches products dynamically from Polar API and caches for 1 hour
        /// </summary>
        Task<List<BillingPlanDto>> GetBillingPlansAsync();

        /// <summary>
        /// ✅ PRO-LEVEL: Create checkout using price ID (not product ID)
        /// This is the recommended way for subscription-based checkout
        /// </summary>
        Task<PriceCheckoutResponse> CreatePriceCheckoutAsync(PriceCheckoutRequest request, string userEmail);

        /// <summary>
        /// ✅ PRO-LEVEL: Force refresh the product catalog cache
        /// </summary>
        Task RefreshProductCacheAsync();

        #endregion
    }
}
