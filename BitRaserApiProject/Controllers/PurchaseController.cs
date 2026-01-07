using DSecureApi.Data;
using DSecureApi.Models;
using DSecureApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DSecureApi.Controllers
{
    /// <summary>
    /// Purchase Controller
    /// Handles product purchases and license management
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class PurchaseController : ControllerBase
    {
        private readonly IPurchaseDomainService _purchaseService;
        private readonly ILogger<PurchaseController> _logger;

        public PurchaseController(
            IPurchaseDomainService purchaseService,
            ILogger<PurchaseController> logger)
        {
            _purchaseService = purchaseService;
            _logger = logger;
        }

        #region Order Endpoints

        /// <summary>
        /// Create a purchase order
        /// POST /api/purchase/orders
        /// Returns payment URL for redirect
        /// </summary>
        [Authorize]
        [HttpPost("orders")]
        [ProducesResponseType(typeof(CreatePurchaseOrderResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<ActionResult<CreatePurchaseOrderResponse>> CreateOrder(
            [FromBody] CreatePurchaseOrderRequest request)
        {
            try
            {
                // Get authenticated user
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized(new CreatePurchaseOrderResponse
                    {
                        Success = false,
                        Message = "User not authenticated"
                    });
                }

                // Validate request
                if (string.IsNullOrEmpty(request.ProductId))
                {
                    return BadRequest(new CreatePurchaseOrderResponse
                    {
                        Success = false,
                        Message = "ProductId is required"
                    });
                }

                if (request.LicenseQuantity <= 0)
                {
                    request.LicenseQuantity = 1;
                }

                if (request.LicenseDurationDays <= 0)
                {
                    request.LicenseDurationDays = 365; // Default 1 year
                }

                // Create order
                var response = await _purchaseService.CreateOrderAsync(request, userId, userEmail);

                if (!response.Success)
                {
                    return BadRequest(response);
                }

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new CreatePurchaseOrderResponse
                {
                    Success = false,
                    Message = "Failed to create order"
                });
            }
        }

        /// <summary>
        /// Get order by ID
        /// GET /api/purchase/orders/{orderId}
        /// </summary>
        [Authorize]
        [HttpGet("orders/{orderId:guid}")]
        public async Task<ActionResult> GetOrder(Guid orderId)
        {
            try
            {
                var order = await _purchaseService.GetOrderAsync(orderId);

                if (order == null)
                {
                    return NotFound(new { success = false, message = "Order not found" });
                }

                // Verify ownership
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId) || order.UserId != userId)
                {
                    // Check if admin
                    if (!User.IsInRole("SuperAdmin") && !User.IsInRole("Admin"))
                    {
                        return Forbid();
                    }
                }

                return Ok(new
                {
                    success = true,
                    order = new
                    {
                        orderId = order.Id,
                        productId = order.ProductId,
                        productName = order.ProductName,
                        licenseQuantity = order.LicenseQuantity,
                        licenseDurationDays = order.LicenseDurationDays,
                        amount = order.Amount,
                        currency = order.Currency,
                        status = order.Status,
                        paymentUrl = order.PaymentUrl,
                        redirectUrl = order.RedirectUrl,
                        createdAt = order.CreatedAtUtc,
                        completedAt = order.CompletedAtUtc,
                        licenses = order.Licenses?.Select(l => new
                        {
                            licenseId = l.Id,
                            licenseKey = l.LicenseKey,
                            startDate = l.StartDateUtc,
                            endDate = l.EndDateUtc,
                            status = l.Status,
                            isActivated = l.IsActivated
                        })
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", orderId);
                return StatusCode(500, new { success = false, message = "Failed to get order" });
            }
        }

        /// <summary>
        /// Get order status (for polling after redirect)
        /// GET /api/purchase/orders/{orderId}/status
        /// </summary>
        [AllowAnonymous]
        [HttpGet("orders/{orderId:guid}/status")]
        public async Task<ActionResult> GetOrderStatus(Guid orderId)
        {
            var order = await _purchaseService.GetOrderAsync(orderId);

            if (order == null)
            {
                return NotFound(new { success = false, status = "not_found" });
            }

            // Build redirect URL with status
            var redirectUrl = order.RedirectUrl ?? "";
            var separator = redirectUrl.Contains("?") ? "&" : "?";
            var fullRedirectUrl = order.Status switch
            {
                "Completed" => $"{redirectUrl}{separator}status=success&orderId={order.Id}",
                "Failed" => $"{redirectUrl}{separator}status=failed&orderId={order.Id}",
                _ => $"{redirectUrl}{separator}status=pending&orderId={order.Id}"
            };

            return Ok(new
            {
                success = true,
                orderId = order.Id,
                status = order.Status,
                redirectUrl = fullRedirectUrl,
                licensesGenerated = order.Status == "Completed" ? order.Licenses?.Count ?? 0 : 0
            });
        }

        #endregion

        #region License Endpoints

        /// <summary>
        /// Get user's purchased licenses
        /// GET /api/purchase/licenses/my
        /// </summary>
        [Authorize]
        [HttpGet("licenses/my")]
        public async Task<ActionResult<List<UserLicenseDto>>> GetMyLicenses()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!int.TryParse(userIdClaim, out var userId))
                {
                    return Unauthorized();
                }

                var licenses = await _purchaseService.GetUserLicensesAsync(userId);

                return Ok(new
                {
                    success = true,
                    count = licenses.Count,
                    licenses = licenses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user licenses");
                return StatusCode(500, new { success = false, message = "Failed to get licenses" });
            }
        }

        #endregion

        #region Quick Purchase (Anonymous for testing)

        /// <summary>
        /// Quick purchase - create order without auth (for testing)
        /// POST /api/purchase/quick
        /// </summary>
        [AllowAnonymous]
        [HttpPost("quick")]
        public async Task<ActionResult<CreatePurchaseOrderResponse>> QuickPurchase(
            [FromBody] QuickPurchaseRequest request)
        {
            try
            {
                // For testing - create order with provided email
                if (string.IsNullOrEmpty(request.Email))
                {
                    return BadRequest(new { success = false, message = "Email is required" });
                }

                var orderRequest = new CreatePurchaseOrderRequest
                {
                    ProductId = request.ProductId ?? "default-product",
                    ProductName = request.ProductName,
                    LicenseQuantity = request.LicenseQuantity > 0 ? request.LicenseQuantity : 1,
                    LicenseDurationDays = request.LicenseDurationDays > 0 ? request.LicenseDurationDays : 365,
                    RedirectUrl = request.RedirectUrl,
                    Amount = request.Amount,
                    Currency = request.Currency ?? "USD"
                };

                var response = await _purchaseService.CreateOrderAsync(orderRequest, 0, request.Email);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in quick purchase");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        #endregion
    }

    /// <summary>
    /// Quick purchase request (for testing)
    /// </summary>
    public class QuickPurchaseRequest
    {
        public string? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int LicenseQuantity { get; set; } = 1;
        public int LicenseDurationDays { get; set; } = 365;
        public string? RedirectUrl { get; set; }
        public string? Email { get; set; }
        public int? Amount { get; set; }
        public string? Currency { get; set; }
    }
}
