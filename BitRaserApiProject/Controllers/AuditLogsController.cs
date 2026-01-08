using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using BitRaserApiProject.Factories;
using System.Text;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// AuditLogsController - Provides access to system audit logs
    /// Route: api/audit
    /// </summary>
    [ApiController]
    [Route("api/audit")]
    [Authorize]
    public class AuditLogsController : ControllerBase
    {
        private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
        private readonly ILogger<AuditLogsController> _logger;

        public AuditLogsController(
            IDbContextFactory<ApplicationDbContext> contextFactory,
            ILogger<AuditLogsController> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        /// <summary>
        /// GET: api/audit/logs
        /// Get audit logs with optional filters
        /// </summary>
        [HttpGet("logs")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] string? level = null,
            [FromQuery] string? category = null,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] string? userEmail = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.Set<logs>().AsNoTracking();

                // Apply filters
                if (!string.IsNullOrEmpty(level))
                {
                    query = query.Where(l => l.log_level == level);
                }

                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(l => l.log_message.Contains(category));
                }

                if (startDate.HasValue)
                {
                    query = query.Where(l => l.created_at >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(l => l.created_at <= endDate.Value);
                }

                if (!string.IsNullOrEmpty(userEmail))
                {
                    var decodedEmail = DecodeEmail(userEmail);
                    query = query.Where(l => l.user_email == decodedEmail);
                }

                var total = await query.CountAsync();
                var logsList = await query
                    .OrderByDescending(l => l.created_at)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new
                    {
                        id = l.log_id,
                        level = l.log_level,
                        message = l.log_message,
                        userEmail = l.user_email,
                        details = l.log_details_json,
                        timestamp = l.created_at
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = logsList,
                    pagination = new
                    {
                        page,
                        pageSize,
                        total,
                        totalPages = (int)Math.Ceiling((double)total / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching audit logs");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/audit/user/{userId}
        /// Get audit logs for a specific user
        /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetUserAuditLogs(
            string userId,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var decodedUserId = DecodeEmail(userId);

                var query = context.Set<logs>()
                    .AsNoTracking()
                    .Where(l => l.user_email == decodedUserId);

                if (startDate.HasValue)
                {
                    query = query.Where(l => l.created_at >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(l => l.created_at <= endDate.Value);
                }

                var total = await query.CountAsync();
                var logsList = await query
                    .OrderByDescending(l => l.created_at)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(l => new
                    {
                        id = l.log_id,
                        level = l.log_level,
                        message = l.log_message,
                        details = l.log_details_json,
                        timestamp = l.created_at
                    })
                    .ToListAsync();

                // Also get session activity for this user
                var sessions = await context.Sessions
                    .AsNoTracking()
                    .Where(s => s.user_email == decodedUserId)
                    .OrderByDescending(s => s.login_time)
                    .Take(10)
                    .Select(s => new
                    {
                        sessionId = s.session_id,
                        status = s.session_status,
                        loginTime = s.login_time,
                        logoutTime = s.logout_time,
                        ipAddress = s.ip_address,
                        device = s.device_info
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    userId = decodedUserId,
                    data = new
                    {
                        logs = logsList,
                        recentSessions = sessions
                    },
                    pagination = new
                    {
                        page,
                        pageSize,
                        total,
                        totalPages = (int)Math.Ceiling((double)total / pageSize)
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching audit logs for user {UserId}", userId);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: api/audit/summary
        /// Get audit logs summary statistics
        /// </summary>
        [HttpGet("summary")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetAuditSummary()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var now = DateTime.UtcNow;
                var today = now.Date;
                var weekAgo = now.AddDays(-7);
                var monthAgo = now.AddDays(-30);

                var totalLogs = await context.Set<logs>().CountAsync();
                var todayLogs = await context.Set<logs>().CountAsync(l => l.created_at >= today);
                var weekLogs = await context.Set<logs>().CountAsync(l => l.created_at >= weekAgo);
                var monthLogs = await context.Set<logs>().CountAsync(l => l.created_at >= monthAgo);

                var errorLogs = await context.Set<logs>().CountAsync(l => l.log_level == "Error");
                var warningLogs = await context.Set<logs>().CountAsync(l => l.log_level == "Warning");
                var infoLogs = await context.Set<logs>().CountAsync(l => l.log_level == "Info");

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        total = totalLogs,
                        today = todayLogs,
                        thisWeek = weekLogs,
                        thisMonth = monthLogs,
                        byLevel = new
                        {
                            errors = errorLogs,
                            warnings = warningLogs,
                            info = infoLogs
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching audit summary");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private static string DecodeEmail(string base64Email)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64Email);
                return Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return base64Email;
            }
        }
    }
}
