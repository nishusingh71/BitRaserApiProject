using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Forgot Password Request Model
    /// Stores OTP and reset token for password reset WITHOUT email sending
    /// Works for both Users and Subusers
    /// </summary>
    [Table("forgot_password_requests")]
    public class ForgotPasswordRequest
    {
      [Key]
 [Column("id")]
    public int Id { get; set; }

    /// <summary>
        /// User ID from users or subuser table
        /// Stores user_id for Users, subuser_id for Subusers
        /// </summary>
        [Required]
        [Column("user_id")]
    public int UserId { get; set; }

 /// <summary>
/// User or Subuser email for reference
 /// </summary>
  [Required]
    [MaxLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User type: "user" or "subuser"
        /// Helps identify which table to update on password reset
        /// </summary>
   [MaxLength(20)]
    [Column("user_type")]
        public string UserType { get; set; } = "user";

        /// <summary>
 /// 6-digit OTP for verification
        /// </summary>
        [Required]
        [StringLength(6, MinimumLength = 6)]
        [Column("otp")]
 public string Otp { get; set; } = string.Empty;

   /// <summary>
   /// Unique reset token (GUID + random bytes)
        /// </summary>
  [Required]
  [MaxLength(500)]
      [Column("reset_token")]
   public string ResetToken { get; set; } = string.Empty;

 /// <summary>
     /// Is this request already used?
     /// </summary>
 [Column("is_used")]
   public bool IsUsed { get; set; } = false;

      /// <summary>
      /// Expiration timestamp (10 minutes from creation)
   /// </summary>
 [Required]
  [Column("expires_at")]
    public DateTime ExpiresAt { get; set; }

  /// <summary>
        /// Request creation timestamp
        /// </summary>
 [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// IP address of requester (optional security)
  /// </summary>
        [MaxLength(50)]
   [Column("ip_address")]
        public string? IpAddress { get; set; }

   /// <summary>
      /// User agent (optional security)
   /// </summary>
 [MaxLength(500)]
        [Column("user_agent")]
        public string? UserAgent { get; set; }

    // Navigation property (optional - only for users, not subusers)
        [ForeignKey("UserId")]
        public virtual users? User { get; set; }
    }
}
