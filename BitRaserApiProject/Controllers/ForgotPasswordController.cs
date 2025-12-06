using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using BCrypt.Net;
using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
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
   private readonly ILogger<ForgotPasswordController> _logger;
        private readonly IConfiguration _configuration;

        public ForgotPasswordController(
            ApplicationDbContext context,
     IOtpService otpService,
    IEmailService emailService,
      ILogger<ForgotPasswordController> logger,
  IConfiguration configuration)
        {
   _context = context;
  _otpService = otpService;
  _emailService = emailService;
      _logger = logger;
            _configuration = configuration;
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
        return BadRequest(new { message = "Email is required" });
      }

          // Check if email exists in Users or Subusers table
        var user = await _context.Users
          .FirstOrDefaultAsync(u => u.user_email == request.Email);

    var subuser = await _context.subuser
            .FirstOrDefaultAsync(s => s.subuser_email == request.Email);

if (user == null && subuser == null)
           {
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

            // Generate OTP
    string otp = _otpService.GenerateOtp(request.Email);

       // Send OTP email
                bool emailSent = await _emailService.SendOtpEmailAsync(request.Email, otp, userName);

  if (!emailSent)
      {
   _logger.LogWarning("Failed to send OTP email to {Email}", request.Email);
           return StatusCode(500, new
      {
  success = false,
      message = "Failed to send OTP email. Please try again later."
       });
                }

                _logger.LogInformation("OTP sent successfully to {Email} ({UserType})", request.Email, userType);

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
            _logger.LogError(ex, "Error requesting OTP for {Email}", request.Email);
   return StatusCode(500, new
{
     success = false,
     message = "Error processing request",
         error = ex.Message
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
    .FirstOrDefaultAsync(u => u.user_email == request.Email);

  var subuser = await _context.subuser
          .FirstOrDefaultAsync(s => s.subuser_email == request.Email);

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
          .FirstOrDefaultAsync(u => u.user_email == request.Email);

        var subuser = await _context.subuser
                .FirstOrDefaultAsync(s => s.subuser_email == request.Email);

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

                // Send success email
       await _emailService.SendPasswordResetSuccessEmailAsync(request.Email, userName);

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
      // Check appsettings configuration
     var smtpHost = _configuration["EmailSettings:SmtpHost"];
        var smtpPort = _configuration["EmailSettings:SmtpPort"];
     var fromEmail = _configuration["EmailSettings:FromEmail"];
    var fromPassword = _configuration["EmailSettings:FromPassword"];
   var fromName = _configuration["EmailSettings:FromName"];
       var enableSsl = _configuration["EmailSettings:EnableSsl"];
   var timeout = _configuration["EmailSettings:Timeout"];

      // Check environment variables (Render.com)
     var envSmtpHost = Environment.GetEnvironmentVariable("EmailSettings__SmtpHost");
   var envSmtpPort = Environment.GetEnvironmentVariable("EmailSettings__SmtpPort");
  var envEmail = Environment.GetEnvironmentVariable("EmailSettings__FromEmail");
  var envPassword = Environment.GetEnvironmentVariable("EmailSettings__FromPassword");
  var envFromName = Environment.GetEnvironmentVariable("EmailSettings__FromName");
      var envTimeout = Environment.GetEnvironmentVariable("EmailSettings__Timeout");

            // Determine effective values (env vars take priority)
       var effectiveHost = envSmtpHost ?? smtpHost ?? "NOT SET";
     var effectivePort = envSmtpPort ?? smtpPort ?? "NOT SET";
      var effectiveEmail = envEmail ?? fromEmail ?? "NOT SET";
  var effectivePassword = envPassword ?? fromPassword;
        var effectiveName = envFromName ?? fromName ?? "NOT SET";
            var effectiveTimeout = envTimeout ?? timeout ?? "NOT SET";

     var configStatus = new
 {
     environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
     serverTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
    
           effectiveConfiguration = new
      {
            smtpHost = effectiveHost,
 smtpPort = effectivePort,
          fromEmail = effectiveEmail,
      fromPassword = string.IsNullOrEmpty(effectivePassword) 
     ? "❌ NOT SET" 
    : $"✅ SET ({effectivePassword.Length} chars)",
          fromName = effectiveName,
  timeout = effectiveTimeout
     },
   
   fromAppSettings = new
   {
 smtpHost = smtpHost ?? "NOT SET",
            smtpPort = smtpPort ?? "NOT SET",
       fromEmail = fromEmail ?? "NOT SET",
   fromPassword = string.IsNullOrEmpty(fromPassword) 
   ? "NOT SET" 
 : $"SET ({fromPassword.Length} chars)",
              fromName = fromName ?? "NOT SET",
enableSsl = enableSsl ?? "NOT SET",
        timeout = timeout ?? "NOT SET"
      },
    
        fromEnvironmentVariables = new
  {
      smtpHost = envSmtpHost ?? "NOT SET",
    smtpPort = envSmtpPort ?? "NOT SET",
         fromEmail = envEmail ?? "NOT SET",
     fromPassword = string.IsNullOrEmpty(envPassword) 
 ? "NOT SET" 
         : $"SET ({envPassword.Length} chars)",
     fromName = envFromName ?? "NOT SET",
       timeout = envTimeout ?? "NOT SET"
    },

  configurationStatus = new
   {
   isConfigured = !string.IsNullOrEmpty(effectiveEmail) && !string.IsNullOrEmpty(effectivePassword),
           issues = GetConfigurationIssues(effectiveEmail, effectivePassword, effectiveHost, effectivePort)
    },
      
   recommendations = new[]
   {
         "1. On Render.com, set environment variables with double underscore: EmailSettings__FromEmail",
      "2. Gmail App Password should be 16 characters without spaces",
       "3. Generate at: https://myaccount.google.com/apppasswords",
 "4. Use test-email endpoint to verify email sending",
           "5. Check Render.com logs for detailed error messages"
       }
            };

     return Ok(configStatus);
 }
      catch (Exception ex)
            {
    return StatusCode(500, new
    {
  error = ex.Message,
     innerError = ex.InnerException?.Message,
       message = "Error checking email configuration"
            });
 }
        }

        private List<string> GetConfigurationIssues(string email, string password, string host, string port)
        {
         var issues = new List<string>();

       if (string.IsNullOrEmpty(email) || email == "NOT SET")
           issues.Add("❌ FromEmail is not configured");
       
  if (string.IsNullOrEmpty(password))
      issues.Add("❌ FromPassword is not configured");
       else if (password.Length != 16)
       issues.Add($"⚠️ Password length is {password.Length} chars (Gmail App Password should be 16)");
            
 if (string.IsNullOrEmpty(host) || host == "NOT SET")
    issues.Add("❌ SmtpHost is not configured");

     if (string.IsNullOrEmpty(port) || port == "NOT SET")
    issues.Add("❌ SmtpPort is not configured");

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
        emailConfigured = !string.IsNullOrEmpty(
   Environment.GetEnvironmentVariable("EmailSettings__FromEmail") ?? 
     _configuration["EmailSettings:FromEmail"]
       )
            });
        }
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
