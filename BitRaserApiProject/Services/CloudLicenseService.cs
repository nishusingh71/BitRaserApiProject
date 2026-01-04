using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using BitRaserApiProject.Factories;
using System.Security.Cryptography;
using System.Text;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// DTOs for Cloud License Activation
    /// </summary>
    public class CloudLoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class CloudLoginResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? Token { get; set; }
        public CloudUserInfo? User { get; set; }
        public List<CloudLicenseInfo>? Licenses { get; set; }
    }

    public class CloudUserInfo
    {
        public string Email { get; set; } = string.Empty;
        public string? Name { get; set; }
    }

    public class CloudLicenseInfo
    {
        public int Id { get; set; }
        public string LicenseKey { get; set; } = string.Empty;
        public string Edition { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ExpiryDate { get; set; }
        public int MaxDevices { get; set; }
        public int ActivatedDevices { get; set; }
        public bool CanActivate { get; set; }
    }

    public class CloudActivateRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string Hwid { get; set; } = string.Empty;
        public string? MachineName { get; set; }
        public string? OsInfo { get; set; }
    }

    public class CloudActivateResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ActivationToken { get; set; }
        public string? ExpiryDate { get; set; }
        public string? Edition { get; set; }
    }

    public class CloudDeviceInfo
    {
        public int Id { get; set; }
        public string Hwid { get; set; } = string.Empty;
        public string? MachineName { get; set; }
        public string? OsInfo { get; set; }
        public DateTime ActivatedAt { get; set; }
        public DateTime? LastSeen { get; set; }
        public bool IsActive { get; set; }
    }

    public class CloudDeactivateRequest
    {
        public int DeviceId { get; set; }
    }

    /// <summary>
    /// Interface for Cloud License Service
    /// </summary>
    public interface ICloudLicenseService
    {
        Task<CloudLoginResponse> LoginAsync(CloudLoginRequest request);
        Task<CloudActivateResponse> ActivateDeviceAsync(string userEmail, CloudActivateRequest request, string? ipAddress);
        Task<List<CloudLicenseInfo>> GetUserLicensesAsync(string userEmail);
        Task<List<CloudDeviceInfo>> GetLicenseDevicesAsync(string licenseKey);
        Task<bool> DeactivateDeviceAsync(string userEmail, int deviceId);
    }

    /// <summary>
    /// Cloud License Service - handles email/password login and multi-device activation
    /// </summary>
    public class CloudLicenseService : ICloudLicenseService
    {
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly ILogger<CloudLicenseService> _logger;
        private readonly IConfiguration _configuration;

        public CloudLicenseService(
            DynamicDbContextFactory contextFactory,
            ILogger<CloudLicenseService> logger,
            IConfiguration configuration)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Login with email/password and get assigned licenses
        /// </summary>
        public async Task<CloudLoginResponse> LoginAsync(CloudLoginRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogInformation("☁️ Cloud login attempt for: {Email}", request.Email);

                // Find user by email
                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.user_email == request.Email);

                if (user == null)
                {
                    _logger.LogWarning("❌ Cloud login failed - User not found: {Email}", request.Email);
                    return new CloudLoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Verify password using BCrypt
                bool passwordValid = false;
                
                if (!string.IsNullOrEmpty(user.hash_password))
                {
                    passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.hash_password);
                }
                else if (!string.IsNullOrEmpty(user.user_password))
                {
                    // Fallback to plaintext comparison (legacy)
                    passwordValid = user.user_password == request.Password;
                }

                if (!passwordValid)
                {
                    _logger.LogWarning("❌ Cloud login failed - Invalid password for: {Email}", request.Email);
                    return new CloudLoginResponse
                    {
                        Success = false,
                        Message = "Invalid email or password"
                    };
                }

                // Fetch user's assigned licenses
                var licenses = await GetUserLicensesAsync(request.Email);

                _logger.LogInformation("✅ Cloud login successful for: {Email}, Licenses: {Count}", 
                    request.Email, licenses.Count);

                return new CloudLoginResponse
                {
                    Success = true,
                    Message = "Login successful",
                    User = new CloudUserInfo
                    {
                        Email = user.user_email,
                        Name = user.user_name
                    },
                    Licenses = licenses
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Cloud login error for: {Email}", request.Email);
                return new CloudLoginResponse
                {
                    Success = false,
                    Message = "Login failed. Please try again."
                };
            }
        }

        /// <summary>
        /// Get all licenses assigned to a user
        /// </summary>
        public async Task<List<CloudLicenseInfo>> GetUserLicensesAsync(string userEmail)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var licenses = await context.Set<LicenseActivation>()
                .Where(l => l.UserEmail == userEmail)
                .ToListAsync();

            var result = new List<CloudLicenseInfo>();

            foreach (var license in licenses)
            {
                // Count active devices
                var activeDevices = await context.Set<LicenseDevice>()
                    .CountAsync(d => d.LicenseId == license.Id && d.IsActive);

                result.Add(new CloudLicenseInfo
                {
                    Id = license.Id,
                    LicenseKey = license.LicenseKey,
                    Edition = license.Edition,
                    Status = license.Status,
                    ExpiryDate = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                    MaxDevices = license.MaxDevices,
                    ActivatedDevices = activeDevices,
                    CanActivate = activeDevices < license.MaxDevices && license.Status == "ACTIVE" && !license.IsExpired
                });
            }

            return result;
        }

        /// <summary>
        /// Activate a device under a license (cloud activation)
        /// </summary>
        public async Task<CloudActivateResponse> ActivateDeviceAsync(string userEmail, CloudActivateRequest request, string? ipAddress)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                _logger.LogInformation("🔑 Cloud activation attempt: {LicenseKey}, HWID: {Hwid}", 
                    request.LicenseKey, request.Hwid);

                // Find license
                var license = await context.Set<LicenseActivation>()
                    .FirstOrDefaultAsync(l => l.LicenseKey == request.LicenseKey && l.UserEmail == userEmail);

                if (license == null)
                {
                    _logger.LogWarning("❌ License not found or not owned by user: {Key}", request.LicenseKey);
                    return new CloudActivateResponse
                    {
                        Success = false,
                        Message = "License not found or not assigned to your account"
                    };
                }

                // Check license status
                if (license.Status == "REVOKED")
                {
                    return new CloudActivateResponse
                    {
                        Success = false,
                        Message = "License has been revoked"
                    };
                }

                if (license.IsExpired)
                {
                    license.Status = "EXPIRED";
                    await context.SaveChangesAsync();
                    return new CloudActivateResponse
                    {
                        Success = false,
                        Message = "License has expired"
                    };
                }

                // Hash HWID for security
                var hwidHash = HashHwid(request.Hwid);

                // Check if device already activated
                var existingDevice = await context.Set<LicenseDevice>()
                    .FirstOrDefaultAsync(d => d.LicenseId == license.Id && d.HwidHash == hwidHash);

                if (existingDevice != null)
                {
                    // Update last seen
                    existingDevice.LastSeen = DateTime.UtcNow;
                    existingDevice.IsActive = true;
                    await context.SaveChangesAsync();

                    _logger.LogInformation("✅ Device already activated, updating last seen: {Hwid}", request.Hwid);

                    return new CloudActivateResponse
                    {
                        Success = true,
                        Message = "Device already activated",
                        ExpiryDate = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                        Edition = license.Edition
                    };
                }

                // Check device limit
                var activeDeviceCount = await context.Set<LicenseDevice>()
                    .CountAsync(d => d.LicenseId == license.Id && d.IsActive);

                if (activeDeviceCount >= license.MaxDevices)
                {
                    _logger.LogWarning("❌ Device limit reached for license: {Key}, Max: {Max}", 
                        request.LicenseKey, license.MaxDevices);
                    return new CloudActivateResponse
                    {
                        Success = false,
                        Message = $"Device limit reached. Maximum {license.MaxDevices} devices allowed. Please deactivate a device first."
                    };
                }

                // Add new device
                var newDevice = new LicenseDevice
                {
                    LicenseId = license.Id,
                    Hwid = request.Hwid,
                    HwidHash = hwidHash,
                    MachineName = request.MachineName,
                    OsInfo = request.OsInfo,
                    IpAddress = ipAddress,
                    ActivatedAt = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow,
                    IsActive = true
                };

                context.Set<LicenseDevice>().Add(newDevice);
                
                // Also update the legacy HWID field if not set
                if (string.IsNullOrEmpty(license.Hwid))
                {
                    license.Hwid = request.Hwid;
                }
                
                license.LastSeen = DateTime.UtcNow;
                license.ServerRevision++;

                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Cloud activation successful: {Key}, Device: {Machine}", 
                    request.LicenseKey, request.MachineName);

                return new CloudActivateResponse
                {
                    Success = true,
                    Message = "Device activated successfully",
                    ExpiryDate = license.ExpiryDate?.ToString("yyyy-MM-dd"),
                    Edition = license.Edition
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Cloud activation error: {Key}", request.LicenseKey);
                return new CloudActivateResponse
                {
                    Success = false,
                    Message = "Activation failed. Please try again."
                };
            }
        }

        /// <summary>
        /// Get all devices activated under a license
        /// </summary>
        public async Task<List<CloudDeviceInfo>> GetLicenseDevicesAsync(string licenseKey)
        {
            using var context = await _contextFactory.CreateDbContextAsync();

            var license = await context.Set<LicenseActivation>()
                .FirstOrDefaultAsync(l => l.LicenseKey == licenseKey);

            if (license == null)
                return new List<CloudDeviceInfo>();

            var devices = await context.Set<LicenseDevice>()
                .Where(d => d.LicenseId == license.Id)
                .OrderByDescending(d => d.ActivatedAt)
                .Select(d => new CloudDeviceInfo
                {
                    Id = d.Id,
                    Hwid = MaskHwid(d.Hwid), // Mask for security
                    MachineName = d.MachineName,
                    OsInfo = d.OsInfo,
                    ActivatedAt = d.ActivatedAt,
                    LastSeen = d.LastSeen,
                    IsActive = d.IsActive
                })
                .ToListAsync();

            return devices;
        }

        /// <summary>
        /// Deactivate a device (remote)
        /// </summary>
        public async Task<bool> DeactivateDeviceAsync(string userEmail, int deviceId)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var device = await context.Set<LicenseDevice>()
                    .Include(d => d.License)
                    .FirstOrDefaultAsync(d => d.Id == deviceId);

                if (device == null || device.License?.UserEmail != userEmail)
                {
                    _logger.LogWarning("❌ Device not found or unauthorized: {DeviceId}", deviceId);
                    return false;
                }

                device.IsActive = false;
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Device deactivated: {DeviceId}, Machine: {Machine}", 
                    deviceId, device.MachineName);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deactivating device: {DeviceId}", deviceId);
                return false;
            }
        }

        /// <summary>
        /// Hash HWID using SHA-256
        /// </summary>
        private string HashHwid(string hwid)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(hwid));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Mask HWID for display (show only first and last 4 chars)
        /// </summary>
        private static string MaskHwid(string hwid)
        {
            if (string.IsNullOrEmpty(hwid) || hwid.Length < 10)
                return "****";
            
            return $"{hwid[..4]}...{hwid[^4..]}";
        }
    }
}
