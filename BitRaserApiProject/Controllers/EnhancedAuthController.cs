using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using BitRaserApiProject.Services;
using BitRaserApiProject.Models;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Authentication controller with session management
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EnhancedAuthController> _logger;
        private readonly ICacheService _cacheService;

        public EnhancedAuthController(
            ApplicationDbContext context, 
            IConfiguration configuration,
            ILogger<EnhancedAuthController> logger,
            ICacheService cacheService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _cacheService = cacheService;
        }

        public class LoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class LoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public string UserType { get; set; } = string.Empty; // "user" or "subuser"
            public string Email { get; set; } = string.Empty;
            public DateTime ExpiresAt { get; set; }
            public int SessionId { get; set; }
        }

        /// <summary>
        /// Enhanced login with session creation for both users and subusers
        /// </summary>

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email and password are required" });
                }

                string? userEmail = null;
                bool isSubuser = false;

                // Try to authenticate as main user first
                var user = await _context.Users.Where(u => u.user_email == request.Email).FirstOrDefaultAsync();
                if (user != null && !string.IsNullOrEmpty(user.hash_password) && BCrypt.Net.BCrypt.Verify(request.Password, user.hash_password))
                {
                    userEmail = request.Email;
                    isSubuser = false;
                }
                else
                {
                    // Try to authenticate as subuser
                    var subuser = await _context.subuser.Where(s => s.subuser_email == request.Email).FirstOrDefaultAsync();
                    if (subuser != null && BCrypt.Net.BCrypt.Verify(request.Password, subuser.subuser_password))
                    {
                        userEmail = request.Email;
                        isSubuser = true;
                    }
                }

                if (userEmail == null)
                {
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                // Create session entry
                var session = new Sessions
                {
                    user_email = userEmail,
                    login_time = DateTime.UtcNow,
                    ip_address = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    device_info = Request.Headers["User-Agent"].ToString(),
                    session_status = "active"
                };

                _context.Sessions.Add(session);
                await _context.SaveChangesAsync();

                var token = GenerateJwtToken(userEmail, isSubuser, session.session_id);

                _logger.LogInformation("Successful login for {UserType}: {Email} with session ID: {SessionId}", 
                    isSubuser ? "subuser" : "user", userEmail, session.session_id);

                // Log the login event
                var logEntry = new logs
                {
                    user_email = userEmail,
                    log_level = "INFO",
                    log_message = $"User login successful",
                    log_details_json = JsonSerializer.Serialize(new
                    {
                        user_type = isSubuser ? "subuser" : "user",
                        session_id = session.session_id,
                        login_time = session.login_time,
                        ip_address = session.ip_address,
                        user_agent = session.device_info
                    }),
                    created_at = DateTime.UtcNow
                };

                _context.logs.Add(logEntry);
                await _context.SaveChangesAsync();

                return Ok(new LoginResponse
                {
                    Token = token,
                    UserType = isSubuser ? "subuser" : "user",
                    Email = userEmail,
                    ExpiresAt = DateTime.UtcNow.AddHours(8),
                    SessionId = session.session_id
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        /// <summary>
        /// Refresh token endpoint
        /// </summary>
        [HttpPost("refresh")]
        [Authorize]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("user_type")?.Value;
                var sessionIdClaim = User.FindFirst("session_id")?.Value;

                if (string.IsNullOrEmpty(userEmail) || !int.TryParse(sessionIdClaim, out int sessionId))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Verify session is still active
                var session = await _context.Sessions
                    .Where(s => s.session_id == sessionId && 
                                             s.user_email == userEmail && 
                                             s.session_status == "active").FirstOrDefaultAsync();

                if (session == null)
                {
                    return Unauthorized(new { message = "Session expired or invalid" });
                }

                var isSubuser = userType == "subuser";
                var newToken = GenerateJwtToken(userEmail, isSubuser, sessionId);

                // Update session login time to extend it
                session.login_time = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new LoginResponse
                {
                    Token = newToken,
                    UserType = isSubuser ? "subuser" : "user",
                    Email = userEmail,
                    ExpiresAt = DateTime.UtcNow.AddHours(8),
                    SessionId = sessionId
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new { message = "An error occurred during token refresh" });
            }
        }

        /// <summary>
        /// Validate token endpoint
        /// </summary>
        [HttpPost("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("user_type")?.Value;
                var sessionIdClaim = User.FindFirst("session_id")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var activeSessions = await _context.Sessions
                    .Where(s => s.user_email == userEmail && s.session_status == "active")
                    .CountAsync();

                return Ok(new
                {
                    valid = true,
                    email = userEmail,
                    user_type = userType,
                    session_id = sessionIdClaim,
                    active_sessions = activeSessions,
                    message = "Token is valid"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token validation");
                return Unauthorized(new { message = "Token validation failed" });
            }
        }

        private string GenerateJwtToken(string email, bool isSubuser, int sessionId)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"];
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            if (string.IsNullOrEmpty(secretKey))
                throw new InvalidOperationException("JWT secret key is not configured");

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("user_type", isSubuser ? "subuser" : "user"),
                new Claim("session_id", sessionId.ToString())
            };

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}