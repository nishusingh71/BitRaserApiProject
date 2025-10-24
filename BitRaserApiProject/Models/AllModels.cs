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
        public string? phone_number { get; set; } // User's phone number
      
        // NEW FIELDS - All Optional (Nullable)
        [MaxLength(100)]
        public string? department { get; set; } // Department name - OPTIONAL
        
     [MaxLength(100)]
        public string? user_group { get; set; } // User group - OPTIONAL  
        public DateTime? last_login { get; set; } // Last login timestamp - OPTIONAL
        
        [MaxLength(50)]
        public string? user_role { get; set; } // User role - OPTIONAL 
        public int? license_allocation { get; set; } // Number of licenses - OPTIONAL
        
        [MaxLength(50)]
        public string? status { get; set; } // User status - OPTIONAL (default handled in controller)
    
        // Existing fields
        public string? payment_details_json { get; set; } // JSON storing payment details
        public string? license_details_json { get; set; } // JSON storing license details
        
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
    
        // Subuser Details (Enhanced fields from documentation)
        [MaxLength(100)]
        public string? subuser_username { get; set; } // Added: Username field
        
      [MaxLength(100)]
        public string? Name { get; set; }
        
     [MaxLength(20)]
        public string? Phone { get; set; }
        
        [MaxLength(100)]
    public string? JobTitle { get; set; }
     
        [MaxLength(100)]
        public string? Department { get; set; }
   
  // Role & Permissions
        [Required, MaxLength(50)]
      public string Role { get; set; } = "subuser"; // subuser, team_member, limited_admin
    
[MaxLength(50)]
        public string AccessLevel { get; set; } = "limited"; // full, limited, read_only
        
        public string? PermissionsJson { get; set; } // JSON string for granular permissions 
        // Machine & License Access
     public int? AssignedMachines { get; set; } = 0;
        public int? MaxMachines { get; set; } = 5;
        public string? MachineIdsJson { get; set; } // JSON array of accessible machine IDs
        public string? LicenseIdsJson { get; set; } // JSON array of accessible license IDs   
        // Group Access
 public int? GroupId { get; set; }
        
     // Status
   [MaxLength(50)]
   public string Status { get; set; } = "active"; // active, inactive, suspended
        
        public bool IsEmailVerified { get; set; } = false;
        public bool CanCreateSubusers { get; set; } = false;
  public bool CanViewReports { get; set; } = true;
      public bool CanManageMachines { get; set; } = false;
        public bool CanAssignLicenses { get; set; } = false;
    
    // Notifications
        public bool EmailNotifications { get; set; } = true;
        public bool SystemAlerts { get; set; } = true;
        
      // Session & Security
[MaxLength(500)]
    public string? LastLoginIp { get; set; }
        
        public DateTime? LastLoginAt { get; set; }
        public int FailedLoginAttempts { get; set; } = 0;
        public DateTime? LockedUntil { get; set; }
        
        // Audit
     public int CreatedBy { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public int? UpdatedBy { get; set; }
  
        [MaxLength(500)]
      public string? Notes { get; set; }
        
        // Navigation properties for role-based system - ignore in JSON to prevent circular references
     [JsonIgnore]
        public ICollection<SubuserRole> SubuserRoles { get; set; } = new List<SubuserRole>();
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

    public class Group
    {
        [Key]
        public int group_id { get; set; }
        
        [Required, MaxLength(100)]
        public string name { get; set; } = string.Empty; // groupname
        
        [MaxLength(500)]
        public string? description { get; set; } // groupdescription
        
        public int license_allocation { get; set; } = 0; // groplicenseallocation
        
        public string? permissions_json { get; set; } // grouppermission stored as JSON
   
      public DateTime created_at { get; set; } = DateTime.UtcNow;
        public DateTime? updated_at { get; set; }
        
        [MaxLength(50)]
        public string status { get; set; } = "active";
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

    // Enhanced Update model with additional properties
    public class Update
    {
        [Key]
        public int version_id { get; set; } // Primary Key

        [Required, MaxLength(20)]
        public string version_number { get; set; } = string.Empty; // e.g. "1.0.0"

        [Required]
        public string changelog { get; set; } = string.Empty; // Description of changes

        [Required, MaxLength(500)]
        public string download_link { get; set; } = string.Empty; // URL to installer

        public DateTime release_date { get; set; } = DateTime.UtcNow; // Release timestamp

        public bool is_mandatory_update { get; set; } = false; // Whether update is mandatory

        // Enhanced properties
        [MaxLength(50)]
        public string? update_type { get; set; } = "minor"; // "major", "minor", "patch", "hotfix"

        [MaxLength(20)]
        public string? update_status { get; set; } = "active"; // "active", "deprecated", "recalled"

        public long? file_size_bytes { get; set; } // Size of update file in bytes

        [MaxLength(255)]
        public string? checksum_md5 { get; set; } // MD5 checksum for file verification

        [MaxLength(255)]
        public string? checksum_sha256 { get; set; } // SHA256 checksum for file verification

        [MaxLength(255)]
        public string? minimum_os_version { get; set; } // Minimum OS version required

        [MaxLength(500)]
        public string? supported_platforms { get; set; } // Comma-separated platform list

        public DateTime? deprecation_date { get; set; } // When this version will be deprecated

        [MaxLength(255)]
        public string? created_by_email { get; set; } // Email of user who created this update

        public DateTime created_at { get; set; } = DateTime.UtcNow; // Creation timestamp

        public DateTime updated_at { get; set; } = DateTime.UtcNow; // Last modification timestamp

        [MaxLength(1000)]
        public string? security_notes { get; set; } // Security-related information

        [MaxLength(1000)]
        public string? installation_notes { get; set; } // Special installation instructions

        public bool requires_restart { get; set; } = false; // Whether system restart is required

        public bool auto_download_enabled { get; set; } = true; // Whether auto-download is allowed

        [MaxLength(100)]
        public string? rollback_version { get; set; } // Version to rollback to if this update fails
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
        
        // Navigation properties
        [JsonIgnore]
        public ICollection<RolePermission>? RolePermissions { get; set; } = new List<RolePermission>();
        
        [JsonIgnore]
        public ICollection<UserRole>? UserRoles { get; set; } = new List<UserRole>();
        
        [JsonIgnore]
        public ICollection<SubuserRole>? SubuserRoles { get; set; } = new List<SubuserRole>();
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
        
        // Navigation properties
        [JsonIgnore]
        public ICollection<RolePermission>? RolePermissions { get; set; } = new List<RolePermission>();
        
        [JsonIgnore]
        public ICollection<PermissionRoute>? PermissionRoutes { get; set; } = new List<PermissionRoute>();
    }

    public class RolePermission
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public Role? Role { get; set; }
        
        [JsonIgnore]
        public Permission? Permission { get; set; }
    }

    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedByEmail { get; set; } = string.Empty;
        
        // Navigation properties
        [JsonIgnore]
        public users? User { get; set; }
        
        [JsonIgnore]
        public Role? Role { get; set; }
    }

    public class SubuserRole
    {
        public int SubuserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedByEmail { get; set; } = string.Empty;
        
        // Navigation properties
        [JsonIgnore]
        public subuser? Subuser { get; set; }
        
        [JsonIgnore]
        public Role? Role { get; set; }
    }

    public class Route
    {
        [Key]
        public int RouteId { get; set; }
        
        [Required, MaxLength(500)]
        public string RoutePath { get; set; } = string.Empty;
        
        [Required, MaxLength(10)]
        public string HttpMethod { get; set; } = string.Empty;
        
        [MaxLength(200)]
        public string Description { get; set; } = string.Empty;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties
        [JsonIgnore]
        public ICollection<PermissionRoute>? PermissionRoutes { get; set; } = new List<PermissionRoute>();
    }

    public class PermissionRoute
    {
        public int PermissionId { get; set; }
        public int RouteId { get; set; }
        
        // Navigation properties
        [JsonIgnore]
        public Permission? Permission { get; set; }
        
        [JsonIgnore]
        public Route? Route { get; set; }
    }
} // End of namespace BitRaserApiProject.Models

// ReportRequest class (used for PDF generation)
// Note: ReportData and ErasureLogEntry are defined in ReportDataOptional.cs
// Note: FlexibleStringConverter is defined in ReportDataOptional.cs
public class ReportRequest
{
    [Required]
public BitRaserApiProject.Models.ReportData ReportData { get; set; } = new();

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