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
        /// Get available products
        /// </summary>
        Task<List<ProductDto>> GetProductsAsync();
    }
}
