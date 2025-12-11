using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models.DTOs
{
    /// <summary>
    /// License Activation API - Request/Response DTOs
    /// Matches Python controller expectations exactly
    /// </summary>

    #region Activation

    /// <summary>
    /// License activation request
    /// POST /api/LicenseActivation/activate
    /// </summary>
    public class ActivateLicenseRequest
    {
        [Required(ErrorMessage = "License key is required")]
        public string license_key { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hardware ID is required")]
    public string hwid { get; set; } = string.Empty;
    }

    /// <summary>
    /// License activation response
    /// </summary>
    public class ActivateLicenseResponse
    {
        public string status { get; set; } = string.Empty; // OK / INVALID_KEY / REVOKED / LICENSE_EXPIRED / HW_MISMATCH / ERROR
 public string? expiry { get; set; } // yyyy-MM-dd format
        public string? edition { get; set; } // BASIC / PRO / ENTERPRISE
        public int? server_revision { get; set; }
        public string? license_status { get; set; } // ACTIVE / REVOKED / EXPIRED
    }

    #endregion

    #region Renewal

    /// <summary>
    /// License renewal request
    /// POST /api/LicenseActivation/renew
 /// </summary>
    public class RenewLicenseRequest
    {
      [Required(ErrorMessage = "License key is required")]
        public string license_key { get; set; } = string.Empty;

        /// <summary>
        /// Number of days to extend (optional, default: 365)
        /// </summary>
        public int? extension_days { get; set; }
    }

    /// <summary>
    /// License renewal response
    /// </summary>
    public class RenewLicenseResponse
    {
        public string status { get; set; } = string.Empty; // OK / INVALID_KEY / REVOKED / ERROR
        public string? new_expiry { get; set; } // yyyy-MM-dd format
        public int? server_revision { get; set; }
    }

  #endregion

    #region Upgrade

    /// <summary>
    /// License upgrade request
    /// POST /api/LicenseActivation/upgrade
    /// </summary>
    public class UpgradeLicenseRequest
    {
        [Required(ErrorMessage = "License key is required")]
        public string license_key { get; set; } = string.Empty;

      [Required(ErrorMessage = "New edition is required")]
        public string new_edition { get; set; } = string.Empty; // BASIC / PRO / ENTERPRISE
    }

    /// <summary>
    /// License upgrade response
    /// </summary>
    public class UpgradeLicenseResponse
    {
        public string status { get; set; } = string.Empty; // OK / INVALID_KEY / REVOKED / INVALID_EDITION / ERROR
        public string? edition { get; set; }
     public int? server_revision { get; set; }
    }

    #endregion

    #region Sync

    /// <summary>
    /// License sync request (check for server-side changes)
    /// POST /api/LicenseActivation/sync
    /// </summary>
    public class SyncLicenseRequest
    {
    [Required(ErrorMessage = "License key is required")]
        public string license_key { get; set; } = string.Empty;

        [Required(ErrorMessage = "Hardware ID is required")]
        public string hwid { get; set; } = string.Empty;

   [Required(ErrorMessage = "Local revision is required")]
        public int local_revision { get; set; }
    }

    /// <summary>
    /// License sync response
    /// </summary>
    public class SyncLicenseResponse
    {
        public string status { get; set; } = string.Empty; // NO_CHANGE / UPDATE / REVOKED / HW_MISMATCH / INVALID_KEY / ERROR
   public string? expiry { get; set; } // yyyy-MM-dd format
        public string? edition { get; set; }
        public int? server_revision { get; set; }
        public string? license_status { get; set; } // ACTIVE / REVOKED / EXPIRED
    }

    #endregion

    #region Admin Operations

    /// <summary>
    /// Create new license (Admin only)
    /// POST /api/LicenseActivation/admin/create
    /// </summary>
    public class CreateLicenseRequest
    {
        [Required(ErrorMessage = "License key is required")]
        [MaxLength(64)]
   public string license_key { get; set; } = string.Empty;

        [Required(ErrorMessage = "Expiry days is required")]
        [Range(1, 36500, ErrorMessage = "Expiry days must be between 1 and 36500")]
        public int expiry_days { get; set; } = 365;

        [Required(ErrorMessage = "Edition is required")]
    [MaxLength(32)]
        public string edition { get; set; } = "BASIC";

        [MaxLength(255)]
        public string? user_email { get; set; }

        [MaxLength(500)]
        public string? notes { get; set; }
    }

    /// <summary>
    /// Revoke license (Admin only)
    /// POST /api/LicenseActivation/admin/revoke
    /// </summary>
    public class RevokeLicenseRequest
    {
      [Required(ErrorMessage = "License key is required")]
        public string license_key { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? reason { get; set; }
    }

    /// <summary>
    /// License statistics response
    /// GET /api/LicenseActivation/admin/statistics
    /// </summary>
    public class LicenseStatisticsResponse
    {
    public int total { get; set; }
        public StatusBreakdown by_status { get; set; } = new();
        public EditionBreakdown by_edition { get; set; } = new();
  public ExpiringBreakdown expiring { get; set; } = new();

        public class StatusBreakdown
        {
            public int active { get; set; }
      public int expired { get; set; }
      public int revoked { get; set; }
     }

        public class EditionBreakdown
        {
 public int basic { get; set; }
       public int pro { get; set; }
            public int enterprise { get; set; }
        }

        public class ExpiringBreakdown
   {
      public int in7Days { get; set; }
 public int in30Days { get; set; }
        }
    }

    /// <summary>
    /// License details response
    /// GET /api/LicenseActivation/admin/{licenseKey}
    /// </summary>
    public class LicenseDetailsResponse
    {
     public int id { get; set; }
        public string license_key { get; set; } = string.Empty;
 public string? hwid { get; set; }
        public int expiry_days { get; set; }
      public string? expiry_date { get; set; } // Calculated
      public int remaining_days { get; set; }
 public string edition { get; set; } = string.Empty;
        public string status { get; set; } = string.Empty;
        public int server_revision { get; set; }
        public DateTime created_at { get; set; }
        public DateTime? last_seen { get; set; }
        public string? user_email { get; set; }
        public string? notes { get; set; }
    }

    /// <summary>
    /// Bulk license generation request (Admin only)
    /// POST /api/LicenseActivation/admin/bulk-generate
    /// </summary>
    public class BulkGenerateLicensesRequest
    {
        [Required]
        [Range(1, 1000, ErrorMessage = "Count must be between 1 and 1000")]
        public int count { get; set; }

        [Required]
        [Range(1, 36500, ErrorMessage = "Expiry days must be between 1 and 36500")]
 public int expiry_days { get; set; } = 365;

   [Required]
public string edition { get; set; } = "BASIC";

     public string? key_prefix { get; set; } // Optional prefix for generated keys
    }

    /// <summary>
    /// Bulk license generation response
    /// </summary>
  public class BulkGenerateLicensesResponse
    {
        public bool success { get; set; }
    public int generated_count { get; set; }
        public List<string> license_keys { get; set; } = new();
   public string message { get; set; } = string.Empty;
    }

    #endregion
}
