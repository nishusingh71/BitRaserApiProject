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

            // Create user with both plain text and hashed password
            var newUser = new users
            {
                user_email = request.UserEmail,
                user_name = request.UserName,
                user_password = request.Password,  // Plain text
                hash_password = BCrypt.Net.BCrypt.HashPassword(request.Password),  // Hashed
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

            // Create user with both plain text and hashed password
            var newUser = new users
            {
                user_email = request.UserEmail,
                user_name = request.UserName,
                user_password = request.Password,  // Plain text
                hash_password = BCrypt.Net.BCrypt.HashPassword(request.Password),  // Hashed
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
        public async Task<IActionResult> ChangePassword(string email, [FromBody] ChangeUserPasswordRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null) 
                return NotFound(new { message = $"User with email {email} not found" });

            // Users can change their own password without permission check
            // Admins need CHANGE_USER_PASSWORDS permission to change others' passwords
            if (email != currentUserEmail)
            {
                // Trying to change someone else's password - need admin permission
                if (!await _authService.HasPermissionAsync(currentUserEmail, "CHANGE_USER_PASSWORDS"))
                {
                    return StatusCode(403, new { error = "You can only change your own password or need CHANGE_USER_PASSWORDS permission to change others' passwords" });
                }
            }

            if (string.IsNullOrEmpty(request.NewPassword))
                return BadRequest(new { message = "New password is required" });

            // Verify current password if it's the user changing their own password
            if (email == currentUserEmail)
            {
                // User is changing their own password - must provide current password
                if (string.IsNullOrEmpty(request.CurrentPassword))
                {
                    return BadRequest(new { message = "Current password is required when changing your own password" });
                }

                // Verify current password with enhanced error handling
                try
                {
                    bool isPasswordValid = false;

                    // Check both password fields for verification
                    // Priority: hash_password (BCrypt) > user_password (plain text or legacy hash)
                    
                    if (!string.IsNullOrEmpty(user.hash_password))
                    {
                        // Use hash_password field (preferred - BCrypt hashed)
                        try
                        {
                            isPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.hash_password);
                        }
                        catch (BCrypt.Net.SaltParseException)
                        {
                            // If hash_password is corrupted, try plain text comparison with user_password
                            if (!string.IsNullOrEmpty(user.user_password))
                            {
                                isPasswordValid = user.user_password == request.CurrentPassword;
                            }
                        }
                    }
                    else if (!string.IsNullOrEmpty(user.user_password))
                    {
                        // Fallback to user_password field (legacy support)
                        // Check if it's a BCrypt hash or plain text
                        if (user.user_password.StartsWith("$2"))
                        {
                            try
                            {
                                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.user_password);
                            }
                            catch (BCrypt.Net.SaltParseException)
                            {
                                // If BCrypt fails, try plain text comparison
                                isPasswordValid = user.user_password == request.CurrentPassword;
                            }
                        }
                        else
                        {
                            // Plain text password in user_password field
                            isPasswordValid = user.user_password == request.CurrentPassword;
                        }
                    }

                    if (!isPasswordValid)
                    {
                        return BadRequest(new { message = "Current password is incorrect" });
                    }
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { 
                        message = "Error verifying current password", 
                        error = ex.Message,
                        hint = "Your password may be in an old format. Please contact administrator."
                    });
                }
            }

            // Update both password fields - plain text and hashed
            try
            {
                // Store plain text password in user_password field
                user.user_password = request.NewPassword;
                
                // Store BCrypt hashed password in hash_password field
                user.hash_password = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                
                // Update timestamp
                user.updated_at = DateTime.UtcNow;

                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Password changed successfully", 
                    userEmail = email,
                    updatedAt = user.updated_at,
                    passwordUpdated = true,
                    hashUpdated = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error changing password", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Update user license details by email
        /// </summary>
        [HttpPatch("{email}/update-license")]
        public async Task<IActionResult> UpdateLicense(string email, [FromBody] UpdateLicenseRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null) 
                return NotFound(new { message = $"User with email {email} not found" });

            // Users can update their own license, or admins can update others
            if (email != currentUserEmail && !await _authService.HasPermissionAsync(currentUserEmail, "UPDATE_USER_LICENSE"))
            {
                return StatusCode(403, new { error = "You can only update your own license details or need UPDATE_USER_LICENSE permission" });
            }

            if (string.IsNullOrEmpty(request.LicenseDetailsJson))
                return BadRequest(new { message = "License details JSON is required" });

            // Validate JSON format
            try
            {
                System.Text.Json.JsonDocument.Parse(request.LicenseDetailsJson);
            }
            catch
            {
                return BadRequest(new { message = "Invalid JSON format for license details" });
            }

            user.license_details_json = request.LicenseDetailsJson;
            user.updated_at = DateTime.UtcNow;

            try
            {
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "License details updated successfully", 
                    userEmail = email,
                    updatedAt = user.updated_at
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error updating license details", 
                    error = ex.Message 
                });
            }
        }

        /// <summary>
        /// Update user payment details by email
        /// </summary>
        [HttpPatch("{email}/update-payment")]
        public async Task<IActionResult> UpdatePayment(string email, [FromBody] UpdatePaymentRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
            if (user == null) 
                return NotFound(new { message = $"User with email {email} not found" });

            // Users can update their own payment details, or admins can update others
            if (email != currentUserEmail && !await _authService.HasPermissionAsync(currentUserEmail, "UPDATE_PAYMENT_DETAILS"))
            {
                return StatusCode(403, new { error = "You can only update your own payment details or need UPDATE_PAYMENT_DETAILS permission" });
            }

            if (string.IsNullOrEmpty(request.PaymentDetailsJson))
                return BadRequest(new { message = "Payment details JSON is required" });

            // Validate JSON format
            try
            {
                System.Text.Json.JsonDocument.Parse(request.PaymentDetailsJson);
            }
            catch
            {
                return BadRequest(new { message = "Invalid JSON format for payment details" });
            }

            user.payment_details_json = request.PaymentDetailsJson;
            user.updated_at = DateTime.UtcNow;

            try
            {
                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Payment details updated successfully", 
                    userEmail = email,
                    updatedAt = user.updated_at
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { 
                    message = "Error updating payment details", 
                    error = ex.Message 
                });
            }
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
    /// User filter request model - All fields optional for flexible filtering
    /// </summary>
    /// <example>
    /// {
    ///   "UserEmail": "test@example.com",
    ///   "UserName": "John Doe",
    ///   "Page": 0,
    ///   "PageSize": 10
    /// }
    /// </example>
    public class UserFilterRequest
    {
        /// <summary>Filter by user email (partial match)</summary>
        /// <example>test@example.com</example>
        public string? UserEmail { get; set; }
        
        /// <summary>Filter by user name (partial match)</summary>
        /// <example>John Doe</example>
        public string? UserName { get; set; }
        
        /// <summary>Filter by phone number (partial match)</summary>
        /// <example>+1234567890</example>
        public string? PhoneNumber { get; set; }
        
        /// <summary>Filter users created from this date</summary>
        /// <example>2024-01-01T00:00:00Z</example>
        public DateTime? CreatedFrom { get; set; }
        
        /// <summary>Filter users created until this date</summary>
        /// <example>2024-12-31T23:59:59Z</example>
        public DateTime? CreatedTo { get; set; }
        
        /// <summary>Filter by license presence</summary>
        /// <example>true</example>
        public bool? HasLicenses { get; set; }
        
        /// <summary>Page number for pagination (0-based)</summary>
        /// <example>0</example>
        public int Page { get; set; } = 0;
        
        /// <summary>Number of items per page</summary>
        /// <example>10</example>
        public int PageSize { get; set; } = 10;
    }

    /// <summary>
    /// User creation request model - Admin use
    /// </summary>
    /// <example>
    /// {
    ///   "UserEmail": "newuser@example.com",
    ///   "UserName": "New User",
    ///   "Password": "SecurePass@123",
    ///   "PhoneNumber": "+1234567890",
    ///   "DefaultRole": "User"
    /// }
    /// </example>
    public class UserCreateRequest
    {
        /// <summary>User's email address (must be unique)</summary>
        /// <example>newuser@example.com</example>
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string UserEmail { get; set; } = null!;
        
        /// <summary>User's full name</summary>
        /// <example>John Doe</example>
        [System.ComponentModel.DataAnnotations.Required]
        public string UserName { get; set; } = null!;
        
        /// <summary>User's password (min 8 chars, uppercase, lowercase, number, special char)</summary>
        /// <example>SecurePass@123</example>
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MinLength(8)]
        public string Password { get; set; } = null!;
        
        /// <summary>User's phone number (optional)</summary>
        /// <example>+1234567890</example>
        public string? PhoneNumber { get; set; }
        
        /// <summary>Payment details as JSON (optional)</summary>
        /// <example>{"cardType":"Visa","last4":"1234"}</example>
        public string? PaymentDetailsJson { get; set; }
        
        /// <summary>License details as JSON (optional)</summary>
        /// <example>{"licenseKey":"ABC-123","plan":"premium"}</example>
        public string? LicenseDetailsJson { get; set; }
        
        /// <summary>Default role to assign (optional)</summary>
        /// <example>User</example>
        public string? DefaultRole { get; set; }
    }

    /// <summary>
    /// User registration request model - Public registration
    /// </summary>
    /// <example>
    /// {
    ///   "UserEmail": "user@example.com",
    ///   "UserName": "John Doe",
    ///   "Password": "SecurePass@123",
    ///   "PhoneNumber": "+1234567890"
    /// }
    /// </example>
    public class UserRegistrationRequest
    {
        /// <summary>User's email address</summary>
        /// <example>user@example.com</example>
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.EmailAddress]
        public string UserEmail { get; set; } = null!;
        
        /// <summary>User's full name</summary>
        /// <example>John Doe</example>
        [System.ComponentModel.DataAnnotations.Required]
        public string UserName { get; set; } = null!;
        
        /// <summary>Password (min 8 chars with uppercase, lowercase, number, special char)</summary>
        /// <example>SecurePass@123</example>
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MinLength(8)]
        public string Password { get; set; } = null!;
        
        /// <summary>Phone number (optional)</summary>
        /// <example>+1234567890</example>
        public string? PhoneNumber { get; set; }
    }

    /// <summary>
    /// User update request model - Update user profile
    /// </summary>
    /// <example>
    /// {
    ///   "UserEmail": "user@example.com",
    ///   "UserName": "Updated Name",
    ///   "PhoneNumber": "+9876543210"
    /// }
    /// </example>
    public class UserUpdateRequest
    {
        /// <summary>User email (must match URL parameter)</summary>
        /// <example>user@example.com</example>
        [System.ComponentModel.DataAnnotations.Required]
        public string UserEmail { get; set; } = null!;
        
        /// <summary>New user name (optional)</summary>
        /// <example>Updated Name</example>
        public string? UserName { get; set; }
        
        /// <summary>New phone number (optional)</summary>
        /// <example>+9876543210</example>
        public string? PhoneNumber { get; set; }
        
        /// <summary>Payment details JSON (optional)</summary>
        /// <example>{"cardType":"MasterCard","last4":"5678"}</example>
        public string? PaymentDetailsJson { get; set; }
        
        /// <summary>License details JSON (optional)</summary>
        /// <example>{"licenseKey":"XYZ-789","plan":"enterprise"}</example>
        public string? LicenseDetailsJson { get; set; }
    }

    /// <summary>
    /// Change password request model
    /// </summary>
    /// <example>
    /// {
    ///   "CurrentPassword": "OldPass@123",
    ///   "NewPassword": "NewSecure@456"
    /// }
    /// </example>
    public class ChangeUserPasswordRequest
    {
        /// <summary>Current password (required when changing own password)</summary>
        /// <example>OldPass@123</example>
        public string? CurrentPassword { get; set; }
        
        /// <summary>New password (min 8 characters)</summary>
        /// <example>NewSecure@456</example>
        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.MinLength(8)]
        public string NewPassword { get; set; } = null!;
    }

    /// <summary>
    /// Update license details request model
    /// </summary>
    /// <example>
    /// {
    ///   "LicenseDetailsJson": "{\"licenseKey\":\"ABC-123\",\"plan\":\"premium\",\"expiryDate\":\"2025-12-31\"}"
    /// }
    /// </example>
    public class UpdateLicenseRequest
    {
        /// <summary>License details as JSON string</summary>
        /// <example>{"licenseKey":"ABC-123","plan":"premium","expiryDate":"2025-12-31"}</example>
        [System.ComponentModel.DataAnnotations.Required]
        public string LicenseDetailsJson { get; set; } = null!;
    }

    /// <summary>
    /// Update payment details request model
    /// </summary>
    /// <example>
    /// {
    ///   "PaymentDetailsJson": "{\"cardType\":\"Visa\",\"last4\":\"1234\",\"expiryMonth\":12,\"expiryYear\":2026}"
    /// }
    /// </example>
    public class UpdatePaymentRequest
    {
        /// <summary>Payment details as JSON string</summary>
        /// <example>{"cardType":"Visa","last4":"1234","expiryMonth":12,"expiryYear":2026}</example>
        [System.ComponentModel.DataAnnotations.Required]
        public string PaymentDetailsJson { get; set; } = null!;
    }

    /// <summary>
    /// Assign role request model
    /// </summary>
    /// <example>
    /// {
    ///   "RoleName": "Manager"
    /// }
    /// </example>
    public class AssignUserRoleRequest
    {
        /// <summary>Name of the role to assign</summary>
        /// <example>Manager</example>
        [System.ComponentModel.DataAnnotations.Required]
        public string RoleName { get; set; } = null!;
    }

    #endregion
}