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
        private readonly ILicenseExportService _exportService;
        private readonly ICloudLicenseService _cloudService;
        private readonly IOfflineLicenseService _offlineService;
        private readonly IRsaTokenService _rsaService;
        private readonly ILicenseKeyGenerator _keyGenerator;

        public LicenseController(
              DynamicDbContextFactory contextFactory,
           ILogger<LicenseController> logger,
       IConfiguration configuration,
       ICacheService cacheService,
       ILicenseExportService exportService,
       ICloudLicenseService cloudService,
       IOfflineLicenseService offlineService,
       IRsaTokenService rsaService,
       ILicenseKeyGenerator keyGenerator)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _configuration = configuration;
            _cacheService = cacheService;
            _exportService = exportService;
            _cloudService = cloudService;
            _offlineService = offlineService;
            _rsaService = rsaService;
            _keyGenerator = keyGenerator;
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

        #region Cloud Activation Endpoints

        /// <summary>
        /// Cloud Login - authenticate with email/password and get assigned licenses
        /// POST /api/License/cloud/login
        /// </summary>
        [AllowAnonymous]
        [HttpPost("cloud/login")]
        [ProducesResponseType(typeof(CloudLoginResponse), 200)]
        public async Task<ActionResult<CloudLoginResponse>> CloudLogin([FromBody] CloudLoginRequest request)
        {
            var response = await _cloudService.LoginAsync(request);
            
            if (!response.Success)
            {
                return Unauthorized(response);
            }
            
            return Ok(response);
        }

        /// <summary>
        /// Cloud Activate - activate license on a device (requires prior login)
        /// POST /api/License/cloud/activate
        /// </summary>
        [Authorize]
        [HttpPost("cloud/activate")]
        [ProducesResponseType(typeof(CloudActivateResponse), 200)]
        public async Task<ActionResult<CloudActivateResponse>> CloudActivate([FromBody] CloudActivateRequest request)
        {
            // Get user email from claims
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new CloudActivateResponse
                {
                    Success = false,
                    Message = "User not authenticated"
                });
            }

            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            var response = await _cloudService.ActivateDeviceAsync(userEmail, request, ipAddress);
            
            return Ok(response);
        }

        /// <summary>
        /// Get user's licenses with device info
        /// GET /api/License/cloud/licenses
        /// </summary>
        [Authorize]
        [HttpGet("cloud/licenses")]
        [ProducesResponseType(typeof(List<CloudLicenseInfo>), 200)]
        public async Task<ActionResult<List<CloudLicenseInfo>>> GetCloudLicenses()
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var licenses = await _cloudService.GetUserLicensesAsync(userEmail);
            return Ok(licenses);
        }

        /// <summary>
        /// Get all devices activated under a license
        /// GET /api/License/cloud/devices/{licenseKey}
        /// </summary>
        [Authorize]
        [HttpGet("cloud/devices/{licenseKey}")]
        [ProducesResponseType(typeof(List<CloudDeviceInfo>), 200)]
        public async Task<ActionResult<List<CloudDeviceInfo>>> GetLicenseDevices(string licenseKey)
        {
            var devices = await _cloudService.GetLicenseDevicesAsync(licenseKey);
            return Ok(devices);
        }

        /// <summary>
        /// Deactivate a device remotely
        /// POST /api/License/cloud/deactivate
        /// </summary>
        [Authorize]
        [HttpPost("cloud/deactivate")]
        public async Task<IActionResult> CloudDeactivate([FromBody] CloudDeactivateRequest request)
        {
            var userEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value
                ?? User.FindFirst("email")?.Value
                ?? User.Identity?.Name;

            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { message = "User not authenticated" });
            }

            var success = await _cloudService.DeactivateDeviceAsync(userEmail, request.DeviceId);
            
            if (success)
            {
                return Ok(new { message = "Device deactivated successfully" });
            }
            
            return BadRequest(new { message = "Failed to deactivate device" });
        }

        #endregion

        #region Offline Activation Endpoints

        /// <summary>
        /// Generate offline request code (called by desktop app)
        /// POST /api/License/offline/generate-request
        /// </summary>
        [AllowAnonymous]
        [HttpPost("offline/generate-request")]
        [ProducesResponseType(typeof(GenerateRequestCodeResponse), 200)]
        public ActionResult<GenerateRequestCodeResponse> GenerateOfflineRequest([FromBody] GenerateRequestCodeRequest request)
        {
            var response = _offlineService.GenerateRequestCode(request);
            return Ok(response);
        }

        /// <summary>
        /// Submit offline request code and get response code (via website)
        /// POST /api/License/offline/submit
        /// </summary>
        [AllowAnonymous]
        [HttpPost("offline/submit")]
        [ProducesResponseType(typeof(SubmitOfflineRequestResponse), 200)]
        public async Task<ActionResult<SubmitOfflineRequestResponse>> SubmitOfflineRequest([FromBody] SubmitOfflineRequestRequest request)
        {
            var response = await _offlineService.SubmitRequestCodeAsync(request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }

        /// <summary>
        /// Validate offline response code (called by desktop app)
        /// POST /api/License/offline/validate
        /// </summary>
        [AllowAnonymous]
        [HttpPost("offline/validate")]
        [ProducesResponseType(typeof(ValidateOfflineCodeResponse), 200)]
        public ActionResult<ValidateOfflineCodeResponse> ValidateOfflineResponse([FromBody] ValidateOfflineCodeRequest request)
        {
            var response = _offlineService.ValidateResponseCode(request);
            
            if (!response.Success)
            {
                return BadRequest(response);
            }
            
            return Ok(response);
        }

        /// <summary>
        /// Decode request code to view details (admin/debug)
        /// POST /api/License/offline/decode
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("offline/decode")]
        public ActionResult DecodeOfflineRequest([FromBody] string requestCode)
        {
            var data = _offlineService.DecodeRequestCode(requestCode);
            
            if (data == null)
            {
                return BadRequest(new { message = "Invalid request code" });
            }
            
            return Ok(new
            {
                licenseKey = data.LicenseKey,
                hwid = data.Hwid,
                machineName = data.MachineName,
                os = data.Os,
                timestamp = DateTimeOffset.FromUnixTimeSeconds(data.Timestamp).UtcDateTime
            });
        }

        #endregion

        #region RSA Token Signing Endpoints

        /// <summary>
        /// Get RSA public key for client-side token verification
        /// GET /api/License/rsa/public-key
        /// </summary>
        [AllowAnonymous]
        [HttpGet("rsa/public-key")]
        public ActionResult GetRsaPublicKey()
        {
            return Ok(new { publicKey = _rsaService.GetPublicKey() });
        }

        /// <summary>
        /// Enhanced activation with detailed hardware info and RSA-signed token
        /// POST /api/License/rsa/activate
        /// </summary>
        [AllowAnonymous]
        [HttpPost("rsa/activate")]
        [ProducesResponseType(typeof(EnhancedActivationResponse), 200)]
        public async Task<ActionResult<EnhancedActivationResponse>> RsaActivate([FromBody] EnhancedActivationRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogInformation("🔐 RSA activation request for: {Key}", request.LicenseKey);

                // Find license
                var license = await context.Set<LicenseActivation>()
                    .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey);

                if (license == null)
                {
                    return BadRequest(new EnhancedActivationResponse
                    {
                        Success = false,
                        Message = "Invalid license key"
                    });
                }

                if (license.Status == "REVOKED")
                {
                    return BadRequest(new EnhancedActivationResponse
                    {
                        Success = false,
                        Message = "License has been revoked"
                    });
                }

                if (license.IsExpired)
                {
                    license.Status = "EXPIRED";
                    await context.SaveChangesAsync();
                    return BadRequest(new EnhancedActivationResponse
                    {
                        Success = false,
                        Message = "License has expired"
                    });
                }

                // Generate fingerprint from hardware info
                var fingerprint = request.Hardware.GenerateFingerprint();
                var hwidHash = ComputeHash(request.Hardware.Hwid);

                // Check existing device or create new
                var existingDevice = await context.Set<LicenseDevice>()
                    .FirstOrDefaultAsync(d => d.LicenseId == license.Id && d.HwidHash == hwidHash);

                if (existingDevice != null)
                {
                    // Update existing device
                    existingDevice.LastSeen = DateTime.UtcNow;
                    existingDevice.CpuId = request.Hardware.CpuId;
                    existingDevice.CpuName = request.Hardware.CpuName;
                    existingDevice.MacAddress = request.Hardware.MacAddress;
                    existingDevice.OsInfo = request.Hardware.OsVersion;
                    existingDevice.GpuInfo = request.Hardware.GpuInfo;
                    existingDevice.RamGb = request.Hardware.RamGb;
                }
                else
                {
                    // Check device limit
                    var activeDevices = await context.Set<LicenseDevice>()
                        .CountAsync(d => d.LicenseId == license.Id && d.IsActive);

                    if (activeDevices >= license.MaxDevices)
                    {
                        return BadRequest(new EnhancedActivationResponse
                        {
                            Success = false,
                            Message = $"Device limit reached ({license.MaxDevices})"
                        });
                    }

                    // Add new device with enhanced hardware details
                    var newDevice = new LicenseDevice
                    {
                        LicenseId = license.Id,
                        Hwid = request.Hardware.Hwid,
                        HwidHash = hwidHash,
                        HardwareFingerprint = fingerprint,
                        MachineName = request.Hardware.MachineName,
                        OsInfo = request.Hardware.OsVersion,
                        OsBuild = request.Hardware.OsBuild,
                        CpuId = request.Hardware.CpuId,
                        CpuName = request.Hardware.CpuName,
                        MacAddress = request.Hardware.MacAddress,
                        MotherboardSerial = request.Hardware.MotherboardSerial,
                        DiskSerial = request.Hardware.DiskSerial,
                        GpuInfo = request.Hardware.GpuInfo,
                        RamGb = request.Hardware.RamGb,
                        Timezone = request.Hardware.Timezone,
                        IpAddress = request.Hardware.IpAddress ?? HttpContext.Connection.RemoteIpAddress?.ToString(),
                        ActivatedAt = DateTime.UtcNow,
                        LastSeen = DateTime.UtcNow,
                        IsActive = true
                    };

                    context.Set<LicenseDevice>().Add(newDevice);
                }

                // Update license
                if (string.IsNullOrEmpty(license.Hwid))
                {
                    license.Hwid = request.Hardware.Hwid;
                }
                license.LastSeen = DateTime.UtcNow;
                license.ServerRevision++;

                await context.SaveChangesAsync();

                // Create RSA-signed token
                var signedToken = _rsaService.CreateSignedToken(
                    license.LicenseKey,
                    fingerprint,
                    license.Edition,
                    license.ExpiryDate ?? DateTime.UtcNow.AddYears(1)
                );

                var tokenBase64 = RsaTokenService.EncodeToken(signedToken);

                _logger.LogInformation("✅ RSA activation successful for: {Key}", license.LicenseKey);

                return Ok(new EnhancedActivationResponse
                {
                    Success = true,
                    Message = "Activation successful",
                    ActivationToken = tokenBase64,
                    PublicKey = _rsaService.GetPublicKey(),
                    Edition = license.Edition,
                    ExpiryDate = license.ExpiryDate?.ToString("yyyy-MM-dd")
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ RSA activation error");
                return StatusCode(500, new EnhancedActivationResponse
                {
                    Success = false,
                    Message = "Activation failed"
                });
            }
        }

        /// <summary>
        /// Verify RSA-signed activation token (can be done offline with public key)
        /// POST /api/License/rsa/verify
        /// </summary>
        [AllowAnonymous]
        [HttpPost("rsa/verify")]
        [ProducesResponseType(typeof(VerifyTokenResponse), 200)]
        public ActionResult<VerifyTokenResponse> RsaVerify([FromBody] VerifyTokenRequest request)
        {
            var isValid = _rsaService.VerifyToken(request.ActivationToken, request.CurrentHwid, out var token);

            if (!isValid || token == null)
            {
                return BadRequest(new VerifyTokenResponse
                {
                    Valid = false,
                    Message = "Invalid or expired token",
                    Status = "INVALID"
                });
            }

            return Ok(new VerifyTokenResponse
            {
                Valid = true,
                Message = "Token is valid",
                LicenseKey = token.LicenseKey,
                Edition = token.Edition,
                ExpiryDate = token.ExpiryDate,
                Status = "ACTIVE"
            });
        }

        /// <summary>
        /// Helper to compute SHA-256 hash
        /// </summary>
        private static string ComputeHash(string data)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }

        #endregion

        #region License Key Generation Endpoints

        /// <summary>
        /// Generate random license keys (preview only, not saved to DB)
        /// POST /api/License/generate/keys
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("generate/keys")]
        public ActionResult GenerateKeys([FromBody] GenerateLicenseKeyRequest request)
        {
            var keys = _keyGenerator.GenerateBatch(
                request.Count > 0 ? Math.Min(request.Count, 100) : 1, 
                request.Format, 
                request.Edition
            );

            return Ok(new
            {
                success = true,
                count = keys.Count,
                format = request.Format.ToString(),
                keys = keys
            });
        }

        /// <summary>
        /// Quick create license with auto-generated key
        /// POST /api/License/generate/quick
        /// No need to paste license key - it's generated automatically
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("generate/quick")]
        public async Task<ActionResult> QuickCreateLicense([FromBody] QuickCreateLicenseRequest request)
        {
            return await CreateAutoLicense(request);
        }

        /// <summary>
        /// Auto-create license (for testing) - generates and stores key
        /// POST /api/License/auto-create
        /// </summary>
        [AllowAnonymous]
        [HttpPost("auto-create")]
        public async Task<ActionResult> AutoCreateLicense([FromBody] QuickCreateLicenseRequest? request = null)
        {
            return await CreateAutoLicense(request ?? new QuickCreateLicenseRequest());
        }

        private async Task<ActionResult> CreateAutoLicense(QuickCreateLicenseRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Auto-generate license key based on edition
                var format = request.Edition?.ToUpperInvariant() switch
                {
                    "BASIC" => LicenseKeyFormat.EditionBased,
                    "PRO" => LicenseKeyFormat.EditionBased,
                    "ENTERPRISE" => LicenseKeyFormat.EditionBased,
                    _ => LicenseKeyFormat.DSecure
                };

                string licenseKey;
                int attempts = 0;
                
                do
                {
                    licenseKey = _keyGenerator.Generate(format, request.Edition);
                    var exists = await context.Set<LicenseActivation>()
                        .AnyAsync(l => l.LicenseKey == licenseKey);
                    
                    if (!exists) break;
                    attempts++;
                } while (attempts < 10);

                // Create license
                var license = new LicenseActivation
                {
                    LicenseKey = licenseKey,
                    ExpiryDays = request.ExpiryDays > 0 ? request.ExpiryDays : 365,
                    Edition = request.Edition ?? "PRO",
                    Status = "ACTIVE",
                    UserEmail = request.UserEmail,
                    Notes = request.Notes ?? $"Auto-generated on {DateTime.UtcNow:yyyy-MM-dd}",
                    CreatedAt = DateTime.UtcNow,
                    ServerRevision = 1
                };

                context.Set<LicenseActivation>().Add(license);
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Quick license created: {Key}, Edition: {Edition}", 
                    licenseKey, license.Edition);

                return Ok(new
                {
                    success = true,
                    message = "License created successfully",
                    license_key = licenseKey,
                    edition = license.Edition,
                    expiry_days = license.ExpiryDays,
                    expiry_date = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                    max_devices = license.MaxDevices,
                    user_email = license.UserEmail
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in quick license creation");
                return StatusCode(500, new { message = "Failed to create license", error = ex.Message });
            }
        }

        /// <summary>
        /// Batch create multiple licenses with auto-generated keys
        /// POST /api/License/generate/batch
        /// </summary>
        [Authorize(Roles = "SuperAdmin")]
        [HttpPost("generate/batch")]
        public async Task<ActionResult> BatchCreateLicenses([FromBody] BatchCreateLicenseRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var count = Math.Min(request.Count, 100); // Max 100 at a time
                var createdLicenses = new List<object>();
                var format = request.Format != LicenseKeyFormat.Standard ? request.Format : LicenseKeyFormat.DSecure;

                for (int i = 0; i < count; i++)
                {
                    string licenseKey;
                    int attempts = 0;
                    
                    do
                    {
                        licenseKey = _keyGenerator.Generate(format, request.Edition);
                        var exists = await context.Set<LicenseActivation>()
                            .AnyAsync(l => l.LicenseKey == licenseKey);
                        
                        if (!exists) break;
                        attempts++;
                    } while (attempts < 10);

                    var license = new LicenseActivation
                    {
                        LicenseKey = licenseKey,
                        ExpiryDays = request.ExpiryDays > 0 ? request.ExpiryDays : 365,
                        Edition = request.Edition ?? "PRO",
                        Status = "ACTIVE",
                        MaxDevices = request.MaxDevices > 0 ? request.MaxDevices : 1,
                        Notes = request.Notes ?? $"Batch generated on {DateTime.UtcNow:yyyy-MM-dd}",
                        CreatedAt = DateTime.UtcNow,
                        ServerRevision = 1
                    };

                    context.Set<LicenseActivation>().Add(license);
                    
                    createdLicenses.Add(new
                    {
                        license_key = licenseKey,
                        edition = license.Edition,
                        expiry_date = license.ExpiryDate?.ToString("yyyy-MM-dd")
                    });
                }

                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Batch created {Count} licenses", createdLicenses.Count);

                return Ok(new
                {
                    success = true,
                    message = $"Created {createdLicenses.Count} licenses",
                    count = createdLicenses.Count,
                    licenses = createdLicenses
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error in batch license creation");
                return StatusCode(500, new { message = "Failed to create licenses", error = ex.Message });
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
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
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
                    .AsNoTracking()  // ✅ RENDER OPTIMIZATION
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
        /// Transfer licenses between users/groups
        /// POST /api/License/transfer
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpPost("transfer")]
        public async Task<IActionResult> TransferLicenses([FromBody] LicenseTransferRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Validate source entity
                var sourceLicenses = await context.Set<LicenseActivation>()
                    .Where(l => l.UserEmail == request.FromEntityId && l.Status == "ACTIVE")
                    .Take(request.LicenseCount)
                    .ToListAsync();

                if (sourceLicenses.Count < request.LicenseCount)
                    return BadRequest(new { error = $"Source only has {sourceLicenses.Count} active licenses, requested {request.LicenseCount}" });

                // Transfer licenses
                foreach (var license in sourceLicenses)
                {
                    license.UserEmail = request.ToEntityId;
                    license.Notes = $"Transferred from {request.FromEntityId}: {request.Reason ?? "No reason provided"}";
                    license.ServerRevision++;
                }

                await context.SaveChangesAsync();

                _logger.LogInformation("🔄 Transferred {Count} licenses from {From} to {To}. Reason: {Reason}",
                    sourceLicenses.Count, request.FromEntityId, request.ToEntityId, request.Reason);

                return Ok(new
                {
                    success = true,
                    message = $"Transferred {sourceLicenses.Count} licenses successfully",
                    from = request.FromEntityId,
                    to = request.ToEntityId,
                    count = sourceLicenses.Count,
                    transferredAt = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error transferring licenses");
                return StatusCode(500, new { message = "Error transferring licenses", error = ex.Message });
            }
        }

        /// <summary>
        /// Export licenses to Excel
        /// GET /api/License/export/excel
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("export/excel")]
        public async Task<IActionResult> ExportLicensesToExcel([FromQuery] string? status = null, [FromQuery] string? edition = null)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var query = context.Set<LicenseActivation>().AsNoTracking();

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(l => l.Status == status.ToUpper());

                if (!string.IsNullOrEmpty(edition))
                    query = query.Where(l => l.Edition == edition.ToUpper());

                var licenses = await query.OrderByDescending(l => l.CreatedAt).ToListAsync();

                using var workbook = new ClosedXML.Excel.XLWorkbook();
                var sheet = workbook.Worksheets.Add("Licenses");

                // Header
                sheet.Cell("A1").Value = "License Key";
                sheet.Cell("B1").Value = "Status";
                sheet.Cell("C1").Value = "Edition";
                sheet.Cell("D1").Value = "User Email";
                sheet.Cell("E1").Value = "Expiry Date";
                sheet.Cell("F1").Value = "Remaining Days";
                sheet.Cell("G1").Value = "Created At";
                sheet.Cell("H1").Value = "Hardware ID";

                var headerRange = sheet.Range("A1:H1");
                headerRange.Style.Font.Bold = true;
                headerRange.Style.Fill.BackgroundColor = ClosedXML.Excel.XLColor.LightGray;

                // Data
                int row = 2;
                foreach (var license in licenses)
                {
                    sheet.Cell($"A{row}").Value = license.LicenseKey;
                    sheet.Cell($"B{row}").Value = license.Status;
                    sheet.Cell($"C{row}").Value = license.Edition;
                    sheet.Cell($"D{row}").Value = license.UserEmail ?? "";
                    sheet.Cell($"E{row}").Value = license.ExpiryDate?.ToString("yyyy-MM-dd") ?? "";
                    sheet.Cell($"F{row}").Value = license.RemainingDays ?? 0;
                    sheet.Cell($"G{row}").Value = license.CreatedAt.ToString("yyyy-MM-dd HH:mm");
                    sheet.Cell($"H{row}").Value = license.Hwid ?? "";
                    row++;
                }

                sheet.Columns().AdjustToContents();

                using var stream = new MemoryStream();
                workbook.SaveAs(stream);

                _logger.LogInformation("📊 Exported {Count} licenses to Excel", licenses.Count);

                return File(
                    stream.ToArray(),
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"licenses_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx"
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exporting licenses to Excel");
                return StatusCode(500, new { message = "Error exporting licenses", error = ex.Message });
            }
        }

        /// <summary>
        /// Export licenses summary to PDF
        /// GET /api/License/export/pdf
        /// </summary>
        [Authorize(Roles = "SuperAdmin,Admin")]
        [HttpGet("export/pdf")]
        public async Task<IActionResult> ExportLicensesToPdf()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var total = await context.Set<LicenseActivation>().CountAsync();
                var active = await context.Set<LicenseActivation>().CountAsync(l => l.Status == "ACTIVE");
                var expired = await context.Set<LicenseActivation>().CountAsync(l => l.Status == "EXPIRED");
                var revoked = await context.Set<LicenseActivation>().CountAsync(l => l.Status == "REVOKED");

                var recentLicenses = await context.Set<LicenseActivation>()
                    .OrderByDescending(l => l.CreatedAt)
                    .Take(20)
                    .ToListAsync();

                QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
                
                var document = QuestPDF.Fluent.Document.Create(container =>
                {
                    container.Page(page =>
                    {
                        page.Size(QuestPDF.Helpers.PageSizes.A4);
                        page.Margin(40);

                        page.Header().Height(60).Background("#1a1a2e").AlignCenter().AlignMiddle()
                            .Text("License Summary Report").FontSize(20).FontColor("#ffffff").Bold();

                        page.Content().PaddingVertical(20).Column(col =>
                        {
                            col.Spacing(15);

                            // Statistics
                            col.Item().Text("License Statistics").FontSize(16).Bold();
                            col.Item().Text($"Total Licenses: {total}");
                            col.Item().Text($"Active: {active}");
                            col.Item().Text($"Expired: {expired}");
                            col.Item().Text($"Revoked: {revoked}");

                            col.Item().PaddingTop(20).Text("Recent Licenses (Last 20)").FontSize(16).Bold();

                            // Table
                            col.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn(3);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(1);
                                    columns.RelativeColumn(2);
                                });

                                table.Header(header =>
                                {
                                    header.Cell().Background("#e0e0e0").Padding(5).Text("License Key").Bold();
                                    header.Cell().Background("#e0e0e0").Padding(5).Text("Status").Bold();
                                    header.Cell().Background("#e0e0e0").Padding(5).Text("Edition").Bold();
                                    header.Cell().Background("#e0e0e0").Padding(5).Text("Created").Bold();
                                });

                                foreach (var license in recentLicenses)
                                {
                                    table.Cell().BorderBottom(1).BorderColor("#cccccc").Padding(5).Text(license.LicenseKey ?? "");
                                    table.Cell().BorderBottom(1).BorderColor("#cccccc").Padding(5).Text(license.Status ?? "");
                                    table.Cell().BorderBottom(1).BorderColor("#cccccc").Padding(5).Text(license.Edition ?? "");
                                    table.Cell().BorderBottom(1).BorderColor("#cccccc").Padding(5).Text(license.CreatedAt.ToString("yyyy-MM-dd"));
                                }
                            });
                        });

                        page.Footer().AlignCenter()
                            .Text($"Generated on {DateTime.UtcNow:yyyy-MM-dd HH:mm} UTC | D-Secure Technologies");
                    });
                });

                var pdfBytes = document.GeneratePdf();

                _logger.LogInformation("📄 Exported license summary to PDF ({Total} total licenses)", total);

                return File(pdfBytes, "application/pdf", $"license_summary_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exporting licenses to PDF");
                return StatusCode(500, new { message = "Error exporting licenses to PDF", error = ex.Message });
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

        /// <summary>
        /// Get license statistics summary
        /// GET /api/License/stats
        /// </summary>
        [Authorize]
        [HttpGet("stats")]
        public async Task<IActionResult> GetLicenseStats()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                var stats = new
                {
                    total = await context.Set<LicenseActivation>().CountAsync(),
                    active = await context.Set<LicenseActivation>().Where(l => l.Status == "ACTIVE").CountAsync(),
                    inactive = await context.Set<LicenseActivation>().Where(l => l.Status == "INACTIVE").CountAsync(),
                    expired = await context.Set<LicenseActivation>().Where(l => l.Status == "EXPIRED").CountAsync(),
                    revoked = await context.Set<LicenseActivation>().Where(l => l.Status == "REVOKED").CountAsync()
                };
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching license stats");
                return StatusCode(500, new { message = "Error fetching stats", error = ex.Message });
            }
        }

        /// <summary>
        /// Get license distribution by edition
        /// GET /api/License/distribution
        /// </summary>
        [Authorize]
        [HttpGet("distribution")]
        public async Task<IActionResult> GetLicenseDistribution()
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();
                
                var total = await context.Set<LicenseActivation>().CountAsync();
                
                if (total == 0)
                {
                    return Ok(new { licenseDetails = new List<object>() });
                }
                
                var distribution = await context.Set<LicenseActivation>()
                    .GroupBy(l => l.Edition)
                    .Select(g => new
                    {
                        type = g.Key,
                        count = g.Count(),
                        percentage = (int)Math.Round((double)g.Count() / total * 100)
                    })
                    .ToListAsync();

                return Ok(new { licenseDetails = distribution });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error fetching license distribution");
                return StatusCode(500, new { message = "Error fetching distribution", error = ex.Message });
            }
        }

        /// <summary>
        /// Export all licenses to Excel or PDF
        /// GET /api/License/admin/export?format=excel|pdf
        /// </summary>
        [Authorize(Roles = "SuperAdmin")]
        [HttpGet("admin/export")]
        [ProducesResponseType(typeof(FileContentResult), 200)]
        public async Task<IActionResult> ExportLicenses([FromQuery] string format = "excel")
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogInformation("📊 License export requested, Format: {Format}", format);

                // Fetch all licenses
                var licenses = await context.Set<LicenseActivation>()
                    .OrderByDescending(l => l.CreatedAt)
                    .Select(l => new LicenseExportData
                    {
                        Id = l.Id,
                        LicenseKey = l.LicenseKey,
                        Hwid = l.Hwid,
                        ExpiryDate = l.ExpiryDate != null ? l.ExpiryDate.Value.ToString("yyyy-MM-dd") : null,
                        Edition = l.Edition,
                        Status = l.Status,
                        UserEmail = l.UserEmail,
                        CreatedAt = l.CreatedAt,
                        LastSeen = l.LastSeen
                    })
                    .ToListAsync();

                _logger.LogInformation("📋 Exporting {Count} licenses", licenses.Count);

                byte[] fileBytes;
                string contentType;
                string fileName;

                if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
                {
                    fileBytes = _exportService.ExportToPdf(licenses);
                    contentType = "application/pdf";
                    fileName = $"DSecure_Licenses_{DateTime.UtcNow:yyyyMMdd_HHmmss}.pdf";
                }
                else
                {
                    // Default to Excel
                    fileBytes = _exportService.ExportToExcel(licenses);
                    contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                    fileName = $"DSecure_Licenses_{DateTime.UtcNow:yyyyMMdd_HHmmss}.xlsx";
                }

                _logger.LogInformation("✅ Export complete: {FileName}, Size: {Size} bytes", fileName, fileBytes.Length);

                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error exporting licenses");
                return StatusCode(500, new { message = "Error exporting licenses", error = ex.Message });
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

    /// <summary>
    /// Request model for transferring licenses between users/groups
    /// </summary>
    public class LicenseTransferRequest
    {
        /// <summary>
        /// Source entity type (user, group)
        /// </summary>
        public string FromEntityType { get; set; } = "user";

        /// <summary>
        /// Source entity identifier (email or group id)
        /// </summary>
        public string FromEntityId { get; set; } = "";

        /// <summary>
        /// Target entity type (user, group)
        /// </summary>
        public string ToEntityType { get; set; } = "user";

        /// <summary>
        /// Target entity identifier (email or group id)
        /// </summary>
        public string ToEntityId { get; set; } = "";

        /// <summary>
        /// Number of licenses to transfer
        /// </summary>
        public int LicenseCount { get; set; } = 1;

        /// <summary>
        /// Reason for the transfer
        /// </summary>
        public string? Reason { get; set; }
    }
}
