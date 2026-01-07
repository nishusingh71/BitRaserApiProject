using BitRaserApiProject.Models;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Interface for rule-based chatbot
    /// Provides condition-based responses (NOT AI/generative)
    /// </summary>
    public interface IChatbotRuleEngine
    {
        /// <summary>
        /// Get response based on query and context
        /// </summary>
        Task<ChatbotResponse> GetResponseAsync(ChatbotQuery query);

        /// <summary>
        /// Get available chatbot capabilities
        /// </summary>
        List<string> GetCapabilities();
    }

    /// <summary>
    /// Chatbot query input
    /// </summary>
    public class ChatbotQuery
    {
        public string Message { get; set; } = string.Empty;
        public string? UserEmail { get; set; }
        public int? OrderId { get; set; }
        public string? Context { get; set; }  // "order", "payment", "support"
    }

    /// <summary>
    /// Chatbot response output
    /// </summary>
    public class ChatbotResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? RuleMatched { get; set; }
        public Dictionary<string, object>? Data { get; set; }
        public List<string>? SuggestedActions { get; set; }
    }

    /// <summary>
    /// Rule-Based Chatbot Engine
    /// Provides deterministic responses based on predefined conditions
    /// NO AI hallucination - all responses are rule-based
    /// </summary>
    public class ChatbotRuleEngine : IChatbotRuleEngine
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ChatbotRuleEngine> _logger;

        // Rule keywords for matching
        private static readonly Dictionary<string, List<string>> RULE_KEYWORDS = new()
        {
            { "order_status", new() { "order", "status", "track", "where is", "tracking" } },
            { "payment", new() { "payment", "paid", "pay", "charge", "invoice", "receipt" } },
            { "license", new() { "license", "key", "activation", "activate", "serial" } },
            { "download", new() { "download", "software", "install", "file" } },
            { "refund", new() { "refund", "cancel", "money back", "return" } },
            { "support", new() { "help", "support", "contact", "issue", "problem" } },
            { "drive_eraser", new() { "drive eraser", "drive wipe", "disk eraser", "hdd eraser" } },
            { "file_eraser", new() { "file eraser", "file wipe", "file delete", "secure delete" } }
        };

        public ChatbotRuleEngine(ApplicationDbContext context, ILogger<ChatbotRuleEngine> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<ChatbotResponse> GetResponseAsync(ChatbotQuery query)
        {
            try
            {
                _logger.LogInformation("🤖 Chatbot query: {Message}", query.Message);

                var message = query.Message.ToLowerInvariant();

                // Rule 1: Order Status Query
                if (MatchesRule(message, "order_status"))
                {
                    return await HandleOrderStatusQuery(query);
                }

                // Rule 2: Payment Query
                if (MatchesRule(message, "payment"))
                {
                    return await HandlePaymentQuery(query);
                }

                // Rule 3: License Query
                if (MatchesRule(message, "license"))
                {
                    return HandleLicenseQuery(query);
                }

                // Rule 4: Download Query
                if (MatchesRule(message, "download"))
                {
                    return HandleDownloadQuery();
                }

                // Rule 5: Refund Query
                if (MatchesRule(message, "refund"))
                {
                    return HandleRefundQuery();
                }

                // Rule 6: Drive Eraser Query
                if (MatchesRule(message, "drive_eraser"))
                {
                    return HandleDriveEraserQuery();
                }

                // Rule 7: File Eraser Query
                if (MatchesRule(message, "file_eraser"))
                {
                    return HandleFileEraserQuery();
                }

                // Rule 8: General Support
                if (MatchesRule(message, "support"))
                {
                    return HandleSupportQuery();
                }

                // Default: Greeting or Unknown
                if (IsGreeting(message))
                {
                    return HandleGreeting();
                }

                // No rule matched
                return new ChatbotResponse
                {
                    Success = true,
                    Message = "I'm here to help with your orders, payments, licenses, and DSecure products. Could you please be more specific about what you need help with?",
                    RuleMatched = "default",
                    SuggestedActions = new() { "Check order status", "View payment details", "Get license info", "Contact support" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chatbot error");
                return new ChatbotResponse
                {
                    Success = false,
                    Message = "I'm having trouble processing your request. Please try again or contact support@dsecuretech.com",
                    RuleMatched = "error"
                };
            }
        }

        public List<string> GetCapabilities()
        {
            return new List<string>
            {
                "Order status tracking",
                "Payment confirmation",
                "License key information",
                "Download assistance",
                "Refund policy information",
                "Drive Eraser product info",
                "File Eraser product info",
                "General support"
            };
        }

        #region Rule Handlers

        private async Task<ChatbotResponse> HandleOrderStatusQuery(ChatbotQuery query)
        {
            if (query.OrderId.HasValue)
            {
                var order = await _context.Orders.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == query.OrderId.Value);

                if (order != null)
                {
                    return new ChatbotResponse
                    {
                        Success = true,
                        Message = $"📦 Order #{order.OrderId} Status: {order.Status}\n\n" +
                                  $"Product: {order.ProductName}\n" +
                                  $"Amount: {order.Currency} {order.AmountCents / 100m:N2}\n" +
                                  $"Ordered: {order.CreatedAt:MMM dd, yyyy}",
                        RuleMatched = "order_status",
                        Data = new() { { "orderId", order.OrderId }, { "status", order.Status ?? "Unknown" } },
                        SuggestedActions = new() { "View invoice", "Download license", "Contact support" }
                    };
                }
            }

            if (!string.IsNullOrEmpty(query.UserEmail))
            {
                var orders = await _context.Orders.AsNoTracking()
                    .Where(o => o.UserEmail == query.UserEmail)
                    .OrderByDescending(o => o.CreatedAt)
                    .Take(3)
                    .ToListAsync();

                if (orders.Any())
                {
                    var orderList = string.Join("\n", orders.Select(o => $"• Order #{o.OrderId}: {o.Status ?? "Processing"}"));
                    return new ChatbotResponse
                    {
                        Success = true,
                        Message = $"📦 Your Recent Orders:\n\n{orderList}\n\nReply with an order number to get more details.",
                        RuleMatched = "order_status_list",
                        Data = new() { { "orderCount", orders.Count } }
                    };
                }
            }

            return new ChatbotResponse
            {
                Success = true,
                Message = "To check your order status, please provide your order number or the email used during purchase.",
                RuleMatched = "order_status_prompt",
                SuggestedActions = new() { "Enter order number", "Enter email address" }
            };
        }

        private async Task<ChatbotResponse> HandlePaymentQuery(ChatbotQuery query)
        {
            if (query.OrderId.HasValue)
            {
                var order = await _context.Orders.AsNoTracking()
                    .FirstOrDefaultAsync(o => o.OrderId == query.OrderId.Value);

                if (order != null)
                {
                    var isPaid = order.Status?.ToLower() == "paid" || order.Status?.ToLower() == "completed";
                    
                    if (isPaid)
                    {
                        return new ChatbotResponse
                        {
                            Success = true,
                            Message = $"✅ Payment Successful!\n\n" +
                                      $"Order: #{order.OrderId}\n" +
                                      $"Amount: {order.Currency} {order.AmountCents / 100m:N2}\n" +
                                      $"Paid: {order.PaidAt:MMM dd, yyyy HH:mm}\n\n" +
                                      $"Your invoice and license keys have been sent to {order.UserEmail}",
                            RuleMatched = "payment_success",
                            Data = new() { { "orderId", order.OrderId }, { "paymentStatus", "Success" } }
                        };
                    }
                    else
                    {
                        return new ChatbotResponse
                        {
                            Success = true,
                            Message = $"⏳ Payment Status: {order.Status ?? "Processing"}\n\n" +
                                      "If you made a payment but see this status, please wait a few minutes and check again. " +
                                      "If the issue persists, contact support@dsecuretech.com",
                            RuleMatched = "payment_pending"
                        };
                    }
                }
            }

            return new ChatbotResponse
            {
                Success = true,
                Message = "To check your payment status, please provide your order number.",
                RuleMatched = "payment_prompt"
            };
        }

        private ChatbotResponse HandleLicenseQuery(ChatbotQuery query)
        {
            return new ChatbotResponse
            {
                Success = true,
                Message = "📋 License Information:\n\n" +
                          "Your license keys are sent to your email immediately after purchase.\n\n" +
                          "Check the Excel attachment in your order confirmation email.\n\n" +
                          "Can't find it? Check your spam folder or contact support@dsecuretech.com",
                RuleMatched = "license_info",
                SuggestedActions = new() { "Resend license email", "Contact support" }
            };
        }

        private ChatbotResponse HandleDownloadQuery()
        {
            return new ChatbotResponse
            {
                Success = true,
                Message = "📥 Download Instructions:\n\n" +
                          "1. Login to your DSecure dashboard\n" +
                          "2. Go to 'Downloads' section\n" +
                          "3. Select your product version\n" +
                          "4. Click download\n\n" +
                          "Dashboard: https://dsecuretech.com/login",
                RuleMatched = "download_info"
            };
        }

        private ChatbotResponse HandleRefundQuery()
        {
            return new ChatbotResponse
            {
                Success = true,
                Message = "💰 Refund Policy:\n\n" +
                          "DSecure offers a 30-day money-back guarantee.\n\n" +
                          "To request a refund:\n" +
                          "1. Email support@dsecuretech.com\n" +
                          "2. Include your order number\n" +
                          "3. Briefly explain the reason\n\n" +
                          "Refunds are typically processed within 5-7 business days.",
                RuleMatched = "refund_policy"
            };
        }

        private ChatbotResponse HandleDriveEraserQuery()
        {
            return new ChatbotResponse
            {
                Success = true,
                Message = "💾 Drive Eraser:\n\n" +
                          "Securely wipe entire hard drives, SSDs, and storage devices.\n\n" +
                          "✅ Certified data destruction\n" +
                          "✅ PDF erasure report included\n" +
                          "✅ Compliant with NIST, DoD standards\n\n" +
                          "Perfect for disposing of old computers or reselling drives.",
                RuleMatched = "drive_eraser_info",
                Data = new() { { "serviceType", "DriveEraser" }, { "attachmentType", "PDF" } }
            };
        }

        private ChatbotResponse HandleFileEraserQuery()
        {
            return new ChatbotResponse
            {
                Success = true,
                Message = "📁 File Eraser:\n\n" +
                          "Securely delete specific files and folders without wiping entire drives.\n\n" +
                          "✅ Selective file destruction\n" +
                          "✅ Excel deletion report included\n" +
                          "✅ Multiple overwrite passes\n\n" +
                          "Ideal for removing sensitive documents permanently.",
                RuleMatched = "file_eraser_info",
                Data = new() { { "serviceType", "FileEraser" }, { "attachmentType", "Excel" } }
            };
        }

        private ChatbotResponse HandleSupportQuery()
        {
            return new ChatbotResponse
            {
                Success = true,
                Message = "🛟 DSecure Support:\n\n" +
                          "📧 Email: support@dsecuretech.com\n" +
                          "🌐 Website: https://dsecuretech.com\n\n" +
                          "Response time: Within 24 hours (business days)",
                RuleMatched = "support_info"
            };
        }

        private ChatbotResponse HandleGreeting()
        {
            return new ChatbotResponse
            {
                Success = true,
                Message = "👋 Hello! Welcome to DSecure Support.\n\n" +
                          "I can help you with:\n" +
                          "• Order status\n" +
                          "• Payment information\n" +
                          "• License keys\n" +
                          "• Drive Eraser / File Eraser info\n\n" +
                          "How can I assist you today?",
                RuleMatched = "greeting"
            };
        }

        #endregion

        #region Helpers

        private bool MatchesRule(string message, string ruleKey)
        {
            if (!RULE_KEYWORDS.TryGetValue(ruleKey, out var keywords))
                return false;

            return keywords.Any(k => message.Contains(k));
        }

        private bool IsGreeting(string message)
        {
            var greetings = new[] { "hi", "hello", "hey", "good morning", "good afternoon", "good evening" };
            return greetings.Any(g => message.StartsWith(g) || message == g);
        }

        #endregion
    }
}
