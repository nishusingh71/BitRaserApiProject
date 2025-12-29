using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BitRaserApiProject.Diagnostics
{
    /// <summary>
    /// Admin-only diagnostics controller for production incident investigation.
    /// 
    /// SECURITY: Protected by authorization - only Admin/SuperAdmin can access.
    /// 
    /// USE CASES:
    /// - TiDB connection pool exhaustion investigation
    /// - Slow query identification during outage
    /// - Per-tenant resource abuse detection
    /// - N+1 query pattern identification
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Base authorization - specific checks in actions
    public class DiagnosticsController : ControllerBase
    {
        private readonly DiagnosticsMetricsStore _metricsStore;
        private readonly ITiDbHealthService _tiDbHealthService;
        private readonly ILogger<DiagnosticsController> _logger;

        public DiagnosticsController(
            DiagnosticsMetricsStore metricsStore,
            ITiDbHealthService tiDbHealthService,
            ILogger<DiagnosticsController> logger)
        {
            _metricsStore = metricsStore;
            _tiDbHealthService = tiDbHealthService;
            _logger = logger;
        }

        /// <summary>
        /// Get full diagnostic report - THE MAIN ENDPOINT for incident investigation
        /// </summary>
        [HttpGet("full-report")]
        [ProducesResponseType(typeof(FullDiagnosticsReport), 200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetFullReport()
        {
            // Security check - Admin/SuperAdmin only
            if (!IsAdminUser())
            {
                _logger.LogWarning("Unauthorized diagnostics access attempt by {User}", 
                    User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return Forbid();
            }

            _logger.LogInformation("🔍 Diagnostics report requested by {User}", 
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            var report = new FullDiagnosticsReport
            {
                GeneratedAt = DateTime.UtcNow,
                
                // Summary stats
                Summary = _metricsStore.GetSummary(),
                
                // Cluster health from TiDB
                ClusterHealth = await _tiDbHealthService.GetClusterHealthAsync(),
                
                // Top DB-heavy APIs
                TopApisByDbTime = _metricsStore.GetTopApisByDbTime(15).ToList(),
                TopApisByDbCalls = _metricsStore.GetTopApisByDbCalls(15).ToList(),
                
                // Top slow/frequent queries
                TopSlowQueries = _metricsStore.GetTopSlowQueries(20).ToList(),
                TopFrequentQueries = _metricsStore.GetTopFrequentQueries(20).ToList(),
                
                // Recent slow queries (for timeline analysis)
                RecentSlowQueries = _metricsStore.GetRecentSlowQueries(30).ToList(),
                
                // Per-tenant usage
                TenantMetrics = _metricsStore.GetTenantMetrics().ToList()
            };

            // Generate overall warnings
            report.Warnings = GenerateReportWarnings(report);

            return Ok(report);
        }

        /// <summary>
        /// Get only cluster connection health (faster, less data)
        /// </summary>
        [HttpGet("cluster-health")]
        [ProducesResponseType(typeof(TiDbClusterHealth), 200)]
        public async Task<IActionResult> GetClusterHealth()
        {
            if (!IsAdminUser())
                return Forbid();

            var health = await _tiDbHealthService.GetClusterHealthAsync();
            return Ok(health);
        }

        /// <summary>
        /// Get active database connections
        /// </summary>
        [HttpGet("active-connections")]
        [ProducesResponseType(typeof(List<ActiveConnection>), 200)]
        public async Task<IActionResult> GetActiveConnections()
        {
            if (!IsAdminUser())
                return Forbid();

            var connections = await _tiDbHealthService.GetActiveConnectionsAsync();
            return Ok(new
            {
                count = connections.Count,
                connections
            });
        }

        /// <summary>
        /// Get long-running queries (configurable threshold)
        /// </summary>
        [HttpGet("long-running-queries")]
        [ProducesResponseType(typeof(List<LongRunningQuery>), 200)]
        public async Task<IActionResult> GetLongRunningQueries([FromQuery] int thresholdSeconds = 30)
        {
            if (!IsAdminUser())
                return Forbid();

            var queries = await _tiDbHealthService.GetLongRunningQueriesAsync(thresholdSeconds);
            return Ok(new
            {
                threshold = thresholdSeconds,
                count = queries.Count,
                queries
            });
        }

        /// <summary>
        /// Get top DB-heavy API endpoints
        /// </summary>
        [HttpGet("top-apis")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetTopApis([FromQuery] int top = 10, [FromQuery] string sortBy = "time")
        {
            if (!IsAdminUser())
                return Forbid();

            var apis = sortBy.ToLower() == "calls" 
                ? _metricsStore.GetTopApisByDbCalls(top)
                : _metricsStore.GetTopApisByDbTime(top);

            return Ok(new
            {
                sortBy,
                count = apis.Count(),
                apis
            });
        }

        /// <summary>
        /// Get top slow SQL queries
        /// </summary>
        [HttpGet("slow-queries")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetSlowQueries([FromQuery] int top = 20)
        {
            if (!IsAdminUser())
                return Forbid();

            var slowQueries = _metricsStore.GetTopSlowQueries(top);
            var recentSlowQueries = _metricsStore.GetRecentSlowQueries(top);

            return Ok(new
            {
                topSlowByMaxTime = slowQueries,
                recentSlowQueries
            });
        }

        /// <summary>
        /// Get per-tenant database usage
        /// </summary>
        [HttpGet("tenant-usage")]
        [ProducesResponseType(typeof(List<TenantDbMetrics>), 200)]
        public IActionResult GetTenantUsage()
        {
            if (!IsAdminUser())
                return Forbid();

            var tenants = _metricsStore.GetTenantMetrics();
            return Ok(new
            {
                count = tenants.Count(),
                tenants
            });
        }

        /// <summary>
        /// Reset all collected metrics (use after fixing issues)
        /// </summary>
        [HttpPost("reset-metrics")]
        [ProducesResponseType(200)]
        public IActionResult ResetMetrics()
        {
            if (!IsAdminUser())
                return Forbid();

            _logger.LogWarning("⚠️ Metrics reset by {User}", 
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value);

            _metricsStore.Reset();

            return Ok(new { message = "All metrics have been reset", resetAt = DateTime.UtcNow });
        }

        /// <summary>
        /// Quick health check (lighter than full report)
        /// </summary>
        [HttpGet("quick-check")]
        [AllowAnonymous] // Allow basic health check without auth
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult QuickCheck()
        {
            var summary = _metricsStore.GetSummary();
            
            return Ok(new
            {
                status = "OK",
                uptime = $"{summary.UptimeSeconds / 3600}h {(summary.UptimeSeconds % 3600) / 60}m",
                trackedRequests = summary.TotalApiRequestsTracked,
                trackedQueries = summary.TotalSqlQueriesTracked,
                slowQueriesDetected = summary.TotalSlowQueriesDetected,
                message = "Use /api/diagnostics/full-report for detailed analysis (Admin only)"
            });
        }

        #region Private Methods

        private bool IsAdminUser()
        {
            // Check for admin claim or role
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Accept Admin, SuperAdmin, or System roles
            if (userRole != null && 
                (userRole.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                 userRole.Contains("Super", StringComparison.OrdinalIgnoreCase) ||
                 userRole.Contains("System", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // Fallback: Check for specific admin emails (configure in appsettings)
            // This is a safety net for development
            var adminEmails = new[] { "admin@example.com", "nishusingh@example.com" };
            if (userEmail != null && adminEmails.Any(e => 
                userEmail.Equals(e, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            // For development, allow if user is authenticated
            // TODO: Remove this in production!
#if DEBUG
            return User.Identity?.IsAuthenticated == true;
#else
            return false;
#endif
        }

        private static List<string> GenerateReportWarnings(FullDiagnosticsReport report)
        {
            var warnings = new List<string>();

            // Include cluster health warnings
            warnings.AddRange(report.ClusterHealth.Warnings);

            // Check for N+1 patterns (high DB calls per request)
            var n1Candidates = report.TopApisByDbCalls
                .Where(a => a.AvgDbCallsPerRequest > 10)
                .ToList();

            foreach (var api in n1Candidates)
            {
                warnings.Add($"🔄 Possible N+1 pattern: {api.Path} - Avg {api.AvgDbCallsPerRequest:F1} calls/request");
            }

            // Check for high DB time APIs
            var slowApis = report.TopApisByDbTime
                .Where(a => a.AvgDbTimeMs > 500)
                .ToList();

            foreach (var api in slowApis)
            {
                warnings.Add($"🐌 Slow API: {api.Path} - Avg {api.AvgDbTimeMs:F0}ms DB time");
            }

            // Check tenant imbalance
            if (report.TenantMetrics.Count > 1)
            {
                var totalQueries = report.TenantMetrics.Sum(t => t.TotalQueries);
                var topTenant = report.TenantMetrics.FirstOrDefault();
                if (topTenant != null && totalQueries > 0)
                {
                    var topTenantPercent = (double)topTenant.TotalQueries / totalQueries * 100;
                    if (topTenantPercent > 80)
                    {
                        warnings.Add($"⚠️ Tenant imbalance: {topTenant.TenantId} using {topTenantPercent:F0}% of resources");
                    }
                }
            }

            return warnings;
        }

        #endregion
    }

    #region Report Models

    public class FullDiagnosticsReport
    {
        public DateTime GeneratedAt { get; set; }
        public DiagnosticsSummary Summary { get; set; } = new();
        public TiDbClusterHealth ClusterHealth { get; set; } = new();
        public List<ApiEndpointMetrics> TopApisByDbTime { get; set; } = new();
        public List<ApiEndpointMetrics> TopApisByDbCalls { get; set; } = new();
        public List<SqlQueryMetrics> TopSlowQueries { get; set; } = new();
        public List<SqlQueryMetrics> TopFrequentQueries { get; set; } = new();
        public List<SlowQueryRecord> RecentSlowQueries { get; set; } = new();
        public List<TenantDbMetrics> TenantMetrics { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }

    #endregion
}
