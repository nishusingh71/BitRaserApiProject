using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// License Management Controller - Bulk assignment and license operations
    /// Based on BitRaser License Management UI
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class LicenseManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ILogger<LicenseManagementController> _logger;

        public LicenseManagementController(
    ApplicationDbContext context,
              IRoleBasedAuthService authService,
              IUserDataService userDataService,
  ILogger<LicenseManagementController> logger)
        {
            _context = context;
            _authService = authService;
            _userDataService = userDataService;
            _logger = logger;
        }

        /// <summary>
        /// POST /api/LicenseManagement/bulk-assign - Bulk assign licenses to multiple users
        /// Matches "Bulk License Assignment" modal from Screenshot 2
        /// </summary>
        [HttpPost("bulk-assign")]
        public async Task<ActionResult<BulkLicenseAssignmentResponse>> BulkAssignLicenses(
      [FromBody] BulkLicenseAssignmentRequest request)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

                // Check permissions
                if (!await _authService.HasPermissionAsync(userEmail, "MANAGE_ALL_MACHINE_LICENSES", isSubuser) &&
                    !await _authService.HasPermissionAsync(userEmail, "SYSTEM_ADMIN", isSubuser))
                {
                    return StatusCode(403, new { message = "Insufficient permissions to assign licenses" });
                }

                // Calculate total licenses needed
                int totalLicensesNeeded = request.TotalLicensesRequired;

                // Get current license availability
                var licenseSettings = await GetCurrentLicenseSettings();
                if (licenseSettings.AvailableLicenses < totalLicensesNeeded)
                {
                    return BadRequest(new
                    {
                        message = $"Insufficient licenses available. Required: {totalLicensesNeeded}, Available: {licenseSettings.AvailableLicenses}"
                    });
                }

                var response = new BulkLicenseAssignmentResponse
                {
                    AssignedAt = DateTime.UtcNow
                };

                // Get users to assign licenses to
                List<string> targetUsers;
                if (request.UserEmails != null && request.UserEmails.Any())
                {
                    targetUsers = request.UserEmails;
                }
                else if (!string.IsNullOrEmpty(request.GroupId))
                {
                    // Get users from group
                    var groupUsers = await _context.Users
                        .Where(u => u.user_id.ToString() == request.GroupId)
                    .Select(u => u.user_email)
                  .ToListAsync();
                    targetUsers = groupUsers;
                }
                else
                {
                    return BadRequest(new { message = "Please specify either UserEmails or GroupId" });
                }

                // Validate number of users
                if (targetUsers.Count != request.NumberOfUsers)
                {
                    _logger.LogWarning("User count mismatch. Expected: {Expected}, Actual: {Actual}",
                     request.NumberOfUsers, targetUsers.Count);
                }

                // Assign licenses to each user
                int successCount = 0;
                int failCount = 0;
                int totalLicensesAssigned = 0;

                foreach (var targetUserEmail in targetUsers)
                {
                    try
                    {
                        // Find user's machines
                        var userMachines = await _context.Machines
                    .Where(m => m.user_email == targetUserEmail)
                         .OrderBy(m => m.created_at)
                             .Take(request.LicensesPerUser)
                             .ToListAsync();

                        if (!userMachines.Any())
                        {
                            _logger.LogWarning("No machines found for user: {Email}", targetUserEmail);
                            response.Errors.Add($"No machines found for user: {targetUserEmail}");
                            failCount++;
                            continue;
                        }

                        // Activate licenses for machines
                        foreach (var machine in userMachines)
                        {
                            if (!machine.license_activated)
                            {
                                machine.license_activated = true;
                                machine.license_activation_date = DateTime.UtcNow;
                                machine.license_days_valid = request.ExpiryDate.HasValue
                          ? (int)(request.ExpiryDate.Value - DateTime.UtcNow).TotalDays
                        : 365;
                                machine.updated_at = DateTime.UtcNow;
                                totalLicensesAssigned++;
                            }
                        }

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error assigning licenses to user: {Email}", targetUserEmail);
                        response.Errors.Add($"Failed to assign licenses to {targetUserEmail}: {ex.Message}");
                        failCount++;
                    }
                }

                await _context.SaveChangesAsync();

                response.Success = successCount > 0;
                response.UsersProcessed = successCount;
                response.LicensesAssigned = totalLicensesAssigned;
                response.FailedAssignments = failCount;
                response.Message = $"Successfully assigned {totalLicensesAssigned} licenses to {successCount} users";

                _logger.LogInformation(
                             "Bulk license assignment completed by {Email}. Users: {Users}, Licenses: {Licenses}",
                userEmail, successCount, totalLicensesAssigned);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during bulk license assignment");
                return StatusCode(500, new { message = "Error assigning licenses", error = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/LicenseManagement/audit-report - Get license audit report
        /// </summary>
        [HttpGet("audit-report")]
        public async Task<ActionResult<LicenseAuditReportDto>> GetLicenseAuditReport()
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

                if (!await _authService.HasPermissionAsync(userEmail, "READ_ALL_REPORTS", isSubuser))
                {
                    return StatusCode(403, new { message = "Insufficient permissions to view audit report" });
                }

                var totalLicenses = await _context.Machines.CountAsync();
                var usedLicenses = await _context.Machines.CountAsync(m => m.license_activated);
                var now = DateTime.UtcNow;
                var expiringDate = now.AddDays(30);

                var expiringLicenses = await _context.Machines
    .Where(m => m.license_activated &&
         m.license_activation_date.HasValue &&
       m.license_activation_date.Value.AddDays(m.license_days_valid) <= expiringDate &&
    m.license_activation_date.Value.AddDays(m.license_days_valid) > now)
                    .CountAsync();

                var expiredLicenses = await _context.Machines
              .Where(m => m.license_activated &&
              m.license_activation_date.HasValue &&
                       m.license_activation_date.Value.AddDays(m.license_days_valid) <= now)
                        .CountAsync();

                var licensesByUser = await _context.Machines
                     .Where(m => m.license_activated)
             .GroupBy(m => m.user_email)
                        .Select(g => new { UserEmail = g.Key, Count = g.Count() })
             .ToDictionaryAsync(x => x.UserEmail, x => x.Count);

                var recentActivity = await _context.Machines
                        .Where(m => m.license_activated && m.license_activation_date.HasValue)
                             .OrderByDescending(m => m.license_activation_date)
                             .Take(10)
                 .Select(m => new LicenseUsageEntry
                 {
                     UserEmail = m.user_email,
                     LicensesAssigned = 1,
                     AssignedAt = m.license_activation_date.GetValueOrDefault(),
                     AssignedBy = "System"
                 })
                       .ToListAsync();

                var report = new LicenseAuditReportDto
                {
                    TotalLicenses = totalLicenses,
                    UsedLicenses = usedLicenses,
                    AvailableLicenses = totalLicenses - usedLicenses,
                    ExpiringWithin30Days = expiringLicenses,
                    ExpiredLicenses = expiredLicenses,
                    LicensesByUser = licensesByUser,
                    RecentActivity = recentActivity
                };

                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating license audit report");
                return StatusCode(500, new { message = "Error generating audit report" });
            }
        }

        /// <summary>
        /// POST /api/LicenseManagement/revoke - Revoke licenses from users
        /// </summary>
        [HttpPost("revoke")]
        public async Task<IActionResult> RevokeLicenses([FromBody] List<string> userEmails)
        {
            try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
                {
                    return Unauthorized(new { message = "User not authenticated" });
                }

                var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

                if (!await _authService.HasPermissionAsync(userEmail, "MANAGE_ALL_MACHINE_LICENSES", isSubuser))
                {
                    return StatusCode(403, new { message = "Insufficient permissions to revoke licenses" });
                }

                var machines = await _context.Machines
                     .Where(m => userEmails.Contains(m.user_email) && m.license_activated)
                     .ToListAsync();

                int revokedCount = 0;
                foreach (var machine in machines)
                {
                    machine.license_activated = false;
                    machine.license_activation_date = null;
                    machine.license_days_valid = 0;
                    machine.updated_at = DateTime.UtcNow;
                    revokedCount++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Revoked {Count} licenses by {Email}", revokedCount, userEmail);

                return Ok(new
                {
                    message = $"Successfully revoked {revokedCount} licenses",
                    revokedCount
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error revoking licenses");
                return StatusCode(500, new { message = "Error revoking licenses" });
            }
        }

        /// <summary>
        /// GET /api/LicenseManagement/statistics - Get license statistics
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<LicenseSettingsDto>> GetLicenseStatistics()
        {
            try
            {
                var settings = await GetCurrentLicenseSettings();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving license statistics");
                return StatusCode(500, new { message = "Error retrieving statistics" });
            }
        }

        #region Helper Methods

        private async Task<LicenseSettingsDto> GetCurrentLicenseSettings()
        {
            var totalLicenses = await _context.Machines.CountAsync();
            var usedLicenses = await _context.Machines.CountAsync(m => m.license_activated);

            return new LicenseSettingsDto
            {
                LicenseType = "Enterprise",
                TotalLicenses = totalLicenses,
                UsedLicenses = usedLicenses,
                LicenseExpiryDate = DateTime.UtcNow.AddYears(1), // Example
                AutoRenew = false,
                SendExpiryReminders = true,
                ReminderDaysBeforeExpiry = 30
            };
        }

        #endregion
    }
}
