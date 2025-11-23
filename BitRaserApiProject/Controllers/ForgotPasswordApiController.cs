using BitRaserApiProject.Models.DTOs;
using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;  // ✅ ADD: For ToListAsync()

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Forgot/Reset Password API WITHOUT Email Sending
    /// Returns OTP and reset link directly in API response for testing
    /// </summary>
    [ApiController]
    [Route("api/forgot")]
  public class ForgotPasswordApiController : ControllerBase
    {
        private readonly IForgotPasswordService _service;
private readonly ILogger<ForgotPasswordApiController> _logger;

        public ForgotPasswordApiController(
          IForgotPasswordService service,
 ILogger<ForgotPasswordApiController> logger)
        {
   _service = service;
_logger = logger;
        }

        /// <summary>
  /// Step 1: Request password reset
        /// Returns OTP and reset link in API response (NO EMAIL SENT)
        /// OTP expires in 10 minutes and records auto-delete after expiry
 /// </summary>
  /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/forgot/request
        ///{
      ///    "email": "user@example.com"
     ///     }
        ///     
        /// Sample response:
        /// 
  ///   {
    ///         "success": true,
        ///   "message": "Password reset code generated successfully...",
        ///         "otp": "123456",
///         "resetLink": "http://localhost:5000/reset-password?token=abc123...",
    ///    "resetToken": "abc123...",
        ///       "expiresAt": "2025-01-14T10:35:00Z",
   ///         "expiryMinutes": 10
        ///   }
        /// </remarks>
        [HttpPost("request")]
        [AllowAnonymous]
    [ProducesResponseType(typeof(ForgotPasswordResponseDto), 200)]
        [ProducesResponseType(400)]
   [ProducesResponseType(500)]
     public async Task<ActionResult<ForgotPasswordResponseDto>> RequestPasswordReset(
[FromBody] ForgotPasswordRequestDto dto)
      {
            if (!ModelState.IsValid)
            {
     return BadRequest(ModelState);
   }

 var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
     var userAgent = Request.Headers["User-Agent"].ToString();

     var result = await _service.RequestPasswordResetAsync(dto, ipAddress, userAgent);

            if (!result.Success)
            {
       return BadRequest(result);
        }

   return Ok(result);
        }

        /// <summary>
        /// Step 1.5: Resend OTP
  /// Expires previous OTP and generates new one
        /// Useful when user doesn't receive OTP or OTP expires
        /// </summary>
/// <remarks>
     /// Sample request:
        /// 
   ///     POST /api/forgot/resend-otp
        ///     {
        ///         "email": "user@example.com"
        ///     }
 ///     
 /// Sample response:
/// 
  ///     {
        ///      "success": true,
   ///   "message": "New OTP generated successfully. Previous OTP has been expired.",
        ///         "otp": "789456",
///    "resetLink": "http://localhost:5000/reset-password?token=xyz789...",
        ///     "resetToken": "xyz789...",
    ///   "expiresAt": "2025-01-15T11:45:00Z",
 ///    "expiryMinutes": 10
        ///   }
  /// 
  /// **Note:** This will EXPIRE all previous active OTPs for this email.
      /// </remarks>
        [HttpPost("resend-otp")]
   [AllowAnonymous]
[ProducesResponseType(typeof(ForgotPasswordResponseDto), 200)]
        [ProducesResponseType(400)]
  [ProducesResponseType(500)]
  public async Task<ActionResult<ForgotPasswordResponseDto>> ResendOtp(
   [FromBody] ForgotPasswordRequestDto dto)
  {
      if (!ModelState.IsValid)
     {
   return BadRequest(ModelState);
 }

       var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers["User-Agent"].ToString();

       _logger.LogInformation("🔄 Resend OTP request from IP: {IP} for email: {Email}", 
     ipAddress, dto.Email);

  var result = await _service.ResendOtpAsync(dto, ipAddress, userAgent);

            if (!result.Success)
      {
     return BadRequest(result);
  }

     return Ok(result);
        }

        /// <summary>
        /// Step 2: Validate reset link/token
 /// Check if reset link is still valid
        /// </summary>
        /// <remarks>
  /// Sample request:
        /// 
        ///     POST /api/forgot/validate-reset-link
        ///     {
     ///   "resetToken": "abc123..."
        ///     }
 ///
        /// Sample response:
    /// 
        ///     {
      ///         "isValid": true,
        ///  "message": "Reset link is valid.",
        ///         "email": "user@example.com",
        ///       "expiresAt": "2025-01-14T10:35:00Z",
        ///         "remainingMinutes": 3
     ///  }
        /// </remarks>
        [HttpPost("validate-reset-link")]
        [AllowAnonymous]
[ProducesResponseType(typeof(ValidateResetLinkResponseDto), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(500)]
        public async Task<ActionResult<ValidateResetLinkResponseDto>> ValidateResetLink(
            [FromBody] ValidateResetLinkDto dto)
        {
            if (!ModelState.IsValid)
            {
      return BadRequest(ModelState);
            }

      var result = await _service.ValidateResetLinkAsync(dto);

   if (!result.IsValid)
            {
    return BadRequest(result);
       }

  return Ok(result);
     }

/// <summary>
     /// Step 3: Verify OTP (optional verification step)
        /// Verify OTP before allowing password reset
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/forgot/verify-otp
        ///     {
     /// "email": "user@example.com",
     ///         "otp": "123456"
        ///     }
        ///     
        /// Sample response:
        /// 
  ///     {
        ///         "success": true,
        ///         "isValid": true,
        ///      "message": "OTP verified successfully.",
   ///         "email": "user@example.com"
      ///     }
        /// </remarks>
        [HttpPost("verify-otp")]
        [AllowAnonymous]
      [ProducesResponseType(typeof(VerifyOtpResponseDto), 200)]
      [ProducesResponseType(400)]
    [ProducesResponseType(500)]
        public async Task<ActionResult<VerifyOtpResponseDto>> VerifyOtp(
            [FromBody] VerifyOtpDto dto)
        {
         if (!ModelState.IsValid)
            {
        return BadRequest(ModelState);
 }

     var result = await _service.VerifyOtpAsync(dto);

            if (!result.Success)
            {
        return BadRequest(result);
            }

       return Ok(result);
        }

        /// <summary>
    /// Step 4: Reset password
        /// Reset password using OTP + reset token
        /// </summary>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/forgot/reset
    ///     {
        ///  "email": "user@example.com",
        ///     "otp": "123456",
        ///   "resetToken": "abc123...",
   ///         "newPassword": "NewSecurePassword@123"
        ///     }
        ///     
        /// Sample response:
 /// 
        ///{
        ///         "success": true,
        ///         "message": "Password reset successfully. You can now log in with your new password.",
 ///         "resetAt": "2025-01-14T10:40:00Z"
    ///     }
        /// </remarks>
        [HttpPost("reset")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(ResetPasswordResponseDto), 200)]
        [ProducesResponseType(400)]
  [ProducesResponseType(500)]
      public async Task<ActionResult<ResetPasswordResponseDto>> ResetPassword(
      [FromBody] ResetPasswordDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
   }

   var result = await _service.ResetPasswordAsync(dto);

      if (!result.Success)
     {
             return BadRequest(result);
  }

            return Ok(result);
  }

   /// <summary>
     /// Admin/System: Cleanup expired password reset requests
 /// Run this periodically to clean up old data
        /// </summary>
        [HttpPost("cleanup")]
        [Authorize(Roles = "SuperAdmin,Admin")]
        [ProducesResponseType(200)]
    [ProducesResponseType(401)]
      [ProducesResponseType(403)]
     public async Task<ActionResult> CleanupExpiredRequests()
  {
 await _service.CleanupExpiredRequestsAsync();

          return Ok(new
          {
       success = true,
    message = "Expired password reset requests cleaned up successfully."
  });
  }

  /// <summary>
    /// Testing endpoint - Get all active reset requests (ADMIN ONLY)
        /// For debugging and testing purposes
        /// </summary>
        [HttpGet("admin/active-requests")]
        [Authorize(Roles = "SuperAdmin")]
[ProducesResponseType(200)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<ActionResult> GetActiveRequests([FromServices] ApplicationDbContext context)
        {
            var requests = await context.ForgotPasswordRequests
       .Where(f => !f.IsUsed && f.ExpiresAt > DateTime.UtcNow)
      .OrderByDescending(f => f.CreatedAt)
       .Select(f => new
                {
  f.Id,
         f.Email,
 f.Otp,
               f.ResetToken,
      f.ExpiresAt,
    f.CreatedAt,
           RemainingMinutes = (int)(f.ExpiresAt - DateTime.UtcNow).TotalMinutes
  })
     .ToListAsync();

            return Ok(new
     {
  totalCount = requests.Count,
     requests
      });
        }
    }
}
