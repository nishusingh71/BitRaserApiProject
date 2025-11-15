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
        
      var enableSslStr = Environment.GetEnvironmentVariable("EmailSettings__EnableSsl") 
        ?? _configuration["EmailSettings:EnableSsl"] 
        ?? "true";
                var enableSsl = bool.Parse(enableSslStr);

        var timeoutStr = Environment.GetEnvironmentVariable("EmailSettings__Timeout") 
      ?? _configuration["EmailSettings:Timeout"] 
             ?? "30000";
       var timeout = int.Parse(timeoutStr);

    if (string.IsNullOrEmpty(fromEmail))
          {
           _logger.LogError("FromEmail is not configured!");
         return false;
      }

       if (string.IsNullOrEmpty(fromPassword))
     {
     _logger.LogError("FromPassword is not configured!");
         return false;
        }

      _logger.LogInformation("Email Configuration [Environment: {Env}] - Host: {Host}:{Port}, SSL: {SSL}", 
        _env.EnvironmentName, smtpHost, smtpPort, enableSsl);

     using var smtpClient = new SmtpClient(smtpHost, smtpPort)
   {
            Credentials = new NetworkCredential(fromEmail, fromPassword),
            EnableSsl = enableSsl,
             DeliveryMethod = SmtpDeliveryMethod.Network,
        UseDefaultCredentials = false,
      Timeout = timeout
   };

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

       int maxRetries = 3;
    int retryCount = 0;
       Exception? lastException = null;

      while (retryCount < maxRetries)
       {
        try
   {
           _logger.LogInformation("Attempt {Retry}/{Max} - Sending email to {Email}", 
     retryCount + 1, maxRetries, toEmail);
        
   await smtpClient.SendMailAsync(mailMessage);
    
  _logger.LogInformation("OTP email sent successfully to {Email}", toEmail);
        return true;
           }
           catch (SmtpException smtpEx)
        {
  lastException = smtpEx;
                retryCount++;
  
           _logger.LogWarning("SMTP attempt {Retry} failed: {Message}", 
       retryCount, smtpEx.Message);

   if (retryCount < maxRetries)
{
        await Task.Delay(1000 * retryCount);
       }
   }
       }

     if (lastException != null)
    {
 throw lastException;
       }

     return false;
            }
 catch (SmtpException smtpEx)
       {
  _logger.LogError(smtpEx, "SMTP Error sending email to {Email}. Status: {Status}", 
           toEmail, smtpEx.StatusCode);
    return false;
            }
            catch (Exception ex)
       {
       _logger.LogError(ex, "Error sending OTP email to {Email}", toEmail);
        return false;
       }
        }

        public async Task<bool> SendPasswordResetSuccessEmailAsync(string toEmail, string userName)
        {
      try
            {
        var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
      var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
        var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "nishus877@gmail.com";
 var fromPassword = _configuration["EmailSettings:FromPassword"]?? "nbaoivfshlzgawtj";
    var fromName = _configuration["EmailSettings:FromName"] ?? "DSecure Support";
          var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

          if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromPassword))
  {
     _logger.LogError("Email settings not configured properly");
  return false;
                }

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
           {
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
     EnableSsl = enableSsl,
          DeliveryMethod = SmtpDeliveryMethod.Network,
      UseDefaultCredentials = false,
       Timeout = 30000
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
                
                _logger.LogInformation("Password reset success email sent to {Email}", toEmail);
        return true;
            }
    catch (Exception ex)
          {
 _logger.LogError(ex, "Failed to send password reset success email to {Email}", toEmail);
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
<div class='header'><h1>Password Reset Request</h1></div>
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
<div class='header'><h1>Password Reset Successful</h1></div>
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
