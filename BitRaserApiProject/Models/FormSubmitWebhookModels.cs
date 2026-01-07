using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// DTO for FormSubmit.co webhook payload
    /// FormSubmit sends form data as JSON to configured webhook URL
    /// </summary>
    public class FormSubmitWebhookDto
    {
        /// <summary>
        /// User's name from the form
        /// </summary>
        [Required(ErrorMessage = "Name is required")]
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// User's email address - used for auto-response
        /// </summary>
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        [JsonPropertyName("email")]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// User's message content
        /// </summary>
        [Required(ErrorMessage = "Message is required")]
        [MinLength(1, ErrorMessage = "Message cannot be empty")]
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Optional subject line (FormSubmit uses _subject)
        /// </summary>
        [JsonPropertyName("_subject")]
        public string? Subject { get; set; }

        /// <summary>
        /// FormSubmit unique submission ID (for duplicate detection)
        /// </summary>
        [JsonPropertyName("_formsubmit_id")]
        public string? FormSubmitId { get; set; }

        /// <summary>
        /// Optional: User's company
        /// </summary>
        [JsonPropertyName("company")]
        public string? Company { get; set; }

        /// <summary>
        /// Optional: User's phone
        /// </summary>
        [JsonPropertyName("phone")]
        public string? Phone { get; set; }

        /// <summary>
        /// Honey pot field for spam detection (should be empty)
        /// </summary>
        [JsonPropertyName("_honey")]
        public string? HoneyPot { get; set; }
    }

    /// <summary>
    /// Response for FormSubmit webhook
    /// </summary>
    public class FormSubmitWebhookResponse
    {
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        [JsonPropertyName("submissionId")]
        public int? SubmissionId { get; set; }

        [JsonPropertyName("emailSent")]
        public bool EmailSent { get; set; }
    }
}
