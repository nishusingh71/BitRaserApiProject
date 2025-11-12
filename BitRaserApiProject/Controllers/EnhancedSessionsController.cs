using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Sessions management controller with automatic expiration and role-based access control
    /// Supports both users and subusers with appropriate access levels
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedSessionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        
        // Default session expiration time (configurable)
        private readonly TimeSpan DefaultSessionTimeout = TimeSpan.FromHours(24); // 24 hours
        private readonly TimeSpan ExtendedSessionTimeout = TimeSpan.FromDays(7);  // 7 days for remember me

        public EnhancedSessionsController(ApplicationDbContext context, IRoleBasedAuthService authService, IUserDataService userDataService)
        {
            _context = context;
            _authService = authService;
            _userDataService = userDataService;
        }

        /// <summary>
        /// Get all sessions with role-based filtering and automatic cleanup
        /// ✅ ENHANCED: Parents can see their own sessions + subuser sessions
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetSessions([FromQuery] SessionFilterRequest? filter)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
    
            // Cleanup expired sessions first
            await CleanupExpiredSessionsAsync();
         
            IQueryable<Sessions> query = _context.Sessions;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_SESSIONS", isCurrentUserSubuser))
            {
                if (isCurrentUserSubuser)
                {
                  // ❌ Subuser - only own sessions
      query = query.Where(s => s.user_email == userEmail);
        }
            else
 {
        // ✅ ENHANCED: User - own sessions + subuser sessions
          var subuserEmails = await _context.subuser
     .Where(s => s.user_email == userEmail)
            .Select(s => s.subuser_email)
    .ToListAsync();
                    
query = query.Where(s => 
 s.user_email == userEmail ||  // Own sessions
     subuserEmails.Contains(s.user_email)  // Subuser sessions
   );
                }
       }

     // Apply additional filters if provided
            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.UserEmail))
                    query = query.Where(s => s.user_email.Contains(filter.UserEmail));

                if (!string.IsNullOrEmpty(filter.SessionStatus))
                    query = query.Where(s => s.session_status.Contains(filter.SessionStatus));

                if (!string.IsNullOrEmpty(filter.IpAddress))
                    query = query.Where(s => s.ip_address != null && s.ip_address.Contains(filter.IpAddress));

                if (filter.LoginFrom.HasValue)
                    query = query.Where(s => s.login_time >= filter.LoginFrom.Value);

                if (filter.LoginTo.HasValue)
                    query = query.Where(s => s.login_time <= filter.LoginTo.Value);

                if (filter.ActiveOnly.HasValue && filter.ActiveOnly.Value)
                    query = query.Where(s => s.session_status == "active");
            }

            var sessions = await query
                .OrderByDescending(s => s.login_time)
                .Take(filter?.PageSize ?? 100)
                .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                .ToListAsync();

            var sessionResults = sessions.Select(s => new {
                s.session_id,
                s.user_email,
                s.login_time,
                s.logout_time,
                s.ip_address,
                s.device_info,
                s.session_status,
                ExpiresAt = CalculateSessionExpiry(s.login_time, s.session_status),
                IsExpired = IsSessionExpired(s.login_time, s.logout_time, s.session_status),
                TimeRemaining = CalculateTimeRemaining(s.login_time, s.logout_time, s.session_status)
            }).ToList();

            return Ok(sessionResults);
        }

        /// <summary>
        /// Get session by ID with ownership validation
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetSession(int id)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            var session = await _context.Sessions.FindAsync(id);
            
            if (session == null) return NotFound();

            // Users and subusers can only view their own sessions unless they have admin permission
            bool canView = session.user_email == userEmail ||
                          await _authService.HasPermissionAsync(userEmail!, "READ_ALL_SESSIONS", isCurrentUserSubuser);

            if (!canView)
            {
                return StatusCode(403, new { error = "You can only view your own sessions" });
            }

            // Check if session is expired
            var isExpired = IsSessionExpired(session.login_time, session.logout_time, session.session_status);
            if (isExpired && session.session_status == "active")
            {
                // Auto-expire the session
                await ExpireSessionAsync(session);
            }

            var sessionDetails = new {
                session.session_id,
                session.user_email,
                session.login_time,
                session.logout_time,
                session.ip_address,
                session.device_info,
                session.session_status,
                ExpiresAt = CalculateSessionExpiry(session.login_time, session.session_status),
                IsExpired = isExpired,
                TimeRemaining = CalculateTimeRemaining(session.login_time, session.logout_time, session.session_status)
            };

            return Ok(sessionDetails);
        }

        /// <summary>
        /// Get user sessions by email with management hierarchy
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<IEnumerable<object>>> GetSessionsByEmail(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            // Check if user can view sessions for this email
            bool canView = email == currentUserEmail ||
                          await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SESSIONS", isCurrentUserSubuser) ||
                          await _authService.CanManageUserAsync(currentUserEmail!, email);

            if (!canView)
            {
                return StatusCode(403, new { error = "You can only view your own sessions or sessions of users you manage" });
            }

            // Cleanup expired sessions first
            await CleanupExpiredSessionsForUserAsync(email);

            var sessions = await _context.Sessions
                .Where(s => s.user_email == email)
                .OrderByDescending(s => s.login_time)
                .ToListAsync();

            var sessionResults = sessions.Select(s => new {
                s.session_id,
                s.user_email,
                s.login_time,
                s.logout_time,
                s.ip_address,
                s.device_info,
                s.session_status,
                ExpiresAt = CalculateSessionExpiry(s.login_time, s.session_status),
                IsExpired = IsSessionExpired(s.login_time, s.logout_time, s.session_status),
                TimeRemaining = CalculateTimeRemaining(s.login_time, s.logout_time, s.session_status)
            }).ToList();

            return sessionResults.Any() ? Ok(sessionResults) : NotFound();
        }

        /// <summary>
        /// Create a new session - Used during login (for both users and subusers)
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<Sessions>> CreateSession([FromBody] SessionCreateRequest request)
        {
            if (string.IsNullOrEmpty(request.UserEmail))
                return BadRequest("User email is required");

            // Validate that user or subuser exists
            bool userExists = await _userDataService.UserExistsAsync(request.UserEmail) ||
                             await _userDataService.SubuserExistsAsync(request.UserEmail);
            
            if (!userExists)
                return BadRequest("User or subuser does not exist");

            var session = new Sessions
            {
                user_email = request.UserEmail,
                login_time = DateTime.UtcNow,
                ip_address = request.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString() ?? "",
                device_info = request.DeviceInfo ?? Request.Headers["User-Agent"].ToString(),
                session_status = "active"
            };

            _context.Sessions.Add(session);
            await _context.SaveChangesAsync();

            var sessionResponse = new {
                session.session_id,
                session.user_email,
                session.login_time,
                session.ip_address,
                session.device_info,
                session.session_status,
                ExpiresAt = CalculateSessionExpiry(session.login_time, session.session_status),
                TimeRemaining = CalculateTimeRemaining(session.login_time, session.logout_time, session.session_status)
            };

            return CreatedAtAction(nameof(GetSession), new { id = session.session_id }, sessionResponse);
        }

        /// <summary>
        /// End session (logout) - Users and subusers can end their own sessions
        /// </summary>
        [HttpPatch("{id}/end")]
        public async Task<IActionResult> EndSession(int id)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            var session = await _context.Sessions.FindAsync(id);
            
            if (session == null) return NotFound();

            // Users and subusers can only end their own sessions unless they have admin permission
            bool canEnd = session.user_email == userEmail ||
                         await _authService.HasPermissionAsync(userEmail!, "END_ALL_SESSIONS", isCurrentUserSubuser);

            if (!canEnd)
            {
                return StatusCode(403, new { error = "You can only end your own sessions" });
            }

            if (session.session_status != "active")
                return BadRequest("Session is already inactive");

            session.logout_time = DateTime.UtcNow;
            session.session_status = "closed";

            _context.Entry(session).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Session ended successfully", sessionId = id });
        }

        /// <summary>
        /// End all active sessions for a user - Admin or user themselves (including subusers)
        /// </summary>
        [HttpPatch("end-all/{email}")]
        public async Task<IActionResult> EndAllUserSessions(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            // Check if user can end sessions for this email
            bool canEnd = email == currentUserEmail ||
                         await _authService.HasPermissionAsync(currentUserEmail!, "END_ALL_SESSIONS", isCurrentUserSubuser);

            if (!canEnd)
            {
                return StatusCode(403, new { error = "You can only end your own sessions or have admin permissions" });
            }

            var activeSessions = await _context.Sessions
                .Where(s => s.user_email == email && s.session_status == "active")
                .ToListAsync();

            if (!activeSessions.Any())
                return NotFound("No active sessions found for the user");

            foreach (var session in activeSessions)
            {
                session.logout_time = DateTime.UtcNow;
                session.session_status = "closed";
            }

            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Ended {activeSessions.Count} active sessions for user {email}",
                sessionIds = activeSessions.Select(s => s.session_id).ToList()
            });
        }

        /// <summary>
        /// Extend session expiration time - Users and subusers can extend their own sessions
        /// </summary>
        [HttpPatch("{id}/extend")]
        public async Task<IActionResult> ExtendSession(int id, [FromBody] SessionExtendRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var session = await _context.Sessions.FindAsync(id);
            
            if (session == null) return NotFound();

            // Users and subusers can only extend their own sessions
            if (session.user_email != userEmail)
                return StatusCode(403, new { error = "You can only extend your own sessions" });

            if (session.session_status != "active")
                return BadRequest("Cannot extend inactive session");

            // Check if session is already expired
            if (IsSessionExpired(session.login_time, session.logout_time, session.session_status))
                return BadRequest("Cannot extend expired session");

            // Update login time to effectively extend the session
            session.login_time = DateTime.UtcNow;

            // If extended session is requested, mark it
            if (request.ExtendedSession)
            {
                if (!session.device_info.Contains("[EXTENDED]"))
                    session.device_info += " [EXTENDED]";
            }

            await _context.SaveChangesAsync();

            var newExpiryTime = CalculateSessionExpiry(session.login_time, session.session_status);

            return Ok(new { 
                message = "Session extended successfully",
                sessionId = id,
                newExpiryTime = newExpiryTime,
                timeRemaining = CalculateTimeRemaining(session.login_time, session.logout_time, session.session_status)
            });
        }

        /// <summary>
        /// Get session statistics - Users and subusers can see their own stats
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetSessionStatistics([FromQuery] string? userEmail)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            // Cleanup expired sessions first
            await CleanupExpiredSessionsAsync();

            IQueryable<Sessions> query = _context.Sessions;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_SESSION_STATISTICS", isCurrentUserSubuser))
            {
                // Users and subusers can only see their own statistics
                userEmail = currentUserEmail;
            }

            if (!string.IsNullOrEmpty(userEmail))
                query = query.Where(s => s.user_email == userEmail);

            var stats = new {
                TotalSessions = await query.CountAsync(),
                ActiveSessions = await query.CountAsync(s => s.session_status == "active"),
                ClosedSessions = await query.CountAsync(s => s.session_status == "closed"),
                ExpiredSessions = await query.CountAsync(s => s.session_status == "expired"),
                SessionsToday = await query.CountAsync(s => s.login_time.Date == DateTime.UtcNow.Date),
                SessionsThisWeek = await query.CountAsync(s => s.login_time >= DateTime.UtcNow.AddDays(-7)),
                SessionsThisMonth = await query.CountAsync(s => s.login_time.Month == DateTime.UtcNow.Month),
                AverageSessionDuration = await CalculateAverageSessionDurationAsync(query),
                TopDevices = await query
                    .Where(s => !string.IsNullOrEmpty(s.device_info))
                    .GroupBy(s => s.device_info)
                    .Select(g => new { Device = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(5)
                    .ToListAsync(),
                TopIpAddresses = await query
                    .Where(s => !string.IsNullOrEmpty(s.ip_address))
                    .GroupBy(s => s.ip_address)
                    .Select(g => new { IpAddress = g.Key, Count = g.Count() })
                    .OrderByDescending(x => x.Count)
                    .Take(10)
                    .ToListAsync()
            };

            return Ok(stats);
        }

        /// <summary>
        /// Cleanup all expired sessions - Admin only
        /// </summary>
        [HttpPost("cleanup-expired")]
        public async Task<IActionResult> CleanupExpiredSessions()
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            if (!await _authService.HasPermissionAsync(userEmail!, "CLEANUP_SESSIONS", isCurrentUserSubuser))
                return StatusCode(403, new { error = "Insufficient permissions to cleanup sessions" });

            var cleanedCount = await CleanupExpiredSessionsAsync();

            return Ok(new { 
                message = $"Cleaned up {cleanedCount} expired sessions",
                cleanedCount = cleanedCount
            });
        }

        #region Private Helper Methods

        private DateTime CalculateSessionExpiry(DateTime loginTime, string sessionStatus)
        {
            if (sessionStatus != "active")
                return loginTime;

            var timeout = sessionStatus.Contains("[EXTENDED]")
                ? ExtendedSessionTimeout 
                : DefaultSessionTimeout;

            return loginTime.Add(timeout);
        }

        private bool IsSessionExpired(DateTime loginTime, DateTime? logoutTime, string sessionStatus)
        {
            if (sessionStatus != "active" || logoutTime.HasValue)
                return false;

            var expiryTime = CalculateSessionExpiry(loginTime, sessionStatus);
            return DateTime.UtcNow > expiryTime;
        }

        private string? CalculateTimeRemaining(DateTime loginTime, DateTime? logoutTime, string sessionStatus)
        {
            if (sessionStatus != "active" || logoutTime.HasValue)
                return null;

            var expiryTime = CalculateSessionExpiry(loginTime, sessionStatus);
            var timeRemaining = expiryTime - DateTime.UtcNow;

            if (timeRemaining.TotalMilliseconds <= 0)
                return "Expired";

            if (timeRemaining.TotalDays >= 1)
                return $"{(int)timeRemaining.TotalDays}d {timeRemaining.Hours}h {timeRemaining.Minutes}m";
            else if (timeRemaining.TotalHours >= 1)
                return $"{timeRemaining.Hours}h {timeRemaining.Minutes}m";
            else
                return $"{timeRemaining.Minutes}m {timeRemaining.Seconds}s";
        }

        private async Task ExpireSessionAsync(Sessions session)
        {
            session.session_status = "expired";
            session.logout_time = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        private async Task<int> CleanupExpiredSessionsAsync()
        {
            var expiredSessions = await _context.Sessions
                .Where(s => s.session_status == "active")
                .ToListAsync();

            var cleanedCount = 0;
            foreach (var session in expiredSessions)
            {
                if (IsSessionExpired(session.login_time, session.logout_time, session.session_status))
                {
                    await ExpireSessionAsync(session);
                    cleanedCount++;
                }
            }

            return cleanedCount;
        }

        private async Task<int> CleanupExpiredSessionsForUserAsync(string userEmail)
        {
            var expiredSessions = await _context.Sessions
                .Where(s => s.user_email == userEmail && s.session_status == "active")
                .ToListAsync();

            var cleanedCount = 0;
            foreach (var session in expiredSessions)
            {
                if (IsSessionExpired(session.login_time, session.logout_time, session.session_status))
                {
                    await ExpireSessionAsync(session);
                    cleanedCount++;
                }
            }

            return cleanedCount;
        }

        private async Task<string> CalculateAverageSessionDurationAsync(IQueryable<Sessions> query)
        {
            var closedSessions = await query
                .Where(s => s.logout_time.HasValue)
                .Select(s => new { s.login_time, s.logout_time })
                .ToListAsync();

            if (!closedSessions.Any())
                return "N/A";

            var totalMinutes = closedSessions
                .Where(s => s.logout_time.HasValue)
                .Average(s => (s.logout_time!.Value - s.login_time).TotalMinutes);

            if (totalMinutes >= 60)
                return $"{totalMinutes / 60:F1} hours";
            else
                return $"{totalMinutes:F1} minutes";
        }

        #endregion
    }

    /// <summary>
    /// Session filter request model
    /// </summary>
    public class SessionFilterRequest
    {
        public string? UserEmail { get; set; }
        public string? SessionStatus { get; set; }
        public string? IpAddress { get; set; }
        public DateTime? LoginFrom { get; set; }
        public DateTime? LoginTo { get; set; }
        public bool? ActiveOnly { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 100;
    }

    /// <summary>
    /// Session creation request model
    /// </summary>
    public class SessionCreateRequest
    {
        public string UserEmail { get; set; } = string.Empty;
        public string? IpAddress { get; set; }
        public string? DeviceInfo { get; set; }
    }

    /// <summary>
    /// Session extension request model
    /// </summary>
    public class SessionExtendRequest
    {
        public bool ExtendedSession { get; set; } = false; // 7 days instead of 24 hours
    }
}