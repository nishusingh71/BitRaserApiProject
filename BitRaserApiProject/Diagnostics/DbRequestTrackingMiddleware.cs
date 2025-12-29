using System.Diagnostics;

namespace BitRaserApiProject.Diagnostics
{
    /// <summary>
    /// Middleware that tracks database usage at the HTTP request level.
    /// Works with DbDiagnosticsInterceptor to correlate queries to API endpoints.
    /// 
    /// WHY Request-Level Tracking?
    /// - Answers "Which API is burning DB resources?"
    /// - Correlates multiple queries to single business operation
    /// - Identifies N+1 query patterns (high call count per request)
    /// 
    /// PLACEMENT: Must be registered BEFORE routing middleware
    /// so it wraps the entire request pipeline.
    /// </summary>
    public class DbRequestTrackingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly DiagnosticsMetricsStore _metricsStore;
        private readonly ILogger<DbRequestTrackingMiddleware> _logger;

        // Skip paths that don't use database
        private static readonly string[] SkipPaths = new[]
        {
            "/swagger",
            "/favicon.ico",
            "/health",
            "/_framework",
            "/api/diagnostics" // Don't track diagnostics checking diagnostics
        };

        public DbRequestTrackingMiddleware(
            RequestDelegate next,
            DiagnosticsMetricsStore metricsStore,
            ILogger<DbRequestTrackingMiddleware> logger)
        {
            _next = next;
            _metricsStore = metricsStore;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var path = context.Request.Path.Value?.ToLowerInvariant() ?? "/";

            // Skip non-API paths
            if (ShouldSkip(path))
            {
                await _next(context);
                return;
            }

            // Start request timing
            var requestStopwatch = Stopwatch.StartNew();

            // Set tenant context from user claims or headers
            SetTenantContext(context);

            try
            {
                // Execute the request
                await _next(context);
            }
            finally
            {
                requestStopwatch.Stop();

                // Get DB metrics from interceptor
                var dbCallCount = DbDiagnosticsInterceptor.GetDbCallCount(context);
                var dbTimeMs = DbDiagnosticsInterceptor.GetDbTotalTimeMs(context);

                // Only record if there were DB calls
                if (dbCallCount > 0)
                {
                    // Normalize path for grouping (remove IDs and GUIDs)
                    var normalizedPath = NormalizePath(path);

                    // Record to metrics store
                    _metricsStore.RecordApiRequest(normalizedPath, dbCallCount, dbTimeMs);

                    // Log high DB usage requests
                    if (dbCallCount > 10 || dbTimeMs > 500)
                    {
                        _logger.LogWarning(
                            "⚠️ HIGH DB USAGE: {Method} {Path} - {DbCalls} calls, {DbTimeMs}ms DB time, {TotalMs}ms total",
                            context.Request.Method,
                            path,
                            dbCallCount,
                            dbTimeMs,
                            requestStopwatch.ElapsedMilliseconds);
                    }
                }
            }
        }

        private static bool ShouldSkip(string path)
        {
            return SkipPaths.Any(p => path.StartsWith(p, StringComparison.OrdinalIgnoreCase));
        }

        private void SetTenantContext(HttpContext context)
        {
            // Try to determine tenant from various sources
            string tenantId = "MAIN_DB";

            // Check if user has private cloud flag
            var isPrivateCloud = context.Items.TryGetValue("IsPrivateCloudUser", out var pcFlag) 
                && (bool?)pcFlag == true;

            if (isPrivateCloud)
            {
                // Get user email for tenant identification
                var userEmail = context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                tenantId = !string.IsNullOrEmpty(userEmail) 
                    ? $"PRIVATE:{userEmail}" 
                    : "PRIVATE:UNKNOWN";
            }

            DbDiagnosticsInterceptor.SetTenantId(context, tenantId);
        }

        /// <summary>
        /// Normalize API paths to group similar endpoints.
        /// /api/users/123 -> /api/users/{id}
        /// /api/reports/abc-def-ghi -> /api/reports/{guid}
        /// </summary>
        private static string NormalizePath(string path)
        {
            if (string.IsNullOrEmpty(path)) return "/";

            // Replace numeric IDs with {id}
            var normalized = System.Text.RegularExpressions.Regex.Replace(
                path, 
                @"/\d+", 
                "/{id}");

            // Replace GUIDs with {guid}
            normalized = System.Text.RegularExpressions.Regex.Replace(
                normalized, 
                @"/[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}", 
                "/{guid}");

            // Replace email-like patterns (for routes that take email)
            normalized = System.Text.RegularExpressions.Regex.Replace(
                normalized, 
                @"/[^/]+@[^/]+\.[^/]+", 
                "/{email}");

            // Replace base64 encoded values (typically 20+ chars of base64)
            normalized = System.Text.RegularExpressions.Regex.Replace(
                normalized, 
                @"/[A-Za-z0-9+/=]{20,}", 
                "/{encoded}");

            return normalized.ToLowerInvariant();
        }
    }

    /// <summary>
    /// Extension method to register the middleware
    /// </summary>
    public static class DbRequestTrackingMiddlewareExtensions
    {
        public static IApplicationBuilder UseDbRequestTracking(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<DbRequestTrackingMiddleware>();
        }
    }
}
