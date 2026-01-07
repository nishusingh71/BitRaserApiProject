using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using BCrypt.Net;

namespace BitRaserApiProject.Controllers    
{
    /// <summary>
    /// Dashboard Authentication Controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedAuthService _authService;
        private readonly ILogger<DashboardAuthController> _logger;
        private readonly ICacheService _cacheService;

        public DashboardAuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IRoleBasedAuthService authService,
            ILogger<DashboardAuthController> logger,
            ICacheService cacheService)
        {
            _context = context;
            _configuration = configuration;
            _authService = authService;
            _logger = logger;
            _cacheService = cacheService;
        }

        // POST: api/DashboardAuth/login
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<ActionResult<DashboardLoginResponseDto>> Login([FromBody] DashboardLoginRequestDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email and password are required" });
                }

                // Try main user authentication
                var user = await _context.Users.AsNoTracking().Where(u => u.user_email == request.Email).FirstOrDefaultAsync();
                
                if (user != null && !string.IsNullOrEmpty(user.hash_password) && 
                    BCrypt.Net.BCrypt.Verify(request.Password, user.hash_password))
                {
                    var roles = await _authService.GetUserRolesAsync(request.Email, false);
                    var permissions = await _authService.GetUserPermissionsAsync(request.Email, false);
                    var token = GenerateJwtToken(request.Email, "user", roles, permissions);
                    
                    // Update updated_at
                    user.updated_at = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new DashboardLoginResponseDto
                    {
                        Token = token,
                        RefreshToken = Guid.NewGuid().ToString(), // Implement proper refresh token logic
                        User = new DashboardUserDto
                        {
                            Id = user.user_id.ToString(),
                            Name = user.user_name,
                            Email = user.user_email,
                            Role = string.Join(", ", roles),
                            TimeZone = "UTC",
                            Department = string.Empty,
                            LastLogin = user.updated_at
                        },
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    });
                }

                // Try subuser authentication
                var subuser = await _context.subuser.AsNoTracking().Where(s => s.subuser_email == request.Email).FirstOrDefaultAsync();
                
                if (subuser != null && !string.IsNullOrEmpty(subuser.subuser_password) && 
                    BCrypt.Net.BCrypt.Verify(request.Password, subuser.subuser_password))
                {
                    var roles = await _authService.GetUserRolesAsync(request.Email, true);
                    var permissions = await _authService.GetUserPermissionsAsync(request.Email, true);
                    var token = GenerateJwtToken(request.Email, "subuser", roles, permissions);

                    return Ok(new DashboardLoginResponseDto
                    {
                        Token = token,
                        RefreshToken = Guid.NewGuid().ToString(),
                        User = new DashboardUserDto
                        {
                            Id = subuser.subuser_id.ToString(),
                            Name = subuser.subuser_email,
                            Email = subuser.subuser_email,
                            Role = string.Join(", ", roles),
                            TimeZone = "UTC",
                            Department = string.Empty,
                            LastLogin = DateTime.UtcNow
                        },
                        ExpiresAt = DateTime.UtcNow.AddHours(24)
                    });
                }

                return Unauthorized(new { message = "Invalid email or password" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login error for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        // POST: api/DashboardAuth/refresh-token
        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<ActionResult<DashboardLoginResponseDto>> RefreshToken([FromBody] DashboardLoginRequestDto request)
        {
            // Simplified implementation - in production, validate the refresh token properly
            return await Login(request);
        }

        // POST: api/DashboardAuth/logout
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userEmail = User.Identity?.Name;
            _logger.LogInformation("User {Email} logged out", userEmail);
            
            // Invalidate tokens in production (e.g., blacklist in Redis)
            return Ok(new { message = "Logged out successfully" });
        }

        private string GenerateJwtToken(string email, string userType, IEnumerable<string> roles, IEnumerable<string> permissions)
        {
            var jwtSettings = _configuration.GetSection("Jwt");
            var secretKey = jwtSettings["Key"] ?? throw new InvalidOperationException("JWT key not configured");
            var issuer = jwtSettings["Issuer"];
            var audience = jwtSettings["Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, email),
                new Claim(ClaimTypes.Name, email),
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("user_type", userType),
                new Claim("email", email)
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            foreach (var permission in permissions)
            {
                claims.Add(new Claim("permission", permission));
            }

            var token = new JwtSecurityToken(
                issuer,
                audience,
                claims,
                expires: DateTime.UtcNow.AddHours(24),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    /// <summary>
    /// Admin Dashboard Controller - Overview and Statistics
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AdminDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly ILogger<AdminDashboardController> _logger;
        private readonly ICacheService _cacheService;

        public AdminDashboardController(
            ApplicationDbContext context,
            IRoleBasedAuthService authService,
            ILogger<AdminDashboardController> logger,
            ICacheService cacheService)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
            _cacheService = cacheService;
        }

        // GET: api/AdminDashboard/overview
        [HttpGet("overview")]
        public async Task<ActionResult<DashboardOverviewDto>> GetDashboardOverview()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var isSubuser = await _context.subuser.AsNoTracking().AnyAsync(s => s.subuser_email == userEmail);

                // Check permissions instead of hard-coded roles
                if (!await _authService.HasPermissionAsync(userEmail, "VIEW_ORGANIZATION_HIERARCHY", isSubuser))
                {
                    return StatusCode(403, new { message = "Insufficient permissions to view dashboard" });
                }

                var totalUsers = await _context.Users.AsNoTracking().CountAsync();
                var activeUsers = await _context.Users.AsNoTracking().CountAsync(u => u.updated_at >= DateTime.UtcNow.AddDays(-30));
                var totalMachines = await _context.Machines.AsNoTracking().CountAsync();
                var activeMachines = await _context.Machines.AsNoTracking().CountAsync(m => m.license_activated);
                
                // Get recent activities from logs
                var recentActivities = await _context.logs
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
                    .OrderByDescending(l => l.created_at)
                    .Take(10)
                    .Select(l => new ActivityDto
                    {
                        Id = l.log_id.ToString(),
                        Type = l.log_level ?? "Info",
                        Description = l.log_message ?? "No description",
                        User = l.user_email ?? "System",
                        Timestamp = l.created_at,
                        Status = l.log_level ?? "Info"
                    })
                    .ToListAsync();

                return Ok(new DashboardOverviewDto
                {
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    TotalLicenses = totalMachines,
                    UsedLicenses = activeMachines,
                    TotalMachines = totalMachines,
                    ActiveMachines = activeMachines,
                    RecentActivities = recentActivities
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard overview");
                return StatusCode(500, new { message = "Error retrieving dashboard data", error = ex.Message });
            }
        }

        // GET: api/AdminDashboard/recent-activities
        [HttpGet("recent-activities")]
        public async Task<ActionResult<List<ActivityDto>>> GetRecentActivities()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var activities = await _context.logs
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
                    .OrderByDescending(l => l.created_at)
                    .Take(50)
                    .Select(l => new ActivityDto
                    {
                        Id = l.log_id.ToString(),
                        Type = l.log_level ?? "Info",
                        Description = l.log_message ?? "No description",
                        User = l.user_email ?? "System",
                        Timestamp = l.created_at,
                        Status = l.log_level ?? "Info"
                    })
                    .ToListAsync();

                return Ok(activities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent activities");
                return StatusCode(500, new { message = "Error retrieving activities", error = ex.Message });
            }
        }
    }

    /// <summary>
    /// Dashboard Users Management Controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly ILogger<DashboardUsersController> _logger;
        private readonly ICacheService _cacheService;

        public DashboardUsersController(
            ApplicationDbContext context,
            IRoleBasedAuthService authService,
            ILogger<DashboardUsersController> logger,
            ICacheService cacheService)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
            _cacheService = cacheService;
        }

        // GET: api/DashboardUsers
        [HttpGet]
        public async Task<ActionResult<PagedResultDto<AdminUserDto>>> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var isSubuser = await _context.subuser.AsNoTracking().AnyAsync(s => s.subuser_email == currentUserEmail);

                // Check permission
                if (!await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_USERS", isSubuser))
                {
                    return StatusCode(403, new { message = "Insufficient permissions to view all users" });
                }

                var totalCount = await _context.Users.AsNoTracking().CountAsync();
                var users = await _context.Users
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
                    .OrderByDescending(u => u.created_at)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new AdminUserDto
                    {
                        Id = u.user_id.ToString(),
                        Name = u.user_name,
                        Email = u.user_email,
                        Department = string.Empty,
                        Role = "User",
                        Status = u.private_api == true ? "Active" : "Inactive",
                        LastLogin = u.updated_at,
                        CreatedAt = u.created_at,
                        Groups = new List<string>(),
                        LicenseCount = 0
                    })
                    .ToListAsync();

                return Ok(new PagedResultDto<AdminUserDto>
                {
                    Items = users,
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { message = "Error retrieving users", error = ex.Message });
            }
        }

        // GET: api/DashboardUsers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AdminUserDto>> GetUser(string id)
        {
            try
            {
                if (!int.TryParse(id, out int userId))
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var roles = await _authService.GetUserRolesAsync(user.user_email, false);
                var machineCount = await _context.Machines.AsNoTracking().CountAsync(m => m.user_email == user.user_email);

                return Ok(new AdminUserDto
                {
                    Id = user.user_id.ToString(),
                    Name = user.user_name,
                    Email = user.user_email,
                    Department = string.Empty,
                    Role = string.Join(", ", roles),
                    Status = user.private_api == true ? "Active" : "Inactive",
                    LastLogin = user.updated_at,
                    CreatedAt = user.created_at,
                    Groups = new List<string>(),
                    LicenseCount = machineCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user {UserId}", id);
                return StatusCode(500, new { message = "Error retrieving user" });
            }
        }

        // POST: api/DashboardUsers
        [HttpPost]
        public async Task<ActionResult<AdminUserDto>> CreateUser([FromBody] CreateUserDto request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    return BadRequest(new { message = "Email and password are required" });
                }

                if (await _context.Users.AnyAsync(u => u.user_email == request.Email))
                {
                    return Conflict(new { message = "User with this email already exists" });
                }

                var user = new users
                {
                    user_name = request.Name,
                    user_email = request.Email,
                    user_password = string.Empty,
                    hash_password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    private_api = true,
                    phone_number = request.Phone ?? string.Empty,
                    payment_details_json = "{}",
                    license_details_json = "{}",
                    created_at = DateTime.UtcNow,
                    updated_at = DateTime.UtcNow
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetUser), new { id = user.user_id }, new AdminUserDto
                {
                    Id = user.user_id.ToString(),
                    Name = user.user_name,
                    Email = user.user_email,
                    Department = request.Department,
                    Role = request.Role,
                    Status = "Active",
                    LastLogin = DateTime.UtcNow,
                    CreatedAt = user.created_at,
                    Groups = new List<string>(),
                    LicenseCount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user");
                return StatusCode(500, new { message = "Error creating user" });
            }
        }

        // PUT: api/DashboardUsers/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<AdminUserDto>> UpdateUser(string id, [FromBody] CreateUserDto request)
        {
            try
            {
                if (!int.TryParse(id, out int userId))
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.user_name = request.Name;
                user.user_email = request.Email;
                user.updated_at = DateTime.UtcNow;
                
                if (!string.IsNullOrEmpty(request.Password))
                {
                    user.hash_password = BCrypt.Net.BCrypt.HashPassword(request.Password);
                }

                await _context.SaveChangesAsync();

                return Ok(new AdminUserDto
                {
                    Id = user.user_id.ToString(),
                    Name = user.user_name,
                    Email = user.user_email,
                    Department = request.Department,
                    Role = request.Role,
                    Status = user.private_api == true ? "Active" : "Inactive",
                    LastLogin = user.updated_at,
                    CreatedAt = user.created_at,
                    Groups = new List<string>(),
                    LicenseCount = 0
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user {UserId}", id);
                return StatusCode(500, new { message = "Error updating user" });
            }
        }

        // DELETE: api/DashboardUsers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(string id)
        {
            try
            {
                if (!int.TryParse(id, out int userId))
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                _context.Users.Remove(user);
                await _context.SaveChangesAsync();

                return Ok(new { message = "User deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user {UserId}", id);
                return StatusCode(500, new { message = "Error deleting user" });
            }
        }

        // PUT: api/DashboardUsers/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(string id, [FromBody] dynamic request)
        {
            try
            {
                if (!int.TryParse(id, out int userId))
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Toggle status
                user.private_api = user.private_api != true;
                user.updated_at = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new { message = "User status updated", status = user.private_api == true ? "Active" : "Inactive" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user status {UserId}", id);
                return StatusCode(500, new { message = "Error updating user status" });
            }
        }
    }

    /// <summary>
    /// Dashboard Licenses Management Controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardLicensesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly ILogger<DashboardLicensesController> _logger;
        private readonly ICacheService _cacheService;

        public DashboardLicensesController(
            ApplicationDbContext context,
            IRoleBasedAuthService authService,
            ILogger<DashboardLicensesController> logger,
            ICacheService cacheService)
        {
            _context = context;
            _authService = authService;
            _logger = logger;
            _cacheService = cacheService;
        }

        // GET: api/DashboardLicenses
        [HttpGet]
        public async Task<ActionResult<List<AdminLicenseDto>>> GetLicenses()
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var isSubuser = await _context.subuser.AsNoTracking().AnyAsync(s => s.subuser_email == currentUserEmail);

                // Check permission
                if (!await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_MACHINES", isSubuser))
                {
                    return StatusCode(403, new { message = "Insufficient permissions to view licenses" });
                }

                var licenses = await _context.Machines
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
                    .Where(m => m.license_activated)
                    .OrderByDescending(m => m.license_activation_date)
                    .Take(100)
                    .Select(m => new AdminLicenseDto
                    {
                        Id = m.fingerprint_hash,
                        Type = m.license_days_valid > 0 ? "Paid" : "Demo",
                        AssignedTo = m.user_email ?? "Unassigned",
                        Status = m.license_days_valid > 0 ? "Active" : "Expired",
                        ExpiryDate = (m.license_activation_date != null) ? m.license_activation_date.Value.AddDays(m.license_days_valid) : DateTime.MinValue,
                        CreatedAt = m.created_at,
                        Features = m.license_details_json ?? "{}"
                    })
                    .ToListAsync();

                return Ok(licenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting licenses");
                return StatusCode(500, new { message = "Error retrieving licenses", error = ex.Message });
            }
        }

        // POST: api/DashboardLicenses/bulk-assign
        [HttpPost("bulk-assign")]
        public async Task<IActionResult> BulkAssignLicenses([FromBody] BulkAssignLicensesDto request)
        {
            try
            {
                foreach (var userId in request.UserIds)
                {
                    if (int.TryParse(userId, out int id))
                    {
                        var user = await _context.Users.FindAsync(id);
                        if (user != null)
                        {
                            // Logic to assign licenses to users
                            _logger.LogInformation("Assigned {LicenseType} to user {Email}", request.LicenseType, user.user_email);
                        }
                    }
                }

                return Ok(new { message = $"Licenses assigned to {request.UserIds.Count} users" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk assigning licenses");
                return StatusCode(500, new { message = "Error assigning licenses" });
            }
        }
    }

    /// <summary>
    /// Dashboard Profile Management Controller
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DashboardProfileController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DashboardProfileController> _logger;

        public DashboardProfileController(ApplicationDbContext context, ILogger<DashboardProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/DashboardProfile
        [HttpGet]
        public async Task<ActionResult<DashboardUserDto>> GetProfile()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _context.Users.AsNoTracking().Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
                if (user != null)
                {
                    return Ok(new DashboardUserDto
                    {
                        Id = user.user_id.ToString(),
                        Name = user.user_name,
                        Email = user.user_email,
                        Role = "User",
                        TimeZone = "UTC",
                        Department = string.Empty,
                        LastLogin = user.updated_at
                    });
                }

                var subuser = await _context.subuser.AsNoTracking().Where(s => s.subuser_email == userEmail).FirstOrDefaultAsync();
                if (subuser != null)
                {
                    return Ok(new DashboardUserDto
                    {
                        Id = subuser.subuser_id.ToString(),
                        Name = subuser.subuser_email,
                        Email = subuser.subuser_email,
                        Role = "Subuser",
                        TimeZone = "UTC",
                        Department = string.Empty,
                        LastLogin = DateTime.UtcNow
                    });
                }

                return NotFound(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for {Email}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "Error retrieving profile", error = ex.Message });
            }
        }

        // PUT: api/DashboardProfile
        [HttpPut]
        public async Task<ActionResult<DashboardUserDto>> UpdateProfile([FromBody] DashboardUserDto request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var user = await _context.Users.Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
                if (user != null)
                {
                    user.user_name = request.Name;
                    user.updated_at = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Ok(new DashboardUserDto
                    {
                        Id = user.user_id.ToString(),
                        Name = user.user_name,
                        Email = user.user_email,
                        Role = "User",
                        TimeZone = "UTC",
                        Department = request.Department,
                        LastLogin = user.updated_at
                    });
                }

                return NotFound(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for {Email}", User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
                return StatusCode(500, new { message = "Error updating profile", error = ex.Message });
            }
        }
    }
}
