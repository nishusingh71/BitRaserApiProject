using System.Text;
using System.Text.RegularExpressions;

namespace BitRaserApiProject.Utilities
{
    /// <summary>
    /// Utility class for encoding/decoding emails in URLs using Base64
    /// ✅ SECURITY: Prevents email exposure in URLs, logs, and analytics
    /// </summary>
    public static class Base64EmailEncoder
    {
        /// <summary>
        /// Encode email to Base64 for URL usage
        /// </summary>
        /// <param name="email">Plain email address</param>
        /// <returns>Base64-encoded email (URL-safe)</returns>
        public static string Encode(string email)
        {
            if (string.IsNullOrEmpty(email))
                throw new ArgumentNullException(nameof(email));

            var bytes = Encoding.UTF8.GetBytes(email);
            return Convert.ToBase64String(bytes)
                .Replace("+", "-")  // URL-safe
                .Replace("/", "_")  // URL-safe
                .TrimEnd('=');      // Remove padding
        }

        /// <summary>
        /// Decode Base64-encoded email from URL
        /// </summary>
        /// <param name="encodedEmail">Base64-encoded email</param>
        /// <returns>Plain email address</returns>
        /// <exception cref="FormatException">If Base64 string is invalid</exception>
        public static string Decode(string encodedEmail)
        {
            if (string.IsNullOrEmpty(encodedEmail))
                throw new ArgumentNullException(nameof(encodedEmail));

            try
            {
                // Restore URL-safe characters
                var base64 = encodedEmail
                    .Replace("-", "+")
                    .Replace("_", "/");

                // Restore padding
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                var bytes = Convert.FromBase64String(base64);
                return Encoding.UTF8.GetString(bytes);
            }
            catch (FormatException)
            {
                throw new FormatException("Invalid Base64-encoded email");
            }
        }

        /// <summary>
        /// Try to decode Base64 email (returns false if invalid)
        /// </summary>
        public static bool TryDecode(string encodedEmail, out string decodedEmail)
        {
            decodedEmail = string.Empty;
            
            if (string.IsNullOrEmpty(encodedEmail))
                return false;

            try
            {
                decodedEmail = Decode(encodedEmail);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Check if string is a valid email format
        /// </summary>
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            var emailRegex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase);
            return emailRegex.IsMatch(email);
        }

        /// <summary>
        /// Decode Base64 email param or pass through if already plain email
        /// ✅ USE THIS: Always call at start of controller actions with email parameters
        /// </summary>
        /// <param name="emailParam">Raw email parameter (may be Base64 or plain)</param>
        /// <returns>Decoded plain email, lowercase and trimmed</returns>
        public static string DecodeEmailParam(string emailParam)
        {
            if (string.IsNullOrWhiteSpace(emailParam))
                return emailParam;

            // Already a plain email? Pass through
            if (IsValidEmail(emailParam))
                return emailParam.Trim().ToLower();

            // Try to decode as Base64
            try
            {
                // Fix missing Base64 padding
                var base64 = emailParam
                    .Replace("-", "+")
                    .Replace("_", "/");
                    
                switch (base64.Length % 4)
                {
                    case 2: base64 += "=="; break;
                    case 3: base64 += "="; break;
                }

                var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(base64));
                return decoded.Trim().ToLower();
            }
            catch
            {
                // If decode fails, return as-is
                return emailParam.Trim().ToLower();
            }
        }

        /// <summary>
        /// Mask email for logging (e.g., j***@g***.com)
        /// </summary>
        public static string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email) || !email.Contains('@'))
                return email;

            var parts = email.Split('@');
            var username = parts[0];
            var domain = parts[1];

            var maskedUsername = username.Length > 1 
                ? username[0] + "***" 
                : username;

            var domainParts = domain.Split('.');
            var maskedDomain = domainParts.Length > 1
                ? domainParts[0][0] + "***." + string.Join(".", domainParts.Skip(1))
                : domain;

            return $"{maskedUsername}@{maskedDomain}";
        }
    }
}
