using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Subusers Management Controller - Complete subuser management
    /// Based on BitRaser Manage Subusers UI (Screenshot 2)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SubusersManagementController2 : ControllerBase
    {
   private readonly ApplicationDbContext _context;
     private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
   private readonly ILogger<SubusersManagementController2> _logger;
   private readonly ICacheService _cacheService;

  public SubusersManagementController2(
      ApplicationDbContext context,
   IRoleBasedAuthService authService,
IUserDataService userDataService,
 ILogger<SubusersManagementController2> logger,
 ICacheService cacheService)
        {
        _context = context;
       _authService = authService;
   _userDataService = userDataService;
         _logger = logger;
         _cacheService = cacheService;
        }

        /// <summary>
        /// POST /api/SubusersManagement/list - Get filtered subusers list
        /// Implements all filters from Screenshot 2: Search, Role, Status, Department
        /// </summary>
    [HttpPost("list")]
public async Task<ActionResult<SubusersManagementListDto>> GetSubusersList(
   [FromBody] SubusersManagementFiltersDto filters)
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
        bool canViewAll = await _authService.HasPermissionAsync(userEmail, "READ_ALL_SUBUSERS", isSubuser);
     bool canViewOwn = await _authService.HasPermissionAsync(userEmail, "READ_USER_SUBUSERS", isSubuser);

   if (!canViewAll && !canViewOwn)
            {
 return StatusCode(403, new { message = "Insufficient permissions to view subusers" });
     }

// Start with base query
    var query = _context.subuser.AsQueryable();

// Apply user filter
       if (!canViewAll)
        {
       var user = await _context.Users.Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
       if (user != null)
        {
   query = query.Where(s => s.user_email == userEmail);
 }
 }

  // Apply search filter
     if (!string.IsNullOrEmpty(filters.Search))
       {
   query = query.Where(s => 
  s.subuser_email.Contains(filters.Search) ||
      (s.Name != null && s.Name.Contains(filters.Search)) ||
    (s.Department != null && s.Department.Contains(filters.Search)));
 }

       // Apply role filter
 if (!string.IsNullOrEmpty(filters.Role) && filters.Role != "All Roles")
       {
  query = query.Where(s => s.Role.ToLower() == filters.Role.ToLower());
       }

    // Apply status filter
     if (!string.IsNullOrEmpty(filters.Status) && filters.Status != "All Statuses")
  {
    query = query.Where(s => s.status.ToLower() == filters.Status.ToLower());
  }

     // Apply department filter
     if (!string.IsNullOrEmpty(filters.Department) && filters.Department != "All Departments")
   {
      query = query.Where(s => s.Department == filters.Department);
   }

    // Get total count before pagination
       var totalCount = await query.CountAsync();

  // Apply sorting
     query = ApplySorting(query, filters.SortBy, filters.SortDirection);

       // Apply pagination
       var subusers = await query
      .Skip((filters.Page - 1) * filters.PageSize)
  .Take(filters.PageSize)
    .Select(s => new SubuserManagementItemDto
      {
   SubuserId = s.subuser_id,
   Email = s.subuser_email,
   Role = s.Role,
Status = s.status,
      Department = s.Department ?? "N/A",
  LastLogin = s.last_login,
       CanView = true,
CanEdit = true,
      CanManagePermissions = true,
      CanReset = true,
     CanDeactivate = s.status == "active",
 CanDelete = true
   })
  .ToListAsync();

return Ok(new SubusersManagementListDto
        {
          Subusers = subusers,
TotalCount = totalCount,
Page = filters.Page,
        PageSize = filters.PageSize
       });
 }
       catch (Exception ex)
{
       _logger.LogError(ex, "Error retrieving subusers list");
      return StatusCode(500, new { message = "Error retrieving subusers", error = ex.Message });
 }
 }

     /// <summary>
        /// POST /api/SubusersManagement/deactivate - Deactivate a subuser
        /// </summary>
    [HttpPost("deactivate")]
    public async Task<IActionResult> DeactivateSubuser([FromBody] DeactivateSubuserRequest request)
      {
       try
     {
       var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   if (string.IsNullOrEmpty(userEmail))
   {
     return Unauthorized(new { message = "User not authenticated" });
  }

 var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

   if (!await _authService.HasPermissionAsync(userEmail, "UPDATE_ALL_SUBUSERS", isSubuser))
        {
      return StatusCode(403, new { message = "Insufficient permissions to deactivate subusers" });
   }

     var subuser = await _context.subuser.Where(s => s.subuser_id == request.SubuserId).FirstOrDefaultAsync();
      if (subuser == null)
{
     return NotFound(new { message = "Subuser not found" });
      }

 subuser.status = "inactive";
  subuser.last_login = DateTime.UtcNow;

     await _context.SaveChangesAsync();

  _logger.LogInformation("Subuser {Email} deactivated by {Admin}", subuser.subuser_email, userEmail);

     return Ok(new { message = "Subuser deactivated successfully" });
 }
       catch (Exception ex)
      {
   _logger.LogError(ex, "Error deactivating subuser");
 return StatusCode(500, new { message = "Error deactivating subuser" });
 }
        }

    /// <summary>
        /// POST /api/SubusersManagement/reset-password - Reset subuser password
        /// </summary>
  [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetSubuserPasswordRequest request)
        {
     try
    {
       var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     if (string.IsNullOrEmpty(userEmail))
       {
            return Unauthorized(new { message = "User not authenticated" });
     }

   var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

            if (!await _authService.HasPermissionAsync(userEmail, "CHANGE_ALL_SUBUSER_PASSWORDS", isSubuser))
{
 return StatusCode(403, new { message = "Insufficient permissions to reset passwords" });
       }

   var subuser = await _context.subuser.Where(s => s.subuser_id == request.SubuserId).FirstOrDefaultAsync();
if (subuser == null)
       {
  return NotFound(new { message = "Subuser not found" });
       }

       // Generate new password
    var newPassword = GenerateRandomPassword();
      var hashedPassword = BCrypt.Net.BCrypt.HashPassword(newPassword);

     subuser.subuser_password = hashedPassword;
   subuser.FailedLoginAttempts = 0;

    await _context.SaveChangesAsync();

       // TODO: Send email with new password if request.SendEmail is true

   _logger.LogInformation("Password reset for subuser {Email} by {Admin}", subuser.subuser_email, userEmail);

   return Ok(new 
    { 
    message = "Password reset successfully", 
     temporaryPassword = request.SendEmail ? null : newPassword 
});
 }
 catch (Exception ex)
        {
   _logger.LogError(ex, "Error resetting subuser password");
            return StatusCode(500, new { message = "Error resetting password" });
   }
   }

  /// <summary>
        /// POST /api/SubusersManagement/update-permissions - Update subuser permissions
        /// </summary>
        [HttpPost("update-permissions")]
  public async Task<IActionResult> UpdatePermissions([FromBody] UpdateSubuserPermissionsRequest request)
        {
        try
       {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userEmail))
      {
 return Unauthorized(new { message = "User not authenticated" });
}

       var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

    if (!await _authService.HasPermissionAsync(userEmail, "ASSIGN_ALL_SUBUSER_ROLES", isSubuser))
       {
    return StatusCode(403, new { message = "Insufficient permissions to update permissions" });
     }

 var subuser = await _context.subuser.Where(s => s.subuser_id == request.SubuserId).FirstOrDefaultAsync();
       if (subuser == null)
      {
    return NotFound(new { message = "Subuser not found" });
   }

     // Update permissions JSON
    subuser.PermissionsJson = System.Text.Json.JsonSerializer.Serialize(request.Permissions);

   await _context.SaveChangesAsync();

       _logger.LogInformation("Permissions updated for subuser {Email} by {Admin}", subuser.subuser_email, userEmail);

   return Ok(new { message = "Permissions updated successfully" });
      }
       catch (Exception ex)
   {
 _logger.LogError(ex, "Error updating subuser permissions");
            return StatusCode(500, new { message = "Error updating permissions" });
}
        }

        /// <summary>
   /// POST /api/SubusersManagement/export - Export subusers
  /// </summary>
     [HttpPost("export")]
public async Task<IActionResult> ExportSubusers([FromBody] ExportSubusersRequest request)
        {
     try
       {
       var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
  {
       return Unauthorized(new { message = "User not authenticated" });
       }

    var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

       if (!await _authService.HasPermissionAsync(userEmail, "EXPORT_USER_DATA", isSubuser))
            {
  return StatusCode(403, new { message = "Insufficient permissions to export" });
      }

   // Generate export file
            var fileName = $"Subusers_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.ExportFormat.ToLower()}";

     // TODO: Implement actual export logic

       _logger.LogInformation("Subusers exported by {Email}", userEmail);

return Ok(new 
            { 
    success = true, 
message = "Export generated successfully",
downloadUrl = $"/api/SubusersManagement/download/{fileName}",
    fileName = fileName
  });
       }
      catch (Exception ex)
       {
       _logger.LogError(ex, "Error exporting subusers");
            return StatusCode(500, new { message = "Error exporting subusers" });
       }
 }

   /// <summary>
        /// GET /api/SubusersManagement/statistics - Get subusers statistics
   /// </summary>
[HttpGet("statistics")]
        public async Task<ActionResult<SubusersStatisticsDto>> GetStatistics()
  {
       try
       {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
       {
    return Unauthorized(new { message = "User not authenticated" });
  }

       var query = _context.subuser.AsQueryable();

     var stats = new SubusersStatisticsDto
   {
    TotalSubusers = await query.CountAsync(),
   ActiveSubusers = await query.CountAsync(s => s.status == "active"),
       InactiveSubusers = await query.CountAsync(s => s.status == "inactive"),
  PendingSubusers = await query.CountAsync(s => s.status == "pending"),
      SubusersByRole = await query
    .GroupBy(s => s.Role ?? "Unknown")
       .Select(g => new { Role = g.Key, Count = g.Count() })
    .ToDictionaryAsync(x => x.Role ?? "Unknown", x => x.Count)
     };

    return Ok(stats);
       }
    catch (Exception ex)
     {
       _logger.LogError(ex, "Error retrieving subusers statistics");
  return StatusCode(500, new { message = "Error retrieving statistics" });
       }
 }

   /// <summary>
        /// GET /api/SubusersManagement/filter-options - Get available filter options
        /// </summary>
 [HttpGet("filter-options")]
public ActionResult<SubusersFilterOptionsDto> GetFilterOptions()
        {
   return Ok(new SubusersFilterOptionsDto());
        }

   #region Private Helper Methods

private IQueryable<subuser> ApplySorting(IQueryable<subuser> query, string? sortBy, int direction)
   {
     var ascending = direction == 1;

       return sortBy switch
       {
       "Email" => ascending ? query.OrderBy(s => s.subuser_email) : query.OrderByDescending(s => s.subuser_email),
   "Role" => ascending ? query.OrderBy(s => s.Role) : query.OrderByDescending(s => s.Role),
  "Status" => ascending ? query.OrderBy(s => s.status) : query.OrderByDescending(s => s.status),
 "Department" => ascending ? query.OrderBy(s => s.Department) : query.OrderByDescending(s => s.Department),
       "Last Login" => ascending ? query.OrderBy(s => s.last_login) : query.OrderByDescending(s => s.last_login),
_ => query.OrderBy(s => s.subuser_email)
     };
 }

    private string GenerateRandomPassword()
 {
       const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
       var random = new Random();
return new string(Enumerable.Repeat(chars, 12)
    .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        #endregion
    }
}
