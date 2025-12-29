using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;
using BitRaserApiProject.Factories;
using System.Security.Cryptography;
using System.Text;
using BitRaserApiProject.Services;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// License Activation API Controller
    /// Completely separate from existing License Management
    /// Matches Python controller expectations exactly
    /// ✅ SUPPORTS PRIVATE CLOUD ROUTING
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class LicenseController : ControllerBase
    {
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly ILogger<LicenseController> _logger;
        private readonly IConfiguration _configuration;
        private readonly ICacheService _cacheService;

        public LicenseController(
              DynamicDbContextFactory contextFactory,
           ILogger<LicenseController> logger,
       IConfiguration configuration,
       ICacheService cacheService)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _configuration = configuration;
            _cacheService = cacheService;
        }

        #region Public Endpoints (Client-facing)

        /// <summary>
        /// Activate a license with hardware ID binding
        /// POST /api/License/activate
        /// </summary>
        [Authorize]
        [HttpPost("activate")]
        [ProducesResponseType(typeof(ActivateLicenseResponse), 200)]
        public async Task<ActionResult<ActivateLicenseResponse>> Activate([FromBody] ActivateLicenseRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogInformation("🔑 License activation attempt: {Key}", request.license_key);

                var license = await context.Set<LicenseActivation>()
                        .Where(l => l.LicenseKey == request.license_key).FirstOrDefaultAsync();

                if (license == null)
                {
                    _logger.LogWarning("❌ Invalid license key: {Key}", request.license_key);
                    return Ok(new ActivateLicenseResponse
                    {
                        status = "INVALID_KEY",
                        expiry = null,
                        edition = null,
                        server_revision = null,
                        license_status = null
                    });
                }

                // Check if revoked
                if (license.Status == "REVOKED")
                {
                    _logger.LogWarning("❌ License revoked: {Key}", request.license_key);
                    await LogUsageAsync(context, request.license_key, "ACTIVATE_FAILED_REVOKED", request.hwid);

                    return Ok(new ActivateLicenseResponse
                    {
                        status = "REVOKED",
                        expiry = null,
                        edition = null,
                        server_revision = license.ServerRevision,
                        license_status = "REVOKED"
                    });
                }

                // Check expiry
                if (license.IsExpired)
                {
                    license.Status = "EXPIRED";
                    await context.SaveChangesAsync();

                    _logger.LogWarning("❌ License expired: {Key}, Expiry: {Expiry}",
                           request.license_key, license.ExpiryDate);

                    await LogUsageAsync(context, request.license_key, "ACTIVATE_FAILED_EXPIRED", request.hwid);

                    return Ok(new ActivateLicenseResponse
                    {
                        status = "LICENSE_EXPIRED",
                        expiry = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                        edition = license.Edition,
                        server_revision = license.ServerRevision,
                        license_status = "EXPIRED"
                    });
                }

                // First activation - bind HWID
                if (string.IsNullOrEmpty(license.Hwid))
                {
                    license.Hwid = request.hwid;
                    license.ServerRevision++;
                    license.LastSeen = DateTime.UtcNow;

                    await context.SaveChangesAsync();
                    await LogUsageAsync(context, request.license_key, "ACTIVATE_FIRST_TIME", request.hwid);

                    _logger.LogInformation("✅ License activated (first time): {Key}, HWID: {Hwid}",
                             request.license_key, request.hwid);
                }
                // HWID mismatch check
                else if (!string.Equals(license.Hwid, request.hwid, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("❌ HWID mismatch: {Key}, Expected: {Expected}, Got: {Got}",
                     request.license_key, license.Hwid, request.hwid);

                    await LogUsageAsync(context, request.license_key, "ACTIVATE_FAILED_HWID_MISMATCH", request.hwid);

                    return Ok(new ActivateLicenseResponse
                    {
                        status = "HW_MISMATCH",
                        expiry = null,
                        edition = null,
                        server_revision = license.ServerRevision,
                        license_status = license.Status
                    });
                }
                else
                {
                    // Update last seen
                    license.LastSeen = DateTime.UtcNow;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("✅ License activation successful: {Key}", request.license_key);
                }

                return Ok(new ActivateLicenseResponse
                {
                    status = "OK",
                    expiry = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                    edition = license.Edition,
                    server_revision = license.ServerRevision,
                    license_status = license.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error activating license: {Key}", request.license_key);
                return Ok(new ActivateLicenseResponse
                {
                    status = "ERROR",
                    expiry = null,
                    edition = null,
                    server_revision = null,
                    license_status = null
                });
            }
        }

        /// <summary>
        /// Renew/Extend a license
        /// POST /api/License/renew
        /// </summary>
        [Authorize]
        [HttpPost("renew")]
        [ProducesResponseType(typeof(RenewLicenseResponse), 200)]
        public async Task<ActionResult<RenewLicenseResponse>> Renew([FromBody] RenewLicenseRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogInformation("🔄 License renewal attempt: {Key}", request.license_key);

                var license = await context.Set<LicenseActivation>()
                  .Where(l => l.LicenseKey == request.license_key).FirstOrDefaultAsync();

                if (license == null)
                {
                    _logger.LogWarning("❌ Invalid license key: {Key}", request.license_key);
                    return Ok(new RenewLicenseResponse
                    {
                        status = "INVALID_KEY",
                        new_expiry = null,
                        server_revision = null
                    });
                }

                if (license.Status == "REVOKED")
                {
                    _logger.LogWarning("❌ Cannot renew revoked license: {Key}", request.license_key);
                    return Ok(new RenewLicenseResponse
                    {
                        status = "REVOKED",
                        new_expiry = null,
                        server_revision = license.ServerRevision
                    });
                }

                // Extend by specified days or default 365 days (1 year)
                int extensionDays = request.extension_days ?? 365;
                int oldExpiryDays = license.ExpiryDays;

                license.ExpiryDays += extensionDays;
                license.Status = "ACTIVE";
                license.ServerRevision++;
                license.LastSeen = DateTime.UtcNow;

                await context.SaveChangesAsync();
                await LogUsageAsync(context, request.license_key, "RENEW", null,
            oldExpiryDays: oldExpiryDays, newExpiryDays: license.ExpiryDays);

                _logger.LogInformation("✅ License renewed: {Key}, New Expiry Days: {Days}, Extension: {Ext} days",
                          request.license_key, license.ExpiryDays, extensionDays);

                return Ok(new RenewLicenseResponse
                {
                    status = "OK",
                    new_expiry = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                    server_revision = license.ServerRevision
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error renewing license: {Key}", request.license_key);
                return Ok(new RenewLicenseResponse
                {
                    status = "ERROR",
                    new_expiry = null,
                    server_revision = null
                });
            }
        }

        /// <summary>
        /// Upgrade license edition (BASIC → PRO → ENTERPRISE)
        /// POST /api/License/upgrade
        /// </summary>
        [Authorize]
        [HttpPost("upgrade")]
        [ProducesResponseType(typeof(UpgradeLicenseResponse), 200)]
        public async Task<ActionResult<UpgradeLicenseResponse>> Upgrade([FromBody] UpgradeLicenseRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogInformation("⬆️ License upgrade attempt: {Key} → {Edition}",
                  request.license_key, request.new_edition);

                var license = await context.Set<LicenseActivation>()
             .Where(l => l.LicenseKey == request.license_key).FirstOrDefaultAsync();

                if (license == null)
                {
                    _logger.LogWarning("❌ Invalid license key: {Key}", request.license_key);
                    return Ok(new UpgradeLicenseResponse
                    {
                        status = "INVALID_KEY",
                        edition = null,
                        server_revision = null
                    });
                }

                if (license.Status == "REVOKED")
                {
                    _logger.LogWarning("❌ Cannot upgrade revoked license: {Key}", request.license_key);
                    return Ok(new UpgradeLicenseResponse
                    {
                        status = "REVOKED",
                        edition = license.Edition,
                        server_revision = license.ServerRevision
                    });
                }

                // Validate edition (BASIC / PRO / ENTERPRISE)
                var validEditions = new[] { "BASIC", "PRO", "ENTERPRISE" };
                var newEdition = request.new_edition.ToUpperInvariant();

                if (!validEditions.Contains(newEdition))
                {
                    _logger.LogWarning("❌ Invalid edition: {Edition}", request.new_edition);
                    return Ok(new UpgradeLicenseResponse
                    {
                        status = "INVALID_EDITION",
                        edition = license.Edition,
                        server_revision = license.ServerRevision
                    });
                }

                string oldEdition = license.Edition;
                license.Edition = newEdition;
                license.ServerRevision++;
                license.LastSeen = DateTime.UtcNow;

                await context.SaveChangesAsync();
                await LogUsageAsync(context, request.license_key, "UPGRADE", null,
         oldEdition: oldEdition, newEdition: newEdition);

                _logger.LogInformation("✅ License upgraded: {Key}, Edition: {Edition}",
                 request.license_key, newEdition);

                return Ok(new UpgradeLicenseResponse
                {
                    status = "OK",
                    edition = license.Edition,
                    server_revision = license.ServerRevision
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error upgrading license: {Key}", request.license_key);
                return Ok(new UpgradeLicenseResponse
                {
                    status = "ERROR",
                    edition = null,
                    server_revision = null
                });
            }
        }

        /// <summary>
        /// Sync license with server to check for remote changes
        /// POST /api/License/sync
        /// </summary>
        [Authorize]
        [HttpPost("sync")]
        [ProducesResponseType(typeof(SyncLicenseResponse), 200)]
        public async Task<ActionResult<SyncLicenseResponse>> Sync([FromBody] SyncLicenseRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogDebug("🔄 License sync: {Key}, Local Revision: {LocalRev}",
                      request.license_key, request.local_revision);

                var license = await context.Set<LicenseActivation>()
                     .Where(l => l.LicenseKey == request.license_key).FirstOrDefaultAsync();

                if (license == null)
                {
                    _logger.LogWarning("❌ Invalid license key: {Key}", request.license_key);
                    return Ok(new SyncLicenseResponse
                    {
                        status = "INVALID_KEY",
                        expiry = null,
                        edition = null,
                        server_revision = null,
                        license_status = null
                    });
                }

                // HWID mismatch
                if (!string.Equals(license.Hwid ?? "", request.hwid ?? "", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("❌ HWID mismatch during sync: {Key}", request.license_key);
                    return Ok(new SyncLicenseResponse
                    {
                        status = "HW_MISMATCH",
                        expiry = null,
                        edition = null,
                        server_revision = license.ServerRevision,
                        license_status = license.Status
                    });
                }

                // Update last seen
                license.LastSeen = DateTime.UtcNow;
                await context.SaveChangesAsync();

                // No changes
                if (request.local_revision >= license.ServerRevision)
                {
                    return Ok(new SyncLicenseResponse
                    {
                        status = "NO_CHANGE",
                        expiry = null,
                        edition = null,
                        server_revision = license.ServerRevision,
                        license_status = license.Status
                    });
                }

                // License revoked
                if (license.Status == "REVOKED")
                {
                    _logger.LogInformation("⚠️ Synced revoked license: {Key}", request.license_key);

                    return Ok(new SyncLicenseResponse
                    {
                        status = "REVOKED",
                        expiry = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                        edition = license.Edition,
                        server_revision = license.ServerRevision,
                        license_status = license.Status
                    });
                }

                // Updated (renewed or upgraded)
                _logger.LogInformation("✅ License synced with updates: {Key}, Server Revision: {ServerRev}",
               request.license_key, license.ServerRevision);

                return Ok(new SyncLicenseResponse
                {
                    status = "UPDATE",
                    expiry = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                    edition = license.Edition,
                    server_revision = license.ServerRevision,
                    license_status = license.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error syncing license: {Key}", request.license_key);
                return Ok(new SyncLicenseResponse
                {
                    status = "ERROR",
                    expiry = null,
                    edition = null,
                    server_revision = null,
                    license_status = null
                });
            }
        }

        #endregion

        #region Admin Endpoints (Protected)

        /// <summary>
        /// Get all licenses (Admin only)
        /// GET /api/License/admin/all
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("admin/all")]
        [ProducesResponseType(typeof(List<LicenseDetailsResponse>), 200)]
        public async Task<ActionResult<IEnumerable<LicenseDetailsResponse>>> GetAllLicenses()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var licenses = await context.Set<LicenseActivation>()
                          .OrderByDescending(l => l.CreatedAt)
                  .Select(l => new LicenseDetailsResponse
                  {
                      id = l.Id,
                      license_key = l.LicenseKey,
                      hwid = l.Hwid,
                      expiry_days = l.ExpiryDays,
                      expiry_date = l.ExpiryDate != null ? l.ExpiryDate.Value.ToString("yyyy-MM-dd") : null,
                      remaining_days = l.RemainingDays,
                      edition = l.Edition,
                      status = l.Status,
                      server_revision = l.ServerRevision,
                      created_at = l.CreatedAt,
                      last_seen = l.LastSeen,
                      user_email = l.UserEmail,
                      notes = l.Notes
                  })
            .ToListAsync();

                return Ok(licenses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching all licenses");
                return StatusCode(500, new { message = "Error fetching licenses", error = ex.Message });
            }
        }

        /// <summary>
        /// Get license by key (Admin only)
        /// GET /api/License/admin/{licenseKey}
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("admin/{licenseKey}")]
        [ProducesResponseType(typeof(LicenseDetailsResponse), 200)]
        public async Task<ActionResult<LicenseDetailsResponse>> GetLicense(string licenseKey)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var license = await context.Set<LicenseActivation>()
           .Where(l => l.LicenseKey == licenseKey).FirstOrDefaultAsync();

                if (license == null)
                    return NotFound(new { message = "License not found" });

                var response = new LicenseDetailsResponse
                {
                    id = license.Id,
                    license_key = license.LicenseKey,
                    hwid = license.Hwid,
                    expiry_days = license.ExpiryDays,
                    expiry_date = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                    remaining_days = license.RemainingDays,
                    edition = license.Edition,
                    status = license.Status,
                    server_revision = license.ServerRevision,
                    created_at = license.CreatedAt,
                    last_seen = license.LastSeen,
                    user_email = license.UserEmail,
                    notes = license.Notes
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching license: {LicenseKey}", licenseKey);
                return StatusCode(500, new { message = "Error fetching license", error = ex.Message });
            }
        }

        /// <summary>
        /// Create a new license (Admin only)
        /// POST /api/License/admin/create
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("admin/create")]
        [ProducesResponseType(typeof(LicenseDetailsResponse), 201)]
        public async Task<ActionResult<LicenseDetailsResponse>> CreateLicense([FromBody] CreateLicenseRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Check for duplicate
                var existing = await context.Set<LicenseActivation>()
                       .Where(l => l.LicenseKey == request.license_key).FirstOrDefaultAsync();

                if (existing != null)
                {
                    return Conflict(new { message = "License key already exists" });
                }

                var license = new LicenseActivation
                {
                    LicenseKey = request.license_key,
                    ExpiryDays = request.expiry_days,
                    Edition = request.edition.ToUpperInvariant(),
                    Status = "ACTIVE",
                    ServerRevision = 1,
                    CreatedAt = DateTime.UtcNow,
                    UserEmail = request.user_email,
                    Notes = request.notes
                };

                context.Set<LicenseActivation>().Add(license);
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ License created: {Key}, Edition: {Edition}, Expiry Days: {Days}",
         license.LicenseKey, license.Edition, license.ExpiryDays);

                var response = new LicenseDetailsResponse
                {
                    id = license.Id,
                    license_key = license.LicenseKey,
                    hwid = license.Hwid,
                    expiry_days = license.ExpiryDays,
                    expiry_date = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                    remaining_days = license.RemainingDays,
                    edition = license.Edition,
                    status = license.Status,
                    server_revision = license.ServerRevision,
                    created_at = license.CreatedAt,
                    last_seen = license.LastSeen,
                    user_email = license.UserEmail,
                    notes = license.Notes
                };

                return CreatedAtAction(nameof(GetLicense), new { licenseKey = license.LicenseKey }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error creating license: {Key}", request.license_key);
                return StatusCode(500, new { message = "Error creating license", error = ex.Message });
            }
        }

        /// <summary>
        /// Revoke a license (Admin only)
        /// POST /api/License/admin/revoke
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("admin/revoke")]
        public async Task<IActionResult> RevokeLicense([FromBody] RevokeLicenseRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var license = await context.Set<LicenseActivation>()
               .Where(l => l.LicenseKey == request.license_key).FirstOrDefaultAsync();

                if (license == null)
                    return NotFound(new { message = "License not found" });

                license.Status = "REVOKED";
                license.ServerRevision++;

                await context.SaveChangesAsync();
                await LogUsageAsync(context, request.license_key, "REVOKE", null, notes: request.reason);

                _logger.LogWarning("⚠️ License revoked: {Key}, Reason: {Reason}",
  request.license_key, request.reason ?? "No reason provided");

                return Ok(new
                {
                    message = "License revoked successfully",
                    license_key = license.LicenseKey,
                    server_revision = license.ServerRevision
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error revoking license: {Key}", request.license_key);
                return StatusCode(500, new { message = "Error revoking license", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete a license permanently (Admin only)
        /// DELETE /api/License/admin/{licenseKey}
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpDelete("admin/{licenseKey}")]
        public async Task<IActionResult> DeleteLicense(string licenseKey)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var license = await context.Set<LicenseActivation>()
                          .Where(l => l.LicenseKey == licenseKey).FirstOrDefaultAsync();

                if (license == null)
                    return NotFound(new { message = "License not found" });

                context.Set<LicenseActivation>().Remove(license);
                await context.SaveChangesAsync();

                _logger.LogWarning("🗑️ License deleted: {Key}", licenseKey);

                return Ok(new { message = "License deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting license: {Key}", licenseKey);
                return StatusCode(500, new { message = "Error deleting license", error = ex.Message });
            }
        }

        /// <summary>
        /// Get license statistics (Admin only)
        /// GET /api/License/admin/statistics
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("admin/statistics")]
        [ProducesResponseType(typeof(LicenseStatisticsResponse), 200)]
        public async Task<ActionResult<LicenseStatisticsResponse>> GetLicenseStatistics()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var total = await context.Set<LicenseActivation>().CountAsync();
                var active = await context.Set<LicenseActivation>().CountAsync(l => l.Status == "ACTIVE");
                var expired = await context.Set<LicenseActivation>().CountAsync(l => l.Status == "EXPIRED");
                var revoked = await context.Set<LicenseActivation>().CountAsync(l => l.Status == "REVOKED");

                var basic = await context.Set<LicenseActivation>().CountAsync(l => l.Edition == "BASIC");
                var pro = await context.Set<LicenseActivation>().CountAsync(l => l.Edition == "PRO");
                var enterprise = await context.Set<LicenseActivation>().CountAsync(l => l.Edition == "ENTERPRISE");

                var sevenDaysFromNow = DateTime.UtcNow.AddDays(7);
                var thirtyDaysFromNow = DateTime.UtcNow.AddDays(30);

                var expiringIn7Days = await context.Set<LicenseActivation>()
                            .Where(l => l.Status == "ACTIVE")
                            .ToListAsync();
                var count7Days = expiringIn7Days.Count(l => l.ExpiryDate <= sevenDaysFromNow);

                var expiringIn30Days = await context.Set<LicenseActivation>()
                    .Where(l => l.Status == "ACTIVE")
                .ToListAsync();
                var count30Days = expiringIn30Days.Count(l => l.ExpiryDate <= thirtyDaysFromNow);

                var response = new LicenseStatisticsResponse
                {
                    total = total,
                    by_status = new LicenseStatisticsResponse.StatusBreakdown
                    {
                        active = active,
                        expired = expired,
                        revoked = revoked
                    },
                    by_edition = new LicenseStatisticsResponse.EditionBreakdown
                    {
                        basic = basic,
                        pro = pro,
                        enterprise = enterprise
                    },
                    expiring = new LicenseStatisticsResponse.ExpiringBreakdown
                    {
                        in7Days = count7Days,
                        in30Days = count30Days
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching license statistics");
                return StatusCode(500, new { message = "Error fetching statistics", error = ex.Message });
            }
        }

        /// <summary>
        /// Bulk generate licenses (Admin only)
        /// POST /api/License/admin/bulk-generate
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("admin/bulk-generate")]
        [ProducesResponseType(typeof(BulkGenerateLicensesResponse), 200)]
        public async Task<ActionResult<BulkGenerateLicensesResponse>> BulkGenerateLicenses([FromBody] BulkGenerateLicensesRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var generatedKeys = new List<string>();
                var licenses = new List<LicenseActivation>();

                for (int i = 0; i < request.count; i++)
                {
                    string licenseKey = GenerateLicenseKey(request.key_prefix);

                    // Check for duplicates
                    while (await context.Set<LicenseActivation>().AnyAsync(l => l.LicenseKey == licenseKey))
                    {
                        licenseKey = GenerateLicenseKey(request.key_prefix);
                    }

                    var license = new LicenseActivation
                    {
                        LicenseKey = licenseKey,
                        ExpiryDays = request.expiry_days,
                        Edition = request.edition.ToUpperInvariant(),
                        Status = "ACTIVE",
                        ServerRevision = 1,
                        CreatedAt = DateTime.UtcNow
                    };

                    licenses.Add(license);
                    generatedKeys.Add(licenseKey);
                }

                context.Set<LicenseActivation>().AddRange(licenses);
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Bulk generated {Count} licenses, Edition: {Edition}",
                       request.count, request.edition);

                return Ok(new BulkGenerateLicensesResponse
                {
                    success = true,
                    generated_count = generatedKeys.Count,
                    license_keys = generatedKeys,
                    message = $"Successfully generated {generatedKeys.Count} licenses"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error bulk generating licenses");
                return StatusCode(500, new { message = "Error generating licenses", error = ex.Message });
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Log license usage activity
        /// </summary>
        private async Task LogUsageAsync(
        ApplicationDbContext context,
            string licenseKey,
string action,
    string? hwid,
            string? oldEdition = null,
            string? newEdition = null,
 int? oldExpiryDays = null,
  int? newExpiryDays = null,
        string? notes = null)
        {
            try
            {
                var log = new LicenseUsageLog
                {
                    LicenseKey = licenseKey,
                    Hwid = hwid,
                    Action = action,
                    OldEdition = oldEdition,
                    NewEdition = newEdition,
                    OldExpiryDays = oldExpiryDays,
                    NewExpiryDays = newExpiryDays,
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString(),
                    UserAgent = HttpContext.Request.Headers["User-Agent"].ToString(),
                    CreatedAt = DateTime.UtcNow,
                    Notes = notes
                };

                context.Set<LicenseUsageLog>().Add(log);
                await context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log license usage for {Key}", licenseKey);
                // Don't throw - logging failure shouldn't break the main operation
            }
        }

        /// <summary>
        /// Generate a random license key
        /// Format: XXXX-XXXX-XXXX-XXXX or PREFIX-XXXX-XXXX-XXXX
        /// </summary>
        private string GenerateLicenseKey(string? prefix = null)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();

            string GenerateSegment() => new string(Enumerable.Range(0, 4)
                .Select(_ => chars[random.Next(chars.Length)])
              .ToArray());

            if (!string.IsNullOrEmpty(prefix))
            {
                return $"{prefix}-{GenerateSegment()}-{GenerateSegment()}-{GenerateSegment()}";
            }

            return $"{GenerateSegment()}-{GenerateSegment()}-{GenerateSegment()}-{GenerateSegment()}";
        }

        #endregion
    }
}
