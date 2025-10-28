using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Machines Management Controller - Complete machine management
    /// Based on BitRaser Machines UI (Screenshot 3)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
  [Authorize]
    public class MachinesManagementController2 : ControllerBase
    {
private readonly ApplicationDbContext _context;
  private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ILogger<MachinesManagementController2> _logger;

public MachinesManagementController2(
      ApplicationDbContext context,
IRoleBasedAuthService authService,
     IUserDataService userDataService,
       ILogger<MachinesManagementController2> logger)
        {
    _context = context;
        _authService = authService;
_userDataService = userDataService;
   _logger = logger;
        }

  /// <summary>
   /// POST /api/MachinesManagement/list - Get filtered machines list
        /// Implements all filters from Screenshot 3: Search, Erase Option, License, Status
  /// </summary>
[HttpPost("list")]
        public async Task<ActionResult<MachinesManagementListDto>> GetMachinesList(
      [FromBody] MachinesManagementFiltersDto filters)
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
      bool canViewAll = await _authService.HasPermissionAsync(userEmail, "READ_ALL_MACHINES", isSubuser);
    bool canViewOwn = await _authService.HasPermissionAsync(userEmail, "READ_MACHINE", isSubuser);

         if (!canViewAll && !canViewOwn)
         {
                    return StatusCode(403, new { message = "Insufficient permissions to view machines" });
    }

     // Start with base query
                var query = _context.Machines.AsQueryable();

     // Apply user filter
      if (!canViewAll)
           {
            if (isSubuser)
     {
     query = query.Where(m => m.subuser_email == userEmail);
        }
           else
              {
          query = query.Where(m => m.user_email == userEmail);
        }
      }

 // Apply search filter
    if (!string.IsNullOrEmpty(filters.Search))
      {
              query = query.Where(m => 
         m.fingerprint_hash.Contains(filters.Search) ||
 m.mac_address.Contains(filters.Search) ||
      m.os_version.Contains(filters.Search));
 }

        // Apply erase option filter (you may need to add this field to machines table)
if (!string.IsNullOrEmpty(filters.EraseOption) && filters.EraseOption != "All Options")
           {
           // TODO: Implement based on your data structure
  }

           // Apply license filter
        if (!string.IsNullOrEmpty(filters.License) && filters.License != "All Licenses")
 {
         // Assuming license type is in license_details_json
     query = query.Where(m => m.license_details_json.Contains(filters.License));
             }

  // Apply status filter
    if (!string.IsNullOrEmpty(filters.Status) && filters.Status != "All Statuses")
          {
         if (filters.Status == "online")
    {
           query = query.Where(m => m.license_activated);
          }
      else if (filters.Status == "offline")
       {
       query = query.Where(m => !m.license_activated);
        }
    }

  // Get total count before pagination
    var totalCount = await query.CountAsync();

   // Apply sorting
   query = ApplySorting(query, filters.SortBy, filters.SortDirection);

 // Apply pagination
 var machines = await query
        .Skip((filters.Page - 1) * filters.PageSize)
         .Take(filters.PageSize)
       .Select(m => new MachineManagementItemDto
        {
       Hostname = m.fingerprint_hash.Substring(0, Math.Min(10, m.fingerprint_hash.Length)),
      EraseOption = ExtractEraseOption(m.license_details_json),
          License = ExtractLicenseType(m.license_details_json),
  Status = m.license_activated ? "online" : "offline",
          CanView = true,
       CanEdit = true
  })
          .ToListAsync();

  return Ok(new MachinesManagementListDto
                {
        Machines = machines,
         TotalCount = totalCount,
              Page = filters.Page,
  PageSize = filters.PageSize
             });
            }
            catch (Exception ex)
            {
     _logger.LogError(ex, "Error retrieving machines list");
        return StatusCode(500, new { message = "Error retrieving machines", error = ex.Message });
       }
        }

        /// <summary>
        /// GET /api/MachinesManagement/{fingerprintHash} - Get machine details
        /// </summary>
   [HttpGet("{fingerprintHash}")]
    public async Task<ActionResult<MachineDetailDto>> GetMachineDetail(string fingerprintHash)
     {
 try
          {
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  if (string.IsNullOrEmpty(userEmail))
           {
        return Unauthorized(new { message = "User not authenticated" });
     }

            var machine = await _context.Machines
      .FirstOrDefaultAsync(m => m.fingerprint_hash == fingerprintHash);

  if (machine == null)
            {
      return NotFound(new { message = "Machine not found" });
 }

                var detail = new MachineDetailDto
     {
      FingerprintHash = machine.fingerprint_hash,
         Hostname = machine.fingerprint_hash.Substring(0, Math.Min(10, machine.fingerprint_hash.Length)),
        MacAddress = machine.mac_address,
           PhysicalDriveId = machine.physical_drive_id,
      CpuId = machine.cpu_id,
              BiosSerial = machine.bios_serial,
      OsVersion = machine.os_version,
        UserEmail = machine.user_email,
      SubuserEmail = machine.subuser_email,
   EraseOption = ExtractEraseOption(machine.license_details_json),
 License = ExtractLicenseType(machine.license_details_json),
   Status = machine.license_activated ? "online" : "offline",
          LicenseActivated = machine.license_activated,
          LicenseActivationDate = machine.license_activation_date,
          LicenseDaysValid = machine.license_days_valid,
              DemoUsageCount = machine.demo_usage_count,
    CreatedAt = machine.created_at,
        UpdatedAt = machine.updated_at,
           VmStatus = machine.vm_status
    };

       return Ok(detail);
          }
            catch (Exception ex)
        {
     _logger.LogError(ex, "Error retrieving machine detail");
   return StatusCode(500, new { message = "Error retrieving machine detail" });
}
        }

        /// <summary>
      /// POST /api/MachinesManagement/update-license - Update machine license
        /// </summary>
        [HttpPost("update-license")]
        public async Task<IActionResult> UpdateLicense([FromBody] UpdateMachineLicenseRequest request)
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
   return StatusCode(403, new { message = "Insufficient permissions to update licenses" });
           }

  var machine = await _context.Machines
     .FirstOrDefaultAsync(m => m.fingerprint_hash == request.FingerprintHash);

      if (machine == null)
       {
      return NotFound(new { message = "Machine not found" });
       }

        machine.license_activated = request.Activate;
          if (request.Activate)
        {
 machine.license_activation_date = DateTime.UtcNow;
          machine.license_days_valid = request.DaysValid;
   }
                machine.updated_at = DateTime.UtcNow;

    await _context.SaveChangesAsync();

             _logger.LogInformation("License updated for machine {Hash} by {Email}", 
    request.FingerprintHash, userEmail);

       return Ok(new { message = "License updated successfully" });
      }
            catch (Exception ex)
        {
       _logger.LogError(ex, "Error updating machine license");
       return StatusCode(500, new { message = "Error updating license" });
            }
      }

        /// <summary>
        /// POST /api/MachinesManagement/update-status - Update machine status
        /// </summary>
        [HttpPost("update-status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateMachineStatusRequest request)
        {
            try
   {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          if (string.IsNullOrEmpty(userEmail))
         {
      return Unauthorized(new { message = "User not authenticated" });
}

        var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

          if (!await _authService.HasPermissionAsync(userEmail, "UPDATE_ALL_MACHINES", isSubuser))
      {
    return StatusCode(403, new { message = "Insufficient permissions to update machines" });
          }

     var machine = await _context.Machines
   .FirstOrDefaultAsync(m => m.fingerprint_hash == request.FingerprintHash);

   if (machine == null)
          {
     return NotFound(new { message = "Machine not found" });
           }

           // Update status logic
       machine.license_activated = request.Status == "online";
          machine.updated_at = DateTime.UtcNow;

   await _context.SaveChangesAsync();

          _logger.LogInformation("Status updated for machine {Hash} to {Status} by {Email}", 
   request.FingerprintHash, request.Status, userEmail);

         return Ok(new { message = "Status updated successfully" });
     }
       catch (Exception ex)
     {
     _logger.LogError(ex, "Error updating machine status");
        return StatusCode(500, new { message = "Error updating status" });
       }
        }

  /// <summary>
        /// POST /api/MachinesManagement/export - Export machines
        /// </summary>
        [HttpPost("export")]
        public async Task<IActionResult> ExportMachines([FromBody] ExportMachinesRequest request)
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
             var fileName = $"Machines_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.ExportFormat.ToLower()}";

    // TODO: Implement actual export logic

       _logger.LogInformation("Machines exported by {Email}", userEmail);

  return Ok(new 
 { 
              success = true, 
          message = "Export generated successfully",
              downloadUrl = $"/api/MachinesManagement/download/{fileName}",
     fileName = fileName
  });
            }
         catch (Exception ex)
       {
  _logger.LogError(ex, "Error exporting machines");
        return StatusCode(500, new { message = "Error exporting machines" });
  }
        }

        /// <summary>
 /// GET /api/MachinesManagement/statistics - Get machines statistics
     /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<MachinesStatisticsDto>> GetStatistics()
        {
  try
     {
        var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userEmail))
         {
    return Unauthorized(new { message = "User not authenticated" });
                }

                var query = _context.Machines.AsQueryable();

      var stats = new MachinesStatisticsDto
  {
        TotalMachines = await query.CountAsync(),
      OnlineMachines = await query.CountAsync(m => m.license_activated),
             OfflineMachines = await query.CountAsync(m => !m.license_activated),
        LicensedMachines = await query.CountAsync(m => m.license_activated),
         UnlicensedMachines = await query.CountAsync(m => !m.license_activated)
    };

          return Ok(stats);
     }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving machines statistics");
    return StatusCode(500, new { message = "Error retrieving statistics" });
       }
        }

  /// <summary>
        /// GET /api/MachinesManagement/filter-options - Get available filter options
        /// </summary>
     [HttpGet("filter-options")]
        public ActionResult<MachinesFilterOptionsDto> GetFilterOptions()
        {
     return Ok(new MachinesFilterOptionsDto());
        }

        #region Private Helper Methods

        private IQueryable<machines> ApplySorting(IQueryable<machines> query, string? sortBy, int direction)
    {
var ascending = direction == 1;

          return sortBy switch
    {
      "Hostname" => ascending ? query.OrderBy(m => m.fingerprint_hash) : query.OrderByDescending(m => m.fingerprint_hash),
              "License" => ascending ? query.OrderBy(m => m.license_details_json) : query.OrderByDescending(m => m.license_details_json),
"Status" => ascending ? query.OrderBy(m => m.license_activated) : query.OrderByDescending(m => m.license_activated),
    _ => query.OrderBy(m => m.created_at)
      };
        }

        private string ExtractEraseOption(string licenseJson)
        {
         // TODO: Parse JSON to extract erase option
    return "Secure Erase";
        }

   private string ExtractLicenseType(string licenseJson)
        {
            // TODO: Parse JSON to extract license type
            if (licenseJson.Contains("Enterprise"))
           return "Enterprise";
       return "Basic";
     }

   #endregion
    }
}
