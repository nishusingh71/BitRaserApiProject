using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace DSecureApi.Middleware
{
    /// <summary>
    /// Rate Limiting Middleware with tiered limits based on user type
    /// - Private Cloud Users (is_private_cloud=true): 500 requests/min
    /// - Normal Users (is_private_cloud=false): 100 requests/min
    /// - Forgot Password endpoints: 5 requests/hour (per IP or email)
    /// - Unauthenticated requests: 50 requests/min (per IP)
    /// </summary>
    public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private readonly IConfiguration _configuration;

        // Thread-safe dictionaries to track request counts
        private static readonly ConcurrentDictionary<string, RateLimitEntry> _userRateLimits = new();
        private static readonly ConcurrentDictionary<string, RateLimitEntry> _ipRateLimits = new();
        private static readonly ConcurrentDictionary<string, ForgotPasswordRateLimitEntry> _forgotPasswordLimits = new();

        // Rate limit configurations
        private readonly int _privateCloudLimit;
        private readonly int _normalUserLimit;
        private readonly int _unauthenticatedLimit;
        private readonly int _forgotPasswordHourlyLimit;
        private readonly TimeSpan _windowDuration = TimeSpan.FromMinutes(1);
        private readonly TimeSpan _forgotPasswordWindowDuration = TimeSpan.FromHours(1);

        public RateLimitingMiddleware(
             RequestDelegate next,
               ILogger<RateLimitingMiddleware> logger,
           IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            _configuration = configuration;

            // Load rate limits from configuration or use defaults
            _privateCloudLimit = _configuration.GetValue<int>("RateLimiting:PrivateCloudLimit", 500);
            _normalUserLimit = _configuration.GetValue<int>("RateLimiting:NormalUserLimit", 100);
            _unauthenticatedLimit = _configuration.GetValue<int>("RateLimiting:UnauthenticatedLimit", 50);
            _forgotPasswordHourlyLimit = _configuration.GetValue<int>("RateLimiting:ForgotPasswordHourlyLimit", 5);

            _logger.LogInformation("✅ Rate Limiting initialized - PrivateCloud: {PC}/min, Normal: {N}/min, Unauth: {U}/min, ForgotPassword: {FP}/hour",
                _privateCloudLimit, _normalUserLimit, _unauthenticatedLimit, _forgotPasswordHourlyLimit);
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;
            var clientIp = GetClientIpAddress(context);

            // Skip rate limiting for certain paths
            if (ShouldSkipRateLimiting(path))
            {
                await _next(context);
                return;
            }

            // Check if it's a forgot password endpoint (special hourly limit)
            if (IsForgotPasswordEndpoint(path))
            {
                // Extract email from request body for tracking
                var email = await GetEmailFromRequestBody(context);

                if (!await CheckForgotPasswordHourlyRateLimit(clientIp, email, context))
                {
                    return;
                }
                await _next(context);
                return;
            }

            // Get user email from JWT token
            var userEmail = GetUserEmail(context);

            if (!string.IsNullOrEmpty(userEmail))
            {
                // Authenticated user - check user-based rate limit
                using var scope = serviceProvider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var isPrivateCloudUser = await IsPrivateCloudUserAsync(dbContext, userEmail);
                var limit = isPrivateCloudUser ? _privateCloudLimit : _normalUserLimit;
                var userType = isPrivateCloudUser ? "PrivateCloud" : "Normal";

                if (!await CheckUserRateLimitAsync(userEmail, limit, userType, context))
                {
                    return;
                }
            }
            else
            {
                // Unauthenticated request - use IP-based rate limit
                if (!await CheckIpRateLimitAsync(clientIp, context))
                {
                    return;
                }
            }

            await _next(context);
        }

        /// <summary>
        /// Check rate limit for authenticated users (per minute)
        /// </summary>
        private async Task<bool> CheckUserRateLimitAsync(string userEmail, int limit, string userType, HttpContext context)
        {
            var key = $"user:{userEmail}";
            var now = DateTime.UtcNow;

            var entry = _userRateLimits.AddOrUpdate(
                        key,
              _ => new RateLimitEntry { Count = 1, WindowStart = now },
            (_, existing) =>
              {
                  if (now - existing.WindowStart > _windowDuration)
                  {
                      // Reset window
                      return new RateLimitEntry { Count = 1, WindowStart = now };
                  }
                  existing.Count++;
                  return existing;
              });

            // Add rate limit headers
            AddRateLimitHeaders(context, limit, entry.Count, entry.WindowStart, _windowDuration);

            if (entry.Count > limit)
            {
                _logger.LogWarning("⚠️ Rate limit exceeded for {UserType} user {Email}: {Count}/{Limit} requests/min",
          userType, userEmail, entry.Count, limit);

                await ReturnRateLimitExceededAsync(context, limit, entry.WindowStart, _windowDuration, "minute");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check rate limit for unauthenticated requests (IP-based, per minute)
        /// </summary>
        private async Task<bool> CheckIpRateLimitAsync(string clientIp, HttpContext context)
        {
            var key = $"ip:{clientIp}";
            var now = DateTime.UtcNow;

            var entry = _ipRateLimits.AddOrUpdate(
       key,
     _ => new RateLimitEntry { Count = 1, WindowStart = now },
          (_, existing) =>
        {
            if (now - existing.WindowStart > _windowDuration)
            {
                return new RateLimitEntry { Count = 1, WindowStart = now };
            }
            existing.Count++;
            return existing;
        });

            AddRateLimitHeaders(context, _unauthenticatedLimit, entry.Count, entry.WindowStart, _windowDuration);

            if (entry.Count > _unauthenticatedLimit)
            {
                _logger.LogWarning("⚠️ Rate limit exceeded for IP {IP}: {Count}/{Limit} requests/min",
               clientIp, entry.Count, _unauthenticatedLimit);

                await ReturnRateLimitExceededAsync(context, _unauthenticatedLimit, entry.WindowStart, _windowDuration, "minute");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Check rate limit for forgot password endpoints (5 requests per HOUR per IP or email)
        /// </summary>
        private async Task<bool> CheckForgotPasswordHourlyRateLimit(string clientIp, string? email, HttpContext context)
        {
            var now = DateTime.UtcNow;
            var hourStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0, DateTimeKind.Utc);

            // Track by both IP and email (if provided)
            var ipKey = $"forgot:ip:{clientIp}";
            var emailKey = !string.IsNullOrEmpty(email) ? $"forgot:email:{email.ToLowerInvariant()}" : null;

            // Check IP-based limit
            var ipEntry = _forgotPasswordLimits.AddOrUpdate(
          ipKey,
 _ => new ForgotPasswordRateLimitEntry { Count = 1, WindowStart = hourStart },
                (_, existing) =>
      {
          if (now - existing.WindowStart >= _forgotPasswordWindowDuration)
          {
              // New hour - reset counter
              return new ForgotPasswordRateLimitEntry { Count = 1, WindowStart = hourStart };
          }
          existing.Count++;
          return existing;
      });

            // Check email-based limit (if email provided)
            ForgotPasswordRateLimitEntry? emailEntry = null;
            if (!string.IsNullOrEmpty(emailKey))
            {
                emailEntry = _forgotPasswordLimits.AddOrUpdate(
                      emailKey,
                       _ => new ForgotPasswordRateLimitEntry { Count = 1, WindowStart = hourStart },
                 (_, existing) =>
                {
                    if (now - existing.WindowStart >= _forgotPasswordWindowDuration)
                    {
                        return new ForgotPasswordRateLimitEntry { Count = 1, WindowStart = hourStart };
                    }
                    existing.Count++;
                    return existing;
                });
            }

            // Calculate time until next hour reset
            var nextHour = hourStart.AddHours(1);
            var timeUntilReset = nextHour - now;

            // Add headers
            var currentCount = Math.Max(ipEntry.Count, emailEntry?.Count ?? 0);
            var remaining = Math.Max(0, _forgotPasswordHourlyLimit - currentCount);

            context.Response.Headers["X-RateLimit-Limit"] = _forgotPasswordHourlyLimit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = ((int)timeUntilReset.TotalSeconds).ToString();
            context.Response.Headers["X-RateLimit-Policy"] = $"{_forgotPasswordHourlyLimit};w=3600";

            // Check if either limit exceeded
            if (ipEntry.Count > _forgotPasswordHourlyLimit)
            {
                _logger.LogWarning("⚠️ Forgot password hourly limit exceeded for IP {IP}: {Count}/{Limit} requests this hour",
       clientIp, ipEntry.Count, _forgotPasswordHourlyLimit);

                await ReturnForgotPasswordHourlyLimitExceeded(context, timeUntilReset, "IP address");
                return false;
            }

            if (emailEntry != null && emailEntry.Count > _forgotPasswordHourlyLimit)
            {
                _logger.LogWarning("⚠️ Forgot password hourly limit exceeded for email {Email}: {Count}/{Limit} requests this hour",
                    email, emailEntry.Count, _forgotPasswordHourlyLimit);

                await ReturnForgotPasswordHourlyLimitExceeded(context, timeUntilReset, "email address");
                return false;
            }

            _logger.LogInformation("📧 Forgot password request #{Count}/{Limit} this hour - IP: {IP}, Email: {Email}",
     currentCount, _forgotPasswordHourlyLimit, clientIp, email ?? "not provided");

            return true;
        }

        /// <summary>
        /// Extract email from request body for forgot password tracking
        /// </summary>
        private async Task<string?> GetEmailFromRequestBody(HttpContext context)
        {
            try
            {
                context.Request.EnableBuffering();

                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;

                if (string.IsNullOrEmpty(body))
                    return null;

                var jsonDoc = System.Text.Json.JsonDocument.Parse(body);

                if (jsonDoc.RootElement.TryGetProperty("email", out var emailElement))
                    return emailElement.GetString();

                if (jsonDoc.RootElement.TryGetProperty("Email", out var emailElement2))
                    return emailElement2.GetString();

                if (jsonDoc.RootElement.TryGetProperty("userEmail", out var emailElement3))
                    return emailElement3.GetString();
            }
            catch (Exception ex)
            {
                _logger.LogDebug("Could not extract email from request body: {Message}", ex.Message);
            }

            return null;
        }

        /// <summary>
        /// Check if user is a private cloud user
        /// </summary>
        private async Task<bool> IsPrivateCloudUserAsync(ApplicationDbContext dbContext, string userEmail)
        {
            try
            {
                var user = await dbContext.Users
              .AsNoTracking()
              .FirstOrDefaultAsync(u => u.user_email == userEmail);

                return user?.is_private_cloud == true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking private cloud status for {Email}", userEmail);
                return false;
            }
        }

        /// <summary>
        /// Get user email from JWT token
        /// </summary>
        private string? GetUserEmail(HttpContext context)
        {
            if (context.User?.Identity?.IsAuthenticated != true)
                return null;

            return context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
         ?? context.User.FindFirst(ClaimTypes.Email)?.Value
       ?? context.User.FindFirst("email")?.Value
        ?? context.User.FindFirst("sub")?.Value;
        }

        /// <summary>
        /// Get client IP address
        /// </summary>
        private string GetClientIpAddress(HttpContext context)
        {
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].FirstOrDefault();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                return forwardedFor.Split(',')[0].Trim();
            }

            var realIp = context.Request.Headers["X-Real-IP"].FirstOrDefault();
            if (!string.IsNullOrEmpty(realIp))
            {
                return realIp;
            }

            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }

        /// <summary>
        /// Add standard rate limit headers to response
        /// </summary>
        private void AddRateLimitHeaders(HttpContext context, int limit, int current, DateTime windowStart, TimeSpan windowDuration)
        {
            var remaining = Math.Max(0, limit - current);
            var resetTime = windowStart.Add(windowDuration);
            var resetSeconds = Math.Max(0, (int)(resetTime - DateTime.UtcNow).TotalSeconds);

            context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
            context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
            context.Response.Headers["X-RateLimit-Reset"] = resetSeconds.ToString();
            context.Response.Headers["X-RateLimit-Policy"] = $"{limit};w={(int)windowDuration.TotalSeconds}";
        }

        /// <summary>
        /// Return 429 Too Many Requests response
        /// </summary>
        private async Task ReturnRateLimitExceededAsync(HttpContext context, int limit, DateTime windowStart, TimeSpan windowDuration, string timeUnit)
        {
            var resetTime = windowStart.Add(windowDuration);
            var retryAfter = Math.Max(1, (int)(resetTime - DateTime.UtcNow).TotalSeconds);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                error = "Rate limit exceeded",
                message = $"Too many requests. Limit: {limit} requests per {timeUnit}.",
                retryAfter = retryAfter,
                retryAfterUnit = "seconds"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Return rate limit exceeded for forgot password (hourly limit)
        /// </summary>
        private async Task ReturnForgotPasswordHourlyLimitExceeded(HttpContext context, TimeSpan timeUntilReset, string limitType)
        {
            var retryAfterSeconds = (int)timeUntilReset.TotalSeconds;
            var retryAfterMinutes = (int)Math.Ceiling(timeUntilReset.TotalMinutes);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfterSeconds.ToString();
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                error = "Hourly limit exceeded",
                message = $"For security reasons, you can only request {_forgotPasswordHourlyLimit} password resets per hour from this {limitType}. Please try again later.",
                retryAfter = retryAfterSeconds,
                retryAfterMinutes = retryAfterMinutes,
                retryAfterUnit = "seconds",
                resetsAt = DateTime.UtcNow.AddSeconds(retryAfterSeconds).ToString("yyyy-MM-ddTHH:mm:ssZ")
            };

            var json = System.Text.Json.JsonSerializer.Serialize(response);
            await context.Response.WriteAsync(json);
        }

        /// <summary>
        /// Check if path should skip rate limiting
        /// </summary>
        private bool ShouldSkipRateLimiting(string path)
        {
            var skipPaths = new[]
            {
                "/swagger",
                "/health",
                "/favicon.ico",
                "/.well-known",
                // ✅ WEBHOOKS: Skip rate limiting for external payment provider callbacks
                "/api/webhooks/dodo",
                "/api/webhooks/polar",
                "/api/payments/dodo/webhook",
                "/api/payments/polar/webhook"
            };

            return skipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Check if path is a forgot password endpoint
        /// </summary>
        private bool IsForgotPasswordEndpoint(string path)
        {
            var forgotPasswordPaths = new[]
  {
     "/api/forgotpassword/request-otp",
  "/api/forgotpassword/verify-otp",
    "/api/forgotpassword/reset-password",
          "/api/forgotpassword/resend-otp"
          //"/api/forgot/request",
          // "/api/forgot/resend-otp",
          //  "/api/forgot/validate-reset-link",
          //   "/api/forgot/verify-otp",
          //      "/api/forgot/reset"

        };

            return forgotPasswordPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase) ||
               path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Background task to clean up expired entries (call periodically)
        /// </summary>
        public static void CleanupExpiredEntries()
        {
            var now = DateTime.UtcNow;
            var expiredWindow = TimeSpan.FromMinutes(2);
            var expiredHourWindow = TimeSpan.FromHours(2); // Keep forgot password entries for 2 hours

            // Cleanup user rate limits (per minute)
            foreach (var key in _userRateLimits.Keys.ToList())
            {
                if (_userRateLimits.TryGetValue(key, out var entry) &&
             now - entry.WindowStart > expiredWindow)
                {
                    _userRateLimits.TryRemove(key, out _);
                }
            }

            // Cleanup IP rate limits (per minute)
            foreach (var key in _ipRateLimits.Keys.ToList())
            {
                if (_ipRateLimits.TryGetValue(key, out var entry) &&
                         now - entry.WindowStart > expiredWindow)
                {
                    _ipRateLimits.TryRemove(key, out _);
                }
            }

            // Cleanup forgot password limits (per hour) - keep for 2 hours then cleanup
            foreach (var key in _forgotPasswordLimits.Keys.ToList())
            {
                if (_forgotPasswordLimits.TryGetValue(key, out var entry) &&
        now - entry.WindowStart > expiredHourWindow)
                {
                    _forgotPasswordLimits.TryRemove(key, out _);
                }
            }
        }
    }

    /// <summary>
    /// Rate limit entry to track request count and window (per minute)
    /// </summary>
    public class RateLimitEntry
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }

    /// <summary>
    /// Forgot password rate limit entry (per hour)
    /// </summary>
    public class ForgotPasswordRateLimitEntry
    {
        public int Count { get; set; }
        public DateTime WindowStart { get; set; }
    }

    /// <summary>
    /// Extension method to register RateLimitingMiddleware
    /// </summary>
    public static class RateLimitingMiddlewareExtensions
    {
        public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}
