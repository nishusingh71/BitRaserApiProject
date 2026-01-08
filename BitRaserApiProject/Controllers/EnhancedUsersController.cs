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
        private readonly ICacheService _cacheService;
        private readonly ILogger<EnhancedUsersController> _logger;

        public EnhancedUsersController(
            ApplicationDbContext context,
            IRoleBasedAuthService authService,
            IUserDataService userDataService,
            ICacheService cacheService,
            ILogger<EnhancedUsersController> logger)
        {
            _context = context;
            _authService = authService;
            _userDataService = userDataService;
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Get all users with role-based filtering (email-based operations)
        /// </summary>
        [HttpGet]
        [RequirePermission("READ_ALL_USERS")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers([FromQuery] UserFilterRequest? filter)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            IQueryable<users> query = _context.Users.AsNoTracking();

            // ✅ HIERARCHICAL FILTERING: Apply role-based access control
            if (await _authService.IsSuperAdminAsync(currentUserEmail!, false))
            {
                // SuperAdmin sees all users - no filtering
            }
            else if (await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_USERS"))
            {
                // Has READ_ALL_USERS permission - can see manageable users
                var managedUserEmails = await _authService.GetManagedUserEmailsAsync(currentUserEmail!);

                // Filter to only show users at lower hierarchy level
                query = query.Where(u => managedUserEmails.Contains(u.user_email) || u.user_email == currentUserEmail);
            }
            else if (await _authService.HasPermissionAsync(currentUserEmail!, "READ_MANAGED_USERS"))
            {
                // Get users this person can manage
                var managedUserEmails = await _authService.GetManagedUserEmailsAsync(currentUserEmail!);
                query = query.Where(u => managedUserEmails.Contains(u.user_email));
            }
            else
            {
                // Only show own profile
                query = query.Where(u => u.user_email == currentUserEmail);
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

                // NEW FIELD FILTERS
                if (!string.IsNullOrEmpty(filter.Department))
                    query = query.Where(u => u.department != null && u.department.Contains(filter.Department));

                if (!string.IsNullOrEmpty(filter.UserGroup))
                    query = query.Where(u => u.user_group != null && u.user_group.Contains(filter.UserGroup));

                if (!string.IsNullOrEmpty(filter.UserRole))
                    query = query.Where(u => u.user_role != null && u.user_role.Contains(filter.UserRole));

                if (!string.IsNullOrEmpty(filter.Status))
                    query = query.Where(u => u.status == filter.Status);

                if (filter.MinLicenseAllocation.HasValue)
                    query = query.Where(u => u.license_allocation >= filter.MinLicenseAllocation.Value);

                if (filter.MaxLicenseAllocation.HasValue)
                    query = query.Where(u => u.license_allocation <= filter.MaxLicenseAllocation.Value);

                if (filter.LastLoginFrom.HasValue)
                    query = query.Where(u => u.last_login >= filter.LastLoginFrom.Value);

                if (filter.LastLoginTo.HasValue)
                    query = query.Where(u => u.last_login <= filter.LastLoginTo.Value);

                // EXISTING FILTERS
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
                .AsSplitQuery()  // ✅ RENDER OPTIMIZATION: Prevent cartesian explosion
                .OrderByDescending(u => u.created_at)
                .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                .Take(filter?.PageSize ?? 100)
         .Select(u => new
         {
             userEmail = u.user_email,
             userName = u.user_name,
             phoneNumber = u.phone_number,
             // NEW FIELDS
             department = u.department,
             userGroup = u.user_group,
             lastLogin = u.last_login,
             userRole = u.user_role,
             licenseAllocation = u.license_allocation,
             status = u.status,
             // EXISTING FIELDS
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
        [DecodeEmail]
        [RequirePermission("READ_USER")]
        public async Task<ActionResult<object>> GetUser(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var user = await _context.Users
                .AsNoTracking()  // ✅ RENDER OPTIMIZATION: Read-only query
                .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
                .ThenInclude(r => r.RolePermissions)
                .ThenInclude(rp => rp.Permission)
                .AsSplitQuery()  // ✅ Prevent cartesian explosion with multiple Includes
                .Where(u => u.user_email == email)
                .FirstOrDefaultAsync();

            if (user == null) return NotFound($"User with email {email} not found");

            // Check if user can view this profile
            if (email != currentUserEmail && !await CanManageUserAsync(currentUserEmail!, email))
            {
                return StatusCode(403, new { error = "You can only view your own profile or profiles you manage" });
            }

            var userDetails = new
            {
                userEmail = user.user_email,
                userName = user.user_name,
                phoneNumber = user.phone_number,
                // NEW FIELDS
                department = user.department,
                userGroup = user.user_group,
                lastLogin = user.last_login,
                userRole = user.user_role,
                licenseAllocation = user.license_allocation,
                status = user.status,
                // EXISTING FIELDS
                createdAt = user.created_at,
                updatedAt = user.updated_at,
                roles = user.UserRoles.Select(ur => new
                {
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
                return StatusCode(403, new { error = "Insufficient permissions to create users" });

            // Validate input
            if (string.IsNullOrEmpty(request.UserEmail) || string.IsNullOrEmpty(request.UserName) || string.IsNullOrEmpty(request.Password))
                return BadRequest("User email, name, and password are required");

            // Check if user already exists
            var existingUser = await _context.Users.AsNoTracking().Where(u => u.user_email == request.UserEmail).FirstOrDefaultAsync();
            if (existingUser != null)
                return Conflict($"User with email {request.UserEmail} already exists");

            // ✅ VALIDATE: Role assignment hierarchy
            var roleToAssign = request.DefaultRole ?? "SuperAdmin";

            // ✅ CHECK: Can current user assign this role?
            if (!await _authService.CanAssignRoleAsync(currentUserEmail!, roleToAssign))
            {
                return StatusCode(403, new
                {
                    success = false,
                    message = $"You cannot create user with role '{roleToAssign}'",
                    detail = "You can only assign roles with lower privilege than your own. Admins cannot create SuperAdmin users."
                });
            }

            // Create user with both plain text and hashed password
            var newUser = new users
            {
                user_email = request.UserEmail,
                user_name = request.UserName,
                user_password = request.Password,  // Plain text
                hash_password = BCrypt.Net.BCrypt.HashPassword(request.Password),  // Hashed
                phone_number = request.PhoneNumber ?? "",
                // NEW FIELDS - All optional, use provided values or null
                department = request.Department,
                user_group = request.UserGroup,
                last_login = null, // Will be set on first login
                user_role = request.UserRole,
                license_allocation = request.LicenseAllocation,
                status = request.Status ?? "active", // Default to "active" if not provided
                                                     // EXISTING FIELDS
                payment_details_json = request.PaymentDetailsJson ?? "{}",
                license_details_json = request.LicenseDetailsJson ?? "{}",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // ✅ CACHE INVALIDATION: Clear user list cache on create
            _cacheService.RemoveByPrefix(CacheService.CacheKeys.UserList);
            _cacheService.RemoveByPrefix(CacheService.CacheKeys.User);

            // ✅ Assign role (already validated above)
            var roleAssigned = await AssignRoleToUserAsync(request.UserEmail, roleToAssign, currentUserEmail!);

            var response = new
            {
                userEmail = newUser.user_email,
                userName = newUser.user_name,
                // NEW FIELDS
                department = newUser.department,
                userGroup = newUser.user_group,
                userRole = newUser.user_role,
                licenseAllocation = newUser.license_allocation,
                status = newUser.status,
                // EXISTING FIELDS
                createdAt = newUser.created_at,
                assignedRole = roleToAssign,
                roleAssignedToRBAC = roleAssigned,
                message = roleAssigned ? $"User created successfully with {roleToAssign} role assigned to RBAC" : $"User created successfully ({roleToAssign} role assignment to RBAC failed)"
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
            var existingUser = await _context.Users.AsNoTracking().Where(u => u.user_email == request.UserEmail).FirstOrDefaultAsync();
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
                // NEW FIELDS - Safe defaults for public registration (all optional)
                department = null, // Can be set later by admin
                user_group = null, // Can be assigned later by admin
                last_login = null, // Will be set on first login
                user_role = null, // Can be assigned later (no default role for public)
                license_allocation = null, // No licenses allocated initially
                status = "pending", // Pending verification for public registrations
                // EXISTING FIELDS
                payment_details_json = "{}",
                license_details_json = "{}",
                created_at = DateTime.UtcNow,
                updated_at = DateTime.UtcNow
            };

            _context.Users.Add(newUser);
            await _context.SaveChangesAsync();

            // ✅ CACHE INVALIDATION: Clear user list cache on register
            _cacheService.RemoveByPrefix(CacheService.CacheKeys.UserList);

            // ✅ Auto-assign User role for public registration to rolebasedAuth system
            var roleAssigned = await AssignRoleToUserAsync(request.UserEmail, "User", "system");

            var response = new
            {
                userEmail = newUser.user_email,
                userName = newUser.user_name,
                createdAt = newUser.created_at,
                assignedRole = "User",
                roleAssignedToRBAC = roleAssigned,
                message = roleAssigned ? "User registered successfully with default role" : "User registered successfully (role assignment to RBAC failed)"
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
        [DecodeEmail]
        [RequirePermission("UPDATE_USER")]
        public async Task<IActionResult> UpdateUser(string email, [FromBody] UserUpdateRequest request)
        {
            if (email != request.UserEmail)
                return BadRequest("Email mismatch in request");

            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.Where(u => u.user_email == email).FirstOrDefaultAsync();

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

            // UPDATE NEW FIELDS
            if (request.Department != null)
                user.department = request.Department;

            if (request.UserGroup != null)
                user.user_group = request.UserGroup;

            if (request.UserRole != null)
                user.user_role = request.UserRole;

            if (request.LicenseAllocation.HasValue)
                user.license_allocation = request.LicenseAllocation.Value;

            if (request.Status != null)
                user.status = request.Status;

            // UPDATE EXISTING FIELDS
            if (!string.IsNullOrEmpty(request.PaymentDetailsJson))
                user.payment_details_json = request.PaymentDetailsJson;

            if (!string.IsNullOrEmpty(request.LicenseDetailsJson))
                user.license_details_json = request.LicenseDetailsJson;

            user.updated_at = DateTime.UtcNow;

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            // ✅ CACHE INVALIDATION: Clear user cache on update
            _cacheService.Remove($"{CacheService.CacheKeys.User}:{email}");
            _cacheService.RemoveByPrefix(CacheService.CacheKeys.UserList);

            return Ok(new
            {
                message = "User updated successfully",
                userEmail = email,
                updatedAt = user.updated_at
            });
        }

        /// <summary>
        /// Change user password by email
        /// </summary>
        [HttpPatch("{email}/change-password")]
        [DecodeEmail]
        public async Task<IActionResult> ChangePassword(string email, [FromBody] ChangeUserPasswordRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.Where(u => u.user_email == email).FirstOrDefaultAsync();
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
                    return StatusCode(500, new
                    {
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

                return Ok(new
                {
                    message = "Password changed successfully",
                    userEmail = email,
                    updatedAt = user.updated_at,
                    passwordUpdated = true,
                    hashUpdated = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error changing password",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update user license details by email
        /// </summary>
        [HttpPatch("{email}/update-license")]
        [DecodeEmail]
        public async Task<IActionResult> UpdateLicense(string email, [FromBody] UpdateLicenseRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.Where(u => u.user_email == email).FirstOrDefaultAsync();
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

                return Ok(new
                {
                    message = "License details updated successfully",
                    userEmail = email,
                    updatedAt = user.updated_at
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error updating license details",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Update user payment details by email
        /// </summary>
        [HttpPatch("{email}/update-payment")]
        [DecodeEmail]
        public async Task<IActionResult> UpdatePayment(string email, [FromBody] UpdatePaymentRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(currentUserEmail))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var user = await _context.Users.Where(u => u.user_email == email).FirstOrDefaultAsync();
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

                return Ok(new
                {
                    message = "Payment details updated successfully",
                    userEmail = email,
                    updatedAt = user.updated_at
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Error updating payment details",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Assign role to user by email - Admin access
        /// </summary>
        [HttpPost("{email}/assign-role")]
        [DecodeEmail]
        [RequirePermission("ASSIGN_ROLES")]
        public async Task<IActionResult> AssignRole(string email, [FromBody] AssignUserRoleRequest request)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (!await _authService.HasPermissionAsync(currentUserEmail!, "ASSIGN_ROLES"))
                return StatusCode(403, new { error = "Insufficient permissions to assign roles" });

            var user = await _context.Users.Where(u => u.user_email == email).FirstOrDefaultAsync();
            if (user == null) return NotFound($"User with email {email} not found");

            await AssignRoleToUserAsync(email, request.RoleName, currentUserEmail!);

            return Ok(new
            {
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
        [DecodeEmail]
        [RequirePermission("ASSIGN_ROLES")]
        public async Task<IActionResult> RemoveRole(string email, string roleName)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var user = await _context.Users.AsNoTracking().Where(u => u.user_email == email).FirstOrDefaultAsync();
            if (user == null) return NotFound($"User with email {email} not found");

            var role = await _context.Roles.AsNoTracking().Where(r => r.RoleName == roleName).FirstOrDefaultAsync();
            if (role == null) return NotFound($"Role {roleName} not found");

            var userRole = await _context.UserRoles
                .Where(ur => ur.UserId == user.user_id && ur.RoleId == role.RoleId).FirstOrDefaultAsync();

            if (userRole == null)
                return NotFound($"Role {roleName} not assigned to user {email}");

            _context.UserRoles.Remove(userRole);
            await _context.SaveChangesAsync();

            return Ok(new
            {
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
        [DecodeEmail]
        [RequirePermission("DELETE_USER")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var user = await _context.Users.Where(u => u.user_email == email).FirstOrDefaultAsync();

            if (user == null) return NotFound($"User with email {email} not found");

            // Check if user can delete this profile
            if (!await _authService.HasPermissionAsync(currentUserEmail!, "DELETE_USER"))
                return StatusCode(403, new { error = "Insufficient permissions to delete users" });

            // Don't allow users to delete themselves
            if (email == currentUserEmail)
                return BadRequest("You cannot delete your own account");

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            // ✅ CACHE INVALIDATION: Clear user cache on delete
            _cacheService.Remove($"{CacheService.CacheKeys.User}:{email}");
            _cacheService.RemoveByPrefix(CacheService.CacheKeys.UserList);
            _cacheService.RemoveByPrefix($"{CacheService.CacheKeys.Subuser}:{email}");

            return Ok(new
            {
                message = $"User {email} deleted successfully",
                deletedAt = DateTime.UtcNow,
                deletedBy = currentUserEmail
            });
        }

        /// <summary>
        /// Get user statistics by email
        /// </summary>
        [HttpGet("{email}/statistics")]
        [DecodeEmail]
        [RequirePermission("READ_USER_STATISTICS")]
        public async Task<ActionResult<object>> GetUserStatistics(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // Check if user can view statistics for this email
            if (email != currentUserEmail && !await CanManageUserAsync(currentUserEmail!, email))
            {
                return StatusCode(403, new { error = "You can only view statistics for your own account or accounts you manage" });
            }

            var user = await _context.Users.AsNoTracking().Where(u => u.user_email == email).FirstOrDefaultAsync();
            if (user == null) return NotFound($"User with email {email} not found");

            // ✅ RENDER OPTIMIZATION: Use parallel execution for independent counts
            var stats = new
            {
                UserEmail = email,
                UserName = user.user_name,
                AccountAge = DateTime.UtcNow - user.created_at,
                TotalMachines = await _context.Machines.AsNoTracking().CountAsync(m => m.user_email == email),
                ActiveLicenses = await _context.Machines.AsNoTracking()
                    .CountAsync(m => m.user_email == email && m.license_activated),
                TotalReports = await _context.AuditReports.AsNoTracking().CountAsync(r => r.client_email == email),
                TotalSessions = await _context.Sessions.AsNoTracking().CountAsync(s => s.user_email == email),
                ActiveSessions = await _context.Sessions.AsNoTracking()
                    .CountAsync(s => s.user_email == email && s.session_status == "active"),
                TotalSubusers = await _context.subuser.AsNoTracking().CountAsync(s => s.user_email == email),
                TotalLogs = await _context.logs.AsNoTracking().CountAsync(l => l.user_email == email),
                LastActivity = await GetLastActivityAsync(email),
                RoleHistory = await _context.UserRoles.AsNoTracking()
                    .Where(ur => ur.User.user_email == email)
                    .Include(ur => ur.Role)
                    .Select(ur => new
                    {
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

        private async Task<bool> AssignRoleToUserAsync(string userEmail, string roleName, string assignedByEmail)
        {
            try
            {
                var user = await _context.Users.Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
                var role = await _context.Roles.Where(r => r.RoleName == roleName).FirstOrDefaultAsync();

                if (user != null && role != null)
                {
                    var existingRole = await _context.UserRoles
                .Where(ur => ur.UserId == user.user_id && ur.RoleId == role.RoleId).FirstOrDefaultAsync();

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
                    return true; // Role assigned successfully or already exists
                }

                return false; // User or role not found
            }
            catch (Exception)
            {
                return false; // Assignment failed
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

        #region User Transfer Endpoints

        /// <summary>
        /// GET: Get user dependencies before deletion/transfer
        /// Shows all related data: subusers, machines, licenses, reports
        /// </summary>
        [HttpGet("{userEmail}/dependencies")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetUserDependencies(string userEmail)
        {
            try
            {
                var decodedEmail = DecodeEmail(userEmail);
                
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.user_email == decodedEmail);
                
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                // Count all dependencies
                var subusersCount = await _context.Set<subuser>()
                    .CountAsync(s => s.user_email == decodedEmail);
                var machinesCount = await _context.Machines
                    .CountAsync(m => m.user_email == decodedEmail);
                var reportsCount = await _context.AuditReports
                    .CountAsync(r => r.client_email == decodedEmail);
                var sessionsCount = await _context.Sessions
                    .CountAsync(s => s.user_email == decodedEmail);

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        userEmail = decodedEmail,
                        dependencies = new
                        {
                            subusers = subusersCount,
                            machines = machinesCount,
                            reports = reportsCount,
                            sessions = sessionsCount,
                            total = subusersCount + machinesCount + reportsCount + sessionsCount
                        },
                        canDelete = subusersCount == 0 && machinesCount == 0,
                        requiresTransfer = subusersCount > 0 || machinesCount > 0
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting dependencies for {Email}", userEmail);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// GET: Get current transfer status for a user
        /// </summary>
        [HttpGet("{userEmail}/transfer-status")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> GetTransferStatus(string userEmail)
        {
            try
            {
                var decodedEmail = DecodeEmail(userEmail);
                
                // Check if there's any pending transfer for this user
                var pendingTransfer = await _context.Sessions
                    .Where(s => s.user_email == decodedEmail && s.session_status == "transfer_pending")
                    .OrderByDescending(s => s.login_time)
                    .FirstOrDefaultAsync();

                if (pendingTransfer != null)
                {
                    return Ok(new
                    {
                        success = true,
                        data = new
                        {
                            status = "pending",
                            transferId = pendingTransfer.session_id,
                            startedAt = pendingTransfer.login_time,
                            message = "Transfer in progress"
                        }
                    });
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        status = "none",
                        message = "No pending transfers"
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error getting transfer status for {Email}", userEmail);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: Transfer user access (roles, permissions) to another user
        /// </summary>
        [HttpPost("{userEmail}/transfer-access")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> TransferAccess(string userEmail, [FromBody] TransferAccessRequest request)
        {
            try
            {
                var sourceEmail = DecodeEmail(userEmail);
                
                var sourceUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == sourceEmail);
                var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == request.TargetUserEmail);

                if (sourceUser == null)
                    return NotFound(new { success = false, message = "Source user not found" });
                if (targetUser == null)
                    return NotFound(new { success = false, message = "Target user not found" });

                // Transfer subusers to new parent
                var subusers = await _context.Set<subuser>()
                    .Where(s => s.user_email == sourceEmail)
                    .ToListAsync();

                foreach (var sub in subusers)
                {
                    sub.user_email = request.TargetUserEmail;
                }

                // Log the transfer
                var transferSession = new Sessions
                {
                    user_email = sourceEmail,
                    session_status = "access_transferred",
                    device_info = $"Access transferred to: {request.TargetUserEmail}",
                    ip_address = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    login_time = DateTime.UtcNow
                };
                _context.Sessions.Add(transferSession);

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Access transferred from {Source} to {Target}: {Count} subusers",
                    sourceEmail, request.TargetUserEmail, subusers.Count);

                return Ok(new
                {
                    success = true,
                    message = "Access transferred successfully",
                    data = new
                    {
                        subusersTransferred = subusers.Count,
                        fromUser = sourceEmail,
                        toUser = request.TargetUserEmail
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error transferring access for {Email}", userEmail);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// POST: Transfer user data (machines, reports) to another user
        /// </summary>
        [HttpPost("{userEmail}/transfer-data")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> TransferData(string userEmail, [FromBody] TransferDataRequest request)
        {
            try
            {
                var sourceEmail = DecodeEmail(userEmail);
                
                var sourceUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == sourceEmail);
                var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.user_email == request.TargetUserEmail);

                if (sourceUser == null)
                    return NotFound(new { success = false, message = "Source user not found" });
                if (targetUser == null)
                    return NotFound(new { success = false, message = "Target user not found" });

                int machinesTransferred = 0;
                int reportsTransferred = 0;

                // Transfer machines if requested
                if (request.TransferMachines)
                {
                    var machines = await _context.Machines
                        .Where(m => m.user_email == sourceEmail)
                        .ToListAsync();

                    foreach (var machine in machines)
                    {
                        machine.user_email = request.TargetUserEmail;
                    }
                    machinesTransferred = machines.Count;
                }

                // Transfer reports if requested
                if (request.TransferReports)
                {
                    var reports = await _context.AuditReports
                        .Where(r => r.client_email == sourceEmail)
                        .ToListAsync();

                    foreach (var report in reports)
                    {
                        report.client_email = request.TargetUserEmail;
                    }
                    reportsTransferred = reports.Count;
                }

                // Log the transfer
                var transferSession = new Sessions
                {
                    user_email = sourceEmail,
                    session_status = "data_transferred",
                    device_info = $"Data transferred to: {request.TargetUserEmail}",
                    ip_address = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    login_time = DateTime.UtcNow
                };
                _context.Sessions.Add(transferSession);

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Data transferred from {Source} to {Target}: {Machines} machines, {Reports} reports",
                    sourceEmail, request.TargetUserEmail, machinesTransferred, reportsTransferred);

                return Ok(new
                {
                    success = true,
                    message = "Data transferred successfully",
                    data = new
                    {
                        machinesTransferred,
                        reportsTransferred,
                        fromUser = sourceEmail,
                        toUser = request.TargetUserEmail
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error transferring data for {Email}", userEmail);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// PUT: Update user status (active/inactive/suspended)
        /// </summary>
        [HttpPut("{userEmail}/status")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        public async Task<IActionResult> UpdateUserStatus(string userEmail, [FromBody] UpdateStatusRequest request)
        {
            try
            {
                var decodedEmail = DecodeEmail(userEmail);
                
                var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == decodedEmail);
                if (user == null)
                    return NotFound(new { success = false, message = "User not found" });

                var validStatuses = new[] { "active", "inactive", "suspended" };
                if (!validStatuses.Contains(request.Status?.ToLower()))
                    return BadRequest(new { success = false, message = "Invalid status. Use: active, inactive, suspended" });

                var oldStatus = user.status;
                user.status = request.Status.ToLower();
                user.updated_at = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ User status updated: {Email} from {Old} to {New}",
                    decodedEmail, oldStatus, user.status);

                return Ok(new
                {
                    success = true,
                    message = "User status updated successfully",
                    data = new
                    {
                        userEmail = decodedEmail,
                        previousStatus = oldStatus,
                        currentStatus = user.status
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error updating status for {Email}", userEmail);
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        private static string DecodeEmail(string base64Email)
        {
            try
            {
                var bytes = Convert.FromBase64String(base64Email);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return base64Email; // Return as-is if not valid base64
            }
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

        /// <summary>Filter by department (partial match)</summary>
        /// <example>Engineering</example>
        public string? Department { get; set; }

        /// <summary>Filter by user group (partial match)</summary>
        /// <example>Development Team</example>
        public string? UserGroup { get; set; }

        /// <summary>Filter by user role (partial match)</summary>
        /// <example>manager</example>
        public string? UserRole { get; set; }

        /// <summary>Filter by status (exact match)</summary>
        /// <example>active</example>
        public string? Status { get; set; }

        /// <summary>Minimum license allocation</summary>
        /// <example>1</example>
        public int? MinLicenseAllocation { get; set; }

        /// <summary>Maximum license allocation</summary>
        /// <example>100</example>
        public int? MaxLicenseAllocation { get; set; }

        /// <summary>Filter users who logged in from this date</summary>
        /// <example>2024-01-01T00:00:00Z</example>
        public DateTime? LastLoginFrom { get; set; }

        /// <summary>Filter users who logged in until this date</summary>
        /// <example>2024-12-31T23:59:59Z</example>
        public DateTime? LastLoginTo { get; set; }

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

        /// <summary>Department name (optional)</summary>
        /// <example>Engineering</example>
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? Department { get; set; }

        /// <summary>User group (optional)</summary>
        /// <example>Development Team</example>
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? UserGroup { get; set; }

        /// <summary>User role (optional, defaults to 'user')</summary>
        /// <example>manager</example>
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? UserRole { get; set; }

        /// <summary>License allocation (optional, defaults to 0)</summary>
        /// <example>5</example>
        public int? LicenseAllocation { get; set; }

        /// <summary>User status (optional, defaults to 'active')</summary>
        /// <example>active</example>
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Status { get; set; }

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

        /// <summary>Department (optional)</summary>
        /// <example>IT Operations</example>
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? Department { get; set; }

        /// <summary>User group (optional)</summary>
        /// <example>Senior Team</example>
        [System.ComponentModel.DataAnnotations.MaxLength(100)]
        public string? UserGroup { get; set; }

        /// <summary>User role (optional)</summary>
        /// <example>admin</expert>
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? UserRole { get; set; }

        /// <summary>License allocation (optional)</summary>
        /// <example>10</example>
        public int? LicenseAllocation { get; set; }

        /// <summary>User status (optional)</summary>
        /// <example>active</example>
        [System.ComponentModel.DataAnnotations.MaxLength(50)]
        public string? Status { get; set; }

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

    /// <summary>
    /// Transfer access request - transfers subusers to another user
    /// </summary>
    public class TransferAccessRequest
    {
        /// <summary>Email of the user to transfer access to</summary>
        [System.ComponentModel.DataAnnotations.Required]
        public string TargetUserEmail { get; set; } = null!;
        
        /// <summary>Optional reason for transfer</summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Transfer data request - transfers machines and reports to another user
    /// </summary>
    public class TransferDataRequest
    {
        /// <summary>Email of the user to transfer data to</summary>
        [System.ComponentModel.DataAnnotations.Required]
        public string TargetUserEmail { get; set; } = null!;
        
        /// <summary>Whether to transfer machines</summary>
        public bool TransferMachines { get; set; } = true;
        
        /// <summary>Whether to transfer reports</summary>
        public bool TransferReports { get; set; } = true;
        
        /// <summary>Optional reason for transfer</summary>
        public string? Reason { get; set; }
    }

    /// <summary>
    /// Update user status request
    /// </summary>
    public class UpdateStatusRequest
    {
        /// <summary>New status: active, inactive, suspended</summary>
        [System.ComponentModel.DataAnnotations.Required]
        public string Status { get; set; } = null!;
        
        /// <summary>Optional reason for status change</summary>
        public string? Reason { get; set; }
    }

    #endregion
}