using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;

namespace BitRaserApiProject.Services
{
    public interface IEmailService
{
        System.Threading.Tasks.Task<bool> SendOtpEmailAsync(string toEmail, string otp, string userName);
        System.Threading.Tasks.Task<bool> SendPasswordResetSuccessEmailAsync(string toEmail, string userName);
        System.Threading.Tasks.Task<bool> SendGenericEmailAsync(string toEmail, string subject, string htmlBody);
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

        public async System.Threading.Tasks.Task<bool> SendOtpEmailAsync(string toEmail, string otp, string userName)
        {
 var subject = "Password Reset OTP - DSecure";
  var htmlBody = GetOtpEmailBody(userName ?? "User", otp);

     return await SendEmailAsync(toEmail, userName ?? toEmail, subject, htmlBody, true);
    }

        public async System.Threading.Tasks.Task<bool> SendPasswordResetSuccessEmailAsync(string toEmail, string userName)
        {
    var subject = "Password Reset Successful - DSecure";
      var htmlBody = GetPasswordResetSuccessEmailBody(userName ?? "User");

   return await SendEmailAsync(toEmail, userName ?? toEmail, subject, htmlBody, false);
      }

        public async System.Threading.Tasks.Task<bool> SendGenericEmailAsync(string toEmail, string subject, string htmlBody)
        {
            return await SendEmailAsync(toEmail, toEmail, subject, htmlBody, false);
        }

        /// <summary>
   /// Main email sending method - auto-detects provider
        /// Priority: Brevo API > Brevo SMTP > Gmail SMTP
  /// </summary>
    private async System.Threading.Tasks.Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, bool withRetry)
      {
            var brevoKey = GetBrevoKey();

      // Check if it's a Brevo API key (xkeysib-...) or SMTP key (xsmtpsib-...)
            if (!string.IsNullOrEmpty(brevoKey))
            {
             if (brevoKey.StartsWith("xkeysib-"))
     {
    // Use Brevo REST API
     _logger.LogInformation("📧 Using Brevo API to send email to {Email}", toEmail);
               var result = await SendViaBrevoApiAsync(toEmail, toName, subject, htmlBody);
        if (result) return true;
   
        // Fallback to SMTP if API fails
           _logger.LogWarning("⚠️ Brevo API failed, trying Brevo SMTP...");
        }
          
  if (brevoKey.StartsWith("xsmtpsib-") || brevoKey.StartsWith("xkeysib-"))
  {
          // Use Brevo SMTP
             _logger.LogInformation("📧 Using Brevo SMTP to send email to {Email}", toEmail);
  var result = await SendViaBrevoSmtpAsync(toEmail, toName, subject, htmlBody, withRetry);
          if (result) return true;
    
        _logger.LogWarning("⚠️ Brevo SMTP failed, trying Gmail SMTP...");
      }
     }

          // Fallback to Gmail SMTP
            _logger.LogInformation("📧 Using Gmail SMTP to send email to {Email}", toEmail);
            return await SendViaGmailSmtpAsync(toEmail, toName, subject, htmlBody, withRetry);
        }

        #region Configuration Getters

        private string? GetBrevoKey()
      {
   return Environment.GetEnvironmentVariable("Brevo__ApiKey")
          ?? Environment.GetEnvironmentVariable("BREVO_API_KEY")
    ?? _configuration["Brevo:ApiKey"];
        }

        private string GetBrevoSenderEmail()
{
            return Environment.GetEnvironmentVariable("Brevo__SenderEmail")
    ?? Environment.GetEnvironmentVariable("BREVO_SENDER_EMAIL")
   ?? _configuration["Brevo:SenderEmail"]
        ?? _configuration["EmailSettings:FromEmail"]
     ?? "noreply@dsecure.com";
        }

        private string GetBrevoSenderName()
        {
            return Environment.GetEnvironmentVariable("Brevo__SenderName")
           ?? Environment.GetEnvironmentVariable("BREVO_SENDER_NAME")
       ?? _configuration["Brevo:SenderName"]
     ?? "DSecure Support";
    }

        #endregion

        #region Brevo REST API

        /// <summary>
        /// Send email via Brevo REST API (requires xkeysib-... key)
        /// </summary>
        private async System.Threading.Tasks.Task<bool> SendViaBrevoApiAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
         var apiKey = GetBrevoKey();
     var senderEmail = GetBrevoSenderEmail();
    var senderName = GetBrevoSenderName();

   if (string.IsNullOrEmpty(apiKey) || !apiKey.StartsWith("xkeysib-"))
      {
       _logger.LogWarning("⚠️ Invalid Brevo API key format (expected xkeysib-...)");
 return false;
     }

                _logger.LogInformation("📧 Brevo API - Sender: {Name} <{Email}>", senderName, senderEmail);

            Configuration.Default.ApiKey.Clear();
            Configuration.Default.ApiKey.Add("api-key", apiKey);

              var apiInstance = new TransactionalEmailsApi();

 var sendSmtpEmail = new SendSmtpEmail
            {
          Sender = new SendSmtpEmailSender
          {
            Email = senderEmail,
       Name = senderName
                    },
       To = new List<SendSmtpEmailTo>
       {
     new SendSmtpEmailTo
   {
    Email = toEmail,
        Name = toName
       }
                 },
                Subject = subject,
 HtmlContent = htmlBody
     };

     var result = await apiInstance.SendTransacEmailAsync(sendSmtpEmail);

          _logger.LogInformation("✅ Brevo API email sent! MessageId: {MessageId}", result.MessageId);
            return true;
            }
      catch (ApiException apiEx)
          {
       _logger.LogError(apiEx, "❌ Brevo API Error: {Code} - {Message}", apiEx.ErrorCode, apiEx.Message);
   string errorContent = apiEx.ErrorContent != null ? Convert.ToString(apiEx.ErrorContent) ?? "No content" : "No content";
                _logger.LogError("   Response: {Response}", errorContent);
    return false;
        }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Brevo API failed for {Email}", toEmail);
    return false;
       }
        }

        #endregion

    #region Brevo SMTP

    /// <summary>
     /// Send email via Brevo SMTP (works with xsmtpsib-... key)
        /// SMTP Server: smtp-relay.brevo.com
    /// Port: 587 (TLS) or 465 (SSL)
        /// Login: Your Brevo account email OR your sender email
     /// Password: Your SMTP key (xsmtpsib-...)
        /// </summary>
        private async System.Threading.Tasks.Task<bool> SendViaBrevoSmtpAsync(string toEmail, string toName, string subject, string htmlBody, bool withRetry)
   {
            try
            {
     var brevoKey = GetBrevoKey();
        var senderEmail = GetBrevoSenderEmail();
          var senderName = GetBrevoSenderName();

    if (string.IsNullOrEmpty(brevoKey))
     {
     _logger.LogError("❌ Brevo key is not configured!");
         return false;
      }

     // Brevo SMTP settings
                const string smtpHost = "smtp-relay.brevo.com";
 const int smtpPort = 587;

            // For Brevo SMTP:
                // - Login: Your Brevo account email (the email you registered with)
          // - Password: Your SMTP key (xsmtpsib-...)
   // Note: Some accounts use the sender email as login
      var smtpLogin = senderEmail;  // Try sender email first
      var smtpPassword = brevoKey;

             _logger.LogInformation("📧 Brevo SMTP Configuration:");
   _logger.LogInformation("   Host: {Host}:{Port}", smtpHost, smtpPort);
           _logger.LogInformation("   Login: {Login}", smtpLogin);
      _logger.LogInformation("   Key: {KeyStart}...{KeyEnd}", 
         brevoKey.Substring(0, Math.Min(15, brevoKey.Length)),
        brevoKey.Length > 20 ? brevoKey.Substring(brevoKey.Length - 8) : "");
       _logger.LogInformation("   Sender: {Name} <{Email}>", senderName, senderEmail);
     _logger.LogInformation("   To: {Email}", toEmail);

           var message = new MimeMessage();
       message.From.Add(new MailboxAddress(senderName, senderEmail));
    message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;

           var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
message.Body = bodyBuilder.ToMessageBody();

  int maxRetries = withRetry ? 3 : 1;
           int retryCount = 0;
    Exception? lastException = null;

       while (retryCount < maxRetries)
     {
            try
  {
       _logger.LogInformation("📧 Brevo SMTP attempt {Retry}/{Max}", retryCount + 1, maxRetries);

 using var smtp = new SmtpClient();
    smtp.Timeout = 120000; // 2 minutes
          smtp.ServerCertificateValidationCallback = (s, c, h, e) => true;

       // Connect with TLS on port 587
     _logger.LogInformation("🔌 Connecting to {Host}:{Port}...", smtpHost, smtpPort);
        await smtp.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
             _logger.LogInformation("✅ Connected to Brevo SMTP");

             // Authenticate
          _logger.LogInformation("🔐 Authenticating as {Login}...", smtpLogin);
      await smtp.AuthenticateAsync(smtpLogin, smtpPassword);
      _logger.LogInformation("✅ Authenticated with Brevo SMTP");

    // Send
     _logger.LogInformation("📤 Sending email...");
            await smtp.SendAsync(message);
              _logger.LogInformation("✅ Email sent via Brevo SMTP");

    // Disconnect
      await smtp.DisconnectAsync(true);

            _logger.LogInformation("✅ Brevo SMTP email sent successfully to {Email}", toEmail);
return true;
                 }
                  catch (MailKit.Security.AuthenticationException authEx)
          {
 _logger.LogError(authEx, "🔐 Brevo SMTP Authentication Failed!");
     _logger.LogError("   Login used: {Login}", smtpLogin);
        _logger.LogError("   Possible fixes:");
    _logger.LogError("   1. Verify sender email is verified in Brevo");
         _logger.LogError("   2. Check SMTP key is correct in Brevo dashboard");
      _logger.LogError("   3. Try using your Brevo account email as login");
             lastException = authEx;
          break; // Don't retry auth failures
  }
          catch (Exception ex)
  {
             lastException = ex;
       retryCount++;
          _logger.LogWarning("⚠️ Brevo SMTP attempt {Retry} failed: {Message}", retryCount, ex.Message);

 if (retryCount < maxRetries)
     {
        await System.Threading.Tasks.Task.Delay(2000 * retryCount);
          }
   }
  }

                if (lastException != null)
       {
  _logger.LogError(lastException, "❌ All Brevo SMTP attempts failed for {Email}", toEmail);
          }

   return false;
            }
            catch (Exception ex)
            {
             _logger.LogError(ex, "❌ Brevo SMTP error for {Email}", toEmail);
        return false;
   }
        }

        #endregion

        #region Gmail SMTP (Fallback)

  /// <summary>
        /// Send email via Gmail SMTP (fallback)
        /// </summary>
        private async System.Threading.Tasks.Task<bool> SendViaGmailSmtpAsync(string toEmail, string toName, string subject, string htmlBody, bool withRetry)
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

  var timeoutStr = Environment.GetEnvironmentVariable("EmailSettings__Timeout")
        ?? _configuration["EmailSettings:Timeout"]
        ?? "120000";
             var timeout = int.Parse(timeoutStr);

      if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(fromPassword))
    {
            _logger.LogError("❌ Gmail SMTP credentials not configured!");
       return false;
       }

  _logger.LogInformation("📧 Gmail SMTP Configuration:");
    _logger.LogInformation("   Host: {Host}:{Port}, Timeout: {Timeout}ms", smtpHost, smtpPort, timeout);

   var message = new MimeMessage();
                message.From.Add(new MailboxAddress(fromName, fromEmail));
      message.To.Add(new MailboxAddress(toName, toEmail));
      message.Subject = subject;

     var bodyBuilder = new BodyBuilder { HtmlBody = htmlBody };
     message.Body = bodyBuilder.ToMessageBody();

           int maxRetries = withRetry ? 3 : 1;
     int retryCount = 0;
     Exception? lastException = null;

 while (retryCount < maxRetries)
           {
      try
        {
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

             _logger.LogInformation("✅ Gmail SMTP email sent successfully to {Email}", toEmail);
          return true;
    }
    catch (Exception ex)
           {
         lastException = ex;
      retryCount++;
    _logger.LogWarning("⚠️ Gmail SMTP attempt {Retry}/{Max} failed: {Message}",
    retryCount, maxRetries, ex.Message);

    if (retryCount < maxRetries)
           {
            await System.Threading.Tasks.Task.Delay(2000 * retryCount);
    }
          }
    }

       if (lastException != null)
  {
                    _logger.LogError(lastException, "❌ All Gmail SMTP attempts failed for {Email}", toEmail);
   }

     return false;
      }
            catch (Exception ex)
            {
    _logger.LogError(ex, "❌ Gmail SMTP error for {Email}", toEmail);
                return false;
   }
        }

        #endregion

        #region Email Templates

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

 #endregion
    }
}
