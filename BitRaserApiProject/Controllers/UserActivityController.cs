using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using BCrypt.Net;

// Use DTOs namespace explicitly
using DTO = BitRaserApiProject.Models.DTOs;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced User Activity Tracking Controller - Hierarchical monitoring with real-time status
    /// Supports Users, Subusers, and Manager hierarchy with email-based filtering
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
        /// Get user activity by email with login/logout time and status
        /// Supports hierarchical access - managers can see their team's activity
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<DTO.UserActivityResponse>> GetUserActivityByEmail(
       string email,
 [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null,
     [FromQuery] string status = "all",
   [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
      {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(currentUserEmail))
           {
     return Unauthorized(new { message = "User not authenticated" });
       }

          var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

         // Check permissions - either viewing own activity or has permission to view others
                if (email != currentUserEmail)
  {
    // Check if current user can view this user's activity
       if (!await CanViewUserActivity(currentUserEmail, email, isCurrentUserSubuser))
 {
        return StatusCode(403, new { message = "You don't have permission to view this user's activity" });
               }
    }

           // Set date range defaults
  startDate ??= DateTime.UtcNow.AddDays(-30);
     endDate ??= DateTime.UtcNow;

             // Get sessions for the user
                var query = _context.Sessions
       .Where(s => s.user_email == email && 
     s.login_time >= startDate && 
s.login_time <= endDate);

           // Apply status filter
                if (status.ToLower() != "all")
                {
         query = query.Where(s => s.session_status == status.ToLower());
       }

         var totalCount = await query.CountAsync();

      var sessions = await query
  .OrderByDescending(s => s.login_time)
         .Skip((page - 1) * pageSize)
     .Take(pageSize)
 .ToListAsync();

   // Determine user type and get name
var userName = await GetUserName(email);
                var userType = await _userDataService.SubuserExistsAsync(email) ? "subuser" : "user";

           var activities = sessions.Select(s => new DTO.UserActivityDto
         {
       Id = s.session_id.ToString(),
      UserEmail = s.user_email,
         UserName = userName,
      UserType = userType,
       LoginTime = s.login_time,
  LogoutTime = s.logout_time,
          Status = DetermineSessionStatus(s),
        IpAddress = s.ip_address ?? "Unknown",
     DeviceInfo = s.device_info ?? "Unknown Device",
 SessionDuration = CalculateSessionDuration(s),
 SessionId = s.session_id,
          Timestamp = s.login_time
      }).ToList();

    // Calculate summary
    var summary = await CalculateActivitySummary(email, startDate.Value, endDate.Value);

      return Ok(new DTO.UserActivityResponse
         {
         Activities = activities,
     TotalCount = totalCount,
         Page = page,
         PageSize = pageSize,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
         Summary = summary
      });
            }
            catch (Exception ex)
  {
      _logger.LogError(ex, "Error getting user activity for {Email}", email);
     return StatusCode(500, new { message = "Error retrieving user activity", error = ex.Message });
        }
    }

        /// <summary>
      /// Get hierarchical activity - manager can see all their users and subusers
     /// Shows complete team activity with online/offline status
        /// </summary>
        [HttpPost("hierarchical")]
        public async Task<ActionResult<DTO.HierarchicalActivityResponse>> GetHierarchicalActivity(
            [FromBody] DTO.HierarchicalActivityRequest request)
        {
        try
            {
                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   if (string.IsNullOrEmpty(currentUserEmail))
           {
         return Unauthorized(new { message = "User not authenticated" });
  }

      var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

      // Check permission to view hierarchical data
                if (!await _authService.HasPermissionAsync(currentUserEmail, "VIEW_HIERARCHY", isSubuser) &&
 !await _authService.HasPermissionAsync(currentUserEmail, "VIEW_ORGANIZATION_HIERARCHY", isSubuser))
      {
        return StatusCode(403, new { message = "Insufficient permissions to view hierarchical activity" });
           }

             request.StartDate ??= DateTime.UtcNow.AddDays(-7);
     request.EndDate ??= DateTime.UtcNow;

     var directUsers = new List<DTO.UserActivityDto>();
   var subusers = new List<DTO.UserActivityDto>();
  var managedUsers = new List<DTO.UserActivityDto>();

        // Get all users if has permission
  if (await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_USERS", isSubuser))
       {
     directUsers = await GetUsersActivity(request.StartDate.Value, request.EndDate.Value, request.StatusFilter);
   }
     else
       {
   // Get only managed users
       managedUsers = await GetManagedUsersActivity(currentUserEmail, request.StartDate.Value, request.EndDate.Value, request.StatusFilter);
     }

        // Get subusers if requested
   if (request.IncludeSubusers)
       {
   if (await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_SUBUSERS", isSubuser))
 {
         subusers = await GetSubusersActivity(request.StartDate.Value, request.EndDate.Value, request.StatusFilter);
   }
     else if (await _authService.HasPermissionAsync(currentUserEmail, "READ_USER_SUBUSERS", isSubuser))
        {
          subusers = await GetUserSubusersActivity(currentUserEmail, request.StartDate.Value, request.EndDate.Value, request.StatusFilter);
                    }
                }

   var allActivities = directUsers.Concat(subusers).Concat(managedUsers).ToList();

         // Apply pagination
        var totalCount = allActivities.Count;
         var paginatedActivities = allActivities
           .OrderByDescending(a => a.LoginTime)
       .Skip((request.Page - 1) * request.PageSize)
        .Take(request.PageSize)
           .ToList();

    // Calculate statistics
        var statistics = CalculateHierarchicalStatistics(directUsers, subusers, managedUsers);

           return Ok(new DTO.HierarchicalActivityResponse
   {
          ManagerEmail = currentUserEmail,
    DirectUsers = paginatedActivities.Where(a => a.UserType == "user").ToList(),
          Subusers = paginatedActivities.Where(a => a.UserType == "subuser").ToList(),
           ManagedUsers = managedUsers,
    Statistics = statistics,
   TotalCount = totalCount,
   Page = request.Page,
            PageSize = request.PageSize,
         TotalPages = (int)Math.Ceiling(totalCount / (double)request.PageSize)
     });
        }
        catch (Exception ex)
 {
           _logger.LogError(ex, "Error getting hierarchical activity");
       return StatusCode(500, new { message = "Error retrieving hierarchical activity", error = ex.Message });
       }
  }

        /// <summary>
   /// Get current online/offline status of all users
        /// Real-time dashboard for monitoring user presence
        /// </summary>
        [HttpGet("live-status")]
 public async Task<ActionResult<DTO.LiveActivityDashboardDto>> GetLiveStatus()
        {
          try
   {
    var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     if (string.IsNullOrEmpty(currentUserEmail))
     {
      return Unauthorized(new { message = "User not authenticated" });
      }

     var isSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail);

// Check permission
        if (!await _authService.HasPermissionAsync(currentUserEmail, "VIEW_USER_ACTIVITY", isSubuser) &&
          !await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_SESSION_STATISTICS", isSubuser))
                {
            return StatusCode(403, new { message = "Insufficient permissions" });
        }

    // Get active sessions (online users)
     var activeSessions = await _context.Sessions
      .Where(s => s.session_status == "active")
        .OrderByDescending(s => s.login_time)
        .Take(50)
            .ToListAsync();

    // Get recent logins (last 1 hour)
     var recentLogins = await _context.Sessions
       .Where(s => s.login_time >= DateTime.UtcNow.AddHours(-1))
        .OrderByDescending(s => s.login_time)
  .Take(20)
         .ToListAsync();

          // Get recent logouts (last 1 hour)
 var recentLogouts = await _context.Sessions
         .Where(s => s.logout_time != null && s.logout_time >= DateTime.UtcNow.AddHours(-1))
          .OrderByDescending(s => s.logout_time)
         .Take(20)
   .ToListAsync();

    // Convert to DTOs
      var onlineUsers = await ConvertSessionsToActivityDtos(activeSessions);
         var recentLoginDtos = await ConvertSessionsToActivityDtos(recentLogins);
 var recentLogoutDtos = await ConvertSessionsToActivityDtos(recentLogouts);

// Calculate statistics
                var totalOnlineUsers = onlineUsers.Count(a => a.UserType == "user");
          var totalOnlineSubusers = onlineUsers.Count(a => a.UserType == "subuser");

      var statistics = new DTO.ActivityStatistics
       {
          OnlineUsers = totalOnlineUsers,
         OnlineSubusers = totalOnlineSubusers,
   TotalUsers = await _context.Users.CountAsync(),
  TotalSubusers = await _context.subuser.CountAsync(),
            OfflineUsers = await _context.Users.CountAsync() - totalOnlineUsers,
   OfflineSubusers = await _context.subuser.CountAsync() - totalOnlineSubusers,
        OnlinePercentage = Math.Round((totalOnlineUsers + totalOnlineSubusers) / 
        (double)(await _context.Users.CountAsync() + await _context.subuser.CountAsync()) * 100, 2),
   LastUpdated = DateTime.UtcNow
                };

            return Ok(new DTO.LiveActivityDashboardDto
         {
   TotalOnlineUsers = totalOnlineUsers,
    TotalOnlineSubusers = totalOnlineSubusers,
  RecentLogins = recentLoginDtos,
             RecentLogouts = recentLogoutDtos,
CurrentlyActive = onlineUsers,
     Statistics = statistics,
         LastRefreshed = DateTime.UtcNow
        });
      }
   catch (Exception ex)
   {
           _logger.LogError(ex, "Error getting live status");
     return StatusCode(500, new { message = "Error retrieving live status", error = ex.Message });
            }
        }

        /// <summary>
        /// Get user activity analytics with detailed breakdown
/// </summary>
        [HttpGet("analytics/{email}")]
        public async Task<ActionResult<DTO.ActivityAnalyticsDto>> GetUserAnalytics(
        string email,
            [FromQuery] int days = 30)
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
        if (email != currentUserEmail && !await CanViewUserActivity(currentUserEmail, email, isSubuser))
                {
        return StatusCode(403, new { message = "Insufficient permissions" });
 }

                var startDate = DateTime.UtcNow.AddDays(-days);

       var sessions = await _context.Sessions
        .Where(s => s.user_email == email && s.login_time >= startDate)
        .ToListAsync();

                var analytics = new DTO.ActivityAnalyticsDto
   {
     UserEmail = email,
       TotalLogins = sessions.Count,
    TotalLogouts = sessions.Count(s => s.logout_time != null),
   FirstLogin = sessions.Min(s => (DateTime?)s.login_time),
         LastLogin = sessions.Max(s => (DateTime?)s.login_time),
       TotalActiveTime = TimeSpan.FromMinutes(sessions
       .Where(s => s.logout_time != null)
  .Sum(s => (s.logout_time!.Value - s.login_time).TotalMinutes)),
        AverageSessionDuration = sessions.Any(s => s.logout_time != null)
   ? TimeSpan.FromMinutes(sessions
          .Where(s => s.logout_time != null)
            .Average(s => (s.logout_time!.Value - s.login_time).TotalMinutes))
      : TimeSpan.Zero,
      DailyActivity = sessions
  .GroupBy(s => s.login_time.Date)
         .Select(g => new DTO.DailyActivityDto
             {
       Date = g.Key,
   LoginCount = g.Count(),
    UniqueUsers = 1,
    TotalActiveTime = TimeSpan.FromMinutes(g
      .Where(s => s.logout_time != null)
     .Sum(s => (s.logout_time!.Value - s.login_time).TotalMinutes))
         })
       .OrderBy(d => d.Date)
   .ToList(),
          HourlyActivity = sessions
       .GroupBy(s => s.login_time.Hour)
       .Select(g => new DTO.HourlyActivityDto
  {
         Hour = g.Key,
   LoginCount = g.Count(),
               ActiveUsers = 1
         })
               .OrderBy(h => h.Hour)
      .ToList(),
     DeviceBreakdown = sessions
         .Where(s => !string.IsNullOrEmpty(s.device_info))
    .GroupBy(s => ExtractDeviceType(s.device_info!))
             .Select(g => new DTO.DeviceUsageDto
   {
           DeviceType = g.Key,
           Count = g.Count(),
       Percentage = Math.Round((g.Count() / (double)sessions.Count) * 100, 2)
    })
          .ToList()
                };

           return Ok(analytics);
            }
            catch (Exception ex)
            {
            _logger.LogError(ex, "Error getting user analytics for {Email}", email);
      return StatusCode(500, new { message = "Error retrieving analytics", error = ex.Message });
            }
        }

        #region Private Helper Methods

        private async Task<bool> CanViewUserActivity(string currentUserEmail, string targetUserEmail, bool isCurrentUserSubuser)
        {
            // SuperAdmin and Admin can view all
  if (await _authService.HasPermissionAsync(currentUserEmail, "VIEW_USER_ACTIVITY", isCurrentUserSubuser) ||
 await _authService.HasPermissionAsync(currentUserEmail, "READ_ALL_SESSION_STATISTICS", isCurrentUserSubuser))
    {
        return true;
     }

  // Check if target user is a subuser of current user
        if (!isCurrentUserSubuser)
            {
                var isSubuser = await _context.subuser.AnyAsync(s => 
       s.subuser_email == targetUserEmail && s.user_email == currentUserEmail);
          if (isSubuser) return true;
            }

            return false;
 }

    private string DetermineSessionStatus(Sessions session)
  {
         if (session.session_status == "active")
            {
           return "online";
         }
            return "offline";
    }

        private TimeSpan? CalculateSessionDuration(Sessions session)
        {
  if (session.logout_time.HasValue)
{
          return session.logout_time.Value - session.login_time;
  }
          else if (session.session_status == "active")
  {
         return DateTime.UtcNow - session.login_time;
      }
return null;
        }

   private async Task<DTO.UserActivitySummary> CalculateActivitySummary(string email, DateTime startDate, DateTime endDate)
        {
          var sessions = await _context.Sessions
       .Where(s => s.user_email == email && s.login_time >= startDate && s.login_time <= endDate)
         .ToListAsync();

    var completedSessions = sessions.Where(s => s.logout_time.HasValue).ToList();
     var avgDuration = completedSessions.Any()
    ? TimeSpan.FromMinutes(completedSessions.Average(s => (s.logout_time!.Value - s.login_time).TotalMinutes))
     : TimeSpan.Zero;

        var lastSession = sessions.OrderByDescending(s => s.login_time).FirstOrDefault();
    var currentStatus = lastSession?.session_status == "active" ? "online" : "offline";

            return new DTO.UserActivitySummary
          {
                TotalSessions = sessions.Count,
     ActiveSessions = sessions.Count(s => s.session_status == "active"),
      OfflineSessions = sessions.Count(s => s.session_status != "active"),
      AverageSessionDuration = avgDuration,
                LastLogin = sessions.Max(s => (DateTime?)s.login_time),
     LastLogout = sessions.Where(s => s.logout_time.HasValue).Max(s => (DateTime?)s.logout_time),
    CurrentStatus = currentStatus
    };
        }

        private async Task<string> GetUserName(string email)
      {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == email);
     if (user != null) return user.user_name;

    var subuser = await _context.subuser.FirstOrDefaultAsync(s => s.subuser_email == email);
    return subuser?.Name ?? subuser?.subuser_username ?? email;
        }

        private async Task<List<DTO.UserActivityDto>> GetUsersActivity(DateTime startDate, DateTime endDate, string statusFilter)
  {
            var query = _context.Sessions
       .Where(s => s.login_time >= startDate && s.login_time <= endDate);

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter.ToLower() != "all")
        {
    query = query.Where(s => s.session_status == statusFilter.ToLower());
        }

     var sessions = await query.OrderByDescending(s => s.login_time).Take(100).ToListAsync();
         return await ConvertSessionsToActivityDtos(sessions);
        }

      private async Task<List<DTO.UserActivityDto>> GetSubusersActivity(DateTime startDate, DateTime endDate, string statusFilter)
        {
     var subuserEmails = await _context.subuser.Select(s => s.subuser_email).ToListAsync();
            
    var query = _context.Sessions
            .Where(s => subuserEmails.Contains(s.user_email) && 
   s.login_time >= startDate && 
   s.login_time <= endDate);

  if (!string.IsNullOrEmpty(statusFilter) && statusFilter.ToLower() != "all")
       {
      query = query.Where(s => s.session_status == statusFilter.ToLower());
     }

            var sessions = await query.OrderByDescending(s => s.login_time).Take(100).ToListAsync();
   return await ConvertSessionsToActivityDtos(sessions);
  }

        private async Task<List<DTO.UserActivityDto>> GetUserSubusersActivity(string userEmail, DateTime startDate, DateTime endDate, string statusFilter)
        {
        var subuserEmails = await _context.subuser
     .Where(s => s.user_email == userEmail)
  .Select(s => s.subuser_email)
       .ToListAsync();
            
            var query = _context.Sessions
        .Where(s => subuserEmails.Contains(s.user_email) && 
             s.login_time >= startDate && 
        s.login_time <= endDate);

            if (!string.IsNullOrEmpty(statusFilter) && statusFilter.ToLower() != "all")
         {
     query = query.Where(s => s.session_status == statusFilter.ToLower());
            }

   var sessions = await query.OrderByDescending(s => s.login_time).ToListAsync();
       return await ConvertSessionsToActivityDtos(sessions);
     }

        private async Task<List<DTO.UserActivityDto>> GetManagedUsersActivity(string managerEmail, DateTime startDate, DateTime endDate, string statusFilter)
        {
          // This would depend on your hierarchy implementation
     // For now, returning empty list - can be extended
            return new List<DTO.UserActivityDto>();
}

        private async Task<List<DTO.UserActivityDto>> ConvertSessionsToActivityDtos(List<Sessions> sessions)
  {
            var activities = new List<DTO.UserActivityDto>();

foreach (var session in sessions)
            {
           var userName = await GetUserName(session.user_email);
  var userType = await _userDataService.SubuserExistsAsync(session.user_email) ? "subuser" : "user";

           activities.Add(new DTO.UserActivityDto
             {
            Id = session.session_id.ToString(),
           UserEmail = session.user_email,
       UserName = userName,
              UserType = userType,
    LoginTime = session.login_time,
                    LogoutTime = session.logout_time,
         Status = DetermineSessionStatus(session),
        IpAddress = session.ip_address ?? "Unknown",
     DeviceInfo = session.device_info ?? "Unknown Device",
         SessionDuration = CalculateSessionDuration(session),
                    SessionId = session.session_id,
 Timestamp = session.login_time
    });
     }

            return activities;
        }

        private DTO.ActivityStatistics CalculateHierarchicalStatistics(
  List<DTO.UserActivityDto> users, 
         List<DTO.UserActivityDto> subusers, 
    List<DTO.UserActivityDto> managed)
  {
 var allActivities = users.Concat(subusers).Concat(managed).ToList();

            var totalUsers = users.Count;
    var onlineUsers = users.Count(a => a.Status == "online");
  var totalSubusers = subusers.Count;
        var onlineSubusers = subusers.Count(a => a.Status == "online");

      var allSessionDurations = allActivities
          .Where(a => a.SessionDuration.HasValue)
     .Select(a => a.SessionDuration!.Value)
  .ToList();

     var avgDuration = allSessionDurations.Any()
          ? TimeSpan.FromMinutes(allSessionDurations.Average(d => d.TotalMinutes))
     : TimeSpan.Zero;

            var totalCount = totalUsers + totalSubusers;
          var onlineCount = onlineUsers + onlineSubusers;

            return new DTO.ActivityStatistics
        {
        TotalUsers = totalUsers,
          OnlineUsers = onlineUsers,
       OfflineUsers = totalUsers - onlineUsers,
     TotalSubusers = totalSubusers,
    OnlineSubusers = onlineSubusers,
           OfflineSubusers = totalSubusers - onlineSubusers,
          OnlinePercentage = totalCount > 0 ? Math.Round((onlineCount / (double)totalCount) * 100, 2) : 0,
   AverageSessionDuration = avgDuration,
   LastUpdated = DateTime.UtcNow
    };
        }

        private string ExtractDeviceType(string deviceInfo)
      {
         if (string.IsNullOrEmpty(deviceInfo)) return "Unknown";

         deviceInfo = deviceInfo.ToLower();
            if (deviceInfo.Contains("mobile") || deviceInfo.Contains("android") || deviceInfo.Contains("ios")) 
                return "Mobile";
     if (deviceInfo.Contains("windows")) return "Windows";
            if (deviceInfo.Contains("mac") || deviceInfo.Contains("darwin")) return "Mac";
            if (deviceInfo.Contains("linux")) return "Linux";
       if (deviceInfo.Contains("tablet") || deviceInfo.Contains("ipad")) return "Tablet";
     
     return "Desktop";
     }

        #endregion
    }
}
