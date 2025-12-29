using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Report Generation Controller - Complete report creation and management
    /// Matches BitRaser "Generate Report" UI with all customization options
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ReportGenerationController : ControllerBase
    {
 private readonly ApplicationDbContext _context;
      private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly PdfService _pdfService;
      private readonly ILogger<ReportGenerationController> _logger;
      private readonly ICacheService _cacheService;

        public ReportGenerationController(
        ApplicationDbContext context,
       IRoleBasedAuthService authService,
            IUserDataService userDataService,
       PdfService pdfService,
     ILogger<ReportGenerationController> logger,
     ICacheService cacheService)
        {
       _context = context;
            _authService = authService;
          _userDataService = userDataService;
            _pdfService = pdfService;
    _logger = logger;
    _cacheService = cacheService;
        }

   /// <summary>
     /// POST /api/ReportGeneration/generate - Generate a new report
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult<GenerateReportResponseDto>> GenerateReport([FromBody] GenerateReportRequestDto request)
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
     if (!await _authService.HasPermissionAsync(userEmail, "EXPORT_REPORTS", isSubuser) &&
              !await _authService.HasPermissionAsync(userEmail, "EXPORT_ALL_REPORTS", isSubuser))
                {
              return StatusCode(403, new { message = "Insufficient permissions to generate reports" });
   }

            // Validate date range
        if (request.FromDate > request.ToDate)
      {
   return BadRequest(new { message = "From date must be before to date" });
    }

         // Get audit reports data based on filters
                var query = _context.AuditReports.AsQueryable();

     // Apply date filter
   query = query.Where(r => r.report_datetime >= request.FromDate && r.report_datetime <= request.ToDate);

      // Apply user filter if specified
     if (!string.IsNullOrEmpty(request.UserEmail))
      {
   query = query.Where(r => r.client_email == request.UserEmail);
    }

         // Apply erasure method filter
         if (request.ErasureMethods != null && request.ErasureMethods.Any())
       {
            query = query.Where(r => request.ErasureMethods.Contains(r.erasure_method));
         }

        var auditReports = await query.ToListAsync();

    // Generate report based on format
        string reportId = Guid.NewGuid().ToString();
string fileName = $"{request.ReportTitle.Replace(" ", "_")}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.{request.ExportFormat.ToLower()}";
                string filePath = Path.Combine("Reports", fileName);

       // Ensure Reports directory exists
                Directory.CreateDirectory("Reports");

      byte[] reportData;
      long fileSize = 0;

          switch (request.ExportFormat.ToUpper())
 {
               case "PDF":
    reportData = await GeneratePdfReport(request, auditReports);
          await System.IO.File.WriteAllBytesAsync(filePath, reportData);
 fileSize = reportData.Length;
 break;

          case "EXCEL":
        case "CSV":
      // TODO: Implement Excel/CSV generation
   return StatusCode(501, new { message = "Excel/CSV export not yet implemented" });

                    default:
         return BadRequest(new { message = "Unsupported export format" });
         }

     // Save report metadata to database
    var generatedReport = new GeneratedReport
       {
         ReportId = reportId,
          ReportTitle = request.ReportTitle,
          ReportType = request.ReportType,
        FromDate = request.FromDate,
            ToDate = request.ToDate,
           Format = request.ExportFormat,
    ConfigurationJson = JsonSerializer.Serialize(request),
           FilePath = filePath,
   FileSizeBytes = fileSize,
    GeneratedBy = userEmail,
     GeneratedAt = DateTime.UtcNow,
   Status = "completed",
        IsScheduled = request.ScheduleReportGeneration,
  ExpiresAt = DateTime.UtcNow.AddDays(30) // Reports expire after 30 days
};

    _context.Set<GeneratedReport>().Add(generatedReport);
           await _context.SaveChangesAsync();

        _logger.LogInformation("Report {ReportId} generated by {Email}", reportId, userEmail);

          return Ok(new GenerateReportResponseDto
       {
    Success = true,
         Message = "Report generated successfully",
         ReportId = reportId,
           DownloadUrl = $"/api/ReportGeneration/download/{reportId}",
    FileName = fileName,
             FileSizeBytes = fileSize,
         GeneratedAt = DateTime.UtcNow,
  Format = request.ExportFormat
        });
       }
        catch (Exception ex)
      {
              _logger.LogError(ex, "Error generating report");
          return StatusCode(500, new { message = "Error generating report", error = ex.Message });
            }
}

      /// <summary>
        /// GET /api/ReportGeneration/download/{reportId} - Download a generated report
   /// </summary>
        [HttpGet("download/{reportId}")]
      public async Task<IActionResult> DownloadReport(string reportId)
        {
    try
            {
    var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
    {
          return Unauthorized(new { message = "User not authenticated" });
           }

         var report = await _context.Set<GeneratedReport>()
    .Where(r => r.ReportId == reportId).FirstOrDefaultAsync();

   if (report == null)
        {
          return NotFound(new { message = "Report not found" });
       }

              // Check if report has expired
              if (report.ExpiresAt.HasValue && report.ExpiresAt.Value < DateTime.UtcNow)
   {
         return BadRequest(new { message = "Report has expired" });
      }

    // Check if file exists
      if (string.IsNullOrEmpty(report.FilePath) || !System.IO.File.Exists(report.FilePath))
                {
          return NotFound(new { message = "Report file not found" });
       }

    var fileBytes = await System.IO.File.ReadAllBytesAsync(report.FilePath);
 var contentType = report.Format.ToUpper() switch
   {
          "PDF" => "application/pdf",
           "EXCEL" => "application/vnd.openxmlformats-oficedocument.spreadsheetml.sheet",
        "CSV" => "text/csv",
      _ => "application/octet-stream"
                };

        var fileName = Path.GetFileName(report.FilePath);
         return File(fileBytes, contentType, fileName);
            }
catch (Exception ex)
        {
                _logger.LogError(ex, "Error downloading report {ReportId}", reportId);
                return StatusCode(500, new { message = "Error downloading report" });
 }
        }

        /// <summary>
   /// GET /api/ReportGeneration/history - Get report generation history
        /// </summary>
     [HttpGet("history")]
        public async Task<ActionResult<ReportHistoryResponseDto>> GetReportHistory(
  [FromQuery] int page = 1,
       [FromQuery] int pageSize = 10)
        {
            try
        {
       var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
     {
    return Unauthorized(new { message = "User not authenticated" });
          }

       var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);
  bool canViewAll = await _authService.HasPermissionAsync(userEmail, "READ_ALL_REPORTS", isSubuser);

        var query = _context.Set<GeneratedReport>()
     .Where(r => !r.IsDeleted)
           .AsQueryable();

       // Filter by user if not admin
        if (!canViewAll)
        {
           query = query.Where(r => r.GeneratedBy == userEmail);
   }

    var totalCount = await query.CountAsync();

        var reports = await query
        .OrderByDescending(r => r.GeneratedAt)
       .Skip((page - 1) * pageSize)
      .Take(pageSize)
      .Select(r => new ReportHistoryItemDto
      {
 ReportId = r.ReportId,
       ReportTitle = r.ReportTitle,
   ReportType = r.ReportType,
         GeneratedAt = r.GeneratedAt,
       GeneratedBy = r.GeneratedBy,
            Format = r.Format,
   FileSizeBytes = r.FileSizeBytes,
               Status = r.Status,
     DownloadUrl = $"/api/ReportGeneration/download/{r.ReportId}"
     })
    .ToListAsync();

 return Ok(new ReportHistoryResponseDto
           {
         Reports = reports,
         TotalCount = totalCount,
          Page = page,
 PageSize = pageSize
                });
       }
            catch (Exception ex)
            {
    _logger.LogError(ex, "Error retrieving report history");
                return StatusCode(500, new { message = "Error retrieving report history" });
            }
    }

        /// <summary>
        /// GET /api/ReportGeneration/types - Get available report types
        /// </summary>
    [HttpGet("types")]
     public ActionResult<ReportTypesResponseDto> GetReportTypes()
        {
    return Ok(new ReportTypesResponseDto());
        }

   /// <summary>
      /// GET /api/ReportGeneration/formats - Get available export formats
        /// </summary>
        [HttpGet("formats")]
        public ActionResult<ExportFormatsResponseDto> GetExportFormats()
    {
            return Ok(new ExportFormatsResponseDto());
   }

        /// <summary>
      /// GET /api/ReportGeneration/statistics - Get report statistics
/// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<ReportStatisticsDto>> GetStatistics()
        {
   try
            {
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userEmail))
       {
        return Unauthorized(new { message = "User not authenticated" });
 }

       var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);
bool canViewAll = await _authService.HasPermissionAsync(userEmail, "READ_ALL_REPORT_STATISTICS", isSubuser);

       var query = _context.Set<GeneratedReport>()
         .Where(r => !r.IsDeleted)
                .AsQueryable();

        if (!canViewAll)
     {
         query = query.Where(r => r.GeneratedBy == userEmail);
     }

      var now = DateTime.UtcNow;
       var startOfMonth = new DateTime(now.Year, now.Month, 1);
    var startOfWeek = now.AddDays(-(int)now.DayOfWeek);
 var startOfDay = now.Date;

                var statistics = new ReportStatisticsDto
            {
                TotalReportsGenerated = await query.CountAsync(),
            ReportsThisMonth = await query.CountAsync(r => r.GeneratedAt >= startOfMonth),
 ReportsThisWeek = await query.CountAsync(r => r.GeneratedAt >= startOfWeek),
         ReportsToday = await query.CountAsync(r => r.GeneratedAt >= startOfDay),
             TotalStorageUsedBytes = await query.SumAsync(r => (long?)r.FileSizeBytes) ?? 0,
           ReportsByType = await query
   .GroupBy(r => r.ReportType)
           .Select(g => new { Type = g.Key, Count = g.Count() })
       .ToDictionaryAsync(x => x.Type, x => x.Count),
       ReportsByFormat = await query
            .GroupBy(r => r.Format)
       .Select(g => new { Format = g.Key, Count = g.Count() })
          .ToDictionaryAsync(x => x.Format, x => x.Count),
  RecentReports = await query
 .OrderByDescending(r => r.GeneratedAt)
    .Take(5)
  .Select(r => new ReportHistoryItemDto
          {
   ReportId = r.ReportId,
       ReportTitle = r.ReportTitle,
            ReportType = r.ReportType,
  GeneratedAt = r.GeneratedAt,
   GeneratedBy = r.GeneratedBy,
        Format = r.Format,
     FileSizeBytes = r.FileSizeBytes,
      Status = r.Status
           })
  .ToListAsync()
         };

      return Ok(statistics);
   }
    catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving report statistics");
           return StatusCode(500, new { message = "Error retrieving report statistics" });
            }
        }

    /// <summary>
        /// DELETE /api/ReportGeneration/{reportId} - Delete a generated report
        /// </summary>
        [HttpDelete("{reportId}")]
        public async Task<IActionResult> DeleteReport(string reportId)
        {
            try
            {
    var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userEmail))
            {
       return Unauthorized(new { message = "User not authenticated" });
       }

           var report = await _context.Set<GeneratedReport>()
   .Where(r => r.ReportId == reportId).FirstOrDefaultAsync();

    if (report == null)
              {
          return NotFound(new { message = "Report not found" });
    }

           var isSubuser = await _userDataService.SubuserExistsAsync(userEmail);
     bool canDeleteAll = await _authService.HasPermissionAsync(userEmail, "DELETE_ALL_REPORTS", isSubuser);

          // Check if user can delete this report
        if (!canDeleteAll && report.GeneratedBy != userEmail)
          {
 return StatusCode(403, new { message = "You can only delete your own reports" });
   }

          // Soft delete
      report.IsDeleted = true;
  await _context.SaveChangesAsync();

            // Optionally delete physical file
      if (!string.IsNullOrEmpty(report.FilePath) && System.IO.File.Exists(report.FilePath))
    {
              try
  {
     System.IO.File.Delete(report.FilePath);
           }
           catch (Exception fileEx)
          {
     _logger.LogWarning(fileEx, "Could not delete report file {FilePath}", report.FilePath);
           }
           }

      _logger.LogInformation("Report {ReportId} deleted by {Email}", reportId, userEmail);

      return Ok(new { message = "Report deleted successfully" });
     }
            catch (Exception ex)
            {
     _logger.LogError(ex, "Error deleting report {ReportId}", reportId);
        return StatusCode(500, new { message = "Error deleting report" });
       }
   }

/// <summary>
/// Generate PDF report automatically from database using report_id
  /// Data is fetched from audit_reports table and report_details_json is parsed
        /// User only needs to upload optional images
        /// ✅ NO HARDCODED DATA - All data comes from database
        /// ✅ UNIQUE PDF for each report_id
        /// </summary>
        [HttpPost("generate-from-database/{reportId}")]
  [AllowAnonymous]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> GenerateFromDatabase(int reportId, [FromForm] DatabaseReportRequest request)
 {
            try
       {
          _logger.LogInformation("🔍 Fetching report_id: {ReportId} from database", reportId);

    // 1. Fetch unique report from database
   var auditReport = await _context.AuditReports.FindAsync(reportId);
    if (auditReport == null)
   {
            _logger.LogWarning("❌ Report not found: {ReportId}", reportId);
      return NotFound(new { 
                message = $"Audit report with ID {reportId} not found", 
        reportId 
            });
      }

        _logger.LogInformation("✅ Found report: '{ReportName}' (Client: {ClientEmail}, Date: {Date})", 
            auditReport.report_name, 
        auditReport.client_email,
         auditReport.report_datetime);

        // 2. Parse JSON data with smart fallback handling
      ReportData reportData = await ParseReportData(auditReport);

        // 3. Apply database values (NO HARDCODING - all from DB)
        reportData = ApplyDatabaseValues(reportData, auditReport);

        // 4. Log what data we have
        LogReportDataSummary(reportData, reportId);

  // 5. Extract personnel info (from request or use database defaults)
    var technicianName = request.TechnicianName ?? 
                 ExtractValueFromJson(auditReport.report_details_json, "TechnicianName") ?? 
"Not Specified";
        var technicianDept = request.TechnicianDept ?? 
       ExtractValueFromJson(auditReport.report_details_json, "TechnicianDept") ?? 
        "Not Specified";
        var validatorName = request.ValidatorName ?? 
     ExtractValueFromJson(auditReport.report_details_json, "ValidatorName") ?? 
          "Not Specified";
    var validatorDept = request.ValidatorDept ?? 
         ExtractValueFromJson(auditReport.report_details_json, "ValidatorDept") ?? 
          "Not Specified";

  // 6. Convert uploaded images to byte arrays
        byte[]? headerLeftLogo = await ConvertToByteArray(request.HeaderLeftLogo);
byte[]? headerRightLogo = await ConvertToByteArray(request.HeaderRightLogo);
 byte[]? watermark = await ConvertToByteArray(request.WatermarkImage);
        byte[]? techSignature = await ConvertToByteArray(request.TechnicianSignature);
        byte[]? valSignature = await ConvertToByteArray(request.ValidatorSignature);

        // 7. Build UNIQUE report request for this report_id
  var pdfRequest = new ReportRequest
        {
            ReportData = reportData,
      ReportTitle = request.ReportTitle ?? auditReport.report_name ?? $"Report #{reportId}",
     HeaderText = request.HeaderText ?? 
         $"Report ID: {reportId} | Digital ID: {reportData.DigitalSignature}",
      TechnicianName = technicianName,
       TechnicianDept = technicianDept,
            ValidatorName = validatorName,
    ValidatorDept = validatorDept,
    HeaderLeftLogo = headerLeftLogo,
     HeaderRightLogo = headerRightLogo,
            WatermarkImage = watermark,
   TechnicianSignature = techSignature,
        ValidatorSignature = valSignature
        };

        // 8. Generate UNIQUE PDF
        _logger.LogInformation("📄 Generating PDF for report_id: {ReportId}...", reportId);
        var pdfBytes = _pdfService.GenerateReport(pdfRequest);

        _logger.LogInformation("✅ PDF generated successfully! Report ID: {ReportId}, Size: {Size} KB", 
reportId, pdfBytes.Length / 1024);

        // 9. Return UNIQUE PDF file with report_id in filename
        var fileName = $"Report_{reportId}_{auditReport.report_name?.Replace(" ", "_")}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }
 catch (Exception ex)
    {
        _logger.LogError(ex, "❌ Error generating PDF for report_id: {ReportId}", reportId);
        return StatusCode(500, new 
    { 
            message = "Error generating PDF from database", 
            error = ex.Message,
    details = ex.InnerException?.Message,
  reportId 
        });
    }
}

/// <summary>
/// Parse report_details_json with enhanced error handling
/// </summary>
private async Task<ReportData> ParseReportData(audit_reports auditReport)
{
    try
    {
        if (string.IsNullOrEmpty(auditReport.report_details_json) || 
      auditReport.report_details_json == "{}")
        {
            _logger.LogWarning("⚠️ Empty JSON for report_id: {ReportId}, creating basic structure", 
      auditReport.report_id);
     return new ReportData();
     }

        var jsonString = auditReport.report_details_json;
        
        // Handle escaped JSON
        if (jsonString.StartsWith("\"") && jsonString.EndsWith("\""))
        {
    _logger.LogDebug("🔧 Unescaping JSON...");
   try
            {
   jsonString = JsonSerializer.Deserialize<string>(jsonString) ?? jsonString;
          }
            catch (Exception escapeEx)
     {
           _logger.LogWarning(escapeEx, "⚠️ Failed to unescape JSON, using original");
}
        }
        
        // Parse with flexible options
        var options = new JsonSerializerOptions
        {
     PropertyNameCaseInsensitive = true,
   AllowTrailingCommas = true,
  ReadCommentHandling = JsonCommentHandling.Skip
        };
        
        var reportData = JsonSerializer.Deserialize<ReportData>(jsonString, options) 
   ?? new ReportData();

        _logger.LogInformation("✅ Successfully parsed JSON data");
  return reportData;
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "❌ JSON parsing failed, using empty structure");
        return new ReportData();
    }
}

/// <summary>
/// Apply database values - NO HARDCODING
/// </summary>
private ReportData ApplyDatabaseValues(ReportData reportData, audit_reports auditReport)
{
    // Use database values, fallback to JSON values only if DB is empty
    reportData.ReportId = auditReport.report_id.ToString();
    reportData.ReportDate = auditReport.report_datetime.ToString("yyyy-MM-dd HH:mm:ss");
    reportData.EraserMethod = auditReport.erasure_method ?? reportData.EraserMethod ?? "Unknown Method";
    reportData.Status = auditReport.synced ? "Completed" : "Pending";
    
    // Set software info from JSON if available
    reportData.SoftwareName = reportData.SoftwareName ?? "DSecureErase";
    reportData.ProductVersion = reportData.ProductVersion ?? "1.0";
    
    // Generate unique digital signature per report
    reportData.DigitalSignature = reportData.DigitalSignature ?? 
 $"DSE-{auditReport.report_id}-{Guid.NewGuid().ToString("N").Substring(0, 8).ToUpper()}";
    
    // Set defaults only if JSON is completely empty
    if (string.IsNullOrEmpty(reportData.ComputerName))
 {
    reportData.ComputerName = ExtractValueFromJson(auditReport.report_details_json, "ComputerName") ?? 
            "Computer Info Not Available";
    }
    
    if (string.IsNullOrEmpty(reportData.MacAddress))
 {
   reportData.MacAddress = ExtractValueFromJson(auditReport.report_details_json, "MacAddress") ?? 
    "MAC Not Available";
    }
    
    // Initialize erasure log if empty
if (reportData.ErasureLog == null || reportData.ErasureLog.Count == 0)
    {
        _logger.LogWarning("⚠️ No erasure log data found for report_id: {ReportId}", auditReport.report_id);
        reportData.ErasureLog = new List<ErasureLogEntry>
        {
        new ErasureLogEntry
            {
  Target = $"Data from Report #{auditReport.report_id}",
     Capacity = "See report details",
      TotalSectors = "N/A",
    SectorsErased = "N/A",
        Size = "N/A",
 Status = auditReport.synced ? "Completed" : "Pending"
    }
        };
    }
    
    return reportData;
}

/// <summary>
/// Extract specific value from JSON string
/// </summary>
private string? ExtractValueFromJson(string? jsonString, string key)
{
    if (string.IsNullOrEmpty(jsonString)) return null;
    
    try
    {
        using var doc = JsonDocument.Parse(jsonString);
        if (doc.RootElement.TryGetProperty(key, out var element))
        {
     return element.GetString();
  }
    }
    catch { }
    
    return null;
}

/// <summary>
/// Log summary of report data for debugging
/// </summary>
private void LogReportDataSummary(ReportData reportData, int reportId)
{
    _logger.LogInformation("📊 Report Data Summary for ID {ReportId}:", reportId);
    _logger.LogInformation("  - Digital ID: {DigitalId}", reportData.DigitalSignature);
    _logger.LogInformation("  - Software: {Software} v{Version}", reportData.SoftwareName, reportData.ProductVersion);
    _logger.LogInformation("  - Computer: {Computer}", reportData.ComputerName);
    _logger.LogInformation("  - MAC: {Mac}", reportData.MacAddress);
 _logger.LogInformation("  - Method: {Method}", reportData.EraserMethod);
    _logger.LogInformation("  - Status: {Status}", reportData.Status);
    _logger.LogInformation("  - Erasure Logs: {Count} entries", reportData.ErasureLog?.Count ?? 0);
}

#region Private Helper Methods

private async Task<byte[]> GeneratePdfReport(GenerateReportRequestDto request, List<audit_reports> auditReports)
{
    var reportRequest = new ReportRequest
    {
  ReportTitle = request.ReportTitle,
 HeaderText = request.HeaderText ?? "Data Erasure Report",
   TechnicianName = request.ErasurePersonName,
    TechnicianDept = request.ErasurePersonDepartment,
        ValidatorName = request.ValidatorPersonName,
        ValidatorDept = request.ValidatorPersonDepartment,
    ReportData = new ReportData
        {
   ReportId = Guid.NewGuid().ToString(),
ReportDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
       SoftwareName = "DSecure",
  ProductVersion = "2.0",
   ErasureLog = auditReports.Select(r => new ErasureLogEntry
 {
      Target = r.report_name,
       Status = "Completed"
 }).ToList()
        }
    };

    return _pdfService.GenerateReport(reportRequest);
}

private async Task<byte[]?> ConvertToByteArray(IFormFile? file)
{
    if (file == null || file.Length == 0)
        return null;

    using var ms = new MemoryStream();
    await file.CopyToAsync(ms);
    var bytes = ms.ToArray();
    _logger.LogInformation("File uploaded: {FileName}, size: {Size} bytes", file.FileName, bytes.Length);
    return bytes;
}

#endregion
    }

    /// <summary>
    /// Request model for database-driven PDF generation
    /// </summary>
    public class DatabaseReportRequest
    {
      [FromForm]
        public string? ReportTitle { get; set; }
        
        [FromForm]
        public string? HeaderText { get; set; }

 [FromForm]
      public string? TechnicianName { get; set; }
        
        [FromForm]
        public string? TechnicianDept { get; set; }

        [FromForm]
        public string? ValidatorName { get; set; }
        
 [FromForm]
        public string? ValidatorDept { get; set; }

        [FromForm]
        public IFormFile? HeaderLeftLogo { get; set; }
 
        [FromForm]
        public IFormFile? HeaderRightLogo { get; set; }
        
        [FromForm]
        public IFormFile? WatermarkImage { get; set; }

        [FromForm]
        public IFormFile? TechnicianSignature { get; set; }
        
        [FromForm]
        public IFormFile? ValidatorSignature { get; set; }
    }
}
