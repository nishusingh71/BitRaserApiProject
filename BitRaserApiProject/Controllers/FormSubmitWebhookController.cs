using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using BitRaserApiProject.Services.Email;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// FormSubmit.co Webhook Controller
    /// Receives form submissions from FormSubmit.co and sends auto-response emails
    /// 
    /// IMPORTANT: FormSubmit sends form-urlencoded data, NOT JSON
    /// Use [FromForm] instead of [FromBody]
    /// </summary>
    [ApiController]
    [Route("api/formsubmit")]
    public class FormSubmitWebhookController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<FormSubmitWebhookController> _logger;
        private readonly IEmailOrchestrator? _emailOrchestrator;
        private const string TEAM_EMAIL = "Support@dsecuretech.com";

        public FormSubmitWebhookController(
            ApplicationDbContext context,
            ILogger<FormSubmitWebhookController> logger,
            IEmailOrchestrator? emailOrchestrator = null)
        {
            _context = context;
            _logger = logger;
            _emailOrchestrator = emailOrchestrator;
        }

        /// <summary>
        /// FormSubmit.co webhook endpoint - receives form submissions
        /// POST /api/formsubmit/webhook
        /// 
        /// Note: FormSubmit can send form-urlencoded, JSON, or multipart data
        /// We accept all content types to be flexible
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous] // FormSubmit doesn't send auth tokens
        public async Task<IActionResult> HandleWebhook([FromForm] FormSubmitPayload payload)
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                _logger.LogInformation("📧 FormSubmit Webhook [{RequestId}]: Received from {Email}", 
                    requestId, SanitizeForLog(payload.Email));

                // ═══════════════════════════════════════════════════════════════
                // VALIDATION: Basic checks
                // ═══════════════════════════════════════════════════════════════
                if (string.IsNullOrWhiteSpace(payload.Email) || string.IsNullOrWhiteSpace(payload.Name))
                {
                    _logger.LogWarning("⚠️ FormSubmit Webhook [{RequestId}]: Missing required fields", requestId);
                    return BadRequest(new { success = false, message = "Name and email are required" });
                }

                // ═══════════════════════════════════════════════════════════════
                // SECURITY: Input Sanitization
                // ═══════════════════════════════════════════════════════════════
                var sanitizedName = SanitizeInput(payload.Name);
                var sanitizedEmail = payload.Email.Trim().ToLowerInvariant();
                var sanitizedMessage = SanitizeInput(payload.Message);

                // ═══════════════════════════════════════════════════════════════
                // DUPLICATE DETECTION: Check for recent identical submissions
                // ═══════════════════════════════════════════════════════════════
                var duplicateWindow = DateTime.UtcNow.AddMinutes(-5);
                var isDuplicate = await _context.ContactFormSubmissions
                    .AsNoTracking()
                    .AnyAsync(c =>
                        c.Email == sanitizedEmail &&
                        c.Message == sanitizedMessage &&
                        c.SubmittedAt >= duplicateWindow);

                if (isDuplicate)
                {
                    _logger.LogWarning("🔄 FormSubmit Webhook [{RequestId}]: Duplicate detected for {Email}",
                        requestId, SanitizeForLog(sanitizedEmail));
                    return Ok(new { success = true, message = "Already received" });
                }

                // ═══════════════════════════════════════════════════════════════
                // DATABASE: Save submission
                // ═══════════════════════════════════════════════════════════════
                var submission = new ContactFormSubmission
                {
                    Name = sanitizedName,
                    Email = sanitizedEmail,
                    Message = sanitizedMessage,
                    Company = SanitizeInput(payload.Company),
                    Phone = SanitizeInput(payload.Phone),
                    Country = SanitizeInput(payload.Country),
                    BusinessType = SanitizeInput(payload.BusinessType),
                    SolutionType = SanitizeInput(payload.SolutionType),
                    ComplianceRequirements = SanitizeInput(payload.ComplianceRequirements),
                    UsageType = SanitizeInput(payload.UsageType),
                    Source = payload.Source ?? "FormSubmit Webhook",
                    SubmittedAt = DateTime.UtcNow,
                    Status = "pending",
                    IpAddress = GetClientIpAddress(),
                    UserAgent = Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                _context.ContactFormSubmissions.Add(submission);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ FormSubmit Webhook [{RequestId}]: Saved ID {Id}", requestId, submission.Id);

                // ═══════════════════════════════════════════════════════════════
                // EMAIL: Send auto-response via Microsoft Graph
                // ═══════════════════════════════════════════════════════════════
                bool emailSent = false;

                if (_emailOrchestrator != null)
                {
                    try
                    {
                        // Send team notification
                        await SendTeamNotificationAsync(submission);

                        // Send user auto-response
                        await SendUserAutoResponseAsync(submission);

                        emailSent = true;
                        _logger.LogInformation("📧 FormSubmit Webhook [{RequestId}]: Emails sent", requestId);
                    }
                    catch (Exception emailEx)
                    {
                        _logger.LogWarning(emailEx, "⚠️ FormSubmit Webhook [{RequestId}]: Email failed", requestId);
                    }
                }

                return Ok(new { 
                    success = true, 
                    message = "Webhook received",
                    submissionId = submission.Id,
                    emailSent
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ FormSubmit Webhook [{RequestId}]: Failed", requestId);
                return StatusCode(500, new { success = false, error = ex.Message });
            }
        }

        #region Email Methods

        private async Task SendTeamNotificationAsync(ContactFormSubmission submission)
        {
            if (_emailOrchestrator == null) return;

            var emailRequest = new EmailSendRequest
            {
                ToEmail = TEAM_EMAIL,
                ToName = "DSecure Support Team",
                Subject = $"📩 New Contact Form: {submission.Name} - {submission.BusinessType ?? "General"}",
                HtmlBody = GenerateTeamNotificationHtml(submission),
                Type = EmailType.Notification
            };

            var result = await _emailOrchestrator.SendEmailAsync(emailRequest);

            if (result.Success)
            {
                _logger.LogInformation("✅ Team notification sent via {Provider}", result.ProviderUsed);
            }
            else
            {
                _logger.LogWarning("⚠️ Team notification failed: {Message}", result.Message);
            }
        }

        private async Task SendUserAutoResponseAsync(ContactFormSubmission submission)
        {
            if (_emailOrchestrator == null) return;

            var emailRequest = new EmailSendRequest
            {
                ToEmail = submission.Email,
                ToName = submission.Name,
                Subject = "Thank you for contacting DSecure Technologies",
                HtmlBody = GenerateUserAutoResponseHtml(submission.Name),
                Type = EmailType.Notification
            };

            var result = await _emailOrchestrator.SendEmailAsync(emailRequest);

            if (result.Success)
            {
                _logger.LogInformation("✅ Auto-response sent to {Email} via {Provider}", 
                    SanitizeForLog(submission.Email), result.ProviderUsed);
            }
            else
            {
                _logger.LogWarning("⚠️ Auto-response failed for {Email}: {Message}", 
                    SanitizeForLog(submission.Email), result.Message);
            }
        }

        private string GenerateTeamNotificationHtml(ContactFormSubmission s)
        {
            return $@"
<!DOCTYPE html><html><head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 600px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden;'>
<div style='background: linear-gradient(135deg, #1a1a2e, #16213e); color: #fff; padding: 25px;'>
<h1 style='margin: 0; font-size: 20px;'>📩 New Contact Form Submission</h1>
<p style='margin: 5px 0 0 0; opacity: 0.8;'>Submitted: {s.SubmittedAt:yyyy-MM-dd HH:mm} UTC</p>
</div>
<div style='padding: 25px;'>
<table style='width: 100%; border-collapse: collapse;'>
<tr><td style='padding: 8px 0; color: #666;'><strong>Name:</strong></td><td>{s.Name}</td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Email:</strong></td><td><a href='mailto:{s.Email}'>{s.Email}</a></td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Company:</strong></td><td>{s.Company ?? "Not provided"}</td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Phone:</strong></td><td>{s.Phone ?? "Not provided"}</td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Country:</strong></td><td>{s.Country ?? "Not provided"}</td></tr>
<tr><td style='padding: 8px 0; color: #666;'><strong>Business Type:</strong></td><td>{s.BusinessType ?? "Not specified"}</td></tr>
</table>
<div style='background: #f8f9fa; padding: 15px; border-radius: 8px; margin-top: 20px;'>
<strong>Message:</strong>
<p style='margin: 10px 0 0 0; white-space: pre-wrap;'>{s.Message}</p>
</div>
</div>
<div style='background: #f8f9fa; padding: 15px; text-align: center; font-size: 12px; color: #888;'>
Submission ID: #{s.Id} • Source: {s.Source}
</div>
</div>
</body></html>";
        }

        private string GenerateUserAutoResponseHtml(string userName)
        {
            return $@"
<!DOCTYPE html><html><head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 600px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden;'>
<div style='background: linear-gradient(135deg, #1a1a2e, #16213e); color: #fff; padding: 30px; text-align: center;'>
<h1 style='margin: 0; font-size: 24px;'>Thank You for Contacting Us!</h1>
</div>
<div style='padding: 30px;'>
<p style='font-size: 16px;'>Dear {userName},</p>
<p>Thank you for reaching out to DSecure Technologies. We have received your message and appreciate you taking the time to contact us.</p>
<div style='background: #e8f5e9; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #4caf50;'>
<p style='margin: 0; font-weight: 500;'>⏰ Our team will get back to you within 24 hours.</p>
</div>
<p>In the meantime, if you have any urgent queries, feel free to reach us at:</p>
<p>📧 <a href='mailto:Support@dsecuretech.com'>Support@dsecuretech.com</a></p>
<p style='margin-top: 25px;'>Best regards,<br><strong>DSecure Technologies Team</strong></p>
</div>
<div style='background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #888;'>
© 2024 DSecure Technologies. All rights reserved.<br>
<a href='https://dsecuretech.com' style='color: #1a1a2e;'>www.dsecuretech.com</a>
</div>
</div>
</body></html>";
        }

        #endregion

        #region Helper Methods

        private string? GetClientIpAddress()
        {
            var forwardedFor = Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',').First().Trim();
            }
            return HttpContext.Connection.RemoteIpAddress?.ToString();
        }

        private static string SanitizeInput(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            var sanitized = input.Trim();
            sanitized = System.Text.RegularExpressions.Regex.Replace(sanitized, "<[^>]*>", "");

            if (sanitized.Length > 5000)
                sanitized = sanitized[..5000];

            return sanitized;
        }

        private static string SanitizeForLog(string email)
        {
            if (string.IsNullOrEmpty(email))
                return "[empty]";

            var atIndex = email.IndexOf('@');
            if (atIndex <= 0)
                return "***";

            return email[..Math.Min(3, atIndex)] + "***@***";
        }

        #endregion
    }

    /// <summary>
    /// DTO for FormSubmit.co form-urlencoded payload
    /// </summary>
    public class FormSubmitPayload
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Message { get; set; } = "";
        public string? Company { get; set; }
        public string? Phone { get; set; }
        public string? Country { get; set; }
        public string? BusinessType { get; set; }
        public string? SolutionType { get; set; }
        public string? ComplianceRequirements { get; set; }
        public string? UsageType { get; set; }
        public string? Timestamp { get; set; }
        public string? Source { get; set; }
    }
}
