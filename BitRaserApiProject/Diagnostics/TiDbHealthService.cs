using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;

namespace DSecureApi.Diagnostics
{
    /// <summary>
    /// Service to inspect TiDB cluster health using SQL queries.
    /// 
    /// WHY direct SQL instead of EF Core?
    /// - INFORMATION_SCHEMA queries are not entity-mapped
    /// - We need raw connection stats, not entity data
    /// - Minimize overhead during incident investigation
    /// 
    /// IMPORTANT: These queries run on the SAME connection pool
    /// so they're affected by pool exhaustion. Use with care.
    /// </summary>
    public interface ITiDbHealthService
    {
        Task<TiDbClusterHealth> GetClusterHealthAsync(string? connectionString = null);
        Task<List<ActiveConnection>> GetActiveConnectionsAsync(string? connectionString = null);
        Task<List<LongRunningQuery>> GetLongRunningQueriesAsync(int thresholdSeconds = 30, string? connectionString = null);
    }

    public class TiDbHealthService : ITiDbHealthService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TiDbHealthService> _logger;
        private readonly IConfiguration _configuration;

        public TiDbHealthService(
            ApplicationDbContext context,
            ILogger<TiDbHealthService> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Get comprehensive cluster health snapshot
        /// </summary>
        public async Task<TiDbClusterHealth> GetClusterHealthAsync(string? connectionString = null)
        {
            var health = new TiDbClusterHealth
            {
                CheckedAt = DateTime.UtcNow
            };

            try
            {
                using var connection = GetConnection(connectionString);
                await connection.OpenAsync();

                // Get max_connections setting
                health.MaxConnections = await GetMaxConnectionsAsync(connection);

                // Get process list stats
                var processStats = await GetProcessListStatsAsync(connection);
                health.TotalConnections = processStats.Total;
                health.ActiveConnections = processStats.Active;
                health.IdleConnections = processStats.Idle;
                health.SleepingConnections = processStats.Sleeping;

                // Calculate usage percentage
                if (health.MaxConnections > 0)
                {
                    health.ConnectionUsagePercent = (double)health.TotalConnections / health.MaxConnections * 100;
                }

                // Check for warnings
                health.Warnings = GenerateWarnings(health);

                // Get long-running queries
                health.LongRunningQueries = await GetLongRunningQueriesInternalAsync(connection, 30);

                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get TiDB cluster health");
                health.Error = ex.Message;
                health.Warnings.Add($"❌ Health check failed: {ex.Message}");
            }

            return health;
        }

        /// <summary>
        /// Get list of active (non-sleeping) connections
        /// </summary>
        public async Task<List<ActiveConnection>> GetActiveConnectionsAsync(string? connectionString = null)
        {
            var connections = new List<ActiveConnection>();

            try
            {
                using var connection = GetConnection(connectionString);
                await connection.OpenAsync();

                // Query INFORMATION_SCHEMA.PROCESSLIST for active connections
                using var command = new MySqlCommand(@"
                    SELECT 
                        ID,
                        USER,
                        HOST,
                        DB,
                        COMMAND,
                        TIME,
                        STATE,
                        LEFT(INFO, 500) as INFO
                    FROM INFORMATION_SCHEMA.PROCESSLIST
                    WHERE COMMAND != 'Sleep'
                    ORDER BY TIME DESC
                    LIMIT 50", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    connections.Add(new ActiveConnection
                    {
                        Id = reader.GetInt64(0),
                        User = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Host = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Database = reader.IsDBNull(3) ? null : reader.GetString(3),
                        Command = reader.IsDBNull(4) ? null : reader.GetString(4),
                        TimeSeconds = reader.IsDBNull(5) ? 0 : reader.GetInt32(5),
                        State = reader.IsDBNull(6) ? null : reader.GetString(6),
                        Info = reader.IsDBNull(7) ? null : reader.GetString(7)
                    });
                }

                await connection.CloseAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active connections");
            }

            return connections;
        }

        /// <summary>
        /// Get queries running longer than threshold
        /// </summary>
        public async Task<List<LongRunningQuery>> GetLongRunningQueriesAsync(
            int thresholdSeconds = 30, 
            string? connectionString = null)
        {
            try
            {
                using var connection = GetConnection(connectionString);
                await connection.OpenAsync();
                var queries = await GetLongRunningQueriesInternalAsync(connection, thresholdSeconds);
                await connection.CloseAsync();
                return queries;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get long-running queries");
                return new List<LongRunningQuery>();
            }
        }

        #region Private Methods

        private MySqlConnection GetConnection(string? connectionString)
        {
            // Try to get connection string from multiple sources
            var connStr = connectionString 
                ?? _configuration.GetConnectionString("ApplicationDbContextConnection")
                ?? _configuration.GetConnectionString("DefaultConnection")
                ?? _context.Database.GetConnectionString()
                ?? throw new InvalidOperationException("No connection string available");

            return new MySqlConnection(connStr);
        }

        private async Task<int> GetMaxConnectionsAsync(MySqlConnection connection)
        {
            try
            {
                using var command = new MySqlCommand(
                    "SHOW VARIABLES LIKE 'max_connections'", 
                    connection);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var value = reader.GetString(1);
                    if (int.TryParse(value, out var maxConn))
                        return maxConn;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get max_connections");
            }

            return 0;
        }

        private async Task<(int Total, int Active, int Idle, int Sleeping)> GetProcessListStatsAsync(
            MySqlConnection connection)
        {
            int total = 0, active = 0, idle = 0, sleeping = 0;

            try
            {
                using var command = new MySqlCommand(@"
                    SELECT 
                        COUNT(*) as total,
                        SUM(CASE WHEN COMMAND != 'Sleep' THEN 1 ELSE 0 END) as active,
                        SUM(CASE WHEN COMMAND = 'Sleep' AND TIME < 60 THEN 1 ELSE 0 END) as idle,
                        SUM(CASE WHEN COMMAND = 'Sleep' AND TIME >= 60 THEN 1 ELSE 0 END) as sleeping
                    FROM INFORMATION_SCHEMA.PROCESSLIST", connection);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    total = reader.IsDBNull(0) ? 0 : Convert.ToInt32(reader.GetValue(0));
                    active = reader.IsDBNull(1) ? 0 : Convert.ToInt32(reader.GetValue(1));
                    idle = reader.IsDBNull(2) ? 0 : Convert.ToInt32(reader.GetValue(2));
                    sleeping = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3));
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get process list stats");
            }

            return (total, active, idle, sleeping);
        }

        private async Task<List<LongRunningQuery>> GetLongRunningQueriesInternalAsync(
            MySqlConnection connection, 
            int thresholdSeconds)
        {
            var queries = new List<LongRunningQuery>();

            try
            {
                using var command = new MySqlCommand($@"
                    SELECT 
                        ID,
                        USER,
                        HOST,
                        DB,
                        TIME,
                        STATE,
                        LEFT(INFO, 1000) as INFO
                    FROM INFORMATION_SCHEMA.PROCESSLIST
                    WHERE TIME >= {thresholdSeconds}
                      AND COMMAND != 'Sleep'
                      AND INFO IS NOT NULL
                    ORDER BY TIME DESC
                    LIMIT 20", connection);

                using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    queries.Add(new LongRunningQuery
                    {
                        ProcessId = reader.GetInt64(0),
                        User = reader.IsDBNull(1) ? null : reader.GetString(1),
                        Host = reader.IsDBNull(2) ? null : reader.GetString(2),
                        Database = reader.IsDBNull(3) ? null : reader.GetString(3),
                        RunningSeconds = reader.IsDBNull(4) ? 0 : reader.GetInt32(4),
                        State = reader.IsDBNull(5) ? null : reader.GetString(5),
                        Query = reader.IsDBNull(6) ? null : SanitizeQuery(reader.GetString(6))
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get long-running queries");
            }

            return queries;
        }

        private static List<string> GenerateWarnings(TiDbClusterHealth health)
        {
            var warnings = new List<string>();

            // Connection pool pressure
            if (health.ConnectionUsagePercent >= 80)
            {
                warnings.Add($"🚨 CRITICAL: Connection usage at {health.ConnectionUsagePercent:F1}% - pool exhaustion imminent!");
            }
            else if (health.ConnectionUsagePercent >= 60)
            {
                warnings.Add($"⚠️ WARNING: Connection usage at {health.ConnectionUsagePercent:F1}% - monitor closely");
            }

            // Too many sleeping connections
            if (health.SleepingConnections > 10)
            {
                warnings.Add($"⚠️ High sleeping connections: {health.SleepingConnections} - possible connection leak");
            }

            // Long-running queries
            if (health.LongRunningQueries.Any())
            {
                warnings.Add($"🐌 {health.LongRunningQueries.Count} long-running queries detected");
            }

            // Low activity with high connections
            if (health.ActiveConnections < 5 && health.TotalConnections > 50)
            {
                warnings.Add("⚠️ Low activity but high connection count - check for leaks");
            }

            return warnings;
        }

        private static string? SanitizeQuery(string? query)
        {
            if (string.IsNullOrEmpty(query)) return query;

            // Remove sensitive data
            var sanitized = System.Text.RegularExpressions.Regex.Replace(
                query,
                @"password\s*=\s*'[^']*'",
                "password='***'",
                System.Text.RegularExpressions.RegexOptions.IgnoreCase);

            return sanitized;
        }

        #endregion
    }

    #region Health Models

    public class TiDbClusterHealth
    {
        public DateTime CheckedAt { get; set; }
        public int MaxConnections { get; set; }
        public int TotalConnections { get; set; }
        public int ActiveConnections { get; set; }
        public int IdleConnections { get; set; }
        public int SleepingConnections { get; set; }
        public double ConnectionUsagePercent { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<LongRunningQuery> LongRunningQueries { get; set; } = new();
        public string? Error { get; set; }

        // Computed health status
        public string Status => Warnings.Any(w => w.Contains("CRITICAL")) ? "CRITICAL" 
            : Warnings.Any(w => w.Contains("WARNING")) ? "WARNING" 
            : "HEALTHY";
    }

    public class ActiveConnection
    {
        public long Id { get; set; }
        public string? User { get; set; }
        public string? Host { get; set; }
        public string? Database { get; set; }
        public string? Command { get; set; }
        public int TimeSeconds { get; set; }
        public string? State { get; set; }
        public string? Info { get; set; }
    }

    public class LongRunningQuery
    {
        public long ProcessId { get; set; }
        public string? User { get; set; }
        public string? Host { get; set; }
        public string? Database { get; set; }
        public int RunningSeconds { get; set; }
        public string? State { get; set; }
        public string? Query { get; set; }
    }

    #endregion
}
