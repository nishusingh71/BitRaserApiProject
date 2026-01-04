using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using BitRaserApiProject.Models;
using BitRaserApiProject.Factories;

namespace BitRaserApiProject.Services
{
    #region DTOs

    public class OfflineRequestData
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string Hwid { get; set; } = string.Empty;
        public string MachineName { get; set; } = string.Empty;
        public string Os { get; set; } = string.Empty;
        public long Timestamp { get; set; }
        public string Checksum { get; set; } = string.Empty;
    }

    public class OfflineResponseData
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string Hwid { get; set; } = string.Empty;
        public string Edition { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public long ActivatedAt { get; set; }
        public long ValidUntil { get; set; }
        public string Signature { get; set; } = string.Empty;
    }

    public class GenerateRequestCodeRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string Hwid { get; set; } = string.Empty;
        public string? MachineName { get; set; }
        public string? Os { get; set; }
    }

    public class GenerateRequestCodeResponse
    {
        public bool Success { get; set; }
        public string? RequestCode { get; set; }
        public string? Message { get; set; }
    }

    public class SubmitOfflineRequestRequest
    {
        public string RequestCode { get; set; } = string.Empty;
    }

    public class SubmitOfflineRequestResponse
    {
        public bool Success { get; set; }
        public string? ResponseCode { get; set; }
        public string? Message { get; set; }
        public string? LicenseKey { get; set; }
        public string? Edition { get; set; }
        public string? ExpiryDate { get; set; }
    }

    public class ValidateOfflineCodeRequest
    {
        public string ResponseCode { get; set; } = string.Empty;
        public string Hwid { get; set; } = string.Empty;
    }

    public class ValidateOfflineCodeResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? LicenseKey { get; set; }
        public string? Edition { get; set; }
        public string? ExpiryDate { get; set; }
    }

    #endregion

    #region Model

    /// <summary>
    /// Offline activation request entity
    /// </summary>
    public class OfflineActivationRequest
    {
        public int Id { get; set; }
        public string RequestCode { get; set; } = string.Empty;
        public string LicenseKey { get; set; } = string.Empty;
        public string Hwid { get; set; } = string.Empty;
        public string HwidHash { get; set; } = string.Empty;
        public string? MachineName { get; set; }
        public string? OsInfo { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ProcessedAt { get; set; }
        public string? ResponseCode { get; set; }
        public string Status { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
        public string? ProcessedBy { get; set; }
    }

    #endregion

    /// <summary>
    /// Interface for Offline License Service
    /// </summary>
    public interface IOfflineLicenseService
    {
        GenerateRequestCodeResponse GenerateRequestCode(GenerateRequestCodeRequest request);
        Task<SubmitOfflineRequestResponse> SubmitRequestCodeAsync(SubmitOfflineRequestRequest request);
        ValidateOfflineCodeResponse ValidateResponseCode(ValidateOfflineCodeRequest request);
        OfflineRequestData? DecodeRequestCode(string requestCode);
        Task<List<OfflineActivationRequest>> GetPendingRequestsAsync();
        Task<bool> ApproveRequestAsync(int requestId, string approvedBy);
        Task<bool> RejectRequestAsync(int requestId, string rejectedBy, string reason);
    }

    /// <summary>
    /// Offline License Activation Service
    /// Handles request code generation, submission, and response code signing
    /// </summary>
    public class OfflineLicenseService : IOfflineLicenseService
    {
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly ILogger<OfflineLicenseService> _logger;
        private readonly IConfiguration _configuration;
        
        // Secret key for signing (should be in config)
        private readonly string _signingSecret;

        public OfflineLicenseService(
            DynamicDbContextFactory contextFactory,
            ILogger<OfflineLicenseService> logger,
            IConfiguration configuration)
        {
            _contextFactory = contextFactory;
            _logger = logger;
            _configuration = configuration;
            _signingSecret = configuration["License:SigningSecret"] 
                ?? configuration["JWT_SECRET"] 
                ?? "DSecure-Offline-Activation-Secret-2024";
        }

        /// <summary>
        /// Generate request code from license key and HWID
        /// Called by desktop app (offline)
        /// </summary>
        public GenerateRequestCodeResponse GenerateRequestCode(GenerateRequestCodeRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.LicenseKey) || string.IsNullOrEmpty(request.Hwid))
                {
                    return new GenerateRequestCodeResponse
                    {
                        Success = false,
                        Message = "License key and HWID are required"
                    };
                }

                var requestData = new OfflineRequestData
                {
                    LicenseKey = request.LicenseKey,
                    Hwid = request.Hwid,
                    MachineName = request.MachineName ?? Environment.MachineName,
                    Os = request.Os ?? Environment.OSVersion.ToString(),
                    Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds()
                };

                // Calculate checksum
                var dataForChecksum = $"{requestData.LicenseKey}|{requestData.Hwid}|{requestData.Timestamp}";
                requestData.Checksum = ComputeChecksum(dataForChecksum);

                // Serialize and encode
                var json = JsonSerializer.Serialize(requestData);
                var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                
                // Format: DSEC-OFF-{base64}
                var requestCode = $"DSEC-OFF-{encoded}";

                _logger.LogInformation("📝 Generated offline request code for key: {Key}", request.LicenseKey);

                return new GenerateRequestCodeResponse
                {
                    Success = true,
                    RequestCode = requestCode,
                    Message = "Request code generated. Submit this on the activation website."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error generating request code");
                return new GenerateRequestCodeResponse
                {
                    Success = false,
                    Message = "Failed to generate request code"
                };
            }
        }

        /// <summary>
        /// Decode request code to get data
        /// </summary>
        public OfflineRequestData? DecodeRequestCode(string requestCode)
        {
            try
            {
                if (!requestCode.StartsWith("DSEC-OFF-"))
                    return null;

                var encoded = requestCode.Substring(9); // Remove "DSEC-OFF-"
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                return JsonSerializer.Deserialize<OfflineRequestData>(json);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Submit request code and get response code
        /// Called via website (requires network)
        /// </summary>
        public async Task<SubmitOfflineRequestResponse> SubmitRequestCodeAsync(SubmitOfflineRequestRequest request)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                // Decode request code
                var requestData = DecodeRequestCode(request.RequestCode);
                if (requestData == null)
                {
                    return new SubmitOfflineRequestResponse
                    {
                        Success = false,
                        Message = "Invalid request code format"
                    };
                }

                // Verify checksum
                var expectedChecksum = ComputeChecksum($"{requestData.LicenseKey}|{requestData.Hwid}|{requestData.Timestamp}");
                if (requestData.Checksum != expectedChecksum)
                {
                    _logger.LogWarning("❌ Checksum mismatch for offline request");
                    return new SubmitOfflineRequestResponse
                    {
                        Success = false,
                        Message = "Invalid request code - tampered"
                    };
                }

                // Check if request is too old (max 7 days)
                var requestTime = DateTimeOffset.FromUnixTimeSeconds(requestData.Timestamp).UtcDateTime;
                if ((DateTime.UtcNow - requestTime).TotalDays > 7)
                {
                    return new SubmitOfflineRequestResponse
                    {
                        Success = false,
                        Message = "Request code expired. Please generate a new one."
                    };
                }

                // Find license
                var license = await context.Set<LicenseActivation>()
                    .FirstOrDefaultAsync(l => l.LicenseKey == requestData.LicenseKey);

                if (license == null)
                {
                    return new SubmitOfflineRequestResponse
                    {
                        Success = false,
                        Message = "License key not found"
                    };
                }

                if (license.Status == "REVOKED")
                {
                    return new SubmitOfflineRequestResponse
                    {
                        Success = false,
                        Message = "License has been revoked"
                    };
                }

                if (license.IsExpired)
                {
                    license.Status = "EXPIRED";
                    await context.SaveChangesAsync();
                    return new SubmitOfflineRequestResponse
                    {
                        Success = false,
                        Message = "License has expired"
                    };
                }

                // Check if HWID already bound (and different)
                if (!string.IsNullOrEmpty(license.Hwid) && 
                    !license.Hwid.Equals(requestData.Hwid, StringComparison.OrdinalIgnoreCase))
                {
                    return new SubmitOfflineRequestResponse
                    {
                        Success = false,
                        Message = "License is already activated on a different device"
                    };
                }

                // Generate response code
                var responseData = new OfflineResponseData
                {
                    LicenseKey = license.LicenseKey,
                    Hwid = requestData.Hwid,
                    Edition = license.Edition,
                    ExpiryDate = license.ExpiryDate?.ToString("yyyy-MM-dd") ?? "",
                    ActivatedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                    ValidUntil = DateTimeOffset.UtcNow.AddDays(365).ToUnixTimeSeconds()
                };

                // Sign the response
                var dataToSign = $"{responseData.LicenseKey}|{responseData.Hwid}|{responseData.Edition}|{responseData.ExpiryDate}|{responseData.ActivatedAt}";
                responseData.Signature = ComputeSignature(dataToSign);

                // Encode response
                var json = JsonSerializer.Serialize(responseData);
                var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                var responseCode = $"DSEC-RSP-{encoded}";

                // Update license
                license.Hwid = requestData.Hwid;
                license.LastSeen = DateTime.UtcNow;
                license.ServerRevision++;
                await context.SaveChangesAsync();

                _logger.LogInformation("✅ Offline activation approved for key: {Key}", license.LicenseKey);

                return new SubmitOfflineRequestResponse
                {
                    Success = true,
                    ResponseCode = responseCode,
                    Message = "Activation approved. Enter this response code in your desktop app.",
                    LicenseKey = license.LicenseKey,
                    Edition = license.Edition,
                    ExpiryDate = license.ExpiryDate?.ToString("yyyy-MM-dd")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error processing offline request");
                return new SubmitOfflineRequestResponse
                {
                    Success = false,
                    Message = "Failed to process request"
                };
            }
        }

        /// <summary>
        /// Validate response code on desktop app (offline)
        /// Verifies signature without network
        /// </summary>
        public ValidateOfflineCodeResponse ValidateResponseCode(ValidateOfflineCodeRequest request)
        {
            try
            {
                if (!request.ResponseCode.StartsWith("DSEC-RSP-"))
                {
                    return new ValidateOfflineCodeResponse
                    {
                        Success = false,
                        Message = "Invalid response code format"
                    };
                }

                // Decode
                var encoded = request.ResponseCode.Substring(9);
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(encoded));
                var responseData = JsonSerializer.Deserialize<OfflineResponseData>(json);

                if (responseData == null)
                {
                    return new ValidateOfflineCodeResponse
                    {
                        Success = false,
                        Message = "Invalid response code"
                    };
                }

                // Verify signature
                var dataToVerify = $"{responseData.LicenseKey}|{responseData.Hwid}|{responseData.Edition}|{responseData.ExpiryDate}|{responseData.ActivatedAt}";
                var expectedSignature = ComputeSignature(dataToVerify);

                if (responseData.Signature != expectedSignature)
                {
                    _logger.LogWarning("❌ Invalid signature in offline response code");
                    return new ValidateOfflineCodeResponse
                    {
                        Success = false,
                        Message = "Invalid response code - signature mismatch"
                    };
                }

                // Verify HWID matches
                if (!responseData.Hwid.Equals(request.Hwid, StringComparison.OrdinalIgnoreCase))
                {
                    return new ValidateOfflineCodeResponse
                    {
                        Success = false,
                        Message = "Response code is for a different device"
                    };
                }

                // Check validity period
                var validUntil = DateTimeOffset.FromUnixTimeSeconds(responseData.ValidUntil).UtcDateTime;
                if (DateTime.UtcNow > validUntil)
                {
                    return new ValidateOfflineCodeResponse
                    {
                        Success = false,
                        Message = "Activation token has expired"
                    };
                }

                _logger.LogInformation("✅ Offline response code validated for key: {Key}", responseData.LicenseKey);

                return new ValidateOfflineCodeResponse
                {
                    Success = true,
                    Message = "License activated successfully",
                    LicenseKey = responseData.LicenseKey,
                    Edition = responseData.Edition,
                    ExpiryDate = responseData.ExpiryDate
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validating offline response code");
                return new ValidateOfflineCodeResponse
                {
                    Success = false,
                    Message = "Invalid response code"
                };
            }
        }

        /// <summary>
        /// Get pending offline activation requests (admin)
        /// </summary>
        public async Task<List<OfflineActivationRequest>> GetPendingRequestsAsync()
        {
            // Note: This would require a separate table for manual approval workflow
            // For now, auto-approval is implemented in SubmitRequestCodeAsync
            return new List<OfflineActivationRequest>();
        }

        public Task<bool> ApproveRequestAsync(int requestId, string approvedBy)
        {
            // For manual approval workflow
            throw new NotImplementedException("Manual approval not implemented - using auto-approval");
        }

        public Task<bool> RejectRequestAsync(int requestId, string rejectedBy, string reason)
        {
            // For manual approval workflow
            throw new NotImplementedException("Manual rejection not implemented");
        }

        /// <summary>
        /// Compute SHA-256 checksum
        /// </summary>
        private string ComputeChecksum(string data)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Compute HMAC-SHA256 signature using secret key
        /// </summary>
        private string ComputeSignature(string data)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_signingSecret));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }
    }
}
