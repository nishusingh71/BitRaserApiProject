using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;
namespace BitRaserApiProject.Models
{
    public class machines
    {
        [Key]
        [Required]
        public string fingerprint_hash { get; set; } // Unique machine identifier

        [Required, MaxLength(255)]
        public string mac_address { get; set; }
        [Required, MaxLength(255)]
        public string physical_drive_id { get; set; }
        [Required, MaxLength(255)]
        public string cpu_id { get; set; }
        [Required, MaxLength(255)]
        public string bios_serial { get; set; }
        [Required, MaxLength(255)]
        public string os_version { get; set; }
        [MaxLength(255)]
        public string user_email { get; set; }
        [MaxLength(255)]
        public string? subuser_email { get; set; }
        public bool license_activated { get; set; } // Activation status
        public DateTime? license_activation_date { get; set; } // Null if never activated
        public int license_days_valid { get; set; } = 0; // Number of valid days
        public string license_details_json { get; set; } // Stores license info
        public int demo_usage_count { get; set; } // Tracks demo usage count
        public DateTime created_at { get; set; } = DateTime.UtcNow; // Auto-set by DB
        public DateTime updated_at { get; set; } = DateTime.UtcNow; // Auto-updated by DB
        public string vm_status { get; set; } // 'physical' or 'vm'
    }

    public class audit_reports
    {
        [Key]
        public int report_id { get; set; } // Primary Key

        [Required, MaxLength(255)]
        public string client_email { get; set; } // Email of the client who performed erasure

        [Required, MaxLength(255)]
        public string report_name { get; set; } // Name of the report

        [Required, MaxLength(255)]
        public string erasure_method { get; set; } // Method used for erasure

        [Required]
        public DateTime report_datetime { get; set; } = DateTime.UtcNow; // Date and time of the report

        [Required]
        public string report_details_json { get; set; } // JSON containing detailed erasure process

        public bool synced { get; set; } = false; // JSON containing detailed erasure process
    }

    public class users
    {
        [Key]
        public int user_id { get; set; } // Primary Key

        [Required, MaxLength(255)]
        public string user_name { get; set; } // Name of the user

        [Required, MaxLength(255)]
        public string user_email { get; set; } // Email (must be unique)

        [Required, MaxLength(255)]
        public string user_password { get; set; } // Plain password

        [JsonIgnore]
        public string? hash_password { get; set; } // Hashed password

        public bool? is_private_cloud { get; set; } = false; // Private cloud flag
        public bool? private_api { get; set; } = false; // Private API access flag

        [MaxLength(20)]
        public string phone_number { get; set; } // User's phone number
        public string payment_details_json { get; set; } // JSON storing payment details
        public string license_details_json { get; set; } // JSON storing license details
        
        public DateTime created_at { get; set; } = DateTime.UtcNow; // Account creation date
        public DateTime updated_at { get; set; } = DateTime.UtcNow; // Last update date
        
        // Navigation properties for role-based system - ignore in JSON to prevent circular references
        [JsonIgnore]
        public ICollection<UserRole>? UserRoles { get; set; } = new List<UserRole>();
    }

    public class Sessions
    {
        [Key]
        public int session_id { get; set; } // Primary Key
        public string user_email { get; set; } // User email (instead of user_id)
        public DateTime login_time { get; set; } // Login timestamp
        public DateTime? logout_time { get; set; } // Logout timestamp (nullable)
        public string ip_address { get; set; } // User IP address
        public string device_info { get; set; } // Device/browser info
        public string session_status { get; set; } // Status: active, closed, expired
    }

    public class logs
    {
        [Key]
        public int log_id { get; set; } // Primary Key
        public string user_email { get; set; } // User email (nullable for system logs)
        public string log_level { get; set; } // e.g. Info, Warning, Error
        public string log_message { get; set; } // Log message
        public string log_details_json { get; set; } // Additional details in JSON
        public DateTime created_at { get; set; } = DateTime.UtcNow; // Timestamp of log creation
    }

    public class subuser
    {
        [Key]
        public int subuser_id { get; set; } // Primary Key
        public int superuser_id { get; set; } // Reference to users.user_id (superuser)
        [Required, MaxLength(255)]
        public string subuser_email { get; set; } // Email of the subuser
        [Required, MaxLength(255)]
        public string subuser_password { get; set; } // Hashed password
        public string user_email { get; set; } // ID of the parent user
        
        // Navigation properties for role-based system - ignore in JSON to prevent circular references
        [JsonIgnore]
        public ICollection<SubuserRole>? SubuserRoles { get; set; } = new List<SubuserRole>();
    }

    public class Commands
    {
        [Key]
        public int Command_id { get; set; }
        public string command_text { get; set; }
        public DateTime issued_at { get; set; } = DateTime.UtcNow;
        public string command_json { get; set; } // Changed from object to string
        public string command_status { get; set; } // Changed from object to string
    }

    public class User_role_profile
    {
        [Key]
        public int role_id { get; set; } // Primary Key
        public string user_email { get; set; } // User's email
        public int manage_user_id { get; set; } // User ID who manages this role
        public string role_name { get; set; } // Role name
        public string role_email { get; set; } // Role email
    }

    public class User_role
    {
        [Key]
        public int role_id { get; set; } // Primary Key
        public string role_name { get; set; } // Role name
        public string permissions_json { get; set; } // Permissions in JSON format
        public string user_email { get; set; } // User's email  
        public string user_password_hash { get; set; } // User's password hash
    }

    // Add the missing Update class
    public class Update
    {
        [Key]
        public int version_id { get; set; } // Primary Key

        [Required, MaxLength(20)]
        public string version_number { get; set; } // e.g. "1.0.0"

        [Required]
        public string changelog { get; set; } // Description of changes

        [Required, MaxLength(500)]
        public string download_link { get; set; } // URL to installer

        public DateTime release_date { get; set; } = DateTime.UtcNow; // Release timestamp

        public bool is_mandatory_update { get; set; } = false; // Release timestamp
    }
    
    // Role-based system models
    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        
        [Required, MaxLength(100)]
        public string RoleName { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public int HierarchyLevel { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }
        
        [Required, MaxLength(100)]
        public string PermissionName { get; set; } = string.Empty;
        
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedByEmail { get; set; } = string.Empty;
    }

    public class SubuserRole
    {
        public int SubuserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedByEmail { get; set; } = string.Empty;
    }
}

public class ReportRequest
{
    [Required]
    public ReportData ReportData { get; set; } = new();

    public string? ReportTitle { get; set; }
    public string? HeaderText { get; set; }

    public byte[]? HeaderLeftLogo { get; set; }
    public byte[]? HeaderRightLogo { get; set; }
    public byte[]? WatermarkImage { get; set; }

    public string? TechnicianName { get; set; }
    public string? TechnicianDept { get; set; }
    public string? ValidatorName { get; set; }
    public string? ValidatorDept { get; set; }

    public byte[]? TechnicianSignature { get; set; }
    public byte[]? ValidatorSignature { get; set; }
}

public class ReportData
{
    [JsonPropertyName("report_id")]
    public string? ReportId { get; set; }

    [JsonPropertyName("datetime")]
    public string? ReportDate { get; set; } 

    [JsonPropertyName("software_name")]
    public string? SoftwareName { get; set; }

    [JsonPropertyName("product_version")]
    public string? ProductVersion { get; set; }

    [JsonPropertyName("digital_signature")]
    public string? DigitalSignature { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("process_mode")]
    public string? ProcessMode { get; set; }

    [JsonPropertyName("os")]
    public string? OS { get; set; }

    [JsonPropertyName("os_version")]
    public string? OSVersion { get; set; }

    [JsonPropertyName("computer_name")]
    public string? ComputerName { get; set; }

    [JsonPropertyName("mac_address")]
    public string? MacAddress { get; set; }

    [JsonPropertyName("manufacturer")]
    public string? Manufacturer { get; set; }

    [JsonPropertyName("Eraser_Start_Time")]
    public string? EraserStartTime { get; set; } 

    [JsonPropertyName("Eraser_End_Time")]
    public string? EraserEndTime { get; set; } 

    [JsonPropertyName("eraser_method")]
    public string? EraserMethod { get; set; }

    [JsonPropertyName("validation_method")]
    public string? ValidationMethod { get; set; } 

    [JsonPropertyName("Erasure_Type")]
    public string? ErasureType { get; set; }

    [JsonPropertyName("total_files")]
    public int TotalFiles { get; set; }

    [JsonPropertyName("erased_files")]
    public int ErasedFiles { get; set; }

    [JsonPropertyName("failed_files")]
    public int FailedFiles { get; set; }

    [JsonPropertyName("erasure_log")]
    public List<ErasureLogEntry>? ErasureLog { get; set; } = new();
}

public class ErasureLogEntry
{
    // JSON: "target"
    [JsonPropertyName("target")]
    public string? Target { get; set; }

    // JSON: "free_space"
    [JsonPropertyName("free_space")]
    public string? Capacity { get; set; }

    [JsonPropertyName("total_sectors")]
    public string? TotalSectors { get; set; }

    [JsonPropertyName("sectors_erased")]
    public string? SectorsErased { get; set; }

    // JSON: "dummy_file_size" → map to File Size column
    [JsonPropertyName("dummy_file_size")]
    public string? Size { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; } 
}