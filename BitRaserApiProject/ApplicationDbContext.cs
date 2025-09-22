using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using BitRaserApiProject.Models;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject
{
    public class ApplicationDbContext : DbContext
    {
        

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // DbSet representing the 'machines' table
        public DbSet<machines> Machines { get; set; }
        public DbSet<audit_reports> AuditReports { get; set; }
        public DbSet<users> Users { get; set; }
        public DbSet<Update> Updates { get; set; }
        public DbSet<Commands> Commands { get; set; }
        public DbSet<User_role_profile> User_role_profile { get; set; }
        public DbSet<Sessions> Sessions { get; set; }
        public DbSet<logs> logs { get; set; }
        public DbSet<subuser> subuser { get; set; }
        //public DbSet<PdfGenerateRequest> PdfGenerateRequests { get; set; }
        
        public static string HashLicenseKey(string licenseKey)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(licenseKey));
            return Convert.ToBase64String(bytes);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Machines Table
            modelBuilder.Entity<machines>()
                .Property(m => m.fingerprint_hash)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.mac_address)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.physical_drive_id)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.cpu_id)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.bios_serial)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.os_version)
                .HasMaxLength(255);

            modelBuilder.Entity<machines>()
                .Property(m => m.user_email)
                .HasMaxLength(255);

            modelBuilder.Entity<machines>()
                .Property(m => m.license_details_json)
                .HasColumnType("json");


            // Audit Reports Table
            modelBuilder.Entity<audit_reports>()
                .HasKey(a => a.report_id);

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.client_email)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.report_name)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.erasure_method)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.report_details_json)
                .HasColumnType("json")
                .IsRequired();

            // Users Table
            modelBuilder.Entity<users>()
                .HasKey(u => u.user_id);

            modelBuilder.Entity<users>()
                .Property(u => u.user_name)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<users>()
                .Property(u => u.user_email)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<users>()
                .HasIndex(u => u.user_email)
                .IsUnique();

            modelBuilder.Entity<users>()
                .Property(u => u.user_password)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<users>()
                .Property(u => u.phone_number)
                .HasMaxLength(20);

            modelBuilder.Entity<users>()
                .Property(u => u.payment_details_json)
                .HasColumnType("json");

            modelBuilder.Entity<users>()
                .Property(u => u.license_details_json)
                .HasColumnType("json");

            //commands table
            modelBuilder.Entity<Commands>()
                .HasKey(c => c.Command_id);
            modelBuilder.Entity<Commands>()
                .Property(c => c.command_name)
                .HasMaxLength(255)
                .IsRequired();
            modelBuilder.Entity<Commands>()
                .Property(c => c.command_description)
                .HasMaxLength(1000)
                .IsRequired();
            modelBuilder.Entity<Commands>()
                .Property(c => c.command_parameters)
                .HasColumnType("json");
            //user role profile table
            modelBuilder.Entity<User_role_profile>()
                .HasKey(r => r.role_id);
            modelBuilder.Entity<User_role_profile>()
                .Property(r => r.role_name)
                .HasMaxLength(255)
                .IsRequired();
            modelBuilder.Entity<User_role_profile>()
                .Property(r => r.role_description)
                .HasMaxLength(1000);
            modelBuilder.Entity<User_role_profile>()
                .Property(r => r.permissions)
                .HasColumnType("json");
            modelBuilder.Entity<User_role_profile>()
                .Property(r => r.created_at)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<User_role_profile>()
                .Property(r => r.updated_at)
                .ValueGeneratedOnUpdate();

            //sessions table
            modelBuilder.Entity<Sessions>()
                .HasKey(s => s.session_id);
            modelBuilder.Entity<Sessions>()
                .Property(s => s.user_email)
                .HasMaxLength(255)
                .IsRequired();
            modelBuilder.Entity<Sessions>()
                .Property(s => s.login_time)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<Sessions>()
                .Property(s => s.ip_address)
                .HasMaxLength(45); // Max length for IPv6
            modelBuilder.Entity<Sessions>()
                .Property(s => s.device_info)
                .HasMaxLength(1000);
            modelBuilder.Entity<Sessions>()
                .Property(s => s.session_status)
                .HasMaxLength(50)
                .IsRequired();
            //logs table
            modelBuilder.Entity<logs>()
                .HasKey(l => l.log_id);
            modelBuilder.Entity<logs>()
                        
                .Property(l => l.user_email)
                .HasMaxLength(255);
            modelBuilder.Entity<logs>()
                .Property(l => l.log_level)
                .HasMaxLength(50)
                .IsRequired();
            modelBuilder.Entity<logs>()
                .Property(l => l.log_timestamp)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
            modelBuilder.Entity<logs>()
                .Property(l => l.message)
                .HasMaxLength(2000)
                .IsRequired();

        }
    }

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

        public DateTime? license_activation_date { get; set; } // Null if never activated

        public int license_days_valid { get; set; } = 0; // Number of valid days

        public string license_details_json { get; set; } // Stores license info
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

            public bool synced { get; set; } = false; // Indicates if report is synced to cloud
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

        
        public string ? hash_password { get; set; } // Hashed password

        public bool is_private_cloud { get; set; } = false; // Private cloud flag
        public bool private_api { get; set; } = false; // Private API access flag



        [MaxLength(20)]
        public string phone_number { get; set; } // User's phone number
        public string payment_details_json { get; set; } // JSON storing payment details
        public string license_details_json { get; set; } // JSON storing license details
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Update
        {
            [Key]
            public int version_id { get; set; }  // Primary Key

            [Required, MaxLength(20)]
            public string version_number { get; set; }  // e.g. "1.0.0"

            [Required]
            public string changelog { get; set; }  // Description of changes

            [Required, MaxLength(500)]
            public string download_link { get; set; }  // URL to installer

            public DateTime release_date { get; set; } = DateTime.UtcNow;  // Release timestamp

            public bool is_mandatory_update { get; set; } = false;  // Flag for mandatory update
        }

    public class  subuser
    {
        
        [Key]
        public int subuser_id { get; set; } // Primary Key
        [Required, MaxLength(255)]
        public string subuser_name { get; set; } // Name of the subuser
        [Required, MaxLength(255)]
        public string subuser_email { get; set; } // Email of the subuser
        [Required, MaxLength(255)]
        public string subuser_password { get; set; } // Hashed password
        public int parent_user_id { get; set; } // ID of the parent user
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
        public DateTime log_timestamp { get; set; } // Timestamp of the log entry
        public string message { get; set; } // Log message/details
    }

    public class Commands
    {
        [Key]
        public int Command_id { get; set; } // Primary Key
        public string command_name { get; set; } // Name of the command
        public string command_description { get; set; } // Description of what the command does
        public string command_parameters { get; set; } // JSON or delimited string of parameters
        public DateTime created_at { get; set; } = DateTime.UtcNow; // Timestamp when command was created
        public DateTime? updated_at { get; set; } // Timestamp when command was last updated
    }

    public class User_role_profile
    {
        [Key]
        public int role_id { get; set; } // Primary Key
        public string role_name { get; set; } // Name of the role (e.g., Admin, User)
        public string role_description { get; set; } // Description of the role
        public string permissions { get; set; } // JSON or delimited string of permissions
        public DateTime created_at { get; set; } = DateTime.UtcNow; // Timestamp when role was created
        public DateTime? updated_at { get; set; } // Timestamp when role was last updated
        public string user_email { get; internal set; }
    }
    public class User_role
    {
        [Key]
        public int user_role_id { get; set; } // Primary Key
        public int user_id { get; set; } // Foreign Key to users table
        public int role_id { get; set; } // Foreign Key to User_role_profile table
        public DateTime assigned_at { get; set; } = DateTime.UtcNow; // Timestamp when role was assigned

        public DateTime updated_at { get; set; } // Timestamp when role was last updated    

    }
     // Hash a license key
    


    }
    public static class SecurityHelpers
    {
        public static string HashPassword(string password, out string salt)
        {
            salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 100_000, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }

        public static bool VerifyPassword(string password, string hash, string salt)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 100_000, HashAlgorithmName.SHA256);
            var computedHash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(hash),
                Convert.FromBase64String(computedHash)
            );
        }
   
}


    