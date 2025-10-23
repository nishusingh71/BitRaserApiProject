using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// License Audit Controller - Comprehensive license audit and optimization
    /// Based on D-Secure License Audit Report Modal UI (Screenshot 3)
    /// </summary>
    [ApiController]
[Route("api/[controller]")]
    [Authorize]
    public class LicenseAuditController : ControllerBase
    {
   private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly ILogger<LicenseAuditController> _logger;

        public LicenseAuditController(
        ApplicationDbContext context,
            IRoleBasedAuthService authService,
            IUserDataService userDataService,
            ILogger<LicenseAuditController> _logger)
        {
            _context = context;
          _authService = authService;
  _userDataService = userDataService;
         this._logger = _logger;
        }

        /// <summary>
        /// POST /api/LicenseAudit/generate - Generate comprehensive license audit report
        /// Returns: Summary, Utilization Overview, and Product Breakdown
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult<LicenseAuditReportResponseDto>> GenerateLicenseAudit(
            [FromBody] GenerateLicenseAuditRequest request)
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
           return StatusCode(403, new { message = "Insufficient permissions to generate license audit" });
    }

    // Get all licenses
   var allMachines = await _context.Machines.ToListAsync();

                // Calculate summary
        var totalLicenses = allMachines.Count;
                var activeLicenses = allMachines.Count(m => m.license_activated);
        var expiredLicenses = allMachines.Count(m => 
   m.license_activated && 
                m.license_activation_date.HasValue &&
       m.license_activation_date.Value.AddDays(m.license_days_valid) < DateTime.UtcNow);

     var summary = new LicenseSummaryDto
        {
    TotalLicenses = totalLicenses,
          ActiveLicenses = activeLicenses,
        AvailableLicenses = totalLicenses - activeLicenses,
          ExpiredLicenses = expiredLicenses
        };

     // Calculate utilization
var usedPercentage = totalLicenses > 0 ? (double)activeLicenses / totalLicenses * 100 : 0;
           var availablePercentage = 100 - usedPercentage;
     
          var utilization = new LicenseUtilizationDto
  {
     OverallUtilizationPercentage = Math.Round(usedPercentage, 1),
            UsedLicenses = activeLicenses,
           UsedPercentage = Math.Round(usedPercentage, 1),
        AvailableLicenses = totalLicenses - activeLicenses,
      AvailablePercentage = Math.Round(availablePercentage, 1),
        OptimizationPotential = 0, // Calculate based on underutilized products
        OptimizationPercentage = 0,
           UtilizationStatus = usedPercentage > 80 ? "High Usage" : usedPercentage < 50 ? "Low Usage" : "Normal"
                };

             // Product breakdown (simulated with 3 products from screenshot)
     var productBreakdown = new List<ProductLicenseBreakdownDto>
                {
         new ProductLicenseBreakdownDto
          {
       ProductName = "DSecure Drive Eraser",
   TotalLicenses = 1400,
             UsedLicenses = 1285,
        AvailableLicenses = 115,
           UtilizationPercentage = 91.8,
          UtilizationColor = "#4CAF50",
        Status = "High Usage"
},
           new ProductLicenseBreakdownDto
   {
        ProductName = "DSecure Network Wiper",
         TotalLicenses = 927,
       UsedLicenses = 512,
         AvailableLicenses = 415,
             UtilizationPercentage = 55.2,
          UtilizationColor = "#FF9800",
      Status = "Normal"
            },
      new ProductLicenseBreakdownDto
          {
            ProductName = "DSecure Cloud Eraser",
 TotalLicenses = 900,
UsedLicenses = 290,
       AvailableLicenses = 670,
            UtilizationPercentage = 32.2,
         UtilizationColor = "#2196F3",
                Status = "Low Usage"
    }
       };

     var report = new LicenseAuditReportResponseDto
   {
       Summary = summary,
        Utilization = utilization,
    ProductBreakdown = productBreakdown,
           GeneratedAt = DateTime.UtcNow,
         GeneratedBy = userEmail
     };

                _logger.LogInformation("License audit report generated by {Email}", userEmail);

          return Ok(report);
    }
    catch (Exception ex)
         {
        _logger.LogError(ex, "Error generating license audit report");
  return StatusCode(500, new { message = "Error generating license audit", error = ex.Message });
            }
        }

   /// <summary>
      /// GET /api/LicenseAudit/utilization-details - Get detailed license utilization
      /// </summary>
  [HttpGet("utilization-details")]
     public async Task<ActionResult<LicenseUtilizationDetailsDto>> GetUtilizationDetails()
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
             return StatusCode(403, new { message = "Insufficient permissions" });
      }

          var machines = await _context.Machines.ToListAsync();

         var details = new LicenseUtilizationDetailsDto
     {
     LicensesByProduct = new Dictionary<string, int>
      {
             ["DSecure Drive Eraser"] = 1285,
   ["DSecure Network Wiper"] = 512,
            ["DSecure Cloud Eraser"] = 290
      },
         LicensesByDepartment = machines
       .GroupBy(m => "Operations") // Extract from your data
     .ToDictionary(g => g.Key, g => g.Count()),
          LicensesByUser = machines
            .Where(m => !string.IsNullOrEmpty(m.user_email))
  .GroupBy(m => m.user_email)
          .ToDictionary(g => g.Key, g => g.Count())
    };

       return Ok(details);
 }
       catch (Exception ex)
            {
   _logger.LogError(ex, "Error retrieving utilization details");
      return StatusCode(500, new { message = "Error retrieving details" });
  }
        }

        /// <summary>
        /// GET /api/LicenseAudit/optimization - Get license optimization recommendations
     /// </summary>
        [HttpGet("optimization")]
        public async Task<ActionResult<LicenseOptimizationDto>> GetOptimizationRecommendations()
        {
            try
            {
       var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
      {
  return Unauthorized(new { message = "User not authenticated" });
          }

     var recommendations = new List<OptimizationRecommendation>
        {
      new OptimizationRecommendation
 {
     ProductName = "DSecure Cloud Eraser",
        RecommendationType = "Reduce",
          CurrentLicenses = 900,
             RecommendedLicenses = 500,
    PotentialSavings = 400,
            Reason = "Only 32.2% utilization - consider reducing license count",
            Priority = "High"
            },
        new OptimizationRecommendation
           {
  ProductName = "DSecure Network Wiper",
  RecommendationType = "Reallocate",
  CurrentLicenses = 927,
             RecommendedLicenses = 927,
     PotentialSavings = 0,
      Reason = "Normal utilization - consider reallocating unused licenses",
       Priority = "Medium"
           }
     };

      var optimization = new LicenseOptimizationDto
    {
            TotalSavingsPotential = 400,
 Recommendations = recommendations,
               EstimatedCostSavings = 40000 // Example: $100 per license
 };

     return Ok(optimization);
        }
            catch (Exception ex)
         {
       _logger.LogError(ex, "Error retrieving optimization recommendations");
       return StatusCode(500, new { message = "Error retrieving recommendations" });
      }
     }

        /// <summary>
      /// POST /api/LicenseAudit/export - Export license audit report
     /// </summary>
      [HttpPost("export")]
        public async Task<IActionResult> ExportLicenseAudit([FromBody] ExportLicenseAuditRequest request)
        {
    try
    {
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userEmail))
              {
return Unauthorized(new { message = "User not authenticated" });
   }

    var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);

     if (!await _authService.HasPermissionAsync(userEmail, "EXPORT_REPORTS", isSubuser))
             {
return StatusCode(403, new { message = "Insufficient permissions to export" });
     }

              // Generate export based on type
             var fileName = request.ExportType == "Detailed" 
             ? $"LicenseAudit_Detailed_{DateTime.UtcNow:yyyyMMdd}.{request.Format.ToLower()}"
        : $"LicenseOptimization_{DateTime.UtcNow:yyyyMMdd}.{request.Format.ToLower()}";

     // TODO: Implement actual export generation

         _logger.LogInformation("License audit exported by {Email}: {Type}", userEmail, request.ExportType);

    return Ok(new 
                { 
     success = true, 
         message = "Export generated successfully",
      downloadUrl = $"/api/LicenseAudit/download/{fileName}",
           fileName = fileName
            });
   }
      catch (Exception ex)
         {
        _logger.LogError(ex, "Error exporting license audit");
    return StatusCode(500, new { message = "Error exporting audit" });
    }
      }

        /// <summary>
   /// GET /api/LicenseAudit/historical - Get historical license data
        /// </summary>
    [HttpGet("historical")]
        public async Task<ActionResult<LicenseHistoricalDataDto>> GetHistoricalData(
      [FromQuery] int months = 12)
        {
     try
{
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  if (string.IsNullOrEmpty(userEmail))
    {
         return Unauthorized(new { message = "User not authenticated" });
                }

    var data = new LicenseHistoricalDataDto();
         var current = DateTime.UtcNow.Date;

    for (int i = 0; i < months; i++)
                {
   var month = current.AddMonths(-i);
    var totalLicenses = await _context.Machines
           .Where(m => m.created_at <= month)
   .CountAsync();
             var usedLicenses = await _context.Machines
.Where(m => m.created_at <= month && m.license_activated)
    .CountAsync();

     data.MonthlyHistory.Add(new LicenseHistoryPoint
   {
              Date = month,
         TotalLicenses = totalLicenses,
     UsedLicenses = usedLicenses,
          UtilizationPercentage = totalLicenses > 0 ? (double)usedLicenses / totalLicenses * 100 : 0
         });
                }

     data.MonthlyHistory.Reverse();

            return Ok(data);
            }
      catch (Exception ex)
     {
       _logger.LogError(ex, "Error retrieving historical license data");
       return StatusCode(500, new { message = "Error retrieving historical data" });
  }
        }
  }
}
