using System.ComponentModel.DataAnnotations;

namespace DSecureApi.Models.DTOs
{
    /// <summary>
    /// DTOs for Forgot/Reset Password WITHOUT Email Sending
    /// </summary>

    #region Request DTOs

    /// <summary>
    /// Request OTP for password reset
    /// </summary>
    public class ForgotPasswordRequestDto
    {
 [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
     public string Email { get; set; } = string.Empty;
    }

    /// <summary>
    /// Verify OTP
    /// </summary>
    public class VerifyOtpDto
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "OTP is required")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "OTP must be 6 numeric digits")]
     public string Otp { get; set; } = string.Empty;
}

    /// <summary>
    /// Validate reset link/token
    /// </summary>
    public class ValidateResetLinkDto
    {
        [Required(ErrorMessage = "Reset token is required")]
  public string ResetToken { get; set; } = string.Empty;
    }

    /// <summary>
    /// Reset password with OTP and token
    /// </summary>
    public class ResetPasswordDto
    {
        [Required(ErrorMessage = "Email is required")]
  [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; } = string.Empty;

   [Required(ErrorMessage = "OTP is required")]
  [StringLength(6, MinimumLength = 6, ErrorMessage = "OTP must be 6 digits")]
  public string Otp { get; set; } = string.Empty;

   [Required(ErrorMessage = "Reset token is required")]
        public string ResetToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "New password is required")]
        [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
        public string NewPassword { get; set; } = string.Empty;
 }

    #endregion

    #region Response DTOs

    /// <summary>
  /// Response after requesting password reset
    /// Returns OTP and reset link (NO EMAIL SENT)
    /// </summary>
    public class ForgotPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        
        /// <summary>
        /// 6-digit OTP (normally sent via email, but returned here for testing)
        /// </summary>
        public string Otp { get; set; } = string.Empty;
        
        /// <summary>
        /// Reset link with token (normally sent via email, but returned here for testing)
        /// </summary>
        public string ResetLink { get; set; } = string.Empty;
        
        /// <summary>
    /// Reset token (can be used directly in API calls)
        /// </summary>
     public string ResetToken { get; set; } = string.Empty;
        
  public DateTime ExpiresAt { get; set; }
        public int ExpiryMinutes { get; set; } = 10;  // ✅ Changed default from 5 to 10 minutes
    }

    /// <summary>
    /// Response after validating reset link
    /// </summary>
    public class ValidateResetLinkResponseDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? Email { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public int? RemainingMinutes { get; set; }
    }

    /// <summary>
    /// Response after verifying OTP
    /// </summary>
    public class VerifyOtpResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsValid { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// Response after resetting password
    /// </summary>
    public class ResetPasswordResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime ResetAt { get; set; }
    }

    #endregion
}
