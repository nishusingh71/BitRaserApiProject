using System.Globalization;

namespace DSecureApi.Helpers
{
    /// <summary>
    /// Helper class for standardized DateTime operations across the application
    /// All DateTime values should use UTC and be formatted as ISO 8601
    /// Format: 2025-11-24T05:07:11.3895396Z
  /// </summary>
    public static class DateTimeHelper
    {
        /// <summary>
        /// Get current server time in UTC
  /// Returns DateTime in UTC timezone
        /// </summary>
    /// <returns>Current UTC DateTime</returns>
    public static DateTime GetUtcNow()
        {
         return DateTime.UtcNow;
        }

   /// <summary>
        /// Format DateTime to ISO 8601 string with Z suffix
        /// Format: 2025-11-24T05:07:11.3895396Z
        /// </summary>
        /// <param name="dateTime">DateTime to format</param>
        /// <returns>ISO 8601 formatted string</returns>
        public static string ToIso8601String(DateTime dateTime)
 {
// Ensure DateTime is in UTC
       var utcDateTime = dateTime.Kind == DateTimeKind.Utc 
     ? dateTime 
     : dateTime.ToUniversalTime();

            // Format with 7 decimal places for fractional seconds + Z suffix
return utcDateTime.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ", CultureInfo.InvariantCulture);
    }

   /// <summary>
/// Format nullable DateTime to ISO 8601 string
        /// Returns null if DateTime is null
        /// </summary>
     /// <param name="dateTime">Nullable DateTime</param>
        /// <returns>ISO 8601 formatted string or null</returns>
        public static string? ToIso8601String(DateTime? dateTime)
        {
 if (!dateTime.HasValue)
      return null;

            return ToIso8601String(dateTime.Value);
        }

/// <summary>
        /// Parse ISO 8601 string to DateTime in UTC
        /// </summary>
    /// <param name="iso8601String">ISO 8601 formatted string</param>
     /// <returns>DateTime in UTC</returns>
        public static DateTime ParseIso8601(string iso8601String)
        {
      var dateTime = DateTime.Parse(iso8601String, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            
      // Ensure result is in UTC
     return dateTime.Kind == DateTimeKind.Utc 
         ? dateTime 
       : dateTime.ToUniversalTime();
        }

        /// <summary>
        /// Try parse ISO 8601 string to DateTime in UTC
        /// </summary>
        /// <param name="iso8601String">ISO 8601 formatted string</param>
 /// <param name="result">Parsed DateTime if successful</param>
        /// <returns>True if parsing successful</returns>
        public static bool TryParseIso8601(string iso8601String, out DateTime result)
  {
       if (DateTime.TryParse(iso8601String, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var dateTime))
          {
     result = dateTime.Kind == DateTimeKind.Utc 
       ? dateTime 
           : dateTime.ToUniversalTime();
   return true;
            }

        result = default;
            return false;
 }

        /// <summary>
 /// Add minutes to current UTC time
        /// </summary>
    /// <param name="minutes">Minutes to add</param>
        /// <returns>Future DateTime in UTC</returns>
        public static DateTime AddMinutesFromNow(int minutes)
        {
      return DateTime.UtcNow.AddMinutes(minutes);
        }

      /// <summary>
        /// Add hours to current UTC time
        /// </summary>
        /// <param name="hours">Hours to add</param>
        /// <returns>Future DateTime in UTC</returns>
  public static DateTime AddHoursFromNow(int hours)
   {
       return DateTime.UtcNow.AddHours(hours);
        }

        /// <summary>
        /// Add days to current UTC time
    /// </summary>
        /// <param name="days">Days to add</param>
        /// <returns>Future DateTime in UTC</returns>
        public static DateTime AddDaysFromNow(int days)
        {
            return DateTime.UtcNow.AddDays(days);
        }

        /// <summary>
        /// Check if DateTime has expired (is in the past)
      /// </summary>
   /// <param name="dateTime">DateTime to check</param>
        /// <returns>True if expired</returns>
        public static bool IsExpired(DateTime dateTime)
   {
  return dateTime < DateTime.UtcNow;
     }

        /// <summary>
        /// Check if nullable DateTime has expired
        /// Returns true if null or expired
        /// </summary>
        /// <param name="dateTime">Nullable DateTime to check</param>
        /// <returns>True if null or expired</returns>
        public static bool IsExpired(DateTime? dateTime)
      {
     if (!dateTime.HasValue)
     return true;

            return IsExpired(dateTime.Value);
        }

        /// <summary>
        /// Get remaining minutes until expiry
        /// Returns 0 if already expired
        /// </summary>
        /// <param name="expiryDateTime">Expiry DateTime</param>
        /// <returns>Remaining minutes or 0 if expired</returns>
        public static int GetRemainingMinutes(DateTime expiryDateTime)
     {
            var remaining = (expiryDateTime - DateTime.UtcNow).TotalMinutes;
   return remaining > 0 ? (int)remaining : 0;
        }

        /// <summary>
        /// Get remaining seconds until expiry
        /// Returns 0 if already expired
        /// </summary>
        /// <param name="expiryDateTime">Expiry DateTime</param>
        /// <returns>Remaining seconds or 0 if expired</returns>
        public static int GetRemainingSeconds(DateTime expiryDateTime)
        {
   var remaining = (expiryDateTime - DateTime.UtcNow).TotalSeconds;
    return remaining > 0 ? (int)remaining : 0;
        }

        /// <summary>
        /// Convert local DateTime to UTC
        /// </summary>
        /// <param name="localDateTime">Local DateTime</param>
   /// <returns>UTC DateTime</returns>
        public static DateTime ToUtc(DateTime localDateTime)
        {
            return localDateTime.Kind == DateTimeKind.Utc 
     ? localDateTime 
     : localDateTime.ToUniversalTime();
        }

        /// <summary>
        /// Create a DateTime response object with ISO 8601 formatted strings
        /// Use this for consistent API responses
        /// </summary>
      /// <param name="dateTime">DateTime to format</param>
  /// <returns>Anonymous object with formatted time</returns>
        public static object CreateTimeResponse(DateTime dateTime)
      {
            return new
          {
  utc = ToIso8601String(dateTime),
    timestamp = ((DateTimeOffset)dateTime).ToUnixTimeSeconds(),
  formatted = dateTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)
            };
  }
    }
}
