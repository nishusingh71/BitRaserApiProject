using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitRaserApiProject.Models
{
    /// <summary>
    /// Private Cloud Database Configuration
 /// Stores custom database connection strings for private cloud users
    /// </summary>
    [Table("private_cloud_databases")]
    public class PrivateCloudDatabase
    {
    [Key]
        [Column("config_id")]
        public int ConfigId { get; set; }

   /// <summary>
        /// User ID who owns this private database
      /// </summary>
        [Required]
        [Column("user_id")]
        public int UserId { get; set; }

        /// <summary>
        /// User email for reference
        /// </summary>
        [Required]
        [MaxLength(255)]
        [Column("user_email")]
        public string UserEmail { get; set; } = string.Empty;

        /// <summary>
        /// Encrypted database connection string
  /// Format: server=xxx;database=xxx;user=xxx;password=xxx;
        /// </summary>
        [Required]
        [Column("connection_string")]
    public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Database type: mysql, postgresql, sqlserver
 /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("database_type")]
        public string DatabaseType { get; set; } = "mysql";

        /// <summary>
      /// Database server host
        /// </summary>
   [MaxLength(255)]
        [Column("server_host")]
        public string ServerHost { get; set; } = string.Empty;

        /// <summary>
        /// Database port
    /// </summary>
        [Column("server_port")]
        public int ServerPort { get; set; } = 3306;

        /// <summary>
        /// Database name
        /// </summary>
        [Required]
    [MaxLength(255)]
   [Column("database_name")]
        public string DatabaseName { get; set; } = string.Empty;

        /// <summary>
    /// Database username
        /// </summary>
        [Required]
[MaxLength(255)]
        [Column("database_username")]
 public string DatabaseUsername { get; set; } = string.Empty;

   /// <summary>
    /// Is this database configuration active?
     /// </summary>
        [Column("is_active")]
     public bool IsActive { get; set; } = true;

        /// <summary>
        /// Last connection test timestamp
        /// </summary>
     [Column("last_tested_at")]
        public DateTime? LastTestedAt { get; set; }

        /// <summary>
        /// Connection test status: success, failed, pending
  /// </summary>
      [MaxLength(50)
        , Column("test_status")]
        public string TestStatus { get; set; } = "pending";

        /// <summary>
   /// Last connection test error (if any)
      /// </summary>
  [Column("test_error")]
     public string? TestError { get; set; }

    /// <summary>
    /// Is database schema initialized?
        /// </summary>
    [Column("schema_initialized")]
        public bool SchemaInitialized { get; set; } = false;

        /// <summary>
    /// Schema initialization date
        /// </summary>
 [Column("schema_initialized_at")]
        public DateTime? SchemaInitializedAt { get; set; }

        /// <summary>
 /// Database version for migration tracking
        /// </summary>
    [MaxLength(50)]
  [Column("schema_version")]
        public string SchemaVersion { get; set; } = "1.0.0";

        /// <summary>
        /// Configuration created date
     /// </summary>
        [Column("created_at")]
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
 /// Last updated date
     /// </summary>
        [Column("updated_at")]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

 /// <summary>
      /// Who created this configuration
 /// </summary>
  [MaxLength(255)]
     [Column("created_by")]
  public string CreatedBy { get; set; } = "system";

   /// <summary>
        /// Additional settings (JSON)
    /// </summary>
        [Column("settings_json")]
        public string? SettingsJson { get; set; }

        /// <summary>
  /// Notes/comments about this database
        /// </summary>
   [Column("notes")]
      public string? Notes { get; set; }

   // Navigation property
        [ForeignKey("UserId")]
   public virtual users? User { get; set; }
    }

    /// <summary>
    /// DTO for creating/updating private cloud database
    /// </summary>
    public class PrivateCloudDatabaseDto
    {
     [Required]
 public string UserEmail { get; set; } = string.Empty;

        [Required]
        public string DatabaseType { get; set; } = "mysql";

        [Required]
      public string ServerHost { get; set; } = string.Empty;

 [Required]
  public int ServerPort { get; set; } = 3306;

    [Required]
        public string DatabaseName { get; set; } = string.Empty;

        [Required]
   public string DatabaseUsername { get; set; } = string.Empty;

 [Required]
        public string DatabasePassword { get; set; } = string.Empty;

        public string? Notes { get; set; }
    }

    /// <summary>
    /// Simplified DTO - User provides only connection string
    /// Backend automatically parses and processes
    /// </summary>
    public class SimplePrivateCloudSetupDto
    {
        /// <summary>
        /// Connection string in any format:
    /// - MySQL URI: mysql://user:pass@host:port/db
 /// - Standard: Server=host;Port=port;Database=db;User=user;Password=pass;
        /// </summary>
        [Required(ErrorMessage = "Connection string is required")]
        public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Optional: Database type (auto-detected from connection string if not provided)
        /// Supported: mysql, postgresql, sqlserver
        /// </summary>
        public string? DatabaseType { get; set; }

        /// <summary>
        /// Optional: Notes about this database
        /// </summary>
        public string? Notes { get; set; }
    }

    /// <summary>
    /// DTO for database connection test result
    /// </summary>
    public class DatabaseTestResult
    {
        public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
        public string? Error { get; set; }
        public DateTime TestedAt { get; set; } = DateTime.UtcNow;
        public int ResponseTimeMs { get; set; }
        public string? ServerVersion { get; set; }
      public bool SchemaExists { get; set; }
        public List<string> MissingTables { get; set; } = new();
    }
}
