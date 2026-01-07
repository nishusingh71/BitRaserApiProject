namespace BitRaserApiProject.Services.License
{
    /// <summary>
    /// License Validation Hook - Future-Ready Placeholder
    /// This interface provides a clean integration point for product-based license validation.
    /// Current implementation passes through (no validation).
    /// Future: Connect to product catalog, validate license types, check feature access.
    /// </summary>
    public interface ILicenseValidationHook
    {
        /// <summary>
        /// Validate if a license key is valid for a specific product
        /// </summary>
        /// <param name="licenseKey">The license key to validate</param>
        /// <param name="productId">Optional product ID for product-specific validation</param>
        /// <returns>Validation result with details</returns>
        Task<LicenseValidationResult> ValidateLicenseAsync(string licenseKey, int? productId = null);

        /// <summary>
        /// Check if a user has access to a specific feature
        /// </summary>
        /// <param name="userEmail">User's email</param>
        /// <param name="featureName">Feature to check access for</param>
        /// <returns>True if user has access</returns>
        Task<bool> HasFeatureAccessAsync(string userEmail, string featureName);

        /// <summary>
        /// Get license details for a user
        /// </summary>
        /// <param name="userEmail">User's email</param>
        /// <returns>License details or null if no license</returns>
        Task<LicenseDetails?> GetLicenseDetailsAsync(string userEmail);
    }

    /// <summary>
    /// Result of license validation
    /// </summary>
    public class LicenseValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? LicenseType { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int? MaxDevices { get; set; }
        public int? UsedDevices { get; set; }
        public List<string> AllowedFeatures { get; set; } = new();

        public static LicenseValidationResult Valid(string message = "License is valid") 
            => new() { IsValid = true, Message = message };
        
        public static LicenseValidationResult Invalid(string message) 
            => new() { IsValid = false, Message = message };
    }

    /// <summary>
    /// License details for a user
    /// </summary>
    public class LicenseDetails
    {
        public string LicenseKey { get; set; } = string.Empty;
        public string LicenseType { get; set; } = "Standard";
        public DateTime? PurchaseDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public int MaxSubusers { get; set; }
        public int UsedSubusers { get; set; }
        public int MaxDevices { get; set; }
        public int UsedDevices { get; set; }
        public List<string> Features { get; set; } = new();
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// Default implementation - passes through without validation
    /// Future: Replace with product-based validation logic
    /// </summary>
    public class DefaultLicenseValidationHook : ILicenseValidationHook
    {
        private readonly ILogger<DefaultLicenseValidationHook> _logger;

        public DefaultLicenseValidationHook(ILogger<DefaultLicenseValidationHook> logger)
        {
            _logger = logger;
        }

        public Task<LicenseValidationResult> ValidateLicenseAsync(string licenseKey, int? productId = null)
        {
            // FUTURE: Implement product-specific license validation
            // For now, pass through as valid if license key exists
            
            if (string.IsNullOrEmpty(licenseKey))
            {
                return Task.FromResult(LicenseValidationResult.Invalid("No license key provided"));
            }

            _logger.LogDebug("📋 License validation hook: {LicenseKey} (product: {ProductId}) - Passing through", 
                licenseKey.Substring(0, Math.Min(8, licenseKey.Length)) + "...", productId);
            
            return Task.FromResult(LicenseValidationResult.Valid("License validated (default hook)"));
        }

        public Task<bool> HasFeatureAccessAsync(string userEmail, string featureName)
        {
            // FUTURE: Check user's license tier for feature access
            // For now, allow all features
            
            _logger.LogDebug("📋 Feature access check: {Email} -> {Feature} - Allowing by default", 
                userEmail, featureName);
            
            return Task.FromResult(true);
        }

        public Task<LicenseDetails?> GetLicenseDetailsAsync(string userEmail)
        {
            // FUTURE: Fetch actual license details from database
            // For now, return null (use system fallback)
            
            _logger.LogDebug("📋 License details request for: {Email} - Using system fallback", userEmail);
            
            return Task.FromResult<LicenseDetails?>(null);
        }
    }
}
