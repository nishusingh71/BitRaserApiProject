using System.Security.Claims;
using DSecureApi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using DSecureApi.Services;
using DSecureApi.Attributes;
using DSecureApi.Utilities; // ✅ ADD: For Base64EmailEncoder.DecodeEmailParam
using System.Text.Json;
using DSecureApi.Factories; // ✅ ADDED

namespace DSecureApi.Controllers
{
    /// <summary>
    /// Enhanced Logs management controller with comprehensive role-based access control and advanced filtering
    /// Supports both users and subusers with appropriate access levels
    /// ✅ NOW SUPPORTS PRIVATE CLOUD ROUTING
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedLogsController : ControllerBase
    {
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ITenantConnectionService _tenantService;
        private readonly ILogger<EnhancedLogsController> _logger;
        private readonly ICacheService _cacheService;

        public EnhancedLogsController(
         DynamicDbContextFactory contextFactory,
            IRoleBasedAuthService authService,
            IUserDataService userDataService,
        ITenantConnectionService tenantService,
            ILogger<EnhancedLogsController> logger,
            ICacheService cacheService)
        {
            _contextFactory = contextFactory;
            _authService = authService;
            _userDataService = userDataService;
            _tenantService = tenantService;
            _logger = logger;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Get all logs with role-based filtering and advanced search
        /// ✅ ENHANCED: Parents can see their own logs + subuser logs
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetLogs([FromQuery] LogFilterRequest? filter)
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);

                _logger.LogInformation("🔍 Fetching logs for user: {Email}", userEmail);

                IQueryable<logs> query = _context.logs;

                // Apply role-based filtering
                if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_LOGS", isCurrentUserSubuser))
                {
                    if (await _authService.HasPermissionAsync(userEmail!, "READ_USER_LOGS", isCurrentUserSubuser))
                    {
                        // Managers and Support can see logs for users they manage
                        var managedUsers = await GetManagedUsersAsync(userEmail!, _context);
                        query = query.Where(l => managedUsers.Contains(l.user_email) || l.user_email == userEmail);
                    }
                    else if (isCurrentUserSubuser)
                    {
                        // ❌ Subuser - only own logs
                        query = query.Where(l => l.user_email == userEmail);
                    }
                    else
                    {
                        // ✅ ENHANCED: User - own logs + subuser logs
                        var subuserEmails = await _context.subuser
                             .Where(s => s.user_email == userEmail)
                           .Select(s => s.subuser_email)
                         .ToListAsync();

                        query = query.Where(l =>
                         l.user_email == userEmail ||  // Own logs
                          subuserEmails.Contains(l.user_email)  // Subuser logs
                      );
                    }
                }

                // Apply additional filters if provided
                if (filter != null)
                {
                    if (!string.IsNullOrEmpty(filter.UserEmail))
                        query = query.Where(l => l.user_email != null && l.user_email.Contains(filter.UserEmail));

                    if (!string.IsNullOrEmpty(filter.LogLevel))
                        query = query.Where(l => l.log_level.Contains(filter.LogLevel));

                    if (!string.IsNullOrEmpty(filter.LogMessage))
                        query = query.Where(l => l.log_message.Contains(filter.LogMessage));

                    if (filter.DateFrom.HasValue)
                        query = query.Where(l => l.created_at >= filter.DateFrom.Value);

                    if (filter.DateTo.HasValue)
                        query = query.Where(l => l.created_at <= filter.DateTo.Value);

                    if (filter.ErrorsOnly.HasValue && filter.ErrorsOnly.Value)
                        query = query.Where(l => l.log_level.ToLower().Contains("error"));

                    if (filter.WarningsOnly.HasValue && filter.WarningsOnly.Value)
                        query = query.Where(l => l.log_level.ToLower().Contains("warning"));
                }

                var logEntries = await query
                   .OrderByDescending(l => l.created_at)
                .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
            .Take(filter?.PageSize ?? 100)
                .Select(l => new
                {
                    l.log_id,
                    l.user_email,
                    l.log_level,
                    l.log_message,
                    l.created_at,
                    log_details_json = l.log_details_json
                })
               .ToListAsync();

                _logger.LogInformation("✅ Found {Count} logs from {DbType} database",
                   logEntries.Count, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                // Process HasDetails safely after materialization
                var processedLogEntries = logEntries.Select(l => new
                {
                    l.log_id,
                    l.user_email,
                    l.log_level,
                    l.log_message,
                    l.created_at,
                    HasDetails = SafeJsonCheck(l.log_details_json)
                }).ToList();

                return Ok(processedLogEntries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching logs");
                return StatusCode(500, new { message = "Error retrieving logs", error = ex.Message });
            }
        }

        /// <summary>
        /// Get log by ID with role validation
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<logs>> GetLog(int id)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var log = await _context.logs.FindAsync(id);

            if (log == null) return NotFound();

            // Check if user can view this log
            if (!await CanViewLogAsync(userEmail!, log, _context))
                return StatusCode(403, new { error = "Insufficient permissions to view this log entry" });

            return Ok(log);
        }

        /// <summary>
        /// Get logs by user email with management hierarchy
        /// </summary>
        [HttpGet("by-email/{email}")]
        [DecodeEmail]
        public async Task<ActionResult<IEnumerable<logs>>> GetLogsByEmail(string email)
        {
            // ✅ CRITICAL: Decode email before any usage
            var decodedEmail = Base64EmailEncoder.DecodeEmailParam(email);
            
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            _logger.LogInformation("🔍 Fetching logs for user: {Email} (decoded)", decodedEmail); // ✅ ADDED

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            // Check if user can view logs for this email - use decoded email
            bool canView = decodedEmail == currentUserEmail?.ToLower() ||
          await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_LOGS", isCurrentUserSubuser) ||
                     await _authService.CanManageUserAsync(currentUserEmail!, decodedEmail);

            if (!canView)
            {
                return StatusCode(403, new { error = "You can only view your own logs or logs of users you manage" });
            }

            var logEntries = await _context.logs
              .Where(l => l.user_email.ToLower() == decodedEmail) // ✅ Use decoded email
        .OrderByDescending(l => l.created_at)
                   .ToListAsync();

            _logger.LogInformation("✅ Found {Count} logs for user: {Email}", logEntries.Count, decodedEmail); // ✅ ADDED

            return logEntries.Any() ? Ok(logEntries) : NotFound();
        }

        /// <summary>
        /// Create a new log entry - Users and subusers can create their own logs
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<logs>> CreateLog([FromBody] LogCreateRequest request)
        {
            try
            {
                using var _context = await _contextFactory.CreateDbContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);

                // Allow users and subusers to create logs for themselves, or if they have special permissions
                var targetUserEmail = request.UserEmail ?? userEmail;

                bool canCreate = targetUserEmail == userEmail ||
                await _authService.HasPermissionAsync(userEmail!, "CREATE_LOG", isCurrentUserSubuser);

                if (!canCreate)
                    return StatusCode(403, new { error = "You can only create logs for yourself or if you have special permissions" });

                if (string.IsNullOrEmpty(request.LogMessage))
                    return BadRequest("Log message is required");

                var validLogLevels = new[] { "Trace", "Debug", "Info", "Information", "Warning", "Error", "Critical", "Fatal" };
                if (!validLogLevels.Contains(request.LogLevel, StringComparer.OrdinalIgnoreCase))
                    return BadRequest($"Invalid log level. Valid levels: {string.Join(", ", validLogLevels)}");

                var log = new logs
                {
                    user_email = targetUserEmail,
                    log_level = request.LogLevel,
                    log_message = request.LogMessage,
                    log_details_json = request.LogDetailsJson ?? "{}",
                    created_at = DateTime.UtcNow
                };

                _context.logs.Add(log);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Created log {LogId} for {Email} in {DbType} database",
                 log.log_id, targetUserEmail,
                   await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                return CreatedAtAction(nameof(GetLog), new { id = log.log_id }, log);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating log");
                return StatusCode(500, new { message = "Error creating log", error = ex.Message });
            }
        }

        /// <summary>
        /// Create log entry for specific user email - Admin level or self
        /// </summary>
        [HttpPost("for-user/{userEmail}")]
        [DecodeEmail]
        public async Task<ActionResult<logs>> CreateLogForUser(string userEmail, [FromBody] LogCreateRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);

            // Allow if creating for self or has admin permissions
            bool canCreate = userEmail == currentUserEmail ||
        await _authService.HasPermissionAsync(currentUserEmail!, "CREATE_LOG", isCurrentUserSubuser);

            if (!canCreate)
                return StatusCode(403, new { error = "You can only create logs for yourself or if you have special permissions" });

            // Validate that target user exists (user or subuser)
            bool targetExists = await _userDataService.UserExistsAsync(userEmail) ||
           await _userDataService.SubuserExistsAsync(userEmail);
            if (!targetExists)
                return BadRequest($"User or subuser with email {userEmail} not found");

            if (string.IsNullOrEmpty(request.LogMessage))
                return BadRequest("Log message is required");

            var validLogLevels = new[] { "Trace", "Debug", "Info", "Information", "Warning", "Error", "Critical", "Fatal" };
            if (!validLogLevels.Contains(request.LogLevel, StringComparer.OrdinalIgnoreCase))
                return BadRequest($"Invalid log level. Valid levels: {string.Join(", ", validLogLevels)}");

            var log = new logs
            {
                user_email = userEmail, // Create log for specified user
                log_level = request.LogLevel,
                log_message = request.LogMessage,
                log_details_json = request.LogDetailsJson ?? "{}",
                created_at = DateTime.UtcNow
            };

            _context.logs.Add(log);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLog), new { id = log.log_id }, log);
        }

        /// <summary>
        /// Create system log entry (for internal system use)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("system")]
        public async Task<ActionResult<logs>> CreateSystemLog([FromBody] SystemLogCreateRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            // Validate system log creation (you might want to add API key validation here)
            if (string.IsNullOrEmpty(request.LogMessage))
                return BadRequest("Log message is required");

            var log = new logs
            {
                user_email = request.UserEmail, // Can be null for system logs
                log_level = request.LogLevel ?? "Info",
                log_message = $"[SYSTEM] {request.LogMessage}",
                log_details_json = request.LogDetailsJson ?? "{}",
                created_at = DateTime.UtcNow
            };

            _context.logs.Add(log);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLog), new { id = log.log_id }, log);
        }

        /// <summary>
        /// Delete log entries - Users can delete their own logs, admins can delete any
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteLog(int id)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);

            var log = await _context.logs.FindAsync(id);
            if (log == null) return NotFound();

            // Allow deletion if it's the user's own log or they have admin permissions
            bool canDelete = log.user_email == userEmail ||
                 await _authService.HasPermissionAsync(userEmail!, "DELETE_LOG", isCurrentUserSubuser);

            if (!canDelete)
                return StatusCode(403, new { error = "You can only delete your own log entries or need admin permissions" });

            _context.logs.Remove(log);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Get log statistics and analytics - Manager level access or own logs
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetLogStatistics([FromQuery] LogStatisticsRequest? request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);

            IQueryable<logs> query = _context.logs;

            // Apply role-based filtering for statistics
            if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_LOG_STATISTICS", isCurrentUserSubuser))
            {
                if (await _authService.HasPermissionAsync(userEmail!, "READ_USER_LOG_STATISTICS", isCurrentUserSubuser))
                {
                    var managedUsers = await GetManagedUsersAsync(userEmail!, _context);
                    query = query.Where(l => managedUsers.Contains(l.user_email) || l.user_email == userEmail);
                }
                else
                {
                    // Users and subusers see only their own log statistics
                    query = query.Where(l => l.user_email == userEmail);
                }
            }

            // Apply date range filter if provided
            if (request?.DateFrom.HasValue == true)
                query = query.Where(l => l.created_at >= request.DateFrom.Value);

            if (request?.DateTo.HasValue == true)
                query = query.Where(l => l.created_at <= request.DateTo.Value);

            // ✅ CACHE: Log statistics with short TTL
            var cacheKey = $"{CacheService.CacheKeys.LogsList}:stats:{userEmail}:{request?.DateFrom?.ToString("yyyyMMdd") ?? "all"}:{request?.DateTo?.ToString("yyyyMMdd") ?? "all"}";
            var stats = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
            {
                return new
                {
                    TotalLogs = await query.CountAsync(),
                    LogsByLevel = new
                    {
                        Trace = await query.CountAsync(l => l.log_level.ToLower() == "trace"),
                        Debug = await query.CountAsync(l => l.log_level.ToLower() == "debug"),
                        Info = await query.CountAsync(l => l.log_level.ToLower().Contains("info")),
                        Warning = await query.CountAsync(l => l.log_level.ToLower().Contains("warning")),
                        Error = await query.CountAsync(l => l.log_level.ToLower().Contains("error")),
                        Critical = await query.CountAsync(l => l.log_level.ToLower().Contains("critical") || l.log_level.ToLower().Contains("fatal"))
                    },
                    LogsToday = await query.CountAsync(l => l.created_at.Date == DateTime.UtcNow.Date),
                    LogsThisWeek = await query.CountAsync(l => l.created_at >= DateTime.UtcNow.AddDays(-7)),
                    LogsThisMonth = await query.CountAsync(l => l.created_at.Month == DateTime.UtcNow.Month),
                    ErrorRate = await CalculateErrorRateAsync(query),
                    TopUsers = request?.UserEmail == null ?
                        await query
                            .Where(l => l.user_email != null)
                            .GroupBy(l => l.user_email)
                            .Select(g => new { UserEmail = g.Key, Count = g.Count() })
                            .OrderByDescending(x => x.Count)
                            .Take(10)
                            .ToListAsync() : null,
                    RecentErrors = await query
                        .Where(l => l.log_level.ToLower().Contains("error") || l.log_level.ToLower().Contains("critical"))
                        .OrderByDescending(l => l.created_at)
                        .Take(5)
                        .Select(l => new { l.log_id, l.user_email, l.log_message, l.created_at })
                        .ToListAsync(),
                    HourlyDistribution = await GetHourlyLogDistributionAsync(query)
                };
            }, CacheService.CacheTTL.Short);

            return Ok(stats);
        }

        /// <summary>
        /// Search logs with advanced filtering - Support level access or own logs
        /// </summary>
        [HttpPost("search")]
        public async Task<ActionResult<IEnumerable<object>>> SearchLogs([FromBody] LogSearchRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);

            try
            {
                IQueryable<logs> query = _context.logs;

                // Apply role-based filtering
                if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_LOGS", isCurrentUserSubuser))
                {
                    if (await _authService.HasPermissionAsync(userEmail!, "SEARCH_LOGS", isCurrentUserSubuser))
                    {
                        var managedUsers = await GetManagedUsersAsync(userEmail!, _context);
                        query = query.Where(l => managedUsers.Contains(l.user_email) || l.user_email == userEmail);
                    }
                    else
                    {
                        // Users and subusers can only search their own logs
                        query = query.Where(l => l.user_email == userEmail);
                    }
                }

                // Apply search filters - exclude problematic JSON searches if they might cause issues
                if (!string.IsNullOrEmpty(request.SearchTerm))
                {
                    query = query.Where(l =>
               l.log_message.Contains(request.SearchTerm) ||
                (l.user_email != null && l.user_email.Contains(request.SearchTerm))
            );
                }

                if (request.UserEmails?.Any() == true)
                {
                    query = query.Where(l => request.UserEmails.Contains(l.user_email));
                }

                if (request.DateFrom.HasValue)
                    query = query.Where(l => l.created_at >= request.DateFrom.Value);

                if (request.DateTo.HasValue)
                    query = query.Where(l => l.created_at <= request.DateTo.Value);

                // Handle LogLevels filtering - this requires client-side evaluation due to EF Core limitations
                List<logs> queryResults;
                if (request.LogLevels?.Any() == true)
                {
                    // Materialize the query first, then apply client-side filtering for LogLevels
                    var allFilteredLogs = await query.ToListAsync();
                    queryResults = allFilteredLogs
                          .Where(l => request.LogLevels.Any(level =>
                           l.log_level.ToLower().Contains(level.ToLower())))
                          .OrderByDescending(l => l.created_at)
                 .Take(request.MaxResults ?? 1000)
                            .ToList();
                }
                else
                {
                    // No LogLevels filtering needed, can use pure EF Core query
                    queryResults = await query
                        .OrderByDescending(l => l.created_at)
                       .Take(request.MaxResults ?? 1000)
                  .ToListAsync();
                }

                var results = queryResults.Select(l => new
                {
                    l.log_id,
                    l.user_email,
                    l.log_level,
                    l.log_message,
                    l.created_at,
                    log_details_json = l.log_details_json
                }).ToList();

                return Ok(new
                {
                    searchTerm = request.SearchTerm,
                    totalResults = results.Count,
                    results = results
                });
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                return StatusCode(500, new
                {
                    error = "Error searching logs",
                    message = ex.Message,
                    details = "There may be data integrity issues with the logs table. Please contact system administrator."
                });
            }
        }

        /// <summary>
        /// Export logs to CSV - Admin access or own logs
        /// </summary>
        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportLogsCSV([FromQuery] LogExportRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);

            IQueryable<logs> query = _context.logs;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(userEmail!, "EXPORT_ALL_LOGS", isCurrentUserSubuser))
            {
                if (await _authService.HasPermissionAsync(userEmail!, "EXPORT_LOGS", isCurrentUserSubuser))
                {
                    var managedUsers = await GetManagedUsersAsync(userEmail!, _context);
                    query = query.Where(l => managedUsers.Contains(l.user_email) || l.user_email == userEmail);
                }
                else
                {
                    // Users and subusers can only export their own logs
                    query = query.Where(l => l.user_email == userEmail);
                }
            }

            // Apply filters
            if (!string.IsNullOrEmpty(request.UserEmail))
                query = query.Where(l => l.user_email == request.UserEmail);

            if (!string.IsNullOrEmpty(request.LogLevel))
                query = query.Where(l => l.log_level == request.LogLevel);

            if (request.DateFrom.HasValue)
                query = query.Where(l => l.created_at >= request.DateFrom.Value);

            if (request.DateTo.HasValue)
                query = query.Where(l => l.created_at <= request.DateTo.Value);

            var logs = await query
             .OrderByDescending(l => l.created_at)
             .Take(request.MaxRecords ?? 10000) // Limit export size
                  .ToListAsync();

            // Generate CSV content
            var csv = GenerateCsvContent(logs);
            var fileName = $"logs_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        /// <summary>
        /// Clean up old logs based on retention policy - Admin only
        /// </summary>
        [HttpPost("cleanup")]
        public async Task<IActionResult> CleanupOldLogs([FromBody] LogCleanupRequest request)
        {
            using var _context = await _contextFactory.CreateDbContextAsync(); // ✅ ADDED

            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);

            if (!await _authService.HasPermissionAsync(userEmail!, "CLEANUP_LOGS", isCurrentUserSubuser))
                return StatusCode(403, new { error = "Insufficient permissions to cleanup logs" });

            var cutoffDate = request.RetentionDays.HasValue
      ? DateTime.UtcNow.AddDays(-request.RetentionDays.Value)
         : DateTime.UtcNow.AddDays(-90); // Default 90 days retention

            var logsToDelete = await _context.logs
    .Where(l => l.created_at < cutoffDate)
  .ToListAsync();

            if (request.LogLevelsToCleanup?.Any() == true)
            {
                logsToDelete = logsToDelete
       .Where(l => request.LogLevelsToCleanup.Any(level =>
      l.log_level.ToLower().Contains(level.ToLower())))
           .ToList();
            }

            var deletedCount = logsToDelete.Count;
            _context.logs.RemoveRange(logsToDelete);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = $"Cleaned up {deletedCount} log entries older than {cutoffDate:yyyy-MM-dd}",
                deletedCount = deletedCount,
                cutoffDate = cutoffDate
            });
        }

        #region Private Helper Methods

        private async Task<bool> CanViewLogAsync(string currentUserEmail, logs log, ApplicationDbContext context) // ✅ ADDED CONTEXT PARAMETER
        {
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

            // System admins can view all logs
            if (await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_LOGS", isCurrentUserSubuser))
                return true;

            // Users and subusers can view their own logs
            if (log.user_email == currentUserEmail)
                return true;

            // Managers can view logs of users they manage
            if (await _authService.HasPermissionAsync(currentUserEmail, "READ_USER_LOGS", isCurrentUserSubuser))
            {
                return await _authService.CanManageUserAsync(currentUserEmail, log.user_email ?? "");
            }

            return false;
        }

        private async Task<List<string>> GetManagedUsersAsync(string managerEmail, ApplicationDbContext context) // ✅ ADDED CONTEXT PARAMETER
        {
            // This would implement your user management hierarchy logic
            // For now, return the manager's own email
            var managedUsers = new List<string> { managerEmail };

            // Add logic to get users managed by this manager
            // This is a placeholder - implement based on your user hierarchy

            return managedUsers;
        }

        private async Task<double> CalculateErrorRateAsync(IQueryable<logs> query)
        {
            var totalLogs = await query.CountAsync();
            if (totalLogs == 0) return 0;

            var errorLogs = await query.CountAsync(l =>
                l.log_level.ToLower().Contains("error") ||
                l.log_level.ToLower().Contains("critical") ||
                l.log_level.ToLower().Contains("fatal"));

            return Math.Round((double)errorLogs / totalLogs * 100, 2);
        }

        private async Task<List<object>> GetHourlyLogDistributionAsync(IQueryable<logs> query)
        {
            var today = DateTime.UtcNow.Date;
            var hourlyDistribution = new List<object>();

            for (int hour = 0; hour < 24; hour++)
            {
                var hourStart = today.AddHours(hour);
                var hourEnd = hourStart.AddHours(1);

                var count = await query.CountAsync(l =>
                    l.created_at >= hourStart && l.created_at < hourEnd);

                hourlyDistribution.Add(new { Hour = hour, Count = count });
            }

            return hourlyDistribution;
        }

        private string GenerateCsvContent(List<logs> logEntries)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Log ID,User Email,Log Level,Log Message,Created At,Has Details");

            foreach (var log in logEntries)
            {
                var hasDetails = SafeJsonCheck(log.log_details_json);
                sb.AppendLine($"{log.log_id},\"{log.user_email ?? "N/A"}\",\"{log.log_level}\",\"{log.log_message.Replace("\"", "\"\"")}\",\"{log.created_at:yyyy-MM-dd HH:mm:ss}\",{hasDetails}");
            }

            return sb.ToString();
        }

        private bool SafeJsonCheck(string? jsonString)
        {
            try
            {
                if (string.IsNullOrEmpty(jsonString) || jsonString == "{}")
                    return false;

                // Try to parse as JSON to validate it's valid
                JsonDocument.Parse(jsonString);
                return true;
            }
            catch
            {
                // If JSON is invalid, return false
                return false;
            }
        }

        #endregion
    }

    /// <summary>
    /// Log filter request model
    /// </summary>
    public class LogFilterRequest
    {
        public string? UserEmail { get; set; }
        public string? LogLevel { get; set; }
        public string? LogMessage { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public bool? ErrorsOnly { get; set; }
        public bool? WarningsOnly { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 100;
    }

    /// <summary>
    /// Log creation request model
    /// </summary>
    public class LogCreateRequest
    {
        public string? UserEmail { get; set; }
        public string LogLevel { get; set; } = "Info";
        public string LogMessage { get; set; } = string.Empty;
        public string? LogDetailsJson { get; set; }
    }

    /// <summary>
    /// System log creation request model
    /// </summary>
    public class SystemLogCreateRequest
    {
        public string? UserEmail { get; set; }
        public string? LogLevel { get; set; }
        public string LogMessage { get; set; } = string.Empty;
        public string? LogDetailsJson { get; set; }
    }

    /// <summary>
    /// Log statistics request model
    /// </summary>
    public class LogStatisticsRequest
    {
        public string? UserEmail { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    /// <summary>
    /// Log search request model
    /// </summary>
    public class LogSearchRequest
    {
        public string? SearchTerm { get; set; }
        public List<string>? LogLevels { get; set; }
        public List<string>? UserEmails { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? MaxResults { get; set; } = 1000;
    }

    /// <summary>
    /// Log export request model
    /// </summary>
    public class LogExportRequest
    {
        public string? UserEmail { get; set; }
        public string? LogLevel { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public int? MaxRecords { get; set; } = 10000;
    }

    /// <summary>
    /// Log cleanup request model
    /// </summary>
    public class LogCleanupRequest
    {
        public int? RetentionDays { get; set; } = 90; // Default 90 days
        public List<string>? LogLevelsToCleanup { get; set; } // If null, cleanup all levels
    }
}