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
    /// <summary>
    /// Primary key
    /// </summary>
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
        /// Encrypted connection string
        /// </summary>
        [Required]
        [Column("connection_string")]
    public string ConnectionString { get; set; } = string.Empty;

        /// <summary>
        /// Database type (mysql, postgresql, sqlserver)
        /// </summary>
        [Required]
        [MaxLength(50)]
        [Column("database_type")]
        public string DatabaseType { get; set; } = "mysql";

        /// <summary>
        /// Server host/IP
        /// </summary>
        [MaxLength(255)]
        [Column("server_host")]
        public string? ServerHost { get; set; }

        /// <summary>
        /// Server port
        /// </summary>
        [Column("server_port")]
        public int ServerPort { get; set; } = 3306;

        /// <summary>
        /// Database name
        /// </summary>
        [MaxLength(255)]
        [Column("database_name")]
        public string? DatabaseName { get; set; }

        /// <summary>
        /// Database username
        /// </summary>
        [MaxLength(255)]
        [Column("database_username")]
        public string? DatabaseUsername { get; set; }

    /// <summary>
    /// JSON object storing which tables user wants in private cloud
  /// Example: {"audit_reports": true, "subuser": true, "machines": false}
    /// </summary>
  [Column("selected_tables", TypeName = "json")]
    public string? SelectedTables { get; set; }

        /// <summary>
        /// Connection test status
        /// </summary>
        [MaxLength(50)]
        [Column("test_status")]
        public string? TestStatus { get; set; }

        /// <summary>
        /// Last connection test timestamp
        /// </summary>
        [Column("last_tested_at")]
        public DateTime? LastTestedAt { get; set; }

        /// <summary>
  /// Is schema initialized
    /// </summary>
    [Column("schema_initialized")]
        public bool SchemaInitialized { get; set; } = false;

        /// <summary>
        /// Schema initialization timestamp
        /// </summary>
 [Column("schema_initialized_at")]
        public DateTime? SchemaInitializedAt { get; set; }

        /// <summary>
        /// Is active configuration
/// </summary>
    [Column("is_active")]
public bool IsActive { get; set; } = true;

        /// <summary>
 /// Optional notes
    /// </summary>
    [MaxLength(500)]
    [Column("notes")]
    public string? Notes { get; set; }

 /// <summary>
    /// Created timestamp
  /// </summary>
    [Column("created_at")]
 public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
    /// Created by user email
    /// </summary>
    [MaxLength(255)]
  [Column("created_by")]
public string? CreatedBy { get; set; }

        /// <summary>
        /// Updated timestamp
        /// </summary>
        [Column("updated_at")]
        public DateTime? UpdatedAt { get; set; }

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

     /// <summary>
        /// ✅ NEW: JSON string containing user's table selection
        /// Example: {"AuditReports": true, "subuser": true, "Roles": true, "SubuserRoles": true, "machines": false}
        /// </summary>
        public string? SelectedTables { get; set; }

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
