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
        public string report_name { get; set; } // Name of the report
        public string erasure_method { get; set; } // Erasure method used
        public DateTime report_datetime { get; set; } // Timestamp of report creation
        public string report_details_json { get; set; } // JSON storing detailed erasure process
    }

    public class users
    {
        public int user_id { get; set; } // Primary Key
        public string user_name { get; set; } // Name of the user
        public string user_email { get; set; } // Email (must be unique)
        public string user_password { get; set; } // Hashed password
        public string phone_number { get; set; } // User's phone number
        public string payment_details_json { get; set; } // JSON storing payment details
        public string license_details_json { get; set; } // JSON storing license details
    }
}
