using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Admin Dashboard Controller - Complete Dashboard Management
    /// Based on BitRaser Admin Dashboard Design
    /// </summary>
 [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class EnhancedDashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
  private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ILogger<EnhancedDashboardController> _logger;

        public EnhancedDashboardController(
    ApplicationDbContext context,
    IRoleBasedAuthService authService,
         IUserDataService userDataService,
            ILogger<EnhancedDashboardController> logger)
    {
            _context = context;
       _authService = authService;
            _userDataService = userDataService;
            _logger = logger;
        }

        /// <summary>
        /// Get comprehensive dashboard overview with metrics and statistics
    /// </summary>
  [HttpGet("overview")]
        public async Task<ActionResult<EnhancedDashboardOverviewDto>> GetDashboardOverview()
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
           if (!await _authService.HasPermissionAsync(userEmail, "VIEW_DASHBOARD", isSubuser))
      {
   return StatusCode(403, new { message = "Insufficient permissions to view dashboard" });
        }

            // Calculate metrics
    var totalLicenses = await _context.Machines.CountAsync();
 var activeLicenses = await _context.Machines.CountAsync(m => m.license_activated);
 var availableLicenses = totalLicenses - activeLicenses;
              
       // Calculate license percentage change (last 30 days)
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
 var previousTotal = await _context.Machines.CountAsync(m => m.created_at < thirtyDaysAgo);
          var licenseChangePercent = previousTotal > 0 ? 
 ((totalLicenses - previousTotal) / (double)previousTotal * 100) : 0;

     // Active Users
   var totalUsers = await _context.Users.CountAsync();
       var activeUsers = await _context.Users.CountAsync(u => u.updated_at >= thirtyDaysAgo);
         var userChangePercent = previousTotal > 0 ?
         ((activeUsers) / (double)totalUsers * 100) : 0;

  // Available Licenses
         var availableChangePercent = totalLicenses > 0 ?
               ((availableLicenses) / (double)totalLicenses * 100) : 0;

    // Success Rate (from logs)
  var totalLogs = await _context.logs.CountAsync(l => l.created_at >= thirtyDaysAgo);
        var successLogs = await _context.logs.CountAsync(l => 
 l.created_at >= thirtyDaysAgo && l.log_level == "Info");
            var successRate = totalLogs > 0 ? (successLogs / (double)totalLogs * 100) : 99.2;

                return Ok(new EnhancedDashboardOverviewDto
                {
WelcomeMessage = $"Welcome back, {userEmail}",
       Metrics = new DashboardMetricsDto
          {
    TotalLicenses = new MetricDto
      {
           Value = totalLicenses,
     Label = "Total Licenses",
       ChangePercent = Math.Round(licenseChangePercent, 1),
 ChangeDirection = licenseChangePercent >= 0 ? "up" : "down",
  Icon = "license"
          },
     ActiveUsers = new MetricDto
  {
         Value = activeUsers,
            Label = "Active Users",
      ChangePercent = Math.Round(userChangePercent, 1),
   ChangeDirection = userChangePercent >= 0 ? "up" : "down",
   Icon = "users"
              },
            AvailableLicenses = new MetricDto
     {
            Value = availableLicenses,
     Label = "Available Licenses",
     ChangePercent = Math.Round(availableChangePercent, 1),
          ChangeDirection = "down", // Less available is typically negative
    Icon = "license-available"
        },
      SuccessRate = new MetricDto
            {
        Value = (int)Math.Round(successRate, 0),
             Label = "Success Rate",
          ChangePercent = 0.3,
     ChangeDirection = "up",
  Icon = "success",
       Unit = "%"
      }
  },
        TotalUsers = totalUsers,
      ActiveUsers = activeUsers,
         TotalMachines = totalLicenses,
     ActiveMachines = activeLicenses
      });
            }
          catch (Exception ex)
          {
       _logger.LogError(ex, "Error getting dashboard overview");
      return StatusCode(500, new { message = "Error retrieving dashboard data", error = ex.Message });
       }
 }

        /// <summary>
/// Get Groups and Users management view
      /// </summary>
        [HttpGet("groups-users")]
        public async Task<ActionResult<GroupsUsersOverviewDto>> GetGroupsAndUsers()
      {
            try
    {
   var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userEmail))
       {
        return Unauthorized(new { message = "User not authenticated" });
 }

      var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

            if (!await _authService.HasPermissionAsync(userEmail, "READ_ALL_USERS", isSubuser))
          {
               return StatusCode(403, new { message = "Insufficient permissions" });
          }

           // Get groups (using roles as groups for now)
     var groups = await _context.Roles
                .OrderBy(r => r.RoleName)
        .Select(r => new DashboardGroupDto
            {
           GroupName = r.RoleName,
    Description = r.Description,
    Licenses = _context.UserRoles.Count(ur => ur.RoleId == r.RoleId),
     DateCreated = r.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss")
               })
    .ToListAsync();

    return Ok(new GroupsUsersOverviewDto
        {
Groups = groups,
            TotalGroups = groups.Count
           });
     }
            catch (Exception ex)
      {
             _logger.LogError(ex, "Error getting groups and users");
         return StatusCode(500, new { message = "Error retrieving groups data", error = ex.Message });
       }
      }

        /// <summary>
  /// Get Recent Reports for dashboard
        /// </summary>
        [HttpGet("recent-reports")]
        public async Task<ActionResult<List<RecentReportDto>>> GetRecentReports([FromQuery] int count = 4)
        {
            try
            {
        var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
        return Unauthorized(new { message = "User not authenticated" });
                }

      var reports = await _context.AuditReports
       .OrderByDescending(r => r.report_datetime)
        .Take(count)
        .Select(r => new RecentReportDto
                {
  ReportId = r.report_id.ToString(),
     ReportName = r.report_name,
           ErasureMethod = r.erasure_method,
  ReportDate = r.report_datetime,
         Day = r.report_datetime.DayOfWeek.ToString().Substring(0, 3),
         DeviceCount = GetDeviceCount(r.report_details_json)
              })
    .ToListAsync();

      return Ok(reports);
            }
            catch (Exception ex)
        {
      _logger.LogError(ex, "Error getting recent reports");
    return StatusCode(500, new { message = "Error retrieving recent reports", error = ex.Message });
            }
        }

        /// <summary>
        /// Get License Details for dashboard
        /// </summary>
        [HttpGet("license-details")]
        public async Task<ActionResult<List<LicenseDetailDto>>> GetLicenseDetails()
        {
         try
      {
         var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   if (string.IsNullOrEmpty(userEmail))
                {
   return Unauthorized(new { message = "User not authenticated" });
          }

            // Group licenses by product type (using os_version as product identifier)
    var licenseDetails = await _context.Machines
       .Where(m => m.license_activated)
          .GroupBy(m => m.os_version)
     .Select(g => new
         {
 Product = g.Key,
     TotalAvailable = g.Count(),
   TotalConsumed = g.Count(m => !string.IsNullOrEmpty(m.user_email)),
     Usage = g.Count(m => !string.IsNullOrEmpty(m.user_email))
    })
         .ToListAsync();

    var result = licenseDetails.Select(ld => new LicenseDetailDto
      {
   Product = GetProductName(ld.Product),
  TotalAvailable = ld.TotalAvailable,
           TotalConsumed = ld.TotalConsumed,
 Usage = ld.TotalAvailable > 0 ? (int)((ld.TotalConsumed / (double)ld.TotalAvailable) * 100) : 0
         }).ToList();

      return Ok(result);
            }
       catch (Exception ex)
        {
        _logger.LogError(ex, "Error getting license details");
                return StatusCode(500, new { message = "Error retrieving license details", error = ex.Message });
}
        }

    /// <summary>
        /// Get Quick Actions for dashboard
 /// </summary>
        [HttpGet("quick-actions")]
        public async Task<ActionResult<List<QuickActionDto>>> GetQuickActions()
        {
            try
      {
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userEmail))
   {
          return Unauthorized(new { message = "User not authenticated" });
       }

 var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

       var actions = new List<QuickActionDto>
     {
     new QuickActionDto
            {
              Id = "manage-users",
    Title = "Manage Users",
              Description = "Add, edit, or remove user accounts",
  Icon = "users",
           IconColor = "#4F46E5",
      Route = "/users",
       Enabled = await _authService.HasPermissionAsync(userEmail, "CREATE_USER", isSubuser)
         },
 new QuickActionDto
        {
    Id = "manage-groups",
   Title = "Manage Groups",
      Description = "Create and manage user groups",
           Icon = "group",
              IconColor = "#7C3AED",
     Route = "/groups",
      Enabled = await _authService.HasPermissionAsync(userEmail, "MANAGE_GROUPS", isSubuser)
                },
           new QuickActionDto
     {
          Id = "admin-reports",
         Title = "Admin Reports",
               Description = "Generate and manage admin reports",
              Icon = "report",
       IconColor = "#10B981",
   Route = "/reports",
      Enabled = await _authService.HasPermissionAsync(userEmail, "VIEW_REPORTS", isSubuser)
           },
       new QuickActionDto
         {
       Id = "system-settings",
     Title = "System Settings",
            Description = "Configure system preferences",
          Icon = "settings",
     IconColor = "#8B5CF6",
      Route = "/settings",
       Enabled = await _authService.HasPermissionAsync(userEmail, "MANAGE_SYSTEM_SETTINGS", isSubuser)
        }
        };

       return Ok(actions);
        }
        catch (Exception ex)
            {
_logger.LogError(ex, "Error getting quick actions");
     return StatusCode(500, new { message = "Error retrieving quick actions", error = ex.Message });
     }
        }

 /// <summary>
     /// Get License Management section data
        /// </summary>
        [HttpGet("license-management")]
      public async Task<ActionResult<LicenseManagementDto>> GetLicenseManagement()
        {
          try
   {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userEmail))
         {
          return Unauthorized(new { message = "User not authenticated" });
  }

 var totalLicenses = await _context.Machines.CountAsync();
         var assignedLicenses = await _context.Machines.CountAsync(m => !string.IsNullOrEmpty(m.user_email));

    return Ok(new LicenseManagementDto
  {
BulkAssignment = new BulkAssignmentDto
     {
      Title = "Bulk License Assignment",
         Description = "Assign licenses to multiple users at once with advanced options",
    Status = "Quick Setup",
           ProcessingStatus = "Batch Processing",
         TotalLicenses = totalLicenses,
          AssignedLicenses = assignedLicenses
            },
 LicenseAudit = new LicenseAuditDto
   {
    Title = "License Audit Report",
         Description = "Comprehensive analysis of license usage and optimization insights",
           Status = "Detailed Analytics",
                AnalysisStatus = "Export Available",
     TotalLicenses = totalLicenses,
      OptimizationScore = CalculateOptimizationScore(totalLicenses, assignedLicenses)
     }
    });
}
     catch (Exception ex)
            {
      _logger.LogError(ex, "Error getting license management");
     return StatusCode(500, new { message = "Error retrieving license management data", error = ex.Message });
            }
        }

        /// <summary>
      /// Get User Activity Timeline
     /// </summary>
      [HttpGet("user-activity")]
   public async Task<ActionResult<List<UserActivityDto>>> GetUserActivity([FromQuery] int days = 7)
        {
    try
       {
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userEmail))
   {
   return Unauthorized(new { message = "User not authenticated" });
        }

     var sinceDate = DateTime.UtcNow.AddDays(-days);

     var activities = await _context.logs
              .Where(l => l.created_at >= sinceDate)
          .OrderByDescending(l => l.created_at)
 .Take(50)
    .Select(l => new UserActivityDto
    {
     Id = l.log_id.ToString(),
UserEmail = l.user_email ?? "System",
    Activity = l.log_message ?? "No description",
       ActivityType = l.log_level ?? "Info",
     Timestamp = l.created_at,
  Icon = GetActivityIcon(l.log_level),
          Color = GetActivityColor(l.log_level)
      })
   .ToListAsync();

         return Ok(activities);
          }
     catch (Exception ex)
 {
        _logger.LogError(ex, "Error getting user activity");
       return StatusCode(500, new { message = "Error retrieving user activity", error = ex.Message });
   }
 }

        /// <summary>
 /// Get Dashboard Statistics Summary
        /// </summary>
        [HttpGet("statistics")]
  public async Task<ActionResult<DashboardStatisticsDto>> GetStatistics()
      {
            try
      {
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userEmail))
      {
        return Unauthorized(new { message = "User not authenticated" });
        }

          var stats = new DashboardStatisticsDto
   {
        TotalUsers = await _context.Users.CountAsync(),
      TotalSubusers = await _context.subuser.CountAsync(),
  TotalMachines = await _context.Machines.CountAsync(),
  TotalReports = await _context.AuditReports.CountAsync(),
        TotalSessions = await _context.Sessions.CountAsync(),
          ActiveSessions = await _context.Sessions.CountAsync(s => s.session_status == "active"),
   TotalLogs = await _context.logs.CountAsync(),
        TotalRoles = await _context.Roles.CountAsync(),
                  TotalPermissions = await _context.Permissions.CountAsync()
       };

          return Ok(stats);
 }
            catch (Exception ex)
            {
     _logger.LogError(ex, "Error getting statistics");
         return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
            }
        }

        #region Private Helper Methods

   private int GetDeviceCount(string reportDetailsJson)
     {
            try
  {
                // Parse JSON to get device count
if (string.IsNullOrEmpty(reportDetailsJson)) return 0;
     
     // Simple estimation based on JSON complexity
           return reportDetailsJson.Split("device").Length - 1;
            }
  catch
         {
     return 1;
 }
        }

        private string GetProductName(string osVersion)
        {
     if (string.IsNullOrEmpty(osVersion)) return "Unknown Product";
    
            if (osVersion.Contains("Windows")) return "DSecure Drive Eraser";
            if (osVersion.Contains("Mac") || osVersion.Contains("Darwin")) return "DSecure Network Eraser";
            if (osVersion.Contains("Linux")) return "DSecure Mobile Diagnostics";
          
   return "DSecure Hardware Diagnostics";
     }

    private int CalculateOptimizationScore(int total, int assigned)
   {
          if (total == 0) return 0;
            var utilization = (assigned / (double)total) * 100;
      
            // Optimal utilization is 70-85%
if (utilization >= 70 && utilization <= 85) return 95;
            if (utilization >= 60 && utilization < 70) return 85;
      if (utilization > 85 && utilization <= 95) return 80;
    
         return 70;
        }

        private string GetActivityIcon(string? logLevel)
 {
         return logLevel?.ToLower() switch
            {
       "info" => "info",
  "warning" => "warning",
    "error" => "error",
     "critical" => "critical",
         _ => "info"
       };
        }

        private string GetActivityColor(string? logLevel)
        {
            return logLevel?.ToLower() switch
    {
     "info" => "#10B981",
                "warning" => "#F59E0B",
                "error" => "#EF4444",
     "critical" => "#DC2626",
                _ => "#6B7280"
          };
        }

        #endregion
    }
}
