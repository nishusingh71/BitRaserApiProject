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
        Task<bool> SetupPrivateDatabaseFromConnectionStringAsync(string userEmail, string connectionString, string? databaseType = null, string? notes = null);
    Task<DatabaseTestResult> TestDatabaseConnectionAsync(string userEmail);
   Task<bool> InitializeDatabaseSchemaAsync(string userEmail);
 Task<string> GetConnectionStringAsync(string userEmail);
   Task<bool> IsPrivateCloudUserAsync(string userEmail);
   Task<DbContext> GetUserDbContextAsync(string userEmail);
     Task<SchemaValidationResult> ValidateDatabaseSchemaAsync(string userEmail);
    Task<List<string>> GetRequiredTablesAsync();
  Task<bool> DeletePrivateDatabaseConfigAsync(string userEmail);
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
  _logger.LogInformation("=== SETUP PRIVATE DATABASE START ===");
     _logger.LogInformation("User Email: {Email}", dto.UserEmail);
        _logger.LogInformation("Database Type: {Type}", dto.DatabaseType);
      _logger.LogInformation("Server Host: {Host}", dto.ServerHost);
 _logger.LogInformation("Server Port: {Port}", dto.ServerPort);
   _logger.LogInformation("Database Name: {Database}", dto.DatabaseName);
       _logger.LogInformation("Username: {Username}", dto.DatabaseUsername);

  // Check if user exists and is marked as private cloud
  var user = await _mainContext.Users
            .FirstOrDefaultAsync(u => u.user_email == dto.UserEmail);

     if (user == null)
        {
      _logger.LogError("❌ ERROR: User {Email} NOT FOUND in database", dto.UserEmail);
        return false;
 }

   _logger.LogInformation("✅ User found: ID={Id}, Email={Email}", user.user_id, user.user_email);

  if (user.is_private_cloud != true)
            {
   _logger.LogError("❌ ERROR: User {Email} is_private_cloud={Flag} (NOT TRUE)", 
    dto.UserEmail, user.is_private_cloud);
      return false;
}

          _logger.LogInformation("✅ User is marked as private cloud user");


// Build connection string
     _logger.LogInformation("Building connection string...");
    var connectionString = BuildConnectionString(dto);
  _logger.LogInformation("✅ Connection string built successfully (length: {Length})", connectionString.Length);

   // Test connection first
    _logger.LogInformation("Testing database connection...");
       var testResult = await TestConnectionAsync(connectionString, dto.DatabaseType);
  
      _logger.LogInformation("Connection test result: Success={Success}, Message={Message}, Error={Error}", 
   testResult.Success, testResult.Message, testResult.Error ?? "None");

 if (!testResult.Success)
    {
              _logger.LogError("❌ ERROR: Database connection test FAILED");
          _logger.LogError("   Error details: {Error}", testResult.Error);
 _logger.LogError(" Connection string (masked): Server={Host};Port={Port};Database={Database}", 
       dto.ServerHost, dto.ServerPort, dto.DatabaseName);
      return false;
    }

     _logger.LogInformation("✅ Connection test PASSED");

 // Check if configuration already exists
         _logger.LogInformation("Checking for existing configuration...");
   var existingConfig = await _mainContext.Set<PrivateCloudDatabase>()
      .FirstOrDefaultAsync(db => db.UserEmail == dto.UserEmail);

     if (existingConfig != null)
 {
          // Update existing configuration
   _logger.LogInformation("Updating existing configuration (ConfigId: {Id})", existingConfig.ConfigId);
  existingConfig.ConnectionString = EncryptConnectionString(connectionString);
   existingConfig.DatabaseType = dto.DatabaseType;
          existingConfig.ServerHost = dto.ServerHost;
   existingConfig.ServerPort = dto.ServerPort;
    existingConfig.DatabaseName = dto.DatabaseName;
  existingConfig.DatabaseUsername = dto.DatabaseUsername;
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
         _logger.LogInformation("Creating new configuration");
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
   Notes = dto.Notes,
 TestStatus = "success",
LastTestedAt = DateTime.UtcNow,
      IsActive = true,
   CreatedBy = dto.UserEmail
  };

    await _mainContext.Set<PrivateCloudDatabase>().AddAsync(newConfig);
      }

  _logger.LogInformation("Saving changes to database...");
     await _mainContext.SaveChangesAsync();

   _logger.LogInformation("✅ === SETUP COMPLETE === Configuration saved for {Email}", dto.UserEmail);
      return true;
            }
    catch (Exception ex)
   {
   _logger.LogError(ex, "❌ === EXCEPTION IN SETUP === Error: {Message}", ex.Message);
      _logger.LogError("Stack Trace: {StackTrace}", ex.StackTrace);
     _logger.LogError("Inner Exception: {InnerError}", ex.InnerException?.Message ?? "None");
  return false;
  }
  }

        /// <summary>
        /// Setup private database from connection string only (SIMPLIFIED)
        /// Auto-parses connection string and extracts components
        /// </summary>
 public async Task<bool> SetupPrivateDatabaseFromConnectionStringAsync(
       string userEmail,
string connectionString,
            string? databaseType = null,
     string? notes = null)
   {
            try
        {
        _logger.LogInformation("=== SIMPLIFIED SETUP START ===");
  _logger.LogInformation("User Email: {Email}", userEmail);
  _logger.LogInformation("Connection String Length: {Length}", connectionString.Length);
    _logger.LogInformation("Database Type: {Type}", databaseType ?? "Auto-detect");

        // Check if user exists and is marked as private cloud
       var user = await _mainContext.Users
       .FirstOrDefaultAsync(u => u.user_email == userEmail);

      if (user == null)
    {
          _logger.LogError("❌ ERROR: User {Email} NOT FOUND in database", userEmail);
return false;
       }

     if (user.is_private_cloud != true)
            {
  _logger.LogError("❌ ERROR: User {Email} is_private_cloud={Flag} (NOT TRUE)",
   userEmail, user.is_private_cloud);
      return false;
      }

      _logger.LogInformation("✅ User is marked as private cloud user");

  // Auto-detect database type if not provided
    if (string.IsNullOrEmpty(databaseType))
   {
           databaseType = DetectDatabaseType(connectionString);
    _logger.LogInformation("🔍 Auto-detected database type: {Type}", databaseType);
         }

       // Parse connection string and extract components
      _logger.LogInformation("📋 Parsing connection string...");
       
     ConnectionStringComponents parsedComponents;
        try
        {
  parsedComponents = ParseConnectionStringComponents(connectionString, databaseType);
            _logger.LogInformation("✅ Parsed components:");
            _logger.LogInformation("   Host: {Host}", parsedComponents.Host);
     _logger.LogInformation("   Port: {Port}", parsedComponents.Port);
     _logger.LogInformation("   Database: {Database}", parsedComponents.Database);
            _logger.LogInformation("   Username: {Username}", parsedComponents.Username);
        }
        catch (Exception parseEx)
        {
      _logger.LogError(parseEx, "❌ Failed to parse connection string");
     _logger.LogError("   Connection string format: {Format}", connectionString.Substring(0, Math.Min(20, connectionString.Length)) + "...");
      throw;
        }

         // Test connection
        _logger.LogInformation("🔌 Testing database connection...");
      
        DatabaseTestResult testResult;
        try
        {
   testResult = await TestConnectionAsync(parsedComponents.StandardConnectionString, databaseType);
            _logger.LogInformation("Connection test result: Success={Success}, Message={Message}",
        testResult.Success, testResult.Message);
      }
        catch (Exception testEx)
        {
            _logger.LogError(testEx, "❌ Exception during connection test");
            return false;
        }

        if (!testResult.Success)
    {
     _logger.LogError("❌ ERROR: Database connection test FAILED");
     _logger.LogError("   Error details: {Error}", testResult.Error);
return false;
     }

     _logger.LogInformation("✅ Connection test PASSED");

      // Check if configuration already exists
   var existingConfig = await _mainContext.Set<PrivateCloudDatabase>()
  .FirstOrDefaultAsync(db => db.UserEmail == userEmail);

         if (existingConfig != null)
     {
   // Update existing
    _logger.LogInformation("📝 Updating existing configuration (ID: {Id})", existingConfig.ConfigId);
  
            try
         {
    existingConfig.ConnectionString = EncryptConnectionString(parsedComponents.StandardConnectionString);
   existingConfig.DatabaseType = databaseType;
         existingConfig.ServerHost = parsedComponents.Host;
   existingConfig.ServerPort = parsedComponents.Port;
          existingConfig.DatabaseName = parsedComponents.Database;
          existingConfig.DatabaseUsername = parsedComponents.Username;
         existingConfig.Notes = notes;
    existingConfig.TestStatus = "success";
      existingConfig.LastTestedAt = DateTime.UtcNow;
          existingConfig.UpdatedAt = DateTime.UtcNow;
     existingConfig.IsActive = true;

       _mainContext.Entry(existingConfig).State = EntityState.Modified;
  _logger.LogInformation("✅ Configuration updated in memory");
    }
catch (Exception updateEx)
{
      _logger.LogError(updateEx, "❌ Failed to update existing configuration");
      throw;
            }
            }
     else
       {
     // Create new
 _logger.LogInformation("📝 Creating new configuration");
            
            try
            {
          var newConfig = new PrivateCloudDatabase
        {
          UserId = user.user_id,
           UserEmail = userEmail,
             ConnectionString = EncryptConnectionString(parsedComponents.StandardConnectionString),
            DatabaseType = databaseType,
          ServerHost = parsedComponents.Host,
            ServerPort = parsedComponents.Port,
          DatabaseName = parsedComponents.Database,
      DatabaseUsername = parsedComponents.Username,
   Notes = notes,
   TestStatus = "success",
           LastTestedAt = DateTime.UtcNow,
      IsActive = true,
         CreatedBy = userEmail
         };

    await _mainContext.Set<PrivateCloudDatabase>().AddAsync(newConfig);
      _logger.LogInformation("✅ New configuration added to context");
            }
      catch (Exception createEx)
            {
     _logger.LogError(createEx, "❌ Failed to create new configuration");
        throw;
     }
      }

        // Save changes
        try
   {
        _logger.LogInformation("💾 Saving changes to database...");
            var changesCount = await _mainContext.SaveChangesAsync();
            _logger.LogInformation("✅ Saved {Count} changes to database", changesCount);
        }
        catch (Exception saveEx)
        {
  _logger.LogError(saveEx, "❌ Failed to save changes to database");
          _logger.LogError("   Inner Exception: {Inner}", saveEx.InnerException?.Message ?? "None");
            throw;
        }

            _logger.LogInformation("✅ === SIMPLIFIED SETUP COMPLETE ===");
      return true;
    }
 catch (FormatException formatEx)
  {
        _logger.LogError(formatEx, "❌ Invalid connection string format");
            _logger.LogError("   Format Error: {Message}", formatEx.Message);
       throw;
 }
    catch (Exception ex)
 {
      _logger.LogError(ex, "❌ === EXCEPTION IN SIMPLIFIED SETUP ===");
 _logger.LogError("   Exception Type: {Type}", ex.GetType().Name);
     _logger.LogError("   Message: {Message}", ex.Message);
      _logger.LogError("   Stack Trace: {StackTrace}", ex.StackTrace);
      _logger.LogError("   Inner Exception: {Inner}", ex.InnerException?.Message ?? "None");
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

        /// <summary>
        /// Delete/deactivate private database configuration
      /// </summary>
        public async Task<bool> DeletePrivateDatabaseConfigAsync(string userEmail)
        {
      try
            {
                _logger.LogInformation("🗑️ Deleting private database configuration for {Email}", userEmail);

        var config = await GetUserPrivateDatabaseAsync(userEmail);
       if (config == null)
    {
                _logger.LogWarning("⚠️ No configuration found for {Email}", userEmail);
          return false;
    }

       // Remove from cache if exists
                if (_dbContextCache.ContainsKey(userEmail))
        {
var cachedContext = _dbContextCache[userEmail];
await cachedContext.DisposeAsync();
     _dbContextCache.Remove(userEmail);
         _logger.LogInformation("✅ Removed cached context for {Email}", userEmail);
    }

       // Soft delete - set IsActive to false
          config.IsActive = false;
          config.UpdatedAt = DateTime.UtcNow;

          _mainContext.Entry(config).State = EntityState.Modified;
             await _mainContext.SaveChangesAsync();

                _logger.LogInformation("✅ Configuration deleted/deactivated for {Email}", userEmail);
     return true;
      }
    catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error deleting configuration for {Email}", userEmail);
 return false;
            }
        }

        #region Private Helper Methods

   /// <summary>
        /// Auto-detect database type from connection string
  /// </summary>
  private string DetectDatabaseType(string connectionString)
  {
      if (connectionString.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase) ||
       connectionString.StartsWith("mariadb://", StringComparison.OrdinalIgnoreCase) ||
  connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
      {
   return "mysql";
        }
    else if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
       connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
 {
     return "postgresql";
        }
 else if (connectionString.Contains("Data Source", StringComparison.OrdinalIgnoreCase) ||
      connectionString.Contains("Initial Catalog", StringComparison.OrdinalIgnoreCase))
        {
 return "sqlserver";
   }

return "mysql"; // Default to MySQL
  }

        /// <summary>
        /// Parse connection string and extract components
     /// </summary>
        private ConnectionStringComponents ParseConnectionStringComponents(string connectionString, string databaseType)
        {
   var components = new ConnectionStringComponents();

     // Check if it's a URI format
        if (connectionString.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase) ||
    connectionString.StartsWith("mariadb://", StringComparison.OrdinalIgnoreCase))
       {
       // Parse MySQL URI
 var uriWithoutProtocol = connectionString.Replace("mysql://", "").Replace("mariadb://", "");
  var parts = uriWithoutProtocol.Split('@');

      if (parts.Length != 2)
       {
    throw new FormatException("Invalid MySQL URI format. Expected: mysql://user:pass@host:port/database");
    }

         // Parse credentials
      var credentials = parts[0].Split(':');
       components.Username = credentials[0];
             var password = credentials.Length > 1 ? credentials[1] : "";

         // Parse host, port, database
      var hostAndDb = parts[1].Split('?')[0]; // Remove query parameters if any
   var hostPortDb = hostAndDb.Split('/');

        if (hostPortDb.Length > 0)
       {
       var hostPort = hostPortDb[0].Split(':');
      components.Host = hostPort[0];
     components.Port = hostPort.Length > 1 && int.TryParse(hostPort[1], out var port) ? port : 3306;
      }

  if (hostPortDb.Length > 1)
  {
      components.Database = hostPortDb[1];
       }

       // Build standard connection string
      components.StandardConnectionString = 
      $"Server={components.Host};Port={components.Port};Database={components.Database};User={components.Username};Password={password};AllowUserVariables=true;SslMode=Required;";
   }
      else
       {
// Parse standard format (Server=...;Database=...;)
   var keyValues = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
            
       foreach (var kv in keyValues)
   {
   var parts = kv.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
  if (parts.Length == 2)
    {
   var key = parts[0].Trim().ToLower();
        var value = parts[1].Trim();

         switch (key)
  {
     case "server":
       case "host":
      components.Host = value;
    break;
 case "port":
    int.TryParse(value, out var port);
        components.Port = port > 0 ? port : 3306;
      break;
    case "database":
              case "initial catalog":
      components.Database = value;
            break;
  case "user":
      case "user id":
case "userid":
      case "username":
       components.Username = value;
         break;
            }
     }
  }

    // Use as-is if standard format (already has password)
       components.StandardConnectionString = connectionString;
        
        // Ensure SSL mode for MySQL
  if (databaseType?.ToLower() == "mysql" && !connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
     {
      components.StandardConnectionString += ";SslMode=Required;";
    }
 }

    return components;
 }

        private string BuildConnectionString(PrivateCloudDatabaseDto dto)
    {
       return dto.DatabaseType.ToLower() switch
     {
         "mysql" => $"server={dto.ServerHost};port={dto.ServerPort};database={dto.DatabaseName};user={dto.DatabaseUsername};password={dto.DatabasePassword};AllowUserVariables=true;SslMode=Required;",
  "postgresql" => $"Host={dto.ServerHost};Port={dto.ServerPort};Database={dto.DatabaseName};Username={dto.DatabaseUsername};Password={dto.DatabasePassword};SslMode=Require;",
 "sqlserver" => $"Server={dto.ServerHost},{dto.ServerPort};Database={dto.DatabaseName};User Id={dto.DatabaseUsername};Password={dto.DatabasePassword};Encrypt=True;TrustServerCertificate=True;",
     _ => throw new NotSupportedException($"Database type {dto.DatabaseType} not supported")
        };
 }

        private async Task<DatabaseTestResult> TestConnectionAsync(string connectionString, string databaseType)
     {
var startTime = DateTime.UtcNow;
     var result = new DatabaseTestResult();

    try
       {
    _logger.LogInformation("🔌 Starting connection test...");
   _logger.LogInformation("   Database Type: {Type}", databaseType);
      _logger.LogInformation("   Connection String Length: {Length}", connectionString.Length);
 
            // Log connection string (masked password)
         var maskedConnStr = System.Text.RegularExpressions.Regex.Replace(
    connectionString, 
   @"(password|pwd)=([^;]*)",
  "$1=***MASKED***",
      System.Text.RegularExpressions.RegexOptions.IgnoreCase);
   _logger.LogInformation("   Connection String: {ConnStr}", maskedConnStr);

 if (databaseType.ToLower() == "mysql")
{
         _logger.LogInformation("   Attempting MySQL connection...");
     using var connection = new MySqlConnection(connectionString);
      
         _logger.LogInformation("   Opening connection...");
await connection.OpenAsync();
   _logger.LogInformation("   ✅ Connection opened successfully!");

   result.Success = true;
        result.Message = "Connection successful";
    result.ServerVersion = connection.ServerVersion;
  result.ResponseTimeMs = (int)(DateTime.UtcNow - startTime).TotalMilliseconds;

            _logger.LogInformation("   Server Version: {Version}", connection.ServerVersion);
      _logger.LogInformation("   Database: {Database}", connection.Database);
       _logger.LogInformation("   Response Time: {Ms} ms", result.ResponseTimeMs);

       // Check if required tables exist
   _logger.LogInformation("   Checking existing tables...");
       var tables = await GetExistingTablesAsync(connection);
         var requiredTables = _tableSchemas.Keys.ToList();
        result.MissingTables = requiredTables.Where(t => !tables.Contains(t)).ToList();
 result.SchemaExists = result.MissingTables.Count == 0;

       _logger.LogInformation("   Tables found: {Count}", tables.Count);
    _logger.LogInformation("   Required tables: {Count}", requiredTables.Count);
   _logger.LogInformation("   Missing tables: {Count}", result.MissingTables.Count);

       await connection.CloseAsync();
       _logger.LogInformation("   ✅ Connection closed successfully");
     }
    else
        {
            _logger.LogWarning("   ❌ Unsupported database type: {Type}", databaseType);
  result.Success = false;
    result.Message = $"Database type {databaseType} not yet supported for testing";
    }
        }
   catch (MySqlException mysqlEx)
            {
       _logger.LogError("   ❌ MySQL EXCEPTION!");
       _logger.LogError("   Error Number: {Number}", mysqlEx.Number);
        _logger.LogError("   Error Message: {Message}", mysqlEx.Message);
       _logger.LogError("   SQL State: {State}", mysqlEx.SqlState);
            
 // Specific error handling
            switch (mysqlEx.Number)
   {
        case 1045: // Access denied
        _logger.LogError("   💡 Access denied - Check username/password");
           result.Error = $"Access denied: {mysqlEx.Message}";
  break;
       case 2003: // Can't connect
            _logger.LogError("💡 Cannot connect to server - Check host/port/firewall");
        result.Error = $"Cannot connect: {mysqlEx.Message}";
   break;
    case 1049: // Unknown database
      _logger.LogError("   💡 Database doesn't exist - Check database name");
            result.Error = $"Database not found: {mysqlEx.Message}";
       break;
         case 2026: // SSL error
            _logger.LogError(" 💡 SSL connection error - TiDB requires SSL");
      result.Error = $"SSL error: {mysqlEx.Message}";
        break;
     default:
         _logger.LogError("   💡 MySQL Error #{Number}: {Message}", mysqlEx.Number, mysqlEx.Message);
       result.Error = mysqlEx.Message;
       break;
    }
   
   result.Success = false;
   result.Message = "Connection failed";
   }
            catch (Exception ex)
  {
            _logger.LogError(ex, "   ❌ GENERAL EXCEPTION during connection test");
   _logger.LogError("   Exception Type: {Type}", ex.GetType().Name);
            _logger.LogError("   Message: {Message}", ex.Message);
       if (ex.InnerException != null)
    {
    _logger.LogError("   Inner Exception: {Inner}", ex.InnerException.Message);
  }
   
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

    /// <summary>
    /// Helper class to store parsed connection string components
    /// </summary>
    internal class ConnectionStringComponents
    {
        public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 3306;
   public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string StandardConnectionString { get; set; } = string.Empty;
    }
}
