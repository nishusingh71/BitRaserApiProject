using System.Collections.Concurrent;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// In-memory OTP service - OTP database mein store nahi hoga
    /// OTP expiry time ke saath validate hoga
    /// </summary>
    public interface IOtpService
    {
        string GenerateOtp(string email);
        bool ValidateOtp(string email, string otp);
        void RemoveOtp(string email);
        bool IsOtpExpired(string email);
    }

    public class OtpService : IOtpService
 {
        // In-memory storage for OTPs with expiry time
        private readonly ConcurrentDictionary<string, OtpData> _otpStorage = new();
        private readonly int _otpExpiryMinutes = 10; // OTP 10 minutes ke liye valid

        private class OtpData
        {
        public string Otp { get; set; } = string.Empty;
    public DateTime ExpiryTime { get; set; }
    public int FailedAttempts { get; set; } = 0;
        }

        /// <summary>
        /// Generate 6-digit OTP for email
      /// </summary>
        public string GenerateOtp(string email)
        {
     // Remove old OTP if exists
            _otpStorage.TryRemove(email.ToLower(), out _);

            // Generate random 6-digit OTP
       var random = new Random();
   string otp = random.Next(100000, 999999).ToString();

        // Store with expiry time
         var otpData = new OtpData
            {
 Otp = otp,
     ExpiryTime = DateTime.UtcNow.AddMinutes(_otpExpiryMinutes),
            FailedAttempts = 0
            };

   _otpStorage.TryAdd(email.ToLower(), otpData);

            return otp;
        }

        /// <summary>
        /// Validate OTP for email
        /// </summary>
        public bool ValidateOtp(string email, string otp)
   {
            var emailKey = email.ToLower();

        if (!_otpStorage.TryGetValue(emailKey, out var otpData))
  {
       return false; // OTP not found
}

            // Check if OTP expired
            if (DateTime.UtcNow > otpData.ExpiryTime)
            {
  _otpStorage.TryRemove(emailKey, out _);
   return false; // OTP expired
     }

  // Check if too many failed attempts (max 5)
  if (otpData.FailedAttempts >= 5)
            {
         _otpStorage.TryRemove(emailKey, out _);
         return false; // Too many attempts
            }

  // Validate OTP
            if (otpData.Otp == otp)
         {
        return true; // Valid OTP
          }

            // Increment failed attempts
        otpData.FailedAttempts++;
          return false; // Invalid OTP
        }

        /// <summary>
   /// Remove OTP after successful validation
    /// </summary>
     public void RemoveOtp(string email)
  {
_otpStorage.TryRemove(email.ToLower(), out _);
    }

   /// <summary>
        /// Check if OTP is expired
  /// </summary>
        public bool IsOtpExpired(string email)
  {
      var emailKey = email.ToLower();

          if (!_otpStorage.TryGetValue(emailKey, out var otpData))
            {
      return true; // No OTP found = expired
            }

            return DateTime.UtcNow > otpData.ExpiryTime;
        }

        // Cleanup expired OTPs periodically (background task)
        public void CleanupExpiredOtps()
        {
            var expiredKeys = _otpStorage
                .Where(x => DateTime.UtcNow > x.Value.ExpiryTime)
      .Select(x => x.Key)
   .ToList();

          foreach (var key in expiredKeys)
        {
       _otpStorage.TryRemove(key, out _);
    }
        }
    }
}
