using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.ComponentModel.DataAnnotations;
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
            
            // Enhanced fields - User/Subuser details
            public string? UserName { get; set; }
            public string? UserRole { get; set; }  // Primary role
            public string? UserGroup { get; set; }
            public string? Department { get; set; }
            public string? Timezone { get; set; }  // User's timezone preference
            public DateTime? LoginTime { get; set; }
            public DateTime? LastLogoutTime { get; set; }
            public string? Phone { get; set; }
            public string? ParentUserEmail { get; set; } // For subusers only
            public int? UserId { get; set; }  // user_id or subuser_id
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
    users? mainUser = null;
    subuser? subuserData = null;

      // Try to authenticate as main user first
  var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == request.Email);
      if (user != null && !string.IsNullOrEmpty(user.hash_password) && BCrypt.Net.BCrypt.Verify(request.Password, user.hash_password))
   {
        userEmail = request.Email;
         isSubuser = false;
         mainUser = user;
        }
        else
       {
// Try to authenticate as subuser
        var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == request.Email);
         if (subuser != null && BCrypt.Net.BCrypt.Verify(request.Password, subuser.subuser_password))
 {
      userEmail = request.Email;
      isSubuser = true;
           subuserData = subuser;
    }
      }

  if (userEmail == null)
         {
            return Unauthorized(new { message = "Invalid credentials" });
           }

    // Get last logout time from users/subusers table or sessions
 DateTime? lastLogoutTime = null;
                if (isSubuser && subuserData != null)
     {
    lastLogoutTime = subuserData.last_logout;
             }
           else if (mainUser != null)
         {
      lastLogoutTime = mainUser.last_logout;
       }
        
                // Fallback to sessions table if not in user table
   if (!lastLogoutTime.HasValue)
   {
          var lastSession = await _context.Sessions
            .Where(s => s.user_email == userEmail && s.logout_time != null)
         .OrderByDescending(s => s.logout_time)
 .FirstOrDefaultAsync();
          lastLogoutTime = lastSession?.logout_time;
          }

  // Create session entry for tracking
          var loginTime = DateTime.UtcNow;
     var session = new Sessions
       {
  user_email = userEmail,
          login_time = loginTime,
        ip_address = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
  device_info = Request.Headers["User-Agent"].ToString(),
         session_status = "active"
        };

 _context.Sessions.Add(session);

    // Update last_login in users or subusers table
         if (isSubuser && subuserData != null)
                {
    subuserData.last_login = loginTime;
                subuserData.LastLoginIp = session.ip_address;
        _context.Entry(subuserData).State = EntityState.Modified;
 }
       else if (mainUser != null)
                {
      mainUser.last_login = loginTime;
          _context.Entry(mainUser).State = EntityState.Modified;
         }

     await _context.SaveChangesAsync();

        var token = await GenerateJwtTokenAsync(userEmail, isSubuser);
          
     // ✅ Get roles from RBAC system (UserRoles/SubuserRoles tables)
   var rolesFromRBAC = (await _roleService.GetUserRolesAsync(userEmail, isSubuser)).ToList();
         var permissions = await _roleService.GetUserPermissionsAsync(userEmail, isSubuser);

                // ✅ ENHANCED: Build complete roles array
      var allRoles = new List<string>(rolesFromRBAC);
       
        // ✅ Add user_role field to roles array if not already present and if it exists
          if (isSubuser && subuserData != null)
      {
      // For subusers: Add subuser.Role field if not empty and not already in roles
            if (!string.IsNullOrEmpty(subuserData.Role) && !allRoles.Contains(subuserData.Role))
                {
     allRoles.Add(subuserData.Role);
          }
     }
     else if (mainUser != null)
          {
         // For main users: Add user.user_role field if not empty and not already in roles
    if (!string.IsNullOrEmpty(mainUser.user_role) && !allRoles.Contains(mainUser.user_role))
       {
      allRoles.Add(mainUser.user_role);
           }
            }
          
        // ✅ If roles array is still empty, add "SuperAdmin" as default (matches CreateUser behavior)
             if (!allRoles.Any())
     {
           allRoles.Add("SuperAdmin");
        }

   _logger.LogInformation("User login successful: {Email} ({UserType})", userEmail, isSubuser ? "subuser" : "user");

   // Build enhanced response
                var response = new RoleBasedLoginResponse
        {
   Token = token,
              UserType = isSubuser ? "subuser" : "user",
  Email = userEmail,
         Roles = allRoles,  // ✅ Use enhanced roles array
               Permissions = permissions,
              ExpiresAt = DateTime.UtcNow.AddHours(8),
          LoginTime = loginTime,
     LastLogoutTime = lastLogoutTime
   };

      // Add user-specific details
      if (isSubuser && subuserData != null)
      {
     response.UserName = subuserData.Name;
          // ✅ Priority: First role from allRoles array (includes RBAC + field roles)
      response.UserRole = allRoles.FirstOrDefault() ?? "User";
          response.Department = subuserData.Department;
       response.Phone = subuserData.Phone;
    response.Timezone = subuserData.timezone;
response.ParentUserEmail = subuserData.user_email;
         response.UserId = subuserData.subuser_id;
            
         // Get group name if GroupId exists
  if (subuserData.GroupId.HasValue)
    {
         var group = await _context.Set<Group>().FindAsync(subuserData.GroupId.Value);
         response.UserGroup = group?.name;
      }
  }
                else if (mainUser != null)
                {
           response.UserName = mainUser.user_name;
        // ✅ Priority: First role from allRoles array (includes RBAC + field roles + default)
    response.UserRole = allRoles.FirstOrDefault() ?? "User";
    response.Department = mainUser.department;
         response.Phone = mainUser.phone_number;
        response.Timezone = mainUser.timezone;
              response.UserId = mainUser.user_id;
           
    // Get group name if user_group exists
         if (!string.IsNullOrEmpty(mainUser.user_group))
     {
              // Try to parse as int to get group from Groups table
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

    return Ok(response);
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

        // Update last_logout in users or subusers table
     if (isSubuser)
  {
    var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == userEmail);
   if (subuser != null)
       {
  subuser.last_logout = logoutTime;
 _context.Entry(subuser).State = EntityState.Modified;
        }
       }
 else
  {
    var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
      if (user != null)
{
 user.last_logout = logoutTime;
        _context.Entry(user).State = EntityState.Modified;
      }
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
  sessionsEnded = activeSessions.Count,
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

                // Get assigner hierarchy level
                var assignerLevel = await _roleService.GetUserHierarchyLevelAsync(assignerEmail, false);

                int? targetUserId = null;
                int? targetSubuserId = null;

                // Find target user/subuser and get current hierarchy level
                if (request.IsSubuser)
                {
                    var subuser = await _context.subuser
                        .FirstOrDefaultAsync(s => s.subuser_email == request.Email);
      
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
                        .FirstOrDefaultAsync(u => u.user_email == request.Email);
        
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
                        .FirstOrDefaultAsync(r => r.RoleName == roleName);

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

       // Assign new roles
        foreach (var role in rolesToAssign)
 {
           if (request.IsSubuser && targetSubuserId.HasValue)
         {
        var subuserRole = new SubuserRole
     {
      SubuserId = targetSubuserId.Value,
         RoleId = role.RoleId,
    AssignedByEmail = assignerEmail,
    AssignedAt = DateTime.UtcNow
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
   AssignedAt = DateTime.UtcNow
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
    updatedAt = DateTime.UtcNow
         });
  }
    catch (Exception ex)
        {
    _logger.LogError(ex, "Error updating roles for {Email}", request.Email);
         return StatusCode(500, new { message = "An error occurred while updating roles", error = ex.Message });
         }
 }

        /// <summary>
        /// Update timezone for user or subuser
     /// User can update their own timezone, admins can update any user's timezone
        /// </summary>
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

       // Check if target is subuser or user
var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == targetEmail);
        if (subuser != null)
    {
    subuser.timezone = request.Timezone;
      subuser.UpdatedAt = DateTime.UtcNow;
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
   updatedAt = DateTime.UtcNow
     });
   }

         var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == targetEmail);
    if (user != null)
            {
       user.timezone = request.Timezone;
  user.updated_at = DateTime.UtcNow;
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
  updatedAt = DateTime.UtcNow
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

    // Try to find as subuser first
    var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == currentUserEmail);
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
    subuser.UpdatedAt = DateTime.UtcNow;

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
    return StatusCode(500, new { 
   message = "Failed to save password changes to database",
   error = "No rows were modified"
});
}

  _logger.LogInformation(
     "Password changed successfully for subuser {Email}. Rows affected: {RowsAffected}", 
        currentUserEmail, 
 rowsAffected
    );

    return Ok(new { 
  success = true,
      message = "Password changed successfully", 
 email = currentUserEmail,
     userType = "subuser",
  changedAt = DateTime.UtcNow,
  rowsAffected = rowsAffected
            });
   }

    // Try to find as main user
             var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == currentUserEmail);
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
  user.updated_at = DateTime.UtcNow;

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
       return StatusCode(500, new { 
message = "Failed to save password changes to database",
       error = "No rows were modified"
      });
 }

         _logger.LogInformation(
       "Password changed successfully for user {Email}. Rows affected: {RowsAffected}", 
  currentUserEmail, 
    rowsAffected
     );

 return Ok(new { 
   success = true,
      message = "Password changed successfully", 
        email = currentUserEmail,
   userType = "user",
          changedAt = DateTime.UtcNow,
   rowsAffected = rowsAffected
      });
 }

  return NotFound(new { message = "User or subuser not found" });
       }
          catch (DbUpdateConcurrencyException ex)
    {
 var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
       _logger.LogError(ex, "Concurrency error changing password for {Email}", userEmail);
     return StatusCode(409, new { 
    success = false,
  message = "Password change failed due to concurrency conflict",
      error = ex.Message
     });
       }
  catch (Exception ex)
  {
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
  _logger.LogError(ex, "Error changing password for {Email}", userEmail);
   return StatusCode(500, new { 
        success = false,
   message = "Error changing password",
           error = ex.Message
   });
       }
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
    }
}