using System.Security.Cryptography;
using System.Text;
using BitRaserApiProject.Models;
using BitRaserApiProject.Models.DTOs;
using BitRaserApiProject.Repositories;
using BitRaserApiProject.Helpers;  // ✅ ADD: For DateTimeHelper
using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;  // ✅ ADD: For MySQL connection

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Service for Forgot/Reset Password WITHOUT Email Sending
    /// Returns OTP and reset link directly in API response
    /// </summary>
    public interface IForgotPasswordService
    {
        Task<ForgotPasswordResponseDto> RequestPasswordResetAsync(ForgotPasswordRequestDto dto, string? ipAddress, string? userAgent);
        Task<ForgotPasswordResponseDto> ResendOtpAsync(ForgotPasswordRequestDto dto, string? ipAddress, string? userAgent);
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
        private readonly ITenantConnectionService _tenantService;  // ✅ ADD: For Private Cloud DB access

        public ForgotPasswordService(
            ApplicationDbContext context,
            IForgotPasswordRepository repository,
            ILogger<ForgotPasswordService> logger,
            IConfiguration configuration,
            ITenantConnectionService tenantService)  // ✅ ADD: Inject tenant service
        {
            _context = context;
            _repository = repository;
            _logger = logger;
            _configuration = configuration;
            _tenantService = tenantService;  // ✅ ADD: Initialize tenant service
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
                // ✅ Step 1: Check Main DB first
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.user_email == dto.Email);

                var subuser = await _context.subuser
                    .FirstOrDefaultAsync(s => s.subuser_email == dto.Email);

                bool isPrivateCloudUser = false;
                string? parentUserEmail = null;

                // ✅ Step 2: If not found in Main DB, check Private Cloud databases
                if (user == null && subuser == null)
                {
                    _logger.LogInformation("🔍 User not in Main DB, checking Private Cloud databases for {Email}", dto.Email);

                    // Get all Private Cloud users
                    var privateCloudUsers = await _context.Users
                        .Where(u => u.is_private_cloud == true)
                        .Select(u => new { u.user_email, u.user_id })
                        .ToListAsync();

                    foreach (var pcUser in privateCloudUsers)
                    {
                        try
                        {
                            var connectionString = await _tenantService.GetConnectionStringForUserAsync(pcUser.user_email);
                            var mainConnectionString = _configuration.GetConnectionString("ApplicationDbContextConnection");

                            if (connectionString == mainConnectionString)
                                continue;

                            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                            using var privateContext = new ApplicationDbContext(optionsBuilder.Options);
                            privateContext.Database.SetCommandTimeout(10);

                            // Check for user
                            user = await privateContext.Users
                                .FirstOrDefaultAsync(u => u.user_email == dto.Email);

                            if (user == null)
                            {
                                // Check for subuser
                                subuser = await privateContext.subuser
                                    .FirstOrDefaultAsync(s => s.subuser_email == dto.Email);

                                if (subuser != null)
                                {
                                    isPrivateCloudUser = true;
                                    parentUserEmail = pcUser.user_email;
                                    _logger.LogInformation("✅ Found subuser in Private Cloud DB of {Parent}", pcUser.user_email);
                                    break;
                                }
                            }
                            else
                            {
                                isPrivateCloudUser = true;
                                parentUserEmail = pcUser.user_email;
                                _logger.LogInformation("✅ Found user in Private Cloud DB");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "⚠️ Failed to check Private Cloud DB for {Email}", pcUser.user_email);
                        }
                    }
                }

                // Step 3: If still not found, return error
                if (user == null && subuser == null)
                {
                    _logger.LogWarning("Password reset requested for non-existent email: {Email}", dto.Email);

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

                // ✅ Step 5: Calculate expiry using DateTimeHelper
                DateTime expiresAt = DateTimeHelper.AddMinutesFromNow(10);

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
                    CreatedAt = DateTimeHelper.GetUtcNow(),  // ✅ Use DateTimeHelper
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
        /// Resend OTP - Expires old OTP and generates new one
        /// </summary>
        public async Task<ForgotPasswordResponseDto> ResendOtpAsync(
            ForgotPasswordRequestDto dto,
            string? ipAddress,
            string? userAgent)
        {
            try
            {
                _logger.LogInformation("🔄 Resend OTP requested for {Email}", dto.Email);

                // ✅ Step 1: Check Main DB first
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.user_email == dto.Email);

                var subuser = await _context.subuser
                    .FirstOrDefaultAsync(s => s.subuser_email == dto.Email);

                bool isPrivateCloudUser = false;
                string? parentUserEmail = null;

                // ✅ Step 2: If not found in Main DB, check Private Cloud databases
                if (user == null && subuser == null)
                {
                    _logger.LogInformation("🔍 User not in Main DB, checking Private Cloud databases for {Email}", dto.Email);

                    var privateCloudUsers = await _context.Users
                        .Where(u => u.is_private_cloud == true)
                        .Select(u => new { u.user_email, u.user_id })
                        .ToListAsync();

                    foreach (var pcUser in privateCloudUsers)
                    {
                        try
                        {
                            var connectionString = await _tenantService.GetConnectionStringForUserAsync(pcUser.user_email);
                            var mainConnectionString = _configuration.GetConnectionString("ApplicationDbContextConnection");

                            if (connectionString == mainConnectionString)
                                continue;

                            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                            using var privateContext = new ApplicationDbContext(optionsBuilder.Options);
                            privateContext.Database.SetCommandTimeout(10);

                            user = await privateContext.Users
                                .FirstOrDefaultAsync(u => u.user_email == dto.Email);

                            if (user == null)
                            {
                                subuser = await privateContext.subuser
                                    .FirstOrDefaultAsync(s => s.subuser_email == dto.Email);

                                if (subuser != null)
                                {
                                    isPrivateCloudUser = true;
                                    parentUserEmail = pcUser.user_email;
                                    _logger.LogInformation("✅ Found subuser in Private Cloud DB of {Parent}", pcUser.user_email);
                                    break;
                                }
                            }
                            else
                            {
                                isPrivateCloudUser = true;
                                parentUserEmail = pcUser.user_email;
                                _logger.LogInformation("✅ Found user in Private Cloud DB");
                                break;
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "⚠️ Failed to check Private Cloud DB for {Email}", pcUser.user_email);
                        }
                    }
                }

                if (user == null && subuser == null)
                {
                    _logger.LogWarning("Resend OTP requested for non-existent email: {Email}", dto.Email);
                    return new ForgotPasswordResponseDto
                    {
                        Success = false,
                        Message = "If this email exists, you will receive a new password reset code.",
                        Otp = string.Empty,
                        ResetLink = string.Empty,
                        ResetToken = string.Empty
                    };
                }

                // Step 2: Expire all previous active requests for this email
                // ✅ Step 2: Expire all previous active requests using DateTimeHelper
                var previousRequests = await _context.ForgotPasswordRequests
                    .Where(f => f.Email == dto.Email && !f.IsUsed && f.ExpiresAt > DateTimeHelper.GetUtcNow())
                    .ToListAsync();

                if (previousRequests.Any())
                {
                    _logger.LogInformation("📋 Expiring {Count} previous OTP requests for {Email}",
                        previousRequests.Count, dto.Email);

                    foreach (var oldRequest in previousRequests)
                    {
                        oldRequest.IsUsed = true;  // Mark as used to expire
                        oldRequest.ExpiresAt = DateTimeHelper.GetUtcNow();  // ✅ Use DateTimeHelper
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("✅ Expired {Count} old OTP(s) for {Email}",
                        previousRequests.Count, dto.Email);
                }

                // ✅ Step 3: Generate new OTP and token with DateTimeHelper
                string newOtp = GenerateOtp();
                string newResetToken = GenerateResetToken();
                DateTime expiresAt = DateTimeHelper.AddMinutesFromNow(10);

                // Step 4: Create new request
                var newRequest = new ForgotPasswordRequest
                {
                    UserId = user?.user_id ?? subuser!.subuser_id,
                    Email = dto.Email,
                    UserType = user != null ? "user" : "subuser",
                    Otp = newOtp,
                    ResetToken = newResetToken,
                    IsUsed = false,
                    ExpiresAt = expiresAt,
                    CreatedAt = DateTimeHelper.GetUtcNow(),  // ✅ Use DateTimeHelper
                    IpAddress = ipAddress,
                    UserAgent = userAgent
                };

                await _repository.CreateAsync(newRequest);

                // Step 5: Generate reset link
                string baseUrl = _configuration["AppSettings:BaseUrl"] ?? "http://localhost:5000";
                string resetLink = $"{baseUrl}/reset-password?token={newResetToken}";

                string userType = user != null ? "User" : "Subuser";
                _logger.LogInformation("✅ NEW OTP generated for {UserType} {Email}. OTP: {Otp}, Token: {Token}",
                    userType, dto.Email, newOtp, newResetToken);

                return new ForgotPasswordResponseDto
                {
                    Success = true,
                    Message = $"New OTP generated successfully for {userType}. Previous OTP has been expired.",
                    Otp = newOtp,
                    ResetLink = resetLink,
                    ResetToken = newResetToken,
                    ExpiresAt = expiresAt,
                    ExpiryMinutes = 10
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resending OTP for {Email}", dto.Email);
                return new ForgotPasswordResponseDto
                {
                    Success = false,
                    Message = "An error occurred while resending OTP.",
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

                // Step 3: Find user or subuser (check Main DB first, then Private Cloud)
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.user_email == dto.Email);

                var subuser = await _context.subuser
                    .FirstOrDefaultAsync(s => s.subuser_email == dto.Email);

                bool isPrivateCloudUser = false;
                string? parentUserEmail = null;
                ApplicationDbContext? privateContext = null;

                // ✅ If not in Main DB, check Private Cloud databases
                if (user == null && subuser == null)
                {
                    _logger.LogInformation("🔍 User not in Main DB, checking Private Cloud for password reset: {Email}", dto.Email);

                    var privateCloudUsers = await _context.Users
                        .Where(u => u.is_private_cloud == true)
                        .Select(u => new { u.user_email, u.user_id })
                        .ToListAsync();

                    foreach (var pcUser in privateCloudUsers)
                    {
                        try
                        {
                            var connectionString = await _tenantService.GetConnectionStringForUserAsync(pcUser.user_email);
                            var mainConnectionString = _configuration.GetConnectionString("ApplicationDbContextConnection");

                            if (connectionString == mainConnectionString)
                                continue;

                            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                            optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

                            var tempContext = new ApplicationDbContext(optionsBuilder.Options);
                            tempContext.Database.SetCommandTimeout(10);

                            user = await tempContext.Users
                                .FirstOrDefaultAsync(u => u.user_email == dto.Email);

                            if (user == null)
                            {
                                subuser = await tempContext.subuser
                                    .FirstOrDefaultAsync(s => s.subuser_email == dto.Email);

                                if (subuser != null)
                                {
                                    isPrivateCloudUser = true;
                                    parentUserEmail = pcUser.user_email;
                                    privateContext = tempContext;
                                    _logger.LogInformation("✅ Found subuser in Private Cloud DB for password reset");
                                    break;
                                }
                            }
                            else
                            {
                                isPrivateCloudUser = true;
                                parentUserEmail = pcUser.user_email;
                                privateContext = tempContext;
                                _logger.LogInformation("✅ Found user in Private Cloud DB for password reset");
                                break;
                            }

                            // If not found, dispose temp context
                            if (!isPrivateCloudUser)
                            {
                                await tempContext.DisposeAsync();
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "⚠️ Failed to check Private Cloud DB for {Email}", pcUser.user_email);
                        }
                    }
                }

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

                // ✅ Step 5: Update password in correct database
                if (user != null)
                {
                    // ✅ Store both: plain text for reference, BCrypt hash for login verification
                    user.user_password = dto.NewPassword;     // Plain text (for admin reference)
                    user.hash_password = hashedPassword;      // BCrypt hashed - LOGIN CHECKS THIS
                    user.updated_at = DateTimeHelper.GetUtcNow();

                    if (isPrivateCloudUser && privateContext != null)
                    {
                        privateContext.Entry(user).State = EntityState.Modified;
                        await privateContext.SaveChangesAsync();
                        await privateContext.DisposeAsync();
                        _logger.LogInformation("✅ Password updated in Private Cloud DB for user {Email}", dto.Email);
                    }
                    else
                    {
                        _context.Entry(user).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("✅ Password updated in Main DB for user {Email}", dto.Email);
                    }
                }
                else
                {
                    subuser!.subuser_password = hashedPassword;  // BCrypt hashed
                    subuser.UpdatedAt = DateTimeHelper.GetUtcNow();

                    if (isPrivateCloudUser && privateContext != null)
                    {
                        privateContext.Entry(subuser).State = EntityState.Modified;
                        await privateContext.SaveChangesAsync();
                        await privateContext.DisposeAsync();
                        _logger.LogInformation("✅ Password updated in Private Cloud DB for subuser {Email}", dto.Email);
                    }
                    else
                    {
                        _context.Entry(subuser).State = EntityState.Modified;
                        await _context.SaveChangesAsync();
                        _logger.LogInformation("✅ Password updated in Main DB for subuser {Email}", dto.Email);
                    }
                }

                // Step 6: Mark request as used
                request.IsUsed = true;
                await _repository.UpdateAsync(request);

                string userType = user != null ? "User" : "Subuser";
                _logger.LogInformation("✅ Password reset successful for {UserType} {Email}", userType, dto.Email);

                return new ResetPasswordResponseDto
                {
                    Success = true,
                    Message = $"Password reset successfully for {userType}. You can now log in with your new password.",
                    ResetAt = DateTimeHelper.GetUtcNow()  // ✅ Use DateTimeHelper
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
