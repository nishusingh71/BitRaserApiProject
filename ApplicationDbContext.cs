using BitRaserApiProject.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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
        public string user_password { get; set; } // Hashed password

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



}
