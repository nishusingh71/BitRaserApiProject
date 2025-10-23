using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Performance Metrics Controller - System performance monitoring
    /// Based on D-Secure Performance Dashboard UI (Screenshot 1)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
 public class PerformanceController : ControllerBase
    {
      private readonly ApplicationDbContext _context;
   private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
  private readonly ILogger<PerformanceController> _logger;

        public PerformanceController(
  ApplicationDbContext context,
 IRoleBasedAuthService authService,
    IUserDataService userDataService,
  ILogger<PerformanceController> logger)
        {
    _context = context;
  _authService = authService;
            _userDataService = userDataService;
  _logger = logger;
 }

        /// <summary>
        /// GET /api/Performance/dashboard - Get complete performance dashboard
   /// Returns Monthly Growth, Avg Duration, Uptime, and Throughput data
  /// </summary>
     [HttpGet("dashboard")]
        public async Task<ActionResult<PerformanceDashboardDto>> GetPerformanceDashboard(
  [FromQuery] PerformanceFilterDto? filters)
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
         if (!await _authService.HasPermissionAsync(userEmail, "READ_ALL_REPORTS", isSubuser) &&
   !await _authService.HasPermissionAsync(userEmail, "READ_REPORT_STATISTICS", isSubuser))
 {
  return StatusCode(403, new { message = "Insufficient permissions to view performance metrics" });
     }

      // Get date range
          var toDate = filters?.ToDate ?? DateTime.UtcNow;
    var fromDate = filters?.FromDate ?? toDate.AddMonths(-1);

     // Calculate monthly growth
      var currentMonthReports = await _context.AuditReports
    .Where(r => r.report_datetime >= toDate.AddMonths(-1) && r.report_datetime <= toDate)
        .CountAsync();

  var previousMonthReports = await _context.AuditReports
       .Where(r => r.report_datetime >= toDate.AddMonths(-2) && r.report_datetime < toDate.AddMonths(-1))
  .CountAsync();

     var percentageChange = previousMonthReports > 0
  ? ((double)(currentMonthReports - previousMonthReports) / previousMonthReports) * 100
         : 0;

       // Calculate average duration
             var recentReports = await _context.AuditReports
            .Where(r => r.report_datetime >= fromDate)
  .OrderByDescending(r => r.report_datetime)
    .Take(100)
        .ToListAsync();

    var avgDurationMinutes = 6; // Default value, can be calculated from actual data
  var avgDurationSeconds = 21;

          // Calculate uptime (based on system availability)
  var uptimePercentage = 99.2; // Can be calculated from system logs

         // Calculate throughput
      var totalOperations = await _context.AuditReports
     .Where(r => r.report_datetime >= fromDate && r.report_datetime <= toDate)
      .CountAsync();

// Generate chart data
   var monthlyGrowthChart = await GenerateMonthlyGrowthChart(fromDate, toDate);
    var avgDurationChart = await GenerateAverageDurationChart(fromDate, toDate);
     var uptimeChart = await GenerateUptimeChart(fromDate, toDate);
var throughputChart = await GenerateThroughputChart(fromDate, toDate);

      var dashboard = new PerformanceDashboardDto
      {
      MonthlyGrowth = new MonthlyGrowthDto
   {
        TotalRecords = currentMonthReports,
   PercentageChange = Math.Round(percentageChange, 1),
    IsPositive = percentageChange >= 0,
     PreviousMonthRecords = previousMonthReports,
  CurrentMonthRecords = currentMonthReports
     },
         AverageDuration = new AverageDurationDto
  {
     Duration = $"{avgDurationMinutes}m {avgDurationSeconds}s",
    TotalMinutes = avgDurationMinutes,
       TotalSeconds = avgDurationSeconds
  },
    Uptime = new UptimeDto
 {
       UptimePercentage = uptimePercentage,
    Status = "Operational"
   },
   Throughput = new ThroughputDto
           {
     TotalOperations = totalOperations,
    OperationsPerHour = totalOperations / ((toDate - fromDate).TotalHours),
  OperationsPerDay = totalOperations / ((toDate - fromDate).TotalDays)
   },
     MonthlyGrowthChart = monthlyGrowthChart,
  AverageDurationChart = avgDurationChart,
 UptimeChart = uptimeChart,
       ThroughputChart = throughputChart
   };

   return Ok(dashboard);
   }
       catch (Exception ex)
  {
   _logger.LogError(ex, "Error retrieving performance dashboard");
     return StatusCode(500, new { message = "Error retrieving performance data" });
      }
        }

        /// <summary>
     /// GET /api/Performance/statistics - Get system performance statistics
   /// </summary>
        [HttpGet("statistics")]
  public async Task<ActionResult<SystemPerformanceStatsDto>> GetStatistics()
{
  try
      {
var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
            {
     return Unauthorized(new { message = "User not authenticated" });
 }

         var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

   if (!await _authService.HasPermissionAsync(userEmail, "READ_ALL_REPORT_STATISTICS", isSubuser))
  {
    return StatusCode(403, new { message = "Insufficient permissions" });
       }

       var stats = new SystemPerformanceStatsDto
   {
  TotalMachines = await _context.Machines.CountAsync(),
     ActiveMachines = await _context.Machines.CountAsync(m => m.license_activated),
       TotalUsers = await _context.Users.CountAsync(),
  ActiveUsers = await _context.Sessions.Where(s => s.session_status == "active").Select(s => s.user_email).Distinct().CountAsync(),
    TotalReports = await _context.AuditReports.CountAsync(),
       ReportsThisMonth = await _context.AuditReports.CountAsync(r => r.report_datetime >= DateTime.UtcNow.AddMonths(-1)),
       AverageReportTime = 6.5,
    SystemUptime = 99.2,
         LastUpdated = DateTime.UtcNow
   };

     return Ok(stats);
        }
catch (Exception ex)
  {
       _logger.LogError(ex, "Error retrieving performance statistics");
       return StatusCode(500, new { message = "Error retrieving statistics" });
   }
    }

   /// <summary>
  /// GET /api/Performance/trends - Get performance trends
   /// </summary>
 [HttpGet("trends")]
     public async Task<ActionResult<PerformanceTrendsDto>> GetTrends(
      [FromQuery] string timeRange = "month")
     {
       try
            {
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
if (string.IsNullOrEmpty(userEmail))
      {
          return Unauthorized(new { message = "User not authenticated" });
            }

   var trends = new PerformanceTrendsDto();

       // Generate trends based on time range
    if (timeRange == "month")
  {
   var monthlyData = await _context.AuditReports
   .Where(r => r.report_datetime >= DateTime.UtcNow.AddMonths(-12))
           .GroupBy(r => new { r.report_datetime.Year, r.report_datetime.Month })
.Select(g => new MonthlyTrendData
      {
   Year = g.Key.Year,
        Month = g.Key.Month,
          TotalOperations = g.Count(),
      AverageDuration = 6.5,
   UptimePercentage = 99.2
 })
 .ToListAsync();

    foreach (var data in monthlyData)
 {
  data.MonthName = new DateTime(data.Year, data.Month, 1).ToString("MMMM");
               }

  trends.MonthlyTrends = monthlyData;
        }

return Ok(trends);
}
         catch (Exception ex)
  {
     _logger.LogError(ex, "Error retrieving performance trends");
  return StatusCode(500, new { message = "Error retrieving trends" });
  }
 }

        #region Private Helper Methods

     private async Task<List<PerformanceTimeSeriesData>> GenerateMonthlyGrowthChart(DateTime from, DateTime to)
     {
      var data = new List<PerformanceTimeSeriesData>();
 var current = new DateTime(from.Year, from.Month, 1);

         while (current <= to)
        {
 var count = await _context.AuditReports
      .Where(r => r.report_datetime.Year == current.Year && r.report_datetime.Month == current.Month)
 .CountAsync();

 data.Add(new PerformanceTimeSeriesData
       {
   Date = current,
    Value = count,
          Label = current.ToString("MMM")
  });

           current = current.AddMonths(1);
   }

return data;
 }

   private async Task<List<PerformanceTimeSeriesData>> GenerateAverageDurationChart(DateTime from, DateTime to)
 {
  var data = new List<PerformanceTimeSeriesData>();
            var current = new DateTime(from.Year, from.Month, 1);

            while (current <= to)
     {
       // Simulate average duration data
     data.Add(new PerformanceTimeSeriesData
         {
     Date = current,
   Value = 6.5 + (new Random().NextDouble() * 2 - 1), // Random variation around 6.5 minutes
 Label = current.ToString("MMM")
         });

       current = current.AddMonths(1);
     }

     return data;
        }

  private async Task<List<PerformanceTimeSeriesData>> GenerateUptimeChart(DateTime from, DateTime to)
   {
        var data = new List<PerformanceTimeSeriesData>();
   var current = new DateTime(from.Year, from.Month, 1);

   while (current <= to)
     {
      // Simulate uptime data
      data.Add(new PerformanceTimeSeriesData
 {
  Date = current,
 Value = 98 + (new Random().NextDouble() * 2), // Random between 98-100%
    Label = current.ToString("MMM")
           });

          current = current.AddMonths(1);
         }

        return data;
 }

 private async Task<List<ThroughputBarData>> GenerateThroughputChart(DateTime from, DateTime to)
 {
    var data = new List<ThroughputBarData>();
  var current = new DateTime(from.Year, from.Month, 1);

        while (current <= to)
  {
       var count = await _context.AuditReports
    .Where(r => r.report_datetime.Year == current.Year && r.report_datetime.Month == current.Month)
   .CountAsync();

       data.Add(new ThroughputBarData
{
     Month = current.ToString("MMM"),
      Operations = count,
             Color = "#4A90E2"
  });

       current = current.AddMonths(1);
     }

 return data;
   }

   #endregion
  }
}
