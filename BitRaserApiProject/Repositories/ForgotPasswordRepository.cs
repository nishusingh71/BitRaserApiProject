using BitRaserApiProject.Models;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Repositories
{
    /// <summary>
 /// Repository for Forgot Password operations
    /// Handles database operations for password reset WITHOUT email sending
    /// </summary>
    public interface IForgotPasswordRepository
{
  Task<ForgotPasswordRequest?> GetByEmailAsync(string email);
        Task<ForgotPasswordRequest?> GetByTokenAsync(string resetToken);
        Task<ForgotPasswordRequest?> GetByEmailAndOtpAsync(string email, string otp);
  Task<ForgotPasswordRequest> CreateAsync(ForgotPasswordRequest request);
        Task UpdateAsync(ForgotPasswordRequest request);
        Task DeleteExpiredRequestsAsync();
   Task<int> GetActiveRequestCountForEmailAsync(string email);
    }

    public class ForgotPasswordRepository : IForgotPasswordRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ForgotPasswordRepository> _logger;

     public ForgotPasswordRepository(
            ApplicationDbContext context,
      ILogger<ForgotPasswordRepository> logger)
 {
   _context = context;
            _logger = logger;
  }

  /// <summary>
        /// Get latest valid (non-used, non-expired) request by email
   /// </summary>
        public async Task<ForgotPasswordRequest?> GetByEmailAsync(string email)
        {
  return await _context.ForgotPasswordRequests
         .Where(f => f.Email == email &&
       !f.IsUsed &&
     f.ExpiresAt > DateTime.UtcNow)
  .OrderByDescending(f => f.CreatedAt)
                .FirstOrDefaultAsync();
        }

    /// <summary>
        /// Get request by reset token
        /// </summary>
    public async Task<ForgotPasswordRequest?> GetByTokenAsync(string resetToken)
        {
      return await _context.ForgotPasswordRequests
       .FirstOrDefaultAsync(f => f.ResetToken == resetToken &&
    !f.IsUsed &&
           f.ExpiresAt > DateTime.UtcNow);
  }

  /// <summary>
        /// Get request by email and OTP
        /// </summary>
        public async Task<ForgotPasswordRequest?> GetByEmailAndOtpAsync(string email, string otp)
    {
     return await _context.ForgotPasswordRequests
       .FirstOrDefaultAsync(f => f.Email == email &&
   f.Otp == otp &&
     !f.IsUsed &&
   f.ExpiresAt > DateTime.UtcNow);
   }

        /// <summary>
  /// Create new forgot password request
     /// </summary>
  public async Task<ForgotPasswordRequest> CreateAsync(ForgotPasswordRequest request)
        {
   _context.ForgotPasswordRequests.Add(request);
  await _context.SaveChangesAsync();
   _logger.LogInformation("Created forgot password request for {Email} with token {Token}", 
    request.Email, request.ResetToken);
       return request;
     }

        /// <summary>
        /// Update existing request (e.g., mark as used)
  /// </summary>
     public async Task UpdateAsync(ForgotPasswordRequest request)
  {
            _context.ForgotPasswordRequests.Update(request);
   await _context.SaveChangesAsync();
            _logger.LogInformation("Updated forgot password request {Id} for {Email}", 
    request.Id, request.Email);
        }

  /// <summary>
 /// Delete expired requests (cleanup)
        /// </summary>
        public async Task DeleteExpiredRequestsAsync()
        {
  var expiredRequests = await _context.ForgotPasswordRequests
         .Where(f => f.ExpiresAt < DateTime.UtcNow || f.IsUsed)
        .ToListAsync();

      if (expiredRequests.Any())
 {
           _context.ForgotPasswordRequests.RemoveRange(expiredRequests);
           await _context.SaveChangesAsync();
        _logger.LogInformation("Deleted {Count} expired forgot password requests", 
        expiredRequests.Count);
      }
}

  /// <summary>
        /// Get count of active (non-expired, non-used) requests for rate limiting
   /// </summary>
        public async Task<int> GetActiveRequestCountForEmailAsync(string email)
  {
    return await _context.ForgotPasswordRequests
     .CountAsync(f => f.Email == email &&
   !f.IsUsed &&
       f.ExpiresAt > DateTime.UtcNow);
        }
    }
}
