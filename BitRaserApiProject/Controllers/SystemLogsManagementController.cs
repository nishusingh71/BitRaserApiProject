using DSecureApi.Models;
using DSecureApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DSecureApi.Controllers
{
    /// <summary>
    /// System Logs Management Controller - Complete logs management
    /// Based on BitRaser System Logs UI (Screenshot 1)
    /// </summary>
  [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SystemLogsManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
  private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
  private readonly ILogger<SystemLogsManagementController> _logger;

public SystemLogsManagementController(
        ApplicationDbContext context,
IRoleBasedAuthService authService,
    IUserDataService userDataService,
    ILogger<SystemLogsManagementController> logger)
    {
 _context = context;
          _authService = authService;
     _userDataService = userDataService;
_logger = logger;
        }

  /// <summary>
        /// POST /api/SystemLogsManagement/list - Get filtered system logs list
 /// Implements all filters from Screenshot 1: Search, Level, Category, Date Range
        /// </summary>
        [HttpPost("list")]
        public async Task<ActionResult<SystemLogsListDto>> GetSystemLogsList(
  [FromBody] SystemLogsFiltersDto filters)
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
 bool canViewAll = await _authService.HasPermissionAsync(userEmail, "READ_ALL_LOGS", isSubuser);
      bool canViewOwn = await _authService.HasPermissionAsync(userEmail, "READ_LOG", isSubuser);

     if (!canViewAll && !canViewOwn)
   {
    return StatusCode(403, new { message = "Insufficient permissions to view logs" });
      }

       // Start with base query
   var query = _context.logs.AsQueryable();

  // Apply user filter
  if (!canViewAll)
  {
            query = query.Where(l => l.user_email == userEmail);
        }

       // Apply search filter
     if (!string.IsNullOrEmpty(filters.Search))
       {
    query = query.Where(l => 
    l.log_message.Contains(filters.Search) ||
     l.user_email.Contains(filters.Search) ||
           l.log_level.Contains(filters.Search));
            }

       // Apply level filter
 if (!string.IsNullOrEmpty(filters.Level) && filters.Level != "All Levels")
     {
         query = query.Where(l => l.log_level.ToUpper() == filters.Level.ToUpper());
            }

      // Apply category filter (assuming category is part of log message or details)
       if (!string.IsNullOrEmpty(filters.Category) && filters.Category != "All Categories")
            {
    query = query.Where(l => l.log_message.Contains(filters.Category));
       }

         // Apply date range filter
       if (filters.FromDate.HasValue)
                {
      query = query.Where(l => l.created_at >= filters.FromDate.Value);
     }
     if (filters.ToDate.HasValue)
    {
 query = query.Where(l => l.created_at <= filters.ToDate.Value);
            }

        // Get total count before pagination
        var totalCount = await query.CountAsync();

 // Apply sorting
            query = ApplySorting(query, filters.SortBy, filters.SortDirection);

 // Apply pagination
                var logs = await query
             .Skip((filters.Page - 1) * filters.PageSize)
       .Take(filters.PageSize)
    .Select(l => new SystemLogItemDto
            {
   LogId = l.log_id,
     Level = l.log_level.ToUpper(),
EventType = ExtractEventTypeFromMessage(l.log_message),
       Timestamp = l.created_at,
    UserEmail = l.user_email ?? "System",
    Message = l.log_message,
      Source = ExtractSourceFromDetails(l.log_details_json),
  CanViewDetails = true
         })
    .ToListAsync();

            return Ok(new SystemLogsListDto
 {
         Logs = logs,
      TotalCount = totalCount,
             Page = filters.Page,
     PageSize = filters.PageSize
         });
   }
  catch (Exception ex)
   {
           _logger.LogError(ex, "Error retrieving system logs list");
            return StatusCode(500, new { message = "Error retrieving logs", error = ex.Message });
       }
        }

        /// <summary>
        /// GET /api/SystemLogsManagement/{logId} - Get single log details
        /// </summary>
        [HttpGet("{logId}")]
  public async Task<ActionResult<SystemLogDetailDto>> GetLogDetail(int logId)
 {
    try
            {
  var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userEmail))
     {
         return Unauthorized(new { message = "User not authenticated" });
 }

    var log = await _context.logs.Where(l => l.log_id == logId).FirstOrDefaultAsync();

       if (log == null)
            {
   return NotFound(new { message = "Log not found" });
   }

 var detail = new SystemLogDetailDto
      {
      LogId = log.log_id,
Level = log.log_level,
     EventType = ExtractEventTypeFromMessage(log.log_message),
        Timestamp = log.created_at,
   UserEmail = log.user_email ?? "System",
   Message = log.log_message,
    Source = ExtractSourceFromDetails(log.log_details_json)
 };

     return Ok(detail);
    }
     catch (Exception ex)
     {
   _logger.LogError(ex, "Error retrieving log detail");
  return StatusCode(500, new { message = "Error retrieving log detail" });
         }
        }

   /// <summary>
        /// POST /api/SystemLogsManagement/export - Export system logs
        /// </summary>
        [HttpPost("export")]
public async Task<ActionResult<ExportSystemLogsResponse>> ExportLogs(
      [FromBody] ExportSystemLogsRequest request)
{
       try
          {
    var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     if (string.IsNullOrEmpty(userEmail))
   {
       return Unauthorized(new { message = "User not authenticated" });
     }

var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

   if (!await _authService.HasPermissionAsync(userEmail, "EXPORT_LOGS", isSubuser))
       {
     return StatusCode(403, new { message = "Insufficient permissions to export logs" });
    }

   // Generate export file
            var fileName = $"SystemLogs_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.ExportFormat.ToLower()}";
    var filePath = Path.Combine("Exports", fileName);

                // Ensure directory exists
     Directory.CreateDirectory("Exports");

    // TODO: Implement actual export logic based on format

      return Ok(new ExportSystemLogsResponse
     {
       Success = true,
      Message = "Export generated successfully",
       DownloadUrl = $"/api/SystemLogsManagement/download/{fileName}",
           FileName = fileName,
   FileSizeBytes = 0,
     ExportedAt = DateTime.UtcNow
});
            }
            catch (Exception ex)
     {
       _logger.LogError(ex, "Error exporting system logs");
       return StatusCode(500, new { message = "Error exporting logs" });
            }
        }

        /// <summary>
        /// GET /api/SystemLogsManagement/statistics - Get logs statistics
        /// </summary>
 [HttpGet("statistics")]
  public async Task<ActionResult<SystemLogsStatisticsDto>> GetStatistics()
   {
    try
    {
  var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
 if (string.IsNullOrEmpty(userEmail))
      {
  return Unauthorized(new { message = "User not authenticated" });
          }

                var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);
       bool canViewAll = await _authService.HasPermissionAsync(userEmail, "READ_ALL_LOG_STATISTICS", isSubuser);

     var query = _context.logs.AsQueryable();

   if (!canViewAll)
          {
    query = query.Where(l => l.user_email == userEmail);
 }

 var now = DateTime.UtcNow;
     var startOfDay = now.Date;
  var startOfWeek = now.AddDays(-(int)now.DayOfWeek);

    var stats = new SystemLogsStatisticsDto
{
         TotalLogs = await query.CountAsync(),
          InfoLogs = await query.CountAsync(l => l.log_level.ToUpper() == "INFO"),
            SuccessLogs = await query.CountAsync(l => l.log_level.ToUpper() == "SUCCESS"),
          WarningLogs = await query.CountAsync(l => l.log_level.ToUpper() == "WARNING"),
            ErrorLogs = await query.CountAsync(l => l.log_level.ToUpper() == "ERROR"),
             CriticalLogs = await query.CountAsync(l => l.log_level.ToUpper() == "CRITICAL"),
      LogsToday = await query.CountAsync(l => l.created_at >= startOfDay),
     LogsThisWeek = await query.CountAsync(l => l.created_at >= startOfWeek),
       LogsByLevel = new Dictionary<string, int>
      {
["INFO"] = await query.CountAsync(l => l.log_level.ToUpper() == "INFO"),
          ["SUCCESS"] = await query.CountAsync(l => l.log_level.ToUpper() == "SUCCESS"),
        ["WARNING"] = await query.CountAsync(l => l.log_level.ToUpper() == "WARNING"),
            ["ERROR"] = await query.CountAsync(l => l.log_level.ToUpper() == "ERROR"),
      ["CRITICAL"] = await query.CountAsync(l => l.log_level.ToUpper() == "CRITICAL")
   }
     };

       return Ok(stats);
         }
      catch (Exception ex)
         {
     _logger.LogError(ex, "Error retrieving logs statistics");
return StatusCode(500, new { message = "Error retrieving statistics" });
    }
        }

        /// <summary>
        /// GET /api/SystemLogsManagement/filter-options - Get available filter options
      /// </summary>
        [HttpGet("filter-options")]
        public ActionResult<SystemLogsFilterOptionsDto> GetFilterOptions()
  {
            return Ok(new SystemLogsFilterOptionsDto());
  }

/// <summary>
   /// POST /api/SystemLogsManagement/clear - Clear old logs
        /// </summary>
        [HttpPost("clear")]
  public async Task<ActionResult<ClearLogsResponse>> ClearLogs([FromBody] ClearLogsRequest request)
        {
     try
            {
  var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userEmail))
  {
     return Unauthorized(new { message = "User not authenticated" });
         }

      var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

       if (!await _authService.HasPermissionAsync(userEmail, "CLEANUP_LOGS", isSubuser))
    {
    return StatusCode(403, new { message = "Insufficient permissions to clear logs" });
   }

      var query = _context.logs.AsQueryable();

       if (request.OlderThan.HasValue)
                {
     query = query.Where(l => l.created_at < request.OlderThan.Value);
 }

        if (!string.IsNullOrEmpty(request.Level))
 {
           query = query.Where(l => l.log_level.ToUpper() == request.Level.ToUpper());
                }

                var logsToDelete = await query.ToListAsync();
        var count = logsToDelete.Count;

_context.logs.RemoveRange(logsToDelete);
   await _context.SaveChangesAsync();

       _logger.LogInformation("{Count} logs cleared by {Email}", count, userEmail);

             return Ok(new ClearLogsResponse
 {
   Success = true,
Message = $"Successfully cleared {count} logs",
     LogsCleared = count
       });
       }
            catch (Exception ex)
{
     _logger.LogError(ex, "Error clearing logs");
    return StatusCode(500, new { message = "Error clearing logs" });
       }
   }

        #region Private Helper Methods

        private IQueryable<logs> ApplySorting(IQueryable<logs> query, string? sortBy, int direction)
     {
       var ascending = direction == 1;

 return sortBy switch
            {
       "Timestamp" => ascending ? query.OrderBy(l => l.created_at) : query.OrderByDescending(l => l.created_at),
     "Level" => ascending ? query.OrderBy(l => l.log_level) : query.OrderByDescending(l => l.log_level),
          "User" => ascending ? query.OrderBy(l => l.user_email) : query.OrderByDescending(l => l.user_email),
          _ => query.OrderByDescending(l => l.created_at)
       };
        }

    private string ExtractEventTypeFromMessage(string message)
        {
            // Simple extraction logic - can be enhanced
  if (message.Contains("API")) return "API";
            if (message.Contains("Data")) return "Data Erasure";
      if (message.Contains("Performance")) return "Performance";
   if (message.Contains("Auth")) return "Authentication";
  return "System";
        }

        private string ExtractSourceFromDetails(string? jsonDetails)
        {
     // TODO: Parse JSON to extract source
  return "API Gateway";
        }

   #endregion
    }
}
