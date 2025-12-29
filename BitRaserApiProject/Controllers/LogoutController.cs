using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using BitRaserApiProject.Models;
using BitRaserApiProject.Services;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Logout controller for handling user and subuser logout with session management
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class LogoutController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<LogoutController> _logger;
        private readonly ICacheService _cacheService;

        public LogoutController(ApplicationDbContext context, ILogger<LogoutController> logger, ICacheService cacheService)
        {
            _context = context;
            _logger = logger;
            _cacheService = cacheService;
        }

        public class LogoutRequest
        {
            public int? SessionId { get; set; } // Optional: specific session to logout
            public bool LogoutAllSessions { get; set; } = false; // Logout from all active sessions
        }

        public class LogoutResponse
        {
            public string Message { get; set; } = string.Empty;
            public int SessionsEnded { get; set; }
            public DateTime LogoutTime { get; set; }
            public string Email { get; set; } = string.Empty;
            public string UserType { get; set; } = string.Empty;
        }

        /// <summary>
        /// Logout endpoint that expires sessions and invalidates JWT tokens
        /// Supports both users and subusers with single session or all sessions logout
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest? request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("user_type")?.Value;
                var sessionIdClaim = User.FindFirst("session_id")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "Invalid token or user not found" });
                }

                var isSubuser = userType == "subuser";
                var logoutTime = DateTime.UtcNow;
                var sessionsEnded = 0;

                // Determine which sessions to end
                if (request?.LogoutAllSessions == true)
                {
                    // End all active sessions for this user/subuser
                    var activeSessions = await _context.Sessions
                        .Where(s => s.user_email == userEmail && s.session_status == "active")
                        .ToListAsync();

                    foreach (var session in activeSessions)
                    {
                        session.logout_time = logoutTime;
                        session.session_status = "closed";
                        sessionsEnded++;
                    }

                    _logger.LogInformation("Ended all {Count} active sessions for {UserType}: {Email}", 
                        activeSessions.Count, isSubuser ? "subuser" : "user", userEmail);
                }
                else if (request?.SessionId != null)
                {
                    // End specific session
                    var specificSession = await _context.Sessions
                        .Where(s => s.session_id == request.SessionId && 
                                                 s.user_email == userEmail && 
                                                 s.session_status == "active").FirstOrDefaultAsync();

                    if (specificSession != null)
                    {
                        specificSession.logout_time = logoutTime;
                        specificSession.session_status = "closed";
                        sessionsEnded = 1;

                        _logger.LogInformation("Ended specific session {SessionId} for {UserType}: {Email}", 
                            request.SessionId, isSubuser ? "subuser" : "user", userEmail);
                    }
                }
                else if (int.TryParse(sessionIdClaim, out int currentSessionId))
                {
                    // End current session from JWT token
                    var currentSession = await _context.Sessions
                        .Where(s => s.session_id == currentSessionId && 
                                                 s.user_email == userEmail && 
                                                 s.session_status == "active").FirstOrDefaultAsync();

                    if (currentSession != null)
                    {
                        currentSession.logout_time = logoutTime;
                        currentSession.session_status = "closed";
                        sessionsEnded = 1;

                        _logger.LogInformation("Ended current session {SessionId} for {UserType}: {Email}", 
                            currentSessionId, isSubuser ? "subuser" : "user", userEmail);
                    }
                }
                else
                {
                    // Fallback: End all active sessions if no specific session found
                    var activeSessions = await _context.Sessions
                        .Where(s => s.user_email == userEmail && s.session_status == "active")
                        .ToListAsync();

                    foreach (var session in activeSessions)
                    {
                        session.logout_time = logoutTime;
                        session.session_status = "closed";
                        sessionsEnded++;
                    }

                    _logger.LogInformation("Fallback: Ended all {Count} active sessions for {UserType}: {Email}", 
                        activeSessions.Count, isSubuser ? "subuser" : "user", userEmail);
                }

                await _context.SaveChangesAsync();

                // Add logout entry to logs table for audit trail
                var logEntry = new logs
                {
                    user_email = userEmail,
                    log_level = "INFO",
                    log_message = $"User logout - {sessionsEnded} session(s) ended",
                    log_details_json = JsonSerializer.Serialize(new
                    {
                        user_type = isSubuser ? "subuser" : "user",
                        sessions_ended = sessionsEnded,
                        logout_time = logoutTime,
                        ip_address = HttpContext.Connection.RemoteIpAddress?.ToString(),
                        user_agent = Request.Headers["User-Agent"].ToString()
                    }),
                    created_at = logoutTime
                };

                _context.logs.Add(logEntry);
                await _context.SaveChangesAsync();

                return Ok(new LogoutResponse
                {
                    Message = sessionsEnded > 0 
                        ? $"Successfully logged out - {sessionsEnded} session(s) ended" 
                        : "No active sessions found to end",
                    SessionsEnded = sessionsEnded,
                    LogoutTime = logoutTime,
                    Email = userEmail,
                    UserType = isSubuser ? "subuser" : "user"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }

        /// <summary>
        /// Check session status - useful for frontend to verify if user is still logged in
        /// </summary>
        [HttpGet("session-status")]
        public async Task<IActionResult> GetSessionStatus()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var sessionIdClaim = User.FindFirst("session_id")?.Value;
                var userType = User.FindFirst("user_type")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var activeSessions = await _context.Sessions
                    .Where(s => s.user_email == userEmail && s.session_status == "active")
                    .OrderByDescending(s => s.login_time)
                    .Select(s => new
                    {
                        s.session_id,
                        s.login_time,
                        s.ip_address,
                        s.device_info,
                        is_current = sessionIdClaim != null && s.session_id.ToString() == sessionIdClaim
                    })
                    .ToListAsync();

                return Ok(new
                {
                    email = userEmail,
                    user_type = userType,
                    active_sessions = activeSessions,
                    total_active_sessions = activeSessions.Count,
                    session_valid = activeSessions.Any()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking session status");
                return StatusCode(500, new { message = "An error occurred while checking session status" });
            }
        }

        /// <summary>
        /// Logout from all sessions - convenience endpoint
        /// </summary>
        [HttpPost("all")]
        public async Task<IActionResult> LogoutAllSessions()
        {
            var request = new LogoutRequest { LogoutAllSessions = true };
            return await Logout(request);
        }
    }
}