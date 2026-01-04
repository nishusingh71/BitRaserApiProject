using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace BitRaserApiProject.Services
{
    #region DTOs

    /// <summary>
    /// Enhanced hardware fingerprint with detailed system info
    /// </summary>
    public class HardwareFingerprint
    {
        public string Hwid { get; set; } = string.Empty;
        public string? CpuId { get; set; }
        public string? CpuName { get; set; }
        public string? MacAddress { get; set; }
        public string? MotherboardSerial { get; set; }
        public string? DiskSerial { get; set; }
        public string? OsVersion { get; set; }
        public string? OsBuild { get; set; }
        public string? MachineName { get; set; }
        public string? UserName { get; set; }
        public int? RamGb { get; set; }
        public string? GpuInfo { get; set; }
        public string? IpAddress { get; set; }
        public string? Timezone { get; set; }
        public long Timestamp { get; set; }

        /// <summary>
        /// Generate combined hardware hash
        /// </summary>
        public string GenerateFingerprint()
        {
            var data = $"{CpuId}|{MacAddress}|{MotherboardSerial}|{DiskSerial}|{MachineName}";
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(bytes);
        }
    }

    /// <summary>
    /// RSA-signed activation token
    /// </summary>
    public class SignedActivationToken
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string HardwareFingerprint { get; set; } = string.Empty;
        public string Edition { get; set; } = string.Empty;
        public string ExpiryDate { get; set; } = string.Empty;
        public long IssuedAt { get; set; }
        public long ExpiresAt { get; set; }
        public string Issuer { get; set; } = "DSecure";
        public string Signature { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for activation with enhanced hardware details
    /// </summary>
    public class EnhancedActivationRequest
    {
        public string LicenseKey { get; set; } = string.Empty;
        public HardwareFingerprint Hardware { get; set; } = new();
    }

    /// <summary>
    /// Response with RSA-signed token
    /// </summary>
    public class EnhancedActivationResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public string? ActivationToken { get; set; }
        public string? PublicKey { get; set; }
        public string? Edition { get; set; }
        public string? ExpiryDate { get; set; }
    }

    /// <summary>
    /// Token verification request
    /// </summary>
    public class VerifyTokenRequest
    {
        public string ActivationToken { get; set; } = string.Empty;
        public string CurrentHwid { get; set; } = string.Empty;
    }

    /// <summary>
    /// Token verification response
    /// </summary>
    public class VerifyTokenResponse
    {
        public bool Valid { get; set; }
        public string? Message { get; set; }
        public string? LicenseKey { get; set; }
        public string? Edition { get; set; }
        public string? ExpiryDate { get; set; }
        public string? Status { get; set; }
    }

    #endregion

    /// <summary>
    /// Interface for RSA Token Service
    /// </summary>
    public interface IRsaTokenService
    {
        string GetPublicKey();
        SignedActivationToken CreateSignedToken(string licenseKey, string fingerprint, string edition, DateTime expiry);
        bool VerifyToken(string tokenBase64, string currentHwid, out SignedActivationToken? token);
        string SignData(string data);
        bool VerifySignature(string data, string signature);
    }

    /// <summary>
    /// RSA Token Service - handles cryptographic signing of activation tokens
    /// Uses RSA-2048 for signing and verification
    /// </summary>
    public class RsaTokenService : IRsaTokenService
    {
        private readonly ILogger<RsaTokenService> _logger;
        private readonly IConfiguration _configuration;
        private readonly RSA _rsa;
        private readonly string _publicKey;
        private readonly string _privateKey;

        public RsaTokenService(ILogger<RsaTokenService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            // Load or generate RSA keys
            var privateKeyPath = configuration["License:RsaPrivateKeyPath"];
            var publicKeyPath = configuration["License:RsaPublicKeyPath"];

            if (!string.IsNullOrEmpty(privateKeyPath) && File.Exists(privateKeyPath))
            {
                // Load from file
                _privateKey = File.ReadAllText(privateKeyPath);
                _publicKey = File.ReadAllText(publicKeyPath ?? privateKeyPath.Replace("private", "public"));
                _rsa = RSA.Create();
                _rsa.ImportFromPem(_privateKey);
            }
            else
            {
                // Generate new keys (for development)
                _rsa = RSA.Create(2048);
                _privateKey = _rsa.ExportRSAPrivateKeyPem();
                _publicKey = _rsa.ExportRSAPublicKeyPem();
                
                _logger.LogWarning("⚠️ Generated new RSA keys - for production, use stored keys");
            }

            _logger.LogInformation("🔐 RSA Token Service initialized");
        }

        /// <summary>
        /// Get public key (for client-side verification)
        /// </summary>
        public string GetPublicKey() => _publicKey;

        /// <summary>
        /// Create RSA-signed activation token
        /// </summary>
        public SignedActivationToken CreateSignedToken(string licenseKey, string fingerprint, string edition, DateTime expiry)
        {
            var token = new SignedActivationToken
            {
                LicenseKey = licenseKey,
                HardwareFingerprint = fingerprint,
                Edition = edition,
                ExpiryDate = expiry.ToString("yyyy-MM-dd"),
                IssuedAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                ExpiresAt = new DateTimeOffset(expiry).ToUnixTimeSeconds(),
                Issuer = "DSecure"
            };

            // Create signature payload
            var payload = $"{token.LicenseKey}|{token.HardwareFingerprint}|{token.Edition}|{token.ExpiryDate}|{token.IssuedAt}|{token.ExpiresAt}";
            token.Signature = SignData(payload);

            _logger.LogInformation("🔏 Created signed token for license: {Key}", licenseKey);

            return token;
        }

        /// <summary>
        /// Sign data with RSA private key
        /// </summary>
        public string SignData(string data)
        {
            var dataBytes = Encoding.UTF8.GetBytes(data);
            var signature = _rsa.SignData(dataBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            return Convert.ToBase64String(signature);
        }

        /// <summary>
        /// Verify signature with RSA public key
        /// </summary>
        public bool VerifySignature(string data, string signature)
        {
            try
            {
                var dataBytes = Encoding.UTF8.GetBytes(data);
                var signatureBytes = Convert.FromBase64String(signature);
                
                using var rsaPublic = RSA.Create();
                rsaPublic.ImportFromPem(_publicKey);
                
                return rsaPublic.VerifyData(dataBytes, signatureBytes, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Signature verification failed");
                return false;
            }
        }

        /// <summary>
        /// Verify token and check if still valid
        /// </summary>
        public bool VerifyToken(string tokenBase64, string currentHwid, out SignedActivationToken? token)
        {
            token = null;
            
            try
            {
                // Decode token
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(tokenBase64));
                token = JsonSerializer.Deserialize<SignedActivationToken>(json);

                if (token == null)
                {
                    _logger.LogWarning("❌ Failed to deserialize token");
                    return false;
                }

                // Verify signature
                var payload = $"{token.LicenseKey}|{token.HardwareFingerprint}|{token.Edition}|{token.ExpiryDate}|{token.IssuedAt}|{token.ExpiresAt}";
                if (!VerifySignature(payload, token.Signature))
                {
                    _logger.LogWarning("❌ Token signature invalid");
                    return false;
                }

                // Check hardware fingerprint
                using var sha256 = SHA256.Create();
                var currentFingerprint = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(currentHwid)));
                
                // Allow matching by either raw HWID or hashed fingerprint
                if (token.HardwareFingerprint != currentHwid && 
                    token.HardwareFingerprint != currentFingerprint)
                {
                    _logger.LogWarning("❌ Hardware mismatch");
                    return false;
                }

                // Check expiry
                var expiresAt = DateTimeOffset.FromUnixTimeSeconds(token.ExpiresAt).UtcDateTime;
                if (DateTime.UtcNow > expiresAt)
                {
                    _logger.LogWarning("❌ Token expired");
                    return false;
                }

                _logger.LogInformation("✅ Token verified for license: {Key}", token.LicenseKey);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Token verification error");
                return false;
            }
        }

        /// <summary>
        /// Encode token to base64 string
        /// </summary>
        public static string EncodeToken(SignedActivationToken token)
        {
            var json = JsonSerializer.Serialize(token);
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
        }
    }
}
