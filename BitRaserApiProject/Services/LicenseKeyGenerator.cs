using System.Security.Cryptography;
using System.Text;

namespace DSecureApi.Services
{
    /// <summary>
    /// License key format types
    /// </summary>
    public enum LicenseKeyFormat
    {
        /// <summary>XXXX-XXXX-XXXX-XXXX (16 chars)</summary>
        Standard,
        
        /// <summary>DSEC-XXXX-XXXX-XXXX-XXXX (20 chars with prefix)</summary>
        DSecure,
        
        /// <summary>XXXXX-XXXXX-XXXXX-XXXXX-XXXXX (25 chars, Windows style)</summary>
        WindowsStyle,
        
        /// <summary>xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx (UUID format)</summary>
        UUID,
        
        /// <summary>DSEC-PRO-XXXXXXXX (Edition-based)</summary>
        EditionBased,
        
        /// <summary>Custom format with prefix</summary>
        CustomPrefix
    }

    /// <summary>
    /// License key generation request
    /// </summary>
    public class GenerateLicenseKeyRequest
    {
        public LicenseKeyFormat Format { get; set; } = LicenseKeyFormat.DSecure;
        public string? Edition { get; set; }
        public string? CustomPrefix { get; set; }
        public int Count { get; set; } = 1;
    }

    /// <summary>
    /// Quick create license request - auto-generates key
    /// </summary>
    public class QuickCreateLicenseRequest
    {
        public string? Edition { get; set; } = "PRO";
        public int ExpiryDays { get; set; } = 365;
        public int MaxDevices { get; set; } = 1;
        public string? UserEmail { get; set; }
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Batch create licenses request
    /// </summary>
    public class BatchCreateLicenseRequest
    {
        public int Count { get; set; } = 10;
        public LicenseKeyFormat Format { get; set; } = LicenseKeyFormat.DSecure;
        public string? Edition { get; set; } = "PRO";
        public int ExpiryDays { get; set; } = 365;
        public int MaxDevices { get; set; } = 1;
        public string? Notes { get; set; }
    }

    /// <summary>
    /// Interface for License Key Generator
    /// </summary>
    public interface ILicenseKeyGenerator
    {
        string Generate(LicenseKeyFormat format = LicenseKeyFormat.DSecure, string? edition = null, string? customPrefix = null);
        List<string> GenerateBatch(int count, LicenseKeyFormat format = LicenseKeyFormat.DSecure, string? edition = null);
        bool IsValidFormat(string key);
        string GetChecksum(string key);
    }

    /// <summary>
    /// Comprehensive License Key Generator
    /// Generates secure, unique, verifiable license keys
    /// </summary>
    public class LicenseKeyGenerator : ILicenseKeyGenerator
    {
        private const string ALPHANUMERIC = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        private const string ALPHANUMERIC_LOWER = "abcdefghijklmnopqrstuvwxyz0123456789";
        private const string HEX = "0123456789ABCDEF";
        
        private readonly ILogger<LicenseKeyGenerator> _logger;

        public LicenseKeyGenerator(ILogger<LicenseKeyGenerator> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Generate a license key in specified format
        /// </summary>
        public string Generate(LicenseKeyFormat format = LicenseKeyFormat.DSecure, string? edition = null, string? customPrefix = null)
        {
            var key = format switch
            {
                LicenseKeyFormat.Standard => GenerateStandard(),
                LicenseKeyFormat.DSecure => GenerateDSecure(),
                LicenseKeyFormat.WindowsStyle => GenerateWindowsStyle(),
                LicenseKeyFormat.UUID => GenerateUUID(),
                LicenseKeyFormat.EditionBased => GenerateEditionBased(edition ?? "PRO"),
                LicenseKeyFormat.CustomPrefix => GenerateWithPrefix(customPrefix ?? "KEY"),
                _ => GenerateDSecure()
            };

            _logger.LogDebug("🔑 Generated license key: {Key}", key);
            return key;
        }

        /// <summary>
        /// Generate batch of unique keys
        /// </summary>
        public List<string> GenerateBatch(int count, LicenseKeyFormat format = LicenseKeyFormat.DSecure, string? edition = null)
        {
            var keys = new HashSet<string>();
            
            while (keys.Count < count)
            {
                keys.Add(Generate(format, edition));
            }

            _logger.LogInformation("🔑 Generated {Count} license keys", count);
            return keys.ToList();
        }

        /// <summary>
        /// Standard format: XXXX-XXXX-XXXX-XXXX (16 chars)
        /// </summary>
        private string GenerateStandard()
        {
            return $"{GenerateSegment(4)}-{GenerateSegment(4)}-{GenerateSegment(4)}-{GenerateSegment(4)}";
        }

        /// <summary>
        /// DSecure format: XXXX-XXXX-XXXX-XXXX (16 chars, same as standard)
        /// </summary>
        private string GenerateDSecure()
        {
            return GenerateStandard(); // Exactly 16 chars
        }

        /// <summary>
        /// Windows style: XXXX-XXXX-XXXX-XXXX (16 chars)
        /// </summary>
        private string GenerateWindowsStyle()
        {
            return GenerateStandard(); // Exactly 16 chars
        }

        /// <summary>
        /// UUID format: XXXX-XXXX-XXXX-XXXX (16 chars)
        /// </summary>
        private string GenerateUUID()
        {
            return GenerateStandard(); // Exactly 16 chars
        }

        /// <summary>
        /// Edition based: XXXX-XXXX-XXXX-XXXX (16 chars)
        /// </summary>
        private string GenerateEditionBased(string edition)
        {
            return GenerateStandard(); // Exactly 16 chars
        }

        /// <summary>
        /// Custom prefix format: XXXX-XXXX-XXXX-XXXX (16 chars)
        /// </summary>
        private string GenerateWithPrefix(string prefix)
        {
            return GenerateStandard(); // Exactly 16 chars
        }

        /// <summary>
        /// Generate random alphanumeric segment
        /// </summary>
        private string GenerateSegment(int length)
        {
            using var rng = RandomNumberGenerator.Create();
            var bytes = new byte[length];
            rng.GetBytes(bytes);
            
            var chars = new char[length];
            for (int i = 0; i < length; i++)
            {
                chars[i] = ALPHANUMERIC[bytes[i] % ALPHANUMERIC.Length];
            }
            
            return new string(chars);
        }

        /// <summary>
        /// Validate license key format
        /// </summary>
        public bool IsValidFormat(string key)
        {
            if (string.IsNullOrEmpty(key)) return false;

            // Check various formats
            var patterns = new[]
            {
                @"^[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$", // Standard
                @"^DSEC-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$", // DSecure
                @"^[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}-[A-Z0-9]{5}$", // Windows
                @"^[a-f0-9]{8}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{4}-[a-f0-9]{12}$", // UUID
                @"^DSEC-(BSC|PRO|ENT|TRL|STD)-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$", // Edition
                @"^[A-Z]{2,10}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}$" // Custom prefix
            };

            return patterns.Any(p => System.Text.RegularExpressions.Regex.IsMatch(key, p));
        }

        /// <summary>
        /// Generate checksum for verification
        /// </summary>
        public string GetChecksum(string key)
        {
            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(key));
            return Convert.ToHexString(hash[..4]); // First 8 hex chars
        }
    }
}
