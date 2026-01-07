using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Mvc;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Chatbot Controller
    /// Rule-based chatbot API endpoint - NO AI hallucination
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ChatbotController : ControllerBase
    {
        private readonly IChatbotRuleEngine _chatbotEngine;
        private readonly ILogger<ChatbotController> _logger;

        public ChatbotController(
            IChatbotRuleEngine chatbotEngine,
            ILogger<ChatbotController> logger)
        {
            _chatbotEngine = chatbotEngine;
            _logger = logger;
        }

        /// <summary>
        /// Send a message to the chatbot and get a rule-based response
        /// </summary>
        [HttpPost("query")]
        public async Task<IActionResult> Query([FromBody] ChatbotQueryRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Message))
                {
                    return BadRequest(new { success = false, message = "Message is required" });
                }

                var query = new ChatbotQuery
                {
                    Message = request.Message,
                    UserEmail = request.Email,
                    OrderId = request.OrderId,
                    Context = request.Context
                };

                var response = await _chatbotEngine.GetResponseAsync(query);

                return Ok(new
                {
                    success = response.Success,
                    message = response.Message,
                    ruleMatched = response.RuleMatched,
                    data = response.Data,
                    suggestedActions = response.SuggestedActions
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Chatbot query error");
                return StatusCode(500, new 
                { 
                    success = false, 
                    message = "An error occurred processing your request" 
                });
            }
        }

        /// <summary>
        /// Get chatbot capabilities
        /// </summary>
        [HttpGet("capabilities")]
        public IActionResult GetCapabilities()
        {
            var capabilities = _chatbotEngine.GetCapabilities();
            return Ok(new
            {
                success = true,
                capabilities = capabilities,
                description = "DSecure Rule-Based Support Bot - Provides deterministic responses based on predefined rules"
            });
        }
    }

    /// <summary>
    /// Chatbot query request DTO
    /// </summary>
    public class ChatbotQueryRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int? OrderId { get; set; }
        public string? Context { get; set; }
    }
}
