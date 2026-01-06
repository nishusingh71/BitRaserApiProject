using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BitRaserApiProject.Services.Email.Providers
{
    /// <summary>
    /// Microsoft Graph API email provider implementation
    /// Uses OAuth2 client credentials flow for sending via Outlook
    /// </summary>
    public class MicrosoftGraphEmailProvider : IEmailProvider
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<MicrosoftGraphEmailProvider> _logger;
        private readonly IEmailQuotaService _quotaService;
        
        private GraphServiceClient? _graphClient;
        private string? _senderEmail;
        private string? _senderUserId;
        private bool _isInitialized = false;

        public string ProviderName => "MicrosoftGraph";
        public int Priority { get; private set; } = 2;

        public MicrosoftGraphEmailProvider(
            IConfiguration configuration,
            ILogger<MicrosoftGraphEmailProvider> logger,
            IEmailQuotaService quotaService)
        {
            _configuration = configuration;
            _logger = logger;
            _quotaService = quotaService;
        }

        public Task InitializeAsync()
        {
            try
            {
                // Load from environment variables first, then config
                var clientId = Environment.GetEnvironmentVariable("MsGraph__ClientId")
                    ?? Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")
                    ?? _configuration["MsGraph:ClientId"];

                var tenantId = Environment.GetEnvironmentVariable("MsGraph__TenantId")
                    ?? Environment.GetEnvironmentVariable("AZURE_TENANT_ID")
                    ?? _configuration["MsGraph:TenantId"];

                var clientSecret = Environment.GetEnvironmentVariable("MsGraph__ClientSecret")
                    ?? Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")
                    ?? _configuration["MsGraph:ClientSecret"];

                _senderEmail = Environment.GetEnvironmentVariable("MsGraph__SenderEmail")
                    ?? Environment.GetEnvironmentVariable("AZURE_SENDER_EMAIL")
                    ?? _configuration["MsGraph:SenderEmail"];

                _senderUserId = Environment.GetEnvironmentVariable("MsGraph__SenderUserId")
                    ?? Environment.GetEnvironmentVariable("AZURE_SENDER_USER_ID")
                    ?? _configuration["MsGraph:SenderUserId"];

                var priorityStr = Environment.GetEnvironmentVariable("MsGraph__Priority")
                    ?? _configuration["MsGraph:Priority"];
                
                if (int.TryParse(priorityStr, out var priority))
                {
                    Priority = priority;
                }

                // Validate required config
                if (string.IsNullOrEmpty(clientId) || 
                    string.IsNullOrEmpty(tenantId) || 
                    string.IsNullOrEmpty(clientSecret) ||
                    string.IsNullOrEmpty(_senderEmail))
                {
                    _logger.LogWarning("⚠️ Microsoft Graph provider not configured - missing credentials");
                    _isInitialized = false;
                    return Task.CompletedTask;
                }

                // Create client credential
                var credential = new ClientSecretCredential(
                    tenantId,
                    clientId,
                    clientSecret
                );

                // Create Graph client with required scopes
                _graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });

                _isInitialized = true;
                _logger.LogInformation("✅ Microsoft Graph provider initialized - SenderEmail: {SenderEmail}", _senderEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to initialize Microsoft Graph provider");
                _isInitialized = false;
            }

            return Task.CompletedTask;
        }

        public async Task<bool> IsAvailableAsync()
        {
            if (!_isInitialized || _graphClient == null)
                return false;

            return await _quotaService.HasAvailableQuotaAsync(ProviderName);
        }

        public async Task<int> GetRemainingQuotaAsync()
        {
            return await _quotaService.GetRemainingQuotaAsync(ProviderName);
        }

        public async Task<EmailSendResult> SendEmailAsync(EmailSendRequest request)
        {
            if (_graphClient == null || string.IsNullOrEmpty(_senderEmail))
            {
                return EmailSendResult.Failed("Microsoft Graph not configured", ProviderName);
            }

            var stopwatch = Stopwatch.StartNew();

            try
            {
                _logger.LogInformation("📧 MS Graph: Sending email to {Email} - Subject: {Subject}", 
                    request.ToEmail, request.Subject);

                // Build message
                var message = new Message
                {
                    Subject = request.Subject,
                    Body = new ItemBody
                    {
                        ContentType = BodyType.Html,
                        Content = request.HtmlBody
                    },
                    ToRecipients = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = request.ToEmail,
                                Name = request.ToName
                            }
                        }
                    },
                    From = new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = request.FromEmail ?? _senderEmail,
                            Name = request.FromName ?? "DSecure"
                        }
                    }
                };

                // Add Reply-To if specified
                if (!string.IsNullOrEmpty(request.ReplyToEmail))
                {
                    message.ReplyTo = new List<Recipient>
                    {
                        new Recipient
                        {
                            EmailAddress = new EmailAddress
                            {
                                Address = request.ReplyToEmail
                            }
                        }
                    };
                }

                // Add attachments if present
                if (request.Attachments != null && request.Attachments.Count > 0)
                {
                    message.Attachments = new List<Attachment>();
                    
                    foreach (var attachment in request.Attachments)
                    {
                        message.Attachments.Add(new FileAttachment
                        {
                            Name = attachment.FileName,
                            ContentType = attachment.ContentType,
                            ContentBytes = attachment.Content,
                            OdataType = "#microsoft.graph.fileAttachment"
                        });
                        
                        _logger.LogDebug("📎 Attached: {FileName} ({Size} bytes)", 
                            attachment.FileName, attachment.Content.Length);
                    }
                }

                // Send via Graph API
                // Use user ID if available, otherwise use email (UPN)
                var userId = _senderUserId ?? _senderEmail;
                
                await _graphClient.Users[userId].SendMail.PostAsync(new SendMailPostRequestBody
                {
                    Message = message,
                    SaveToSentItems = true
                });

                stopwatch.Stop();

                await _quotaService.IncrementUsageAsync(ProviderName);
                await _quotaService.RecordSuccessAsync(ProviderName);
                
                _logger.LogInformation("✅ MS Graph: Email sent successfully to {Email} in {Duration}ms", 
                    request.ToEmail, stopwatch.ElapsedMilliseconds);
                
                return EmailSendResult.Succeeded(
                    ProviderName, 
                    null, // Graph doesn't return message ID on send
                    (int)stopwatch.ElapsedMilliseconds
                );
            }
            catch (Microsoft.Graph.Models.ODataErrors.ODataError odataError)
            {
                stopwatch.Stop();
                var errorMessage = $"Graph API error: {odataError.Error?.Code} - {odataError.Error?.Message}";
                
                await _quotaService.RecordFailureAsync(ProviderName, errorMessage);
                _logger.LogError("❌ MS Graph: OData error - {Error}", errorMessage);
                
                // Check for throttling
                if (odataError.ResponseStatusCode == 429)
                {
                    _logger.LogWarning("⚠️ MS Graph: Throttled! Consider reducing send rate.");
                }
                
                return EmailSendResult.Failed(errorMessage, ProviderName);
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                await _quotaService.RecordFailureAsync(ProviderName, ex.Message);
                
                _logger.LogError(ex, "❌ MS Graph: Exception while sending email to {Email}", request.ToEmail);
                return EmailSendResult.Failed(ex.Message, ProviderName, ex);
            }
        }
    }
}
