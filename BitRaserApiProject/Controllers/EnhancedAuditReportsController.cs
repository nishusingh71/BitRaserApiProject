using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;
using System.Text.Json;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Audit Reports management controller with comprehensive role-based access control
    /// Supports both users and subusers with appropriate access levels
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedAuditReportsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly PdfService _pdfService;

        public EnhancedAuditReportsController(ApplicationDbContext context, IRoleBasedAuthService authService, IUserDataService userDataService, PdfService pdfService)
        {
            _context = context;
            _authService = authService;
            _userDataService = userDataService;
            _pdfService = pdfService;
        }

        /// <summary>
        /// Get all audit reports with role-based filtering
        /// ✅ ENHANCED: Parents can see their own reports + subuser reports
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAuditReports([FromQuery] ReportFilterRequest? filter)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            IQueryable<audit_reports> query = _context.AuditReports;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_REPORTS", isCurrentUserSubuser))
            {
                if (isCurrentUserSubuser)
                {
                    // ❌ Subuser - only own reports
                    query = query.Where(r => r.client_email == userEmail);
                }
                else
                {
                    // ✅ ENHANCED: User - own reports + subuser reports
                    var subuserEmails = await _context.subuser
     .Where(s => s.user_email == userEmail)
                      .Select(s => s.subuser_email)
                      .ToListAsync();
     
                    query = query.Where(r => 
r.client_email == userEmail ||// Own reports
      subuserEmails.Contains(r.client_email)            // Subuser reports
       );
                }
            }

            // Apply additional filters if provided
            if (filter != null)
            {
                if (!string.IsNullOrEmpty(filter.ClientEmail))
                    query = query.Where(r => r.client_email.Contains(filter.ClientEmail));

                if (!string.IsNullOrEmpty(filter.ErasureMethod))
                    query = query.Where(r => r.erasure_method.Contains(filter.ErasureMethod));

                if (filter.DateFrom.HasValue)
                    query = query.Where(r => r.report_datetime >= filter.DateFrom.Value);

                if (filter.DateTo.HasValue)
                    query = query.Where(r => r.report_datetime <= filter.DateTo.Value);

                if (filter.SyncedOnly.HasValue)
                    query = query.Where(r => r.synced == filter.SyncedOnly.Value);
            }

            var reports = await query
                .OrderByDescending(r => r.report_datetime)
                .Take(filter?.PageSize ?? 100)
                .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
                .Select(r => new {
                    r.report_id,
                    r.client_email,
                    r.report_name,
                    r.erasure_method,
                    r.report_datetime,
                    r.synced,
                    HasDetails = !string.IsNullOrEmpty(r.report_details_json) && r.report_details_json != "{}"
                })
                .ToListAsync();

            return Ok(reports);
        }

        /// <summary>
        /// Get audit report by ID with ownership validation
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<audit_reports>> GetAuditReport(int id)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            var report = await _context.AuditReports.FindAsync(id);
            
            if (report == null) return NotFound();

            // Users and subusers can only view their own reports unless they have admin permission
            bool canView = report.client_email == userEmail ||
                          await _authService.HasPermissionAsync(userEmail!, "READ_ALL_REPORTS", isCurrentUserSubuser);

            if (!canView)
            {
                return StatusCode(403, new { error = "You can only view your own reports" });
            }

            return Ok(report);
        }

        /// <summary>
        /// Get audit reports by client email with management hierarchy
        /// </summary>
        [HttpGet("by-email/{email}")]
        public async Task<ActionResult<IEnumerable<audit_reports>>> GetAuditReportsByEmail(string email)
        {
            var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(currentUserEmail!);
            
            // Check if user can view reports for this email
            bool canView = email == currentUserEmail ||
                          await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_REPORTS", isCurrentUserSubuser) ||
                          await _authService.CanManageUserAsync(currentUserEmail!, email);

            if (!canView)
            {
                return StatusCode(403, new { error = "You can only view your own reports or reports of users you manage" });
            }

            var reports = await _context.AuditReports
                .Where(r => r.client_email == email)
                .OrderByDescending(r => r.report_datetime)
                .ToListAsync();

            return reports.Any() ? Ok(reports) : NotFound();
        }

        /// <summary>
        /// Create a new audit report with automatic client assignment
        /// Supports both users and subusers
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<audit_reports>> CreateAuditReport([FromBody] AuditReportCreateRequest request)
        {
            // For anonymous requests, client_email must be provided
            if (string.IsNullOrEmpty(request.ClientEmail))
                return BadRequest("Client email is required for anonymous report creation");

            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var targetEmail = request.ClientEmail;

            var report = new audit_reports
            {
                client_email = targetEmail,
                report_name = request.ReportName ?? $"Audit Report {DateTime.UtcNow:yyyy-MM-dd HH:mm}",
                erasure_method = request.ErasureMethod ?? "Unknown",
                report_datetime = DateTime.UtcNow,
                report_details_json = request.ReportDetailsJson ?? "{}",
                synced = false
            };

            // If user is authenticated, apply business rules
            if (!string.IsNullOrEmpty(userEmail))
            {
                var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail);
                
                // Allow users and subusers to create reports for themselves
                // Allow users with special permissions to create for others
                if (request.ClientEmail != userEmail)
                {
                    if (!await _authService.HasPermissionAsync(userEmail, "CREATE_REPORTS_FOR_OTHERS", isCurrentUserSubuser))
                    {
                        report.client_email = userEmail; // Override to current user
                    }
                }
            }

            _context.AuditReports.Add(report);
            await _context.SaveChangesAsync();
            
            return CreatedAtAction(nameof(GetAuditReport), new { id = report.report_id }, report);
        }

        /// <summary>
        /// Update audit report by ID with ownership validation
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAuditReport(int id, [FromBody] AuditReportUpdateRequest request)
        {
            if (id != request.ReportId)
                return BadRequest(new { message = "Report ID mismatch" });

            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            var report = await _context.AuditReports.FindAsync(id);
            
            if (report == null) return NotFound();

            // Users and subusers can only update their own reports unless they have admin permission
            bool canUpdate = report.client_email == userEmail ||
                           await _authService.HasPermissionAsync(userEmail!, "UPDATE_ALL_REPORTS", isCurrentUserSubuser);

            if (!canUpdate)
            {
                return StatusCode(403, new { error = "You can only update your own reports" });
            }

            // Don't allow changing client_email unless user has admin permission
            if (request.ClientEmail != report.client_email && 
                !await _authService.HasPermissionAsync(userEmail!, "UPDATE_ALL_REPORTS", isCurrentUserSubuser))
            {
                return StatusCode(403, new { error = "You cannot change the client email of a report" });
            }

            if (!string.IsNullOrEmpty(request.ReportName))
                report.report_name = request.ReportName;

            if (!string.IsNullOrEmpty(request.ErasureMethod))
                report.erasure_method = request.ErasureMethod;

            if (!string.IsNullOrEmpty(request.ReportDetailsJson))
                report.report_details_json = request.ReportDetailsJson;

            _context.Entry(report).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        /// <summary>
        /// Delete audit report by ID with proper authorization
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuditReport(int id)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            var report = await _context.AuditReports.FindAsync(id);
            
            if (report == null) return NotFound();

            // Users and subusers can only delete their own reports unless they have admin permission
            bool canDelete = report.client_email == userEmail ||
                           await _authService.HasPermissionAsync(userEmail!, "DELETE_ALL_REPORTS", isCurrentUserSubuser);

            if (!canDelete)
            {
                return StatusCode(403, new { error = "You can only delete your own reports" });
            }

            _context.AuditReports.Remove(report);
            await _context.SaveChangesAsync();
            
            return NoContent();
        }

        /// <summary>
        /// Reserve a unique report ID for client applications (both users and subusers)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("reserve-id")]
        public async Task<ActionResult<int>> ReserveReportId([FromBody] ReportReservationRequest request)
        {
            if (string.IsNullOrEmpty(request.ClientEmail))
                return BadRequest("Client email is required");

            var newReport = new audit_reports
            {
                client_email = request.ClientEmail,
                synced = false,
                report_details_json = "{}",
                report_name = "Reserved",
                erasure_method = "Reserved",
                report_datetime = DateTime.UtcNow
            };

            _context.AuditReports.Add(newReport);
            await _context.SaveChangesAsync();

            return Ok(new { 
                ReportId = newReport.report_id,
                Message = "Report ID reserved successfully",
                ExpiresIn = "24 hours if not uploaded"
            });
        }

        /// <summary>
        /// Upload full report data after reserving ID
        /// </summary>
        [AllowAnonymous]
        [HttpPut("upload-report/{id}")]
        public async Task<IActionResult> UploadReportData(int id, [FromBody] ReportUploadRequest request)
        {
            if (id != request.ReportId)
                return BadRequest(new { message = "Report ID mismatch" });

            var report = await _context.AuditReports.FindAsync(id);
            if (report == null) return NotFound();

            // Check if report is still in reserved state
            if (report.synced)
                return BadRequest("Report has already been finalized");

            // Validate that the client email matches
            if (report.client_email != request.ClientEmail)
                return BadRequest("Client email mismatch");

            report.report_name = request.ReportName;
            report.erasure_method = request.ErasureMethod;
            report.report_details_json = request.ReportDetailsJson;
            report.report_datetime = DateTime.UtcNow;

            _context.Entry(report).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Report data uploaded successfully" });
        }

        /// <summary>
        /// Mark report as synced after full upload
        /// </summary>
        [AllowAnonymous]
        [HttpPatch("mark-synced/{id}")]
        public async Task<IActionResult> MarkReportSynced(int id, [FromBody] SyncConfirmationRequest request)
        {
            var report = await _context.AuditReports.FindAsync(id);
            if (report == null) return NotFound();

            // Validate client email for security
            if (report.client_email != request.ClientEmail)
                return BadRequest("Client email mismatch");

            if (report.synced)
                return BadRequest("Report is already marked as synced");

            report.synced = true;
            _context.Entry(report).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Report marked as synced successfully" });
        }

        /// <summary>
        /// Get report statistics for a user or all users
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetReportStatistics([FromQuery] string? clientEmail)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            IQueryable<audit_reports> query = _context.AuditReports;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_REPORT_STATISTICS", isCurrentUserSubuser))
            {
                // Users and subusers can only see their own statistics
                clientEmail = userEmail;
            }

            if (!string.IsNullOrEmpty(clientEmail))
                query = query.Where(r => r.client_email == clientEmail);

            var stats = new {
                TotalReports = await query.CountAsync(),
                SyncedReports = await query.CountAsync(r => r.synced),
                PendingReports = await query.CountAsync(r => !r.synced),
                ReportsThisMonth = await query.CountAsync(r => r.report_datetime.Month == DateTime.UtcNow.Month),
                ReportsThisWeek = await query.CountAsync(r => r.report_datetime >= DateTime.UtcNow.AddDays(-7)),
                ReportsToday = await query.CountAsync(r => r.report_datetime.Date == DateTime.UtcNow.Date),
                ErasureMethods = await query
                    .GroupBy(r => r.erasure_method)
                    .Select(g => new { Method = g.Key, Count = g.Count() })
                    .ToListAsync(),
                ClientEmails = clientEmail == null ? 
                    await query
                        .GroupBy(r => r.client_email)
                        .Select(g => new { Email = g.Key, Count = g.Count() })
                        .OrderByDescending(x => x.Count)
                        .Take(10)
                        .ToListAsync() : null
            };

            return Ok(stats);
        }

        /// <summary>
        /// Export reports to CSV format
        /// </summary>
        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportReportsCSV([FromQuery] ReportExportRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            IQueryable<audit_reports> query = _context.AuditReports;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(userEmail!, "EXPORT_ALL_REPORTS", isCurrentUserSubuser))
            {
                // Users and subusers can export their own reports
                query = query.Where(r => r.client_email == userEmail);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(request.ClientEmail))
                query = query.Where(r => r.client_email == request.ClientEmail);

            if (request.DateFrom.HasValue)
                query = query.Where(r => r.report_datetime >= request.DateFrom.Value);

            if (request.DateTo.HasValue)
                query = query.Where(r => r.report_datetime <= request.DateTo.Value);

            var reports = await query.OrderByDescending(r => r.report_datetime).ToListAsync();

            // Generate CSV content
            var csv = GenerateCsvContent(reports);
            var fileName = $"audit_reports_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
        }

        /// <summary>
        /// Export reports to PDF format using existing PDF service (Basic)
        /// </summary>
        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportReportsPDF([FromQuery] ReportExportRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            IQueryable<audit_reports> query = _context.AuditReports;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(userEmail!, "EXPORT_ALL_REPORTS", isCurrentUserSubuser))
            {
                // Users and subusers can export their own reports
                query = query.Where(r => r.client_email == userEmail);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(request.ClientEmail))
                query = query.Where(r => r.client_email == request.ClientEmail);

            if (request.DateFrom.HasValue)
                query = query.Where(r => r.report_datetime >= request.DateFrom.Value);

            if (request.DateTo.HasValue)
                query = query.Where(r => r.report_datetime <= request.DateTo.Value);

            var reports = await query.OrderByDescending(r => r.report_datetime).ToListAsync();

            if (!reports.Any())
                return NotFound("No reports found for the specified criteria");

            // Generate PDF for multiple reports
            var pdfBytes = await GenerateReportsPDF(reports, request);
            var fileName = $"audit_reports_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Export reports to PDF with file uploads (Headers, Signatures, Watermark)
        /// </summary>
        [HttpPost("export-pdf-with-files")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ExportReportsPDFWithFiles([FromForm] ReportExportWithFilesRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            
            IQueryable<audit_reports> query = _context.AuditReports;

            // Apply role-based filtering
            if (!await _authService.HasPermissionAsync(userEmail!, "EXPORT_ALL_REPORTS", isCurrentUserSubuser))
            {
                // Users and subusers can export their own reports
                query = query.Where(r => r.client_email == userEmail);
            }

            // Apply filters
            if (!string.IsNullOrEmpty(request.ClientEmail))
                query = query.Where(r => r.client_email == request.ClientEmail);

            if (request.DateFrom.HasValue)
                query = query.Where(r => r.report_datetime >= request.DateFrom.Value);

            if (request.DateTo.HasValue)
                query = query.Where(r => r.report_datetime <= request.DateTo.Value);

            var reports = await query.OrderByDescending(r => r.report_datetime).ToListAsync();

            if (!reports.Any())
                return NotFound("No reports found for the specified criteria");

            // Generate PDF with uploaded files
            var pdfBytes = await GenerateReportsPDFWithFiles(reports, request);
            var fileName = $"audit_reports_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Export single report to PDF by ID (Basic)
        /// </summary>
        [HttpGet("{id}/export-pdf")]
        public async Task<IActionResult> ExportSingleReportPDF(int id, [FromQuery] PdfExportOptions? options)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            var report = await _context.AuditReports.FindAsync(id);
            
            if (report == null) return NotFound();

            // Users and subusers can only export their own reports unless they have admin permission
            bool canExport = report.client_email == userEmail ||
                           await _authService.HasPermissionAsync(userEmail!, "EXPORT_ALL_REPORTS", isCurrentUserSubuser);

            if (!canExport)
            {
                return StatusCode(403, new { error = "You can only export your own reports" });
            }

            // Generate PDF for single report
            var pdfBytes = await GenerateSingleReportPDF(report, options);
            var fileName = $"report_{report.report_id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        /// <summary>
        /// Export single report to PDF with file uploads (Headers, Signatures, Watermark)
        /// </summary>
        [HttpPost("{id}/export-pdf-with-files")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ExportSingleReportPDFWithFiles(int id, [FromForm] SingleReportExportWithFilesRequest request)
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var isCurrentUserSubuser = await _userDataService.SubuserExistsAsync(userEmail!);
            var report = await _context.AuditReports.FindAsync(id);
            
            if (report == null) return NotFound();

            // Users and subusers can only export their own reports unless they have admin permission
            bool canExport = report.client_email == userEmail ||
                           await _authService.HasPermissionAsync(userEmail!, "EXPORT_ALL_REPORTS", isCurrentUserSubuser);

            if (!canExport)
            {
                return StatusCode(403, new { error = "You can only export your own reports" });
            }

            // Generate PDF for single report with uploaded files
            var pdfBytes = await GenerateSingleReportPDFWithFiles(report, request);
            var fileName = $"report_{report.report_id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

            return File(pdfBytes, "application/pdf", fileName);
        }

        #region Private Helper Methods

        private string GenerateCsvContent(List<audit_reports> reports)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("Report ID,Client Email,Report Name,Erasure Method,Report Date,Synced");

            foreach (var report in reports)
            {
                sb.AppendLine($"{report.report_id},{report.client_email},{report.report_name},{report.erasure_method},{report.report_datetime:yyyy-MM-dd HH:mm:ss},{report.synced}");
            }

            return sb.ToString();
        }

        private async Task<byte[]> GenerateReportsPDF(List<audit_reports> reports, ReportExportRequest request)
        {
            // Create a summary report for multiple reports
            var reportData = new ReportData
            {
                ReportId = $"SUMMARY_{DateTime.UtcNow:yyyyMMddHHmmss}",
                ReportDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                SoftwareName = "DSecure API",
                ProductVersion = "v1.0",
                Status = "Completed",
                ProcessMode = "Summary Export",
                ErasureLog = reports.Select(r => new ErasureLogEntry
                {
                    Target = r.report_name,
                    Status = r.synced ? "Synced" : "Pending",
                    Size = r.erasure_method,
                    Capacity = r.report_datetime.ToString("yyyy-MM-dd")
                }).ToList()
            };

            var reportRequest = new ReportRequest
            {
                ReportData = reportData,
                ReportTitle = $"Audit Reports Summary ({reports.Count} reports)",
                HeaderText = "DSecure API - Audit Reports Export",
                TechnicianName = "System",
                TechnicianDept = "API Service",
                ValidatorName = "System",
                ValidatorDept = "Automated Export"
            };

            return _pdfService.GenerateReport(reportRequest);
        }

        private async Task<byte[]> GenerateReportsPDFWithFiles(List<audit_reports> reports, ReportExportWithFilesRequest request)
        {
            // Create a summary report for multiple reports
            var reportData = new ReportData
            {
                ReportId = $"SUMMARY_{DateTime.UtcNow:yyyyMMddHHmmss}",
                ReportDate = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),
                SoftwareName = "DSecure API",
                ProductVersion = "v1.0",
                Status = "Completed",
                ProcessMode = "Summary Export",
                ErasureLog = reports.Select(r => new ErasureLogEntry
                {
                    Target = r.report_name,
                    Status = r.synced ? "Synced" : "Pending",
                    Size = r.erasure_method,
                    Capacity = r.report_datetime.ToString("yyyy-MM-dd")
                }).ToList()
            };

            var reportRequest = new ReportRequest
            {
                ReportData = reportData,
                ReportTitle = request.ReportTitle ?? $"Audit Reports Summary ({reports.Count} reports)",
                HeaderText = request.HeaderText ?? "DSecure API - Audit Reports Export",
                TechnicianName = request.TechnicianName ?? "System",
                TechnicianDept = request.TechnicianDept ?? "API Service",
                ValidatorName = request.ValidatorName ?? "System",
                ValidatorDept = request.ValidatorDept ?? "Automated Export"
            };

            // Convert uploaded files to byte arrays
            if (request.HeaderLeftLogo != null && request.HeaderLeftLogo.Length > 0)
            {
                using var ms = new MemoryStream();
                await request.HeaderLeftLogo.CopyToAsync(ms);
                reportRequest.HeaderLeftLogo = ms.ToArray();
            }

            if (request.HeaderRightLogo != null && request.HeaderRightLogo.Length > 0)
            {
                using var ms = new MemoryStream();
                await request.HeaderRightLogo.CopyToAsync(ms);
                reportRequest.HeaderRightLogo = ms.ToArray();
            }

            if (request.WatermarkImage != null && request.WatermarkImage.Length > 0)
            {
                using var ms = new MemoryStream();
                await request.WatermarkImage.CopyToAsync(ms);
                reportRequest.WatermarkImage = ms.ToArray();
            }

            if (request.TechnicianSignature != null && request.TechnicianSignature.Length > 0)
            {
                using var ms = new MemoryStream();
                await request.TechnicianSignature.CopyToAsync(ms);
                reportRequest.TechnicianSignature = ms.ToArray();
            }

            if (request.ValidatorSignature != null && request.ValidatorSignature.Length > 0)
            {
                using var ms = new MemoryStream();
                await request.ValidatorSignature.CopyToAsync(ms);
                reportRequest.ValidatorSignature = ms.ToArray();
            }

            return _pdfService.GenerateReport(reportRequest);
        }

        private async Task<byte[]> GenerateSingleReportPDF(audit_reports report, PdfExportOptions? options)
        {
            // ✅ Parse D-Secure JSON format properly
      ReportData reportData = await ParseDSecureReportData(report);

            // ✅ Create proper report request with mapped data
    var reportRequest = new ReportRequest
    {
    ReportData = reportData,
          ReportTitle = report.report_name ?? $"Report #{report.report_id}",
     HeaderText = options?.HeaderText ?? $"D-SecureErase Audit Report - ID: {report.report_id}",
                TechnicianName = options?.TechnicianName ?? "System",
                TechnicianDept = options?.TechnicianDept ?? "API Service",
           ValidatorName = options?.ValidatorName ?? "System",
              ValidatorDept = options?.ValidatorDept ?? "Automated Export"
        };

 return _pdfService.GenerateReport(reportRequest);
        }

   private async Task<byte[]> GenerateSingleReportPDFWithFiles(audit_reports report, SingleReportExportWithFilesRequest request)
        {
   // ✅ Parse D-Secure JSON format properly
          ReportData reportData = await ParseDSecureReportData(report);

          // ✅ Create proper report request with mapped data
     var reportRequest = new ReportRequest
          {
        ReportData = reportData,
       ReportTitle = request.ReportTitle ?? report.report_name ?? $"Report #{report.report_id}",
     HeaderText = request.HeaderText ?? $"D-SecureErase Audit Report - ID: {report.report_id}",
   TechnicianName = request.TechnicianName ?? "System",
        TechnicianDept = request.TechnicianDept ?? "API Service",
          ValidatorName = request.ValidatorName ?? "System",
                ValidatorDept = request.ValidatorDept ?? "Automated Export"
         };

// Convert uploaded files to byte arrays
      if (request.HeaderLeftLogo != null && request.HeaderLeftLogo.Length > 0)
   {
  using var ms = new MemoryStream();
    await request.HeaderLeftLogo.CopyToAsync(ms);
      reportRequest.HeaderLeftLogo = ms.ToArray();
   }

            if (request.HeaderRightLogo != null && request.HeaderRightLogo.Length > 0)
       {
      using var ms = new MemoryStream();
      await request.HeaderRightLogo.CopyToAsync(ms);
 reportRequest.HeaderRightLogo = ms.ToArray();
            }

            if (request.WatermarkImage != null && request.WatermarkImage.Length > 0)
            {
    using var ms = new MemoryStream();
    await request.WatermarkImage.CopyToAsync(ms);
      reportRequest.WatermarkImage = ms.ToArray();
    }

      if (request.TechnicianSignature != null && request.TechnicianSignature.Length > 0)
      {
                using var ms = new MemoryStream();
                await request.TechnicianSignature.CopyToAsync(ms);
      reportRequest.TechnicianSignature = ms.ToArray();
     }

            if (request.ValidatorSignature != null && request.ValidatorSignature.Length > 0)
            {
  using var ms = new MemoryStream();
     await request.ValidatorSignature.CopyToAsync(ms);
      reportRequest.ValidatorSignature = ms.ToArray();
            }

     return _pdfService.GenerateReport(reportRequest);
 }

        /// <summary>
        /// Parse D-Secure report_details_json with proper field mapping
        /// Handles the exact JSON format from D-SecureErase client application
        /// </summary>
        private async Task<ReportData> ParseDSecureReportData(audit_reports auditReport)
        {
      var reportData = new ReportData();

            try
   {
 if (string.IsNullOrEmpty(auditReport.report_details_json) || 
      auditReport.report_details_json == "{}")
         {
       return CreateDefaultReportData(auditReport);
           }

      // ✅ Parse JSON with case-insensitive matching
       using var doc = JsonDocument.Parse(auditReport.report_details_json);
        var root = doc.RootElement;

             // ✅ Map D-Secure fields to ReportData fields
 reportData.ReportId = GetJsonString(root, "report_id") ?? auditReport.report_id.ToString();
 reportData.ReportDate = GetJsonString(root, "datetime") ?? auditReport.report_datetime.ToString("yyyy-MM-dd HH:mm:ss");
          reportData.DigitalSignature = GetJsonString(root, "digital_signature") ?? $"DSE-{auditReport.report_id}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
     
// Software information
   reportData.SoftwareName = GetJsonString(root, "software_name") ?? "D-SecureErase";
           reportData.ProductVersion = GetJsonString(root, "product_version") ?? "1.0";
 
 // Machine information
           reportData.ComputerName = GetJsonString(root, "computer_name") ?? "Unknown";
     reportData.MacAddress = GetJsonString(root, "mac_address") ?? "Unknown";
      reportData.Manufacturer = GetJsonString(root, "manufacturer") ?? "Unknown";
  
      // ✅ Merge os and os_version into single field
     var os = GetJsonString(root, "os");
    var osVersion = GetJsonString(root, "os_version");
     
          if (!string.IsNullOrEmpty(os) && !string.IsNullOrEmpty(osVersion))
    {
        reportData.OSVersion = $"{os} {osVersion}";
              }
      else if (!string.IsNullOrEmpty(os))
{
       reportData.OSVersion = os;
      }
     else if (!string.IsNullOrEmpty(osVersion))
      {
   reportData.OSVersion = osVersion;
              }
     else
           {
       reportData.OSVersion = "Unknown";
                }
    
    // Erasure information
   reportData.EraserMethod = GetJsonString(root, "eraser_method") ?? auditReport.erasure_method ?? "Unknown";
      reportData.Status = GetJsonString(root, "status") ?? (auditReport.synced ? "Completed" : "Pending");
      reportData.ProcessMode = GetJsonString(root, "process_mode") ?? "Standard Erasure";
    reportData.ValidationMethod = GetJsonString(root, "validation_method") ?? "Not Specified";
                
     // Timing information
      reportData.EraserStartTime = GetJsonString(root, "Eraser_Start_Time");
          reportData.EraserEndTime = GetJsonString(root, "Eraser_End_Time");
              
 // File statistics
                reportData.TotalFiles = GetJsonInt(root, "total_files");
     reportData.ErasedFiles = GetJsonInt(root, "erased_files");
     reportData.FailedFiles = GetJsonInt(root, "failed_files");

   // ✅ Parse erasure_log array with proper mapping
    if (root.TryGetProperty("erasure_log", out var logArray) && logArray.ValueKind == JsonValueKind.Array)
       {
    reportData.ErasureLog = new List<ErasureLogEntry>();
              
 foreach (var logEntry in logArray.EnumerateArray())
   {
       var entry = new ErasureLogEntry
          {
     Target = GetJsonString(logEntry, "target") ?? "Unknown",
   Status = GetJsonString(logEntry, "status") ?? "Unknown",
      Capacity = GetJsonString(logEntry, "free_space") ?? GetJsonString(logEntry, "dummy_file_size") ?? "Unknown",
     Size = GetJsonString(logEntry, "dummy_file_size") ?? "Unknown",
          TotalSectors = GetJsonString(logEntry, "sectors_erased") ?? "Unknown",
 SectorsErased = GetJsonString(logEntry, "sectors_erased") ?? "Unknown"
     };
        
           reportData.ErasureLog.Add(entry);
      }
          }

      return reportData;
            }
       catch (JsonException ex)
    {
          // Fallback to default data if JSON parsing fails
return CreateDefaultReportData(auditReport);
       }
        }

        /// <summary>
        /// Create default ReportData when JSON is missing or invalid
        /// </summary>
        private ReportData CreateDefaultReportData(audit_reports auditReport)
 {
    return new ReportData
 {
   ReportId = auditReport.report_id.ToString(),
         ReportDate = auditReport.report_datetime.ToString("yyyy-MM-dd HH:mm:ss"),
     DigitalSignature = $"DSE-{auditReport.report_id}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
             SoftwareName = "D-SecureErase",
          ProductVersion = "1.0",
      ComputerName = auditReport.client_email,
     MacAddress = "Unknown",
     Manufacturer = "Unknown",
        OSVersion = "Unknown",
     EraserMethod = auditReport.erasure_method ?? "Unknown",
             Status = auditReport.synced ? "Completed" : "Pending",
            ProcessMode = "Standard Erasure",
   ValidationMethod = "Not Specified",
         ErasureLog = new List<ErasureLogEntry>
         {
           new ErasureLogEntry
            {
        Target = $"Report #{auditReport.report_id}",
           Status = auditReport.synced ? "Completed" : "Pending",
              Capacity = "See database for details",
   Size = "Unknown",
       TotalSectors = "Unknown",
             SectorsErased = "Unknown"
            }
     }
    };
        }

        /// <summary>
        /// Safely get string value from JSON element
        /// </summary>
        private string? GetJsonString(JsonElement element, string propertyName)
        {
          if (element.TryGetProperty(propertyName, out var prop))
  {
          return prop.ValueKind == JsonValueKind.String ? prop.GetString() : prop.ToString();
      }
    return null;
        }

        /// <summary>
        /// Safely get int value from JSON element
      /// </summary>
  private int GetJsonInt(JsonElement element, string propertyName)
        {
       if (element.TryGetProperty(propertyName, out var prop))
            {
        if (prop.ValueKind == JsonValueKind.Number)
        {
   return prop.GetInt32();
    }
    if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var result))
    {
          return result;
                }
   }
            return 0;
        }

        #endregion
}
    /// <summary>
    /// Report filter request model
    /// </summary>
    public class ReportFilterRequest
    {
        public string? ClientEmail { get; set; }
        public string? ErasureMethod { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
        public bool? SyncedOnly { get; set; }
        public int Page { get; set; } = 0;
        public int PageSize { get; set; } = 100;
    }

    /// <summary>
    /// Audit report creation request model
    /// </summary>
    public class AuditReportCreateRequest
    {
        public string ClientEmail { get; set; } = string.Empty;
        public string? ReportName { get; set; }
        public string? ErasureMethod { get; set; }
        public string? ReportDetailsJson { get; set; }
    }

    /// <summary>
    /// Audit report update request model
    /// </summary>
    public class AuditReportUpdateRequest
    {
        public int ReportId { get; set; }
        public string? ClientEmail { get; set; }
        public string? ReportName { get; set; }
        public string? ErasureMethod { get; set; }
        public string? ReportDetailsJson { get; set; }
    }

    /// <summary>
    /// Report reservation request model
    /// </summary>
    public class ReportReservationRequest
    {
        public string ClientEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// Report upload request model
    /// </summary>
    public class ReportUploadRequest
    {
        public int ReportId { get; set; }
        public string ClientEmail { get; set; } = string.Empty;
        public string ReportName { get; set; } = string.Empty;
        public string ErasureMethod { get; set; } = string.Empty;
        public string ReportDetailsJson { get; set; } = string.Empty;
    }

    /// <summary>
    /// Sync confirmation request model
    /// </summary>
    public class SyncConfirmationRequest
    {
        public string ClientEmail { get; set; } = string.Empty;
    }

    /// <summary>
    /// Report export request model
    /// </summary>
    public class ReportExportRequest
    {
        public string? ClientEmail { get; set; }
        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }
    }

    /// <summary>
    /// PDF export options model
    /// </summary>
    public class PdfExportOptions
    {
        public string? HeaderText { get; set; }
        public string? TechnicianName { get; set; }
        public string? TechnicianDept { get; set; }
        public string? ValidatorName { get; set; }
        public string? ValidatorDept { get; set; }
    }

    /// <summary>
    /// Report export with files request model (for multipart/form-data)
    /// </summary>
    public class ReportExportWithFilesRequest
    {
        [FromForm]
        public string? ClientEmail { get; set; }
        
        [FromForm]
        public DateTime? DateFrom { get; set; }
        
        [FromForm]
        public DateTime? DateTo { get; set; }

        [FromForm]
        public string? ReportTitle { get; set; }
        
        [FromForm]
        public string? HeaderText { get; set; }

        // Image files for upload via multipart/form-data
        [FromForm]
        public IFormFile? HeaderLeftLogo { get; set; }
        
        [FromForm]
        public IFormFile? HeaderRightLogo { get; set; }
        
        [FromForm]
        public IFormFile? WatermarkImage { get; set; }

        [FromForm]
        public string? TechnicianName { get; set; }
        
        [FromForm]
        public string? TechnicianDept { get; set; }
        
        [FromForm]
        public string? ValidatorName { get; set; }
        
        [FromForm]
        public string? ValidatorDept { get; set; }

        [FromForm]
        public IFormFile? TechnicianSignature { get; set; }
        
        [FromForm]
        public IFormFile? ValidatorSignature { get; set; }
    }

    /// <summary>
    /// Single report export with files request model (for multipart/form-data)
    /// </summary>
    public class SingleReportExportWithFilesRequest
    {
        [FromForm]
        public string? ReportTitle { get; set; }
        
        [FromForm]
        public string? HeaderText { get; set; }

        // Image files for upload via multipart/form-data
        [FromForm]
        public IFormFile? HeaderLeftLogo { get; set; }
        
        [FromForm]
        public IFormFile? HeaderRightLogo { get; set; }
        
        [FromForm]
        public IFormFile? WatermarkImage { get; set; }

        [FromForm]
        public string? TechnicianName { get; set; }
        
        [FromForm]
        public string? TechnicianDept { get; set; }
        
        [FromForm]
        public string? ValidatorName { get; set; }
        
        [FromForm]
        public string? ValidatorDept { get; set; }

        [FromForm]
        public IFormFile? TechnicianSignature { get; set; }
        
        [FromForm]
        public IFormFile? ValidatorSignature { get; set; }
    }
}