using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using sib_api_v3_sdk.Api;
using sib_api_v3_sdk.Client;
using sib_api_v3_sdk.Model;
using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace DSecureApi.Services
{
    public interface IEmailService
    {
        System.Threading.Tasks.Task<bool> SendOtpEmailAsync(string toEmail, string otp, string userName);
        System.Threading.Tasks.Task<bool> SendPasswordResetSuccessEmailAsync(string toEmail, string userName);
        System.Threading.Tasks.Task<bool> SendGenericEmailAsync(string toEmail, string subject, string htmlBody);
        System.Threading.Tasks.Task<bool> SendAccountCreatedEmailAsync(string toEmail, string userName, string tempPassword, string loginUrl, string? productName = null, int? quantity = null, decimal? price = null, List<string>? licenseKeys = null, string? invoiceUrl = null);
        System.Threading.Tasks.Task<bool> SendPaymentFailedEmailAsync(string toEmail, string userName, string productName, decimal? amount = null);
        System.Threading.Tasks.Task<bool> SendPaymentSuccessEmailAsync(string toEmail, string userName, string productName, decimal amount, int quantity = 1, List<string>? licenseKeys = null, string status = "Paid", string? invoiceUrl = null);
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
        /// Send account created email with temporary password
        /// ⚠️ SECURITY: tempPassword is ONLY used in email, NEVER logged
        /// </summary>
        public async System.Threading.Tasks.Task<bool> SendAccountCreatedEmailAsync(string toEmail, string userName, string tempPassword, string loginUrl, string? productName = null, int? quantity = null, decimal? price = null, List<string>? licenseKeys = null, string? invoiceUrl = null)
        {
            var subject = "Your Account is Ready - DSecure";
            var htmlBody = GetAccountCreatedEmailBody(userName ?? "User", toEmail, tempPassword, loginUrl, productName, quantity, price, licenseKeys, invoiceUrl);
            
            // Pass invoiceUrl as attachmentUrl if present
            return await SendEmailAsync(toEmail, userName ?? toEmail, subject, htmlBody, true, invoiceUrl);
        }

        public async System.Threading.Tasks.Task<bool> SendPaymentFailedEmailAsync(string toEmail, string userName, string productName, decimal? amount = null)
        {
            var subject = "Payment Failed - DSecure";
            var htmlBody = GetPaymentFailedEmailBody(userName ?? "Customer", toEmail, productName, amount);
            
            return await SendEmailAsync(toEmail, userName ?? toEmail, subject, htmlBody, true);
        }

        public async System.Threading.Tasks.Task<bool> SendPaymentSuccessEmailAsync(string toEmail, string userName, string productName, decimal amount, int quantity = 1, List<string>? licenseKeys = null, string status = "Paid", string? invoiceUrl = null)
        {
            var subject = "Payment Successful - DSecure";
            var htmlBody = GetPaymentSuccessEmailBody(userName ?? "Customer", toEmail, productName, amount, quantity, licenseKeys, status, invoiceUrl);
            
            // Pass invoiceUrl as attachmentUrl if present
            return await SendEmailAsync(toEmail, userName ?? toEmail, subject, htmlBody, true, invoiceUrl);
        }

        /// <summary>
   /// Main email sending method - auto-detects provider
        /// Priority: SendGrid API (Render-friendly) > Brevo API > FormSubmit > SMTP fallbacks
  /// </summary>
    private async System.Threading.Tasks.Task<bool> SendEmailAsync(string toEmail, string toName, string subject, string htmlBody, bool withRetry = true, string? attachmentUrl = null)
      {
            // Download attachment if URL provided
            byte[]? attachmentData = null;
            string? attachmentName = null;

            if (!string.IsNullOrEmpty(attachmentUrl))
            {
                try
                {
                    _logger.LogInformation("📎 Downloading attachment from: {Url}", attachmentUrl);
                    using var httpClient = new HttpClient();
                    // Add User-Agent to avoid 403 Forbidden from some servers
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "DSecureApi/1.0");
                    attachmentData = await httpClient.GetByteArrayAsync(attachmentUrl);
                    
                    try 
                    {
                        var uri = new Uri(attachmentUrl);
                        attachmentName = System.IO.Path.GetFileName(uri.LocalPath);
                    } 
                    catch {}
                    
                    if (string.IsNullOrEmpty(attachmentName)) attachmentName = "Invoice.pdf";
                    if (!attachmentName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)) attachmentName += ".pdf";

                    _logger.LogInformation("✅ Attachment downloaded: {Name} ({Size} bytes)", attachmentName, attachmentData.Length);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Failed to download attachment from {Url}", attachmentUrl);
                }
            }

            // 🚀 PRIORITY 1: SendGrid API (professional, 100 emails/day free, works on Render)
            var sendGridKey = GetSendGridKey();
            if (!string.IsNullOrEmpty(sendGridKey))
            {
                _logger.LogInformation("📧 Attempting to send email via SendGrid API to {Email}", toEmail);
                var sendGridResult = await SendViaSendGridAsync(toEmail, toName, subject, htmlBody, null, null, attachmentData, attachmentName);
                if (sendGridResult)
                {
                    _logger.LogInformation("✅ Email sent successfully via SendGrid");
                    return true;
                }
                _logger.LogWarning("⚠️ SendGrid failed, trying other providers...");
            }
            
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
            return await SendViaGmailSmtpAsync(toEmail, toName, subject, htmlBody, withRetry, attachmentData, attachmentName);
        }

        #region Configuration Getters

        private string? GetBrevoKey()
      {
   return Environment.GetEnvironmentVariable("Brevo__ApiKey")
          ?? Environment.GetEnvironmentVariable("BREVO_API_KEY")
    ?? _configuration["Brevo:ApiKey"];
        }

        private string? GetSendGridKey()
        {
            return Environment.GetEnvironmentVariable("SendGrid__ApiKey")
                ?? Environment.GetEnvironmentVariable("SENDGRID_API_KEY")
                ?? _configuration["SendGrid:ApiKey"];
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
        private async System.Threading.Tasks.Task<bool> SendViaBrevoSmtpAsync(string toEmail, string toName, string subject, string htmlBody, bool withRetry, byte[]? attachmentData = null, string? attachmentName = null)
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
           
           if (attachmentData != null && !string.IsNullOrEmpty(attachmentName))
           {
               bodyBuilder.Attachments.Add(attachmentName, attachmentData);
           }

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
        private async System.Threading.Tasks.Task<bool> SendViaGmailSmtpAsync(string toEmail, string toName, string subject, string htmlBody, bool withRetry, byte[]? attachmentData = null, string? attachmentName = null)
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

     if (attachmentData != null && !string.IsNullOrEmpty(attachmentName))
     {
         bodyBuilder.Attachments.Add(attachmentName, attachmentData);
     }

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

        /// <summary>
        /// Send email via SendGrid API (professional email service)
        /// 100 emails/day free tier, works on Render (HTTP API)
        /// </summary>
        /// <summary>
        /// Send email via SendGrid with optional CC and BCC support
        /// CC - visible to recipient (customer + manager)
        /// BCC - hidden from recipient (internal audit, admin monitoring)
        /// </summary>
        private async System.Threading.Tasks.Task<bool> SendViaSendGridAsync(
            string toEmail, 
            string toName, 
            string subject, 
            string htmlBody,
            List<string>? ccEmails = null,
            List<string>? bccEmails = null,
            byte[]? attachmentData = null, 
            string? attachmentName = null)
        {
            try
            {
                var apiKey = GetSendGridKey();
                if (string.IsNullOrEmpty(apiKey))
                {
                    _logger.LogWarning("SendGrid API key not configured");
                    return false;
                }

                var client = new SendGridClient(apiKey);
                
                // Get sender info from config
                var fromEmail = Environment.GetEnvironmentVariable("SendGrid__SenderEmail")
                    ?? Environment.GetEnvironmentVariable("SENDGRID_SENDER_EMAIL")
                    ?? _configuration["SendGrid:SenderEmail"]
                    ?? _configuration["EmailSettings:FromEmail"]
                    ?? "noreply@dsecure.com";

                var fromName = Environment.GetEnvironmentVariable("SendGrid__SenderName")
                    ?? Environment.GetEnvironmentVariable("SENDGRID_SENDER_NAME")
                    ?? _configuration["SendGrid:SenderName"]
                    ?? "DSecure";

                var from = new EmailAddress(fromEmail, fromName);
                var to = new EmailAddress(toEmail, toName);
                
                // Create message with basic fields
                var msg = new SendGridMessage
                {
                    From = from,
                    Subject = subject,
                    HtmlContent = htmlBody,
                    PlainTextContent = htmlBody
                };
                
                // Add primary recipient
                msg.AddTo(to);
                
                // Add CC recipients (visible to primary recipient)
                if (ccEmails != null && ccEmails.Any())
                {
                    foreach (var ccEmail in ccEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                    {
                        msg.AddCc(new EmailAddress(ccEmail.Trim()));
                    }
                    _logger.LogInformation("📋 CC added: {CcEmails}", string.Join(", ", ccEmails));
                }
                
                // Add BCC recipients (hidden from primary recipient - for audit/monitoring)
                if (bccEmails != null && bccEmails.Any())
                {
                    foreach (var bccEmail in bccEmails.Where(e => !string.IsNullOrWhiteSpace(e)))
                    {
                        msg.AddBcc(new EmailAddress(bccEmail.Trim()));
                    }
                    _logger.LogInformation("🔒 BCC added (hidden): {Count} recipients", bccEmails.Count);
                }
                
                // Add attachment if present
                if (attachmentData != null && !string.IsNullOrEmpty(attachmentName))
                {
                    msg.AddAttachment(attachmentName, Convert.ToBase64String(attachmentData));
                    _logger.LogInformation("📎 Added attachment to SendGrid: {Name}", attachmentName);
                }



                _logger.LogInformation("🚀 Sending via SendGrid to {Email}", toEmail);
                
                var response = await client.SendEmailAsync(msg);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ SendGrid email sent successfully to {Email}", toEmail);
                    return true;
                }
                else
                {
                    var responseBody = await response.Body.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ SendGrid returned {Status}: {Response}", 
                        response.StatusCode, responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ SendGrid error for {Email}", toEmail);
                return false;
            }
        }
        
        /// <summary>
        /// Get default BCC emails for internal monitoring/audit
        /// Configure in .env or appsettings.json
        /// </summary>
        private List<string> GetDefaultBccEmails()
        {
            var bccList = new List<string>();
            
            // Get from environment variables or config
            var bccConfig = Environment.GetEnvironmentVariable("SENDGRID_BCC_EMAILS")
                ?? _configuration["SendGrid:BccEmails"]
                ?? "";
            
            if (!string.IsNullOrEmpty(bccConfig))
            {
                bccList = bccConfig.Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(e => e.Trim())
                    .Where(e => !string.IsNullOrEmpty(e))
                    .ToList();
            }
            
            return bccList;
        }

        /// <summary>
        /// Send email via FormSubmit.co (HTTP-based, works on Render)
        /// No SMTP ports required - uses HTTP POST
        /// </summary>
        private async System.Threading.Tasks.Task<bool> SendViaFormSubmitAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            try
            {
                using var httpClient = new HttpClient();
                
                // FormSubmit.co endpoint
                var formSubmitUrl = $"https://formsubmit.co/{toEmail}";
                
                // Prepare form data
                var formData = new Dictionary<string, string>
                {
                    { "_subject", subject },
                    { "name", toName },
                    { "email", toEmail },
                    { "message", htmlBody },
                    { "_template", "box" }, // Use FormSubmit's box template for HTML
                    { "_captcha", "false" }, // Disable captcha for API use
                    { "_autoresponse", "Thank you for your inquiry. We'll get back to you soon." }
                };

                var content = new FormUrlEncodedContent(formData);
                
                _logger.LogInformation("🚀 Sending email via FormSubmit.co to {Email}", toEmail);
                
                var response = await httpClient.PostAsync(formSubmitUrl, content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("✅ FormSubmit.co email sent successfully to {Email}", toEmail);
                    return true;
                }
                else
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("⚠️ FormSubmit.co returned {Status}: {Response}", 
                        response.StatusCode, responseBody);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ FormSubmit.co error for {Email}", toEmail);
                return false;
            }
        }

        #endregion

        #region Email Templates

        private string GetOtpEmailBody(string userName, string otp)
        {
 return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Password Reset OTP - DSecure</title>
<style>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
* {{ margin: 0; padding: 0; box-sizing: border-box; }}
body {{ font-family: 'Inter', -apple-system, BlinkMacSystemFont, 'Segoe UI', Arial, sans-serif; background: #f8fafc; margin: 0; padding: 0; }}
.email-container {{ max-width: 600px; margin: 0 auto; background: #ffffff; }}
.header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 48px 40px; text-align: center; }}
.header-icon {{ font-size: 64px; margin-bottom: 16px; animation: pulse 2s infinite; }}
@keyframes pulse {{ 0%, 100% {{ transform: scale(1); }} 50% {{ transform: scale(1.08); }} }}
.header h1 {{ color: #ffffff; font-size: 26px; font-weight: 700; margin: 0; }}
.content {{ padding: 40px; background: #ffffff; }}
.greeting {{ font-size: 18px; color: #1e293b; margin-bottom: 16px; font-weight: 500; }}
.message {{ font-size: 15px; color: #64748b; line-height: 1.7; margin-bottom: 32px; }}
.otp-box {{ background: linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%); border: 2px solid #10b981; border-radius: 12px; padding: 32px; text-align: center; margin: 32px 0; }}
.otp-label {{ font-size: 13px; text-transform: uppercase; letter-spacing: 1.2px; color: #059669; font-weight: 600; margin-bottom: 12px; }}
.otp-code {{ font-size: 42px; font-weight: 700; color: #10b981; letter-spacing: 10px; margin: 12px 0; font-family: 'Courier New', monospace; user-select: all; }}
.timer-badge {{ background: #ffffff; color: #f59e0b; padding: 10px 20px; border-radius: 24px; font-size: 14px; font-weight: 600; display: inline-flex; align-items: center; gap: 6px; margin-top: 16px; border: 2px solid #fef3c7; }}
.security-notice {{ background: #fffbeb; border-left: 4px solid #f59e0b; padding: 20px; border-radius: 6px; margin: 32px 0; }}
.security-notice p {{ color: #92400e; font-size: 14px; line-height: 1.6; margin: 0; }}
.footer {{ background: #f8fafc; padding: 32px 40px; text-align: center; border-top: 1px solid #e2e8f0; }}
.footer p {{ color: #64748b; font-size: 13px; margin: 6px 0; }}
@media only screen and (max-width: 600px) {{
    .header {{ padding: 36px 24px; }}
    .content {{ padding: 28px 24px; }}
    .otp-code {{ font-size: 36px; letter-spacing: 6px; }}
}}
</style>
</head>
<body>
<div class='email-container'>
<div class='header'>

<h1>Password Reset Request</h1>
</div>
<div class='content'>
<p class='greeting'>Hello {userName},</p>
<p class='message'>We received a request to reset your password. To continue, please use the One-Time Password below:</p>
<div class='otp-box'>
<div class='otp-label'>Your One-Time Password</div>
<div class='otp-code'>{otp}</div>
<div class='timer-badge'>Valid for 10 minutes</div>
</div>
<div class='security-notice'>
<p><strong>Security Notice:</strong> Never share this OTP with anyone, including our support team. We will never ask for your OTP via email, phone, or chat.</p>
</div>
<p class='message'>If you didn't request this password reset, please ignore this email. Your password will remain unchanged.</p>
</div>
<div class='footer'>
<p><strong>Need Help?</strong> Contact us at Support@dsecuretech.com</p>
<p>© {DateTime.Now.Year} D-Secure Technologies Pvt Ltd. All rights reserved.</p>
</div>
</div>
</body>
</html>";
     }

        private string GetPasswordResetSuccessEmailBody(string userName)
        {
    return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Password Reset Successful - DSecure</title>
<style>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
* {{ margin: 0; padding: 0; box-sizing: border-box; }}
body {{ font-family: 'Inter', sans-serif; background: #f8fafc; margin: 0; padding: 0; }}
.email-container {{ max-width: 600px; margin: 0 auto; background: #ffffff; }}
.header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 48px 40px; text-align: center; }}
.header-icon {{ font-size: 64px; margin-bottom: 16px; animation: bounce 1s ease; }}
@keyframes bounce {{ 0%, 100% {{ transform: translateY(0); }} 50% {{ transform: translateY(-12px); }} }}
.header h1 {{ color: #ffffff; font-size: 26px; font-weight: 700; margin: 0; }}
.content {{ padding: 40px; background: #ffffff; }}
.greeting {{ font-size: 18px; color: #1e293b; margin-bottom: 16px; font-weight: 500; }}
.message {{ font-size: 15px; color: #64748b; line-height: 1.7; margin-bottom: 24px; }}
.success-card {{ background: linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%); border: 2px solid #10b981; border-radius: 12px; padding: 24px; margin: 24px 0; text-align: center; }}
.success-card h2 {{ color: #10b981; font-size: 20px; margin-bottom: 8px; font-weight: 600; }}
.success-card p {{ color: #065f46; margin: 0; font-size: 14px; }}
.cta-button {{ display: inline-block; background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: #fff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; margin: 20px 0; }}
.security-tips {{ background: #f8fafc; border: 1px solid #e2e8f0; border-radius: 12px; padding: 24px; margin: 24px 0; }}
.security-tips h3 {{ color: #1e293b; font-size: 16px; margin-bottom: 16px; font-weight: 600; }}
.security-tips ul {{ list-style: none; padding: 0; margin: 0; }}
.security-tips li {{ padding: 10px 0; color: #64748b; display: flex; align-items: center; gap: 10px; font-size: 14px; }}
.security-tips li::before {{ content: '✓'; color: #10b981; font-weight: bold; font-size: 16px; }}
.footer {{ background: #f8fafc; padding: 32px 40px; text-align: center; border-top: 1px solid #e2e8f0; }}
.footer p {{ color: #64748b; font-size: 13px; margin: 6px 0; }}
@media only screen and (max-width: 600px) {{
    .header {{ padding: 36px 24px; }}
    .content {{ padding: 28px 24px; }}
}}
</style>
</head>
<body>
<div class='email-container'>
<div class='header'>

<h1>Password Reset Successful!</h1>
</div>
<div class='content'>
<p class='greeting'>Hello {userName},</p>
<div class='success-card'>
<h2>All Set!</h2>
<p>Your password has been successfully reset. You can now access your account with your new credentials.</p>
</div>
<p class='message'>Your account security is our top priority. You can now log in with your new password and continue using D-Secure.</p>
<div style='text-align: center;'>
<a href='https://dsecuretech.com/login' class='cta-button' style='color: #ffffff;'>Go to Dashboard</a>
</div>
<div class='security-tips'>
<h3>Security Best Practices</h3>
<ul>
<li>Use a unique password for each online account</li>
<li>Enable two-factor authentication for extra security</li>
<li>Never share your password with anyone</li>
<li>Update your password regularly every 3-6 months</li>
</ul>
</div>
<p class='message'>If you didn't make this change, please contact our support team immediately at Support@dsecuretech.com</p>
</div>
<div class='footer'>
<p><strong>Need Help?</strong> We're here for you 24/7</p>
<p>© {DateTime.Now.Year} D-Secure Technologies Pvt Ltd. All rights reserved.</p>
</div>
</div>
</body>
</html>";
        }

 #endregion

        #region Account Created Email Template

        private string GetAccountCreatedEmailBody(string userName, string email, string tempPassword, string loginUrl, string? productName = null, int? quantity = null, decimal? price = null, List<string>? licenseKeys = null, string? invoiceUrl = null)
        {
            var licenseKeysHtml = "";
            if (licenseKeys != null && licenseKeys.Any())
            {
                var keysListHtml = string.Join("", licenseKeys.Select(k => $"<div style='background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%); border: 2px solid #10b981; border-radius: 8px; padding: 12px 16px; margin: 8px 0; font-family: \"Courier New\", monospace; font-size: 15px; font-weight: 600; color: #047857; text-align: center; box-shadow: 0 2px 8px rgba(16,185,129,0.1);'>{k}</div>"));
                licenseKeysHtml = $@"
<div style='background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%); border: 2px solid #10b981; border-radius: 12px; padding: 24px; margin: 30px 0;'>
<h3 style='margin: 0 0 16px 0; color: #047857; font-size: 20px; text-align: center;'>Your License Keys</h3>
<p style='text-align: center; color: #065f46; margin-bottom: 16px; font-size: 14px;'>Generated {licenseKeys.Count} premium license key(s)</p>
{keysListHtml}
<p style='text-align: center; font-size: 13px; color: #059669; margin: 16px 0 0 0; font-weight: 500;'>Save these keys securely - you'll need them to activate your software!</p>
</div>";
            }

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Welcome to DSecure</title>
<style>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
* {{ margin: 0; padding: 0; box-sizing: border-box; }}
body {{ font-family: 'Inter', sans-serif; background: #f8fafc; margin: 0; padding: 0; }}
.email-container {{ max-width: 600px; margin: 0 auto; background: #ffffff; }}
.header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 48px 40px; text-align: center; }}
.header::before {{ content: '🎉'; font-size: 72px; display: block; margin-bottom: 16px; animation: celebrate 2s ease; }}
@keyframes celebrate {{ 0% {{ transform: scale(0) rotate(0deg); }} 50% {{ transform: scale(1.2) rotate(180deg); }} 100% {{ transform: scale(1) rotate(360deg); }} }}
.header h1 {{ color: #fff; font-size: 28px; font-weight: 700; margin: 0; text-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
.content {{ padding: 40px 30px; }}
.welcome-badge {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); color: #fff; padding: 12px 24px; border-radius:20px; display: inline-block; font-size: 14px; font-weight: 600; margin-bottom: 24px; box-shadow: 0 4px 12px rgba(16,185,129,0.3); }}
.greeting {{ font-size: 18px; color: #1a1a2e; margin-bottom: 20px; line-height: 1.6; }}
.greeting strong {{ color: #10b981; font-weight: 600; }}
.message {{ font-size: 16px; color: #4a5568; line-height: 1.8; margin-bottom: 24px; }}
.credentials-card {{ background: linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%); border: 2px solid #10b981; border-radius: 12px; padding: 24px; margin: 24px 0; }}
.credentials-card h3 {{ color: #10b981; font-size: 16px; margin-bottom: 16px; text-align: center; }}
.cred-item {{ margin: 12px 0; }}
.cred-label {{ font-size: 12px; color: #6b7280; text-transform: uppercase; letter-spacing: 0.5px; margin-bottom: 4px; }}
.cred-value {{ font-family: 'Courier New', monospace; font-size: 16px; font-weight: 600; color: #1a1a2e; background: #fff; padding: 10px 14px; border-radius: 6px; display: inline-block; box-shadow: 0 2px 4px rgba(0,0,0,0.05); user-select: all; }}
.cta-button {{ display: inline-block; background: linear-gradient(135deg, #059669 0%, #047857 100%); color: #ffffff; padding: 16px 40px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 16px; margin: 20px 0; }}
.cta-button:hover {{ box-shadow: 0 6px 16px rgba(5,150,105,0.4); }}
.checklist {{ background: #f8f9fa; border-radius: 12px; padding: 24px; margin: 24px 0; }}
.checklist h3 {{ color: #1a1a2e; font-size: 16px; margin-bottom: 16px; display: flex; align-items: center; gap: 8px; }}
.checklist h3::before {{ content: '✓'; background: #10b981; color: #fff; width: 24px; height: 24px; border-radius: 50%; display: flex; align-items: center; justify-content: center; font-size: 14px; font-weight: bold; }}
.checklist ul {{ list-style: none; padding: 0; }}
.checklist li {{ padding: 10px 0; color: #4a5568; display: flex; align-items: flex-start; gap: 10px; border-bottom: 1px solid #e5e7eb; }}
.checklist li:last-child {{ border-bottom: none; }}
.checklist li::before {{ content: '→'; color: #10b981; font-weight: bold; flex-shrink: 0; }}
.warning-box {{ background: #fef3c7; border-left: 4px solid #f59e0b; padding: 16px 20px; border-radius: 8px; margin: 24px 0; }}
.warning-box p {{ color: #92400e; font-size: 14px; line-height: 1.6; margin: 0; }}
.footer {{ background: #f8f9fa; padding: 30px; text-align: center; }}
.footer p {{ color: #6b7280; font-size: 14px; margin: 8px 0; }}
@media only screen and (max-width: 600px) {{
    .header {{ padding: 40px 20px; }}
    .header h1 {{ font-size: 24px; }}
    .content {{ padding: 30px 20px; }}
    .cta-button {{ padding: 14px 32px; font-size: 15px; }}
}}
</style>
</head>
<body>
<div class='email-wrapper'>
<div class='header'>
<h1>Welcome to DSecure!</h1>
</div>
<div class='content'>
<div style='text-align: center;'>
<div class='welcome-badge'>Account Successfully Created</div>
</div>
<p class='greeting'>Hello <strong>{userName}</strong>,</p>
<p class='message'>Welcome aboard! Your payment was successful and your account is now active. We're excited to have you join the DSecure family.</p>
<div class='credentials-card'>
<h3>Your Login Credentials</h3>
<div class='cred-item'>
<div class='cred-label'>Email Address</div>
<div class='cred-value'>{email}</div>
</div>
<div class='cred-item'>
<div class='cred-label'>Temporary Password</div>
<div class='cred-value'>{tempPassword}</div>
</div>
</div>
<div style='text-align: center;'>
<a href='{loginUrl}' class='cta-button' style='color: #ffffff;'>Access Your Dashboard</a>
</div>
{licenseKeysHtml}
<div class='warning-box'>
<p><strong>Security First:</strong> Please change your temporary password immediately after logging in. This ensures your account remains secure.</p>
</div>
<div class='checklist'>
<h3>Quick Start Guide</h3>
<ul>
<li>Log in with your credentials above</li>
<li>Update your password to something secure</li>
<li>Complete your profile setup</li>
<li>Explore premium features and tools</li>
</ul>
</div>
<p class='message' style='font-size: 14px; color: #6b7280; text-align: center;'>Need help getting started? Our support team is here for you 24/7 at <strong>support@dsecure.com</strong></p>
</div>
<div class='footer'>
<p><strong>Questions?</strong> We're here to help!</p>
<p>© {DateTime.Now.Year} D-Secure Technologies Pvt Ltd. All rights reserved.</p>
</div>
</div>
</body>
</html>";
        }

        private string GetPaymentFailedEmailBody(string userName, string email, string productName, decimal? amount = null)
        {
            var amountText = amount.HasValue ? $"${amount.Value:F2}" : "N/A";
            
            return $@"
<!DOCTYPE html>
<html>
<head>
<style>
body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
.container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
.header {{ background: linear-gradient(135deg, #ff6b6b 0%, #ee5a6f 100%); padding: 30px; text-align: center; color: white; border-radius: 8px 8px 0 0; }}
.content {{ background-color: #ffffff; padding: 30px; border: 1px solid #eee; }}
.info {{ background-color: #fff3cd; border-left: 4px solid #ffc107; padding: 15px; margin: 20px 0; }}
.details-box {{ background-color: #f8f9fa; border-radius: 8px; padding: 20px; margin: 20px 0; border-left: 4px solid #ff6b6b; }}
.retry-btn {{ display: inline-block; background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 15px 40px; text-decoration: none; border-radius: 8px; font-weight: bold; font-size: 16px; margin: 20px 0; }}
.footer {{ text-align: center; margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; color: #666; font-size: 12px; }}
.support-box {{ background-color: #e7f3ff; border-left: 4px solid #2196f3; padding: 15px; margin-top: 20px; }}
</style>
</head>
<body>
<div class='container'>
<div class='header'>
<h1 style='margin: 0;'>Payment Failed</h1>
</div>
<div class='content'>
<p>Hello <strong>{userName}</strong>,</p>
<p>We regret to inform you that your recent payment attempt was unsuccessful.</p>

<div class='details-box'>
<h3 style='margin: 0 0 15px 0; color: #ff6b6b;'>Payment Details</h3>
<p style='margin: 5px 0;'><strong>Email:</strong> {email}</p>
<p style='margin: 5px 0;'><strong>Product:</strong> {productName}</p>
<p style='margin: 5px 0;'><strong>Amount:</strong> {amountText}</p>
<p style='margin: 5px 0;'><strong>Status:</strong> <span style='color: #ff6b6b; font-weight: bold;'>Failed</span></p>
</div>

<div class='info'>
<strong>Common Reasons for Payment Failure:</strong>
<ul style='margin: 10px 0;'>
<li>Insufficient funds</li>
<li>Incorrect card details</li>
<li>Card expired</li>
<li>Bank declined the transaction</li>
<li>Network timeout</li>
</ul>
</div>

<div class='support-box'>
<strong>What to do next:</strong>
<ol style='margin: 10px 0;'>
<li>Verify your payment method details</li>
<li>Ensure sufficient balance in your account</li>
<li>Contact your bank if the issue persists</li>
<li>Try again with a different payment method</li>
</ol>
</div>

<div style='text-align: center; margin-top: 30px;'>
<p>Ready to try again?</p>
<a href='https://your-site.com/checkout' class='retry-btn'>Retry Payment</a>
</div>

<p style='margin-top: 30px;'>If you continue to experience issues or have any questions, please don't hesitate to contact our support team.</p>
</div>

<div class='footer'>
<p><strong>Need Help?</strong> Contact us at Support@dsecuretech.com</p>
<p>© {DateTime.Now.Year} D-Secure Technologies Pvt Ltd. All rights reserved.</p>
</div>
</div>
</body>
</html>";
        }


        private string GetPaymentSuccessEmailBody(string userName, string email, string productName, decimal amount, int quantity, List<string>? licenseKeys = null, string status = "Paid", string? invoiceUrl = null)
        {
            var licenseKeysHtml = "";
            if (licenseKeys != null && licenseKeys.Any())
            {
                var keysListHtml = string.Join("", licenseKeys.Select(k => $"<div style='background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%); border: 2px solid #10b981; border-radius: 8px; padding: 12px 16px; margin: 8px 0; font-family: \"Courier New\", monospace; font-size: 15px; font-weight: 600; color: #047857; text-align: center; box-shadow: 0 2px 8px rgba(16,185,129,0.1);'>{k}</div>"));
                licenseKeysHtml = $@"
<div style='background: linear-gradient(135deg, #f0fdf4 0%, #dcfce7 100%); border: 2px solid #10b981; border-radius: 12px; padding: 24px; margin: 30px 0;'>
<h3 style='margin: 0 0 16px 0; color: #047857; font-size: 20px; text-align: center;'>Your License Keys</h3>
<p style='text-align: center; color: #065f46; margin-bottom: 16px; font-size: 14px;'>Generated {licenseKeys.Count} premium license key(s)</p>
{keysListHtml}
<p style='text-align: center; font-size: 13px; color: #059669; margin: 16px 0 0 0; font-weight: 500;'>Save these keys securely - you'll need them to activate your software!</p>
</div>";
            }

            // Invoice download section - Dodo style dark button
            var invoiceHtml = "";
            if (!string.IsNullOrEmpty(invoiceUrl))
            {
                invoiceHtml = $@"
<div style='background: #1a1a2e; border-radius: 12px; padding: 24px; margin: 30px 0; text-align: center;'>
<a href='{invoiceUrl}' target='_blank' style='display: inline-flex; align-items: center; justify-content: center; gap: 10px; background: #2d2d44; color: #ffffff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 15px; border: 1px solid #3d3d5c; transition: all 0.3s ease;'>
<svg xmlns='http://www.w3.org/2000/svg' width='18' height='18' viewBox='0 0 24 24' fill='none' stroke='currentColor' stroke-width='2' stroke-linecap='round' stroke-linejoin='round' style='vertical-align: middle;'><path d='M21 15v4a2 2 0 0 1-2 2H5a2 2 0 0 1-2-2v-4'/><polyline points='7 10 12 15 17 10'/><line x1='12' y1='15' x2='12' y2='3'/></svg>
Download Invoice
</a>
</div>";
            }

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Payment Successful</title>
<style>
@import url('https://fonts.googleapis.com/css2?family=Inter:wght@400;500;600;700&display=swap');
* {{ margin: 0; padding: 0; box-sizing: border-box; }}
body {{ font-family: 'Inter', sans-serif; background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 40px 20px; }}
.email-wrapper {{ max-width: 600px; margin: 0 auto; background: #fff; border-radius: 16px; overflow: hidden; box-shadow: 0 20px 60px rgba(0,0,0,0.15); }}
.header {{ background: linear-gradient(135deg, #10b981 0%, #059669 100%); padding: 50px 30px; text-align: center; }}
@keyframes confetti {{ 0% {{ transform: scale(0) rotate(-45deg); opacity: 0; }} 50% {{ transform: scale(1.2) rotate(10deg); }} 100% {{ transform: scale(1) rotate(0deg); opacity: 1; }} }}
.header h1 {{ color: #fff; font-size: 28px; font-weight: 700; margin: 0; text-shadow: 0 2px 4px rgba(0,0,0,0.1); }}
.content {{ padding: 40px 30px; }}
.celebration-badge {{ background: linear-gradient(135deg, #3b82f6 0%, #2563eb 100%); color: #fff; padding: 12px 24px; border-radius: 20px; display: inline-block; font-size: 14px; font-weight: 600; margin-bottom: 24px; }}
.greeting {{ font-size: 18px; color: #1a1a2e; margin-bottom: 20px; }}
.greeting strong {{ color: #10b981; font-weight: 600; }}
.message {{ font-size: 16px; color: #4a5568; line-height: 1.8; margin-bottom: 24px; }}
.receipt {{ background: linear-gradient(135deg, #f8f9fa 0%, #e9ecef 100%); border: 2px dashed #10b981; border-radius: 12px; padding: 24px; margin: 24px 0; }}
.receipt h3 {{ color: #1a1a2e; font-size: 18px; margin-bottom: 20px; text-align: center; }}
.receipt-item {{ display: flex; justify-content: space-between; padding: 12px 0; border-bottom: 1px solid #dee2e6; }}
.receipt-item:last-child {{ border-bottom: none; font-weight: 600; background: linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%); padding: 16px; margin: 16px -8px -8px -8px; border-radius: 8px; }}
.receipt-label {{ color: #6b7280; font-size: 14px; }}
.receipt-value {{ color: #1a1a2e; font-weight: 500; font-size: 14px; }}
.success-card {{ background: linear-gradient(135deg, #ecfdf5 0%, #d1fae5 100%); border: 2px solid #10b981; border-radius: 12px; padding: 20px; margin: 24px 0; text-align: center; }}
.success-card p {{ color: #047857; font-weight: 500; margin: 0; }}
.cta-button {{ display: inline-block; background: linear-gradient(135deg, #059669 0%, #047857 100%); color: #ffffff; padding: 16px 40px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 16px; margin: 20px 0; }}
.footer {{ background: #f8f9fa; padding: 30px; text-align: center; }}
.footer p {{ color: #6b7280; font-size: 14px; margin: 8px 0; }}
@media only screen and (max-width: 600px) {{
    .header {{ padding: 40px 20px; }}
    .header h1 {{ font-size: 24px; }}
    .content {{ padding: 30px 20px; }}
}}
</style>
</head>
<body>
<div class='email-wrapper'>
<div class='header'>
<h1>Payment Successful!</h1>
</div>
<div class='content'>
<div style='text-align: center;'>
<div class='celebration-badge'>Transaction Completed</div>
</div>
<p class='greeting'>Hello <strong>{userName}</strong>,</p>
<p class='message'>Thank you for your purchase! Your payment has been successfully processed and your order is complete.</p>
<div class='receipt'>
<h3>Purchase Receipt</h3>
<div class='receipt-item'>
<span class='receipt-label'>Product</span>
<span class='receipt-value' style='margin-left: 20px;'>{productName}</span>
</div>
<div class='receipt-item'>
<span class='receipt-label'>Quantity</span>
<span class='receipt-value' style='margin-left: 20px;'>{quantity}</span>
</div>
<div class='receipt-item'>
<span class='receipt-label'>Status</span>
<span class='receipt-value' style='color: #10b981; font-weight: 600; margin-left: 20px;'>{status}</span>
</div>
<div class='receipt-item'>
<span class='receipt-label'>Total Amount</span>
<span class='receipt-value' style='font-size: 18px; color: #10b981; margin-left: 20px;'>${amount:F2}</span>
</div>
</div>
{licenseKeysHtml}
<div style='text-align: center;'>
<a href='https://dsecuretech.com/login' class='cta-button' style='color: #ffffff;'>Go to Dashboard</a>
</div>
<p class='message' style='font-size: 14px; color: #1a1a2e; text-align: center; margin-top: 30px;'>Need help getting started? Contact us at <strong style='color: #059669;'>Support@dsecuretech.com</strong></p>
</div>
<div class='footer'>
<p><strong>Thank you for choosing D-Secure Technologies Pvt Ltd!</strong></p>
<p>© {DateTime.Now.Year} D-Secure Technologies Pvt Ltd. All rights reserved.</p>
</div>
</div>
</body>
</html>";
        }

        #endregion
    }
}
