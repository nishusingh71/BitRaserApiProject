using System.Collections.Concurrent;

namespace DSecureApi.Diagnostics
{
    /// <summary>
    /// Thread-safe in-memory metrics store for database diagnostics.
    /// Uses ConcurrentDictionary to avoid locks and ensure high-throughput under load.
    /// 
    /// WHY ConcurrentDictionary?
    /// - Lock-free reads (critical during incident debugging)
    /// - Atomic updates via AddOrUpdate
    /// - No contention bottleneck under high traffic
    /// 
    /// WHY in-memory only?
    /// - Zero latency for reads
    /// - No additional DB load during outage
    /// - Acceptable to lose metrics on restart (diagnostics, not business data)
    /// </summary>
    public class DiagnosticsMetricsStore
    {
        // API endpoint metrics: path -> stats
        private readonly ConcurrentDictionary<string, ApiEndpointMetrics> _apiMetrics = new();
        
        // SQL query metrics: queryHash -> stats
        private readonly ConcurrentDictionary<string, SqlQueryMetrics> _sqlMetrics = new();
        
        // Per-tenant metrics: tenantId (main/private) -> stats
        private readonly ConcurrentDictionary<string, TenantDbMetrics> _tenantMetrics = new();
        
        // Recent slow queries (ring buffer, max 100)
        private readonly ConcurrentQueue<SlowQueryRecord> _slowQueries = new();
        private const int MaxSlowQueryRecords = 100;
        
        // Slow query threshold in milliseconds
        public const int SlowQueryThresholdMs = 1000;
        
        // Store start time for uptime calculation
        public DateTime StartTime { get; } = DateTime.UtcNow;

        #region API Endpoint Metrics

        /// <summary>
        /// Record metrics for an API request
        /// </summary>
        public void RecordApiRequest(string path, int dbCallCount, long dbTimeMs)
        {
            _apiMetrics.AddOrUpdate(
                path,
                // Add new entry
                _ => new ApiEndpointMetrics
                {
                    Path = path,
                    TotalRequests = 1,
                    TotalDbCalls = dbCallCount,
                    TotalDbTimeMs = dbTimeMs,
                    MaxDbTimeMs = dbTimeMs,
                    LastAccessTime = DateTime.UtcNow
                },
                // Update existing entry (atomic)
                (_, existing) =>
                {
                    existing.TotalRequests++;
                    existing.TotalDbCalls += dbCallCount;
                    existing.TotalDbTimeMs += dbTimeMs;
                    if (dbTimeMs > existing.MaxDbTimeMs)
                        existing.MaxDbTimeMs = dbTimeMs;
                    existing.LastAccessTime = DateTime.UtcNow;
                    return existing;
                });
        }

        public IEnumerable<ApiEndpointMetrics> GetTopApisByDbTime(int top = 10)
        {
            return _apiMetrics.Values
                .OrderByDescending(m => m.TotalDbTimeMs)
                .Take(top)
                .ToList();
        }

        public IEnumerable<ApiEndpointMetrics> GetTopApisByDbCalls(int top = 10)
        {
            return _apiMetrics.Values
                .OrderByDescending(m => m.TotalDbCalls)
                .Take(top)
                .ToList();
        }

        #endregion

        #region SQL Query Metrics

        /// <summary>
        /// Record metrics for a SQL query execution
        /// </summary>
        public void RecordSqlQuery(string sql, long executionTimeMs, string tenantId)
        {
            // Create a hash for the query to group similar queries
            var queryHash = GetQueryHash(sql);
            
            _sqlMetrics.AddOrUpdate(
                queryHash,
                _ => new SqlQueryMetrics
                {
                    QueryHash = queryHash,
                    SampleQuery = TruncateSql(sql, 500), // Keep sample for debugging
                    TotalExecutions = 1,
                    TotalTimeMs = executionTimeMs,
                    MaxTimeMs = executionTimeMs,
                    MinTimeMs = executionTimeMs,
                    LastExecutionTime = DateTime.UtcNow
                },
                (_, existing) =>
                {
                    existing.TotalExecutions++;
                    existing.TotalTimeMs += executionTimeMs;
                    if (executionTimeMs > existing.MaxTimeMs)
                        existing.MaxTimeMs = executionTimeMs;
                    if (executionTimeMs < existing.MinTimeMs)
                        existing.MinTimeMs = executionTimeMs;
                    existing.LastExecutionTime = DateTime.UtcNow;
                    return existing;
                });

            // Track slow queries separately for quick access
            if (executionTimeMs >= SlowQueryThresholdMs)
            {
                RecordSlowQuery(sql, executionTimeMs, tenantId);
            }

            // Update tenant metrics
            RecordTenantDbUsage(tenantId, executionTimeMs);
        }

        private void RecordSlowQuery(string sql, long executionTimeMs, string tenantId)
        {
            _slowQueries.Enqueue(new SlowQueryRecord
            {
                Query = TruncateSql(sql, 1000),
                ExecutionTimeMs = executionTimeMs,
                TenantId = tenantId,
                Timestamp = DateTime.UtcNow
            });

            // Keep queue bounded (ring buffer behavior)
            while (_slowQueries.Count > MaxSlowQueryRecords)
            {
                _slowQueries.TryDequeue(out _);
            }
        }

        public IEnumerable<SqlQueryMetrics> GetTopSlowQueries(int top = 10)
        {
            return _sqlMetrics.Values
                .Where(m => m.MaxTimeMs >= SlowQueryThresholdMs)
                .OrderByDescending(m => m.MaxTimeMs)
                .Take(top)
                .ToList();
        }

        public IEnumerable<SqlQueryMetrics> GetTopFrequentQueries(int top = 10)
        {
            return _sqlMetrics.Values
                .OrderByDescending(m => m.TotalExecutions)
                .Take(top)
                .ToList();
        }

        public IEnumerable<SlowQueryRecord> GetRecentSlowQueries(int count = 20)
        {
            return _slowQueries.Reverse().Take(count).ToList();
        }

        #endregion

        #region Tenant Metrics

        private void RecordTenantDbUsage(string tenantId, long executionTimeMs)
        {
            _tenantMetrics.AddOrUpdate(
                tenantId,
                _ => new TenantDbMetrics
                {
                    TenantId = tenantId,
                    TotalQueries = 1,
                    TotalTimeMs = executionTimeMs,
                    LastActivityTime = DateTime.UtcNow
                },
                (_, existing) =>
                {
                    existing.TotalQueries++;
                    existing.TotalTimeMs += executionTimeMs;
                    existing.LastActivityTime = DateTime.UtcNow;
                    return existing;
                });
        }

        public IEnumerable<TenantDbMetrics> GetTenantMetrics()
        {
            return _tenantMetrics.Values.OrderByDescending(m => m.TotalTimeMs).ToList();
        }

        #endregion

        #region Summary Stats

        public DiagnosticsSummary GetSummary()
        {
            var sqlMetrics = _sqlMetrics.Values.ToList();
            var apiMetrics = _apiMetrics.Values.ToList();
            
            return new DiagnosticsSummary
            {
                UptimeSeconds = (long)(DateTime.UtcNow - StartTime).TotalSeconds,
                TotalApiRequestsTracked = apiMetrics.Sum(m => m.TotalRequests),
                TotalSqlQueriesTracked = sqlMetrics.Sum(m => m.TotalExecutions),
                TotalSlowQueriesDetected = _slowQueries.Count,
                UniqueApiEndpoints = apiMetrics.Count,
                UniqueSqlQueries = sqlMetrics.Count,
                TenantCount = _tenantMetrics.Count
            };
        }

        /// <summary>
        /// Reset all metrics (useful for testing or after fixing issues)
        /// </summary>
        public void Reset()
        {
            _apiMetrics.Clear();
            _sqlMetrics.Clear();
            _tenantMetrics.Clear();
            while (_slowQueries.TryDequeue(out _)) { }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Create a hash for SQL query to group similar queries.
        /// Normalizes parameters to group queries with different values.
        /// </summary>
        private static string GetQueryHash(string sql)
        {
            if (string.IsNullOrEmpty(sql)) return "EMPTY";

            // Normalize: remove parameter values, collapse whitespace
            var normalized = System.Text.RegularExpressions.Regex.Replace(sql, @"'[^']*'", "'?'");
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\d+", "N");
            normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"\s+", " ");
            
            // Take first 100 chars + hash for uniqueness
            var prefix = normalized.Length > 100 ? normalized[..100] : normalized;
            var hash = normalized.GetHashCode().ToString("X8");
            
            return $"{prefix}...{hash}";
        }

        private static string TruncateSql(string sql, int maxLength)
        {
            if (string.IsNullOrEmpty(sql)) return string.Empty;
            return sql.Length > maxLength ? sql[..maxLength] + "..." : sql;
        }

        #endregion
    }

    #region Metric Classes

    public class ApiEndpointMetrics
    {
        public string Path { get; set; } = string.Empty;
        public long TotalRequests { get; set; }
        public long TotalDbCalls { get; set; }
        public long TotalDbTimeMs { get; set; }
        public long MaxDbTimeMs { get; set; }
        public DateTime LastAccessTime { get; set; }

        // Computed
        public double AvgDbTimeMs => TotalRequests > 0 ? (double)TotalDbTimeMs / TotalRequests : 0;
        public double AvgDbCallsPerRequest => TotalRequests > 0 ? (double)TotalDbCalls / TotalRequests : 0;
    }

    public class SqlQueryMetrics
    {
        public string QueryHash { get; set; } = string.Empty;
        public string SampleQuery { get; set; } = string.Empty;
        public long TotalExecutions { get; set; }
        public long TotalTimeMs { get; set; }
        public long MaxTimeMs { get; set; }
        public long MinTimeMs { get; set; }
        public DateTime LastExecutionTime { get; set; }

        // Computed
        public double AvgTimeMs => TotalExecutions > 0 ? (double)TotalTimeMs / TotalExecutions : 0;
    }

    public class TenantDbMetrics
    {
        public string TenantId { get; set; } = string.Empty;
        public long TotalQueries { get; set; }
        public long TotalTimeMs { get; set; }
        public DateTime LastActivityTime { get; set; }

        // Computed
        public double AvgQueryTimeMs => TotalQueries > 0 ? (double)TotalTimeMs / TotalQueries : 0;
    }

    public class SlowQueryRecord
    {
        public string Query { get; set; } = string.Empty;
        public long ExecutionTimeMs { get; set; }
        public string TenantId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    public class DiagnosticsSummary
    {
        public long UptimeSeconds { get; set; }
        public long TotalApiRequestsTracked { get; set; }
        public long TotalSqlQueriesTracked { get; set; }
        public long TotalSlowQueriesDetected { get; set; }
        public int UniqueApiEndpoints { get; set; }
        public int UniqueSqlQueries { get; set; }
        public int TenantCount { get; set; }
    }

    #endregion
}
