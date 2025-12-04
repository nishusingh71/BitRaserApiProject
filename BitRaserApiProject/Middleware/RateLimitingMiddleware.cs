using System.Collections.Concurrent;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Middleware
{
  /// <summary>
    /// Rate Limiting Middleware with tiered limits based on user type
    /// - Private Cloud Users (is_private_cloud=true): 500 requests/min
    /// - Normal Users (is_private_cloud=false): 100 requests/min
    /// - Forgot Password endpoints: 5 requests/min (per IP)
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
      private static readonly ConcurrentDictionary<string, RateLimitEntry> _forgotPasswordLimits = new();

  // Rate limit configurations (requests per minute)
  private readonly int _privateCloudLimit;
        private readonly int _normalUserLimit;
        private readonly int _unauthenticatedLimit;
        private readonly int _forgotPasswordLimit;
        private readonly TimeSpan _windowDuration = TimeSpan.FromMinutes(1);

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
    _forgotPasswordLimit = _configuration.GetValue<int>("RateLimiting:ForgotPasswordLimit", 5);

            _logger.LogInformation("✅ Rate Limiting initialized - PrivateCloud: {PC}/min, Normal: {N}/min, Unauth: {U}/min, ForgotPassword: {FP}/min",
      _privateCloudLimit, _normalUserLimit, _unauthenticatedLimit, _forgotPasswordLimit);
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

       // Check if it's a forgot password endpoint (special limit)
 if (IsForgotPasswordEndpoint(path))
            {
       if (!await CheckForgotPasswordRateLimit(clientIp, context))
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

                if (!CheckUserRateLimit(userEmail, limit, userType, context))
                {
           return;
   }
            }
   else
      {
       // Unauthenticated request - use IP-based rate limit
  if (!CheckIpRateLimit(clientIp, context))
       {
        return;
          }
         }

  await _next(context);
        }

        /// <summary>
  /// Check rate limit for authenticated users
    /// </summary>
        private bool CheckUserRateLimit(string userEmail, int limit, string userType, HttpContext context)
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
      AddRateLimitHeaders(context, limit, entry.Count, entry.WindowStart);

   if (entry.Count > limit)
            {
     _logger.LogWarning("⚠️ Rate limit exceeded for {UserType} user {Email}: {Count}/{Limit} requests/min",
           userType, userEmail, entry.Count, limit);
    
        ReturnRateLimitExceeded(context, limit, entry.WindowStart);
     return false;
    }

       return true;
   }

        /// <summary>
  /// Check rate limit for unauthenticated requests (IP-based)
     /// </summary>
        private bool CheckIpRateLimit(string clientIp, HttpContext context)
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

      AddRateLimitHeaders(context, _unauthenticatedLimit, entry.Count, entry.WindowStart);

  if (entry.Count > _unauthenticatedLimit)
      {
      _logger.LogWarning("⚠️ Rate limit exceeded for IP {IP}: {Count}/{Limit} requests/min",
        clientIp, entry.Count, _unauthenticatedLimit);
            
    ReturnRateLimitExceeded(context, _unauthenticatedLimit, entry.WindowStart);
    return false;
        }

            return true;
        }

        /// <summary>
        /// Check rate limit for forgot password endpoints (strict limit)
        /// </summary>
        private async Task<bool> CheckForgotPasswordRateLimit(string clientIp, HttpContext context)
        {
            var key = $"forgot:{clientIp}";
   var now = DateTime.UtcNow;

    var entry = _forgotPasswordLimits.AddOrUpdate(
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

       AddRateLimitHeaders(context, _forgotPasswordLimit, entry.Count, entry.WindowStart);

   if (entry.Count > _forgotPasswordLimit)
  {
     _logger.LogWarning("⚠️ Forgot password rate limit exceeded for IP {IP}: {Count}/{Limit} requests/min",
           clientIp, entry.Count, _forgotPasswordLimit);
          
  await ReturnForgotPasswordRateLimitExceeded(context, entry.WindowStart);
 return false;
 }

          return true;
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
      // Check for forwarded IP (behind proxy/load balancer)
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
   private void AddRateLimitHeaders(HttpContext context, int limit, int current, DateTime windowStart)
        {
      var remaining = Math.Max(0, limit - current);
            var resetTime = windowStart.Add(_windowDuration);
        var resetSeconds = Math.Max(0, (int)(resetTime - DateTime.UtcNow).TotalSeconds);

        context.Response.Headers["X-RateLimit-Limit"] = limit.ToString();
 context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
 context.Response.Headers["X-RateLimit-Reset"] = resetSeconds.ToString();
        context.Response.Headers["X-RateLimit-Policy"] = $"{limit};w=60";
        }

        /// <summary>
        /// Return 429 Too Many Requests response
        /// </summary>
        private void ReturnRateLimitExceeded(HttpContext context, int limit, DateTime windowStart)
        {
  var resetTime = windowStart.Add(_windowDuration);
      var retryAfter = Math.Max(1, (int)(resetTime - DateTime.UtcNow).TotalSeconds);

            context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            context.Response.Headers["Retry-After"] = retryAfter.ToString();
     context.Response.ContentType = "application/json";

            var response = new
 {
        success = false,
                error = "Rate limit exceeded",
        message = $"Too many requests. Limit: {limit} requests per minute.",
   retryAfter = retryAfter,
                retryAfterUnit = "seconds"
  };

            var json = System.Text.Json.JsonSerializer.Serialize(response);
            context.Response.WriteAsync(json).Wait();
   }

        /// <summary>
        /// Return rate limit exceeded for forgot password (custom message)
  /// </summary>
        private async Task ReturnForgotPasswordRateLimitExceeded(HttpContext context, DateTime windowStart)
        {
      var resetTime = windowStart.Add(_windowDuration);
        var retryAfter = Math.Max(1, (int)(resetTime - DateTime.UtcNow).TotalSeconds);

context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
context.Response.Headers["Retry-After"] = retryAfter.ToString();
            context.Response.ContentType = "application/json";

       var response = new
         {
    success = false,
          error = "Too many password reset attempts",
      message = $"For security reasons, you can only request {_forgotPasswordLimit} password resets per minute. Please wait and try again.",
   retryAfter = retryAfter,
        retryAfterUnit = "seconds"
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
    "/.well-known"
            };

      return skipPaths.Any(p => path.StartsWith(p));
        }

        /// <summary>
        /// Check if path is a forgot password endpoint
/// </summary>
        private bool IsForgotPasswordEndpoint(string path)
        {
  var forgotPasswordPaths = new[]
  {
      "/api/forgot/request",
     "/api/forgot/resend-otp",
   "/api/forgot/validate-reset-link",
  "/api/forgot/verify-otp",
  "/api/forgot/reset",
  "/api/forgot/cleanup",
  "/api/forgot/admin/active-requests"
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
            var expiredWindow = TimeSpan.FromMinutes(2); // Keep entries for 2 minutes after expiry

     // Cleanup user rate limits
 foreach (var key in _userRateLimits.Keys.ToList())
          {
          if (_userRateLimits.TryGetValue(key, out var entry) && 
   now - entry.WindowStart > expiredWindow)
                {
         _userRateLimits.TryRemove(key, out _);
      }
            }

            // Cleanup IP rate limits
   foreach (var key in _ipRateLimits.Keys.ToList())
{
         if (_ipRateLimits.TryGetValue(key, out var entry) && 
           now - entry.WindowStart > expiredWindow)
 {
       _ipRateLimits.TryRemove(key, out _);
  }
         }

            // Cleanup forgot password limits
    foreach (var key in _forgotPasswordLimits.Keys.ToList())
  {
      if (_forgotPasswordLimits.TryGetValue(key, out var entry) && 
         now - entry.WindowStart > expiredWindow)
    {
    _forgotPasswordLimits.TryRemove(key, out _);
       }
   }
        }
    }

    /// <summary>
    /// Rate limit entry to track request count and window
    /// </summary>
    public class RateLimitEntry
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
