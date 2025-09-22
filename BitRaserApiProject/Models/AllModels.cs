using System.ComponentModel.DataAnnotations;

namespace BitRaserApiProject.Models
{
    public class machines
    {
        public int machine_id { get; set; } // Primary Key
        public string fingerprint_hash { get; set; } // Unique machine hash
        public string mac_address { get; set; }
        public string physical_drive_id { get; set; }
        public string cpu_id { get; set; }
        public string bios_serial { get; set; }
        public string os_version { get; set; }
        public string user_email { get; set; }
        public bool license_activated { get; set; } // Activation status
        public DateTime? license_activation_date { get; set; } // Null if never activated
        public int license_days_valid { get; set; } // 0 if in demo mode
        public string license_details_json { get; set; } // Stores cloud service & usage details
        public int demo_usage_count { get; set; } // Tracks demo usage count
        public DateTime created_at { get; set; } // Auto-set by DB
        public DateTime updated_at { get; set; } // Auto-updated by DB
        public string vm_status { get; set; } // 'physical' or 'vm'
    }

    public class audit_reports
    {
        public int report_id { get; set; } // Primary Key
        public string client_email { get; set; } // Client email
        //public string user_email { get; set; }
        public string report_name { get; set; } // Name of the report
        public string erasure_method { get; set; } // Erasure method used
        public DateTime report_datetime { get; set; } // Timestamp of report creation
        public string report_details_json { get; set; } // JSON storing detailed erasure process
    }

    public class users
    {
        public int user_id { get; set; }
        public string user_name { get; set; }
        public string user_email { get; set; }
        public string user_password { get; set; } // Hashed password
        public string? user_password_encrypted { get; set; } // Encrypted password

        // Subuser relationship: parent user id (nullable, if not a subuser)
        public int? parent_user_id { get; set; }

        // Private cloud flag
        public bool is_private_cloud { get; set; }

        // Private API flag
        public bool private_api { get; set; }

        public string phone_number { get; set; }
        public string payment_details_json { get; set; }
        public string license_details_json { get; set; }
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
        public DateTime created_at { get; set; } // Timestamp of log creation
    }

    public class subuser
    {
        [Key]
        public int subuser_id { get; set; } // Primary Key
        public int superuser_id { get; set; } // Reference to users.user_id (superuser)
        public string subuser_email { get; set; } // Subuser email
        public string subuser_password_hash { get; set; } // Subuser password hash
    }
    public class Commands
    {
        [Key]
        public int Command_id { get; set; }
        public string command_text { get; set; }
        public DateTime issued_at { get; set; }
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

    public class ReportRequest
    {
        // General Report Info
        public string ReportId { get; set; }
        public string defualt_image { get; set; }   // e.g. Completed, Failed, In Progress
        public string client_email { get; set; }     // e.g. Secure Erase, Quick Erase
        public string jsonreport_details_json { get; set; }        // e.g. D-SecureErase v1.2.3
        //public string DigitalIdentifier { get; set; } // e.g. Machine unique ID
        //public DateTime ReportDate { get; set; } = DateTime.UtcNow;

        //// Annexure: Erasure Log (table data)
        //public List<ErasureLog> Logs { get; set; } = new List<ErasureLog>();
    }

    //public class ErasureLog
    //{
    //    public string Volume { get; set; }          // e.g. Disk C:
    //    public string Capacity { get; set; }        // e.g. 512GB
    //    public long TotalSectors { get; set; }
    //    public long ErasedSectors { get; set; }
    //    public string Status { get; set; }          // e.g. Success, Partial, Failed
    //}


}
