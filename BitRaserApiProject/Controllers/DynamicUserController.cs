using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BitRaserApiProject.Services;
using BitRaserApiProject.Models;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Dynamic User Management Controller - All operations are email-based
    /// No hardcoded IDs required for any operations
    /// Supports both users and subusers with appropriate access levels
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicUserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserDataService _userDataService;
        private readonly IRoleBasedAuthService _authService;
        private readonly ILogger<DynamicUserController> _logger;

        public DynamicUserController(
            ApplicationDbContext context,
            IUserDataService userDataService,
            IRoleBasedAuthService authService,
            ILogger<DynamicUserController> logger)
        {
            _context = context;
            _userDataService = userDataService;
            _authService = authService;
            _logger = logger;
        }

        #region User/Subuser Profile Operations

        [HttpGet("profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);

            if (isSubuser)
            {
                var subuser = await _userDataService.GetSubuserByEmailAsync(email);
                if (subuser == null)
                    return NotFound("Subuser not found");

                var subuserInfo = new
                {
                    subuser_email = subuser.subuser_email,
                    parent_user_email = subuser.user_email,
                    subuser_id = subuser.subuser_id,
                    user_type = "Subuser",
                    roles = await _userDataService.GetUserRoleNamesAsync(email, true),
                    permissions = await _userDataService.GetUserPermissionsAsync(email, true),
                    parent_subusers = new List<string>(), // Subusers cannot have subusers
                    can_create_subusers = false
                };

                return Ok(subuserInfo);
            }
            else
            {
                var user = await _userDataService.GetUserByEmailAsync(email);
                if (user == null)
                    return NotFound("User not found");

                var userInfo = new
                {
                    user.user_email,
                    user.user_name,
                    user.phone_number,
                    user_type = "User",
                    roles = await _userDataService.GetUserRoleNamesAsync(email, false),
                    permissions = await _userDataService.GetUserPermissionsAsync(email, false),
                    subusers = (await _userDataService.GetSubusersByParentEmailAsync(email)).Select(s => s.subuser_email),
                    can_create_subusers = await _userDataService.HasPermissionAsync(email, "UserManagement")
                };

                return Ok(userInfo);
            }
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);

            if (isSubuser)
            {
                // Subusers have limited profile update options
                return Ok(new { 
                    message = "Subuser profile updates are limited. Contact your parent user for changes.",
                    user_type = "Subuser",
                    subuser_email = email,
                    note = "Use Enhanced Subuser Controller for password changes or contact parent user for other updates"
                });
            }
            else
            {
                var user = await _userDataService.GetUserByEmailAsync(email);
                if (user == null)
                    return NotFound("User not found");

                user.user_name = request.UserName ?? user.user_name;
                user.phone_number = request.PhoneNumber ?? user.phone_number;

                _context.Entry(user).State = EntityState.Modified;
                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "User profile updated successfully",
                    user_type = "User",
                    updated_at = DateTime.UtcNow
                });
            }
        }

        #endregion

        #region Subuser Management (Users Only)

        [HttpGet("subusers")]
        public async Task<IActionResult> GetMySubusers()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);
            if (isSubuser)
            {
                return StatusCode(403, new { error = "Subusers cannot manage other subusers" });
            }

            var subusers = await _userDataService.GetSubusersByParentEmailAsync(email);
            
            var subuserInfo = new List<object>();
            foreach (var subuser in subusers)
            {
                subuserInfo.Add(new
                {
                    subuser.subuser_email,
                    subuser.user_email,
                    subuser.subuser_id,
                    roles = await _userDataService.GetUserRoleNamesAsync(subuser.subuser_email, true),
                    permissions = await _userDataService.GetUserPermissionsAsync(subuser.subuser_email, true),
                    has_password = !string.IsNullOrEmpty(subuser.subuser_password)
                });
            }

            return Ok(new {
                parent_user_email = email,
                total_subusers = subuserInfo.Count,
                subusers = subuserInfo
            });
        }

        [HttpPost("subusers")]
        public async Task<IActionResult> CreateSubuser([FromBody] CreateSubuserRequest request)
        {
            var parentEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(parentEmail))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(parentEmail);
            if (isSubuser)
            {
                return StatusCode(403, new { error = "Subusers cannot create other subusers" });
            }

            // Validate email format
            if (!await _userDataService.ValidateEmailFormatAsync(request.SubuserEmail))
                return BadRequest("Invalid email format");

            // Check if subuser already exists
            if (await _userDataService.SubuserExistsAsync(request.SubuserEmail))
                return Conflict("Subuser email already exists");

            // Check if email is already used as a main user
            if (await _userDataService.UserExistsAsync(request.SubuserEmail))
                return Conflict("Email is already used as a main user account");

            // Get parent user
            var parentUser = await _userDataService.GetUserByEmailAsync(parentEmail);
            if (parentUser == null)
                return BadRequest("Parent user not found");

            // Create subuser
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);
            var newSubuser = new subuser
            {
                subuser_email = request.SubuserEmail,
                subuser_password = hashedPassword,
                user_email = parentEmail,
                superuser_id = parentUser.user_id
            };

            _context.subuser.Add(newSubuser);
            await _context.SaveChangesAsync();

            // Assign default role if specified, otherwise assign "SubUser" role
            var roleToAssign = request.DefaultRole ?? "SubUser";
            await _userDataService.AssignRoleByEmailAsync(request.SubuserEmail, roleToAssign, parentEmail, true);

            return Ok(new { 
                message = "Subuser created successfully", 
                subuser_email = request.SubuserEmail,
                parent_email = parentEmail,
                default_role = roleToAssign,
                created_at = DateTime.UtcNow
            });
        }

        [HttpPut("subusers/{subuserEmail}/roles")]
        public async Task<IActionResult> ManageSubuserRoles(string subuserEmail, [FromBody] ManageRolesRequest request)
        {
            var parentEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(parentEmail))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(parentEmail);
            if (isSubuser)
            {
                return StatusCode(403, new { error = "Subusers cannot manage roles" });
            }

            // Check if this subuser belongs to the current user
            if (!await _userDataService.IsSubuserOfUserAsync(subuserEmail, parentEmail))
                return StatusCode(403, new { error = "You can only manage roles for your own subusers" });

            var results = new List<object>();

            // Add roles
            foreach (var roleName in request.RolesToAdd ?? new List<string>())
            {
                var success = await _userDataService.AssignRoleByEmailAsync(subuserEmail, roleName, parentEmail, true);
                results.Add(new { action = "add", role = roleName, success });
            }

            // Remove roles
            foreach (var roleName in request.RolesToRemove ?? new List<string>())
            {
                var success = await _userDataService.RemoveRoleByEmailAsync(subuserEmail, roleName, true);
                results.Add(new { action = "remove", role = roleName, success });
            }

            return Ok(new { 
                message = "Role operations completed", 
                subuser_email = subuserEmail,
                parent_email = parentEmail,
                results = results,
                timestamp = DateTime.UtcNow
            });
        }

        [HttpDelete("subusers/{subuserEmail}")]
        public async Task<IActionResult> DeleteSubuser(string subuserEmail)
        {
            var parentEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(parentEmail))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(parentEmail);
            if (isSubuser)
            {
                return StatusCode(403, new { error = "Subusers cannot delete other subusers" });
            }

            // Check if this subuser belongs to the current user
            if (!await _userDataService.IsSubuserOfUserAsync(subuserEmail, parentEmail))
                return StatusCode(403, new { error = "You can only delete your own subusers" });

            var subuser = await _userDataService.GetSubuserByEmailAsync(subuserEmail);
            if (subuser == null)
                return NotFound("Subuser not found");

            _context.subuser.Remove(subuser);
            await _context.SaveChangesAsync();

            return Ok(new { 
                message = "Subuser deleted successfully",
                deleted_subuser_email = subuserEmail,
                parent_email = parentEmail,
                deleted_at = DateTime.UtcNow
            });
        }

        #endregion

        #region Data Access (User-Scoped)

        [HttpGet("machines")]
        public async Task<IActionResult> GetMyMachines()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);
            var machines = await _userDataService.GetMachinesByUserEmailAsync(email);

            return Ok(new {
                user_email = email,
                user_type = isSubuser ? "Subuser" : "User",
                total_machines = machines.Count(),
                machines = machines
            });
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetMyReports()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);
            var reports = await _userDataService.GetAuditReportsByEmailAsync(email);

            return Ok(new {
                user_email = email,
                user_type = isSubuser ? "Subuser" : "User",
                total_reports = reports.Count(),
                reports = reports
            });
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetMySessions()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);
            var sessions = await _userDataService.GetSessionsByEmailAsync(email);

            return Ok(new {
                user_email = email,
                user_type = isSubuser ? "Subuser" : "User",
                total_sessions = sessions.Count(),
                sessions = sessions
            });
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetMyLogs()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);
            var logs = await _userDataService.GetLogsByEmailAsync(email);

            return Ok(new {
                user_email = email,
                user_type = isSubuser ? "Subuser" : "User",
                total_logs = logs.Count(),
                logs = logs
            });
        }

        [HttpGet("statistics")]
        public async Task<IActionResult> GetMyStatistics()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);

            var machines = await _userDataService.GetMachinesByUserEmailAsync(email);
            var reports = await _userDataService.GetAuditReportsByEmailAsync(email);
            var sessions = await _userDataService.GetSessionsByEmailAsync(email);
            var logs = await _userDataService.GetLogsByEmailAsync(email);

            var baseStatistics = new
            {
                user_email = email,
                user_type = isSubuser ? "Subuser" : "User",
                summary = new
                {
                    total_machines = machines.Count(),
                    active_machines = machines.Count(m => m.license_activated),
                    total_reports = reports.Count(),
                    synced_reports = reports.Count(r => r.synced),
                    total_sessions = sessions.Count(),
                    active_sessions = sessions.Count(s => s.session_status == "active"),
                    total_logs = logs.Count(),
                    error_logs = logs.Count(l => l.log_level.ToLower().Contains("error"))
                },
                recent_activity = new
                {
                    last_login = sessions.OrderByDescending(s => s.login_time).FirstOrDefault()?.login_time,
                    recent_machines = machines.OrderByDescending(m => m.created_at).Take(5).Select(m => new {
                        m.mac_address,
                        m.os_version,
                        m.created_at,
                        m.license_activated
                    }),
                    recent_reports = reports.OrderByDescending(r => r.report_datetime).Take(5).Select(r => new {
                        r.report_id,
                        r.report_name,
                        r.report_datetime,
                        r.synced
                    })
                }
            };

            if (!isSubuser)
            {
                var subusers = await _userDataService.GetSubusersByParentEmailAsync(email);
                var extendedStatistics = new
                {
                    baseStatistics.user_email,
                    baseStatistics.user_type,
                    baseStatistics.summary,
                    baseStatistics.recent_activity,
                    subuser_management = new
                    {
                        total_subusers = subusers.Count(),
                        subuser_list = subusers.Select(s => new {
                            s.subuser_email,
                            s.subuser_id
                        })
                    }
                };
                return Ok(extendedStatistics);
            }

            return Ok(baseStatistics);
        }

        #endregion

        #region Access Control Information

        [HttpGet("permissions")]
        public async Task<IActionResult> GetMyPermissions()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);
            var permissions = await _userDataService.GetUserPermissionsAsync(email, isSubuser);
            var roles = await _userDataService.GetUserRoleNamesAsync(email, isSubuser);

            var baseResponse = new
            {
                email,
                userType = isSubuser ? "subuser" : "user",
                roles,
                permissions,
                access_info = new
                {
                    can_create_subusers = !isSubuser && await _userDataService.HasPermissionAsync(email, "UserManagement"),
                    can_manage_roles = !isSubuser && await _userDataService.HasPermissionAsync(email, "UserManagement"),
                    can_view_all_users = await _userDataService.HasPermissionAsync(email, "FullAccess", isSubuser),
                    has_admin_access = await _userDataService.HasPermissionAsync(email, "FullAccess", isSubuser)
                }
            };

            if (isSubuser)
            {
                var subuser = await _userDataService.GetSubuserByEmailAsync(email);
                if (subuser != null)
                {
                    var extendedResponse = new
                    {
                        baseResponse.email,
                        baseResponse.userType,
                        baseResponse.roles,
                        baseResponse.permissions,
                        baseResponse.access_info,
                        parent_user_email = subuser.user_email,
                        subuser_id = subuser.subuser_id
                    };
                    return Ok(extendedResponse);
                }
            }

            return Ok(baseResponse);
        }

        [HttpGet("available-roles")]
        public async Task<IActionResult> GetAvailableRoles()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);
            if (isSubuser)
            {
                return StatusCode(403, new { error = "Subusers cannot assign roles" });
            }

            var availableRoles = await _userDataService.GetAvailableRolesForUserAsync(email);
            return Ok(new {
                user_email = email,
                can_assign_roles = await _userDataService.HasPermissionAsync(email, "UserManagement"),
                available_roles = availableRoles.Select(r => new { 
                    r.RoleId, 
                    r.RoleName, 
                    r.Description, 
                    r.HierarchyLevel,
                    can_assign_to_subusers = r.RoleName != "Admin" && r.RoleName != "Manager" // Basic restriction
                })
            });
        }

        [HttpPost("check-access")]
        public async Task<IActionResult> CheckAccess([FromBody] CheckAccessRequest request)
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var isSubuser = await _userDataService.SubuserExistsAsync(email);
            var hasAccess = await _userDataService.IsUserAuthorizedForOperationAsync(email, request.Operation, request.ResourceOwner);

            return Ok(new
            {
                userEmail = email,
                userType = isSubuser ? "subuser" : "user",
                operation = request.Operation,
                resourceOwner = request.ResourceOwner,
                hasAccess,
                checked_at = DateTime.UtcNow
            });
        }

        #endregion

        #region Helper Methods

        private string? GetCurrentUserEmail()
        {
            return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        #endregion

        #region Request Models

        public class UpdateProfileRequest
        {
            public string? UserName { get; set; }
            public string? PhoneNumber { get; set; }
        }

        public class CreateSubuserRequest
        {
            public string SubuserEmail { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string? DefaultRole { get; set; }
        }

        public class ManageRolesRequest
        {
            public List<string>? RolesToAdd { get; set; }
            public List<string>? RolesToRemove { get; set; }
        }

        public class CheckAccessRequest
        {
            public string Operation { get; set; } = string.Empty;
            public string? ResourceOwner { get; set; }
        }

        #endregion
    }
}