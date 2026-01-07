using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;  // ✅ ADD: For JsonPropertyName
using BCrypt.Net;
using DSecureApi.Services;
using DSecureApi.Attributes;
using DSecureApi.Models;
using DSecureApi.Helpers;
using DSecureApi.Utilities;  // For Base64EmailEncoder
using DSecureApi.Factories;  // ✅ ADD: For DynamicDbContextFactory

namespace DSecureApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class RoleBasedAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IRoleBasedAuthService _roleService;
        private readonly ILogger<RoleBasedAuthController> _logger;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ICacheService _cacheService;

        public RoleBasedAuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            IRoleBasedAuthService roleService,
            ILogger<RoleBasedAuthController> logger,
            IHttpClientFactory httpClientFactory,
            ICacheService cacheService)
        {
            _context = context;
            _configuration = configuration;
            _roleService = roleService;
            _logger = logger;
            _httpClientFactory = httpClientFactory;
            _cacheService = cacheService;
        }

        public class RoleBasedLoginRequest
        {
            public string Email { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }

        public class RoleBasedLoginResponse
        {
            public string Token { get; set; } = string.Empty;
            public string UserType { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public IEnumerable<string> Roles { get; set; } = new List<string>();
            public IEnumerable<string> Permissions { get; set; } = new List<string>();
            public DateTime ExpiresAt { get; set; }

            // ✅ User info - single normalized fields (no duplicates)
            // PropertyNameCaseInsensitive=true in Program.cs means "Name" and "name" collide!
            public string? Name { get; set; }
            public string? UserRole { get; set; }
            public string? UserGroup { get; set; }
            public string? Department { get; set; }
            public string? Timezone { get; set; }
            public DateTime? LoginTime { get; set; }
            public DateTime? LastLogoutTime { get; set; }
            public string? PhoneNumber { get; set; }
            
            public string? ParentUserEmail { get; set; }
            public int? UserId { get; set; }
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

        public class UpdateUserRolesRequest
        {
            public string Email { get; set; } = string.Empty;
            public bool IsSubuser { get; set; } = false;
            public List<string> RoleNames { get; set; } = new List<string>();
        }

        #region Helper Methods

        /// <summary>
        /// Get server time from TimeController with fallback to DateTimeHelper
        /// </summary>
        private async Task<DateTime> GetServerTimeAsync()
        {
            try
            {
                var client = _httpClientFactory.CreateClient();
                client.BaseAddress = new Uri($"{Request.Scheme}://{Request.Host}");

                var response = await client.GetAsync("/api/Time/server-time");
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var json = System.Text.Json.JsonDocument.Parse(content);
                    var serverTimeStr = json.RootElement.GetProperty("server_time").GetString();
                    return DateTimeHelper.ParseIso8601(serverTimeStr!);  // ✅ Use DateTimeHelper
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get server time, using DateTimeHelper.GetUtcNow()");
            }

            return DateTimeHelper.GetUtcNow();  // ✅ Use DateTimeHelper instead of DateTime.UtcNow
        }

        #endregion

        /// <summary>
        /// Validate token and return user details for session restoration
        /// </summary>
        [HttpPost("validate")]
        [Authorize]
        public async Task<IActionResult> ValidateToken()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userType = User.FindFirst("user_type")?.Value;
                
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "Invalid token" });
                }

                // Get fresh user details and permissions
                bool isSubuser = userType == "subuser";
                var roles = await _roleService.GetUserRolesAsync(userEmail, isSubuser);
                var permissions = await _roleService.GetUserPermissionsAsync(userEmail, isSubuser);

                // Build response similar to Login
                var response = new
                {
                    isValid = true,
                    email = userEmail,
                    userType = userType,
                    roles = roles,
                    permissions = permissions,
                    message = "Token is valid"
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token for current user");
                return Unauthorized(new { message = "Token validation failed" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] RoleBasedLoginRequest request)
        {
            try
            {
                // ✅ LOG: Request received
                _logger.LogInformation("🔐 LOGIN REQUEST RECEIVED - Email: {Email}", request.Email);
                
                if (string.IsNullOrEmpty(request.Email) || string.IsNullOrEmpty(request.Password))
                {
                    _logger.LogWarning("❌ LOGIN FAILED - Missing email or password");
                    return BadRequest(new { message = "Email and password are required" });
                }

                string? userEmail = null;
                bool isSubuser = false;
                users? mainUser = null;
                subuser? subuserData = null;
                bool isPrivateCloudSubuser = false;
                string? parentUserEmail = null;

                // ✅ LOG: Checking main user
                _logger.LogInformation("🔍 STEP 1: Checking Main DB for user {Email}", request.Email);
                
                // Try to authenticate as main user first
                var user = await _context.Users
                    .Where(u => u.user_email == request.Email)
                    .FirstOrDefaultAsync();
                if (user != null && !string.IsNullOrEmpty(user.hash_password) && BCrypt.Net.BCrypt.Verify(request.Password, user.hash_password))
                {
                    userEmail = request.Email;
                    isSubuser = false;
                    mainUser = user;
                    
                    _logger.LogInformation("✅ MAIN USER AUTHENTICATED - Email: {Email}, IsPrivateCloud: {IsPrivateCloud}", 
                        request.Email, user.is_private_cloud);
                }
                else
                {
                    // ✅ LOG: Main user not found, trying subuser
                    _logger.LogInformation("🔍 STEP 2: Main user not found, checking as SUBUSER for {Email}", request.Email);
   
                    // ✅ OPTIMIZED: Check MAIN DB FIRST (fast), then Private Cloud DBs (slow)
                    bool foundSubuser = false;
                    
                    // ✅ STEP 2a: Check MAIN DB for subuser FIRST (fastest path)
                    _logger.LogInformation("🔍 STEP 2a: Checking MAIN DB for subuser {Email}", request.Email);
                    
                    var subuser = await _context.subuser
                        .Where(s => s.subuser_email == request.Email)
                        .FirstOrDefaultAsync();
 
                    if (subuser != null)
                    {
                        _logger.LogInformation("✅ FOUND subuser {Email} in MAIN DB, parent: {Parent}", request.Email, subuser.user_email);
                        
                        // Check if parent has private cloud enabled
                        var parentUser = await _context.Users
                            .Where(u => u.user_email == subuser.user_email).FirstOrDefaultAsync();
                        
                        if (parentUser?.is_private_cloud == true)
                        {
                            // Parent has private cloud enabled - subuser should authenticate against Private Cloud DB
                            _logger.LogInformation("🔄 Parent {Parent} has private cloud enabled, checking Private Cloud DB...", subuser.user_email);
                            
                            try
                            {
                                var tenantService = HttpContext.RequestServices.GetRequiredService<ITenantConnectionService>();
                                var connectionString = await tenantService.GetConnectionStringForUserAsync(subuser.user_email);
                                var mainConnectionString = _configuration.GetConnectionString("ApplicationDbContextConnection");
                                
                                if (connectionString != mainConnectionString && !string.IsNullOrWhiteSpace(connectionString))
                                {
                                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                                    var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
                                    optionsBuilder.UseMySql(connectionString, serverVersion, mysqlOptions =>
                                    {
                                        mysqlOptions.CommandTimeout(15); // ✅ Increased timeout for cold connections
                                        mysqlOptions.EnableRetryOnFailure(3, TimeSpan.FromSeconds(3), null); // ✅ More retries
                                    });
                                    
                                    using var privateContext = new ApplicationDbContext(optionsBuilder.Options);
                                    privateContext.Database.SetCommandTimeout(15); // ✅ Increased timeout
                                    
                                    var privateSubuser = await privateContext.subuser
                                        .Where(s => s.subuser_email == request.Email).FirstOrDefaultAsync();
                                    
                                    if (privateSubuser != null && BCrypt.Net.BCrypt.Verify(request.Password, privateSubuser.subuser_password))
                                    {
                                        userEmail = request.Email;
                                        isSubuser = true;
                                        subuserData = privateSubuser;
                                        isPrivateCloudSubuser = true;
                                        parentUserEmail = subuser.user_email;
                                        foundSubuser = true;
                                        
                                        _logger.LogInformation("✅ PRIVATE CLOUD SUBUSER AUTHENTICATED - Email: {Email}, Parent: {Parent}", 
                                            request.Email, subuser.user_email);
                                    }
                                    else if (privateSubuser == null)
                                    {
                                        _logger.LogWarning("⚠️ Subuser {Email} not found in Private Cloud DB, trying Main DB password", request.Email);
                                    }
                                    else
                                    {
                                        _logger.LogWarning("❌ PASSWORD MISMATCH for subuser {Email} in Private Cloud DB", request.Email);
                                    }
                                }
                            }
                            catch (Exception pcEx)
                            {
                                _logger.LogError(pcEx, "⚠️ Error accessing Private Cloud DB, falling back to Main DB for subuser {Email}", request.Email);
                            }
                        }
                        
                        // If not found in private cloud OR parent doesn't have private cloud, authenticate against Main DB
                        if (!foundSubuser)
                        {
                            if (BCrypt.Net.BCrypt.Verify(request.Password, subuser.subuser_password))
                            {
                                userEmail = request.Email;
                                isSubuser = true;
                                subuserData = subuser;
                                isPrivateCloudSubuser = false;
                                parentUserEmail = subuser.user_email;
                                foundSubuser = true;
  
                                _logger.LogInformation("✅ MAIN DB SUBUSER AUTHENTICATED - Email: {Email}, Parent: {Parent}", 
                                    request.Email, subuser.user_email);
                            }
                            else
                            {
                                _logger.LogWarning("❌ PASSWORD MISMATCH for subuser {Email} in MAIN DB", request.Email);
                            }
                        }
                    }
                    else
                    {
                        // ✅ STEP 2b: Subuser NOT in Main DB - check Private Cloud DBs
                        _logger.LogInformation("🔍 STEP 2b: Subuser not in Main DB, checking Private Cloud databases...");
                        
                        // Get all users with private cloud enabled
                        var privateCloudUsers = await _context.Users
                            .Where(u => u.is_private_cloud == true)
                            .Select(u => new { u.user_email, u.user_id })
                            .ToListAsync();
          
                        if (privateCloudUsers.Any())
                        {
                            _logger.LogInformation("🔍 Found {Count} private cloud users, checking their databases...", privateCloudUsers.Count);
   
                            // ✅ Reduced timeout for faster failure
                            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(20)); // ✅ Increased timeout for searching Private Cloud DBs
   
                            foreach (var pcUser in privateCloudUsers)
                            {
                                try
                                {
                                    if (cts.Token.IsCancellationRequested) break;
    
                                    var tenantService = HttpContext.RequestServices.GetRequiredService<ITenantConnectionService>();
                                    var connectionString = await tenantService.GetConnectionStringForUserAsync(pcUser.user_email);
    
                                    var mainConnectionString = _configuration.GetConnectionString("ApplicationDbContextConnection");
                                    if (connectionString == mainConnectionString) continue;
         
                                    if (string.IsNullOrWhiteSpace(connectionString) || !connectionString.Contains("Server=")) continue;
                                
                                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                                    var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
                                    optionsBuilder.UseMySql(connectionString, serverVersion, mysqlOptions =>
                                    {
                                        mysqlOptions.CommandTimeout(10); // ✅ Increased timeout
                                        mysqlOptions.EnableRetryOnFailure(2, TimeSpan.FromSeconds(2), null); // ✅ More retries
                                    });
  
                                    using var privateContext = new ApplicationDbContext(optionsBuilder.Options);
                                    privateContext.Database.SetCommandTimeout(10); // ✅ Increased timeout
   
                                    var privateSubuser = await privateContext.subuser
                                        .Where(s => s.subuser_email == request.Email).FirstOrDefaultAsync(cts.Token);
      
                                    if (privateSubuser != null && BCrypt.Net.BCrypt.Verify(request.Password, privateSubuser.subuser_password))
                                    {
                                        userEmail = request.Email;
                                        isSubuser = true;
                                        subuserData = privateSubuser;
                                        isPrivateCloudSubuser = true;
                                        parentUserEmail = pcUser.user_email;
                                        foundSubuser = true;
        
                                        _logger.LogInformation("✅ PRIVATE CLOUD SUBUSER AUTHENTICATED - Email: {Email}, Parent: {Parent}", 
                                            request.Email, pcUser.user_email);
                                        break;
                                    }
                                }
                                catch (OperationCanceledException)
                                {
                                    _logger.LogWarning("⚠️ TIMEOUT checking Private Cloud DBs");
                                    break;
                                }
                                catch (Exception ex)
                                {
                                    _logger.LogDebug(ex, "⚠️ Error checking Private Cloud DB for {Email}", pcUser.user_email);
                                    continue;
                                }
                            }
                        }
                    }
                }

                if (userEmail == null)
                {
                    _logger.LogWarning("❌ AUTHENTICATION FAILED - {Email} not found or invalid password in any database", request.Email);
                    return Unauthorized(new { message = "Invalid credentials" });
                }

                // ✅ LOG: Authentication successful
                _logger.LogInformation("✅ AUTHENTICATION SUCCESS - Email: {Email}, Type: {Type}, IsPrivateCloud: {IsPrivateCloud}", 
                    userEmail, isSubuser ? "subuser" : "user", isPrivateCloudSubuser);

                // Get server time for login
                var loginTime = await GetServerTimeAsync();
                
                _logger.LogInformation("🕐 Server time fetched: {Time}", loginTime);

                // Get PREVIOUS last_logout time
                DateTime? previousLastLogout = null;
                if (isSubuser && subuserData != null)
                {
                    previousLastLogout = subuserData.last_logout;
                }
                else if (mainUser != null)
                {
                    previousLastLogout = mainUser.last_logout;
                }

                // Create session entry in MAIN database
                var session = new Sessions
                {
                    user_email = userEmail,
                    login_time = loginTime,
                    ip_address = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    device_info = Request.Headers["User-Agent"].ToString(),
                    session_status = "active"
                };

                _context.Sessions.Add(session);
                _logger.LogInformation("📝 Session created for {Email}", userEmail);

                // Update last_login in appropriate database
                if (isSubuser && subuserData != null)
                {
                    if (isPrivateCloudSubuser && !string.IsNullOrEmpty(parentUserEmail))
                    {
                        // Update in private cloud database
                        _logger.LogInformation("🔄 Updating last_login in Private Cloud DB for subuser {Email}", userEmail);
                        
                        try
                        {
                            var tenantService = HttpContext.RequestServices.GetRequiredService<ITenantConnectionService>();
                            var connectionString = await tenantService.GetConnectionStringForUserAsync(parentUserEmail);
      
                            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
   
                            using var privateContext = new ApplicationDbContext(optionsBuilder.Options);
                            var privateSubuser = await privateContext.subuser
                                .Where(s => s.subuser_email == userEmail).FirstOrDefaultAsync();
   
                            if (privateSubuser != null)
                            {
                                privateSubuser.last_login = loginTime;
                                privateSubuser.last_logout = null;
                                privateSubuser.LastLoginIp = session.ip_address;
                                privateSubuser.activity_status = "online";
                                privateContext.Entry(privateSubuser).State = EntityState.Modified;
                                await privateContext.SaveChangesAsync();
          
                                _logger.LogInformation("✅ Updated last_login in Private Cloud DB for subuser {Email}", userEmail);
                            }
                            else
                            {
                                _logger.LogWarning("⚠️ Subuser {Email} not found in Private Cloud DB during last_login update", userEmail);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "❌ Failed to update last_login in Private Cloud DB for subuser {Email}", userEmail);
                        }
                    }
                    else
                    {
                        // Update in main database
                        _logger.LogInformation("🔄 Updating last_login in MAIN DB for subuser {Email}", userEmail);
                        
                        subuserData.last_login = loginTime;
                        subuserData.last_logout = null;
                        subuserData.LastLoginIp = session.ip_address;
                        subuserData.activity_status = "online";
                        _context.Entry(subuserData).State = EntityState.Modified;
                    }
                }
                else if (mainUser != null)
                {
                    _logger.LogInformation("🔄 Updating last_login in MAIN DB for user {Email}", userEmail);
                    
                    mainUser.last_login = loginTime;
                    mainUser.last_logout = null;
                    mainUser.activity_status = "online";
                    _context.Entry(mainUser).State = EntityState.Modified;
                }

                _logger.LogInformation("💾 Saving changes to MAIN database...");
                await _context.SaveChangesAsync();
                _logger.LogInformation("✅ Changes saved successfully");

                _logger.LogInformation("🔑 Generating JWT token for {Email}...", userEmail);
                var token = await GenerateJwtTokenAsync(userEmail, isSubuser, session.ip_address);
                _logger.LogInformation("✅ JWT token generated");

                // Get roles and permissions
                _logger.LogInformation("👤 Fetching roles and permissions for {Email}...", userEmail);
                var rolesFromRBAC = (await _roleService.GetUserRolesAsync(userEmail, isSubuser, isPrivateCloudSubuser ? parentUserEmail : null)).ToList();
                var permissions = await _roleService.GetUserPermissionsAsync(userEmail, isSubuser, isPrivateCloudSubuser ? parentUserEmail : null);
                
                _logger.LogInformation("✅ Roles fetched: {Roles}", string.Join(", ", rolesFromRBAC));

                // Build complete roles array
                var allRoles = new List<string>(rolesFromRBAC);

                if (isSubuser && subuserData != null)
                {
                    if (!string.IsNullOrEmpty(subuserData.Role) && !allRoles.Contains(subuserData.Role))
                    {
                        allRoles.Add(subuserData.Role);
                    }
                }
                else if (mainUser != null)
                {
                    if (!string.IsNullOrEmpty(mainUser.user_role) && !allRoles.Contains(mainUser.user_role))
                    {
                        allRoles.Add(mainUser.user_role);
                    }
                }

                if (!allRoles.Any())
                {
                    allRoles.Add("SuperAdmin");
                }

                _logger.LogInformation("✅ LOGIN SUCCESSFUL - Email: {Email}, Type: {Type}, Database: {DB}, Roles: {Roles}", 
                    userEmail, 
                    isSubuser ? "subuser" : "user",
                    isPrivateCloudSubuser ? "Private Cloud" : "Main",
                    string.Join(", ", allRoles));

                // Build response
                var response = new RoleBasedLoginResponse
                {
                    Token = token,
                    UserType = isSubuser ? "subuser" : "user",
                    Email = userEmail,
                    Roles = allRoles,
                    Permissions = permissions,
                    ExpiresAt = DateTimeHelper.AddHoursFromNow(8),
                    LoginTime = loginTime,
                    LastLogoutTime = previousLastLogout
                };

                // Add user-specific details
                if (isSubuser && subuserData != null)
                {
                    response.Name = subuserData.Name;
                    response.UserRole = allRoles.FirstOrDefault() ?? "User";
                    response.Department = subuserData.Department;
                    response.PhoneNumber = subuserData.Phone;
                    response.Timezone = subuserData.timezone;
                    response.ParentUserEmail = subuserData.user_email;
                    response.UserId = subuserData.subuser_id;

                    if (subuserData.GroupId.HasValue)
                    {
                        var group = await _context.Set<Group>().FindAsync(subuserData.GroupId.Value);
                        response.UserGroup = group?.name;
                    }
                }
                else if (mainUser != null)
                {
                    response.Name = mainUser.user_name;
                    response.UserRole = allRoles.FirstOrDefault() ?? "User";
                    response.Department = mainUser.department;
                    response.PhoneNumber = mainUser.phone_number;
                    response.Timezone = mainUser.timezone;
                    response.UserId = mainUser.user_id;

                    if (!string.IsNullOrEmpty(mainUser.user_group))
                    {
                        if (int.TryParse(mainUser.user_group, out int groupId))
                        {
                            var group = await _context.Set<Group>().FindAsync(groupId);
                            response.UserGroup = group?.name ?? mainUser.user_group;
                        }
                        else
                        {
                            response.UserGroup = mainUser.user_group;
                        }
                    }
                }

                _logger.LogInformation("📤 Sending login response for {Email}", userEmail);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ CRITICAL ERROR during login for {Email} - Message: {Message}, StackTrace: {StackTrace}", 
                    request.Email, ex.Message, ex.StackTrace);
                return StatusCode(500, new { 
                    message = "An error occurred during login", 
                    error = ex.Message,
                    detail = ex.InnerException?.Message 
                });
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

                // ✅ CHECK: User role cannot create subusers
                if (!await _roleService.CanCreateSubusersAsync(userEmail))
                    return StatusCode(403, new { message = "Users with 'User' role cannot create subusers" });

                // Get the parent user
                var parentUser = await _context.Users
                    .Where(u => u.user_email == userEmail)
                    .FirstOrDefaultAsync();
                if (parentUser == null)
                    return BadRequest(new { message = "Parent user not found" });

                // Check if subuser email already exists
                var existingSubuser = await _context.subuser
                  .Where(s => s.subuser_email == request.SubuserEmail).FirstOrDefaultAsync();
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

                // ✅ Assign roles if provided - with hierarchy validation
                foreach (var roleId in request.RoleIds)
                {
                    var role = await _context.Roles.FindAsync(roleId);
                    if (role != null)
                    {
                        // ✅ Check if parent can assign this role
                        if (await _roleService.CanAssignRoleAsync(userEmail, role.RoleName))
                        {
                            await _roleService.AssignRoleToSubuserAsync(newSubuser.subuser_id, roleId, userEmail);
                        }
                        else
                        {
                            _logger.LogWarning("User {Email} attempted to assign role {Role} which is not allowed", userEmail, role.RoleName);
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

                // ✅ CHECK: Can assigner assign this role?
                if (!await _roleService.CanAssignRoleAsync(assignerEmail, role.RoleName))
                {
                    return StatusCode(403, new
                    {
                        message = $"You cannot assign role '{role.RoleName}'",
                        detail = "You can only assign roles with lower privilege than your own"
                    });
                }

                bool success;
                if (request.SubuserId.HasValue)
                {
                    // Assigning to subuser
                    var subuser = await _context.subuser.FindAsync(request.SubuserId.Value);
                    if (subuser == null)
                        return BadRequest(new { message = "Subuser not found" });

                    // ✅ CHECK: Can assigner manage this subuser?
                    var canManage = await _roleService.CanManageUserAsync(assignerEmail, subuser.subuser_email, true);
                    if (!canManage)
                        return StatusCode(403, new { error = "You cannot manage this subuser" });

                    success = await _roleService.AssignRoleToSubuserAsync(request.SubuserId.Value, request.RoleId, assignerEmail);
                }
                else
                {
                    // Assigning to main user
                    var user = await _context.Users.FindAsync(request.UserId);
                    if (user == null)
                        return BadRequest(new { message = "User not found" });

                    // ✅ CHECK: Can assigner manage this user?
                    var canManage = await _roleService.CanManageUserAsync(assignerEmail, user.user_email, false);
                    if (!canManage)
                        return StatusCode(403, new { error = "You cannot manage this user" });

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
                        return StatusCode(403, new { error = "You cannot manage this subuser" });

                    success = await _roleService.RemoveRoleFromSubuserAsync(request.SubuserId.Value, request.RoleId);
                }
                else
                {
                    var user = await _context.Users.FindAsync(request.UserId);
                    if (user == null)
                        return BadRequest(new { message = "User not found" });

                    var canManage = await _roleService.CanManageUserAsync(assignerEmail, user.user_email, false);
                    if (!canManage)
                        return StatusCode(403, new { error = "You cannot manage this user" });

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
                // ✅ CACHE: Roles rarely change, cache for 30 minutes
                var cacheKey = CacheService.CacheKeys.RoleList;
                var roles = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
                {
                    return await _context.Roles
                        .Select(r => new { r.RoleId, r.RoleName, r.Description, r.HierarchyLevel })
                        .OrderBy(r => r.HierarchyLevel)
                        .ToListAsync();
                }, CacheService.CacheTTL.Long);

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
                // ✅ CACHE: Permissions rarely change, cache for 30 minutes
                var cacheKey = CacheService.CacheKeys.PermissionList;
                var permissions = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
                {
                    return await _context.Permissions
                        .Select(p => new { p.PermissionId, p.PermissionName, p.Description })
                        .ToListAsync();
                }, CacheService.CacheTTL.Long);

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

        private async Task<string> GenerateJwtTokenAsync(string email, bool isSubuser, string ipAddress)
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
                new Claim("ip_address", ipAddress ?? "unknown")
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
         expires: DateTimeHelper.AddHoursFromNow(8),  // ✅ Use DateTimeHelper
 signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        /// <summary>
        /// Simple Logout - Clear JWT token and automatically logout user from system
        /// Updates last_logout timestamp in database
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

       // ✅ Get server time for logout
       var logoutTime = await GetServerTimeAsync();

      // End all active sessions for this user (in MAIN DB)
              var activeSessions = await _context.Sessions
        .Where(s => s.user_email == userEmail && s.session_status == "active")
           .ToListAsync();

     foreach (var session in activeSessions)
                {
    session.logout_time = logoutTime;
        session.session_status = "closed";
           }

      // ✅ Update last_logout and activity_status
      if (isSubuser)
      {
    // ✅ Check MAIN DB first
       var subuser = await _context.subuser
                    .Where(s => s.subuser_email == userEmail)
                    .FirstOrDefaultAsync();
       if (subuser != null)
      {
      // Found in MAIN DB - update here
     subuser.last_logout = logoutTime;
  subuser.activity_status = "offline";
      _context.Entry(subuser).State = EntityState.Modified;
      
          _logger.LogInformation("✅ Updated logout in Main DB for subuser {Email}", userEmail);
      
      // ✅ NEW: Also check if parent has Private Cloud and update there too!
      var parentUser = await _context.Users
                    .Where(u => u.user_email == subuser.user_email)
                    .FirstOrDefaultAsync();
      if (parentUser != null && parentUser.is_private_cloud == true)
      {
          try
          {
              _logger.LogInformation("🔍 Subuser's parent {Parent} has Private Cloud=true, updating logout in Private Cloud DB too...", subuser.user_email);
              
              var tenantService = HttpContext.RequestServices.GetRequiredService<ITenantConnectionService>();
              var connectionString = await tenantService.GetConnectionStringForUserAsync(subuser.user_email);
              
              var mainConnectionString = _configuration.GetConnectionString("ApplicationDbContextConnection");
              if (connectionString != mainConnectionString && !string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("Server="))
              {
                  var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                  var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
                  optionsBuilder.UseMySql(connectionString, serverVersion, mysqlOptions =>
                  {
                      mysqlOptions.CommandTimeout(5);
                      mysqlOptions.EnableRetryOnFailure(1, TimeSpan.FromSeconds(2), null);
                  });
                  
                  using var privateContext = new ApplicationDbContext(optionsBuilder.Options);
                  privateContext.Database.SetCommandTimeout(5);
                  
                  var privateSubuser = await privateContext.subuser.Where(s => s.subuser_email == userEmail).FirstOrDefaultAsync();
                  if (privateSubuser != null)
                  {
                      privateSubuser.last_logout = logoutTime;
                      privateSubuser.activity_status = "offline";
                      privateContext.Entry(privateSubuser).State = EntityState.Modified;
                      
                      var saveResult = await privateContext.SaveChangesAsync();
                      _logger.LogInformation("✅ ALSO updated logout in Private Cloud DB for subuser {Email} (parent: {Parent}). Rows affected: {Rows}", 
                          userEmail, subuser.user_email, saveResult);
                  }
                  else
                  {
                      _logger.LogDebug("Subuser {Email} not found in Private Cloud DB (parent: {Parent})", userEmail, subuser.user_email);
                  }
              }
          }
          catch (Exception ex)
          {
              _logger.LogWarning(ex, "⚠️ Failed to update logout in Private Cloud DB for subuser {Email}", userEmail);
          }
      }
      }
        else
         {
            // ✅ NOT IN MAIN DB - Check Private Cloud databases
          _logger.LogInformation("🔍 Subuser {Email} not in Main DB, checking Private Cloud...", userEmail);
    
          var privateCloudUsers = await _context.Users
          .Where(u => u.is_private_cloud == true)
          .Select(u => new { u.user_email, u.user_id })
      .ToListAsync();
      
          bool logoutUpdatedInPrivateCloud = false;
      
 foreach (var pcUser in privateCloudUsers)
        {
  try
 {
  var tenantService = HttpContext.RequestServices.GetRequiredService<ITenantConnectionService>();
var connectionString = await tenantService.GetConnectionStringForUserAsync(pcUser.user_email);
        
       var mainConnectionString = _configuration.GetConnectionString("ApplicationDbContextConnection");
        if (connectionString == mainConnectionString)
                 continue;
   
                // ✅ IMPROVED: Validate connection string
                if (string.IsNullOrWhiteSpace(connectionString) || !connectionString.Contains("Server="))
                {
                    _logger.LogWarning("⚠️ Invalid connection string for user {Email}, skipping logout update", pcUser.user_email);
                    continue;
                }
                
  var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                
                // ✅ IMPROVED: Use fixed MySQL version to avoid slow AutoDetect
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
                optionsBuilder.UseMySql(connectionString, serverVersion, mysqlOptions =>
                {
                    mysqlOptions.CommandTimeout(5); // 5 seconds max per query
                    mysqlOptions.EnableRetryOnFailure(1, TimeSpan.FromSeconds(2), null);
                });
   
  using var privateContext = new ApplicationDbContext(optionsBuilder.Options);
                
                // ✅ IMPROVED: Reduced command timeout
                privateContext.Database.SetCommandTimeout(5);
          
   var privateSubuser = await privateContext.subuser
            .Where(s => s.subuser_email == userEmail).FirstOrDefaultAsync();
            
     if (privateSubuser != null)
      {
                // ✅ FOUND! Update logout in private DB
 privateSubuser.last_logout = logoutTime;
    privateSubuser.activity_status = "offline";
             privateContext.Entry(privateSubuser).State = EntityState.Modified;
                
                // ✅ CRITICAL FIX: Ensure SaveChangesAsync completes successfully
                var saveResult = await privateContext.SaveChangesAsync();
                
                if (saveResult > 0)
                {
                    logoutUpdatedInPrivateCloud = true;
                    _logger.LogInformation("✅ Updated logout in Private Cloud DB for subuser {Email} (parent: {Parent}). Rows affected: {Rows}", 
                        userEmail, pcUser.user_email, saveResult);
                    break; // Success - exit loop
                }
                else
                {
                    _logger.LogWarning("⚠️ SaveChanges returned 0 rows for subuser {Email} in Private Cloud DB (parent: {Parent})", 
                        userEmail, pcUser.user_email);
                }
        }
        else
        {
            _logger.LogDebug("Subuser {Email} not found in Private Cloud DB of parent {Parent}", 
                userEmail, pcUser.user_email);
        }
     }
     catch (MySql.Data.MySqlClient.MySqlException mysqlEx)
     {
         _logger.LogWarning("⚠️ MySQL error updating logout in Private Cloud DB for user {Email} - Code: {Code}, Message: {Message}", 
             pcUser.user_email, mysqlEx.Number, mysqlEx.Message);
         // Continue to next private cloud user
     }
                 catch (Exception ex)
      {
      _logger.LogWarning(ex, "⚠️ Failed to update logout in Private Cloud DB for user {Email}", pcUser.user_email);
       }
}

            // ✅ Check if logout was updated in any private cloud database
            if (!logoutUpdatedInPrivateCloud)
            {
                _logger.LogWarning("⚠️ Failed to update logout for subuser {Email} in any Private Cloud database", userEmail);
            }
         }
     }
 	else
         {
     // Regular user logout
 var user = await _context.Users
                    .Where(u => u.user_email == userEmail)
                    .FirstOrDefaultAsync();
      if (user != null)
      {
      user.last_logout = logoutTime;
   user.activity_status = "offline";
      _context.Entry(user).State = EntityState.Modified;
              
        _logger.LogInformation("✅ Updated logout in Main DB for user {Email}, is_private_cloud={IsPrivate}", userEmail, user.is_private_cloud);
      
      // ✅ ALSO update Private Cloud DB if user has private cloud enabled
      if (user.is_private_cloud == true)
      {
          try
          {
              _logger.LogInformation("🔍 User {Email} has Private Cloud=true, updating logout there too...", userEmail);
              
              var tenantService = HttpContext.RequestServices.GetRequiredService<ITenantConnectionService>();
              var connectionString = await tenantService.GetConnectionStringForUserAsync(userEmail);
              
              var mainConnectionString = _configuration.GetConnectionString("ApplicationDbContextConnection");
              
              _logger.LogDebug("🔍 Connection strings - Main: {MainCS}, Private: {PrivateCS}", 
                  mainConnectionString?.Substring(0, Math.Min(50, mainConnectionString?.Length ?? 0)) + "...",
                  connectionString?.Substring(0, Math.Min(50, connectionString?.Length ?? 0)) + "...");
              
              if (connectionString != mainConnectionString && !string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("Server="))
              {
                  _logger.LogInformation("🔍 Creating Private Cloud DB connection for logout...");
                  
                  var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                  var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
                  optionsBuilder.UseMySql(connectionString, serverVersion, mysqlOptions =>
                  {
                      mysqlOptions.CommandTimeout(5);
                      mysqlOptions.EnableRetryOnFailure(1, TimeSpan.FromSeconds(2), null);
                  });
                  
                  using var privateContext = new ApplicationDbContext(optionsBuilder.Options);
                  privateContext.Database.SetCommandTimeout(5);
                  
                  var privateUser = await privateContext.Users.Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
                  
                  _logger.LogInformation("🔍 Private Cloud user lookup result: {Found}", privateUser != null);
                  
                  if (privateUser != null)
                  {
                      privateUser.last_logout = logoutTime;
                      privateUser.activity_status = "offline";
                      privateContext.Entry(privateUser).State = EntityState.Modified;
                      
                      var saveResult = await privateContext.SaveChangesAsync();
                      _logger.LogInformation("✅ Updated logout in Private Cloud DB for user {Email}. Rows affected: {Rows}", userEmail, saveResult);
                  }
                  else
                  {
                      _logger.LogWarning("⚠️ User {Email} NOT found in Private Cloud DB!", userEmail);
                  }
              }
              else
              {
                  _logger.LogWarning("⚠️ Private Cloud connection string same as Main or invalid for user {Email}", userEmail);
              }
          }
          catch (Exception ex)
          {
              _logger.LogWarning(ex, "⚠️ Failed to update logout in Private Cloud DB for user {Email}", userEmail);
          }
      }
      else
      {
          _logger.LogDebug("User {Email} is_private_cloud={IsPrivate}, skipping Private Cloud update", userEmail, user.is_private_cloud);
      }
      }
    }

      await _context.SaveChangesAsync();

        _logger.LogInformation("User logout: {Email} ({UserType}) at {LogoutTime}",
    userEmail, isSubuser ? "subuser" : "user", DateTimeHelper.ToIso8601String(logoutTime));

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
          lastLogoutTime = logoutTime,  // ✅ Include for consistency
             activity_status = "offline",
            sessionsEnded = activeSessions.Count,
      clearToken = true,
       swaggerLogout = true
    });
    }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "Logout failed", error = ex.Message });
            }
        }
        
        /// <summary>
        /// Update user roles - Replace existing roles with new ones
        /// Supports both main users and subusers
        /// </summary>
        [HttpPatch("update-roles")]
        [RequirePermission("UserManagement")]
        public async Task<IActionResult> UpdateUserRoles([FromBody] UpdateUserRolesRequest request)
        {
            try
            {
                var assignerEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(assignerEmail))
                    return Unauthorized(new { message = "Authentication required" });

                // Validate email
                if (string.IsNullOrEmpty(request.Email))
                    return BadRequest(new { message = "Email is required" });

                // Validate at least one role is provided
                if (request.RoleNames == null || !request.RoleNames.Any())
                    return BadRequest(new { message = "At least one role must be specified" });

                // ✅ Get server time for role assignment
                var assignmentTime = await GetServerTimeAsync();

                // Get assigner hierarchy level
                var assignerLevel = await _roleService.GetUserHierarchyLevelAsync(assignerEmail, false);

                int? targetUserId = null;
                int? targetSubuserId = null;

                // Find target user/subuser and get current hierarchy level
                if (request.IsSubuser)
                {
                    var subuser = await _context.subuser
                   .Where(s => s.subuser_email == request.Email).FirstOrDefaultAsync();

                    if (subuser == null)
                        return NotFound(new { message = "Subuser not found" });

                    // Check if assigner can manage this subuser
                    var canManage = await _roleService.CanManageUserAsync(assignerEmail, subuser.subuser_email, true);
                    if (!canManage)
                        return StatusCode(403, new { message = "You cannot manage this subuser" });

                    targetSubuserId = subuser.subuser_id;
                }
                else
                {
                    var user = await _context.Users
               .Where(u => u.user_email == request.Email).FirstOrDefaultAsync();

                    if (user == null)
                        return NotFound(new { message = "User not found" });

                    // Check if assigner can manage this user
                    var canManage = await _roleService.CanManageUserAsync(assignerEmail, user.user_email, false);
                    if (!canManage)
                        return StatusCode(403, new { message = "You cannot manage this user" });

                    targetUserId = user.user_id;
                }

                // Validate and get role IDs from role names
                var rolesToAssign = new List<Role>();
                foreach (var roleName in request.RoleNames)
                {
                    var role = await _context.Roles
        .Where(r => r.RoleName == roleName).FirstOrDefaultAsync();

                    if (role == null)
                        return BadRequest(new { message = $"Role '{roleName}' not found" });

                    // Check if assigner can assign this role (must have higher privilege)
                    if (assignerLevel >= role.HierarchyLevel)
                        return StatusCode(403, new { message = $"You cannot assign role '{roleName}' - insufficient privileges" });

                    rolesToAssign.Add(role);
                }

                // Remove all existing roles for the user
                if (request.IsSubuser && targetSubuserId.HasValue)
                {
                    var existingRoles = await _context.SubuserRoles
                      .Where(sr => sr.SubuserId == targetSubuserId.Value)
                         .ToListAsync();

                    _context.SubuserRoles.RemoveRange(existingRoles);
                }
                else if (targetUserId.HasValue)
                {
                    var existingRoles = await _context.UserRoles
                    .Where(ur => ur.UserId == targetUserId.Value)
                          .ToListAsync();

                    _context.UserRoles.RemoveRange(existingRoles);
                }

                // Assign new roles with server time
                foreach (var role in rolesToAssign)
                {
                    if (request.IsSubuser && targetSubuserId.HasValue)
                    {
                        var subuserRole = new SubuserRole
                        {
                            SubuserId = targetSubuserId.Value,
                            RoleId = role.RoleId,
                            AssignedByEmail = assignerEmail,
                            AssignedAt = assignmentTime  // ✅ ISO 8601 via converter
                        };
                        _context.SubuserRoles.Add(subuserRole);
                    }
                    else if (targetUserId.HasValue)
                    {
                        var userRole = new UserRole
                        {
                            UserId = targetUserId.Value,
                            RoleId = role.RoleId,
                            AssignedByEmail = assignerEmail,
                            AssignedAt = assignmentTime// ✅ ISO 8601 via converter
                        };
                        _context.UserRoles.Add(userRole);
                    }
                }

                await _context.SaveChangesAsync();

                // Get updated roles and permissions
                var updatedRoles = await _roleService.GetUserRolesAsync(request.Email, request.IsSubuser);
                var updatedPermissions = await _roleService.GetUserPermissionsAsync(request.Email, request.IsSubuser);

                _logger.LogInformation("Roles updated for {UserType} {Email} by {Assigner}. New roles: {Roles}",
                    request.IsSubuser ? "subuser" : "user",
                  request.Email,
                   assignerEmail,
                string.Join(", ", updatedRoles));

                return Ok(new
                {
                    success = true,
                    message = "Roles updated successfully",
                    email = request.Email,
                    userType = request.IsSubuser ? "subuser" : "user",
                    roles = updatedRoles,
                    permissions = updatedPermissions,
                    updatedBy = assignerEmail,
                    updatedAt = assignmentTime  // ✅ ISO 8601 via converter
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating roles for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred while updating roles", error = ex.Message });
            }
        }

        /// <summary>
        /// Edit Profile - Update name, phone, timezone for both Users and Subusers
        /// Self-service: User can update their own profile
        /// Supports both Main DB and Private Cloud databases
        /// PATCH method for partial updates
        /// </summary>
        [HttpPatch("edit-profile")]
        [Authorize]
        public async Task<IActionResult> EditProfile([FromBody] EditProfileRequest request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { message = "Authentication required" });

                // ✅ Get server time for update timestamp
                var updateTime = await GetServerTimeAsync();

                // Track which fields were updated
                var updatedFields = new List<string>();

                // ✅ NEW: Use TenantConnectionService to get the CORRECT database context
                // This respects Private Cloud priority when enabled
                var tenantService = HttpContext.RequestServices.GetRequiredService<ITenantConnectionService>();
                var connectionString = await tenantService.GetConnectionStringForUserAsync(currentUserEmail);
                var mainConnectionString = _configuration.GetConnectionString("ApplicationDbContextConnection");
                
                ApplicationDbContext? targetContext = null;
                bool usingPrivateCloud = false;
                string dbSource = "Main DB";
                
                if (connectionString != mainConnectionString && !string.IsNullOrWhiteSpace(connectionString) && connectionString.Contains("Server="))
                {
                    // ✅ Private Cloud connection returned - use Private Cloud DB
                    _logger.LogInformation("🔌 EditProfile: Using Private Cloud DB for user {Email}", currentUserEmail);
                    
                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
                    optionsBuilder.UseMySql(connectionString, serverVersion, mysqlOptions =>
                    {
                        mysqlOptions.CommandTimeout(10);
                        mysqlOptions.EnableRetryOnFailure(1, TimeSpan.FromSeconds(2), null);
                    });
                    
                    targetContext = new ApplicationDbContext(optionsBuilder.Options);
                    targetContext.Database.SetCommandTimeout(10);
                    usingPrivateCloud = true;
                    dbSource = "Private Cloud DB";
                }
                else
                {
                    // ✅ Use Main DB
                    _logger.LogInformation("📊 EditProfile: Using Main DB for user {Email}", currentUserEmail);
                    targetContext = _context;
                }

                // ✅ STEP 1: Try to find as SUBUSER in the correct database
                var subuser = await targetContext.subuser.Where(s => s.subuser_email == currentUserEmail).FirstOrDefaultAsync();
                
                if (subuser != null)
                {
                    // ✅ SUBUSER FOUND - Update in the correct database
                    _logger.LogInformation("📝 Updating profile for SUBUSER: {Email} in {DbSource}", currentUserEmail, dbSource);

                    // Update name if provided
                    if (!string.IsNullOrWhiteSpace(request.Name))
                    {
                        subuser.Name = request.Name.Trim();
                        updatedFields.Add("Name");
                    }

                    // Update phone if provided
                    if (request.Phone != null)
                    {
                        subuser.Phone = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
                        updatedFields.Add("Phone");
                    }

                    // Update timezone if provided
                    if (!string.IsNullOrWhiteSpace(request.Timezone))
                    {
                        subuser.timezone = request.Timezone.Trim();
                        updatedFields.Add("Timezone");
                    }

                    // Check if any fields were updated
                    if (updatedFields.Count == 0)
                    {
                        return BadRequest(new 
                        { 
                            message = "No fields to update. Provide at least one field: Name, Phone, or Timezone" 
                        });
                    }

                    subuser.UpdatedAt = updateTime;
                    
                    // ✅ Use targetContext - already points to correct DB (Private Cloud or Main)
                    targetContext.Entry(subuser).State = EntityState.Modified;
                    await targetContext.SaveChangesAsync();
                    
                    _logger.LogInformation("✅ Profile updated for SUBUSER {Email} in {DbSource}. Fields: {Fields}", 
                        currentUserEmail, dbSource, string.Join(", ", updatedFields));
                    
                    // Dispose private context if we created one
                    if (usingPrivateCloud && targetContext != _context)
                    {
                        await targetContext.DisposeAsync();
                    }
                    return Ok(new
                    {
                        success = true,
                        message = "Profile updated successfully",
                        userType = "subuser",
                        email = currentUserEmail,
                        name = subuser.Name,
                        phone = subuser.Phone,
                        timezone = subuser.timezone,
                        updatedFields = updatedFields,
                        updatedAt = updateTime,
                        databaseSource = dbSource
                    });
                }

                // ✅ STEP 2: NOT FOUND AS SUBUSER - Try to find as USER in the same context
                var user = await targetContext.Users.Where(u => u.user_email == currentUserEmail).FirstOrDefaultAsync();
                
                if (user != null)
                {
                    // ✅ USER FOUND - Update in the correct database
                    _logger.LogInformation("📝 Updating profile for USER: {Email} in {DbSource}", currentUserEmail, dbSource);

                    // Update name if provided
                    if (!string.IsNullOrWhiteSpace(request.Name))
                    {
                        user.user_name = request.Name.Trim();
                        updatedFields.Add("Name");
                    }

                    // Update phone if provided
                    if (request.Phone != null)
                    {
                        user.phone_number = string.IsNullOrWhiteSpace(request.Phone) ? null : request.Phone.Trim();
                        updatedFields.Add("Phone");
                    }

                    // Update timezone if provided
                    if (!string.IsNullOrWhiteSpace(request.Timezone))
                    {
                        user.timezone = request.Timezone.Trim();
                        updatedFields.Add("Timezone");
                    }

                    // Check if any fields were updated
                    if (updatedFields.Count == 0)
                    {
                        return BadRequest(new 
                        { 
                            message = "No fields to update. Provide at least one field: Name, Phone, or Timezone" 
                        });
                    }

                    user.updated_at = updateTime;
                    
                    // ✅ Use targetContext - already points to correct DB (Private Cloud or Main)
                    targetContext.Entry(user).State = EntityState.Modified;
                    await targetContext.SaveChangesAsync();

                    _logger.LogInformation("✅ Profile updated for USER {Email} in {DbSource}. Fields: {Fields}", 
                        currentUserEmail, dbSource, string.Join(", ", updatedFields));
                    
                    // Dispose private context if we created one
                    if (usingPrivateCloud && targetContext != _context)
                    {
                        await targetContext.DisposeAsync();
                    }

                    return Ok(new
                    {
                        success = true,
                        message = "Profile updated successfully",
                        userType = "user",
                        email = currentUserEmail,
                        name = user.user_name,
                        phone = user.phone_number,
                        timezone = user.timezone,
                        updatedFields = updatedFields,
                        updatedAt = updateTime,
                        databaseSource = dbSource
                    });
                }

                // ✅ NOT FOUND IN MAIN DB - This shouldn't happen if user is authenticated
                _logger.LogWarning("⚠️ User/Subuser not found for authenticated email: {Email}", currentUserEmail);
                return NotFound(new 
                { 
                    message = "User or subuser profile not found",
                    email = currentUserEmail
                });
            }
            catch (Exception ex)
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
                _logger.LogError(ex, "❌ Error updating profile for {Email}", userEmail);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error updating profile",
                    error = ex.Message
                });
            }
        }

        [HttpPatch("update-timezone")]
        [Authorize]
        public async Task<IActionResult> UpdateTimezone([FromBody] UpdateTimezoneRequest request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { message = "Authentication required" });

                // If no email provided, update current user's timezone
                var targetEmail = string.IsNullOrEmpty(request.Email) ? currentUserEmail : request.Email;
                var isCurrentUser = targetEmail == currentUserEmail;

                // Validate timezone format (basic validation)
                if (string.IsNullOrWhiteSpace(request.Timezone))
                    return BadRequest(new { message = "Timezone is required" });

                // If updating someone else's timezone, check permissions
                if (!isCurrentUser)
                {
                    var currentIsSubuser = await _context.subuser.AnyAsync(s => s.subuser_email == currentUserEmail);
                    var hasPermission = await _roleService.HasPermissionAsync(currentUserEmail, "UPDATE_USER", currentIsSubuser);

                    if (!hasPermission)
                        return StatusCode(403, new { message = "You don't have permission to update other users' timezones" });
                }

                // ✅ Get server time for update timestamp
                var updateTime = await GetServerTimeAsync();

                // Check if target is subuser or user
                var subuser = await _context.subuser
                    .Where(s => s.subuser_email == targetEmail)
                    .FirstOrDefaultAsync();
                if (subuser != null)
                {
                    subuser.timezone = request.Timezone;
                    subuser.UpdatedAt = updateTime;  // ✅ ISO 8601 via converter
                    _context.Entry(subuser).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Timezone updated for subuser {Email} to {Timezone}", targetEmail, request.Timezone);

                    return Ok(new
                    {
                        success = true,
                        message = "Timezone updated successfully",
                        email = targetEmail,
                        userType = "subuser",
                        timezone = request.Timezone,
                        updatedBy = currentUserEmail,
                        updatedAt = updateTime  // ✅ ISO 8601 via converter
                    });
                }

                var user = await _context.Users
                    .Where(u => u.user_email == targetEmail)
                    .FirstOrDefaultAsync();
                if (user != null)
                {
                    user.timezone = request.Timezone;
                    user.updated_at = updateTime;  // ✅ ISO 8601 via converter
                    _context.Entry(user).State = EntityState.Modified;
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Timezone updated for user {Email} to {Timezone}", targetEmail, request.Timezone);

                    return Ok(new
                    {
                        success = true,
                        message = "Timezone updated successfully",
                        email = targetEmail,
                        userType = "user",
                        timezone = request.Timezone,
                        updatedBy = currentUserEmail,
                        updatedAt = updateTime  // ✅ ISO 8601 via converter
                    });
                }

                return NotFound(new { message = "User or subuser not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating timezone for {Email}", request.Email);
                return StatusCode(500, new { message = "An error occurred while updating timezone", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all permissions for a specific role
        /// Anyone can view role permissions
        /// </summary>
        [HttpGet("roles/{roleName}/permissions")]
        [Authorize]
        public async Task<IActionResult> GetRolePermissions(string roleName)
        {
            try
            {
                var permissions = await _roleService.GetRolePermissionsAsync(roleName);

                if (!permissions.Any())
                {
                    return NotFound(new
                    {
                        message = $"Role '{roleName}' not found or has no permissions"
                    });
                }

                return Ok(new
                {
                    roleName = roleName,
                    permissions = permissions,
                    count = permissions.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting permissions for role {Role}", roleName);
                return StatusCode(500, new { message = "Error retrieving role permissions" });
            }
        }

        /// <summary>
        /// Get all available permissions in the system
        /// </summary>
        [HttpGet("permissions/all")]
        [Authorize]
        public async Task<IActionResult> GetAllPermissions()
        {
            try
            {
                var permissions = await _roleService.GetAllPermissionsAsync();

                return Ok(new
                {
                    permissions = permissions.Select(p => new
                    {
                        permissionId = p.PermissionId,
                        permissionName = p.PermissionName,
                        description = p.Description
                    }),
                    count = permissions.Count()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all permissions");
                return StatusCode(500, new { message = "Error retrieving permissions" });
            }
        }

        /// <summary>
        /// Add permission to a role (SuperAdmin/Admin only)
        /// </summary>
        [HttpPost("roles/{roleName}/permissions")]
        [Authorize]
        public async Task<IActionResult> AddPermissionToRole(string roleName, [FromBody] AddPermissionRequest request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized();

                // Validate request
                if (string.IsNullOrEmpty(request.PermissionName))
                    return BadRequest(new { message = "Permission name is required" });

                // ✅ Get server time for modification timestamp
                var modificationTime = await GetServerTimeAsync();

                // Check if user can modify this role's permissions
                if (!await _roleService.CanModifyRolePermissionsAsync(currentUserEmail, roleName))
                {
                    return StatusCode(403, new
                    {
                        message = $"You cannot modify permissions for role '{roleName}'",
                        detail = "Only SuperAdmin and Admin can modify role permissions, and only for roles below their level"
                    });
                }

                var success = await _roleService.AddPermissionToRoleAsync(roleName, request.PermissionName, currentUserEmail);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Permission '{request.PermissionName}' added to role '{roleName}'",
                        roleName = roleName,
                        permissionName = request.PermissionName,
                        modifiedBy = currentUserEmail,
                        modifiedAt = modificationTime  // ✅ ISO 8601 via converter
                    });
                }
                else
                {
                    return BadRequest(new { message = "Failed to add permission to role" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding permission to role");
                return StatusCode(500, new { message = "Error adding permission to role" });
            }
        }

        /// <summary>
        /// Remove permission from a role (SuperAdmin/Admin only)
        /// </summary>
        [HttpDelete("roles/{roleName}/permissions/{permissionName}")]
        [Authorize]
        public async Task<IActionResult> RemovePermissionFromRole(string roleName, string permissionName)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized();

                // ✅ Get server time for modification timestamp (CONSISTENT FORMAT)
                var modificationTime = await GetServerTimeAsync();

                // Check if user can modify this role's permissions
                if (!await _roleService.CanModifyRolePermissionsAsync(currentUserEmail, roleName))
                {
                    return StatusCode(403, new
                    {
                        message = $"You cannot modify permissions for role '{roleName}'",
                        detail = "Only SuperAdmin and Admin can modify role permissions, and only for roles below their level"
                    });
                }

                var success = await _roleService.RemovePermissionFromRoleAsync(roleName, permissionName, currentUserEmail);

                if (success)
                {
                    return Ok(new
                    {
                        success = true,
                        message = $"Permission '{permissionName}' removed from role '{roleName}'",
                        roleName = roleName,
                        permissionName = permissionName,
                        modifiedBy = currentUserEmail,
                        modifiedAt = modificationTime  // ✅ ISO 8601 via converter
                    });
                }
                else
                {
                    return BadRequest(new { message = "Failed to remove permission from role" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing permission from role");
                return StatusCode(500, new { message = "Error removing permission from role" });
            }
        }

        /// <summary>
        /// Update all permissions for a role (replace existing)
        /// SuperAdmin/Admin only
        /// </summary>
        [HttpPut("roles/{roleName}/permissions")]
        [Authorize]
        public async Task<IActionResult> UpdateRolePermissions(string roleName, [FromBody] UpdateRolePermissionsRequest request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized();

                // Validate request
                if (request.PermissionNames == null || !request.PermissionNames.Any())
                {
                    return BadRequest(new { message = "At least one permission must be specified" });
                }

                // ✅ Get server time for modification timestamp
                var modificationTime = await GetServerTimeAsync();

                // Check if user can modify this role's permissions
                if (!await _roleService.CanModifyRolePermissionsAsync(currentUserEmail, roleName))
                {
                    return StatusCode(403, new
                    {
                        message = $"You cannot modify permissions for role '{roleName}'",
                        detail = "Only SuperAdmin and Admin can modify role permissions, and only for roles below their level"
                    });
                }

                var success = await _roleService.UpdateRolePermissionsAsync(roleName, request.PermissionNames, currentUserEmail);

                if (success)
                {
                    var updatedPermissions = await _roleService.GetRolePermissionsAsync(roleName);

                    return Ok(new
                    {
                        success = true,
                        message = $"Permissions updated for role '{roleName}'",
                        roleName = roleName,
                        permissions = updatedPermissions,
                        modifiedBy = currentUserEmail,
                        modifiedAt = modificationTime  // ✅ ISO 8601 via converter
                    });
                }
                else
                {
                    return BadRequest(new { message = "Failed to update role permissions" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating role permissions");
                return StatusCode(500, new { message = "Error updating role permissions" });
            }
        }

        /// <summary>
        /// Unified Password Change - Works for both Users and Subusers
        /// Automatically detects user type and updates password
        /// Requires current password verification for security
        /// Self-service only - User can only change their own password
        /// </summary>
        [HttpPatch("change-password")]
        [Authorize]
        public async Task<IActionResult> UnifiedChangePassword([FromBody] SelfServicePasswordChangeRequest request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { message = "Authentication required" });

                // Validate request
                if (string.IsNullOrEmpty(request.CurrentPassword))
                    return BadRequest(new { message = "Current password is required" });

                if (string.IsNullOrEmpty(request.NewPassword))
                    return BadRequest(new { message = "New password is required" });

                if (request.NewPassword.Length < 8)
                    return BadRequest(new { message = "New password must be at least 8 characters" });

                // ✅ Get server time for update timestamp
                var updateTime = await GetServerTimeAsync();

                // Try to find as subuser first
                var subuser = await _context.subuser
                    .Where(s => s.subuser_email == currentUserEmail)
                    .FirstOrDefaultAsync();
                if (subuser != null)
                {
                    // Verify current password
                    if (string.IsNullOrEmpty(subuser.subuser_password))
                        return BadRequest(new { message = "Subuser password not set" });

                    bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
                   request.CurrentPassword,
                       subuser.subuser_password
                     );

                    if (!isCurrentPasswordValid)
                    {
                        _logger.LogWarning(
                            "Failed password change attempt for subuser {Email} - incorrect current password",
                           currentUserEmail
                          );
                        return BadRequest(new { message = "Current password is incorrect" });
                    }

                    // Check if new password is same as current password
                    bool isSamePassword = BCrypt.Net.BCrypt.Verify(
                  request.NewPassword,
                      subuser.subuser_password
                    );

                    if (isSamePassword)
                        return BadRequest(new { message = "New password must be different from current password" });

                    // Update password
                    string newHashedPassword = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                    subuser.subuser_password = newHashedPassword;
                    subuser.UpdatedAt = updateTime;  // ✅ ISO 8601 via converter

                    // Mark entity as modified
                    _context.Entry(subuser).State = EntityState.Modified;
                    _context.Entry(subuser).Property(s => s.subuser_password).IsModified = true;

                    // Save changes
                    int rowsAffected = await _context.SaveChangesAsync();

                    if (rowsAffected == 0)
                    {
                        _logger.LogError(
                               "SaveChanges returned 0 rows affected for subuser {Email}",
                       currentUserEmail
                           );
                        return StatusCode(500, new
                        {
                            message = "Failed to save password changes to database",
                            error = "No rows were modified"
                        });
                    }

                    _logger.LogInformation(
                       "Password changed successfully for subuser {Email}. Rows affected: {RowsAffected}",
                          currentUserEmail,
                   rowsAffected
                      );

                    return Ok(new
                    {
                        success = true,
                        message = "Password changed successfully",
                        email = currentUserEmail,
                        userType = "subuser",
                        changedAt = updateTime,  // ✅ ISO 8601 via converter
                        rowsAffected = rowsAffected
                    });
                }

                // Try to find as main user
                var user = await _context.Users
                    .Where(u => u.user_email == currentUserEmail)
                    .FirstOrDefaultAsync();
                if (user != null)
                {
                    // Verify current password
                    if (string.IsNullOrEmpty(user.hash_password))
                        return BadRequest(new { message = "User password not set" });

                    bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(
                    request.CurrentPassword,
                   user.hash_password
               );

                    if (!isCurrentPasswordValid)
                    {
                        _logger.LogWarning(
                    "Failed password change attempt for user {Email} - incorrect current password",
                   currentUserEmail
                   );
                        return BadRequest(new { message = "Current password is incorrect" });
                    }

                    // Check if new password is same as current password
                    bool isSamePassword = BCrypt.Net.BCrypt.Verify(
                          request.NewPassword,
                      user.hash_password
                       );

                    if (isSamePassword)
                        return BadRequest(new { message = "New password must be different from current password" });

                    // Update both password fields for users
                    user.user_password = request.NewPassword; // Plain text (if required)
                    user.hash_password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword); // Hashed
                    user.updated_at = updateTime;  // ✅ ISO 8601 via converter

                    // Mark entity as modified
                    _context.Entry(user).State = EntityState.Modified;
                    _context.Entry(user).Property(u => u.user_password).IsModified = true;
                    _context.Entry(user).Property(u => u.hash_password).IsModified = true;

                    // Save changes
                    int rowsAffected = await _context.SaveChangesAsync();

                    if (rowsAffected == 0)
                    {
                        _logger.LogError(
                       "SaveChanges returned 0 rows affected for user {Email}",
                          currentUserEmail
                       );
                        return StatusCode(500, new
                        {
                            message = "Failed to save password changes to database",
                            error = "No rows were modified"
                        });
                    }

                    _logger.LogInformation(
                  "Password changed successfully for user {Email}. Rows affected: {RowsAffected}",
             currentUserEmail,
               rowsAffected
                );

                    return Ok(new
                    {
                        success = true,
                        message = "Password changed successfully",
                        email = currentUserEmail,
                        userType = "user",
                        changedAt = updateTime,  // ✅ ISO 8601 via converter
                        rowsAffected = rowsAffected
                    });
                }

                return NotFound(new { message = "User or subuser not found" });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
                _logger.LogError(ex, "Concurrency error changing password for {Email}", userEmail);
                return StatusCode(409, new
                {
                    success = false,
                    message = "Password change failed due to concurrency conflict",
                    error = ex.Message
                });
            }
            catch (Exception ex)
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
                _logger.LogError(ex, "Error changing password for {Email}", userEmail);
                return StatusCode(500, new
                {
                    success = false,
                    message = "Error changing password",
                    error = ex.Message
                });
            }
        }

        #region Department & Group Management

        /// <summary>
        /// Assign subuser to department and/or group
        /// Admin can organize subusers by department and group
        /// </summary>
        [HttpPut("subusers/{subuserEmail}/assign-department-group")]
        [Authorize]
        public async Task<IActionResult> AssignDepartmentGroup(string subuserEmail, [FromBody] AssignDeptGroupRequest request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                // Decode subuser email
                var decodedSubuserEmail = Base64EmailEncoder.DecodeEmailParam(subuserEmail);

                // Find subuser and verify ownership
                var subuser = await _context.subuser
                    .Where(s => s.subuser_email.ToLower() == decodedSubuserEmail.ToLower() 
                                           && s.user_email.ToLower() == currentUserEmail.ToLower()).FirstOrDefaultAsync();

                if (subuser == null)
                {
                    return NotFound(new { success = false, message = $"Subuser '{decodedSubuserEmail}' not found or not managed by you" });
                }

                // Update department if provided
                if (!string.IsNullOrEmpty(request.Department))
                {
                    subuser.Department = request.Department;
                }

                // Update group if provided
                if (request.GroupId.HasValue)
                {
                    var group = await _context.Groups.FindAsync(request.GroupId.Value);
                    if (group == null)
                    {
                        return NotFound(new { success = false, message = $"Group with ID {request.GroupId} not found" });
                    }
                    subuser.GroupId = request.GroupId;
                    subuser.subuser_group = group.name;
                }
                else if (!string.IsNullOrEmpty(request.GroupName))
                {
                    subuser.subuser_group = request.GroupName;
                }

                subuser.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Assigned subuser {Subuser} to dept={Dept}, group={Group}", 
                    decodedSubuserEmail, request.Department, request.GroupName ?? request.GroupId?.ToString());

                return Ok(new
                {
                    success = true,
                    message = "Subuser department/group updated successfully",
                    data = new
                    {
                        subuserEmail = subuser.subuser_email,
                        department = subuser.Department,
                        groupId = subuser.GroupId,
                        groupName = subuser.subuser_group
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning department/group to subuser {Email}", subuserEmail);
                return StatusCode(500, new { success = false, message = "Error updating subuser", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all subusers by department
        /// Returns subusers belonging to specified department
        /// </summary>
        [HttpGet("subusers/by-department/{department}")]
        [Authorize]
        public async Task<IActionResult> GetSubusersByDepartment(string department)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                var decodedDept = Uri.UnescapeDataString(department);

                var subusers = await _context.subuser
                    .Where(s => s.user_email.ToLower() == currentUserEmail.ToLower() 
                             && s.Department != null 
                             && s.Department.ToLower() == decodedDept.ToLower())
                    .Select(s => new
                    {
                        subuserEmail = s.subuser_email,
                        name = s.Name,
                        department = s.Department,
                        groupId = s.GroupId,
                        groupName = s.subuser_group,
                        role = s.Role,
                        status = s.status,
                        lastLogin = s.last_login
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    department = decodedDept,
                    count = subusers.Count,
                    subusers = subusers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subusers by department {Dept}", department);
                return StatusCode(500, new { success = false, message = "Error fetching subusers", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all subusers by group
        /// Returns subusers belonging to specified group
        /// </summary>
        [HttpGet("subusers/by-group/{groupId:int}")]
        [Authorize]
        public async Task<IActionResult> GetSubusersByGroup(int groupId)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                var group = await _context.Groups.FindAsync(groupId);
                var groupName = group?.name;
                
                var subusers = await _context.subuser
                    .Where(s => s.user_email.ToLower() == currentUserEmail.ToLower() 
                             && (s.GroupId == groupId || s.subuser_group == groupName))
                    .Select(s => new
                    {
                        subuserEmail = s.subuser_email,
                        name = s.Name,
                        department = s.Department,
                        groupId = s.GroupId,
                        groupName = s.subuser_group,
                        role = s.Role,
                        status = s.status,
                        lastLogin = s.last_login
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    groupId = groupId,
                    groupName = group?.name,
                    count = subusers.Count,
                    subusers = subusers
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting subusers by group {GroupId}", groupId);
                return StatusCode(500, new { success = false, message = "Error fetching subusers", error = ex.Message });
            }
        }

        /// <summary>
        /// Get all departments with subuser counts
        /// Returns list of all departments used by current user's subusers
        /// </summary>
        [HttpGet("departments")]
        [Authorize]
        public async Task<IActionResult> GetDepartments()
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                var departments = await _context.subuser
                    .Where(s => s.user_email.ToLower() == currentUserEmail.ToLower() && s.Department != null)
                    .GroupBy(s => s.Department)
                    .Select(g => new
                    {
                        department = g.Key,
                        subuserCount = g.Count()
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = departments.Count,
                    departments = departments
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting departments");
                return StatusCode(500, new { success = false, message = "Error fetching departments", error = ex.Message });
            }
        }

        /// <summary>
        /// Reassign all resources from one user/subuser to another
        /// Transfers machines, reports, sessions to target user
        /// </summary>
        [HttpPost("reassign-resources")]
        [Authorize]
        public async Task<IActionResult> ReassignResources([FromBody] ReassignResourcesRequest request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                // Decode emails
                var fromEmail = Base64EmailEncoder.DecodeEmailParam(request.FromEmail);
                var toEmail = Base64EmailEncoder.DecodeEmailParam(request.ToEmail);

                // Verify target exists (subuser under current user)
                var targetSubuser = await _context.subuser
                    .Where(s => s.subuser_email.ToLower() == toEmail.ToLower() 
                                           && s.user_email.ToLower() == currentUserEmail.ToLower()).FirstOrDefaultAsync();

                if (targetSubuser == null)
                {
                    return NotFound(new { success = false, message = $"Target subuser '{toEmail}' not found or not managed by you" });
                }

                var results = new Dictionary<string, int>();

                // Reassign Machines
                if (request.ResourceTypes == null || request.ResourceTypes.Contains("machines"))
                {
                    var machines = await _context.Machines
                        .Where(m => m.subuser_email != null && m.subuser_email.ToLower() == fromEmail.ToLower())
                        .ToListAsync();

                    foreach (var machine in machines)
                    {
                        machine.subuser_email = toEmail;
                        machine.updated_at = DateTime.UtcNow;
                    }
                    results["machines"] = machines.Count;
                }

                // Reassign Reports
                if (request.ResourceTypes == null || request.ResourceTypes.Contains("reports"))
                {
                    var reports = await _context.AuditReports
                        .Where(r => r.client_email.ToLower() == fromEmail.ToLower())
                        .ToListAsync();

                    foreach (var report in reports)
                    {
                        report.client_email = toEmail;
                    }
                    results["reports"] = reports.Count;
                }

                // Reassign Sessions
                if (request.ResourceTypes == null || request.ResourceTypes.Contains("sessions"))
                {
                    var sessions = await _context.Sessions
                        .Where(s => s.user_email.ToLower() == fromEmail.ToLower())
                        .ToListAsync();

                    foreach (var session in sessions)
                    {
                        session.user_email = toEmail;
                    }
                    results["sessions"] = sessions.Count;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Reassigned resources from {From} to {To}: {Results}", 
                    fromEmail, toEmail, string.Join(", ", results.Select(r => $"{r.Key}={r.Value}")));

                return Ok(new
                {
                    success = true,
                    message = $"Resources reassigned successfully from {fromEmail} to {toEmail}",
                    fromEmail = fromEmail,
                    toEmail = toEmail,
                    reassigned = results,
                    totalResourcesTransferred = results.Values.Sum()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reassigning resources");
                return StatusCode(500, new { success = false, message = "Error reassigning resources", error = ex.Message });
            }
        }

        /// <summary>
        /// Bulk assign multiple subusers to a department
        /// </summary>
        [HttpPost("subusers/bulk-assign-department")]
        [Authorize]
        public async Task<IActionResult> BulkAssignDepartment([FromBody] BulkAssignDeptRequest request)
        {
            try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
                    return Unauthorized(new { success = false, message = "User not authenticated" });

                if (request.SubuserEmails == null || !request.SubuserEmails.Any())
                {
                    return BadRequest(new { success = false, message = "At least one subuser email is required" });
                }

                var decodedEmails = request.SubuserEmails.Select(e => Base64EmailEncoder.DecodeEmailParam(e).ToLower()).ToList();

                var subusers = await _context.subuser
                    .Where(s => s.user_email.ToLower() == currentUserEmail.ToLower() 
                             && decodedEmails.Contains(s.subuser_email.ToLower()))
                    .ToListAsync();

                foreach (var subuser in subusers)
                {
                    subuser.Department = request.Department;
                    subuser.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Assigned {subusers.Count} subusers to department '{request.Department}'",
                    department = request.Department,
                    updatedCount = subusers.Count,
                    updatedEmails = subusers.Select(s => s.subuser_email).ToList()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error bulk assigning department");
                return StatusCode(500, new { success = false, message = "Error updating subusers", error = ex.Message });
            }
        }

        #endregion

        /// <summary>
        /// Request model for editing profile (name, phone, timezone)
        /// All fields are optional - only provided fields will be updated
        /// </summary>
        public class EditProfileRequest
        {
            /// <summary>
            /// User/Subuser name (optional)
            /// </summary>
            [MaxLength(100)]
            public string? Name { get; set; }

            /// <summary>
            /// Phone number (optional, can be set to null to clear)
            /// </summary>
            [MaxLength(20)]
            public string? Phone { get; set; }

            /// <summary>
            /// Timezone (optional, e.g., "Asia/Kolkata", "America/New_York")
            /// </summary>
            [MaxLength(100)]
            public string? Timezone { get; set; }
        }

        public class UpdateTimezoneRequest
        {
            /// <summary>
            /// Email of user/subuser to update. If not provided, updates current user's timezone
            /// </summary>
            public string? Email { get; set; }

            /// <summary>
            /// Timezone string (e.g., "Asia/Kolkata", "America/New_York", "Europe/London")
            /// </summary>
            [Required(ErrorMessage = "Timezone is required")]
            [MaxLength(100)]
            public string Timezone { get; set; } = string.Empty;
        }

        /// <summary>
        /// Self-service password change request - NO EMAIL NEEDED
        /// User can only change their own password (detected from JWT token)
        /// </summary>
        public class SelfServicePasswordChangeRequest
        {
            /// <summary>
            /// Current password for verification
            /// </summary>
            [Required(ErrorMessage = "Current password is required")]
            public string CurrentPassword { get; set; } = string.Empty;

            /// <summary>
            /// New password (minimum 8 characters)
            /// </summary>
            [Required(ErrorMessage = "New password is required")]
            [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
            public string NewPassword { get; set; } = string.Empty;
        }

        /// <summary>
        /// Unified password change request - Works for both Users and Subusers
        /// DEPRECATED: Use SelfServicePasswordChangeRequest instead for self-service
        /// This model supports admin changing other users' passwords
        /// </summary>
        [Obsolete("Use SelfServicePasswordChangeRequest for self-service password changes")]
        public class UnifiedChangePasswordRequest
        {
            /// <summary>
            /// Email of user/subuser to change password. If not provided, changes current user's password
            /// </summary>
            public string? Email { get; set; }

            /// <summary>
            /// Current password for verification
            /// </summary>
            [Required(ErrorMessage = "Current password is required")]
            public string CurrentPassword { get; set; } = string.Empty;

            /// <summary>
            /// New password (minimum 8 characters)
            /// </summary>
            [Required(ErrorMessage = "New password is required")]
            [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
            public string NewPassword { get; set; } = string.Empty;
        }

        /// <summary>
        /// Request model for adding permission to role
        /// </summary>
        public class AddPermissionRequest
        {
            /// <summary>
            /// Name of the permission to add (e.g., "UserManagement", "ReportAccess")
            /// </summary>
            [Required(ErrorMessage = "Permission name is required")]
            [MaxLength(100)]
            public string PermissionName { get; set; } = string.Empty;
        }

        /// <summary>
        /// Request model for updating all permissions for a role
        /// </summary>
        public class UpdateRolePermissionsRequest
        {
            /// <summary>
            /// List of permission names to assign to the role (replaces existing)
            /// </summary>
            [Required(ErrorMessage = "Permission names are required")]
            public List<string> PermissionNames { get; set; } = new List<string>();
        }

        #region Department & Group Request Models

        /// <summary>
        /// Request model for assigning department/group to subuser
        /// </summary>
        public class AssignDeptGroupRequest
        {
            /// <summary>
            /// Department name to assign
            /// </summary>
            public string? Department { get; set; }

            /// <summary>
            /// Group ID to assign (will also set GroupName from DB)
            /// </summary>
            public int? GroupId { get; set; }

            /// <summary>
            /// Group name to assign (used if GroupId not provided)
            /// </summary>
            public string? GroupName { get; set; }
        }

        /// <summary>
        /// Request model for reassigning resources between users
        /// </summary>
        public class ReassignResourcesRequest
        {
            /// <summary>
            /// Source email (user/subuser to transfer from)
            /// </summary>
            [Required(ErrorMessage = "FromEmail is required")]
            public string FromEmail { get; set; } = string.Empty;

            /// <summary>
            /// Target email (subuser to transfer to)
            /// </summary>
            [Required(ErrorMessage = "ToEmail is required")]
            public string ToEmail { get; set; } = string.Empty;

            /// <summary>
            /// Resource types to transfer: "machines", "reports", "sessions"
            /// If null/empty, all resource types will be transferred
            /// </summary>
            public List<string>? ResourceTypes { get; set; }
        }

        /// <summary>
        /// Request model for bulk assigning department to subusers
        /// </summary>
        public class BulkAssignDeptRequest
        {
            /// <summary>
            /// List of subuser emails to update
            /// </summary>
            [Required(ErrorMessage = "SubuserEmails are required")]
            public List<string> SubuserEmails { get; set; } = new();

            /// <summary>
            /// Department name to assign to all subusers
            /// </summary>
            [Required(ErrorMessage = "Department is required")]
            public string Department { get; set; } = string.Empty;
        }

        #endregion
    }
}