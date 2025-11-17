using System.Net;
using System.Net.Mail;

namespace BitRaserApiProject.Services
{
    public interface IEmailService
    {
      Task<bool> SendOtpEmailAsync(string toEmail, string otp, string userName);
        Task<bool> SendPasswordResetSuccessEmailAsync(string toEmail, string userName);
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
    // ✅ Direct configuration from appsettings.json (no .env dependency)
         var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
    var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        var fromEmail = _configuration["EmailSettings:FromEmail"];
           var fromPassword = _configuration["EmailSettings:FromPassword"];
  var fromName = _configuration["EmailSettings:FromName"] ?? "DSecure Support";
   var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");
    var timeout = int.Parse(_configuration["EmailSettings:Timeout"] ?? "60000");

           // ✅ Validate required settings
     if (string.IsNullOrEmpty(fromEmail))
         {
     _logger.LogError("❌ FromEmail is not configured in appsettings.json!");
       return false;
      }

    if (string.IsNullOrEmpty(fromPassword))
       {
    _logger.LogError("❌ FromPassword is not configured in appsettings.json!");
      _logger.LogError("📝 Please set EmailSettings:FromPassword in:");
         _logger.LogError("   1. appsettings.json (for local)");
          _logger.LogError("   2. appsettings.Production.json (for production)");
            return false;
    }

       _logger.LogInformation("📧 Email Configuration [Environment: {Env}]", _env.EnvironmentName);
  _logger.LogInformation("   Host: {Host}:{Port}, SSL: {SSL}, Timeout: {Timeout}ms", 
          smtpHost, smtpPort, enableSsl, timeout);
      _logger.LogInformation("   From: {From}, Password length: {Length} chars", 
        fromEmail, fromPassword.Length);

        // ✅ Production-ready SMTP configuration
 using var smtpClient = new SmtpClient(smtpHost, smtpPort)
      {
          Credentials = new NetworkCredential(fromEmail, fromPassword),
   EnableSsl = enableSsl,
        DeliveryMethod = SmtpDeliveryMethod.Network,
           UseDefaultCredentials = false,
          Timeout = timeout
     };

      // ✅ Set TLS version for production
      ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

          var mailMessage = new MailMessage
       {
     From = new MailAddress(fromEmail, fromName),
         Subject = "Password Reset OTP - DSecure",
            Body = GetOtpEmailBody(userName, otp),
            IsBodyHtml = true,
    Priority = MailPriority.High
   };

              mailMessage.To.Add(toEmail);

      // ✅ Retry logic for production
     int maxRetries = 3;
        int retryCount = 0;
      Exception? lastException = null;

   while (retryCount < maxRetries)
        {
 try
      {
   _logger.LogInformation("📧 Attempt {Retry}/{Max} - Sending OTP email to {Email}", 
         retryCount + 1, maxRetries, toEmail);
             
   await smtpClient.SendMailAsync(mailMessage);
  
    _logger.LogInformation("✅ OTP email sent successfully to {Email}", toEmail);
              return true;
   }
         catch (SmtpException smtpEx)
  {
              lastException = smtpEx;
        retryCount++;
             
                 _logger.LogWarning("⚠️ SMTP attempt {Retry} failed: {Message}", 
               retryCount, smtpEx.Message);

          if (retryCount < maxRetries)
           {
              await Task.Delay(1000 * retryCount); // Exponential backoff
  }
                    }
       }

                // All retries failed
             if (lastException != null)
                {
      throw lastException;
 }

                return false;
     }
  catch (SmtpException smtpEx)
      {
       _logger.LogError(smtpEx, "❌ SMTP Error sending OTP email to {Email}", toEmail);
                _logger.LogError("   Status: {Status}, Message: {Message}", smtpEx.StatusCode, smtpEx.Message);
      
        if (smtpEx.Message.Contains("Authentication") || smtpEx.Message.Contains("535"))
      {
         _logger.LogError("🔐 SMTP Authentication Failed!");
          _logger.LogError("   ✅ Check EmailSettings:FromPassword in appsettings.json");
_logger.LogError("   ✅ Verify Gmail App Password (16 chars, no spaces)");
          _logger.LogError("   ✅ Generate new: https://myaccount.google.com/apppasswords");
         }
  else if (smtpEx.Message.Contains("timed out") || smtpEx.Message.Contains("timeout"))
   {
     _logger.LogError("⏱️ SMTP Connection Timeout!");
        _logger.LogError("   ✅ Check firewall allows port 587");
        _logger.LogError("   ✅ Try increasing EmailSettings:Timeout in appsettings.json");
                }

      return false;
     }
    catch (Exception ex)
       {
                _logger.LogError(ex, "❌ Unexpected error sending OTP email to {Email}", toEmail);
       _logger.LogError("   Environment: {Env}", _env.EnvironmentName);
       _logger.LogError("   InnerException: {Inner}", ex.InnerException?.Message);
     return false;
      }
        }

        public async Task<bool> SendPasswordResetSuccessEmailAsync(string toEmail, string userName)
        {
            try
         {
    // ✅ Direct configuration from appsettings.json
 var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
     var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
      var fromEmail = _configuration["EmailSettings:FromEmail"];
           var fromPassword = _configuration["EmailSettings:FromPassword"];
                var fromName = _configuration["EmailSettings:FromName"] ?? "DSecure Support";
     var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

   if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromPassword))
         {
          _logger.LogError("❌ Email settings not configured in appsettings.json");
           return false;
 }

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
          {
                Credentials = new NetworkCredential(fromEmail, fromPassword),
      EnableSsl = enableSsl,
            DeliveryMethod = SmtpDeliveryMethod.Network,
 UseDefaultCredentials = false,
                Timeout = 60000
             };

       ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;

      var mailMessage = new MailMessage
                {
     From = new MailAddress(fromEmail, fromName),
           Subject = "Password Reset Successful - DSecure",
                  Body = GetPasswordResetSuccessEmailBody(userName),
           IsBodyHtml = true
   };

mailMessage.To.Add(toEmail);
   await smtpClient.SendMailAsync(mailMessage);
     
      _logger.LogInformation("✅ Password reset success email sent to {Email}", toEmail);
    return true;
    }
 catch (Exception ex)
            {
       _logger.LogError(ex, "❌ Failed to send password reset success email to {Email}", toEmail);
    return false;
    }
        }

        private string GetOtpEmailBody(string userName, string otp)
        {
  return @"
<!DOCTYPE html>
<html>
<head>
<style>
body { font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }
.container { max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; }
.header { text-align: center; color: #333; margin-bottom: 30px; }
.otp-box { background-color: #f8f9fa; border: 2px solid #007bff; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0; }
.otp-code { font-size: 32px; font-weight: bold; color: #007bff; letter-spacing: 5px; }
.info { color: #666; line-height: 1.6; }
</style>
</head>
<body>
<div class='container'>
<div class='header'><h1>🔐 Password Reset Request</h1></div>
<p class='info'>Hello <strong>" + userName + @"</strong>,</p>
<p class='info'>Use the following OTP to reset your password:</p>
<div class='otp-box'>
<p style='margin: 0; color: #666;'>Your OTP Code</p>
<p class='otp-code'>" + otp + @"</p>
</div>
<p class='info'>Valid for 10 minutes. Do not share with anyone.</p>
</div>
</body>
</html>";
     }

private string GetPasswordResetSuccessEmailBody(string userName)
        {
 return @"
<!DOCTYPE html>
<html>
<head>
<style>
body { font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }
.container { max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; }
.header { text-align: center; color: #28a745; margin-bottom: 30px; }
.success-box { background-color: #d4edda; border: 2px solid #28a745; border-radius: 8px; padding: 20px; text-align: center; }
</style>
</head>
<body>
<div class='container'>
<div class='header'><h1>✅ Password Reset Successful</h1></div>
<p>Hello <strong>" + userName + @"</strong>,</p>
<div class='success-box'>
<h2 style='color: #28a745; margin: 0;'>Your password has been successfully reset!</h2>
</div>
<p>You can now log in with your new password.</p>
</div>
</body>
</html>";
        }
    }
}
