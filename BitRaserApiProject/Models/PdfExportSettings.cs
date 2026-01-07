using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace DSecureApi.Models
{
    /// <summary>
    /// PDF Export Settings entity for storing user/subuser default export configurations
    /// </summary>
    [Table("pdf_export_settings")]
    public class PdfExportSettings
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [StringLength(255)]
        [Column("user_email")]
        public string UserEmail { get; set; } = string.Empty;

        [StringLength(500)]
        [Column("report_title")]
        public string? ReportTitle { get; set; }

        [StringLength(500)]
        [Column("header_text")]
        public string? HeaderText { get; set; }

        [StringLength(255)]
        [Column("technician_name")]
        public string? TechnicianName { get; set; }

        [StringLength(255)]
        [Column("technician_dept")]
        public string? TechnicianDept { get; set; }

        [StringLength(255)]
        [Column("validator_name")]
        public string? ValidatorName { get; set; }

        [StringLength(255)]
        [Column("validator_dept")]
        public string? ValidatorDept { get; set; }

        // Base64 encoded images stored in database
        [Column("header_left_logo")]
        public string? HeaderLeftLogoBase64 { get; set; }

        [Column("header_right_logo")]
        public string? HeaderRightLogoBase64 { get; set; }

        [Column("watermark_image")]
        public string? WatermarkImageBase64 { get; set; }

        [Column("technician_signature")]
        public string? TechnicianSignatureBase64 { get; set; }

        [Column("validator_signature")]
        public string? ValidatorSignatureBase64 { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        [Column("created_at")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// DTO for saving PDF export settings (multipart/form-data)
    /// </summary>
    public class SavePdfExportSettingsRequest
    {
        [FromForm]
        public string? ReportTitle { get; set; }

        [FromForm]
        public string? HeaderText { get; set; }

        [FromForm]
        public string? TechnicianName { get; set; }

        [FromForm]
        public string? TechnicianDept { get; set; }

        [FromForm]
        public string? ValidatorName { get; set; }

        [FromForm]
        public string? ValidatorDept { get; set; }

        // Image files for upload via multipart/form-data
        [FromForm]
        public IFormFile? HeaderLeftLogo { get; set; }

        [FromForm]
        public IFormFile? HeaderRightLogo { get; set; }

        [FromForm]
        public IFormFile? WatermarkImage { get; set; }

        [FromForm]
        public IFormFile? TechnicianSignature { get; set; }

        [FromForm]
        public IFormFile? ValidatorSignature { get; set; }
    }

    /// <summary>
    /// DTO for saving PDF export settings (JSON body with base64 images)
    /// </summary>
    public class SavePdfExportSettingsJsonRequest
    {
        [JsonPropertyName("reportTitle")]
        public string? ReportTitle { get; set; }

        [JsonPropertyName("headerText")]
        public string? HeaderText { get; set; }

        [JsonPropertyName("technicianName")]
        public string? TechnicianName { get; set; }

        [JsonPropertyName("technicianDept")]
        public string? TechnicianDept { get; set; }

        [JsonPropertyName("validatorName")]
        public string? ValidatorName { get; set; }

        [JsonPropertyName("validatorDept")]
        public string? ValidatorDept { get; set; }

        // Base64 encoded images
        [JsonPropertyName("headerLeftLogo")]
        public string? HeaderLeftLogoBase64 { get; set; }

        [JsonPropertyName("headerRightLogo")]
        public string? HeaderRightLogoBase64 { get; set; }

        [JsonPropertyName("watermarkImage")]
        public string? WatermarkImageBase64 { get; set; }

        [JsonPropertyName("technicianSignature")]
        public string? TechnicianSignatureBase64 { get; set; }

        [JsonPropertyName("validatorSignature")]
        public string? ValidatorSignatureBase64 { get; set; }
    }

    /// <summary>
    /// Response DTO for PDF export settings
    /// </summary>
    public class PdfExportSettingsResponse
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("userEmail")]
        public string UserEmail { get; set; } = string.Empty;

        [JsonPropertyName("reportTitle")]
        public string? ReportTitle { get; set; }

        [JsonPropertyName("headerText")]
        public string? HeaderText { get; set; }

        [JsonPropertyName("technicianName")]
        public string? TechnicianName { get; set; }

        [JsonPropertyName("technicianDept")]
        public string? TechnicianDept { get; set; }

        [JsonPropertyName("validatorName")]
        public string? ValidatorName { get; set; }

        [JsonPropertyName("validatorDept")]
        public string? ValidatorDept { get; set; }

        [JsonPropertyName("hasHeaderLeftLogo")]
        public bool HasHeaderLeftLogo { get; set; }

        [JsonPropertyName("hasHeaderRightLogo")]
        public bool HasHeaderRightLogo { get; set; }

        [JsonPropertyName("hasWatermarkImage")]
        public bool HasWatermarkImage { get; set; }

        [JsonPropertyName("hasTechnicianSignature")]
        public bool HasTechnicianSignature { get; set; }

        [JsonPropertyName("hasValidatorSignature")]
        public bool HasValidatorSignature { get; set; }

        // ✅ Actual base64 image data for frontend display
        [JsonPropertyName("headerLeftLogoBase64")]
        public string? HeaderLeftLogoBase64 { get; set; }

        [JsonPropertyName("headerRightLogoBase64")]
        public string? HeaderRightLogoBase64 { get; set; }

        [JsonPropertyName("watermarkImageBase64")]
        public string? WatermarkImageBase64 { get; set; }

        [JsonPropertyName("technicianSignatureBase64")]
        public string? TechnicianSignatureBase64 { get; set; }

        [JsonPropertyName("validatorSignatureBase64")]
        public string? ValidatorSignatureBase64 { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }

        [JsonPropertyName("createdAt")]
        public DateTime CreatedAt { get; set; }

        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }
}
