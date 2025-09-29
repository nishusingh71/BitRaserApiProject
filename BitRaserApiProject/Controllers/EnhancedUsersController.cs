using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;
using BCrypt.Net;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Users management controller with comprehensive email-based operations and role-based access control
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedUsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;

        public EnhancedUsersController(
            ApplicationDbContext context, 
            IRoleBasedAuthService authService,
            IUserDataService userDataService)
        {
            _context = context;
            _authService = authService;
            _userDataService = userDataService;
        }

        /// <summary>
        /// Get all users with role-based filtering (email-based operations)
        /// </summary>
        [HttpGet]
        [RequirePermission("READ_ALL_USERS")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers([FromQuery] UserFilterRequest? filter)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            IQueryable<users> query = _context.Users;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_USERS"))
            {
                // Users can only see their own profile unless they have elevated permissions
                if (await _authService.HasPermissionAsync(currentUserEmail!, "READ_MANAGED_USERS"))
                {
                    // Get users this person can manage
                    var managedUserEmails = await GetManagedUserEmailsAsync(currentUserEmail!);
                    query = query.Where(u => managedUserEmails.Contains(u.user_email));
                }
                else
                {
                    // Only show own profile
                    query = query.Where(u => u.user_email == currentUserEmail);
                }
            }

            // Apply additional filters if provided
            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.UserEmail))
                    query = query.Where(u => u.user_email.Contains(filter.UserEmail));

                if (!string.IsNullOrEmpty(filter.UserName))
                    query = query.Where(u => u.user_name.Contains(filter.UserName));

                if (!string.IsNullOrEmpty(filter.PhoneNumber))
                    query = query.Where(u => u.phone_number != null && u.phone_number.Contains(filter.PhoneNumber));

                if (filter.CreatedFrom.HasValue)
                    query = query.Where(u => u.created_at >= filter.CreatedFrom.Value);

                if (filter.CreatedTo.HasValue)
                    query = query.Where(u => u.created_at <= filter.CreatedTo.Value);

                if (filter.HasLicenses.HasValue)
                {
                    if (filter.HasLicenses.Value)
                        query = query.Where(u => !string.IsNullOrEmpty(u.license_details_json) && u.license_details_json != "{}");
                    else
                        query = query.Where(u => string.IsNullOrEmpty(u.license_details_json) || u.license_details_json == "{}");
                }
            }

            var users = await query
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .OrderByDescending(u => u.created_at)
                .Take(filter?.PageSize ?? 100)
                .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                .Select(u => new {
                    userEmail = u.user_email,
                    userName = u.user_name,
                    phoneNumber = u.phone_number,
                    createdAt = u.created_at,
                    updatedAt = u.updated_at,
                    roles = u.UserRoles.Select(ur => ur.Role.RoleName).ToList(),
                    hasLicenses = !string.IsNullOrEmpty(u.license_details_json) && u.license_details_json != "{}",
                    hasPaymentDetails = !string.IsNullOrEmpty(u.payment_details_json) && u.payment_details_json != "{}"
                })
                .ToListAsync();

            return Ok(users);
        }

        /// <summary>
        /// Get user by email with comprehensive details
        /// </summary>
        [HttpGet("{email}")]
        [RequirePermission("READ_USER")]
        public async Task<ActionResult<object>> GetUser(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var user = await _context.Users
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .FirstOrDefaultAsync(u => u.user_email == email);
            
            if (user == null) return NotFound($"User with email {email} not found");

            // Check if user can view this profile
            if (email != currentUserEmail && !await CanManageUserAsync(currentUserEmail!, email))
            {
                return StatusCode(403,new { error = "You can only view your own profile or profiles you manage" });
            }

            var userDetails = new {
                userEmail = user.user_email,
                userName = user.user_name,
                phoneNumber = user.phone_number,
                createdAt = user.created_at,
                updatedAt = user.updated_at,
                roles = user.UserRoles.Select(ur => new {
                    roleName = ur.Role.RoleName,
                    description = ur.Role.Description,
                    hierarchyLevel = ur.Role.HierarchyLevel,
                    assignedAt = ur.AssignedAt,
                    assignedBy = ur.AssignedByEmail
                }).ToList(),
                permissions = user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.PermissionName)
                    .Distinct()
                    .ToList(),
                hasLicenses = !string.IsNullOrEmpty(user.license_details_json) && user.license_details_json != "{}",
                hasPaymentDetails = !string.IsNullOrEmpty(user.payment_details_json) && user.payment_details_json != "{}"
            };

            return Ok(userDetails);
        }

        /// <summary>
        /// Create a new user - Email-based creation
        /// </summary>
        [HttpPost]
        [RequirePermission("CREATE_USER")]
        public async Task<ActionResult<object>> CreateUser([FromBody] UserCreateRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "CREATE_USER"))
                return StatusCode(403,new { error = "Insufficient permissions to create users" });

            // Validate input
            if (string.IsNullOrEmpty(request.UserEmail) || string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
                return BadRequest("User email, name, and password are required");

            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == request.UserEmail);
            if (existingUser != null)
                return Conflict($"User with email {request.UserEmail} already exists");

            // Create user
            var newUser = new users
            {
                user_email = request.UserEmail,
                user_name = request.UserName,
                user_password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                phone_number = request.PhoneNumber ?? "",
                payment_details_json = request.PaymentDetailsJson ?? "{}",
                license_details_json = request.LicenseDetailsJson ?? "{}",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Assign default role if specified
            if (!string.IsNullOrEmpty(request.DefaultRole))
            {
                await AssignRoleToUserAsync(request.UserEmail, request.DefaultRole, currentUserEmail!);
            }

            var response = new {
                userEmail = newUser.user_email,
                userName = newUser.user_name,
                createdAt = newUser.created_at,
                message = "User created successfully"
            };

            return CreatedAtAction(nameof(GetUser), new { email = newUser.user_email }, response);
        }

        /// <summary>
        /// Public user registration endpoint (no authentication required)
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> RegisterUser([FromBody] UserRegistrationRequest request)
        {
            // Validate input
            if (string.IsNullOrEmpty(request.UserEmail) || string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
                return BadRequest("User email, name, and password are required");

            // Validate email format
            if (!IsValidEmail(request.UserEmail))
                return BadRequest("Invalid email format");

            // Validate password strength
            if (!IsValidPassword(request.Password))
                return BadRequest("Password must be at least 8 characters with uppercase, lowercase, number, and special character");

            // Check if user already exists
            var existingUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == request.UserEmail);
            if (existingUser != null)
                return Conflict($"User with email {request.UserEmail} already exists");

            // Create user
            var newUser = new users
            {
                user_email = request.UserEmail,
                user_name = request.UserName,
                user_password = BCrypt.Net.BCrypt.HashPassword(request.Password),
                phone_number = request.PhoneNumber ?? "",
                payment_details_json = "{}",
                license_details_json = "{}",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // Auto-assign User role for public registration
            await AssignRoleToUserAsync(request.UserEmail, "User", "system");

            var response = new {
                userEmail = newUser.user_email,
                userName = newUser.user_name,
                createdAt = newUser.created_at,
                message = "User registered successfully"
            };

            return CreatedAtAction(nameof(GetUser), new { email = newUser.user_email }, response);
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Validate password strength
        /// </summary>
        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasUpper = false, hasLower = false, hasDigit = false, hasSpecial = false;

            foreach (char c in password)
            {
                if (char.IsUpper(c)) hasUpper = true;
                else if (char.IsLower(c)) hasLower = true;
                else if (char.IsDigit(c)) hasDigit = true;
                else if (!char.IsLetterOrDigit(c)) hasSpecial = true;
            }

            return hasUpper && hasLower && hasDigit && hasSpecial;
        }

        /// <summary>
        /// Update user information by email
        /// </summary>
        [HttpPut("{email}")]
        [RequirePermission("UPDATE_USER")]
        public async Task<IActionResult> UpdateUser(string email, [FromBody] UserUpdateRequest request)
        {
            if (email != request.UserEmail)
                return BadRequest("Email mismatch in request");

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            
            if (user == null) return NotFound($"User with email {email} not found");

            // Check if user can update this profile
            if (email != currentUserEmail && !await CanManageUserAsync(currentUserEmail!, email))
            {
                return StatusCode(403, new { error = "You can only update your own profile or profiles you manage" });
            }

            // Update user information
            if (!string.IsNullOrEmpty(request.UserName))
                user.user_name = request.UserName;

            if (!string.IsNullOrEmpty(request.PhoneNumber))
                user.phone_number = request.PhoneNumber;

            if (!string.IsNullOrEmpty(request.PaymentDetailsJson))
                user.payment_details_json = request.PaymentDetailsJson;

            if (!string.IsNullOrEmpty(request.LicenseDetailsJson))
                user.license_details_json = request.LicenseDetailsJson;

            user.updated_at = DateTime.UtcNow;

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "User updated successfully", 
                userEmail = email,
                updatedAt = user.updated_at
            });
        }

        /// <summary>
        /// Change user password by email
        /// </summary>
        [HttpPatch("{email}/change-password")]
        [RequirePermission("CHANGE_USER_PASSWORDS")]
        public async Task<IActionResult> ChangePassword(string email, [FromBody] ChangeUserPasswordRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            
            if (user == null) return NotFound($"User with email {email} not found");

            // Users can change their own password, or admins can change others
            if (email != currentUserEmail && !await _authService.HasPermissionAsync(currentUserEmail!, "CHANGE_USER_PASSWORDS"))
            {
                return StatusCode(403,new { error = "You can only change your own password" });
            }

            if (string.IsNullOrEmpty(request.NewPassword))
                return BadRequest("New password is required");

            // Verify current password if it's the user changing their own password
            if (email == currentUserEmail && !string.IsNullOrEmpty(request.CurrentPassword))
            {
                if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.user_password))
                {
                    return BadRequest("Current password is incorrect");
                }
            }

            user.user_password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
            user.updated_at = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Password changed successfully", 
                userEmail = email
            });
        }

        /// <summary>
        /// Assign role to user by email - Admin access
        /// </summary>
        [HttpPost("{email}/assign-role")]
        [RequirePermission("ASSIGN_ROLES")]
        public async Task<IActionResult> AssignRole(string email, [FromBody] AssignUserRoleRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "ASSIGN_ROLES"))
                return StatusCode(403,new { error = "Insufficient permissions to assign roles" });

            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null) return NotFound($"User with email {email} not found");

            await AssignRoleToUserAsync(email, request.RoleName, currentUserEmail!);

            return Ok(new { 
                message = $"Role {request.RoleName} assigned to user {email}", 
                userEmail = email,
                roleName = request.RoleName,
                assignedBy = currentUserEmail,
                assignedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Remove role from user by email
        /// </summary>
        [HttpDelete("{email}/remove-role/{roleName}")]
        [RequirePermission("ASSIGN_ROLES")]
        public async Task<IActionResult> RemoveRole(string email, string roleName)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null) return NotFound($"User with email {email} not found");

            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);
            if (role == null) return NotFound($"Role {roleName} not found");

            var userRole = await _context.UserRoles
                .FirstOrDefaultAsync(ur => ur.UserId == user.user_id && ur.RoleId == role.RoleId);

            if (userRole == null)
                return NotFound($"Role {roleName} not assigned to user {email}");

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = $"Role {roleName} removed from user {email}",
                userEmail = email,
                roleName = roleName,
                removedBy = currentUserEmail,
                removedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Delete user by email
        /// </summary>
        [HttpDelete("{email}")]
        [RequirePermission("DELETE_USER")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            
            if (user == null) return NotFound($"User with email {email} not found");

            // Check if user can delete this profile
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_USER"))
                return StatusCode(403,new { error = "Insufficient permissions to delete users" });

            // Don't allow users to delete themselves
            if (email == currentUserEmail)
                return BadRequest("You cannot delete your own account");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "User deleted successfully", 
                userEmail = email,
                deletedAt = DateTime.UtcNow
            });
        }

        /// <summary>
        /// Get user statistics by email
        /// </summary>
        [HttpGet("{email}/statistics")]
        [RequirePermission("READ_USER_STATISTICS")]
        public async Task<ActionResult<object>> GetUserStatistics(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            
            // Check if user can view statistics for this email
            if (email != currentUserEmail && !await CanManageUserAsync(currentUserEmail!, email))
            {
                return StatusCode(403,new { error = "You can only view statistics for your own account or accounts you manage" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null) return NotFound($"User with email {email} not found");

            var stats = new {
                UserEmail = email,
                UserName = user.user_name,
                AccountAge = DateTime.UtcNow - user.created_at,
                TotalMachines = await _context.Machines.CountAsync(m => m.user_email == email),
                ActiveLicenses = await _context.Machines
                    .CountAsync(m => m.user_email == email && m.license_activated),
                TotalReports = await _context.AuditReports.CountAsync(r => r.client_email == email),
                TotalSessions = await _context.Sessions.CountAsync(s => s.user_email == email),
                ActiveSessions = await _context.Sessions
                    .CountAsync(s => s.user_email == email && s.session_status == "active"),
                TotalSubusers = await _context.subuser.CountAsync(s => s.user_email == email),
                TotalLogs = await _context.logs.CountAsync(l => l.user_email == email),
                LastActivity = await GetLastActivityAsync(email),
                RoleHistory = await _context.UserRoles
                    .Where(ur => ur.User.user_email == email)
                    .Include(ur => ur.Role)
                    .Select(ur => new {
                        roleName = ur.Role.RoleName,
                        assignedAt = ur.AssignedAt,
                        assignedBy = ur.AssignedByEmail
                    })
                    .ToListAsync()
            };

            return Ok(stats);
        }

        #region Private Helper Methods

        private async Task<bool> CanManageUserAsync(string currentUserEmail, string targetUserEmail)
        {
            // Check if current user has admin permissions
            if (await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_USERS"))
                return true;

            // Check if current user can manage this specific user (implement hierarchy logic)
            return await _authService.CanManageUserAsync(currentUserEmail, targetUserEmail);
        }

        private async Task<List<string>> GetManagedUserEmailsAsync(string managerEmail)
        {
            // Get users this manager can manage based on hierarchy
            var managedEmails = new List<string> { managerEmail };

            // Add logic to get users managed by this manager
            // This is a placeholder - implement based on your user hierarchy
            
            return managedEmails;
        }

        private async Task AssignRoleToUserAsync(string userEmail, string roleName, string assignedByEmail)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == roleName);

            if (user != null && role != null)
            {
                // Check if role already assigned
                var existingRole = await _context.UserRoles
                    .FirstOrDefaultAsync(ur => ur.UserId == user.user_id && ur.RoleId == role.RoleId);

                if (existingRole == null)
                {
                    var userRole = new UserRole
                    {
                        UserId = user.user_id,
                        RoleId = role.RoleId,
                        AssignedAt = DateTime.UtcNow,
                        AssignedByEmail = assignedByEmail
                    };

                    _context.UserRoles.Add(userRole);
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task<DateTime?> GetLastActivityAsync(string userEmail)
        {
            // Get last activity from sessions, logs, or other activity tables
            var lastSession = await _context.Sessions
                .Where(s => s.user_email == userEmail)
                .OrderByDescending(s => s.login_time)
                .Select(s => s.login_time)
                .FirstOrDefaultAsync();

            return lastSession;
        }

        #endregion
    }

    #region Request Models

    /// <summary>
    /// User filter request model
    /// </summary>
    public class UserFilterRequest
    {
        public string? UserEmail { get; set; }
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
        public bool? HasLicenses { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 100;
    }

    /// <summary>
    /// User creation request model
    /// </summary>
    public class UserCreateRequest
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? PaymentDetailsJson { get; set; }
        public string? LicenseDetailsJson { get; set; }
        public string? DefaultRole { get; set; }
    }

    /// <summary>
    /// User registration request model (for public registration)
    /// </summary>
    public class UserRegistrationRequest
    {
        public string UserEmail { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// User update request model
    /// </summary>
    public class UserUpdateRequest
    {
        public string UserEmail { get; set; } = string.Empty;
        public string? UserName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? PaymentDetailsJson { get; set; }
        public string? LicenseDetailsJson { get; set; }
    }

    /// <summary>
    /// Change password request model
    /// </summary>
    public class ChangeUserPasswordRequest
    {
        public string? CurrentPassword { get; set; }
        public string NewPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Assign role request model
    /// </summary>
    public class AssignUserRoleRequest
    {
        public string RoleName { get; set; } = string.Empty;
    }

    #endregion
}