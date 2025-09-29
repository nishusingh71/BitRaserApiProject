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
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DynamicUserController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserDataService _userDataService;
        private readonly ILogger<DynamicUserController> _logger;

        public DynamicUserController(
            ApplicationDbContext context,
            IUserDataService userDataService,
            ILogger<DynamicUserController> logger)
        {
            _context = context;
            _userDataService = userDataService;
            _logger = logger;
        }

        #region User Profile Operations

        [HttpGet("profile")]
        public async Task<IActionResult> GetMyProfile()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _userDataService.GetUserByEmailAsync(email);
            if (user == null)
                return NotFound("User not found");

            var userInfo = new
            {
                user.user_email,
                user.user_name,
                user.phone_number,
                roles = await _userDataService.GetUserRoleNamesAsync(email, false),
                permissions = await _userDataService.GetUserPermissionsAsync(email, false),
                subusers = (await _userDataService.GetSubusersByParentEmailAsync(email)).Select(s => s.subuser_email)
            };

            return Ok(userInfo);
        }

        [HttpPut("profile")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileRequest request)
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var user = await _userDataService.GetUserByEmailAsync(email);
            if (user == null)
                return NotFound("User not found");

            user.user_name = request.UserName ?? user.user_name;
            user.phone_number = request.PhoneNumber ?? user.phone_number;

            _context.Entry(user).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Profile updated successfully" });
        }

        #endregion

        #region Subuser Management

        [HttpGet("subusers")]
        public async Task<IActionResult> GetMySubusers()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var subusers = await _userDataService.GetSubusersByParentEmailAsync(email);
            
            var subuserInfo = new List<object>();
            foreach (var subuser in subusers)
            {
                subuserInfo.Add(new
                {
                    subuser.subuser_email,
                    roles = await _userDataService.GetUserRoleNamesAsync(subuser.subuser_email, true),
                    permissions = await _userDataService.GetUserPermissionsAsync(subuser.subuser_email, true)
                });
            }

            return Ok(subuserInfo);
        }

        [HttpPost("subusers")]
        public async Task<IActionResult> CreateSubuser([FromBody] CreateSubuserRequest request)
        {
            var parentEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(parentEmail))
                return Unauthorized();

            // Validate email format
            if (!await _userDataService.ValidateEmailFormatAsync(request.SubuserEmail))
                return BadRequest("Invalid email format");

            // Check if subuser already exists
            if (await _userDataService.SubuserExistsAsync(request.SubuserEmail))
                return Conflict("Subuser email already exists");

            // Check if requester has permission to create subusers
            if (!await _userDataService.HasPermissionAsync(parentEmail, "UserManagement"))
                return StatusCode(403,new { error = "You don't have permission to create subusers" });

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

            // Assign default role if specified
            if (!string.IsNullOrEmpty(request.DefaultRole))
            {
                await _userDataService.AssignRoleByEmailAsync(request.SubuserEmail, request.DefaultRole, parentEmail, true);
            }

            return Ok(new { message = "Subuser created successfully", email = request.SubuserEmail });
        }

        [HttpPut("subusers/{subuserEmail}/roles")]
        public async Task<IActionResult> ManageSubuserRoles(string subuserEmail, [FromBody] ManageRolesRequest request)
        {
            var parentEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(parentEmail))
                return Unauthorized();

            // Check if this subuser belongs to the current user
            if (!await _userDataService.IsSubuserOfUserAsync(subuserEmail, parentEmail))
				StatusCode(403, new { error = "You don't have permission to create subusers" });

			// Check permission
			if (!await _userDataService.HasPermissionAsync(parentEmail, "UserManagement"))
				StatusCode(403, new { error = "You don't have permission to create subusers" });

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

            return Ok(new { message = "Role operations completed", results });
        }

        [HttpDelete("subusers/{subuserEmail}")]
        public async Task<IActionResult> DeleteSubuser(string subuserEmail)
        {
            var parentEmail = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(parentEmail))
                return Unauthorized();

            // Check if this subuser belongs to the current user
            if (!await _userDataService.IsSubuserOfUserAsync(subuserEmail, parentEmail))
				StatusCode(403, new { error = "You don't have permission to create subusers" });

			var subuser = await _userDataService.GetSubuserByEmailAsync(subuserEmail);
            if (subuser == null)
                return NotFound("Subuser not found");

            _context.subuser.Remove(subuser);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Subuser deleted successfully" });
        }

        #endregion

        #region Data Access (User-Scoped)

        [HttpGet("machines")]
        public async Task<IActionResult> GetMyMachines()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var machines = await _userDataService.GetMachinesByUserEmailAsync(email);
            return Ok(machines);
        }

        [HttpGet("reports")]
        public async Task<IActionResult> GetMyReports()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var reports = await _userDataService.GetAuditReportsByEmailAsync(email);
            return Ok(reports);
        }

        [HttpGet("sessions")]
        public async Task<IActionResult> GetMySessions()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var sessions = await _userDataService.GetSessionsByEmailAsync(email);
            return Ok(sessions);
        }

        [HttpGet("logs")]
        public async Task<IActionResult> GetMyLogs()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var logs = await _userDataService.GetLogsByEmailAsync(email);
            return Ok(logs);
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

            return Ok(new
            {
                email,
                userType = isSubuser ? "subuser" : "user",
                roles,
                permissions
            });
        }

        [HttpGet("available-roles")]
        public async Task<IActionResult> GetAvailableRoles()
        {
            var email = GetCurrentUserEmail();
            if (string.IsNullOrEmpty(email))
                return Unauthorized();

            var availableRoles = await _userDataService.GetAvailableRolesForUserAsync(email);
            return Ok(availableRoles.Select(r => new { r.RoleId, r.RoleName, r.Description, r.HierarchyLevel }));
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
                operation = request.Operation,
                resourceOwner = request.ResourceOwner,
                hasAccess,
                userType = isSubuser ? "subuser" : "user"
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