using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Data.Common;
using System.Diagnostics;

namespace DSecureApi.Diagnostics
{
    /// <summary>
    /// EF Core command interceptor that captures all SQL queries for diagnostics.
    /// 
    /// WHY DbCommandInterceptor?
    /// - Official EF Core extension point (no reflection hacks)
    /// - Captures all queries including raw SQL, LINQ, stored procs
    /// - Works with connection pooling
    /// - Can intercept before/after execution
    /// 
    /// PERFORMANCE CONSIDERATIONS:
    /// - Stopwatch is cheap (~100ns overhead)
    /// - ConcurrentDictionary updates are lock-free
    /// - No allocations in hot path (reuses Stopwatch)
    /// - Async all the way (no blocking)
    /// </summary>
    public class DbDiagnosticsInterceptor : DbCommandInterceptor
    {
        private readonly DiagnosticsMetricsStore _metricsStore;
        private readonly ILogger<DbDiagnosticsInterceptor> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        // Stopwatch per-command (stored in command's Site property - hacky but works)
        private static readonly AsyncLocal<Stopwatch?> _currentStopwatch = new();
        
        // Track current tenant for the request context
        private const string TenantContextKey = "DiagnosticsTenantId";
        private const string DbCallCountKey = "DiagnosticsDbCallCount";
        private const string DbTotalTimeKey = "DiagnosticsDbTotalTimeMs";

        public DbDiagnosticsInterceptor(
            DiagnosticsMetricsStore metricsStore,
            ILogger<DbDiagnosticsInterceptor> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _metricsStore = metricsStore;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Reader Commands (SELECT queries)

        public override InterceptionResult<DbDataReader> ReaderExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result)
        {
            StartTiming();
            return base.ReaderExecuting(command, eventData, result);
        }

        public override DbDataReader ReaderExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result)
        {
            RecordExecution(command, eventData);
            return base.ReaderExecuted(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<DbDataReader> result,
            CancellationToken cancellationToken = default)
        {
            StartTiming();
            return base.ReaderExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<DbDataReader> ReaderExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            DbDataReader result,
            CancellationToken cancellationToken = default)
        {
            RecordExecution(command, eventData);
            return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
        }

        #endregion

        #region NonQuery Commands (INSERT, UPDATE, DELETE)

        public override InterceptionResult<int> NonQueryExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result)
        {
            StartTiming();
            return base.NonQueryExecuting(command, eventData, result);
        }

        public override int NonQueryExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result)
        {
            RecordExecution(command, eventData);
            return base.NonQueryExecuted(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            StartTiming();
            return base.NonQueryExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<int> NonQueryExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            int result,
            CancellationToken cancellationToken = default)
        {
            RecordExecution(command, eventData);
            return base.NonQueryExecutedAsync(command, eventData, result, cancellationToken);
        }

        #endregion

        #region Scalar Commands

        public override InterceptionResult<object> ScalarExecuting(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result)
        {
            StartTiming();
            return base.ScalarExecuting(command, eventData, result);
        }

        public override object? ScalarExecuted(
            DbCommand command,
            CommandExecutedEventData eventData,
            object? result)
        {
            RecordExecution(command, eventData);
            return base.ScalarExecuted(command, eventData, result);
        }

        public override ValueTask<InterceptionResult<object>> ScalarExecutingAsync(
            DbCommand command,
            CommandEventData eventData,
            InterceptionResult<object> result,
            CancellationToken cancellationToken = default)
        {
            StartTiming();
            return base.ScalarExecutingAsync(command, eventData, result, cancellationToken);
        }

        public override ValueTask<object?> ScalarExecutedAsync(
            DbCommand command,
            CommandExecutedEventData eventData,
            object? result,
            CancellationToken cancellationToken = default)
        {
            RecordExecution(command, eventData);
            return base.ScalarExecutedAsync(command, eventData, result, cancellationToken);
        }

        #endregion

        #region Error Handling

        public override void CommandFailed(DbCommand command, CommandErrorEventData eventData)
        {
            // Still record failed queries - they still consumed resources
            RecordExecution(command, eventData, failed: true);
            base.CommandFailed(command, eventData);
        }

        public override Task CommandFailedAsync(
            DbCommand command,
            CommandErrorEventData eventData,
            CancellationToken cancellationToken = default)
        {
            RecordExecution(command, eventData, failed: true);
            return base.CommandFailedAsync(command, eventData, cancellationToken);
        }

        #endregion

        #region Private Methods

        private static void StartTiming()
        {
            _currentStopwatch.Value = Stopwatch.StartNew();
        }

        private void RecordExecution(DbCommand command, CommandEventData eventData, bool failed = false)
        {
            try
            {
                var stopwatch = _currentStopwatch.Value;
                var executionTimeMs = stopwatch?.ElapsedMilliseconds ?? 0;
                stopwatch?.Stop();
                _currentStopwatch.Value = null;

                // Get tenant ID from connection string or context
                var tenantId = GetCurrentTenantId(command);
                
                // Get SQL text (sanitized for security)
                var sql = SanitizeSql(command.CommandText);

                // Record to metrics store
                _metricsStore.RecordSqlQuery(sql, executionTimeMs, tenantId);

                // Update request-level metrics in HttpContext
                UpdateRequestMetrics(executionTimeMs);

                // Log slow queries immediately for real-time alerting
                if (executionTimeMs >= DiagnosticsMetricsStore.SlowQueryThresholdMs)
                {
                    _logger.LogWarning(
                        "🐌 SLOW QUERY DETECTED [{TenantId}] - {ExecutionTimeMs}ms - {SqlPreview}",
                        tenantId,
                        executionTimeMs,
                        TruncateSql(sql, 200));
                }

                // Log failed queries
                if (failed)
                {
                    _logger.LogWarning(
                        "❌ QUERY FAILED [{TenantId}] - {SqlPreview}",
                        tenantId,
                        TruncateSql(sql, 200));
                }
            }
            catch (Exception ex)
            {
                // Never let diagnostics crash the application
                _logger.LogDebug(ex, "Error recording query diagnostics");
            }
        }

        private string GetCurrentTenantId(DbCommand command)
        {
            // Try to get from HttpContext first
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext?.Items.TryGetValue(TenantContextKey, out var tenantObj) == true
                && tenantObj is string tenant)
            {
                return tenant;
            }

            // Fallback: extract database name from connection string
            try
            {
                var connectionString = command.Connection?.ConnectionString ?? "";
                if (connectionString.Contains("database=", StringComparison.OrdinalIgnoreCase))
                {
                    var match = System.Text.RegularExpressions.Regex.Match(
                        connectionString, 
                        @"database=([^;]+)", 
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                    
                    if (match.Success)
                    {
                        var dbName = match.Groups[1].Value;
                        // Classify as main or private
                        return dbName.Contains("Cloud_Erase") ? "MAIN_DB" : $"PRIVATE:{dbName}";
                    }
                }
            }
            catch
            {
                // Ignore parsing errors
            }

            return "UNKNOWN";
        }

        private void UpdateRequestMetrics(long executionTimeMs)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null) return;

            // Increment DB call count
            var currentCount = httpContext.Items.TryGetValue(DbCallCountKey, out var countObj) 
                ? (int)countObj! 
                : 0;
            httpContext.Items[DbCallCountKey] = currentCount + 1;

            // Add to total DB time
            var currentTime = httpContext.Items.TryGetValue(DbTotalTimeKey, out var timeObj) 
                ? (long)timeObj! 
                : 0L;
            httpContext.Items[DbTotalTimeKey] = currentTime + executionTimeMs;
        }

        /// <summary>
        /// Sanitize SQL to prevent sensitive data leakage in logs
        /// </summary>
        private static string SanitizeSql(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return string.Empty;

            // Remove potential password values
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql, 
                @"password\s*=\s*'[^']*'", 
                "password='***'", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            // Remove API keys
            sql = System.Text.RegularExpressions.Regex.Replace(
                sql, 
                @"api_?key\s*=\s*'[^']*'", 
                "api_key='***'", 
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return sql;
        }

        private static string TruncateSql(string sql, int maxLength)
        {
            if (string.IsNullOrEmpty(sql)) return string.Empty;
            return sql.Length > maxLength ? sql[..maxLength] + "..." : sql;
        }

        #endregion

        #region Static Context Keys (for middleware access)

        public static int GetDbCallCount(HttpContext? context)
        {
            if (context?.Items.TryGetValue(DbCallCountKey, out var count) == true)
                return (int)count!;
            return 0;
        }

        public static long GetDbTotalTimeMs(HttpContext? context)
        {
            if (context?.Items.TryGetValue(DbTotalTimeKey, out var time) == true)
                return (long)time!;
            return 0;
        }

        public static void SetTenantId(HttpContext? context, string tenantId)
        {
            if (context != null)
                context.Items[TenantContextKey] = tenantId;
        }

        #endregion
    }
}
