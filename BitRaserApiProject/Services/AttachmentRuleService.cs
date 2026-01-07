using BitRaserApiProject.Models;
using BitRaserApiProject.Services.Email;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Interface for attachment rule service
    /// Handles conditional attachment logic based on service type
    /// </summary>
    public interface IAttachmentRuleService
    {
        /// <summary>
        /// Filter attachments based on service type rules
        /// DriveEraser → PDF only, FileEraser → Excel only
        /// </summary>
        List<EmailAttachment> FilterAttachments(ServiceType serviceType, List<EmailAttachment>? attachments);

        /// <summary>
        /// Validate if an attachment is allowed for the given service type
        /// </summary>
        bool IsAttachmentAllowed(ServiceType serviceType, EmailAttachment attachment);

        /// <summary>
        /// Detect service type from product name or metadata
        /// </summary>
        ServiceType DetectServiceType(string? productName);

        /// <summary>
        /// Get allowed MIME types for a service type
        /// </summary>
        List<string> GetAllowedMimeTypes(ServiceType serviceType);
    }

    /// <summary>
    /// Attachment Rule Service Implementation
    /// Enforces conditional attachment rules for Drive Eraser and File Eraser
    /// </summary>
    public class AttachmentRuleService : IAttachmentRuleService
    {
        private readonly ILogger<AttachmentRuleService> _logger;

        // MIME type constants
        private static readonly List<string> PDF_MIME_TYPES = new()
        {
            "application/pdf"
        };

        private static readonly List<string> EXCEL_MIME_TYPES = new()
        {
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "application/vnd.ms-excel",
            "application/excel",
            "application/x-excel"
        };

        public AttachmentRuleService(ILogger<AttachmentRuleService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Filter attachments based on service type rules
        /// DriveEraser → PDF only (no Excel)
        /// FileEraser → Excel only (no PDF)
        /// </summary>
        public List<EmailAttachment> FilterAttachments(ServiceType serviceType, List<EmailAttachment>? attachments)
        {
            if (attachments == null || !attachments.Any())
            {
                return new List<EmailAttachment>();
            }

            var filteredAttachments = new List<EmailAttachment>();

            foreach (var attachment in attachments)
            {
                if (IsAttachmentAllowed(serviceType, attachment))
                {
                    // Validate attachment content
                    if (ValidateAttachmentContent(attachment))
                    {
                        filteredAttachments.Add(attachment);
                        _logger.LogInformation("✅ Attachment allowed: {FileName} ({MimeType}) for {ServiceType}",
                            attachment.FileName, attachment.ContentType, serviceType);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ Attachment validation failed: {FileName} - Empty or corrupted content",
                            attachment.FileName);
                    }
                }
                else
                {
                    _logger.LogWarning("🚫 Attachment blocked: {FileName} ({MimeType}) not allowed for {ServiceType}",
                        attachment.FileName, attachment.ContentType, serviceType);
                }
            }

            return filteredAttachments;
        }

        /// <summary>
        /// Check if attachment is allowed for the given service type
        /// </summary>
        public bool IsAttachmentAllowed(ServiceType serviceType, EmailAttachment attachment)
        {
            if (attachment == null) return false;

            var contentType = attachment.ContentType?.ToLowerInvariant() ?? "";
            var fileName = attachment.FileName?.ToLowerInvariant() ?? "";

            return serviceType switch
            {
                // Drive Eraser: PDF ONLY (block Excel)
                ServiceType.DriveEraser => IsPdfAttachment(contentType, fileName),

                // File Eraser: Excel ONLY (block PDF)
                ServiceType.FileEraser => IsExcelAttachment(contentType, fileName),

                // Combined or Unknown: Allow both
                ServiceType.Combined => IsPdfAttachment(contentType, fileName) || IsExcelAttachment(contentType, fileName),
                
                // Default: Allow all
                _ => true
            };
        }

        /// <summary>
        /// Detect service type from product name
        /// </summary>
        public ServiceType DetectServiceType(string? productName)
        {
            if (string.IsNullOrEmpty(productName))
            {
                return ServiceType.Unknown;
            }

            var name = productName.ToLowerInvariant();

            if (name.Contains("drive") && name.Contains("eraser"))
            {
                return ServiceType.DriveEraser;
            }

            if (name.Contains("file") && name.Contains("eraser"))
            {
                return ServiceType.FileEraser;
            }

            if (name.Contains("combined") || name.Contains("bundle") || name.Contains("suite"))
            {
                return ServiceType.Combined;
            }

            return ServiceType.Unknown;
        }

        /// <summary>
        /// Get allowed MIME types for a service type
        /// </summary>
        public List<string> GetAllowedMimeTypes(ServiceType serviceType)
        {
            return serviceType switch
            {
                ServiceType.DriveEraser => PDF_MIME_TYPES,
                ServiceType.FileEraser => EXCEL_MIME_TYPES,
                ServiceType.Combined => PDF_MIME_TYPES.Concat(EXCEL_MIME_TYPES).ToList(),
                _ => PDF_MIME_TYPES.Concat(EXCEL_MIME_TYPES).ToList()
            };
        }

        #region Private Helpers

        private bool IsPdfAttachment(string contentType, string fileName)
        {
            return PDF_MIME_TYPES.Any(mime => contentType.Contains(mime)) ||
                   fileName.EndsWith(".pdf");
        }

        private bool IsExcelAttachment(string contentType, string fileName)
        {
            return EXCEL_MIME_TYPES.Any(mime => contentType.Contains(mime)) ||
                   fileName.EndsWith(".xlsx") ||
                   fileName.EndsWith(".xls");
        }

        private bool ValidateAttachmentContent(EmailAttachment attachment)
        {
            // Check for empty or corrupted content
            if (attachment.Content == null || attachment.Content.Length == 0)
            {
                return false;
            }

            // Check file size (max 25MB for emails)
            const int MAX_SIZE = 25 * 1024 * 1024;
            if (attachment.Content.Length > MAX_SIZE)
            {
                _logger.LogWarning("⚠️ Attachment too large: {FileName} ({Size} bytes)", 
                    attachment.FileName, attachment.Content.Length);
                return false;
            }

            // Basic magic number validation for PDF
            if (attachment.ContentType?.Contains("pdf") == true)
            {
                // PDF files start with %PDF
                if (attachment.Content.Length >= 4)
                {
                    var header = System.Text.Encoding.ASCII.GetString(attachment.Content, 0, 4);
                    if (!header.StartsWith("%PDF"))
                    {
                        _logger.LogWarning("⚠️ Invalid PDF header: {FileName}", attachment.FileName);
                        return false;
                    }
                }
            }

            // Basic magic number validation for Excel (XLSX is a ZIP file)
            if (attachment.ContentType?.Contains("spreadsheet") == true || 
                attachment.ContentType?.Contains("excel") == true)
            {
                // XLSX files are ZIP archives starting with PK
                if (attachment.Content.Length >= 2)
                {
                    if (attachment.Content[0] != 0x50 || attachment.Content[1] != 0x4B) // PK
                    {
                        _logger.LogWarning("⚠️ Invalid Excel/ZIP header: {FileName}", attachment.FileName);
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion
    }
}
