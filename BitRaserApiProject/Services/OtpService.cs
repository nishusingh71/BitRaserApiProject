using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Hybrid OTP service - Uses both in-memory cache AND database storage
    /// This ensures OTPs survive server restarts on Render.com
    /// OTP expiry time: 10 minutes
    /// </summary>
    public interface IOtpService
    {
        string GenerateOtp(string email);
        bool ValidateOtp(string email, string otp);
        void RemoveOtp(string email);
        bool IsOtpExpired(string email);
        Task<string> GenerateOtpAsync(string email);
        Task<bool> ValidateOtpAsync(string email, string otp);
        Task RemoveOtpAsync(string email);
        Task<bool> IsOtpExpiredAsync(string email);
    }

    public class OtpService : IOtpService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OtpService> _logger;

        // In-memory cache for fast access (backup for database)
        private readonly ConcurrentDictionary<string, OtpData> _otpCache = new();
        private readonly int _otpExpiryMinutes = 10;

        private class OtpData
        {
            public string Otp { get; set; } = string.Empty;
            public DateTime ExpiryTime { get; set; }
            public int FailedAttempts { get; set; } = 0;
        }

        public OtpService(IServiceProvider serviceProvider, ILogger<OtpService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        #region Synchronous Methods (use cache + db fallback)

        /// <summary>
        /// Generate 6-digit OTP for email (sync version - uses cache)
        /// </summary>
        public string GenerateOtp(string email)
        {
            var emailKey = email.ToLower().Trim();

            // Remove old OTP from cache
            _otpCache.TryRemove(emailKey, out _);

            // Generate random 6-digit OTP
            var random = new Random();
            string otp = random.Next(100000, 999999).ToString();

            // Store in cache
            var otpData = new OtpData
            {
                Otp = otp,
                ExpiryTime = DateTime.UtcNow.AddMinutes(_otpExpiryMinutes),
                FailedAttempts = 0
            };
            _otpCache.TryAdd(emailKey, otpData);

            // Also store in database (async, fire-and-forget with error logging for observability)
            _ = StoreOtpInDatabaseAsync(emailKey, otp).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogError(t.Exception, "❌ Background OTP storage failed for {Email}", emailKey);
            }, TaskContinuationOptions.OnlyOnFaulted);

            _logger.LogInformation("📧 OTP generated for {Email}: {Otp} (expires in {Minutes} min)",
                emailKey, otp, _otpExpiryMinutes);

            return otp;
        }

        /// <summary>
        /// Validate OTP for email (sync version)
        /// </summary>
        public bool ValidateOtp(string email, string otp)
        {
            var emailKey = email.ToLower().Trim();

            // First check cache
            if (_otpCache.TryGetValue(emailKey, out var otpData))
            {
                if (DateTime.UtcNow > otpData.ExpiryTime)
                {
                    _otpCache.TryRemove(emailKey, out _);
                    _logger.LogWarning("⏰ OTP expired (cache) for {Email}", emailKey);
                    return false;
                }

                if (otpData.FailedAttempts >= 5)
                {
                    _otpCache.TryRemove(emailKey, out _);
                    _logger.LogWarning("🚫 Too many failed attempts for {Email}", emailKey);
                    return false;
                }

                if (otpData.Otp == otp)
                {
                    _logger.LogInformation("✅ OTP validated (cache) for {Email}", emailKey);
                    return true;
                }

                otpData.FailedAttempts++;
                _logger.LogWarning("❌ Invalid OTP attempt #{Attempts} for {Email}",
                    otpData.FailedAttempts, emailKey);
                return false;
            }

            // Cache miss - try database (sync call for compatibility)
            _logger.LogInformation("🔍 OTP not in cache, checking database for {Email}", emailKey);
            return ValidateOtpFromDatabaseSync(emailKey, otp);
        }

        /// <summary>
        /// Remove OTP after successful use
        /// </summary>
        public void RemoveOtp(string email)
        {
            var emailKey = email.ToLower().Trim();
            _otpCache.TryRemove(emailKey, out _);

            // Also remove from database (with error logging)
            _ = RemoveOtpFromDatabaseAsync(emailKey).ContinueWith(t =>
            {
                if (t.IsFaulted)
                    _logger.LogError(t.Exception, "❌ Background OTP removal failed for {Email}", emailKey);
            }, TaskContinuationOptions.OnlyOnFaulted);

            _logger.LogInformation("🗑️ OTP removed for {Email}", emailKey);
        }

        /// <summary>
        /// Check if OTP is expired
        /// </summary>
        public bool IsOtpExpired(string email)
        {
            var emailKey = email.ToLower().Trim();

            // Check cache first
            if (_otpCache.TryGetValue(emailKey, out var otpData))
            {
                return DateTime.UtcNow > otpData.ExpiryTime;
            }

            // Check database
            return IsOtpExpiredInDatabaseSync(emailKey);
        }

        #endregion

        #region Async Methods (recommended for production)

        /// <summary>
        /// Generate OTP (async version - stores in both cache and database)
        /// </summary>
        public async Task<string> GenerateOtpAsync(string email)
        {
            var emailKey = email.ToLower().Trim();

            _otpCache.TryRemove(emailKey, out _);

            var random = new Random();
            string otp = random.Next(100000, 999999).ToString();

            var otpData = new OtpData
            {
                Otp = otp,
                ExpiryTime = DateTime.UtcNow.AddMinutes(_otpExpiryMinutes),
                FailedAttempts = 0
            };
            _otpCache.TryAdd(emailKey, otpData);

            // Store in database
            await StoreOtpInDatabaseAsync(emailKey, otp);

            _logger.LogInformation("📧 OTP generated (async) for {Email}: {Otp}", emailKey, otp);
            return otp;
        }

        /// <summary>
        /// Validate OTP (async version - checks cache then database)
        /// </summary>
        public async Task<bool> ValidateOtpAsync(string email, string otp)
        {
            var emailKey = email.ToLower().Trim();

            // Check cache first
            if (_otpCache.TryGetValue(emailKey, out var otpData))
            {
                if (DateTime.UtcNow > otpData.ExpiryTime)
                {
                    _otpCache.TryRemove(emailKey, out _);
                    return false;
                }

                if (otpData.FailedAttempts >= 5)
                {
                    _otpCache.TryRemove(emailKey, out _);
                    return false;
                }

                if (otpData.Otp == otp)
                {
                    return true;
                }

                otpData.FailedAttempts++;
                return false;
            }

            // Check database
            return await ValidateOtpFromDatabaseAsync(emailKey, otp);
        }

        public async Task RemoveOtpAsync(string email)
        {
            var emailKey = email.ToLower().Trim();
            _otpCache.TryRemove(emailKey, out _);
            await RemoveOtpFromDatabaseAsync(emailKey);
        }

        public async Task<bool> IsOtpExpiredAsync(string email)
        {
            var emailKey = email.ToLower().Trim();

            if (_otpCache.TryGetValue(emailKey, out var otpData))
            {
                return DateTime.UtcNow > otpData.ExpiryTime;
            }

            return await IsOtpExpiredInDatabaseAsync(emailKey);
        }

        #endregion

        #region Database Operations

        private async Task StoreOtpInDatabaseAsync(string email, string otp)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                // Remove old OTPs for this email
                var oldOtps = await context.ForgotPasswordRequests
                                .Where(f => f.Email.ToLower() == email && !f.IsUsed)
                    .ToListAsync();

                if (oldOtps.Any())
                {
                    context.ForgotPasswordRequests.RemoveRange(oldOtps);
                }

                // Create new OTP record with HASHED OTP for security
                var otpRecord = new BitRaserApiProject.Models.ForgotPasswordRequest
                {
                    Email = email,
                    Otp = HashOtp(otp), // ✅ SECURITY FIX: Hash OTP before storing
                    ResetToken = Guid.NewGuid().ToString("N"),
                    ExpiresAt = DateTime.UtcNow.AddMinutes(_otpExpiryMinutes),
                    CreatedAt = DateTime.UtcNow,
                    IsUsed = false,
                    UserType = "unknown"
                };

                context.ForgotPasswordRequests.Add(otpRecord);
                await context.SaveChangesAsync();

                _logger.LogInformation("💾 OTP stored in database (hashed) for {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Failed to store OTP in database for {Email}", email);
            }
        }

        private async Task<bool> ValidateOtpFromDatabaseAsync(string email, string otp)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // ✅ SECURITY: Compare with hashed OTP (with backward compatibility)
                var hashedOtp = HashOtp(otp);

                var otpRecord = await context.ForgotPasswordRequests
                       .Where(f => f.Email.ToLower() == email &&
                    (f.Otp == hashedOtp || f.Otp == otp) && // Check hashed OR plain for backward compat
                    !f.IsUsed &&
                      f.ExpiresAt > DateTime.UtcNow)
                  .FirstOrDefaultAsync();

                if (otpRecord != null)
                {
                    // Also add to cache for faster subsequent validations
                    _otpCache.TryAdd(email, new OtpData
                    {
                        Otp = otp,
                        ExpiryTime = otpRecord.ExpiresAt,
                        FailedAttempts = 0
                    });

                    _logger.LogInformation("✅ OTP validated from database for {Email}", email);
                    return true;
                }

                _logger.LogWarning("❌ OTP not found in database for {Email}", email);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validating OTP from database for {Email}", email);
                return false;
            }
        }

        private bool ValidateOtpFromDatabaseSync(string email, string otp)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                
                // ✅ SECURITY: Compare with hashed OTP (with backward compatibility)
                var hashedOtp = HashOtp(otp);

                var otpRecord = context.ForgotPasswordRequests
.Where(f => f.Email.ToLower() == email &&
      (f.Otp == hashedOtp || f.Otp == otp) && // Check hashed OR plain for backward compat
              !f.IsUsed &&
 f.ExpiresAt > DateTime.UtcNow)
         .FirstOrDefault();

                if (otpRecord != null)
                {
                    _otpCache.TryAdd(email, new OtpData
                    {
                        Otp = otp,
                        ExpiryTime = otpRecord.ExpiresAt,
                        FailedAttempts = 0
                    });

                    _logger.LogInformation("✅ OTP validated from database (sync) for {Email}", email);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error validating OTP from database (sync) for {Email}", email);
                return false;
            }
        }

        private async Task RemoveOtpFromDatabaseAsync(string email)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var otpRecords = await context.ForgotPasswordRequests
              .Where(f => f.Email.ToLower() == email && !f.IsUsed)
   .ToListAsync();

                if (otpRecords.Any())
                {
                    foreach (var record in otpRecords)
                    {
                        record.IsUsed = true;
                    }
                    await context.SaveChangesAsync();
                }

                _logger.LogInformation("🗑️ OTP marked as used in database for {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error removing OTP from database for {Email}", email);
            }
        }

        private async Task<bool> IsOtpExpiredInDatabaseAsync(string email)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var hasValidOtp = await context.ForgotPasswordRequests
                     .AnyAsync(f => f.Email.ToLower() == email &&
        !f.IsUsed &&
        f.ExpiresAt > DateTime.UtcNow);

                return !hasValidOtp;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error checking OTP expiry in database for {Email}", email);
                return true;
            }
        }

        private bool IsOtpExpiredInDatabaseSync(string email)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var hasValidOtp = context.ForgotPasswordRequests
     .Any(f => f.Email.ToLower() == email &&
    !f.IsUsed &&
                 f.ExpiresAt > DateTime.UtcNow);

                return !hasValidOtp;
            }
            catch
            {
                return true;
            }
        }

        #endregion

        /// <summary>
        /// Cleanup expired OTPs from cache
        /// </summary>
        public void CleanupExpiredOtps()
        {
            var expiredKeys = _otpCache
       .Where(x => DateTime.UtcNow > x.Value.ExpiryTime)
        .Select(x => x.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _otpCache.TryRemove(key, out _);
            }

            _logger.LogInformation("🧹 Cleaned up {Count} expired OTPs from cache", expiredKeys.Count);
        }

        #region OTP Hashing Helpers

        /// <summary>
        /// Hash OTP using SHA256 for secure storage
        /// </summary>
        private static string HashOtp(string otp)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(otp));
            return Convert.ToBase64String(bytes);
        }

        #endregion
    }
}
