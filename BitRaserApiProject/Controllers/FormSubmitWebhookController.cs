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
        /// Accepts: application/json, multipart/form-data, x-www-form-urlencoded
        /// </summary>
        [HttpPost("webhook")]
        [AllowAnonymous] // FormSubmit doesn't send auth tokens
        public async Task<IActionResult> HandleWebhook()
        {
            var requestId = Guid.NewGuid().ToString("N")[..8];

            try
            {
                // ═══════════════════════════════════════════════════════════════
                // PARSE REQUEST: Handle JSON, Form, and any other content type
                // FormSubmit can send various formats, so we try multiple parsers
                // ═══════════════════════════════════════════════════════════════
                FormSubmitPayload? payload = null;
                var contentType = Request.ContentType?.ToLowerInvariant() ?? "";

                _logger.LogInformation("📧 FormSubmit Webhook [{RequestId}]: ContentType={ContentType}", 
                    requestId, contentType);

                // Try parsing based on content type, with fallbacks
                try
                {
                    if (contentType.Contains("application/json"))
                    {
                        // JSON body
                        payload = await Request.ReadFromJsonAsync<FormSubmitPayload>();
                    }
                    else if (contentType.Contains("multipart/form-data") || 
                             contentType.Contains("x-www-form-urlencoded") ||
                             contentType.Contains("form"))
                    {
                        // Form data
                        var form = await Request.ReadFormAsync();
                        payload = new FormSubmitPayload
                        {
                            Name = form["name"].ToString(),
                            Email = form["email"].ToString(),
                            Message = form["message"].ToString(),
                            Company = form["company"].ToString(),
                            Phone = form["phone"].ToString(),
                            Country = form["country"].ToString(),
                            BusinessType = form["businessType"].ToString(),
                            SolutionType = form["solutionType"].ToString(),
                            ComplianceRequirements = form["complianceRequirements"].ToString(),
                            UsageType = form["usageType"].ToString(),
                            Timestamp = form["timestamp"].ToString(),
                            Source = form["source"].ToString()
                        };
                    }
                    else
                    {
                        // Unknown content type - try Form first, then JSON
                        try
                        {
                            var form = await Request.ReadFormAsync();
                            if (form.Keys.Count > 0)
                            {
                                payload = new FormSubmitPayload
                                {
                                    Name = form["name"].ToString(),
                                    Email = form["email"].ToString(),
                                    Message = form["message"].ToString(),
                                    Company = form["company"].ToString(),
                                    Phone = form["phone"].ToString(),
                                    Country = form["country"].ToString(),
                                    BusinessType = form["businessType"].ToString(),
                                    SolutionType = form["solutionType"].ToString(),
                                    ComplianceRequirements = form["complianceRequirements"].ToString(),
                                    UsageType = form["usageType"].ToString(),
                                    Timestamp = form["timestamp"].ToString(),
                                    Source = form["source"].ToString()
                                };
                                _logger.LogInformation("📧 FormSubmit Webhook [{RequestId}]: Parsed as Form data (unknown content type)", requestId);
                            }
                        }
                        catch
                        {
                            // Form parsing failed, try JSON
                            Request.Body.Position = 0; // Reset stream position
                            payload = await Request.ReadFromJsonAsync<FormSubmitPayload>();
                            _logger.LogInformation("📧 FormSubmit Webhook [{RequestId}]: Parsed as JSON (unknown content type)", requestId);
                        }
                    }
                }
                catch (Exception parseEx)
                {
                    _logger.LogWarning(parseEx, "⚠️ FormSubmit Webhook [{RequestId}]: Parse failed for ContentType={ContentType}", 
                        requestId, contentType);
                    return BadRequest(new { success = false, message = $"Failed to parse request body: {parseEx.Message}" });
                }

                if (payload == null)
                {
                    _logger.LogWarning("⚠️ FormSubmit Webhook [{RequestId}]: Payload is null", requestId);
                    return BadRequest(new { success = false, message = "Invalid request body" });
                }

                _logger.LogInformation("📧 FormSubmit Webhook [{RequestId}]: Received from {Email}", 
                    requestId, SanitizeForLog(payload.Email));

                // ═══════════════════════════════════════════════════════════════
                // VALIDATION: Basic checks with CustomerEmail fallback
                // ═══════════════════════════════════════════════════════════════
                
                // Use Email field, fallback to CustomerEmail if empty
                var userEmail = !string.IsNullOrWhiteSpace(payload.Email) 
                    ? payload.Email 
                    : payload.CustomerEmail;
                
                if (string.IsNullOrWhiteSpace(userEmail) || string.IsNullOrWhiteSpace(payload.Name))
                {
                    _logger.LogWarning("⚠️ FormSubmit Webhook [{RequestId}]: Missing required fields - Name={Name}, Email={Email}, CustomerEmail={CustomerEmail}", 
                        requestId, payload.Name ?? "null", payload.Email ?? "null", payload.CustomerEmail ?? "null");
                    return BadRequest(new { success = false, message = "Name and email are required" });
                }
                
                // Check if sendAutoReply flag is set
                var shouldSendAutoReply = string.Equals(payload.SendAutoReply, "true", StringComparison.OrdinalIgnoreCase);
                _logger.LogInformation("📧 FormSubmit Webhook [{RequestId}]: SendAutoReply={Flag}", requestId, shouldSendAutoReply);

                // ═══════════════════════════════════════════════════════════════
                // SECURITY: Input Sanitization
                // ═══════════════════════════════════════════════════════════════
                var sanitizedName = SanitizeInput(payload.Name);
                var sanitizedEmail = userEmail.Trim().ToLowerInvariant();
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
                bool autoReplySent = false;

                if (_emailOrchestrator != null)
                {
                    try
                    {
                        // Always send team notification
                        await SendTeamNotificationAsync(submission);

                        // Send user auto-response ONLY if sendAutoReply flag is "true"
                        if (shouldSendAutoReply)
                        {
                            await SendUserAutoResponseAsync(submission);
                            autoReplySent = true;
                            _logger.LogInformation("📧 FormSubmit Webhook [{RequestId}]: Auto-reply sent to user", requestId);
                        }
                        else
                        {
                            _logger.LogInformation("📧 FormSubmit Webhook [{RequestId}]: Auto-reply skipped (flag not set)", requestId);
                        }

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
                    emailSent,
                    autoReplySent
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
<div style='max-width: 600px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.1);'>
<div style='background: linear-gradient(135deg, #1a1a2e, #16213e); color: #fff; padding: 35px; text-align: center;'>
<h1 style='margin: 0; font-size: 26px;'>Thank You for Contacting Us!</h1>
<p style='margin: 10px 0 0 0; opacity: 0.9;'>We've received your inquiry</p>
</div>
<div style='padding: 35px;'>
<p style='font-size: 16px; margin: 0 0 20px 0;'>Dear {userName},</p>
<p style='color: #444; line-height: 1.6;'>Thank you for reaching out to DSecure Technologies. We have successfully received your message and our team is already reviewing your inquiry.</p>

<div style='background: linear-gradient(135deg, #e8f5e9, #c8e6c9); padding: 25px; border-radius: 10px; margin: 25px 0; border-left: 5px solid #4caf50;'>
<p style='margin: 0; font-weight: 600; font-size: 17px; color: #2e7d32;'>⏰ Our support team will respond within 12 hours</p>
<p style='margin: 8px 0 0 0; color: #388e3c; font-size: 14px;'>We prioritize all inquiries and aim to resolve them promptly.</p>
</div>

<p style='color: #555;'>In the meantime, if you have any urgent queries, feel free to contact us directly:</p>

<div style='background: #f8f9fa; padding: 20px; border-radius: 8px; margin: 20px 0;'>
<p style='margin: 0 0 10px 0;'>📧 <strong>Email:</strong> <a href='mailto:Support@dsecuretech.com' style='color: #1a1a2e;'>Support@dsecuretech.com</a></p>
<p style='margin: 0;'>🌐 <strong>Website:</strong> <a href='https://dsecuretech.com' style='color: #1a1a2e;'>www.dsecuretech.com</a></p>
</div>

<p style='margin-top: 30px; color: #444;'>Best regards,<br><strong style='color: #1a1a2e;'>DSecure Technologies Support Team</strong></p>
</div>
<div style='background: #1a1a2e; padding: 25px; text-align: center; font-size: 12px; color: #aaa;'>
<p style='margin: 0;'>© 2024 DSecure Technologies. All rights reserved.</p>
<p style='margin: 8px 0 0 0;'><a href='https://dsecuretech.com' style='color: #4CAF50;'>www.dsecuretech.com</a></p>
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
    /// DTO for FormSubmit.co payload (supports both JSON and Form data)
    /// </summary>
    public class FormSubmitPayload
    {
        // Core form fields
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

        // ═══════════════════════════════════════════════════════════════
        // NEW: Explicit fields sent from frontend for auto-reply control
        // ═══════════════════════════════════════════════════════════════
        
        /// <summary>
        /// Flag to indicate if auto-reply should be sent (from frontend)
        /// Value: "true" or "false"
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("sendAutoReply")]
        public string? SendAutoReply { get; set; }

        /// <summary>
        /// Fallback customer email if 'Email' field is missing
        /// </summary>
        [System.Text.Json.Serialization.JsonPropertyName("customer_email")]
        public string? CustomerEmail { get; set; }

        // FormSubmit metadata fields (we capture but don't rely on)
        [System.Text.Json.Serialization.JsonPropertyName("_replyto")]
        public string? ReplyTo { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("_subject")]
        public string? Subject { get; set; }
        
        [System.Text.Json.Serialization.JsonPropertyName("_webhookContentType")]
        public string? WebhookContentType { get; set; }
    }
}
