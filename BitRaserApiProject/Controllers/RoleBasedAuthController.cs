using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;
using BitRaserApiProject.Models;

namespace BitRaserApiProject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleBasedAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedAuthService _roleService;
        private readonly ILogger<RoleBasedAuthController> _logger;

        public RoleBasedAuthController(
            ApplicationDbContext context, 
            IConfiguration configuration,
            IRoleBasedAuthService roleService,
            ILogger<RoleBasedAuthController> logger)
        {
            _context = context;
            _configuration = configuration;
            _roleService = roleService;
            _logger = logger;
        }

        public class RoleBasedLoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class RoleBasedLoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public string UserType { get; set; } = string.Empty; // "user" or "subuser"
            public string Email { get; set; } = string.Empty;
            public IEnumerable<string> Roles { get; set; } = new List<string>();
            public IEnumerable<string> Permissions { get; set; } = new List<string>();
            public DateTime ExpiresAt { get; set; }
        }

        public class CreateSubuserRequest
        {
            public string SubuserEmail { get; set; } = string.Empty;
            public string SubuserPassword { get; set; } = string.Empty;
            public List<int> RoleIds { get; set; } = new List<int>();
        }

        public class AssignRoleRequest
        {
            public int UserId { get; set; }
            public int? SubuserId { get; set; }
            public int RoleId { get; set; }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] RoleBasedLoginRequest request)
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
                var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == request.Email);
                if (user != null && !string.IsNullOrEmpty(user.hash_password) && BCrypt.Net.BCrypt.Verify(request.Password, user.hash_password))
                {
                    userEmail = request.Email;
                    isSubuser = false;
                }
                else
                {
                    // Try to authenticate as subuser
                    var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == request.Email);
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

                // Create session entry for tracking
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

                var token = await GenerateJwtTokenAsync(userEmail, isSubuser);
                var roles = await _roleService.GetUserRolesAsync(userEmail, isSubuser);
                var permissions = await _roleService.GetUserPermissionsAsync(userEmail, isSubuser);

                _logger.LogInformation("User login successful: {Email} ({UserType})", userEmail, isSubuser ? "subuser" : "user");

                return Ok(new RoleBasedLoginResponse
                {
                    Token = token,
                    UserType = isSubuser ? "subuser" : "user",
                    Email = userEmail,
                    Roles = roles,
                    Permissions = permissions,
                    ExpiresAt = DateTime.UtcNow.AddHours(8)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("create-subuser")]
        [RequirePermission("UserManagement")]
        public async Task<IActionResult> CreateSubuser([FromBody] CreateSubuserRequest request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized();

                // Get the parent user
                var parentUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
                if (parentUser == null)
                    return BadRequest(new { message = "Parent user not found" });

                // Check if subuser email already exists
                var existingSubuser = await _context.subuser
                    .FirstOrDefaultAsync(s => s.subuser_email == request.SubuserEmail);
                if (existingSubuser != null)
                    return Conflict(new { message = "Subuser email already exists" });

                // Create subuser
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.SubuserPassword);
                var newSubuser = new subuser
                {
                    subuser_email = request.SubuserEmail,
                    subuser_password = hashedPassword,
                    user_email = userEmail,
                    superuser_id = parentUser.user_id
                };

                _context.subuser.Add(newSubuser);
                await _context.SaveChangesAsync();

                // Assign roles if provided
                foreach (var roleId in request.RoleIds)
                {
                    // Validate that the parent user can assign this role
                    var role = await _context.Roles.FindAsync(roleId);
                    if (role != null)
                    {
                        var parentUserLevel = await _roleService.GetUserHierarchyLevelAsync(userEmail, false);
                        if (parentUserLevel < role.HierarchyLevel) // Can only assign lower privilege roles
                        {
                            await _roleService.AssignRoleToSubuserAsync(newSubuser.subuser_id, roleId, userEmail);
                        }
                    }
                }

                return Ok(new { message = "Subuser created successfully", subuserId = newSubuser.subuser_id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating subuser");
                return StatusCode(500, new { message = "An error occurred while creating subuser" });
            }
        }

        [HttpPost("assign-role")]
        [RequirePermission("UserManagement")]
        public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request)
        {
            try
            {
                var assignerEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(assignerEmail))
                    return Unauthorized();

                // Validate role exists
                var role = await _context.Roles.FindAsync(request.RoleId);
                if (role == null)
                    return BadRequest(new { message = "Role not found" });

                // Check if assigner can assign this role (must have higher privilege)
                var assignerLevel = await _roleService.GetUserHierarchyLevelAsync(assignerEmail, false);
                if (assignerLevel >= role.HierarchyLevel)
                    return StatusCode(403,new { error = "You cannot assign a role with equal or higher privilege than your own" });

                bool success;
                if (request.SubuserId.HasValue)
                {
                    // Assigning to subuser
                    var subuser = await _context.subuser.FindAsync(request.SubuserId.Value);
                    if (subuser == null)
                        return BadRequest(new { message = "Subuser not found" });

                    // Check if assigner can manage this subuser
                    var canManage = await _roleService.CanManageUserAsync(assignerEmail, subuser.subuser_email, true);
                    if (!canManage)
                        return StatusCode(403,new { error = "You cannot manage this subuser" });

                    success = await _roleService.AssignRoleToSubuserAsync(request.SubuserId.Value, request.RoleId, assignerEmail);
                }
                else
                {
                    // Assigning to main user
                    var user = await _context.Users.FindAsync(request.UserId);
                    if (user == null)
                        return BadRequest(new { message = "User not found" });

                    // Check if assigner can manage this user
                    var canManage = await _roleService.CanManageUserAsync(assignerEmail, user.user_email, false);
                    if (!canManage)
                        return StatusCode(403,new { error = "You cannot manage this user" });

                    success = await _roleService.AssignRoleToUserAsync(request.UserId, request.RoleId, assignerEmail);
                }

                if (success)
                    return Ok(new { message = "Role assigned successfully" });
                else
                    return BadRequest(new { message = "Failed to assign role" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning role");
                return StatusCode(500, new { message = "An error occurred while assigning role" });
            }
        }

        [HttpDelete("remove-role")]
        [RequirePermission("UserManagement")]
        public async Task<IActionResult> RemoveRole([FromBody] AssignRoleRequest request)
        {
            try
            {
                var assignerEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(assignerEmail))
                    return Unauthorized();

                bool success;
                if (request.SubuserId.HasValue)
                {
                    var subuser = await _context.subuser.FindAsync(request.SubuserId.Value);
                    if (subuser == null)
                        return BadRequest(new { message = "Subuser not found" });

                    var canManage = await _roleService.CanManageUserAsync(assignerEmail, subuser.subuser_email, true);
                    if (!canManage)
                        return StatusCode(403,new { error = "You cannot manage this subuser" });

                    success = await _roleService.RemoveRoleFromSubuserAsync(request.SubuserId.Value, request.RoleId);
                }
                else
                {
                    var user = await _context.Users.FindAsync(request.UserId);
                    if (user == null)
                        return BadRequest(new { message = "User not found" });

                    var canManage = await _roleService.CanManageUserAsync(assignerEmail, user.user_email, false);
                    if (!canManage)
                        return StatusCode(403,new { error = "You cannot manage this user" });

                    success = await _roleService.RemoveRoleFromUserAsync(request.UserId, request.RoleId);
                }

                if (success)
                    return Ok(new { message = "Role removed successfully" });
                else
                    return BadRequest(new { message = "Failed to remove role" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing role");
                return StatusCode(500, new { message = "An error occurred while removing role" });
            }
        }

        [HttpGet("my-permissions")]
        [Authorize]
        public async Task<IActionResult> GetMyPermissions()
        {
            try
            {
                var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(email))
                    return Unauthorized();

                var isSubuser = await _context.subuser.AnyAsync(s => s.subuser_email == email);
                var permissions = await _roleService.GetUserPermissionsAsync(email, isSubuser);
                var roles = await _roleService.GetUserRolesAsync(email, isSubuser);

                return Ok(new { permissions, roles, userType = isSubuser ? "subuser" : "user" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions");
                return StatusCode(500, new { message = "An error occurred while getting permissions" });
            }
        }

        [HttpGet("roles")]
        [RequirePermission("UserManagement")]
        public async Task<IActionResult> GetAvailableRoles()
        {
            try
            {
                var roles = await _context.Roles
                    .Select(r => new { r.RoleId, r.RoleName, r.Description, r.HierarchyLevel })
                    .OrderBy(r => r.HierarchyLevel)
                    .ToListAsync();

                return Ok(roles);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available roles");
                return StatusCode(500, new { message = "An error occurred while getting roles" });
            }
        }

        [HttpGet("permissions")]
        [RequirePermission("UserManagement")]
        public async Task<IActionResult> GetAvailablePermissions()
        {
            try
            {
                var permissions = await _context.Permissions
                    .Select(p => new { p.PermissionId, p.PermissionName, p.Description })
                    .ToListAsync();

                return Ok(permissions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting available permissions");
                return StatusCode(500, new { message = "An error occurred while getting permissions" });
            }
        }

        [HttpGet("my-subusers")]
        [Authorize]
        public async Task<IActionResult> GetMySubusers()
        {
            try
            {
                var email = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(email))
                    return Unauthorized();

                var subusers = await _context.subuser
                    .Where(s => s.user_email == email)
                    .Include(s => s.SubuserRoles)
                    .ThenInclude(sr => sr.Role)
                    .Select(s => new
                    {
                        s.subuser_id,
                        s.subuser_email,
                        roles = s.SubuserRoles.Select(sr => new { sr.Role.RoleId, sr.Role.RoleName }).ToList()
                    })
                    .ToListAsync();

                return Ok(subusers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subusers");
                return StatusCode(500, new { message = "An error occurred while getting subusers" });
            }
        }

        private async Task<string> GenerateJwtTokenAsync(string email, bool isSubuser)
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
                new Claim("user_type", isSubuser ? "subuser" : "user")
            };

            // Add role claims
            var roles = await _roleService.GetUserRolesAsync(email, isSubuser);
            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddHours(8),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Simple Logout - Clear JWT token and automatically logout user from system
        /// </summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("user_type")?.Value;

                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                var isSubuser = userType == "subuser";
                var logoutTime = DateTime.UtcNow;

                // End all active sessions for this user
                var activeSessions = await _context.Sessions
                    .Where(s => s.user_email == userEmail && s.session_status == "active")
                    .ToListAsync();

                foreach (var session in activeSessions)
                {
                    session.logout_time = logoutTime;
                    session.session_status = "closed";
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User logout: {Email} ({UserType})", userEmail, isSubuser ? "subuser" : "user");

                // Set response headers to help clear token from browser/Swagger
                Response.Headers["Clear-Site-Data"] = "\"storage\"";
                Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";

                return Ok(new
                {
                    success = true,
                    message = "Logout successful - JWT token cleared, user logged out automatically",
                    email = userEmail,
                    userType = isSubuser ? "subuser" : "user",
                    logoutTime = logoutTime,
                    // Add Swagger UI clearing instructions
                    clearToken = true,
                    swaggerLogout = true
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Logout failed" });
            }
        }
    }
}