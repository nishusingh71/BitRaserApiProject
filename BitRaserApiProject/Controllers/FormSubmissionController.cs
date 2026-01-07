using BitRaserApiProject.Services.Email;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Form Submission Controller
    /// Handles contact forms and sends to company email only (NOT to customer)
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class FormSubmissionController : ControllerBase
    {
        private readonly IEmailOrchestrator? _emailOrchestrator;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FormSubmissionController> _logger;

        // Company email to receive form submissions
        private const string COMPANY_EMAIL = "support@dsecuretech.com";

        public FormSubmissionController(
            IEmailOrchestrator? emailOrchestrator,
            IConfiguration configuration,
            ILogger<FormSubmissionController> logger)
        {
            _emailOrchestrator = emailOrchestrator;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Submit a contact form - sends to company email only
        /// </summary>
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitForm([FromBody] FormSubmissionRequest request)
        {
            try
            {
                // Validate request
                if (!ModelState.IsValid)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid form data",
                        errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                    });
                }

                _logger.LogInformation("📝 Form submission received from: {Email}", request.Email);

                if (_emailOrchestrator == null)
                {
                    _logger.LogError("❌ Email orchestrator not available");
                    return StatusCode(500, new { success = false, message = "Email service unavailable" });
                }

                // Build HTML email body
                var htmlBody = GenerateFormEmailHtml(request);

                // Send to COMPANY email only (not customer)
                var companyEmail = Environment.GetEnvironmentVariable("COMPANY_EMAIL")
                    ?? _configuration["CompanyEmail"]
                    ?? COMPANY_EMAIL;

                var emailRequest = new EmailSendRequest
                {
                    ToEmail = companyEmail,
                    ToName = "DSecure Support",
                    Subject = $"[Form] {request.Subject} - from {request.Name}",
                    HtmlBody = htmlBody,
                    Type = EmailType.Notification,
                    ReplyToEmail = request.Email  // Set reply-to as customer email
                };

                var result = await _emailOrchestrator.SendEmailAsync(emailRequest);

                if (result.Success)
                {
                    _logger.LogInformation("✅ Form submitted and email sent to company via {Provider}", result.ProviderUsed);
                    return Ok(new
                    {
                        success = true,
                        message = "Thank you! Your message has been received. We'll get back to you soon.",
                        provider = result.ProviderUsed
                    });
                }
                else
                {
                    _logger.LogError("❌ Failed to send form email: {Message}", result.Message);
                    return StatusCode(500, new { success = false, message = "Failed to submit form. Please try again." });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing form submission");
                return StatusCode(500, new { success = false, message = "An error occurred. Please try again later." });
            }
        }

        /// <summary>
        /// Generate HTML email body for form submission
        /// </summary>
        private string GenerateFormEmailHtml(FormSubmissionRequest request)
        {
            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <style>
        body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f5f5f5; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 4px 12px rgba(0,0,0,0.1); }}
        .header {{ background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%); color: white; padding: 25px; text-align: center; }}
        .content {{ padding: 25px; }}
        .field {{ margin-bottom: 15px; }}
        .label {{ font-weight: 600; color: #1a1a2e; margin-bottom: 5px; }}
        .value {{ background: #f8f9fa; padding: 12px; border-radius: 6px; color: #333; }}
        .message-box {{ background: #f0f4c3; padding: 15px; border-radius: 8px; margin-top: 15px; white-space: pre-wrap; }}
        .footer {{ background: #f5f5f5; padding: 15px; text-align: center; font-size: 12px; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h2 style='margin: 0;'>📝 New Form Submission</h2>
            <p style='margin: 5px 0 0 0; opacity: 0.9;'>{DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC</p>
        </div>
        <div class='content'>
            <div class='field'>
                <div class='label'>👤 Name</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(request.Name)}</div>
            </div>
            <div class='field'>
                <div class='label'>📧 Email</div>
                <div class='value'><a href='mailto:{request.Email}'>{System.Net.WebUtility.HtmlEncode(request.Email)}</a></div>
            </div>
            {(!string.IsNullOrEmpty(request.Phone) ? $@"
            <div class='field'>
                <div class='label'>📞 Phone</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(request.Phone)}</div>
            </div>" : "")}
            {(!string.IsNullOrEmpty(request.Company) ? $@"
            <div class='field'>
                <div class='label'>🏢 Company</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(request.Company)}</div>
            </div>" : "")}
            <div class='field'>
                <div class='label'>📋 Subject</div>
                <div class='value'>{System.Net.WebUtility.HtmlEncode(request.Subject)}</div>
            </div>
            <div class='field'>
                <div class='label'>💬 Message</div>
                <div class='message-box'>{System.Net.WebUtility.HtmlEncode(request.Message)}</div>
            </div>
        </div>
        <div class='footer'>
            <p>This is an automated email from DSecure contact form.</p>
            <p>Reply directly to respond to the customer.</p>
        </div>
    </div>
</body>
</html>";
        }
    }

    /// <summary>
    /// Form submission request DTO
    /// </summary>
    public class FormSubmissionRequest
    {
        [Required(ErrorMessage = "Name is required")]
        [StringLength(100, MinimumLength = 2)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Subject is required")]
        [StringLength(200, MinimumLength = 3)]
        public string Subject { get; set; } = string.Empty;

        [Required(ErrorMessage = "Message is required")]
        [StringLength(5000, MinimumLength = 10)]
        public string Message { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Invalid phone number")]
        public string? Phone { get; set; }

        [StringLength(100)]
        public string? Company { get; set; }
    }
}
