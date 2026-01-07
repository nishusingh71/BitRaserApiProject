using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System;
using System.Collections.Generic;

namespace DSecureApi.Models
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

        public string? machine_details_json { get; set; } // Stores additional machine details in JSON format
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

        // ✅ PRIVATE CLOUD / MULTI-TENANT FIELDS
        public bool? is_private_cloud { get; set; } = false; // Private cloud flag
        public bool? private_api { get; set; } = false; // Private API access flag

        [JsonIgnore] // Don't expose in API responses
        public string? private_db_connection_string { get; set; } // Encrypted connection string

        public DateTime? private_db_created_at { get; set; } // When private DB was setup

        [MaxLength(20)]
        public string? private_db_status { get; set; } = "inactive"; // active/inactive/error

        public DateTime? private_db_last_validated { get; set; } // Last health check

        [MaxLength(20)]
        public string? private_db_schema_version { get; set; } // Schema version tracking

        [MaxLength(20)]
        public string? phone_number { get; set; } // User's phone number

        // NEW FIELDS - All Optional (Nullable)
        [MaxLength(100)]
        public string? department { get; set; } // Department name - OPTIONAL

        [MaxLength(100)]
        public string? user_group { get; set; } // User group - OPTIONAL  

        // Session & Status Fields
        public DateTime? last_login { get; set; } // Last login timestamp - OPTIONAL
        public DateTime? last_logout { get; set; } // Last logout timestamp - OPTIONAL

        [MaxLength(50)]
        public string? user_role { get; set; } // User role - OPTIONAL 
        public int? license_allocation { get; set; } // Number of licenses - OPTIONAL

        [MaxLength(50)]
        public string? status { get; set; } // Account status - OPTIONAL (active, inactive, suspended, banned)

        [MaxLength(50)]
        public string? activity_status { get; set; } // Online/Offline status - OPTIONAL (online, offline)

        // Timezone Support
        [MaxLength(100)]
        public string? timezone { get; set; } // User's timezone (e.g., "Asia/Kolkata", "America/New_York")

        // Domain-related fields
        [MaxLength(255)]
        public string? domain { get; set; } // Domain name for organization (e.g., "company.com")

        [MaxLength(255)]
        public string? organization_name { get; set; } // Organization/Company name

        public bool? is_domain_admin { get; set; } = false; // Is this user a domain admin?

        // ✅ QUOTA & LIMITS - Set by admin, enforced everywhere (including private cloud)
        // These fields are READ-ONLY for users - only admin can modify via main DB
        
        /// <summary>
        /// Maximum number of subusers this user can create
        /// </summary>
        public int? max_subusers { get; set; } = 5;

        /// <summary>
        /// Maximum number of groups this user can create
        /// </summary>
        public int? max_groups { get; set; } = 3;

        /// <summary>
        /// Maximum number of departments this user can use
        /// </summary>
        public int? max_departments { get; set; } = 3;

        /// <summary>
        /// Maximum total licenses this user can allocate
        /// </summary>
        public int? max_licenses { get; set; } = 10;

        /// <summary>
        /// License expiry date - after this date, all write operations blocked
        /// </summary>
        public DateTime? license_expiry_date { get; set; }

        /// <summary>
        /// Current number of subusers used (auto-updated)
        /// </summary>
        public int? used_subusers { get; set; } = 0;

        /// <summary>
        /// Current total licenses allocated to subusers (auto-updated)
        /// </summary>
        public int? used_licenses { get; set; } = 0;

        /// <summary>
        /// Quota last synced to private cloud
        /// </summary>
        public DateTime? quota_synced_at { get; set; }

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

        // ✅ NEW: Activity Tracking Fields
        /// <summary>
        /// Type of activity: LOGIN, LOGOUT, REPORT_DOWNLOAD, SUBUSER_CREATE, etc.
        /// </summary>
        [MaxLength(50)]
        public string? activity_type { get; set; }

        /// <summary>
        /// JSON with additional activity details
        /// </summary>
        public string? activity_details { get; set; }

        /// <summary>
        /// ID of affected resource (subuser_id, report_id, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? resource_id { get; set; }

        /// <summary>
        /// Type of resource: subuser, report, machine, group, department
        /// </summary>
        [MaxLength(50)]
        public string? resource_type { get; set; }
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
        public int? superuser_id { get; set; } // ✅ Made nullable - Reference to users.user_id (superuser)

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
        public string? Department { get; set; }

        // Role & Permissions
        [MaxLength(50)]
        public string? Role { get; set; } = "subuser"; // subuser, team_member, limited_admin

        public string? PermissionsJson { get; set; } // JSON string for granular permissions 

        // Machine & License Access
        public int? AssignedMachines { get; set; } = 0;
        public int? MaxMachines { get; set; } = 5;

        public string? MachineIdsJson { get; set; } // JSON array of accessible machine IDs

        public string? LicenseIdsJson { get; set; } // JSON array of accessible license IDs   

        // Group Access
        public int? GroupId { get; set; }

        [MaxLength(100)]
        public string? subuser_group { get; set; } // Group name or identifier (string format)

        public int? license_allocation { get; set; } = 0; // Number of licenses allocated to subuser

        // Status & Activity Fields
        [MaxLength(50)]
        public string? status { get; set; } = "active"; // Account status: active, inactive, suspended

        [MaxLength(50)]
        public string? activity_status { get; set; } // Online/Offline status: online, offline

        public DateTime? last_login { get; set; } // Last login timestamp
        public DateTime? last_logout { get; set; } // Last logout timestamp

        [MaxLength(100)]
        public string? timezone { get; set; } // Subuser's timezone (e.g., Asia/Kolkata)

        // Domain-related fields
        [MaxLength(255)]
        public string? domain { get; set; } // Domain inherited from parent user

        [MaxLength(255)]
        public string? organization_name { get; set; } // Organization name inherited from parent

        // Permissions Flags
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
        public string user_email { get; set; }
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

        // ✅ NEW: Admin User ID for ownership tracking
        [MaxLength(255)]
        public string? admin_user_id { get; set; }

        // ✅ Navigation property for group members
        public virtual ICollection<GroupMember>? GroupMembers { get; set; }
    }

    /// <summary>
    /// GroupMember entity for tracking user membership in groups
    /// </summary>
    public class GroupMember
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GroupId { get; set; }

        [Required]
        [MaxLength(255)]
        public string UserId { get; set; } = string.Empty;

        [MaxLength(255)]
        public string? UserEmail { get; set; }

        [MaxLength(100)]
        public string? UserName { get; set; }

        [MaxLength(50)]
        public string? Role { get; set; } // member, admin, viewer

        [MaxLength(100)]
        public string? Department { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // ✅ Navigation properties
        public virtual Group? Group { get; set; }
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
}

public class License
{
    [Key]
    public int Id { get; set; }
    [Required, MaxLength(64)]
    public string LicenseKey { get; set; } = string.Empty;
    [MaxLength(128)]
    public string? Hwid { get; set; }
    [Column(TypeName = "int")]
    public DateTime ExpiryDays { get; set; }
    [Required, MaxLength(32)]
    public string Edition { get; set; } = "BASIC"; // BASIC / PRO / ENTERPRISE
    [Required, MaxLength(16)]
    public string Status { get; set; } = "ACTIVE"; // ACTIVE / REVOKED / EXPIRED
    public int ServerRevision { get; set; } = 1;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastSeen { get; set; }
}
// End of namespace DSecureApi.Models
// ReportRequest class (used for PDF generation)
// Note: ReportData and ErasureLogEntry are defined in ReportDataOptional.cs
// Note: FlexibleStringConverter is defined in ReportDataOptional.cs
public class ReportRequest
{
    [Required]
    public DSecureApi.Models.ReportData ReportData { get; set; } = new();

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