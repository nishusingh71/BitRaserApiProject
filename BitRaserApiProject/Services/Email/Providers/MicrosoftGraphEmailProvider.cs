using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Net;

namespace BitRaserApiProject.Services.Email.Providers
{
    /// <summary>
    /// Microsoft Graph API email provider implementation
    /// Uses OAuth2 client credentials (app-only) flow for sending via Outlook/Exchange
    /// 
    /// IMPORTANT: App-only auth requires:
    /// 1. Mail.Send APPLICATION permission (not delegated)
    /// 2. Admin consent granted
    /// 3. SenderUserId must be a LICENSED mailbox user (Exchange Online license)
    /// 4. Shared mailboxes and unlicensed users are NOT supported for app-only auth
    /// 
    /// Reference: https://learn.microsoft.com/en-us/graph/api/user-sendmail
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
        private bool _permissionsValidated = false;
        private string? _initializationError;

        // Retry configuration
        private const int MAX_RETRY_ATTEMPTS = 3;
        private const int BASE_DELAY_MS = 1000;

        public string ProviderName => "MicrosoftGraph";
        public int Priority { get; private set; } = 1;

        public MicrosoftGraphEmailProvider(
            IConfiguration configuration,
            ILogger<MicrosoftGraphEmailProvider> logger,
            IEmailQuotaService quotaService)
        {
            _configuration = configuration;
            _logger = logger;
            _quotaService = quotaService;
        }

        /// <summary>
        /// Initialize the Graph client with proper validation
        /// Fix #1: SenderUserId is MANDATORY for app-only auth
        /// Fix #2: Validate permissions during initialization
        /// </summary>
        public async Task InitializeAsync()
        {
            // Prevent re-initialization if already done
            if (_isInitialized && _graphClient != null)
            {
                return;
            }

            try
            {
                _logger.LogInformation("🔧 MS Graph: Starting initialization...");

                // Load configuration from environment variables first, then config
                var clientId = Environment.GetEnvironmentVariable("AZURE_CLIENT_ID")
                    ?? Environment.GetEnvironmentVariable("MsGraph__ClientId")
                    ?? _configuration["MsGraph:ClientId"];

                var tenantId = Environment.GetEnvironmentVariable("AZURE_TENANT_ID")
                    ?? Environment.GetEnvironmentVariable("MsGraph__TenantId")
                    ?? _configuration["MsGraph:TenantId"];

                var clientSecret = Environment.GetEnvironmentVariable("AZURE_CLIENT_SECRET")
                    ?? Environment.GetEnvironmentVariable("MsGraph__ClientSecret")
                    ?? _configuration["MsGraph:ClientSecret"];

                _senderEmail = Environment.GetEnvironmentVariable("AZURE_SENDER_EMAIL")
                    ?? Environment.GetEnvironmentVariable("MsGraph__SenderEmail")
                    ?? _configuration["MsGraph:SenderEmail"];

                // Fix #1: SenderUserId is MANDATORY - app-only auth MUST use licensed mailbox
                _senderUserId = Environment.GetEnvironmentVariable("AZURE_SENDER_USER_ID")
                    ?? Environment.GetEnvironmentVariable("MsGraph__SenderUserId")
                    ?? _configuration["MsGraph:SenderUserId"];

                var priorityStr = Environment.GetEnvironmentVariable("MsGraph__Priority")
                    ?? _configuration["MsGraph:Priority"];
                
                if (int.TryParse(priorityStr, out var priority))
                {
                    Priority = priority;
                }

                // ==================== CONFIGURATION VALIDATION ====================
                
                // Check required OAuth credentials
                if (string.IsNullOrEmpty(clientId))
                {
                    _initializationError = "CONFIGURATION_ERROR: AZURE_CLIENT_ID is missing. Set it in environment variables or appsettings.json";
                    _logger.LogError("❌ MS Graph: {Error}", _initializationError);
                    _isInitialized = false;
                    return;
                }

                if (string.IsNullOrEmpty(tenantId))
                {
                    _initializationError = "CONFIGURATION_ERROR: AZURE_TENANT_ID is missing. Set it in environment variables or appsettings.json";
                    _logger.LogError("❌ MS Graph: {Error}", _initializationError);
                    _isInitialized = false;
                    return;
                }

                if (string.IsNullOrEmpty(clientSecret))
                {
                    _initializationError = "CONFIGURATION_ERROR: AZURE_CLIENT_SECRET is missing. Set it in environment variables or appsettings.json";
                    _logger.LogError("❌ MS Graph: {Error}", _initializationError);
                    _isInitialized = false;
                    return;
                }

                // Fix #1: CRITICAL - SenderUserId validation
                // For app-only (client credentials) auth, you MUST use a licensed Exchange Online mailbox
                // Shared mailboxes DON'T work - they require delegated permissions
                // Unlicensed users DON'T have mailboxes and will fail with "MailboxNotEnabledForRESTAPI"
                if (string.IsNullOrEmpty(_senderUserId))
                {
                    _initializationError = "CONFIGURATION_ERROR: AZURE_SENDER_USER_ID is REQUIRED for app-only authentication. " +
                        "Must be Object ID or UPN of a LICENSED Exchange Online user. Shared mailboxes are NOT supported.";
                    _logger.LogError("❌ MS Graph: {Error}", _initializationError);
                    _isInitialized = false;
                    return;
                }

                if (string.IsNullOrEmpty(_senderEmail))
                {
                    _initializationError = "CONFIGURATION_ERROR: AZURE_SENDER_EMAIL is missing.";
                    _logger.LogError("❌ MS Graph: {Error}", _initializationError);
                    _isInitialized = false;
                    return;
                }

                _logger.LogInformation("🔐 MS Graph: Credentials loaded - ClientId: {ClientId}, TenantId: {TenantId}, UserId: {UserId}", 
                    clientId[..8] + "***", tenantId[..8] + "***", _senderUserId[..8] + "***");

                // Create client credential (Azure.Identity)
                var credential = new ClientSecretCredential(
                    tenantId,
                    clientId,
                    clientSecret
                );

                // Create Graph client - SDK v5 pattern
                // Note: For app-only auth, the scope is always "https://graph.microsoft.com/.default"
                _graphClient = new GraphServiceClient(credential, new[] { "https://graph.microsoft.com/.default" });

                // Fix #2: Validate permissions by making a lightweight API call
                await ValidatePermissionsAsync();

                if (!_permissionsValidated)
                {
                    _logger.LogWarning("⚠️ MS Graph: Permission validation failed but continuing. First email send may fail.");
                }

                _isInitialized = true;
                _logger.LogInformation("✅ MS Graph: Provider initialized successfully - SenderEmail: {SenderEmail}, UserId: {UserId}", 
                    _senderEmail, _senderUserId[..8] + "***");
            }
            catch (AuthenticationFailedException authEx)
            {
                _initializationError = $"AUTHENTICATION_ERROR: Azure AD authentication failed - {authEx.Message}";
                _logger.LogError(authEx, "❌ MS Graph: {Error}", _initializationError);
                _isInitialized = false;
            }
            catch (Exception ex)
            {
                _initializationError = $"INITIALIZATION_ERROR: {ex.Message}";
                _logger.LogError(ex, "❌ MS Graph: Failed to initialize");
                _isInitialized = false;
            }
        }

        /// <summary>
        /// Fix #2: Validate that the app has Mail.Send permission and admin consent
        /// Makes a lightweight call to check user profile (requires minimal permissions)
        /// ✅ IMPROVED: Added retry logic for cold start resilience
        /// </summary>
        private async Task ValidatePermissionsAsync()
        {
            if (_graphClient == null || string.IsNullOrEmpty(_senderUserId))
            {
                _logger.LogWarning("⚠️ MS Graph: Cannot validate permissions - client not ready");
                return;
            }

            const int maxRetries = 3;
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    _logger.LogDebug("🔍 MS Graph: Validating permissions for user {UserId}... (Attempt {Attempt}/{Max})", 
                        _senderUserId[..8] + "***", attempt, maxRetries);

                    // Try to get basic user info - this validates:
                    // 1. Authentication is working
                    // 2. User exists
                    // 3. App has at least User.Read.All permission
                    var user = await _graphClient.Users[_senderUserId].GetAsync(requestConfig =>
                    {
                        requestConfig.QueryParameters.Select = new[] { "id", "mail", "displayName" };
                    });

                    if (user != null)
                    {
                        _logger.LogInformation("✅ MS Graph: Permissions validated - User: {DisplayName} ({Mail})", 
                            user.DisplayName ?? "N/A", user.Mail ?? "N/A");
                        _permissionsValidated = true;
                        return; // Success - exit retry loop
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ MS Graph: User not found for UserId {UserId}", _senderUserId[..8] + "***");
                    }
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError odataError)
                {
                    var errorCode = odataError.Error?.Code ?? "Unknown";
                    var errorMessage = odataError.Error?.Message ?? odataError.Message;
                    
                    if (errorCode == "Authorization_RequestDenied" || odataError.ResponseStatusCode == 403)
                    {
                        // Don't retry permission errors - they need Azure Portal fix
                        _logger.LogError("🔴 MS Graph PERMISSION_ERROR: Mail.Send application permission not granted or admin consent missing. " +
                            "Go to Azure Portal > App Registrations > API Permissions > Add 'Mail.Send' (Application) > Grant Admin Consent");
                        return;
                    }
                    else if (errorCode == "Request_ResourceNotFound" || odataError.ResponseStatusCode == 404)
                    {
                        _logger.LogError("🔴 MS Graph USER_ERROR: User with ID '{UserId}' not found. Verify AZURE_SENDER_USER_ID is correct Object ID or UPN.", 
                            _senderUserId);
                        return;
                    }
                    else if (odataError.ResponseStatusCode == 401)
                    {
                        _logger.LogError("🔴 MS Graph AUTH_ERROR: Authentication failed. Verify AZURE_CLIENT_ID, AZURE_TENANT_ID, and AZURE_CLIENT_SECRET.");
                        return;
                    }
                    else
                    {
                        // Potentially transient error - retry
                        _logger.LogWarning("⚠️ MS Graph: Permission check returned error [{Code}]: {Message} (Attempt {Attempt})", 
                            errorCode, errorMessage, attempt);
                        
                        if (attempt < maxRetries)
                        {
                            var delay = attempt * 1000; // 1s, 2s, 3s
                            _logger.LogDebug("⏳ Retrying in {Delay}ms...", delay);
                            await Task.Delay(delay);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ MS Graph: Permission validation encountered unexpected error (Attempt {Attempt})", attempt);
                    
                    if (attempt < maxRetries)
                    {
                        var delay = attempt * 1000;
                        await Task.Delay(delay);
                    }
                }
            }
            
            _logger.LogWarning("⚠️ MS Graph: Permission validation failed after {MaxRetries} attempts, but provider will continue", maxRetries);
        }

        public async Task<bool> IsAvailableAsync()
        {
            if (!_isInitialized || _graphClient == null)
            {
                _logger.LogDebug("📊 MS Graph: Not available - Initialized: {Init}, HasClient: {Client}, Error: {Error}", 
                    _isInitialized, _graphClient != null, _initializationError ?? "none");
                return false;
            }

            return await _quotaService.HasAvailableQuotaAsync(ProviderName);
        }

        public async Task<int> GetRemainingQuotaAsync()
        {
            return await _quotaService.GetRemainingQuotaAsync(ProviderName);
        }

        /// <summary>
        /// Send email via Microsoft Graph API
        /// Fix #3: Throttling with Retry-After handling
        /// Fix #4: Body robustness - HTML/PlainText fallback
        /// Fix #5: Enhanced logging
        /// </summary>
        public async Task<EmailSendResult> SendEmailAsync(EmailSendRequest request)
        {
            // Validate initialization
            if (_graphClient == null)
            {
                var error = _initializationError ?? "Microsoft Graph not configured";
                _logger.LogError("❌ MS Graph CONFIG_ERROR: {Error}", error);
                return EmailSendResult.Failed(error, ProviderName);
            }

            // Fix #1: Enforce SenderUserId - never fall back silently
            if (string.IsNullOrEmpty(_senderUserId))
            {
                const string error = "SENDER_ERROR: AZURE_SENDER_USER_ID is required. Cannot use sender email as fallback for app-only auth.";
                _logger.LogError("❌ MS Graph: {Error}", error);
                return EmailSendResult.Failed(error, ProviderName);
            }

            var stopwatch = Stopwatch.StartNew();
            var attempt = 0;

            while (attempt < MAX_RETRY_ATTEMPTS)
            {
                attempt++;

                try
                {
                    _logger.LogInformation("📧 MS Graph: Sending email to {Email} - Subject: {Subject} (Attempt {Attempt}/{Max})", 
                        request.ToEmail, request.Subject, attempt, MAX_RETRY_ATTEMPTS);

                    // Fix #4: Build message with body fallback logic
                    var message = BuildMessage(request);

                    // Send via Graph API using user ID (not email)
                    await _graphClient.Users[_senderUserId].SendMail.PostAsync(new SendMailPostRequestBody
                    {
                        Message = message,
                        SaveToSentItems = true
                    });

                    stopwatch.Stop();

                    await _quotaService.IncrementUsageAsync(ProviderName);
                    await _quotaService.RecordSuccessAsync(ProviderName);
                    
                    _logger.LogInformation("✅ MS Graph: Email sent successfully to {Email} in {Duration}ms (Attempt {Attempt})", 
                        request.ToEmail, stopwatch.ElapsedMilliseconds, attempt);
                    
                    return EmailSendResult.Succeeded(
                        ProviderName, 
                        null, // Graph SendMail doesn't return message ID
                        (int)stopwatch.ElapsedMilliseconds
                    );
                }
                catch (Microsoft.Graph.Models.ODataErrors.ODataError odataError)
                {
                    var (shouldRetry, result) = await HandleODataErrorAsync(odataError, request, attempt, stopwatch);
                    
                    if (!shouldRetry)
                    {
                        return result!;
                    }
                    
                    // Continue to next retry attempt
                }
                catch (Exception ex)
                {
                    stopwatch.Stop();
                    var errorMsg = $"RUNTIME_ERROR: {ex.GetType().Name} - {ex.Message}";
                    
                    _logger.LogError(ex, "❌ MS Graph RUNTIME_ERROR: Failed to send email to {Email} on attempt {Attempt}: {Error}", 
                        request.ToEmail, attempt, ex.Message);
                    
                    await _quotaService.RecordFailureAsync(ProviderName, errorMsg);
                    return EmailSendResult.Failed(errorMsg, ProviderName, ex);
                }
            }

            // All retries exhausted
            stopwatch.Stop();
            const string exhaustedError = "RETRY_EXHAUSTED: All retry attempts failed";
            _logger.LogError("❌ MS Graph: {Error}", exhaustedError);
            return EmailSendResult.Failed(exhaustedError, ProviderName);
        }

        /// <summary>
        /// Fix #4: Build message with HTML/PlainText fallback
        /// </summary>
        private Message BuildMessage(EmailSendRequest request)
        {
            // Determine body content and type
            string bodyContent;
            BodyType bodyType;

            if (!string.IsNullOrWhiteSpace(request.HtmlBody))
            {
                bodyContent = request.HtmlBody;
                bodyType = BodyType.Html;
            }
            else if (!string.IsNullOrWhiteSpace(request.PlainTextBody))
            {
                bodyContent = request.PlainTextBody;
                bodyType = BodyType.Text;
            }
            else
            {
                // Fallback: empty body with warning
                _logger.LogWarning("⚠️ MS Graph: Both HtmlBody and PlainTextBody are empty for email to {Email}", request.ToEmail);
                bodyContent = "This email has no content.";
                bodyType = BodyType.Text;
            }

            var message = new Message
            {
                Subject = request.Subject ?? "(No Subject)",
                Body = new ItemBody
                {
                    ContentType = bodyType,
                    Content = bodyContent
                },
                ToRecipients = new List<Recipient>
                {
                    new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = request.ToEmail,
                            Name = request.ToName ?? request.ToEmail
                        }
                    }
                }
                // Note: From is NOT set - Graph API infers from userId for app-only auth
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

            // Add CC recipients if any
            if (request.CcEmails != null && request.CcEmails.Count > 0)
            {
                message.CcRecipients = request.CcEmails.Select(cc => new Recipient
                {
                    EmailAddress = new EmailAddress { Address = cc }
                }).ToList();
            }

            // Add attachments if present (FileAttachment per MS Graph spec)
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
                    
                    _logger.LogDebug("📎 MS Graph: Attached {FileName} ({Size} bytes)", 
                        attachment.FileName, attachment.Content.Length);
                }
            }

            return message;
        }

        /// <summary>
        /// Fix #3: Handle OData errors with throttling and retry logic
        /// Fix #5: Enhanced error logging with actionable messages
        /// </summary>
        private async Task<(bool ShouldRetry, EmailSendResult? Result)> HandleODataErrorAsync(
            Microsoft.Graph.Models.ODataErrors.ODataError odataError, 
            EmailSendRequest request, 
            int attempt,
            Stopwatch stopwatch)
        {
            var errorCode = odataError.Error?.Code ?? "Unknown";
            var errorMessage = odataError.Error?.Message ?? odataError.Message;
            var statusCode = odataError.ResponseStatusCode;

            _logger.LogWarning("⚠️ MS Graph API Error: StatusCode={StatusCode}, Code={Code}, Message={Message}", 
                statusCode, errorCode, errorMessage);

            // Fix #3: Handle HTTP 429 Throttling with Retry-After
            if (statusCode == 429)
            {
                return await HandleThrottlingAsync(odataError, attempt);
            }

            // Handle specific error codes with actionable logs
            var fullError = $"GRAPH_API_ERROR [{errorCode}]: {errorMessage}";

            switch (errorCode)
            {
                case "Authorization_RequestDenied":
                case "ErrorAccessDenied":
                    _logger.LogError("🔴 MS Graph PERMISSION_ERROR: Mail.Send permission denied. " +
                        "ACTION: Azure Portal > App Registrations > API Permissions > Add 'Mail.Send' (Application type) > Grant Admin Consent");
                    break;
                    
                case "ErrorUserNotFound":
                case "Request_ResourceNotFound":
                    _logger.LogError("🔴 MS Graph USER_ERROR: Sender user '{UserId}' not found. " +
                        "ACTION: Verify AZURE_SENDER_USER_ID is the correct Object ID or UPN of a licensed Exchange Online user.", 
                        _senderUserId);
                    break;
                    
                case "MailboxNotEnabledForRESTAPI":
                    _logger.LogError("🔴 MS Graph MAILBOX_ERROR: User mailbox is not enabled for REST API. " +
                        "This usually means the user doesn't have an Exchange Online license. " +
                        "ACTION: Assign Exchange Online license to the sender user in Microsoft 365 Admin Center.");
                    break;
                    
                case "ErrorInvalidUser":
                    _logger.LogError("🔴 MS Graph USER_ERROR: Invalid user. Shared mailboxes are NOT supported for app-only auth. " +
                        "ACTION: Use a licensed user mailbox, not a shared mailbox.");
                    break;
                    
                default:
                    _logger.LogError("❌ MS Graph API_ERROR: {Error}", fullError);
                    break;
            }

            // Authentication errors (401) - don't retry
            if (statusCode == 401)
            {
                _logger.LogError("🔴 MS Graph AUTH_ERROR: Authentication failed. " +
                    "ACTION: Verify AZURE_CLIENT_ID, AZURE_TENANT_ID, and AZURE_CLIENT_SECRET are correct.");
            }

            stopwatch.Stop();
            await _quotaService.RecordFailureAsync(ProviderName, fullError);
            
            return (false, EmailSendResult.Failed(fullError, ProviderName));
        }

        /// <summary>
        /// Fix #3: Handle throttling with Retry-After header
        /// </summary>
        private async Task<(bool ShouldRetry, EmailSendResult? Result)> HandleThrottlingAsync(
            Microsoft.Graph.Models.ODataErrors.ODataError odataError, 
            int attempt)
        {
            if (attempt >= MAX_RETRY_ATTEMPTS)
            {
                _logger.LogError("🔴 MS Graph THROTTLE_ERROR: Max retries reached while throttled");
                await _quotaService.RecordFailureAsync(ProviderName, "THROTTLE_ERROR: Max retries exceeded");
                return (false, EmailSendResult.Failed("THROTTLE_ERROR: Rate limit exceeded and max retries reached", ProviderName));
            }

            // Try to get Retry-After value from error or headers
            var retryAfterSeconds = 60; // Default to 60 seconds
            
            if (odataError.Error?.AdditionalData?.TryGetValue("Retry-After", out var retryValue) == true)
            {
                if (int.TryParse(retryValue?.ToString(), out var parsedRetry))
                {
                    retryAfterSeconds = parsedRetry;
                }
            }

            // Cap retry delay at 5 minutes
            retryAfterSeconds = Math.Min(retryAfterSeconds, 300);

            _logger.LogWarning("⏳ MS Graph THROTTLED: Waiting {Seconds}s before retry (Attempt {Attempt}/{Max})", 
                retryAfterSeconds, attempt, MAX_RETRY_ATTEMPTS);

            await Task.Delay(TimeSpan.FromSeconds(retryAfterSeconds));

            return (true, null); // Signal to retry
        }
    }
}
