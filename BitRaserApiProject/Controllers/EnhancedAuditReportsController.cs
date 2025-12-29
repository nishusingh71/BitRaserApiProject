using System.Security.Claims;
using BitRaserApiProject.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Services;
using BitRaserApiProject.Attributes;
using BitRaserApiProject.Utilities; // ✅ ADD: For Base64EmailEncoder.DecodeEmailParam
using System.Text.Json;
using BitRaserApiProject.Factories;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Enhanced Audit Reports management controller with comprehensive role-based access control
    /// Supports both users and subusers with appropriate access levels
    /// ✅ MULTI-TENANT: Uses DynamicDbContextFactory for automatic database routing
    /// </summary>
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EnhancedAuditReportsController : ControllerBase
    {
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly ITenantConnectionService _tenantService;
        private readonly IRoleBasedAuthService _authService;
        private readonly IUserDataService _userDataService;
        private readonly PdfService _pdfService;
        private readonly ILogger<EnhancedAuditReportsController> _logger;
        private readonly ICacheService _cacheService;

        public EnhancedAuditReportsController(
   DynamicDbContextFactory contextFactory,
      ITenantConnectionService tenantService,
         IRoleBasedAuthService authService,
    IUserDataService userDataService,
         PdfService pdfService,
         ILogger<EnhancedAuditReportsController> logger,
         ICacheService cacheService)
        {
            _contextFactory = contextFactory;
            _tenantService = tenantService;
            _authService = authService;
            _userDataService = userDataService;
            _pdfService = pdfService;
            _logger = logger;
            _cacheService = cacheService;
        }

        /// <summary>
        /// ✅ Gets the correct database context - DIRECT TenantConnectionService approach
        /// </summary>
        private async Task<ApplicationDbContext> GetContextAsync()
        {
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "unknown";
            
            _logger.LogInformation("🔍 EnhancedAuditReports.GetContextAsync called for user: {Email}", userEmail);
            
            try
            {
                // ✅ DIRECT APPROACH: Use TenantConnectionService to get correct connection string
                var connectionString = await _tenantService.GetConnectionStringForUserAsync(userEmail);
                var mainConnStr = HttpContext.RequestServices.GetRequiredService<IConfiguration>()
                    .GetConnectionString("ApplicationDbContextConnection");
                
                var isPrivateCloud = (connectionString != mainConnStr && 
                                     !string.IsNullOrWhiteSpace(connectionString) && 
                                     connectionString.Contains("Server="));
                
                _logger.LogInformation("🔌 EnhancedAuditReports: Resolved {DbType} for user {Email}", 
                    isPrivateCloud ? "PRIVATE CLOUD DB" : "MAIN DB", userEmail);
                
                // Create DbContext with resolved connection string
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                optionsBuilder.UseMySql(
                    connectionString,
                    new MySqlServerVersion(new Version(8, 0, 21)),
                    mySqlOptions =>
                    {
                        mySqlOptions.EnableRetryOnFailure(
                            maxRetryCount: 3,
                            maxRetryDelay: TimeSpan.FromSeconds(5),
                            errorNumbersToAdd: null);
                        mySqlOptions.CommandTimeout(120);
                    });
                
                var context = new ApplicationDbContext(optionsBuilder.Options);
                _logger.LogInformation("✅ EnhancedAuditReports: Created {DbType} context for {Email}", 
                    isPrivateCloud ? "Private Cloud" : "Main", userEmail);
                
                return context;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ EnhancedAuditReports: Error creating context for {Email}, falling back to factory", userEmail);
                return await _contextFactory.CreateDbContextAsync();
            }
        }

        /// <summary>
        /// ✅ Check if current user is a subuser - uses middleware flag first
        /// </summary>
        private async Task<bool> IsCurrentUserSubuserAsync(string? userEmail)
        {
            // ✅ FIRST: Check middleware-provided flag (correctly set for private cloud subusers)
            if (HttpContext.Items.TryGetValue("IsSubuser", out var isSubuserObj) && isSubuserObj is bool isSubuser)
            {
                _logger.LogDebug("✅ Using middleware-provided IsSubuser flag: {IsSubuser}", isSubuser);
                return isSubuser;
            }
            
            // ✅ FALLBACK: Use service (for non-middleware requests)
            if (string.IsNullOrEmpty(userEmail)) return false;
            return await _userDataService.SubuserExistsAsync(userEmail);
        }

        /// <summary>
        /// ✅ Get the effective parent email for permission lookups (null for regular users)
        /// </summary>
        private string? GetEffectiveParentEmail()
        {
            // For private cloud subusers, middleware sets EffectiveUserEmail to parent email
            if (HttpContext.Items.TryGetValue("IsPrivateCloud", out var isPrivateObj) && isPrivateObj is bool isPrivate && isPrivate)
            {
                if (HttpContext.Items.TryGetValue("EffectiveUserEmail", out var parentObj) && parentObj is string parentEmail)
                {
                    _logger.LogDebug("✅ Using parent email for permissions: {ParentEmail}", parentEmail);
                    return parentEmail;
                }
            }
            return null;
        }

        /// <summary>
        /// Get all audit reports with role-based filtering
        /// ✅ ENHANCED: Parents can see their own reports + subuser reports
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAuditReports([FromQuery] ReportFilterRequest? filter)
        {
            try
            {
                // ✅ Use dynamic context for multi-tenant routing
                var context = await GetContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);

                IQueryable<audit_reports> query = context.AuditReports;

                // Apply role-based filtering
                if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_REPORTS", isCurrentUserSubuser, GetEffectiveParentEmail()))
                {
                    if (isCurrentUserSubuser)
                    {
                        // ❌ Subuser - only own reports
                        query = query.Where(r => r.client_email == userEmail);
                    }
                    else
                    {
                        // ✅ ENHANCED: User - own reports + subuser reports
                        var subuserEmails = await context.subuser
                               .Where(s => s.user_email == userEmail)
                                 .Select(s => s.subuser_email)
                          .ToListAsync();

                        query = query.Where(r =>
                  r.client_email == userEmail || // Own reports
                      subuserEmails.Contains(r.client_email) // Subuser reports
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
                          .Skip((filter?.Page ?? 0) * (filter?.PageSize ?? 100))
              .Take(filter?.PageSize ?? 100)
                  .Select(r => new
                  {
                      r.report_id,
                      r.client_email,
                      r.report_name,
                      r.erasure_method,
                      r.report_datetime,
                      r.synced,
                      HasDetails = !string.IsNullOrEmpty(r.report_details_json) && r.report_details_json != "{}"
                  })
                .ToListAsync();

                _logger.LogInformation("Retrieved {Count} reports for {Email} from {DbType} database",
                reports.Count, userEmail, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                return Ok(reports);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit reports");
                return StatusCode(500, new { message = "Error retrieving reports", error = ex.Message });
            }
        }

        /// <summary>
        /// Get audit report by ID with ownership validation
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<audit_reports>> GetAuditReport(int id)
        {
            try
            {
                // ✅ Use dynamic context
                var context = await GetContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);
                var report = await context.AuditReports.FindAsync(id);

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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting audit report {Id}", id);
                return StatusCode(500, new { message = "Error retrieving report", error = ex.Message });
            }
        }

        /// <summary>
        /// Get audit reports by client email with management hierarchy
        /// </summary>
        [HttpGet("by-email/{email}")]
        [DecodeEmail]
        public async Task<ActionResult<IEnumerable<audit_reports>>> GetAuditReportsByEmail(string email)
        {
            try
            {
                // ✅ CRITICAL: Decode email before any usage
                var decodedEmail = Base64EmailEncoder.DecodeEmailParam(email);

                var context = await GetContextAsync();

                var currentUserEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(currentUserEmail!);

                // Check if user can view reports for this email - use decoded email
                bool canView = decodedEmail == currentUserEmail?.ToLower() ||
                                 await _authService.HasPermissionAsync(currentUserEmail!, "READ_ALL_REPORTS", isCurrentUserSubuser) ||
                               await _authService.CanManageUserAsync(currentUserEmail!, decodedEmail);

                if (!canView)
                {
                    return StatusCode(403, new { error = "You can only view your own reports or reports of users you manage" });
                }

                var reports = await context.AuditReports
                       .Where(r => r.client_email.ToLower() == decodedEmail) // ✅ Use decoded email
              .OrderByDescending(r => r.report_datetime)
                          .ToListAsync();

                _logger.LogInformation("Retrieved {Count} reports for {Email} (decoded) from {DbType} database",
                       reports.Count, decodedEmail, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                return reports.Any() ? Ok(reports) : NotFound();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting reports for {Email}", email);
                return StatusCode(500, new { message = "Error retrieving reports", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new audit report with automatic client assignment
        /// Supports both users and subusers
        /// </summary>
        [AllowAnonymous]
        [HttpPost]
        public async Task<ActionResult<audit_reports>> CreateAuditReport([FromBody] AuditReportCreateRequest request)
        {
            try
            {
                var context = await GetContextAsync();

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
                    var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail);

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

                context.AuditReports.Add(report);
                await context.SaveChangesAsync();

                // ✅ CACHE INVALIDATION: Clear report caches
                _cacheService.RemoveByPrefix(CacheService.CacheKeys.ReportList);

                _logger.LogInformation("✅ Created report {Id} for {Email} in {DbType} database",
             report.report_id, report.client_email,
                  await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                return CreatedAtAction(nameof(GetAuditReport), new { id = report.report_id }, report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating audit report");
                return StatusCode(500, new { message = "Error creating report", error = ex.Message });
            }
        }

        /// <summary>
        /// Update audit report by ID with ownership validation
        /// </summary>
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAuditReport(int id, [FromBody] AuditReportUpdateRequest request)
        {
            try
            {
                var context = await GetContextAsync();

                if (id != request.ReportId)
                    return BadRequest(new { message = "Report ID mismatch" });

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);
                var report = await context.AuditReports.FindAsync(id);

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

                context.Entry(report).State = EntityState.Modified;
                await context.SaveChangesAsync();

                // ✅ CACHE INVALIDATION: Clear report caches
                _cacheService.Remove($"{CacheService.CacheKeys.Report}:{id}");
                _cacheService.RemoveByPrefix(CacheService.CacheKeys.ReportList);

                _logger.LogInformation("✅ Updated report {Id} in {DbType} database",
                id, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating audit report {Id}", id);
                return StatusCode(500, new { message = "Error updating report", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete audit report by ID with proper authorization
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAuditReport(int id)
        {
            try
            {
                var context = await GetContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);
                var report = await context.AuditReports.FindAsync(id);

                if (report == null) return NotFound();

                // Users and subusers can only delete their own reports unless they have admin permission
                bool canDelete = report.client_email == userEmail ||
               await _authService.HasPermissionAsync(userEmail!, "DELETE_ALL_REPORTS", isCurrentUserSubuser);

                if (!canDelete)
                {
                    return StatusCode(403, new { error = "You can only delete your own reports" });
                }

                context.AuditReports.Remove(report);
                await context.SaveChangesAsync();

                // ✅ CACHE INVALIDATION: Clear report caches
                _cacheService.Remove($"{CacheService.CacheKeys.Report}:{id}");
                _cacheService.RemoveByPrefix(CacheService.CacheKeys.ReportList);

                _logger.LogInformation("✅ Deleted report {Id} from {DbType} database",
                      id, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting audit report {Id}", id);
                return StatusCode(500, new { message = "Error deleting report", error = ex.Message });
            }
        }

        /// <summary>
        /// Reserve a unique report ID for client applications (both users and subusers)
        /// </summary>
        [AllowAnonymous]
        [HttpPost("reserve-id")]
        public async Task<ActionResult<int>> ReserveReportId([FromBody] ReportReservationRequest request)
        {
            try
            {
                var context = await GetContextAsync();

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

                context.AuditReports.Add(newReport);
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Reserved report ID {Id} for {Email}",
                       newReport.report_id, request.ClientEmail);

                return Ok(new
                {
                    ReportId = newReport.report_id,
                    Message = "Report ID reserved successfully",
                    ExpiresIn = "24 hours if not uploaded"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving report ID");
                return StatusCode(500, new { message = "Error reserving report ID", error = ex.Message });
            }
        }

        /// <summary>
        /// Upload full report data after reserving ID
        /// </summary>
        [AllowAnonymous]
        [HttpPut("upload-report/{id}")]
        public async Task<IActionResult> UploadReportData(int id, [FromBody] ReportUploadRequest request)
        {
            try
            {
                var context = await GetContextAsync();

                if (id != request.ReportId)
                    return BadRequest(new { message = "Report ID mismatch" });

                var report = await context.AuditReports.FindAsync(id);
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

                context.Entry(report).State = EntityState.Modified;
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Uploaded data for report {Id}", id);

                return Ok(new { message = "Report data uploaded successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading report data for ID {Id}", id);
                return StatusCode(500, new { message = "Error uploading report data", error = ex.Message });
            }
        }

        /// <summary>
        /// Mark report as synced after full upload
        /// </summary>
        [AllowAnonymous]
        [HttpPatch("mark-synced/{id}")]
        public async Task<IActionResult> MarkReportSynced(int id, [FromBody] SyncConfirmationRequest request)
        {
            try
            {
                var context = await GetContextAsync();

                var report = await context.AuditReports.FindAsync(id);
                if (report == null) return NotFound();

                // Validate client email for security
                if (report.client_email != request.ClientEmail)
                    return BadRequest("Client email mismatch");

                if (report.synced)
                    return BadRequest("Report is already marked as synced");

                report.synced = true;
                context.Entry(report).State = EntityState.Modified;
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Marked report {Id} as synced", id);

                return Ok(new { message = "Report marked as synced successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking report {Id} as synced", id);
                return StatusCode(500, new { message = "Error marking report as synced", error = ex.Message });
            }
        }

        /// <summary>
        /// Get report statistics for a user or all users
        /// </summary>
        [HttpGet("statistics")]
        public async Task<ActionResult<object>> GetReportStatistics([FromQuery] string? clientEmail)
        {
            try
            {
                var context = await GetContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);

                IQueryable<audit_reports> query = context.AuditReports;

                // Apply role-based filtering
                if (!await _authService.HasPermissionAsync(userEmail!, "READ_ALL_REPORT_STATISTICS", isCurrentUserSubuser))
                {
                    // Users and subusers can only see their own statistics
                    clientEmail = userEmail;
                }

                if (!string.IsNullOrEmpty(clientEmail))
                    query = query.Where(r => r.client_email == clientEmail);

                // ✅ CACHE: Statistics with short TTL for near-real-time balance
                var cacheKey = $"{CacheService.CacheKeys.ReportList}:stats:{clientEmail ?? "all"}:{userEmail}";
                var stats = await _cacheService.GetOrCreateAsync(cacheKey, async () =>
                {
                    return new
                    {
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
                }, CacheService.CacheTTL.Short);

                _logger.LogInformation("Retrieved statistics from {DbType} database",
                     await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting report statistics");
                return StatusCode(500, new { message = "Error retrieving statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Export reports to CSV format
        /// </summary>
        [HttpGet("export-csv")]
        public async Task<IActionResult> ExportReportsCSV([FromQuery] ReportExportRequest request)
        {
            try
            {
                var context = await GetContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);

                IQueryable<audit_reports> query = context.AuditReports;

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

                _logger.LogInformation("Exporting {Count} reports to CSV from {DbType} database",
                     reports.Count, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                // Generate CSV content
                var csv = GenerateCsvContent(reports);
                var fileName = $"audit_reports_{DateTime.UtcNow:yyyyMMdd_HHmmss}.csv";

                return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting reports to CSV");
                return StatusCode(500, new { message = "Error exporting reports", error = ex.Message });
            }
        }

        /// <summary>
        /// Export reports to PDF format using existing PDF service (Basic)
        /// </summary>
        [HttpGet("export-pdf")]
        public async Task<IActionResult> ExportReportsPDF([FromQuery] ReportExportRequest request)
        {
            try
            {
                var context = await GetContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);

                IQueryable<audit_reports> query = context.AuditReports;

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

                _logger.LogInformation("Exporting {Count} reports to PDF from {DbType} database",
                    reports.Count, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                // Generate PDF for multiple reports
                var pdfBytes = await GenerateReportsPDF(reports, request);
                var fileName = $"audit_reports_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting reports to PDF");
                return StatusCode(500, new { message = "Error exporting reports", error = ex.Message });
            }
        }

        /// <summary>
        /// Export reports to PDF with file uploads (Headers, Signatures, Watermark)
        /// </summary>
        [HttpPost("export-pdf-with-files")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ExportReportsPDFWithFiles([FromForm] ReportExportWithFilesRequest request)
        {
            try
            {
                var context = await GetContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);

                IQueryable<audit_reports> query = context.AuditReports;

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

                _logger.LogInformation("Exporting {Count} reports to PDF with files from {DbType} database",
                 reports.Count, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                // Generate PDF with uploaded files
                var pdfBytes = await GenerateReportsPDFWithFiles(reports, request);
                var fileName = $"audit_reports_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting reports to PDF with files");
                return StatusCode(500, new { message = "Error exporting reports", error = ex.Message });
            }
        }

        /// <summary>
        /// Export single report to PDF by ID (Basic)
        /// </summary>
        [HttpGet("{id}/export-pdf")]
        public async Task<IActionResult> ExportSingleReportPDF(int id, [FromQuery] PdfExportOptions? options)
        {
            try
            {
                var context = await GetContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);
                var report = await context.AuditReports.FindAsync(id);

                if (report == null) return NotFound();

                // Users and subusers can only export their own reports unless they have admin permission
                bool canExport = report.client_email == userEmail ||
                await _authService.HasPermissionAsync(userEmail!, "EXPORT_ALL_REPORTS", isCurrentUserSubuser);

                if (!canExport)
                {
                    return StatusCode(403, new { error = "You can only export your own reports" });
                }

                _logger.LogInformation("Exporting report {Id} to PDF from {DbType} database",
                 id, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                // Generate PDF for single report
                var pdfBytes = await GenerateSingleReportPDF(report, options);
                var fileName = $"report_{report.report_id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting single report {Id} to PDF", id);
                return StatusCode(500, new { message = "Error exporting report", error = ex.Message });
            }
        }

        /// <summary>
        /// Export single report to PDF with file uploads (Headers, Signatures, Watermark)
        /// </summary>
        [HttpPost("{id}/export-pdf-with-files")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> ExportSingleReportPDFWithFiles(int id, [FromForm] SingleReportExportWithFilesRequest request)
        {
            try
            {
                var context = await GetContextAsync();

                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);
                var report = await context.AuditReports.FindAsync(id);

                if (report == null) return NotFound();

                // Users and subusers can only export their own reports unless they have admin permission
                bool canExport = report.client_email == userEmail ||
               await _authService.HasPermissionAsync(userEmail!, "EXPORT_ALL_REPORTS", isCurrentUserSubuser);

                if (!canExport)
                {
                    return StatusCode(403, new { error = "You can only export your own reports" });
                }

                _logger.LogInformation("Exporting report {Id} to PDF with files from {DbType} database",
                   id, await _tenantService.IsPrivateCloudUserAsync() ? "PRIVATE" : "MAIN");

                // Generate PDF for single report with uploaded files
                var pdfBytes = await GenerateSingleReportPDFWithFiles(report, request);
                var fileName = $"report_{report.report_id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting single report {Id} to PDF with files", id);
                return StatusCode(500, new { message = "Error exporting report", error = ex.Message });
            }
        }

        #region PDF Export Settings Endpoints

        /// <summary>
        /// Save PDF export settings for the current user (One-time setup)
        /// After saving, all future exports will automatically use these settings
        /// </summary>
        [HttpPost("export-settings")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> SaveExportSettings([FromForm] SavePdfExportSettingsRequest request)
        {
            try
            {
                var context = await GetContextAsync();
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                // Check if settings already exist for this user
                var existingSettings = await context.PdfExportSettings
                    .Where(s => s.UserEmail == userEmail)
                    .FirstOrDefaultAsync();

                if (existingSettings != null)
                {
                    // Update existing settings
                    existingSettings.ReportTitle = request.ReportTitle ?? existingSettings.ReportTitle;
                    existingSettings.HeaderText = request.HeaderText ?? existingSettings.HeaderText;
                    existingSettings.TechnicianName = request.TechnicianName ?? existingSettings.TechnicianName;
                    existingSettings.TechnicianDept = request.TechnicianDept ?? existingSettings.TechnicianDept;
                    existingSettings.ValidatorName = request.ValidatorName ?? existingSettings.ValidatorName;
                    existingSettings.ValidatorDept = request.ValidatorDept ?? existingSettings.ValidatorDept;
                    existingSettings.UpdatedAt = DateTime.UtcNow;

                    // Update images if provided
                    if (request.HeaderLeftLogo != null && request.HeaderLeftLogo.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await request.HeaderLeftLogo.CopyToAsync(ms);
                        existingSettings.HeaderLeftLogoBase64 = Convert.ToBase64String(ms.ToArray());
                    }

                    if (request.HeaderRightLogo != null && request.HeaderRightLogo.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await request.HeaderRightLogo.CopyToAsync(ms);
                        existingSettings.HeaderRightLogoBase64 = Convert.ToBase64String(ms.ToArray());
                    }

                    if (request.WatermarkImage != null && request.WatermarkImage.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await request.WatermarkImage.CopyToAsync(ms);
                        existingSettings.WatermarkImageBase64 = Convert.ToBase64String(ms.ToArray());
                    }

                    if (request.TechnicianSignature != null && request.TechnicianSignature.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await request.TechnicianSignature.CopyToAsync(ms);
                        existingSettings.TechnicianSignatureBase64 = Convert.ToBase64String(ms.ToArray());
                    }

                    if (request.ValidatorSignature != null && request.ValidatorSignature.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await request.ValidatorSignature.CopyToAsync(ms);
                        existingSettings.ValidatorSignatureBase64 = Convert.ToBase64String(ms.ToArray());
                    }

                    await context.SaveChangesAsync();

                    _logger.LogInformation("Updated PDF export settings for {Email}", userEmail);
                    return Ok(new { 
                        success = true, 
                        message = "PDF export settings updated successfully",
                        data = MapToSettingsResponse(existingSettings)
                    });
                }
                else
                {
                    // Create new settings
                    var newSettings = new PdfExportSettings
                    {
                        UserEmail = userEmail,
                        ReportTitle = request.ReportTitle,
                        HeaderText = request.HeaderText,
                        TechnicianName = request.TechnicianName,
                        TechnicianDept = request.TechnicianDept,
                        ValidatorName = request.ValidatorName,
                        ValidatorDept = request.ValidatorDept,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    // Save images as base64
                    if (request.HeaderLeftLogo != null && request.HeaderLeftLogo.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await request.HeaderLeftLogo.CopyToAsync(ms);
                        newSettings.HeaderLeftLogoBase64 = Convert.ToBase64String(ms.ToArray());
                    }

                    if (request.HeaderRightLogo != null && request.HeaderRightLogo.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await request.HeaderRightLogo.CopyToAsync(ms);
                        newSettings.HeaderRightLogoBase64 = Convert.ToBase64String(ms.ToArray());
                    }

                    if (request.WatermarkImage != null && request.WatermarkImage.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await request.WatermarkImage.CopyToAsync(ms);
                        newSettings.WatermarkImageBase64 = Convert.ToBase64String(ms.ToArray());
                    }

                    if (request.TechnicianSignature != null && request.TechnicianSignature.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await request.TechnicianSignature.CopyToAsync(ms);
                        newSettings.TechnicianSignatureBase64 = Convert.ToBase64String(ms.ToArray());
                    }

                    if (request.ValidatorSignature != null && request.ValidatorSignature.Length > 0)
                    {
                        using var ms = new MemoryStream();
                        await request.ValidatorSignature.CopyToAsync(ms);
                        newSettings.ValidatorSignatureBase64 = Convert.ToBase64String(ms.ToArray());
                    }

                    context.PdfExportSettings.Add(newSettings);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Created PDF export settings for {Email}", userEmail);
                    return CreatedAtAction(nameof(GetExportSettings), null, new { 
                        success = true, 
                        message = "PDF export settings saved successfully",
                        data = MapToSettingsResponse(newSettings)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving PDF export settings");
                return StatusCode(500, new { success = false, message = "Error saving settings", error = ex.Message });
            }
        }

        /// <summary>
        /// Save PDF export settings via JSON body (with base64 encoded images)
        /// </summary>
        [HttpPost("export-settings-json")]
        public async Task<IActionResult> SaveExportSettingsJson([FromBody] SavePdfExportSettingsJsonRequest request)
        {
            try
            {
                var context = await GetContextAsync();
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                // Check if settings already exist for this user
                var existingSettings = await context.PdfExportSettings
                    .Where(s => s.UserEmail == userEmail)
                    .FirstOrDefaultAsync();

                if (existingSettings != null)
                {
                    // Update existing settings
                    existingSettings.ReportTitle = request.ReportTitle ?? existingSettings.ReportTitle;
                    existingSettings.HeaderText = request.HeaderText ?? existingSettings.HeaderText;
                    existingSettings.TechnicianName = request.TechnicianName ?? existingSettings.TechnicianName;
                    existingSettings.TechnicianDept = request.TechnicianDept ?? existingSettings.TechnicianDept;
                    existingSettings.ValidatorName = request.ValidatorName ?? existingSettings.ValidatorName;
                    existingSettings.ValidatorDept = request.ValidatorDept ?? existingSettings.ValidatorDept;
                    existingSettings.HeaderLeftLogoBase64 = request.HeaderLeftLogoBase64 ?? existingSettings.HeaderLeftLogoBase64;
                    existingSettings.HeaderRightLogoBase64 = request.HeaderRightLogoBase64 ?? existingSettings.HeaderRightLogoBase64;
                    existingSettings.WatermarkImageBase64 = request.WatermarkImageBase64 ?? existingSettings.WatermarkImageBase64;
                    existingSettings.TechnicianSignatureBase64 = request.TechnicianSignatureBase64 ?? existingSettings.TechnicianSignatureBase64;
                    existingSettings.ValidatorSignatureBase64 = request.ValidatorSignatureBase64 ?? existingSettings.ValidatorSignatureBase64;
                    existingSettings.UpdatedAt = DateTime.UtcNow;

                    await context.SaveChangesAsync();

                    _logger.LogInformation("Updated PDF export settings (JSON) for {Email}", userEmail);
                    return Ok(new { 
                        success = true, 
                        message = "PDF export settings updated successfully",
                        data = MapToSettingsResponse(existingSettings)
                    });
                }
                else
                {
                    // Create new settings
                    var newSettings = new PdfExportSettings
                    {
                        UserEmail = userEmail,
                        ReportTitle = request.ReportTitle,
                        HeaderText = request.HeaderText,
                        TechnicianName = request.TechnicianName,
                        TechnicianDept = request.TechnicianDept,
                        ValidatorName = request.ValidatorName,
                        ValidatorDept = request.ValidatorDept,
                        HeaderLeftLogoBase64 = request.HeaderLeftLogoBase64,
                        HeaderRightLogoBase64 = request.HeaderRightLogoBase64,
                        WatermarkImageBase64 = request.WatermarkImageBase64,
                        TechnicianSignatureBase64 = request.TechnicianSignatureBase64,
                        ValidatorSignatureBase64 = request.ValidatorSignatureBase64,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };

                    context.PdfExportSettings.Add(newSettings);
                    await context.SaveChangesAsync();

                    _logger.LogInformation("Created PDF export settings (JSON) for {Email}", userEmail);
                    return CreatedAtAction(nameof(GetExportSettings), null, new { 
                        success = true, 
                        message = "PDF export settings saved successfully",
                        data = MapToSettingsResponse(newSettings)
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving PDF export settings via JSON");
                return StatusCode(500, new { success = false, message = "Error saving settings", error = ex.Message });
            }
        }

        /// <summary>
        /// Get current user's PDF export settings
        /// </summary>
        [HttpGet("export-settings")]
        public async Task<IActionResult> GetExportSettings()
        {
            try
            {
                var context = await GetContextAsync();
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                var settings = await context.PdfExportSettings
                    .Where(s => s.UserEmail == userEmail && s.IsActive)
                    .FirstOrDefaultAsync();

                if (settings == null)
                {
                    return Ok(new { 
                        success = true, 
                        message = "No export settings found. Default values will be used.",
                        hasSettings = false,
                        data = (object?)null
                    });
                }

                return Ok(new { 
                    success = true, 
                    hasSettings = true,
                    data = MapToSettingsResponse(settings)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting PDF export settings");
                return StatusCode(500, new { success = false, message = "Error getting settings", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete current user's PDF export settings
        /// </summary>
        [HttpDelete("export-settings")]
        public async Task<IActionResult> DeleteExportSettings()
        {
            try
            {
                var context = await GetContextAsync();
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userEmail))
                    return Unauthorized(new { message = "User not authenticated" });

                var settings = await context.PdfExportSettings
                    .Where(s => s.UserEmail == userEmail)
                    .FirstOrDefaultAsync();

                if (settings == null)
                {
                    return NotFound(new { success = false, message = "No export settings found" });
                }

                context.PdfExportSettings.Remove(settings);
                await context.SaveChangesAsync();

                _logger.LogInformation("Deleted PDF export settings for {Email}", userEmail);
                return Ok(new { success = true, message = "PDF export settings deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting PDF export settings");
                return StatusCode(500, new { success = false, message = "Error deleting settings", error = ex.Message });
            }
        }

        /// <summary>
        /// Export single report to PDF using saved settings (no need to pass all parameters)
        /// Just call this endpoint and saved settings will be automatically applied
        /// </summary>
        [HttpGet("{id}/export-pdf-with-settings")]
        public async Task<IActionResult> ExportSingleReportPDFWithSettings(int id)
        {
            try
            {
                var context = await GetContextAsync();
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);
                var report = await context.AuditReports.FindAsync(id);

                if (report == null) return NotFound();

                // Authorization check
                bool canExport = report.client_email == userEmail ||
                   await _authService.HasPermissionAsync(userEmail!, "EXPORT_ALL_REPORTS", isCurrentUserSubuser);

                if (!canExport)
                {
                    return StatusCode(403, new { error = "You can only export your own reports" });
                }

                // Get saved settings for this user
                var settings = await context.PdfExportSettings
                    .Where(s => s.UserEmail == userEmail && s.IsActive)
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Exporting report {Id} to PDF with saved settings for {Email}", id, userEmail);

                // Generate PDF with saved settings
                var pdfBytes = await GenerateSingleReportPDFWithSavedSettings(report, settings);
                var fileName = $"report_{report.report_id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";

                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error exporting single report {Id} to PDF with settings", id);
                return StatusCode(500, new { message = "Error exporting report", error = ex.Message });
            }
        }

        /// <summary>
        /// Export multiple reports to PDF as ZIP file using saved settings
        /// Single API call for batch download - no need to call individual endpoints
        /// </summary>
        /// <param name="request">List of report IDs to export</param>
        [HttpPost("batch-export-pdf")]
        public async Task<IActionResult> BatchExportReportsPDF([FromBody] BatchExportRequest request)
        {
            try
            {
                if (request.ReportIds == null || !request.ReportIds.Any())
                {
                    return BadRequest(new { error = "At least one report ID is required" });
                }

                var context = await GetContextAsync();
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);

                // Get saved settings for this user
                var settings = await context.PdfExportSettings
                    .Where(s => s.UserEmail == userEmail && s.IsActive)
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Batch exporting {Count} reports for {Email}", request.ReportIds.Count, userEmail);

                // Get all requested reports
                var reports = await context.AuditReports
                    .Where(r => request.ReportIds.Contains(r.report_id))
                    .ToListAsync();

                if (!reports.Any())
                {
                    return NotFound(new { error = "No reports found with the provided IDs" });
                }

                // Check authorization for each report
                var hasExportAllPermission = await _authService.HasPermissionAsync(userEmail!, "EXPORT_ALL_REPORTS", isCurrentUserSubuser);
                
                var authorizedReports = reports.Where(r => 
                    r.client_email == userEmail || hasExportAllPermission
                ).ToList();

                if (!authorizedReports.Any())
                {
                    return StatusCode(403, new { error = "You don't have permission to export any of the selected reports" });
                }

                // If only one report, return PDF directly
                if (authorizedReports.Count == 1)
                {
                    var singleReport = authorizedReports.First();
                    var pdfBytes = await GenerateSingleReportPDFWithSavedSettings(singleReport, settings);
                    var fileName = $"report_{singleReport.report_id}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
                    return File(pdfBytes, "application/pdf", fileName);
                }

                // Multiple reports - create ZIP file
                using var memoryStream = new MemoryStream();
                using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    foreach (var report in authorizedReports)
                    {
                        try
                        {
                            var pdfBytes = await GenerateSingleReportPDFWithSavedSettings(report, settings);
                            var entryName = $"report_{report.report_id}_{report.report_name?.Replace(" ", "_") ?? "export"}.pdf";
                            
                            var entry = archive.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Fastest);
                            using var entryStream = entry.Open();
                            await entryStream.WriteAsync(pdfBytes, 0, pdfBytes.Length);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to generate PDF for report {Id}, skipping", report.report_id);
                        }
                    }
                }

                memoryStream.Position = 0;
                var zipFileName = $"reports_batch_{DateTime.UtcNow:yyyyMMdd_HHmmss}.zip";

                _logger.LogInformation("Batch export completed: {Count} reports exported for {Email}", 
                    authorizedReports.Count, userEmail);

                return File(memoryStream.ToArray(), "application/zip", zipFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error batch exporting reports to PDF");
                return StatusCode(500, new { message = "Error exporting reports", error = ex.Message });
            }
        }

        /// <summary>
        /// Preview single report as PDF (inline display in browser)
        /// Uses saved settings if available
        /// </summary>
        /// <param name="id">Report ID to preview</param>
        [HttpGet("{id}/preview")]
        public async Task<IActionResult> PreviewReportPDF(int id)
        {
            try
            {
                var context = await GetContextAsync();
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var isCurrentUserSubuser = await IsCurrentUserSubuserAsync(userEmail!);
                var report = await context.AuditReports.FindAsync(id);

                if (report == null) 
                {
                    return NotFound(new { error = "Report not found" });
                }

                // Authorization check
                bool canPreview = report.client_email == userEmail ||
                   await _authService.HasPermissionAsync(userEmail!, "READ_ALL_REPORTS", isCurrentUserSubuser);

                if (!canPreview)
                {
                    return StatusCode(403, new { error = "You can only preview your own reports" });
                }

                // Get saved settings for this user
                var settings = await context.PdfExportSettings
                    .Where(s => s.UserEmail == userEmail && s.IsActive)
                    .FirstOrDefaultAsync();

                _logger.LogInformation("Previewing report {Id} for {Email}", id, userEmail);

                // Generate PDF with saved settings
                var pdfBytes = await GenerateSingleReportPDFWithSavedSettings(report, settings);

                // Return PDF inline (for browser preview) instead of attachment (download)
                Response.Headers.Append("Content-Disposition", $"inline; filename=\"report_{report.report_id}_preview.pdf\"");
                
                return File(pdfBytes, "application/pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error previewing report {Id}", id);
                return StatusCode(500, new { message = "Error previewing report", error = ex.Message });
            }
        }

        private PdfExportSettingsResponse MapToSettingsResponse(PdfExportSettings settings)
        {
            return new PdfExportSettingsResponse
            {
                Id = settings.Id,
                UserEmail = settings.UserEmail,
                ReportTitle = settings.ReportTitle,
                HeaderText = settings.HeaderText,
                TechnicianName = settings.TechnicianName,
                TechnicianDept = settings.TechnicianDept,
                ValidatorName = settings.ValidatorName,
                ValidatorDept = settings.ValidatorDept,
                HasHeaderLeftLogo = !string.IsNullOrEmpty(settings.HeaderLeftLogoBase64),
                HasHeaderRightLogo = !string.IsNullOrEmpty(settings.HeaderRightLogoBase64),
                HasWatermarkImage = !string.IsNullOrEmpty(settings.WatermarkImageBase64),
                HasTechnicianSignature = !string.IsNullOrEmpty(settings.TechnicianSignatureBase64),
                HasValidatorSignature = !string.IsNullOrEmpty(settings.ValidatorSignatureBase64),
                // ✅ Return actual base64 image data
                HeaderLeftLogoBase64 = settings.HeaderLeftLogoBase64,
                HeaderRightLogoBase64 = settings.HeaderRightLogoBase64,
                WatermarkImageBase64 = settings.WatermarkImageBase64,
                TechnicianSignatureBase64 = settings.TechnicianSignatureBase64,
                ValidatorSignatureBase64 = settings.ValidatorSignatureBase64,
                IsActive = settings.IsActive,
                CreatedAt = settings.CreatedAt,
                UpdatedAt = settings.UpdatedAt
            };
        }

        private async Task<byte[]> GenerateSingleReportPDFWithSavedSettings(audit_reports report, PdfExportSettings? settings)
        {
            // Get logged-in user details
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDetails = await GetUserDetailsForPDF(userEmail);

            // Parse D-Secure JSON format properly
            ReportData reportData = await ParseDSecureReportData(report);

            // Create proper report request with mapped data
            var reportRequest = new ReportRequest
            {
                ReportData = reportData,
                ReportTitle = settings?.ReportTitle ?? report.report_name ?? $"Report #{report.report_id}",
                HeaderText = settings?.HeaderText ?? "D-SecureErase Audit Report",
                TechnicianName = settings?.TechnicianName ?? userDetails.UserName,
                TechnicianDept = settings?.TechnicianDept ?? userDetails.Department,
                ValidatorName = settings?.ValidatorName ?? userDetails.UserName,
                ValidatorDept = settings?.ValidatorDept ?? userDetails.Department
            };

            // Apply saved images if available
            if (!string.IsNullOrEmpty(settings?.HeaderLeftLogoBase64))
            {
                reportRequest.HeaderLeftLogo = Convert.FromBase64String(settings.HeaderLeftLogoBase64);
            }

            if (!string.IsNullOrEmpty(settings?.HeaderRightLogoBase64))
            {
                reportRequest.HeaderRightLogo = Convert.FromBase64String(settings.HeaderRightLogoBase64);
            }

            if (!string.IsNullOrEmpty(settings?.WatermarkImageBase64))
            {
                reportRequest.WatermarkImage = Convert.FromBase64String(settings.WatermarkImageBase64);
            }

            if (!string.IsNullOrEmpty(settings?.TechnicianSignatureBase64))
            {
                reportRequest.TechnicianSignature = Convert.FromBase64String(settings.TechnicianSignatureBase64);
            }

            if (!string.IsNullOrEmpty(settings?.ValidatorSignatureBase64))
            {
                reportRequest.ValidatorSignature = Convert.FromBase64String(settings.ValidatorSignatureBase64);
            }

            return _pdfService.GenerateReport(reportRequest);
        }

        #endregion

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
            // ✅ Get logged-in user details
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDetails = await GetUserDetailsForPDF(userEmail);

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
                // ✅ Use logged-in user details (not hardcoded)
                TechnicianName = userDetails.UserName,
                TechnicianDept = userDetails.Department,
                ValidatorName = userDetails.UserName, // Same user as technician for automated exports
                ValidatorDept = userDetails.Department
            };

            return _pdfService.GenerateReport(reportRequest);
        }

        private async Task<byte[]> GenerateReportsPDFWithFiles(List<audit_reports> reports, ReportExportWithFilesRequest request)
        {
            // ✅ Get logged-in user details
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDetails = await GetUserDetailsForPDF(userEmail);

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
                // ✅ Use request values if provided, otherwise use logged-in user details
                TechnicianName = !string.IsNullOrWhiteSpace(request.TechnicianName) ? request.TechnicianName : userDetails.UserName,
                TechnicianDept = !string.IsNullOrWhiteSpace(request.TechnicianDept) ? request.TechnicianDept : userDetails.Department,
                ValidatorName = !string.IsNullOrWhiteSpace(request.ValidatorName) ? request.ValidatorName : userDetails.UserName,
                ValidatorDept = !string.IsNullOrWhiteSpace(request.ValidatorDept) ? request.ValidatorDept : userDetails.Department
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
            // ✅ Get logged-in user details
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDetails = await GetUserDetailsForPDF(userEmail);

            // ✅ Parse D-Secure JSON format properly
            ReportData reportData = await ParseDSecureReportData(report);

            // ✅ Create proper report request with mapped data
            var reportRequest = new ReportRequest
            {
                ReportData = reportData,
                ReportTitle = report.report_name ?? $"Report #{report.report_id}",
                HeaderText = options?.HeaderText ?? $"D-SecureErase Audit Report",
                // ✅ Use options if provided, otherwise use logged-in user details
                TechnicianName = !string.IsNullOrWhiteSpace(options?.TechnicianName) ? options.TechnicianName : userDetails.UserName,
                TechnicianDept = !string.IsNullOrWhiteSpace(options?.TechnicianDept) ? options.TechnicianDept : userDetails.Department,
                ValidatorName = !string.IsNullOrWhiteSpace(options?.ValidatorName) ? options.ValidatorName : userDetails.UserName,
                ValidatorDept = !string.IsNullOrWhiteSpace(options?.ValidatorDept) ? options.ValidatorDept : userDetails.Department
            };

            return _pdfService.GenerateReport(reportRequest);
        }

        private async Task<byte[]> GenerateSingleReportPDFWithFiles(audit_reports report, SingleReportExportWithFilesRequest request)
        {
            // ✅ Get logged-in user details
            var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userDetails = await GetUserDetailsForPDF(userEmail);

            // ✅ Parse D-Secure JSON format properly
            ReportData reportData = await ParseDSecureReportData(report);

            // ✅ Create proper report request with mapped data
            var reportRequest = new ReportRequest
            {
                ReportData = reportData,
                ReportTitle = request.ReportTitle ?? report.report_name ?? $"Report #{report.report_id}",
                HeaderText = request.HeaderText ?? $"D-SecureErase Audit Report",
                // ✅ Use request values if provided, otherwise use logged-in user details
                TechnicianName = !string.IsNullOrWhiteSpace(request.TechnicianName) ? request.TechnicianName : userDetails.UserName,
                TechnicianDept = !string.IsNullOrWhiteSpace(request.TechnicianDept) ? request.TechnicianDept : userDetails.Department,
                ValidatorName = !string.IsNullOrWhiteSpace(request.ValidatorName) ? request.ValidatorName : userDetails.UserName,
                ValidatorDept = !string.IsNullOrWhiteSpace(request.ValidatorDept) ? request.ValidatorDept : userDetails.Department
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
        /// Get user details for PDF generation from logged-in user
        /// Returns user name and department, handles both Users and Subusers
        /// ✅ FIXED: No Users table query for private cloud compatibility
        /// </summary>
        private async Task<UserDetailsForPDF> GetUserDetailsForPDF(string? userEmail)
        {
            var result = new UserDetailsForPDF
            {
                UserName = null,
                Department = null
            };

            if (string.IsNullOrEmpty(userEmail))
                return result;

            try
            {
                var context = await GetContextAsync();

                // ✅ Try to find as subuser first
                var subuser = await context.subuser
                           .Where(s => s.subuser_email == userEmail)
               .Select(s => new { s.Name, s.Department })
                   .FirstOrDefaultAsync();

                if (subuser != null)
                {
                    if (!string.IsNullOrWhiteSpace(subuser.Name))
                        result.UserName = subuser.Name.Trim();

                    if (!string.IsNullOrWhiteSpace(subuser.Department))
                        result.Department = subuser.Department.Trim();

                    return result;
                }

                // ✅ FIX: Don't query Users table for private cloud compatibility
                // For private cloud users, if not a subuser, use email as fallback
                // Main database users won't reach here as they authenticate separately

                // Use email as username fallback
                result.UserName = userEmail.Split('@')[0]; // Use part before @
                result.Department = null; // No department info available
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not get user details for {Email}", userEmail);
                // Return default result
            }

            return result;
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

                // ✅ FIXED: Super smart OS handling - "N/A" replaced with blank string
                var os = GetJsonString(root, "os");
                var osVersion = GetJsonString(root, "os_version");

                // ✅ Clean up "N/A" and empty values - convert "N/A" to empty string
                if (string.IsNullOrWhiteSpace(os) || os.Trim().Equals("", StringComparison.OrdinalIgnoreCase))
                {
                    os = string.Empty;  // ✅ Blank string instead of null
                }
                else
                {
                    os = os.Trim();
                }

                if (string.IsNullOrWhiteSpace(osVersion) || osVersion.Trim().Equals("", StringComparison.OrdinalIgnoreCase))
                {
                    osVersion = string.Empty;  // ✅ Blank string instead of null
                }
                else
                {
                    osVersion = osVersion.Trim();
                }

                // ✅ Build OSVersion string intelligently - NEVER include "N/A"
                if (!string.IsNullOrEmpty(os) && !string.IsNullOrEmpty(osVersion))
                {
                    // Both available and valid: "Windows Windows 11 10.0.26100"
                    reportData.OSVersion = $"{os} {osVersion}";
                }
                else if (!string.IsNullOrEmpty(osVersion))
                {
                    // Only version available (os is N/A): "Windows 11 10.0.26100"
                    // ✅ This handles your case where os="N/A" but osVersion has data
                    reportData.OSVersion = osVersion;
                }
                else if (!string.IsNullOrEmpty(os))
                {
                    // Only OS available (version is N/A): "Windows"
                    reportData.OSVersion = os;
                }
                else
                {
                    // Both are N/A or empty: blank string
                    reportData.OSVersion = string.Empty;  // ✅ Blank string instead of null
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
                OSVersion = string.Empty,  // ✅ FIXED: blank string instead of null or "Unknown"
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
    /// User details for PDF generation (internal use)
    /// </summary>
    internal class UserDetailsForPDF
    {
        /// <summary>
        /// User name - null if not available (never "N/A")
        /// </summary>
        public string? UserName { get; set; }

        /// <summary>
        /// Department name - null if not available (never "N/A")
        /// </summary>
        public string? Department { get; set; }
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

    /// <summary>
    /// Batch export request model for downloading multiple reports
    /// </summary>
    public class BatchExportRequest
    {
        /// <summary>
        /// List of report IDs to export as PDF
        /// </summary>
        public List<int> ReportIds { get; set; } = new();
    }
}
