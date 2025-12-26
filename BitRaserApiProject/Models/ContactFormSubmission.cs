using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Contact form submission entity for storing website contact requests
    /// </summary>
    [Table("contact_form_submissions")]
    public class ContactFormSubmission
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Column("name")]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(255)]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [StringLength(255)]
        [Column("company")]
        public string? Company { get; set; }

        [StringLength(50)]
        [Column("phone")]
        public string? Phone { get; set; }

        [StringLength(100)]
        [Column("country")]
        public string? Country { get; set; }

        [StringLength(50)]
        [Column("business_type")]
        public string? BusinessType { get; set; }

        [StringLength(255)]
        [Column("solution_type")]
        public string? SolutionType { get; set; }

        [StringLength(500)]
        [Column("compliance_requirements")]
        public string? ComplianceRequirements { get; set; }

        [Required]
        [MinLength(10)]
        [Column("message")]
        public string Message { get; set; } = string.Empty;

        [StringLength(20)]
        [Column("usage_type")]
        public string? UsageType { get; set; }

        [StringLength(100)]
        [Column("source")]
        public string? Source { get; set; }

        [Column("submitted_at")]
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        [StringLength(50)]
        [Column("status")]
        public string Status { get; set; } = "pending";

        [StringLength(50)]
        [Column("ip_address")]
        public string? IpAddress { get; set; }

        [StringLength(500)]
        [Column("user_agent")]
        public string? UserAgent { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("read_at")]
        public DateTime? ReadAt { get; set; }

        [StringLength(255)]
        [Column("read_by")]
        public string? ReadBy { get; set; }

        [Column("notes")]
        public string? Notes { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating a new contact form submission
    /// </summary>
    public class ContactFormSubmissionDto
    {
        [Required(ErrorMessage = "Name is required")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("businessType")]
        public string? BusinessType { get; set; }

        [JsonPropertyName("solutionType")]
        public string? SolutionType { get; set; }

        [JsonPropertyName("complianceRequirements")]
        public string? ComplianceRequirements { get; set; }

        [Required(ErrorMessage = "Message is required")]
        [MinLength(10, ErrorMessage = "Message must be at least 10 characters")]
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("usageType")]
        public string? UsageType { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("timestamp")]
        public string? Timestamp { get; set; }
    }

    /// <summary>
    /// Response DTO for contact form submission
    /// </summary>
    public class ContactFormSubmissionResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        [JsonPropertyName("company")]
        public string? Company { get; set; }

        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        [JsonPropertyName("country")]
        public string? Country { get; set; }

        [JsonPropertyName("businessType")]
        public string? BusinessType { get; set; }

        [JsonPropertyName("solutionType")]
        public string? SolutionType { get; set; }

        [JsonPropertyName("complianceRequirements")]
        public string? ComplianceRequirements { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("usageType")]
        public string? UsageType { get; set; }

        [JsonPropertyName("source")]
        public string? Source { get; set; }

        [JsonPropertyName("submittedAt")]
        public DateTime SubmittedAt { get; set; }

        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("isRead")]
        public bool IsRead { get; set; }

        [JsonPropertyName("readAt")]
        public DateTime? ReadAt { get; set; }

        [JsonPropertyName("readBy")]
        public string? ReadBy { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for updating contact form status
    /// </summary>
    public class UpdateContactFormStatusDto
    {
        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("notes")]
        public string? Notes { get; set; }

        [JsonPropertyName("isRead")]
        public bool? IsRead { get; set; }
    }
}
