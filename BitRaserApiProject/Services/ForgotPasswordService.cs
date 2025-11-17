using System.Security.Cryptography;
using System.Text;
using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;
using BitRaserApiProject.Repositories;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Service for Forgot/Reset Password WITHOUT Email Sending
    /// Returns OTP and reset link directly in API response
    /// </summary>
    public interface IForgotPasswordService
    {
        Task<ForgotPasswordResponseDto> RequestPasswordResetAsync(ForgotPasswordRequestDto dto, string? ipAddress, string? userAgent);
  Task<ValidateResetLinkResponseDto> ValidateResetLinkAsync(ValidateResetLinkDto dto);
        Task<VerifyOtpResponseDto> VerifyOtpAsync(VerifyOtpDto dto);
        Task<ResetPasswordResponseDto> ResetPasswordAsync(ResetPasswordDto dto);
  Task CleanupExpiredRequestsAsync();
    }

  public class ForgotPasswordService : IForgotPasswordService
    {
        private readonly ApplicationDbContext _context;
  private readonly IForgotPasswordRepository _repository;
        private readonly ILogger<ForgotPasswordService> _logger;
    private readonly IConfiguration _configuration;

     public ForgotPasswordService(
ApplicationDbContext context,
      IForgotPasswordRepository repository,
 ILogger<ForgotPasswordService> logger,
    IConfiguration configuration)
        {
   _context = context;
         _repository = repository;
  _logger = logger;
       _configuration = configuration;
        }

   /// <summary>
        /// Request password reset - generates OTP and reset token
        /// Returns them in API response (NO EMAIL SENT)
  /// </summary>
        public async Task<ForgotPasswordResponseDto> RequestPasswordResetAsync(
      ForgotPasswordRequestDto dto,
 string? ipAddress,
        string? userAgent)
 {
       try
   {
       // Step 1: Verify email exists in users or subusers table
    var user = await _context.Users
     .FirstOrDefaultAsync(u => u.user_email == dto.Email);

    var subuser = await _context.subuser
       .FirstOrDefaultAsync(s => s.subuser_email == dto.Email);

     if (user == null && subuser == null)
  {
 _logger.LogWarning("Password reset requested for non-existent email: {Email}", dto.Email);
   
      // ⚠️ Security: Don't reveal if email exists
       // But for testing, we'll be more explicit
       return new ForgotPasswordResponseDto
 {
    Success = false,
  Message = "If this email exists, you will receive a password reset code.",
 Otp = string.Empty,
       ResetLink = string.Empty,
        ResetToken = string.Empty
        };
       }

// Step 2: Rate limiting - max 3 active requests per email
         var activeRequestCount = await _repository.GetActiveRequestCountForEmailAsync(dto.Email);
         if (activeRequestCount >= 3)
    {
             _logger.LogWarning("Too many active password reset requests for {Email}", dto.Email);
 return new ForgotPasswordResponseDto
        {
     Success = false,
       Message = "Too many active reset requests. Please wait for previous requests to expire.",
  Otp = string.Empty,
 ResetLink = string.Empty,
  ResetToken = string.Empty
    };
      }

       // Step 3: Generate 6-digit OTP
      string otp = GenerateOtp();

    // Step 4: Generate unique reset token (GUID + random bytes)
   string resetToken = GenerateResetToken();

     // Step 5: Calculate expiry (10 minutes from now)
  DateTime expiresAt = DateTime.UtcNow.AddMinutes(10);  // ✅ Changed from 5 to 10 minutes

       // Step 6: Create forgot password request
        var request = new ForgotPasswordRequest
  {
        UserId = user?.user_id ?? subuser!.subuser_id,
     Email = dto.Email,
  UserType = user != null ? "user" : "subuser",  // ✅ Set user type
      Otp = otp,
       ResetToken = resetToken,
IsUsed = false,
         ExpiresAt = expiresAt,
      CreatedAt = DateTime.UtcNow,
    IpAddress = ipAddress,
         UserAgent = userAgent
 };

      await _repository.CreateAsync(request);

   // Step 7: Generate reset link
     string baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
      string resetLink = $"{baseUrl}/reset-password?token={resetToken}";

       string userType = user != null ? "User" : "Subuser";
 _logger.LogInformation("✅ Password reset requested for {UserType} {Email}. OTP: {Otp}, Token: {Token}", 
  userType, dto.Email, otp, resetToken);

// ✅ Return OTP and reset link in API response (NO EMAIL SENT)
return new ForgotPasswordResponseDto
    {
     Success = true,
    Message = $"Password reset code generated successfully for {userType}. Use the OTP and reset link below.",
    Otp = otp,  // ✅ Normally sent via email, but returned here for testing
       ResetLink = resetLink,  // ✅ Normally sent via email, but returned here for testing
      ResetToken = resetToken,  // ✅ Can be used directly in API calls
  ExpiresAt = expiresAt,
ExpiryMinutes = 10  // ✅ Changed from 5 to 10 minutes
    };
 }
            catch (Exception ex)
       {
 _logger.LogError(ex, "Error requesting password reset for {Email}", dto.Email);
   return new ForgotPasswordResponseDto
   {
      Success = false,
   Message = "An error occurred while processing your request.",
       Otp = string.Empty,
  ResetLink = string.Empty,
  ResetToken = string.Empty
          };
     }
  }

    /// <summary>
   /// Validate reset link/token
  /// </summary>
        public async Task<ValidateResetLinkResponseDto> ValidateResetLinkAsync(ValidateResetLinkDto dto)
  {
    try
    {
 var request = await _repository.GetByTokenAsync(dto.ResetToken);

       if (request == null)
      {
       _logger.LogWarning("Invalid or expired reset token: {Token}", dto.ResetToken);
       return new ValidateResetLinkResponseDto
  {
  IsValid = false,
    Message = "Invalid or expired reset link."
           };
         }

      var remainingMinutes = (int)(request.ExpiresAt - DateTime.UtcNow).TotalMinutes;

    _logger.LogInformation("✅ Valid reset token for {Email}. Expires in {Minutes} minutes", 
        request.Email, remainingMinutes);

 return new ValidateResetLinkResponseDto
     {
  IsValid = true,
 Message = "Reset link is valid.",
           Email = request.Email,
       ExpiresAt = request.ExpiresAt,
     RemainingMinutes = remainingMinutes
     };
       }
  catch (Exception ex)
      {
 _logger.LogError(ex, "Error validating reset link");
  return new ValidateResetLinkResponseDto
            {
      IsValid = false,
      Message = "An error occurred while validating the reset link."
       };
}
  }

   /// <summary>
/// Verify OTP (optional step before reset)
        /// </summary>
   public async Task<VerifyOtpResponseDto> VerifyOtpAsync(VerifyOtpDto dto)
    {
       try
     {
   var request = await _repository.GetByEmailAndOtpAsync(dto.Email, dto.Otp);

   if (request == null)
      {
      _logger.LogWarning("Invalid OTP for {Email}: {Otp}", dto.Email, dto.Otp);
      return new VerifyOtpResponseDto
   {
         Success = false,
       IsValid = false,
     Message = "Invalid or expired OTP."
      };
   }

         _logger.LogInformation("✅ Valid OTP for {Email}", dto.Email);

    return new VerifyOtpResponseDto
       {
    Success = true,
     IsValid = true,
   Message = "OTP verified successfully.",
   Email = request.Email
       };
 }
  catch (Exception ex)
        {
           _logger.LogError(ex, "Error verifying OTP");
 return new VerifyOtpResponseDto
     {
     Success = false,
   IsValid = false,
       Message = "An error occurred while verifying the OTP."
     };
   }
   }

  /// <summary>
/// Reset password using OTP and reset token
/// </summary>
   public async Task<ResetPasswordResponseDto> ResetPasswordAsync(ResetPasswordDto dto)
 {
   try
        {
       // Step 1: Validate OTP + Token
          var request = await _repository.GetByEmailAndOtpAsync(dto.Email, dto.Otp);

     if (request == null)
    {
       _logger.LogWarning("Invalid OTP/Email combination for password reset: {Email}", dto.Email);
    return new ResetPasswordResponseDto
          {
       Success = false,
         Message = "Invalid email or OTP."
      };
   }

      // Step 2: Verify token matches
    if (request.ResetToken != dto.ResetToken)
 {
_logger.LogWarning("Reset token mismatch for {Email}", dto.Email);
     return new ResetPasswordResponseDto
  {
        Success = false,
  Message = "Invalid reset token."
    };
  }

     // Step 3: Find user or subuser
     var user = await _context.Users
     .FirstOrDefaultAsync(u => u.user_email == dto.Email);

  var subuser = await _context.subuser
    .FirstOrDefaultAsync(s => s.subuser_email == dto.Email);

      if (user == null && subuser == null)
        {
   _logger.LogError("User not found during password reset: {Email}", dto.Email);
     return new ResetPasswordResponseDto
     {
   Success = false,
 Message = "User not found."
           };
  }

    // Step 4: Hash new password using BCrypt
string hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);

// Step 5: Update password
if (user != null)
 {
      user.user_password = dto.NewPassword;  // Plain text (as per your current schema)
            user.hash_password = hashedPassword;  // BCrypt hashed
   user.updated_at = DateTime.UtcNow;
       _context.Entry(user).State = EntityState.Modified;
      }
      else
     {
      subuser!.subuser_password = hashedPassword;  // BCrypt hashed
       subuser.UpdatedAt = DateTime.UtcNow;
_context.Entry(subuser).State = EntityState.Modified;
   }

       await _context.SaveChangesAsync();

       // Step 6: Mark request as used
         request.IsUsed = true;
await _repository.UpdateAsync(request);

        string userType = user != null ? "User" : "Subuser";
     _logger.LogInformation("✅ Password reset successful for {UserType} {Email}", userType, dto.Email);

       return new ResetPasswordResponseDto
    {
   Success = true,
 Message = $"Password reset successfully for {userType}. You can now log in with your new password.",
 ResetAt = DateTime.UtcNow
      };
            }
catch (Exception ex)
   {
_logger.LogError(ex, "Error resetting password for {Email}", dto.Email);
    return new ResetPasswordResponseDto
     {
Success = false,
   Message = "An error occurred while resetting your password."
   };
   }
 }

        /// <summary>
/// Cleanup expired requests (should be run periodically)
/// </summary>
   public async Task CleanupExpiredRequestsAsync()
        {
            try
   {
  await _repository.DeleteExpiredRequestsAsync();
_logger.LogInformation("✅ Cleaned up expired password reset requests");
    }
       catch (Exception ex)
         {
     _logger.LogError(ex, "Error cleaning up expired requests");
      }
}

    #region Private Helper Methods

        /// <summary>
  /// Generate 6-digit OTP
/// </summary>
      private string GenerateOtp()
      {
  var random = new Random();
return random.Next(100000, 999999).ToString();
     }

        /// <summary>
   /// Generate unique reset token (GUID + random bytes)
 /// </summary>
private string GenerateResetToken()
   {
        var guid = Guid.NewGuid().ToString("N");  // Remove hyphens
            var randomBytes = RandomNumberGenerator.GetBytes(16);
     var randomString = Convert.ToBase64String(randomBytes)
          .Replace("+", "")
         .Replace("/", "")
.Replace("=", "")
    .Substring(0, 16);

     return $"{guid}{randomString}";
        }

        #endregion
    }
}
