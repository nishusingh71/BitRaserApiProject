using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace BitRaserApiProject.Services
{
    public interface IEmailService
    {
        Task<bool> SendOtpEmailAsync(string toEmail, string otp, string userName);
     Task<bool> SendPasswordResetSuccessEmailAsync(string toEmail, string userName);
   Task<bool> SendGenericEmailAsync(string toEmail, string subject, string htmlBody);
  }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
      private readonly ILogger<EmailService> _logger;
     private readonly IWebHostEnvironment _env;

        public EmailService(
 IConfiguration configuration,
      ILogger<EmailService> logger,
            IWebHostEnvironment env)
        {
            _configuration = configuration;
_logger = logger;
      _env = env;
        }

    public async Task<bool> SendOtpEmailAsync(string toEmail, string otp, string userName)
        {
       try
            {
          // ✅ Get configuration - Check environment variables first (for Render.com)
       var smtpHost = Environment.GetEnvironmentVariable("EmailSettings__SmtpHost") 
         ?? _configuration["EmailSettings:SmtpHost"] 
 ?? "smtp.gmail.com";
            
             var smtpPortStr = Environment.GetEnvironmentVariable("EmailSettings__SmtpPort") 
          ?? _configuration["EmailSettings:SmtpPort"] 
     ?? "587";
    var smtpPort = int.Parse(smtpPortStr);
   
   var fromEmail = Environment.GetEnvironmentVariable("EmailSettings__FromEmail") 
           ?? _configuration["EmailSettings:FromEmail"];
           
 var fromPassword = Environment.GetEnvironmentVariable("EmailSettings__FromPassword") 
      ?? _configuration["EmailSettings:FromPassword"];
  
      var fromName = Environment.GetEnvironmentVariable("EmailSettings__FromName") 
       ?? _configuration["EmailSettings:FromName"] 
  ?? "DSecure Support";
     
        var timeoutStr = Environment.GetEnvironmentVariable("EmailSettings__Timeout") 
   ?? _configuration["EmailSettings:Timeout"] 
             ?? "120000"; // ✅ Increased to 120 seconds for Render
           var timeout = int.Parse(timeoutStr);

 // ✅ Validate required settings
         if (string.IsNullOrEmpty(fromEmail))
    {
       _logger.LogError("❌ FromEmail is not configured!");
  _logger.LogError("   Set EmailSettings__FromEmail environment variable on Render.com");
   return false;
    }

   if (string.IsNullOrEmpty(fromPassword))
    {
       _logger.LogError("❌ FromPassword is not configured!");
  _logger.LogError("   Set EmailSettings__FromPassword environment variable on Render.com");
 return false;
  }

     _logger.LogInformation("📧 Email Configuration [Environment: {Env}]", _env.EnvironmentName);
 _logger.LogInformation("   Host: {Host}:{Port}, Timeout: {Timeout}ms", smtpHost, smtpPort, timeout);
 _logger.LogInformation("   From: {From}, Password: {PasswordLength} chars", 
        fromEmail, fromPassword.Length);

      // ✅ Create MimeMessage
        var message = new MimeMessage();
   message.From.Add(new MailboxAddress(fromName, fromEmail));
    message.To.Add(new MailboxAddress(userName ?? toEmail, toEmail));
  message.Subject = "Password Reset OTP - DSecure";
    message.Priority = MessagePriority.Urgent;

        var bodyBuilder = new BodyBuilder
     {
      HtmlBody = GetOtpEmailBody(userName ?? "User", otp)
    };
   message.Body = bodyBuilder.ToMessageBody();

     // ✅ Retry logic with better error handling for Render.com
    int maxRetries = 3;
                int retryCount = 0;
 Exception? lastException = null;

         while (retryCount < maxRetries)
  {
   try
      {
     _logger.LogInformation("📧 Attempt {Retry}/{Max} - Sending OTP email to {Email}",
      retryCount + 1, maxRetries, toEmail);

    using var smtpClient = new SmtpClient();
smtpClient.Timeout = timeout;
        
        // ✅ Disable certificate validation for Render.com (if needed)
       smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;

         // ✅ Connect with appropriate security
  if (smtpPort == 465)
  {
           await smtpClient.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
    }
     else
      {
             await smtpClient.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
              }

 _logger.LogInformation("✅ Connected to SMTP server");

    // ✅ Authenticate
       await smtpClient.AuthenticateAsync(fromEmail, fromPassword);
   _logger.LogInformation("✅ Authenticated with SMTP server");

         // ✅ Send email
      await smtpClient.SendAsync(message);
    _logger.LogInformation("✅ Email sent to SMTP server");

      // ✅ Disconnect
            await smtpClient.DisconnectAsync(true);

   _logger.LogInformation("✅ OTP email sent successfully to {Email}", toEmail);
       return true;
       }
           catch (MailKit.Security.AuthenticationException authEx)
     {
         _logger.LogError(authEx, "🔐 SMTP Authentication Failed");
 _logger.LogError("   Check EmailSettings__FromPassword on Render.com");
   lastException = authEx;
  break; // Don't retry auth failures
  }
catch (System.Net.Sockets.SocketException socketEx)
      {
       lastException = socketEx;
        retryCount++;
       _logger.LogWarning("🔌 Socket error (attempt {Retry}): {Message}", retryCount, socketEx.Message);
       
            if (retryCount < maxRetries)
        {
          await Task.Delay(2000 * retryCount);
       }
       }
      catch (TimeoutException timeoutEx)
        {
     lastException = timeoutEx;
  retryCount++;
     _logger.LogWarning("⏱️ Timeout (attempt {Retry}): {Message}", retryCount, timeoutEx.Message);
     
     if (retryCount < maxRetries)
     {
      await Task.Delay(2000 * retryCount);
             }
      }
   catch (Exception ex)
           {
   lastException = ex;
       retryCount++;
       _logger.LogWarning("⚠️ SMTP error (attempt {Retry}): {Message}", retryCount, ex.Message);

     if (retryCount < maxRetries)
    {
  await Task.Delay(2000 * retryCount);
   }
 }
    }

      // All retries failed
    if (lastException != null)
    {
  _logger.LogError(lastException, "❌ All {Max} attempts failed for {Email}", maxRetries, toEmail);
        }

return false;
   }
   catch (Exception ex)
 {
         _logger.LogError(ex, "❌ Unexpected error sending OTP email to {Email}", toEmail);
  return false;
   }
     }

     public async Task<bool> SendPasswordResetSuccessEmailAsync(string toEmail, string userName)
        {
try
   {
var smtpHost = Environment.GetEnvironmentVariable("EmailSettings__SmtpHost") 
   ?? _configuration["EmailSettings:SmtpHost"] 
    ?? "smtp.gmail.com";
         var smtpPort = int.Parse(Environment.GetEnvironmentVariable("EmailSettings__SmtpPort") 
        ?? _configuration["EmailSettings:SmtpPort"] 
      ?? "587");
                var fromEmail = Environment.GetEnvironmentVariable("EmailSettings__FromEmail") 
      ?? _configuration["EmailSettings:FromEmail"];
     var fromPassword = Environment.GetEnvironmentVariable("EmailSettings__FromPassword") 
   ?? _configuration["EmailSettings:FromPassword"];
    var fromName = Environment.GetEnvironmentVariable("EmailSettings__FromName") 
        ?? _configuration["EmailSettings:FromName"] 
 ?? "DSecure Support";
     var timeout = int.Parse(Environment.GetEnvironmentVariable("EmailSettings__Timeout") 
       ?? _configuration["EmailSettings:Timeout"] 
       ?? "120000");

       if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromPassword))
   {
           _logger.LogError("❌ Email settings not configured");
     return false;
  }

    var message = new MimeMessage();
  message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(userName ?? toEmail, toEmail));
      message.Subject = "Password Reset Successful - DSecure";

          var bodyBuilder = new BodyBuilder
            {
     HtmlBody = GetPasswordResetSuccessEmailBody(userName ?? "User")
      };
  message.Body = bodyBuilder.ToMessageBody();

    using var smtpClient = new SmtpClient();
  smtpClient.Timeout = timeout;
     smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;

        if (smtpPort == 465)
    {
          await smtpClient.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
     }
       else
          {
       await smtpClient.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
                }

            await smtpClient.AuthenticateAsync(fromEmail, fromPassword);
  await smtpClient.SendAsync(message);
     await smtpClient.DisconnectAsync(true);

 _logger.LogInformation("✅ Password reset success email sent to {Email}", toEmail);
           return true;
      }
 catch (Exception ex)
     {
           _logger.LogError(ex, "❌ Failed to send password reset success email to {Email}", toEmail);
     return false;
     }
        }

     public async Task<bool> SendGenericEmailAsync(string toEmail, string subject, string htmlBody)
 {
            try
            {
   var smtpHost = Environment.GetEnvironmentVariable("EmailSettings__SmtpHost") 
      ?? _configuration["EmailSettings:SmtpHost"] 
         ?? "smtp.gmail.com";
         var smtpPort = int.Parse(Environment.GetEnvironmentVariable("EmailSettings__SmtpPort") 
?? _configuration["EmailSettings:SmtpPort"] 
         ?? "587");
     var fromEmail = Environment.GetEnvironmentVariable("EmailSettings__FromEmail") 
 ?? _configuration["EmailSettings:FromEmail"];
  var fromPassword = Environment.GetEnvironmentVariable("EmailSettings__FromPassword") 
 ?? _configuration["EmailSettings:FromPassword"];
      var fromName = Environment.GetEnvironmentVariable("EmailSettings__FromName") 
             ?? _configuration["EmailSettings:FromName"] 
  ?? "DSecure Support";
          var timeout = int.Parse(Environment.GetEnvironmentVariable("EmailSettings__Timeout") 
            ?? _configuration["EmailSettings:Timeout"] 
    ?? "120000");

          if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromPassword))
        {
     _logger.LogError("❌ Email settings not configured");
   return false;
         }

      var message = new MimeMessage();
       message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
       message.Subject = subject;

        var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
  message.Body = bodyBuilder.ToMessageBody();

            using var smtpClient = new SmtpClient();
   smtpClient.Timeout = timeout;
smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;

  if (smtpPort == 465)
               {
      await smtpClient.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.SslOnConnect);
    }
            else
  {
      await smtpClient.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
           }

          await smtpClient.AuthenticateAsync(fromEmail, fromPassword);
        await smtpClient.SendAsync(message);
               await smtpClient.DisconnectAsync(true);

    _logger.LogInformation("✅ Generic email sent to {Email} - Subject: {Subject}", toEmail, subject);
           return true;
    }
          catch (Exception ex)
  {
  _logger.LogError(ex, "❌ Failed to send generic email to {Email}", toEmail);
          return false;
    }
  }

        private string GetOtpEmailBody(string userName, string otp)
      {
      return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<style>
body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
.container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
.header {{ text-align: center; color: #1a1a2e; margin-bottom: 30px; }}
.header h1 {{ margin: 0; font-size: 28px; }}
.otp-box {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); border-radius: 12px; padding: 30px; text-align: center; margin: 30px 0; }}
.otp-code {{ font-size: 42px; font-weight: bold; color: white; letter-spacing: 8px; margin: 0; text-shadow: 2px 2px 4px rgba(0,0,0,0.2); }}
.info {{ color: #666; line-height: 1.8; font-size: 16px; }}
.warning {{ background-color: #fff3cd; border: 1px solid #ffc107; border-radius: 8px; padding: 15px; margin-top: 20px; color: #856404; }}
.footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #999; font-size: 14px; }}
</style>
</head>
<body>
<div class='container'>
<div class='header'>
<h1>🔐 Password Reset Request</h1>
</div>
<p class='info'>Hello <strong>{userName}</strong>,</p>
<p class='info'>We received a request to reset your password. Use the following OTP code to proceed:</p>
<div class='otp-box'>
<p style='margin: 0 0 10px 0; color: rgba(255,255,255,0.9); font-size: 14px;'>Your One-Time Password</p>
<p class='otp-code'>{otp}</p>
</div>
<p class='info'>This OTP is valid for <strong>10 minutes</strong>.</p>
<div class='warning'>
⚠️ <strong>Security Notice:</strong> Never share this OTP with anyone. Our team will never ask for your OTP.
</div>
<div class='footer'>
<p>If you didn't request this password reset, please ignore this email.</p>
<p>© {DateTime.Now.Year} DSecure. All rights reserved.</p>
</div>
</div>
</body>
</html>";
    }

        private string GetPasswordResetSuccessEmailBody(string userName)
        {
  return $@"
<!DOCTYPE html>
<html>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<style>
body {{ font-family: 'Segoe UI', Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
.container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 40px; border-radius: 12px; box-shadow: 0 4px 6px rgba(0,0,0,0.1); }}
.header {{ text-align: center; color: #28a745; margin-bottom: 30px; }}
.header h1 {{ margin: 0; font-size: 28px; }}
.success-box {{ background: linear-gradient(135deg, #28a745 0%, #20c997 100%); border-radius: 12px; padding: 30px; text-align: center; margin: 30px 0; }}
.success-icon {{ font-size: 48px; margin-bottom: 15px; }}
.success-text {{ color: white; font-size: 20px; font-weight: bold; margin: 0; }}
.info {{ color: #666; line-height: 1.8; font-size: 16px; }}
.security-tips {{ background-color: #e8f5e9; border-radius: 8px; padding: 20px; margin-top: 20px; }}
.security-tips h3 {{ color: #2e7d32; margin-top: 0; }}
.security-tips ul {{ margin: 0; padding-left: 20px; color: #555; }}
.footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #999; font-size: 14px; }}
</style>
</head>
<body>
<div class='container'>
<div class='header'>
<h1>✅ Password Reset Successful</h1>
</div>
<p class='info'>Hello <strong>{userName}</strong>,</p>
<div class='success-box'>
<div class='success-icon'>🎉</div>
<p class='success-text'>Your password has been successfully reset!</p>
</div>
<p class='info'>You can now log in to your account with your new password.</p>
<div class='security-tips'>
<h3>🔒 Security Tips:</h3>
<ul>
<li>Use a strong, unique password</li>
<li>Enable two-factor authentication if available</li>
<li>Never share your password with anyone</li>
<li>Log out from shared devices</li>
</ul>
</div>
<div class='footer'>
<p>If you didn't make this change, please contact our support team immediately.</p>
<p>© {DateTime.Now.Year} DSecure. All rights reserved.</p>
</div>
</div>
</body>
</html>";
     }
    }
}
