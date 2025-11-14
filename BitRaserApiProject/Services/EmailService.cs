using System.Net;
using System.Net.Mail;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Email service to send OTP emails
    /// </summary>
    public interface IEmailService
    {
   Task<bool> SendOtpEmailAsync(string toEmail, string otp, string userName);
        Task<bool> SendPasswordResetSuccessEmailAsync(string toEmail, string userName);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
      private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
  _logger = logger;
        }

    /// <summary>
        /// Send OTP email to user
        /// </summary>
        public async Task<bool> SendOtpEmailAsync(string toEmail, string otp, string userName)
        {
       try
       {
      // Get configuration from both sources
            var smtpHost = Environment.GetEnvironmentVariable("EmailSettings__SmtpHost") 
        ?? _configuration["EmailSettings:SmtpHost"] 
    ?? "smtp.gmail.com";
        
 var smtpPort = int.Parse(Environment.GetEnvironmentVariable("EmailSettings__SmtpPort") 
      ?? _configuration["EmailSettings:SmtpPort"] 
   ?? "587");
     
       var fromEmail = Environment.GetEnvironmentVariable("EmailSettings__FromEmail") 
    ?? _configuration["EmailSettings:FromEmail"] 
   ?? "noreply@dsecuretech.com";
       
            var fromPassword = Environment.GetEnvironmentVariable("EmailSettings__FromPassword") 
    ?? _configuration["EmailSettings:FromPassword"] 
          ?? "";
  
         var fromName = Environment.GetEnvironmentVariable("EmailSettings__FromName") 
       ?? _configuration["EmailSettings:FromName"] 
        ?? "DSecure Support";
  
  var enableSsl = bool.Parse(Environment.GetEnvironmentVariable("EmailSettings__EnableSsl") 
   ?? _configuration["EmailSettings:EnableSsl"] 
      ?? "true");

       _logger.LogInformation("📧 Email Configuration - Host: {Host}, Port: {Port}, From: {From}, SSL: {SSL}", 
          smtpHost, smtpPort, fromEmail, enableSsl);

     if (string.IsNullOrEmpty(fromPassword))
      {
     _logger.LogError("❌ Email password is empty! Check .env file: EmailSettings__FromPassword");
      _logger.LogError("❌ Also check appsettings.json: EmailSettings:FromPassword");
   return false;
        }

   _logger.LogInformation("📧 Password loaded: {Length} characters", fromPassword.Length);

  _logger.LogInformation("📧 Attempting to send OTP email to {Email} from {FromEmail}", toEmail, fromEmail);

    using var smtpClient = new SmtpClient(smtpHost, smtpPort)
       {
    Credentials = new NetworkCredential(fromEmail, fromPassword),
    EnableSsl = enableSsl,
    DeliveryMethod = SmtpDeliveryMethod.Network,
 UseDefaultCredentials = false,
        Timeout = 30000 // 30 seconds timeout
};

   var mailMessage = new MailMessage
    {
        From = new MailAddress(fromEmail, fromName),
  Subject = "Password Reset OTP - DSecure",
   Body = GetOtpEmailBody(userName, otp),
   IsBodyHtml = true
    };

     mailMessage.To.Add(toEmail);

 _logger.LogInformation("📧 Connecting to SMTP server {Host}:{Port}...", smtpHost, smtpPort);
    await smtpClient.SendMailAsync(mailMessage);
  _logger.LogInformation("✅ OTP email sent successfully to {Email}", toEmail);
     return true;
      }
   catch (SmtpException smtpEx)
    {
  _logger.LogError(smtpEx, "❌ SMTP Error sending OTP email to {Email}. Status: {Status}, Message: {Message}", 
     toEmail, smtpEx.StatusCode, smtpEx.Message);
      
     if (smtpEx.Message.Contains("Authentication"))
      {
    _logger.LogError("🔐 SMTP Authentication Failed! Please check:");
   _logger.LogError("   1. .env file: EmailSettings__FromPassword = {Password}", 
    Environment.GetEnvironmentVariable("EmailSettings__FromPassword") ?? "NOT SET IN ENV");
    _logger.LogError("   2. appsettings.json: EmailSettings:FromPassword = {Password}", 
_configuration["EmailSettings:FromPassword"] ?? "NOT SET IN CONFIG");
  _logger.LogError("   3. Gmail App Password should be 16 characters (NO SPACES!)");
      _logger.LogError("   4. Generate new: https://myaccount.google.com/apppasswords");
   }
    
       return false;
      }
  catch (Exception ex)
    {
  _logger.LogError(ex, "❌ General error sending OTP email to {Email}", toEmail);
  return false;
    }
        }

        /// <summary>
        /// Send password reset success email
        /// </summary>
     public async Task<bool> SendPasswordResetSuccessEmailAsync(string toEmail, string userName)
  {
  try
   {
     var smtpHost = _configuration["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
            var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
     var fromEmail = _configuration["EmailSettings:FromEmail"] ?? "nishus877@gmail.com";
     var fromPassword = _configuration["EmailSettings:FromPassword"] ?? "";
    var fromName = _configuration["EmailSettings:FromName"] ?? "DSecure Support";
            var enableSsl = bool.Parse(_configuration["EmailSettings:EnableSsl"] ?? "true");

       using var smtpClient = new SmtpClient(smtpHost, smtpPort)
   {
            Credentials = new NetworkCredential(fromEmail, fromPassword),
    EnableSsl = enableSsl,
      DeliveryMethod = SmtpDeliveryMethod.Network,
  UseDefaultCredentials = false
    };

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
 return $@"
<!DOCTYPE html>
<html>
<head>
  <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #333; margin-bottom: 30px; }}
        .otp-box {{ background-color: #f8f9fa; border: 2px solid #007bff; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0; }}
        .otp-code {{ font-size: 32px; font-weight: bold; color: #007bff; letter-spacing: 5px; }}
        .info {{ color: #666; line-height: 1.6; }}
        .warning {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; color: #856404; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
        <h1>🔐 Password Reset Request</h1>
        </div>
        
        <p class='info'>Hello <strong>{userName}</strong>,</p>
        
        <p class='info'>
            You have requested to reset your password. Please use the following One-Time Password (OTP) to proceed:
        </p>
   
        <div class='otp-box'>
   <p style='margin: 0; color: #666; font-size: 14px;'>Your OTP Code</p>
   <p class='otp-code'>{otp}</p>
        </div>
        
        <div class='warning'>
        <p style='margin: 0;'><strong>⚠️ Important:</strong></p>
            <ul style='margin: 10px 0 0 0; padding-left: 20px;'>
      <li>This OTP is valid for <strong>10 minutes</strong></li>
 <li>Maximum <strong>5 attempts</strong> allowed</li>
           <li>Do not share this OTP with anyone</li>
     <li>If you didn't request this, please ignore this email</li>
  </ul>
    </div>
        
  <p class='info'>
            If you have any questions or didn't request this password reset, please contact our support team immediately.
        </p>
        
      <div class='footer'>
            <p>© 2025 DSecure. All rights reserved.</p>
            <p>This is an automated email. Please do not reply to this message.</p>
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
    <style>
        body {{ font-family: Arial, sans-serif; background-color: #f4f4f4; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background-color: white; padding: 30px; border-radius: 10px; box-shadow: 0 2px 10px rgba(0,0,0,0.1); }}
        .header {{ text-align: center; color: #28a745; margin-bottom: 30px; }}
        .success-box {{ background-color: #d4edda; border: 2px solid #28a745; border-radius: 8px; padding: 20px; text-align: center; margin: 20px 0; }}
     .info {{ color: #666; line-height: 1.6; }}
        .footer {{ text-align: center; color: #999; font-size: 12px; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; }}
    </style>
</head>
<body>
    <div class='container'>
    <div class='header'>
   <h1>✅ Password Reset Successful</h1>
      </div>
        
     <p class='info'>Hello <strong>{userName}</strong>,</p>
   
        <div class='success-box'>
    <h2 style='color: #28a745; margin: 0;'>Your password has been successfully reset!</h2>
   </div>
        
        <p class='info'>
        You can now log in to your account using your new password. If you did not make this change, please contact our support team immediately.
        </p>
    
        <p class='info'>
   For security reasons, we recommend:
  </p>
        <ul class='info'>
            <li>Using a strong, unique password</li>
            <li>Enabling two-factor authentication if available</li>
         <li>Never sharing your password with anyone</li>
 </ul>
        
      <div class='footer'>
         <p>© 2025 DSecure. All rights reserved.</p>
       <p>This is an automated email. Please do not reply to this message.</p>
        </div>
    </div>
</body>
</html>";
   }
    }
}
