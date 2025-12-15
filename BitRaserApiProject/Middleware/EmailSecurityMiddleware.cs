using System.Text.RegularExpressions;
using BitRaserApiProject.Utilities;

namespace BitRaserApiProject.Middleware
{
    /// <summary>
    /// Middleware to prevent raw email addresses in URLs
    /// ✅ SECURITY: Rejects requests with @ symbol in URL paths
    /// ✅ LOGGING: Masks emails in logs automatically
    /// </summary>
    public class EmailSecurityMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<EmailSecurityMiddleware> _logger;
        private static readonly Regex EmailInUrlRegex = new Regex(@"[?&/]([^?&/]*@[^?&/]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public EmailSecurityMiddleware(RequestDelegate next, ILogger<EmailSecurityMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value ?? string.Empty;
            var queryString = context.Request.QueryString.Value ?? string.Empty;
            var fullUrl = path + queryString;

            // ✅ BYPASS: Allow Swagger UI and development testing
            // Swagger UI can send normal emails, backend will handle encoding
            var userAgent = context.Request.Headers["User-Agent"].ToString();
            var referer = context.Request.Headers["Referer"].ToString();
            
            bool isSwaggerRequest = userAgent.Contains("Swagger", StringComparison.OrdinalIgnoreCase) ||
                                   referer.Contains("/swagger", StringComparison.OrdinalIgnoreCase) ||
                                   path.StartsWith("/swagger", StringComparison.OrdinalIgnoreCase);

            // Check for raw email in URL (@ symbol)
            if (!isSwaggerRequest && EmailInUrlRegex.IsMatch(fullUrl))
            {
                var maskedUrl = MaskEmailsInUrl(fullUrl);
                
                _logger.LogWarning(
                    "🚫 SECURITY: Raw email detected in URL. Path: {MaskedPath}. " +
                    "Use Base64-encoded emails instead. Client IP: {ClientIP}",
                    maskedUrl,
                    context.Connection.RemoteIpAddress?.ToString()
                );

                context.Response.StatusCode = 400;
                context.Response.ContentType = "application/json";
                
                await context.Response.WriteAsJsonAsync(new
                {
                    error = "Invalid URL format",
                    message = "Email addresses must be Base64-encoded in URLs. Use Base64EmailEncoder.Encode() to encode emails.",
                    code = "EMAIL_NOT_ENCODED",
                    hint = "Example: /api/Users/{Base64EncodedEmail} instead of /api/Users/user@example.com",
                    timestamp = DateTime.UtcNow
                });
                
                return;
            }

            // Mask emails in logs
            var originalPath = context.Request.Path;
            var originalQueryString = context.Request.QueryString;
            
            try
            {
                await _next(context);
            }
            finally
            {
                // Log with masked emails
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    var maskedPath = MaskEmailsInUrl(originalPath.Value ?? "");
                    var maskedQuery = MaskEmailsInUrl(originalQueryString.Value ?? "");
                    
                    _logger.LogInformation(
                        "Request: {Method} {MaskedPath}{MaskedQuery} - Status: {StatusCode}",
                        context.Request.Method,
                        maskedPath,
                        maskedQuery,
                        context.Response.StatusCode
                    );
                }
            }
        }

        /// <summary>
        /// Mask all email-like patterns in URL
        /// </summary>
        private static string MaskEmailsInUrl(string url)
        {
            return EmailInUrlRegex.Replace(url, match =>
            {
                var email = match.Groups[1].Value;
                return match.Value.Replace(email, Base64EmailEncoder.MaskEmail(email));
            });
        }
    }

    /// <summary>
    /// Extension method to register Email Security Middleware
    /// </summary>
    public static class EmailSecurityMiddlewareExtensions
    {
        public static IApplicationBuilder UseEmailSecurity(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<EmailSecurityMiddleware>();
        }
    }
}
