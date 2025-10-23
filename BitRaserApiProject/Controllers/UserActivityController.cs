using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// User Activity Tracking Controller - Monitor user login/logout activity
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserActivityController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ILogger<UserActivityController> _logger;

 public UserActivityController(
   ApplicationDbContext context,
            IRoleBasedAuthService authService,
            IUserDataService userDataService,
            ILogger<UserActivityController> logger)
        {
       _context = context;
       _authService = authService;
  _userDataService = userDataService;
      _logger = logger;
  }

      /// <summary>
      /// Get cloud users activity - Login/Logout tracking
        /// </summary>
[HttpGet("cloud-users")]
      public async Task<ActionResult<CloudUsersActivityDto>> GetCloudUsersActivity(
          [FromQuery] int page = 1,
     [FromQuery] int pageSize = 20)
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
              if (!await _authService.HasPermissionAsync(userEmail, "VIEW_USER_ACTIVITY", isSubuser))
           {
        return StatusCode(403, new { message = "Insufficient permissions to view user activity" });
              }

     // Get sessions with user activity
         var query = _context.Sessions
       .OrderByDescending(s => s.login_time);

       var totalCount = await query.CountAsync();
                var sessions = await query
      .Skip((page - 1) * pageSize)
        .Take(pageSize)
         .Select(s => new UserActivityItemDto
       {
              UserEmail = s.user_email,
  LoginTime = s.login_time,
      LogoutTime = s.logout_time,
         Status = s.session_status == "active" ? "active" : "offline",
       IpAddress = s.ip_address,
      DeviceInfo = s.device_info
      })
      .ToListAsync();

      return Ok(new CloudUsersActivityDto
   {
  Title = "Cloud Users Activity",
    Description = "Monitor user login and logout activity",
             Activities = sessions,
          TotalCount = totalCount,
          Page = page,
          PageSize = pageSize,
     TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    });
     }
     catch (Exception ex)
      {
                _logger.LogError(ex, "Error getting cloud users activity");
   return StatusCode(500, new { message = "Error retrieving user activity", error = ex.Message });
         }
        }

        /// <summary>
        /// Get user login history
    /// </summary>
        [HttpGet("login-history/{email}")]
    public async Task<ActionResult<List<UserActivityItemDto>>> GetUserLoginHistory(string email)
   {
      try
            {
    var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 if (string.IsNullOrEmpty(currentUserEmail))
          {
        return Unauthorized(new { message = "User not authenticated" });
       }

                var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

     // Users can view their own activity or admins can view all
          if (email != currentUserEmail && 
       !await _authService.HasPermissionAsync(currentUserEmail, "VIEW_ALL_USER_ACTIVITY", isSubuser))
                {
      return StatusCode(403, new { message = "You can only view your own activity" });
          }

                var history = await _context.Sessions
         .Where(s => s.user_email == email)
   .OrderByDescending(s => s.login_time)
      .Take(50)
      .Select(s => new UserActivityItemDto
 {
      UserEmail = s.user_email,
       LoginTime = s.login_time,
     LogoutTime = s.logout_time,
          Status = s.session_status == "active" ? "active" : "offline",
  IpAddress = s.ip_address,
       DeviceInfo = s.device_info
 })
                .ToListAsync();

   return Ok(history);
            }
         catch (Exception ex)
      {
  _logger.LogError(ex, "Error getting user login history for {Email}", email);
   return StatusCode(500, new { message = "Error retrieving login history" });
            }
     }

 /// <summary>
/// Get active users count
      /// </summary>
        [HttpGet("active-count")]
        public async Task<ActionResult<ActiveUsersCountDto>> GetActiveUsersCount()
 {
            try
  {
  var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
       {
     return Unauthorized(new { message = "User not authenticated" });
 }

         var activeCount = await _context.Sessions
             .CountAsync(s => s.session_status == "active");

        var totalUsers = await _context.Users.CountAsync();

         return Ok(new ActiveUsersCountDto
           {
           ActiveCount = activeCount,
 TotalUsers = totalUsers,
      OfflineCount = totalUsers - activeCount,
            LastUpdated = DateTime.UtcNow
    });
       }
         catch (Exception ex)
            {
       _logger.LogError(ex, "Error getting active users count");
        return StatusCode(500, new { message = "Error retrieving active users count" });
      }
        }

        /// <summary>
/// Get erasure reports for Reports tab
     /// </summary>
        [HttpGet("erasure-reports")]
    public async Task<ActionResult<ErasureReportsDto>> GetErasureReports(
     [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
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
       if (!await _authService.HasPermissionAsync(userEmail, "VIEW_REPORTS", isSubuser))
     {
           return StatusCode(403, new { message = "Insufficient permissions to view reports" });
 }

             var query = _context.AuditReports.OrderByDescending(r => r.report_datetime);

          var totalCount = await query.CountAsync();
     var reports = await query
         .Skip((page - 1) * pageSize)
 .Take(pageSize)
        .Select(r => new ErasureReportItemDto
           {
ReportId = r.report_id.ToString(),
     Type = r.erasure_method,
    Devices = GetDeviceCountFromJson(r.report_details_json),
       Status = r.synced ? "completed" : "running",
        Date = r.report_datetime,
  Method = GetMethodFromType(r.erasure_method)
     })
        .ToListAsync();

     return Ok(new ErasureReportsDto
         {
          Title = "Erasure Reports",
       Description = "View and manage erasure reports",
       Reports = reports,
          TotalCount = totalCount,
     Page = page,
             PageSize = pageSize,
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
    });
  }
            catch (Exception ex)
            {
_logger.LogError(ex, "Error getting erasure reports");
     return StatusCode(500, new { message = "Error retrieving erasure reports", error = ex.Message });
      }
        }

        /// <summary>
        /// Create new user (Add User functionality)
        /// </summary>
      [HttpPost("create-user")]
        public async Task<ActionResult<CreateUserResponseDto>> CreateNewUser([FromBody] CreateNewUserDto request)
        {
   try
            {
   var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  if (string.IsNullOrEmpty(currentUserEmail))
          {
       return Unauthorized(new { message = "User not authenticated" });
}

      var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

       // Check permissions
      if (!await _authService.HasPermissionAsync(currentUserEmail, "CREATE_USER", isSubuser))
       {
          return StatusCode(403, new { message = "Insufficient permissions to create users" });
             }

       // Validate input
     if (string.IsNullOrEmpty(request.FullName) || 
string.IsNullOrEmpty(request.EmailAddress) || 
            string.IsNullOrEmpty(request.Password))
             {
return BadRequest(new { message = "Full name, email, and password are required" });
         }

                // Check if user already exists
          if (await _context.Users.AnyAsync(u => u.user_email == request.EmailAddress))
         {
      return Conflict(new { message = "User with this email already exists" });
                }

   // Validate password match
           if (request.Password != request.ConfirmPassword)
         {
  return BadRequest(new { message = "Passwords do not match" });
       }

     // Create new user
     var newUser = new users
         {
  user_name = request.FullName,
        user_email = request.EmailAddress,
         user_password = request.Password,
          hash_password = BCrypt.Net.BCrypt.HashPassword(request.Password),
         phone_number = string.Empty,
         payment_details_json = "{}",
    license_details_json = $"{{\"licenseAllocation\": {request.LicenseAllocation}}}",
   created_at = DateTime.UtcNow,
         updated_at = DateTime.UtcNow,
            private_api = request.AccountStatus == "Active"
};

                _context.Users.Add(newUser);
           await _context.SaveChangesAsync();

       // Assign role
 var role = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == request.UserRole);
   if (role != null)
     {
              var userRole = new UserRole
    {
        UserId = newUser.user_id,
              RoleId = role.RoleId,
   AssignedAt = DateTime.UtcNow,
 AssignedByEmail = currentUserEmail
  };
        _context.UserRoles.Add(userRole);
             await _context.SaveChangesAsync();
     }

              // Assign to group (using roles as groups)
   // Group assignment can be extended based on requirements

                _logger.LogInformation("User {Email} created by {Creator}", request.EmailAddress, currentUserEmail);

      return Ok(new CreateUserResponseDto
    {
 Success = true,
  Message = "User created successfully",
        UserId = newUser.user_id.ToString(),
         UserEmail = newUser.user_email,
        CreatedAt = newUser.created_at
      });
     }
 catch (Exception ex)
       {
       _logger.LogError(ex, "Error creating new user");
         return StatusCode(500, new { message = "Error creating user", error = ex.Message });
            }
        }

    #region Private Helper Methods

        private int GetDeviceCountFromJson(string reportDetailsJson)
        {
       try
   {
      if (string.IsNullOrEmpty(reportDetailsJson)) return 1;
       
       // Simple count based on JSON structure
       var deviceCount = reportDetailsJson.Split("device", StringSplitOptions.RemoveEmptyEntries).Length - 1;
         return deviceCount > 0 ? deviceCount : 1;
      }
            catch
            {
       return 1;
         }
        }

        private string GetMethodFromType(string erasureMethod)
        {
  if (string.IsNullOrEmpty(erasureMethod)) return "Unknown";

            return erasureMethod.ToLower() switch
            {
            var m when m.Contains("drive") => "NIST 800-88 Purge",
var m when m.Contains("mobile") => "Hardware Scan",
   var m when m.Contains("network") => "DoD 5220.22-M",
    var m when m.Contains("file") => "Secure Delete",
      _ => "Standard Erase"
     };
        }

        #endregion

     /// <summary>
        /// Get available roles for Add User dropdown
        /// </summary>
        [HttpGet("available-roles")]
        public async Task<ActionResult<AvailableRolesDto>> GetAvailableRoles()
        {
         try
            {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
                {
   return Unauthorized(new { message = "User not authenticated" });
       }

    var roles = await _context.Roles
        .OrderBy(r => r.HierarchyLevel)
             .Select(r => new RoleOptionDto
          {
        Value = r.RoleName,
         Label = r.RoleName,
 Description = r.Description
     })
  .ToListAsync();

 return Ok(new AvailableRolesDto { Roles = roles });
            }
            catch (Exception ex)
            {
 _logger.LogError(ex, "Error getting available roles");
     return StatusCode(500, new { message = "Error retrieving roles" });
            }
        }

        /// <summary>
      /// Get available groups for Add User dropdown
      /// </summary>
     [HttpGet("available-groups")]
     public async Task<ActionResult<AvailableGroupsDto>> GetAvailableGroups()
        {
     try
  {
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          if (string.IsNullOrEmpty(userEmail))
      {
    return Unauthorized(new { message = "User not authenticated" });
            }

     // Using roles as groups
     var groups = await _context.Roles
  .Select(r => new GroupOptionDto
        {
   Value = r.RoleName,
       Label = $"{r.RoleName} Group",
     MemberCount = _context.UserRoles.Count(ur => ur.RoleId == r.RoleId)
 })
          .ToListAsync();

                return Ok(new AvailableGroupsDto { Groups = groups });
          }
            catch (Exception ex)
      {
    _logger.LogError(ex, "Error getting available groups");
     return StatusCode(500, new { message = "Error retrieving groups" });
   }
   }

      /// <summary>
  /// Get user activity analytics
   /// </summary>
        [HttpGet("analytics")]
        public async Task<ActionResult<UserActivityAnalyticsDto>> GetActivityAnalytics([FromQuery] int days = 7)
        {
            try
   {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  if (string.IsNullOrEmpty(userEmail))
                {
 return Unauthorized(new { message = "User not authenticated" });
    }

         var startDate = DateTime.UtcNow.AddDays(-days);

            var totalLogins = await _context.Sessions
     .CountAsync(s => s.login_time >= startDate);

    var uniqueUsers = await _context.Sessions
       .Where(s => s.login_time >= startDate)
         .Select(s => s.user_email)
             .Distinct()
        .CountAsync();

    var currentlyActive = await _context.Sessions
  .CountAsync(s => s.session_status == "active");

             // Calculate average session duration
   var sessionsWithDuration = await _context.Sessions
   .Where(s => s.login_time >= startDate && s.logout_time != null)
     .Select(s => new { 
         Duration = (s.logout_time!.Value - s.login_time).TotalMinutes 
       })
    .ToListAsync();

     var avgDuration = sessionsWithDuration.Any() 
? sessionsWithDuration.Average(s => s.Duration) 
        : 0;

         // Daily activity
    var dailyActivity = await _context.Sessions
      .Where(s => s.login_time >= startDate)
         .GroupBy(s => s.login_time.Date)
          .Select(g => new DailyActivityDto
        {
    Date = g.Key,
              LoginCount = g.Count(),
         UniqueUsers = g.Select(s => s.user_email).Distinct().Count()
         })
          .OrderBy(d => d.Date)
         .ToListAsync();

       return Ok(new UserActivityAnalyticsDto
        {
   TotalLogins = totalLogins,
  UniqueUsers = uniqueUsers,
       CurrentlyActive = currentlyActive,
 AverageSessionDuration = Math.Round(avgDuration, 2),
  DailyActivity = dailyActivity,
       HourlyActivity = new List<HourlyActivityDto>()
 });
         }
  catch (Exception ex)
     {
        _logger.LogError(ex, "Error getting activity analytics");
       return StatusCode(500, new { message = "Error retrieving analytics" });
   }
        }

      /// <summary>
        /// Get erasure report analytics
        /// </summary>
    [HttpGet("report-analytics")]
  public async Task<ActionResult<ErasureReportAnalyticsDto>> GetReportAnalytics()
        {
     try
      {
   var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   if (string.IsNullOrEmpty(userEmail))
       {
     return Unauthorized(new { message = "User not authenticated" });
    }

       var totalReports = await _context.AuditReports.CountAsync();
   var completedReports = await _context.AuditReports.CountAsync(r => r.synced);
          var runningReports = await _context.AuditReports.CountAsync(r => !r.synced);

      var typeBreakdown = await _context.AuditReports
         .GroupBy(r => r.erasure_method)
       .Select(g => new ReportTypeStatsDto
      {
         Type = g.Key,
      Count = g.Count(),
     DeviceCount = g.Sum(r => GetDeviceCountFromJson(r.report_details_json))
       })
         .ToListAsync();

       return Ok(new ErasureReportAnalyticsDto
     {
       TotalReports = totalReports,
           CompletedReports = completedReports,
          RunningReports = runningReports,
     FailedReports = 0,
       TotalDevicesErased = typeBreakdown.Sum(t => t.DeviceCount),
     TypeBreakdown = typeBreakdown,
     MethodBreakdown = new List<MethodStatsDto>()
   });
    }
         catch (Exception ex)
     {
  _logger.LogError(ex, "Error getting report analytics");
 return StatusCode(500, new { message = "Error retrieving report analytics" });
    }
     }

 /// <summary>
 /// Update user status (Activate/Deactivate)
        /// </summary>
  [HttpPatch("update-status")]
        public async Task<IActionResult> UpdateUserStatus([FromBody] UpdateUserStatusDto request)
     {
try
  {
  var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  if (string.IsNullOrEmpty(currentUserEmail))
        {
    return Unauthorized(new { message = "User not authenticated" });
        }

       var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

  if (!await _authService.HasPermissionAsync(currentUserEmail, "UPDATE_USER_STATUS", isSubuser))
     {
      return StatusCode(403, new { message = "Insufficient permissions" });
   }

     var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == request.UserEmail);
  if (user == null)
         {
    return NotFound(new { message = "User not found" });
    }

      user.private_api = request.Status == "Active";
 user.updated_at = DateTime.UtcNow;

    await _context.SaveChangesAsync();

     _logger.LogInformation("User {Email} status updated to {Status} by {Admin}", 
       request.UserEmail, request.Status, currentUserEmail);

     return Ok(new { 
     message = "User status updated successfully",
       userEmail = request.UserEmail,
       status = request.Status
             });
       }
        catch (Exception ex)
       {
         _logger.LogError(ex, "Error updating user status");
      return StatusCode(500, new { message = "Error updating user status" });
 }
        }

        /// <summary>
        /// Bulk update user status
        /// </summary>
  [HttpPatch("bulk-update-status")]
        public async Task<IActionResult> BulkUpdateUserStatus([FromBody] BulkUpdateUserStatusDto request)
  {
      try
          {
      var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 if (string.IsNullOrEmpty(currentUserEmail))
    {
         return Unauthorized(new { message = "User not authenticated" });
     }

           var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

     if (!await _authService.HasPermissionAsync(currentUserEmail, "BULK_UPDATE_USERS", isSubuser))
     {
      return StatusCode(403, new { message = "Insufficient permissions" });
  }

      var users = await _context.Users
     .Where(u => request.UserEmails.Contains(u.user_email))
       .ToListAsync();

 foreach (var user in users)
          {
       user.private_api = request.Status == "Active";
          user.updated_at = DateTime.UtcNow;
     }

    await _context.SaveChangesAsync();

     _logger.LogInformation("Bulk status update: {Count} users updated to {Status} by {Admin}", 
      users.Count, request.Status, currentUserEmail);

    return Ok(new { 
   message = $"Successfully updated {users.Count} users",
    updatedCount = users.Count,
  status = request.Status
   });
      }
      catch (Exception ex)
    {
       _logger.LogError(ex, "Error bulk updating user status");
       return StatusCode(500, new { message = "Error bulk updating user status" });
    }
        }
    }
}
