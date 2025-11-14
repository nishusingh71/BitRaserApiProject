using Microsoft.EntityFrameworkCore;
using MySql.Data.MySqlClient;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using BitRaserApiProject.Models;

namespace BitRaserApiProject.Services
{
    public interface IPrivateCloudService
  {
        Task<PrivateCloudDatabase?> GetUserPrivateDatabaseAsync(string userEmail);
Task<bool> SetupPrivateDatabaseAsync(PrivateCloudDatabaseDto dto);
    Task<DatabaseTestResult> TestDatabaseConnectionAsync(string userEmail);
   Task<bool> InitializeDatabaseSchemaAsync(string userEmail);
 Task<string> GetConnectionStringAsync(string userEmail);
   Task<bool> IsPrivateCloudUserAsync(string userEmail);
   Task<DbContext> GetUserDbContextAsync(string userEmail);
     Task<SchemaValidationResult> ValidateDatabaseSchemaAsync(string userEmail);
        Task<List<string>> GetRequiredTablesAsync();
    }

    public class PrivateCloudService : IPrivateCloudService
    {
 private readonly ApplicationDbContext _mainContext;
        private readonly ILogger<PrivateCloudService> _logger;
        private readonly IConfiguration _configuration;
    private static readonly Dictionary<string, DbContext> _dbContextCache = new();

        // ✅ Complete table schema with relationships
    private readonly Dictionary<string, string> _tableSchemas = new()
        {
      // Users table (Parent)
      ["users"] = @"
CREATE TABLE IF NOT EXISTS `users` (
    `user_id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_name` VARCHAR(255) NOT NULL,
    `user_email` VARCHAR(255) NOT NULL UNIQUE,
    `user_password` VARCHAR(255) NOT NULL,
    `hash_password` VARCHAR(255),
    `phone_number` VARCHAR(20),
    `department` VARCHAR(100),
    `user_group` VARCHAR(100),
    `user_role` VARCHAR(50),
    `license_allocation` INT DEFAULT 0,
    `status` VARCHAR(50) DEFAULT 'active',
    `timezone` VARCHAR(100),
    `domain` VARCHAR(255),
    `organization_name` VARCHAR(255),
    `is_domain_admin` BOOLEAN DEFAULT FALSE,
    `is_private_cloud` BOOLEAN DEFAULT FALSE,
    `private_api` BOOLEAN DEFAULT FALSE,
 `payment_details_json` JSON,
    `license_details_json` JSON,
    `last_login` TIMESTAMP NULL,
    `last_logout` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_user_email (`user_email`),
    INDEX idx_status (`status`),
    INDEX idx_organization (`organization_name`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;'",

            // Subusers table (depends on users)
      ["subuser"] = @"
CREATE TABLE IF NOT EXISTS `subuser` (
    `subuser_id` INT AUTO_INCREMENT PRIMARY KEY,
    `subuser_email` VARCHAR(255) NOT NULL UNIQUE,
    `subuser_password` VARCHAR(255) NOT NULL,
    `subuser_username` VARCHAR(100),
    `user_email` VARCHAR(255) NOT NULL,
    `superuser_id` INT,
    `Name` VARCHAR(100),
    `Phone` VARCHAR(20),
    `Department` VARCHAR(100),
    `Role` VARCHAR(50) NOT NULL DEFAULT 'subuser',
 `PermissionsJson` JSON,
    `AssignedMachines` INT DEFAULT 0,
    `MaxMachines` INT DEFAULT 5,
 `MachineIdsJson` JSON,
    `LicenseIdsJson` JSON,
    `GroupId` INT,
    `subuser_group` VARCHAR(100),
  `license_allocation` INT DEFAULT 0,
    `status` VARCHAR(50) DEFAULT 'active',
    `timezone` VARCHAR(100),
    `domain` VARCHAR(255),
 `organization_name` VARCHAR(255),
    `IsEmailVerified` BOOLEAN DEFAULT FALSE,
    `CanCreateSubusers` BOOLEAN DEFAULT FALSE,
    `CanViewReports` BOOLEAN DEFAULT TRUE,
    `CanManageMachines` BOOLEAN DEFAULT FALSE,
    `CanAssignLicenses` BOOLEAN DEFAULT FALSE,
    `EmailNotifications` BOOLEAN DEFAULT TRUE,
    `SystemAlerts` BOOLEAN DEFAULT TRUE,
    `LastLoginIp` VARCHAR(500),
    `last_login` TIMESTAMP NULL,
    `last_logout` TIMESTAMP NULL,
    `FailedLoginAttempts` INT DEFAULT 0,
    `LockedUntil` TIMESTAMP NULL,
    `CreatedBy` INT NOT NULL,
    `CreatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `UpdatedAt` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    `UpdatedBy` INT,
    `Notes` VARCHAR(500),
    INDEX idx_subuser_email (`subuser_email`),
    INDEX idx_user_email (`user_email`),
    INDEX idx_superuser (`superuser_id`),
    INDEX idx_status (`status`),
    CONSTRAINT fk_subuser_user FOREIGN KEY (`user_email`) REFERENCES `users`(`user_email`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

            // Machines table (depends on users)
  ["machines"] = @"
CREATE TABLE IF NOT EXISTS `machines` (
    `fingerprint_hash` VARCHAR(255) PRIMARY KEY,
    `mac_address` VARCHAR(255) NOT NULL,
    `physical_drive_id` VARCHAR(255) NOT NULL,
    `cpu_id` VARCHAR(255) NOT NULL,
    `bios_serial` VARCHAR(255) NOT NULL,
    `os_version` VARCHAR(255),
    `user_email` VARCHAR(255),
    `subuser_email` VARCHAR(255),
    `license_details_json` JSON,
    `machine_details_json` JSON,
    `license_activation_date` TIMESTAMP NULL,
    `license_days_valid` INT DEFAULT 0,
    `license_activated` BOOLEAN DEFAULT FALSE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_mac_address (`mac_address`),
    INDEX idx_user_email (`user_email`),
    INDEX idx_subuser_email (`subuser_email`),
    CONSTRAINT fk_machine_user FOREIGN KEY (`user_email`) REFERENCES `users`(`user_email`) ON DELETE SET NULL,
    CONSTRAINT fk_machine_subuser FOREIGN KEY (`subuser_email`) REFERENCES `subuser`(`subuser_email`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

   // Audit Reports table (depends on users)
         ["audit_reports"] = @"
CREATE TABLE IF NOT EXISTS `audit_reports` (
    `report_id` INT AUTO_INCREMENT PRIMARY KEY,
    `client_email` VARCHAR(255) NOT NULL,
  `report_name` VARCHAR(255) NOT NULL,
    `erasure_method` VARCHAR(255) NOT NULL,
    `report_datetime` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `report_details_json` JSON NOT NULL,
    `synced` BOOLEAN DEFAULT FALSE,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_client_email (`client_email`),
    INDEX idx_report_date (`report_datetime`),
    INDEX idx_synced (`synced`),
    CONSTRAINT fk_report_user FOREIGN KEY (`client_email`) REFERENCES `users`(`user_email`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

      // Sessions table (depends on users)
    ["sessions"] = @"
CREATE TABLE IF NOT EXISTS `sessions` (
    `session_id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_email` VARCHAR(255) NOT NULL,
  `ip_address` VARCHAR(45),
    `device_info` VARCHAR(1000),
    `session_status` VARCHAR(50) NOT NULL DEFAULT 'active',
    `login_time` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `logout_time` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
`updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_user_email (`user_email`),
    INDEX idx_status (`session_status`),
    INDEX idx_login_time (`login_time`),
    CONSTRAINT fk_session_user FOREIGN KEY (`user_email`) REFERENCES `users`(`user_email`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

            // Logs table (depends on users)
         ["logs"] = @"
CREATE TABLE IF NOT EXISTS `logs` (
    `log_id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_email` VARCHAR(255),
    `log_level` VARCHAR(50) NOT NULL,
    `log_message` VARCHAR(2000) NOT NULL,
    `log_details_json` JSON,
  `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user_email (`user_email`),
  INDEX idx_log_level (`log_level`),
    INDEX idx_created_at (`created_at`),
    CONSTRAINT fk_log_user FOREIGN KEY (`user_email`) REFERENCES `users`(`user_email`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

      // Commands table (depends on users)
         ["commands"] = @"
CREATE TABLE IF NOT EXISTS `commands` (
    `Command_id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_email` VARCHAR(255),
    `command_text` VARCHAR(2000) NOT NULL,
  `command_json` JSON,
    `command_status` VARCHAR(100) DEFAULT 'pending',
    `issued_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `executed_at` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    INDEX idx_user_email (`user_email`),
    INDEX idx_status (`command_status`),
    CONSTRAINT fk_command_user FOREIGN KEY (`user_email`) REFERENCES `users`(`user_email`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

  // Groups table (independent)
            ["groups"] = @"
CREATE TABLE IF NOT EXISTS `groups` (
    `group_id` INT AUTO_INCREMENT PRIMARY KEY,
    `name` VARCHAR(100) NOT NULL,
    `description` VARCHAR(500),
    `total_users` INT DEFAULT 0,
    `total_licenses` INT DEFAULT 0,
    `status` VARCHAR(50) DEFAULT 'active',
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_name (`name`),
    INDEX idx_status (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;"
        };

        public PrivateCloudService(
     ApplicationDbContext mainContext,
  ILogger<PrivateCloudService> logger,
            IConfiguration configuration)
        {
     _mainContext = mainContext;
  _logger = logger;
    _configuration = configuration;
   }

 /// <summary>
/// Get private database configuration for user
        /// </summary>
        public async Task<PrivateCloudDatabase?> GetUserPrivateDatabaseAsync(string userEmail)
        {
  return await _mainContext.Set<PrivateCloudDatabase>()
     .FirstOrDefaultAsync(db => db.UserEmail == userEmail && db.IsActive);
   }

      /// <summary>
        /// Check if user is private cloud user
      /// </summary>
 public async Task<bool> IsPrivateCloudUserAsync(string userEmail)
        {
  var user = await _mainContext.Users
       .FirstOrDefaultAsync(u => u.user_email == userEmail);

 return user?.is_private_cloud == true;
 }

        /// <summary>
        /// Setup private database for user
        /// </summary>
    public async Task<bool> SetupPrivateDatabaseAsync(PrivateCloudDatabaseDto dto)
     {
    try
      {
       _logger.LogInformation("Setting up private database for {Email}", dto.UserEmail);

                // Check if user exists and is marked as private cloud
      var user = await _mainContext.Users
         .FirstOrDefaultAsync(u => u.user_email == dto.UserEmail);

    if (user == null)
 {
            _logger.LogError("User {Email} not found", dto.UserEmail);
 return false;
    }

           if (user.is_private_cloud != true)
      {
     _logger.LogError("User {Email} is not marked as private cloud user", dto.UserEmail);
         return false;
}

       // Build connection string
            var connectionString = BuildConnectionString(dto);

        // Test connection first
         var testResult = await TestConnectionAsync(connectionString, dto.DatabaseType);
     if (!testResult.Success)
       {
    _logger.LogError("Database connection test failed: {Error}", testResult.Error);
return false;
     }

   // Check if configuration already exists
       var existingConfig = await _mainContext.Set<PrivateCloudDatabase>()
  .FirstOrDefaultAsync(db => db.UserEmail == dto.UserEmail);

        if (existingConfig != null)
 {
        // Update existing configuration
      existingConfig.ConnectionString = EncryptConnectionString(connectionString);
   existingConfig.DatabaseType = dto.DatabaseType;
         existingConfig.ServerHost = dto.ServerHost;
    existingConfig.ServerPort = dto.ServerPort;
    existingConfig.DatabaseName = dto.DatabaseName;
   existingConfig.DatabaseUsername = dto.DatabaseUsername;
         existingConfig.StorageLimitMb = dto.StorageLimitMb;
     existingConfig.Notes = dto.Notes;
existingConfig.TestStatus = "success";
          existingConfig.LastTestedAt = DateTime.UtcNow;
  existingConfig.UpdatedAt = DateTime.UtcNow;
           existingConfig.IsActive = true;

 _mainContext.Entry(existingConfig).State = EntityState.Modified;
     }
            else
            {
// Create new configuration
  var newConfig = new PrivateCloudDatabase
    {
      UserId = user.user_id,
     UserEmail = dto.UserEmail,
       ConnectionString = EncryptConnectionString(connectionString),
   DatabaseType = dto.DatabaseType,
        ServerHost = dto.ServerHost,
       ServerPort = dto.ServerPort,
      DatabaseName = dto.DatabaseName,
       DatabaseUsername = dto.DatabaseUsername,
 StorageLimitMb = dto.StorageLimitMb,
        Notes = dto.Notes,
          TestStatus = "success",
        LastTestedAt = DateTime.UtcNow,
     IsActive = true,
 CreatedBy = dto.UserEmail
        };

await _mainContext.Set<PrivateCloudDatabase>().AddAsync(newConfig);
  }

    await _mainContext.SaveChangesAsync();

   _logger.LogInformation("Private database configuration saved for {Email}", dto.UserEmail);
     return true;
     }
     catch (Exception ex)
         {
      _logger.LogError(ex, "Error setting up private database for {Email}", dto.UserEmail);
          return false;
          }
        }

  /// <summary>
        /// Test database connection
        /// </summary>
  public async Task<DatabaseTestResult> TestDatabaseConnectionAsync(string userEmail)
        {
            try
{
 var config = await GetUserPrivateDatabaseAsync(userEmail);
           if (config == null)
    {
          return new DatabaseTestResult
          {
   Success = false,
         Message = "Private database configuration not found"
           };
         }

    var connectionString = DecryptConnectionString(config.ConnectionString);
    return await TestConnectionAsync(connectionString, config.DatabaseType);
  }
 catch (Exception ex)
  {
  _logger.LogError(ex, "Error testing database connection for {Email}", userEmail);
       return new DatabaseTestResult
     {
     Success = false,
           Message = "Error testing connection",
  Error = ex.Message
   };
     }
        }

        /// <summary>
        /// Initialize database schema in private database
  /// </summary>
     public async Task<bool> InitializeDatabaseSchemaAsync(string userEmail)
        {
      try
            {
_logger.LogInformation("Initializing database schema for {Email}", userEmail);

            var config = await GetUserPrivateDatabaseAsync(userEmail);
      if (config == null)
         {
       _logger.LogError("Private database configuration not found for {Email}", userEmail);
 return false;
     }

            if (config.SchemaInitialized)
  {
   _logger.LogInformation("Schema already initialized for {Email}", userEmail);
      return true;
       }

     var connectionString = DecryptConnectionString(config.ConnectionString);

   // Create tables in private database with proper order
  var success = await CreateDatabaseSchemaAsync(connectionString, config.DatabaseType);

if (success)
       {
           config.SchemaInitialized = true;
     config.SchemaInitializedAt = DateTime.UtcNow;
         config.UpdatedAt = DateTime.UtcNow;
              await _mainContext.SaveChangesAsync();

      _logger.LogInformation("Database schema initialized successfully for {Email}", userEmail);
      }

    return success;
  }
       catch (Exception ex)
  {
  _logger.LogError(ex, "Error initializing database schema for {Email}", userEmail);
   return false;
  }
}

        /// <summary>
  /// Get connection string for user's private database
        /// </summary>
        public async Task<string> GetConnectionStringAsync(string userEmail)
        {
            var config = await GetUserPrivateDatabaseAsync(userEmail);
  if (config == null)
      {
        throw new InvalidOperationException($"Private database not configured for {userEmail}");
     }

  return DecryptConnectionString(config.ConnectionString);
  }

        /// <summary>
   /// Get DbContext for user's private database
        /// </summary>
  public async Task<DbContext> GetUserDbContextAsync(string userEmail)
      {
    // Check cache first
          if (_dbContextCache.TryGetValue(userEmail, out var cachedContext))
            {
   return cachedContext;
      }

 var connectionString = await GetConnectionStringAsync(userEmail);
 
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
    optionsBuilder.UseMySql(connectionString, 
       new MySqlServerVersion(new Version(8, 0, 21)));

   var context = new ApplicationDbContext(optionsBuilder.Options);

      // Cache the context
   _dbContextCache[userEmail] = context;

return context;
   }

        /// <summary>
    /// Validate database schema
     /// </summary>
        public async Task<SchemaValidationResult> ValidateDatabaseSchemaAsync(string userEmail)
        {
         try
 {
        var config = await GetUserPrivateDatabaseAsync(userEmail);
           if (config == null)
              {
                return new SchemaValidationResult
           {
     IsValid = false,
  Message = "No database configuration found"
            };
     }

            var connectionString = DecryptConnectionString(config.ConnectionString);
    using var connection = new MySqlConnection(connectionString);
      await connection.OpenAsync();

    var existingTables = await GetExistingTablesAsync(connection);
    var requiredTables = _tableSchemas.Keys.ToList();
      var missingTables = requiredTables.Where(t => !existingTables.Contains(t)).ToList();

                await connection.CloseAsync();

       return new SchemaValidationResult
                {
    IsValid = missingTables.Count == 0,
           Message = missingTables.Count == 0 
  ? "All tables exist" 
    : $"Missing tables: {string.Join(", ", missingTables)}",
         ExistingTables = existingTables,
       MissingTables = missingTables,
              RequiredTables = requiredTables
       };
    }
            catch (Exception ex)
   {
                _logger.LogError(ex, "Error validating schema");
        return new SchemaValidationResult
            {
          IsValid = false,
        Message = $"Validation error: {ex.Message}"
    };
            }
    }

        /// <summary>
        /// Get list of required tables
   /// </summary>
        public async Task<List<string>> GetRequiredTablesAsync()
        {
            return await Task.FromResult(_tableSchemas.Keys.ToList());
        }

        #region Private Helper Methods

        private string BuildConnectionString(PrivateCloudDatabaseDto dto)
    {
       return dto.DatabaseType.ToLower() switch
     {
         "mysql" => $"server={dto.ServerHost};port={dto.ServerPort};database={dto.DatabaseName};user={dto.DatabaseUsername};password={dto.DatabasePassword};AllowUserVariables=true;",
  "postgresql" => $"Host={dto.ServerHost};Port={dto.ServerPort};Database={dto.DatabaseName};Username={dto.DatabaseUsername};Password={dto.DatabasePassword};",
 "sqlserver" => $"Server={dto.ServerHost},{dto.ServerPort};Database={dto.DatabaseName};User Id={dto.DatabaseUsername};Password={dto.DatabasePassword};",
     _ => throw new NotSupportedException($"Database type {dto.DatabaseType} not supported")
        };
 }

        private async Task<DatabaseTestResult> TestConnectionAsync(string connectionString, string databaseType)
     {
var startTime = DateTime.UtcNow;
     var result = new DatabaseTestResult();

    try
       {
 if (databaseType.ToLower() == "mysql")
{
     using var connection = new MySqlConnection(connectionString);
await connection.OpenAsync();

   result.Success = true;
        result.Message = "Connection successful";
    result.ServerVersion = connection.ServerVersion;
  result.ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

       // Check if required tables exist
       var tables = await GetExistingTablesAsync(connection);
         var requiredTables = _tableSchemas.Keys.ToList();
        result.MissingTables = requiredTables.Where(t => !tables.Contains(t)).ToList();
 result.SchemaExists = result.MissingTables.Count == 0;

       await connection.CloseAsync();
     }
         else
        {
  result.Success = false;
    result.Message = $"Database type {databaseType} not yet supported for testing";
        }
        }
            catch (Exception ex)
  {
   result.Success = false;
   result.Message = "Connection failed";
       result.Error = ex.Message;
   }

   return result;
        }

        private async Task<List<string>> GetExistingTablesAsync(MySqlConnection connection)
        {
     var tables = new List<string>();
       var command = connection.CreateCommand();
          command.CommandText = "SHOW TABLES";

using var reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
        {
   tables.Add(reader.GetString(0));
       }

 return tables;
        }

        private async Task<bool> CreateDatabaseSchemaAsync(string connectionString, string databaseType)
  {
     try
 {
  if (databaseType.ToLower() != "mysql")
       {
  _logger.LogError("Schema creation not supported for {Type}", databaseType);
  return false;
         }

      using var connection = new MySqlConnection(connectionString);
     await connection.OpenAsync();

        // Create tables in dependency order
           var tableOrder = new[] { "users", "groups", "subuser", "machines", "audit_reports", "sessions", "logs", "commands" };

        foreach (var tableName in tableOrder)
    {
            if (_tableSchemas.TryGetValue(tableName, out var schema))
   {
           _logger.LogInformation("Creating table: {TableName}", tableName);
     
        var command = connection.CreateCommand();
        command.CommandText = schema;
           await command.ExecuteNonQueryAsync();
         
         _logger.LogInformation("✅ Table created: {TableName}", tableName);
    }
      }

      await connection.CloseAsync();

      _logger.LogInformation("Database schema created successfully");
    return true;
    }
  catch (Exception ex)
     {
         _logger.LogError(ex, "Error creating database schema");
        return false;
   }
   }

   private string EncryptConnectionString(string connectionString)
        {
  // Simple encryption - use better encryption in production
      var key = _configuration["Encryption:Key"] ?? "DefaultEncryptionKey12345678901234";
var iv = _configuration["Encryption:IV"] ?? "1234567890123456";

 using var aes = Aes.Create();
      aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
  aes.IV = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));

   var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
    using var ms = new MemoryStream();
     using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
  using (var sw = new StreamWriter(cs))
    {
          sw.Write(connectionString);
      }

  return Convert.ToBase64String(ms.ToArray());
        }

        private string DecryptConnectionString(string encryptedConnectionString)
        {
            var key = _configuration["Encryption:Key"] ?? "DefaultEncryptionKey12345678901234";
    var iv = _configuration["Encryption:IV"] ?? "1234567890123456";

      using var aes = Aes.Create();
      aes.Key = Encoding.UTF8.GetBytes(key.PadRight(32).Substring(0, 32));
            aes.IV = Encoding.UTF8.GetBytes(iv.PadRight(16).Substring(0, 16));

 var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
     using var ms = new MemoryStream(Convert.FromBase64String(encryptedConnectionString));
     using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
 using var sr = new StreamReader(cs);

      return sr.ReadToEnd();
        }

        #endregion
    }

    /// <summary>
    /// Schema validation result
    /// </summary>
    public class SchemaValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<string> ExistingTables { get; set; } = new();
 public List<string> MissingTables { get; set; } = new();
        public List<string> RequiredTables { get; set; } = new();
    }
}
