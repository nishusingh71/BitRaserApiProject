using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BCrypt.Net;
using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using BitRaserApiProject.Services.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Forgot Password Controller - OTP-based password reset
    /// Works for both Users and Subusers
    /// OTP is NOT stored in database - in-memory only
    /// OTP expires after 10 minutes
    /// </summary>
    [ApiController]
  [Route("api/[controller]")]
    public class ForgotPasswordController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IOtpService _otpService;
        private readonly IEmailService _emailService;
        private readonly IEmailOrchestrator? _emailOrchestrator;
        private readonly ILogger<ForgotPasswordController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;

        public ForgotPasswordController(
            ApplicationDbContext context,
            IOtpService otpService,
            IEmailService emailService,
            ILogger<ForgotPasswordController> logger,
            IConfiguration configuration,
            ICacheService cacheService,
            IEmailOrchestrator? emailOrchestrator = null)
        {
            _context = context;
            _otpService = otpService;
            _emailService = emailService;
            _emailOrchestrator = emailOrchestrator;
            _logger = logger;
            _configuration = configuration;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Step 1: Request OTP for password reset
        /// Email pe OTP bhejega (Users aur Subusers dono ke liye)
        /// </summary>
        [HttpPost("request-otp")]
    [AllowAnonymous]
        public async Task<IActionResult> RequestOtp([FromBody] RequestOtpRequest request)
   {
  try
{
     // Validate email
       if (string.IsNullOrEmpty(request.Email))
      {
        return BadRequest(new { success = false, message = "Email is required" });
      }

      _logger.LogInformation("📧 OTP Request received for: {Email}", request.Email);

        // Check if email exists in Users or Subusers table
        var user = await _context.Users
          .Where(u => u.user_email == request.Email).FirstOrDefaultAsync();

    var subuser = await _context.subuser
      .Where(s => s.subuser_email == request.Email).FirstOrDefaultAsync();

if (user == null && subuser == null)
         {
     _logger.LogWarning("⚠️ Email not found in database: {Email}", request.Email);
            // Security: Don't reveal if email exists or not
        return Ok(new
   {
   success = true,
   message = "If this email exists, an OTP has been sent. Please check your inbox.",
         note = "OTP is valid for 10 minutes"
          });
         }

      // Determine user type and name
string userType = user != null ? "User" : "Subuser";
  string userName = user != null ? user.user_name : (subuser?.Name ?? "User");

      _logger.LogInformation("✅ User found: {UserType} - {UserName}", userType, userName);

      // Generate OTP
    string otp = _otpService.GenerateOtp(request.Email);
            _logger.LogInformation("📧 OTP generated: {OTP} for {Email}", otp, request.Email);

            // Send OTP email via hybrid system (Graph > SendGrid) or fallback
            _logger.LogInformation("📧 Attempting to send OTP email...");
            bool emailSent = await SendOtpEmailAsync(request.Email, otp, userName);

  if (!emailSent)
      {
   _logger.LogError("❌ Failed to send OTP email to {Email}", request.Email);
    return StatusCode(500, new
   {
  success = false,
      message = "Failed to send OTP email. Please try again later.",
       troubleshooting = new
       {
  step1 = "Check Brevo sender email is verified",
  step2 = "Check Brevo API/SMTP key is correct",
 step3 = "Check application logs for detailed error",
 step4 = "Try /api/ForgotPassword/email-config-check endpoint"
       }
       });
  }

  _logger.LogInformation("✅ OTP sent successfully to {Email} ({UserType})", request.Email, userType);

  return Ok(new
        {
  success = true,
message = "OTP has been sent to your email. Please check your inbox.",
  email = request.Email,
           userType = userType,
    expiryMinutes = 10,
        maxAttempts = 5
 });
  }
 catch (Exception ex)
 {
      _logger.LogError(ex, "❌ Error requesting OTP for {Email}", request.Email);
   return StatusCode(500, new
{
   success = false,
   message = "Error processing request",
         error = ex.Message,
         innerError = ex.InnerException?.Message
         });
     }
   }
        /// <summary>
        /// Step 2: Verify OTP
    /// OTP validate karega
 /// </summary>
     [HttpPost("verify-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
  try
{
      // Validate input
    if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Otp))
            {
                  return BadRequest(new { message = "Email and OTP are required" });
       }

      // Check if email exists
  var user = await _context.Users
    .Where(u => u.user_email == request.Email).FirstOrDefaultAsync();

  var subuser = await _context.subuser
          .Where(s => s.subuser_email == request.Email).FirstOrDefaultAsync();

    if (user == null && subuser == null)
    {
      return NotFound(new
   {
       success = false,
    message = "Email not found"
        });
      }

       // Validate OTP
        bool isValid = _otpService.ValidateOtp(request.Email, request.Otp);

         if (!isValid)
           {
  _logger.LogWarning("Invalid OTP attempt for {Email}", request.Email);
    
        // Check if OTP expired
           if (_otpService.IsOtpExpired(request.Email))
      {
           return BadRequest(new
    {
         success = false,
      message = "OTP has expired. Please request a new one.",
   expired = true
     });
         }

 return BadRequest(new
        {
              success = false,
 message = "Invalid OTP. Please check and try again.",
      expired = false
     });
         }

            string userType = user != null ? "User" : "Subuser";

     _logger.LogInformation("OTP verified successfully for {Email} ({UserType})", request.Email, userType);

       return Ok(new
       {
      success = true,
         message = "OTP verified successfully. You can now reset your password.",
         email = request.Email,
            userType = userType,
          verified = true
      });
    }
 catch (Exception ex)
    {
          _logger.LogError(ex, "Error verifying OTP for {Email}", request.Email);
                return StatusCode(500, new
         {
        success = false,
         message = "Error verifying OTP",
      error = ex.Message
         });
            }
        }

/// <summary>
    /// Step 3: Reset Password using OTP
        /// OTP verify karke password reset karega (Users aur Subusers dono)
        /// </summary>
  [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
  try
        {
     // Validate input
                if (string.IsNullOrEmpty(request.Email) || 
    string.IsNullOrEmpty(request.Otp) || 
         string.IsNullOrEmpty(request.NewPassword))
    {
      return BadRequest(new { message = "Email, OTP, and new password are required" });
      }

           if (request.NewPassword.Length < 8)
      {
        return BadRequest(new { message = "Password must be at least 8 characters long" });
                }

                // Validate OTP first
       bool isValidOtp = _otpService.ValidateOtp(request.Email, request.Otp);

          if (!isValidOtp)
     {
           _logger.LogWarning("Invalid OTP for password reset: {Email}", request.Email);
       
          if (_otpService.IsOtpExpired(request.Email))
   {
  return BadRequest(new
    {
        success = false,
          message = "OTP has expired. Please request a new one."
 });
    }

        return BadRequest(new
         {
           success = false,
      message = "Invalid OTP. Please check and try again."
        });
    }

                // Find user in Users or Subusers table
         var user = await _context.Users
          .Where(u => u.user_email == request.Email).FirstOrDefaultAsync();

        var subuser = await _context.subuser
                .Where(s => s.subuser_email == request.Email).FirstOrDefaultAsync();

                if (user == null && subuser == null)
{
         return NotFound(new
            {
             success = false,
           message = "User not found"
      });
         }

      string userType;
      string userName;

       // Update password based on user type
       if (user != null)
    {
        // Update main user password
         user.user_password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
      user.updated_at = DateTime.UtcNow;
       
     _context.Entry(user).State = EntityState.Modified;
              userType = "User";
        userName = user.user_name;
     }
                else
      {
     // Update subuser password
           subuser!.subuser_password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
       subuser.UpdatedAt = DateTime.UtcNow;
 
    _context.Entry(subuser).State = EntityState.Modified;
           _context.Entry(subuser).Property(s => s.subuser_password).IsModified = true;
     userType = "Subuser";
          userName = subuser.Name ?? "User";
        }

          // Save to database
       int rowsAffected = await _context.SaveChangesAsync();

        if (rowsAffected == 0)
{
      _logger.LogError("Failed to save password reset for {Email}", request.Email);
   return StatusCode(500, new
   {
       success = false,
  message = "Failed to reset password. Please try again."
                  });
 }

                // Remove OTP after successful password reset
                _otpService.RemoveOtp(request.Email);

                // Send success email via hybrid system
                await SendPasswordResetSuccessEmailAsync(request.Email, userName);

     _logger.LogInformation("Password reset successfully for {Email} ({UserType})", request.Email, userType);

   return Ok(new
   {
        success = true,
         message = "Password reset successfully. You can now log in with your new password.",
         email = request.Email,
           userType = userType,
           resetAt = DateTime.UtcNow
           });
         }
        catch (Exception ex)
          {
                _logger.LogError(ex, "Error resetting password for {Email}", request.Email);
                return StatusCode(500, new
           {
         success = false,
     message = "Error resetting password",
         error = ex.Message
    });
            }
        }

        /// <summary>
    /// Resend OTP - agar pehla OTP expire ho gaya ya nahi mila
        /// </summary>
        [HttpPost("resend-otp")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendOtp([FromBody] RequestOtpRequest request)
   {
  // Same as RequestOtp - just logging for tracking
            _logger.LogInformation("OTP resend requested for {Email}", request.Email);
            return await RequestOtp(request);
        }

        /// <summary>
        /// Check OTP status - testing/debugging purpose
      /// </summary>
        [HttpGet("otp-status/{email}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public IActionResult CheckOtpStatus(string email)
      {
       bool isExpired = _otpService.IsOtpExpired(email);

        return Ok(new
        {
        email = email,
        hasOtp = !isExpired,
      isExpired = isExpired,
  message = isExpired ? "No active OTP found" : "Active OTP exists"
});
        }

     /// <summary>
        /// Test email configuration - DEVELOPMENT ONLY
        /// </summary>
     [HttpPost("test-email")]
        [AllowAnonymous]
public async Task<IActionResult> TestEmailConfiguration([FromBody] TestEmailRequest request)
        {
 try
{
       _logger.LogInformation("🧪 Testing email configuration for {Email}", request.Email);

            // Send test OTP
 string testOtp = _otpService.GenerateOtp(request.Email);
     
             bool emailSent = await _emailService.SendOtpEmailAsync(
         request.Email, 
       testOtp, 
     "Test User"
      );

 if (emailSent)
    {
         return Ok(new
   {
 success = true,
      message = "✅ Test email sent successfully! Check your inbox.",
    email = request.Email,
     testOtp = testOtp, // Only for testing
          note = "If you received the email, your SMTP configuration is correct!"
   });
    }
    else
            {
   return StatusCode(500, new
        {
          success = false,
   message = "❌ Failed to send test email. Check logs for details.",
      troubleshooting = new
    {
        step1 = "Verify Gmail App Password in .env file",
     step2 = "Enable 2-Step Verification in Google Account",
      step3 = "Generate new App Password from https://myaccount.google.com/apppasswords",
   step4 = "Check if 'Less secure app access' is needed (deprecated)",
        step5 = "Try using different email provider (SendGrid, Mailtrap)"
     }
        });
  }
        }
 catch (Exception ex)
    {
      _logger.LogError(ex, "Error testing email configuration");
    return StatusCode(500, new
          {
    success = false,
         message = "Error testing email configuration",
        error = ex.Message,
       innerError = ex.InnerException?.Message
         });
    }
        }

        /// <summary>
      /// Diagnostic endpoint - Check email configuration
      /// </summary>
    [HttpGet("email-config-check")]
    [AllowAnonymous]
     public IActionResult CheckEmailConfiguration()
        {
     try
   {
  // Check Brevo configuration
           var brevoApiKey = Environment.GetEnvironmentVariable("Brevo__ApiKey")
   ?? Environment.GetEnvironmentVariable("BREVO_API_KEY")
        ?? _configuration["Brevo:ApiKey"];
 var brevoSenderEmail = Environment.GetEnvironmentVariable("Brevo__SenderEmail")
           ?? _configuration["Brevo:SenderEmail"];
 var brevoSenderName = Environment.GetEnvironmentVariable("Brevo__SenderName")
   ?? _configuration["Brevo:SenderName"];

       // Check Gmail SMTP configuration
     var smtpHost = Environment.GetEnvironmentVariable("EmailSettings__SmtpHost")
       ?? _configuration["EmailSettings:SmtpHost"];
    var smtpPort = Environment.GetEnvironmentVariable("EmailSettings__SmtpPort")
  ?? _configuration["EmailSettings:SmtpPort"];
   var fromEmail = Environment.GetEnvironmentVariable("EmailSettings__FromEmail")
         ?? _configuration["EmailSettings:FromEmail"];
   var fromPassword = Environment.GetEnvironmentVariable("EmailSettings__FromPassword")
    ?? _configuration["EmailSettings:FromPassword"];
   var fromName = Environment.GetEnvironmentVariable("EmailSettings__FromName")
  ?? _configuration["EmailSettings:FromName"];
     var timeout = Environment.GetEnvironmentVariable("EmailSettings__Timeout")
        ?? _configuration["EmailSettings:Timeout"];

 // Determine active provider
   var activeProvider = !string.IsNullOrEmpty(brevoApiKey) ? "Brevo" : "Gmail SMTP";

         var configStatus = new
            {
     environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
serverTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        activeEmailProvider = activeProvider,
  
        brevoConfiguration = new
     {
           isConfigured = !string.IsNullOrEmpty(brevoApiKey),
     apiKey = string.IsNullOrEmpty(brevoApiKey) 
    ? "❌ NOT SET" 
       : $"✅ SET ({brevoApiKey.Length} chars, starts with: {brevoApiKey.Substring(0, Math.Min(8, brevoApiKey.Length))}...)",
    senderEmail = brevoSenderEmail ?? "NOT SET",
 senderName = brevoSenderName ?? "NOT SET"
        },

        gmailSmtpConfiguration = new
                {
        isConfigured = !string.IsNullOrEmpty(fromEmail) && !string.IsNullOrEmpty(fromPassword),
          smtpHost = smtpHost ?? "NOT SET",
  smtpPort = smtpPort ?? "NOT SET",
 fromEmail = fromEmail ?? "NOT SET",
        fromPassword = string.IsNullOrEmpty(fromPassword)
   ? "❌ NOT SET"
 : $"✅ SET ({fromPassword.Length} chars)",
      fromName = fromName ?? "NOT SET",
  timeout = timeout ?? "NOT SET"
      },

 configurationStatus = new
   {
      activeProvider = activeProvider,
       brevoConfigured = !string.IsNullOrEmpty(brevoApiKey),
    gmailConfigured = !string.IsNullOrEmpty(fromEmail) && !string.IsNullOrEmpty(fromPassword),
    issues = GetConfigurationIssues(
               brevoApiKey, brevoSenderEmail,
         fromEmail, fromPassword, smtpHost, smtpPort
        )
      },

setupInstructions = new
  {
             brevo = new[]
   {
            "1. Sign up at https://www.brevo.com (free tier: 300 emails/day)",
       "2. Go to SMTP & API → API Keys → Create new API key",
   "3. Set environment variable: Brevo__ApiKey=your-api-key",
         "4. Set environment variable: Brevo__SenderEmail=your-verified-email"
    },
       gmail = new[]
    {
    "1. Enable 2-Step Verification on your Google Account",
        "2. Generate App Password at: https://myaccount.google.com/apppasswords",
 "3. Set EmailSettings__FromPassword=your-16-char-app-password"
        }
   }
      };

     return Ok(configStatus);
     }
    catch (Exception ex)
 {
      return StatusCode(500, new
  {
   error = ex.Message,
           message = "Error checking email configuration"
      });
 }
        }

        private List<string> GetConfigurationIssues(
            string? brevoApiKey, string? brevoSenderEmail,
            string? gmailEmail, string? gmailPassword, string? smtpHost, string? smtpPort)
        {
   var issues = new List<string>();

    // Check Brevo
          if (!string.IsNullOrEmpty(brevoApiKey))
  {
  if (string.IsNullOrEmpty(brevoSenderEmail))
      issues.Add("⚠️ Brevo: SenderEmail not set (will use default)");
       }

      // Check Gmail (if Brevo not configured)
    if (string.IsNullOrEmpty(brevoApiKey))
   {
     if (string.IsNullOrEmpty(gmailEmail))
         issues.Add("❌ Gmail: FromEmail is not configured");

    if (string.IsNullOrEmpty(gmailPassword))
   issues.Add("❌ Gmail: FromPassword is not configured");
     else if (gmailPassword.Length != 16)
   issues.Add($"⚠️ Gmail: Password length is {gmailPassword.Length} chars (App Password should be 16)");

   if (string.IsNullOrEmpty(smtpHost))
   issues.Add("❌ Gmail: SmtpHost is not configured");

  if (string.IsNullOrEmpty(smtpPort))
   issues.Add("❌ Gmail: SmtpPort is not configured");
   }

 if (!issues.Any())
    issues.Add("✅ All configurations look good!");

    return issues;
 }

  /// <summary>
   /// Render.com health check - Verify service is running
        /// </summary>
        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult HealthCheck()
        {
            return Ok(new
      {
    status = "healthy",
service = "ForgotPasswordController",
        environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
          serverTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
       emailProvider = !string.IsNullOrEmpty(
           Environment.GetEnvironmentVariable("Brevo__ApiKey") ?? 
   Environment.GetEnvironmentVariable("BREVO_API_KEY") ?? 
    _configuration["Brevo:ApiKey"]
         ) ? "Brevo" : "Gmail SMTP"
            });
        }

        #region Hybrid Email Helpers

        /// <summary>
        /// Send OTP email via hybrid system (MS Graph > SendGrid) with fallback to old EmailService
        /// </summary>
        private async Task<bool> SendOtpEmailAsync(string email, string otp, string userName)
        {
            // Try hybrid orchestrator first
            if (_emailOrchestrator != null)
            {
                try
                {
                    var request = new EmailSendRequest
                    {
                        ToEmail = email,
                        ToName = userName,
                        Subject = $"Your DSecure Verification Code: {otp}",
                        HtmlBody = GenerateOtpEmailHtml(otp, userName),
                        Type = EmailType.OTP
                    };

                    var result = await _emailOrchestrator.SendEmailAsync(request);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation("✅ OTP email sent via {Provider} to {Email}", 
                            result.ProviderUsed, email);
                        return true;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Hybrid OTP email failed: {Message}, falling back to old service", 
                            result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Hybrid OTP email exception, falling back to old service");
                }
            }

            // Fallback to old EmailService
            return await _emailService.SendOtpEmailAsync(email, otp, userName);
        }

        /// <summary>
        /// Send password reset success email via hybrid system with fallback
        /// </summary>
        private async Task SendPasswordResetSuccessEmailAsync(string email, string userName)
        {
            // Try hybrid orchestrator first
            if (_emailOrchestrator != null)
            {
                try
                {
                    var request = new EmailSendRequest
                    {
                        ToEmail = email,
                        ToName = userName,
                        Subject = "Your DSecure Password Has Been Reset",
                        HtmlBody = GeneratePasswordResetSuccessHtml(userName),
                        Type = EmailType.Transactional
                    };

                    var result = await _emailOrchestrator.SendEmailAsync(request);
                    
                    if (result.Success)
                    {
                        _logger.LogInformation("✅ Password reset success email sent via {Provider} to {Email}", 
                            result.ProviderUsed, email);
                        return;
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Hybrid password reset email failed: {Message}, falling back", 
                            result.Message);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ Hybrid password reset email exception, falling back");
                }
            }

            // Fallback to old EmailService
            await _emailService.SendPasswordResetSuccessEmailAsync(email, userName);
        }

        private string GenerateOtpEmailHtml(string otp, string userName)
        {
            return $@"
<!DOCTYPE html><html><head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden;'>
<div style='background: linear-gradient(135deg, #1a1a2e, #16213e); color: #fff; padding: 30px; text-align: center;'>
<h1 style='margin: 0; font-size: 22px;'>🔐 Verification Code</h1>
</div>
<div style='padding: 30px; text-align: center;'>
<p style='font-size: 16px;'>Dear {userName},</p>
<p>Use this code to verify your identity:</p>
<div style='background: #f0f7ff; padding: 25px; border-radius: 10px; margin: 25px 0;'>
<span style='font-size: 36px; font-weight: bold; letter-spacing: 10px; color: #1a1a2e;'>{otp}</span>
</div>
<p style='color: #888; font-size: 14px;'>This code expires in <strong>10 minutes</strong>.</p>
<p style='color: #888; font-size: 12px;'>If you didn't request this code, please ignore this email.</p>
</div>
<div style='background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #888;'>
© {DateTime.UtcNow.Year} DSecure Technologies. All rights reserved.<br>
<a href='mailto:Support@dsecuretech.com' style='color: #1a1a2e;'>Support@dsecuretech.com</a>
</div>
</div>
</body></html>";
        }

        private string GeneratePasswordResetSuccessHtml(string userName)
        {
            return $@"
<!DOCTYPE html><html><head><meta charset='utf-8'></head>
<body style='font-family: Segoe UI, Arial; padding: 20px; background: #f5f5f5;'>
<div style='max-width: 500px; margin: 0 auto; background: #fff; border-radius: 10px; overflow: hidden;'>
<div style='background: linear-gradient(135deg, #059669, #047857); color: #fff; padding: 30px; text-align: center;'>
<h1 style='margin: 0; font-size: 22px;'>✅ Password Reset Successful</h1>
</div>
<div style='padding: 30px;'>
<p style='font-size: 16px;'>Dear {userName},</p>
<p>Your password has been successfully reset. You can now log in to your dashboard with your new password.</p>
<div style='background: #e8f5e9; padding: 20px; border-radius: 8px; margin: 25px 0; border-left: 4px solid #4caf50;'>
<p style='margin: 0;'>🔒 <strong>Security Tip:</strong> Never share your password with anyone.</p>
</div>
<p>If you did not make this change, please contact us immediately at:</p>
<p>📧 <a href='mailto:Support@dsecuretech.com'>Support@dsecuretech.com</a></p>
</div>
<div style='background: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #888;'>
© {DateTime.UtcNow.Year} DSecure Technologies. All rights reserved.
</div>
</div>
</body></html>";
        }

        #endregion
    }

  #region Request Models

    public class RequestOtpRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    public class VerifyOtpRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
 public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string Otp { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
     public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP is required")]
     [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        public string Otp { get; set; } = string.Empty;

        [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
     public string NewPassword { get; set; } = string.Empty;
    }

    public class TestEmailRequest
    {
        [Required(ErrorMessage = "Email is required")]
     [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;
    }

    #endregion
}
