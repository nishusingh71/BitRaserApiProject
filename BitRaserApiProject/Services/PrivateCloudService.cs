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

        // ✅ Complete table schema - NO FOREIGN KEYS for TiDB compatibility
        private readonly Dictionary<string, string> _tableSchemas = new()
        {
       ["users"] = @"
CREATE TABLE IF NOT EXISTS `users` (
    `user_id` INT AUTO_INCREMENT PRIMARY KEY,
    `user_name` VARCHAR(255) NOT NULL,
    `user_email` VARCHAR(255) NOT NULL UNIQUE,
    `phone_number` VARCHAR(20),
    `department` VARCHAR(100),
 `user_group` VARCHAR(100),
    `user_role` VARCHAR(50),
    `license_allocation` INT DEFAULT 0,
    `status` VARCHAR(50) DEFAULT 'active',
    `activity_status` VARCHAR(50) DEFAULT 'offline',
    `timezone` VARCHAR(100),
 `is_private_cloud` BOOLEAN DEFAULT FALSE,
    `private_api` BOOLEAN DEFAULT FALSE,
    `payment_details_json` JSON,
    `license_details_json` JSON,
    `last_login` TIMESTAMP NULL,
  `last_logout` TIMESTAMP NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_user_email (`user_email`),
    INDEX idx_status (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='Private DB Users - Simplified schema without auth/private-cloud metadata';",

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
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

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
 `activity_status` varchar(50) DEFAULT 'offline',
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
    INDEX idx_status (`status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

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
     `demo_usage_count` int(11) NOT NULL DEFAULT '0',
     `vm_status` longtext NOT NULL,
    `created_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    `updated_at` TIMESTAMP DEFAULT CURRENT_TIMESTAMP ON UPDATE CURRENT_TIMESTAMP,
    INDEX idx_mac_address (`mac_address`),
    INDEX idx_user_email (`user_email`),
    INDEX idx_subuser_email (`subuser_email`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

            ["AuditReports"] = @"
CREATE TABLE IF NOT EXISTS `AuditReports` (
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
    INDEX idx_synced (`synced`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

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
    INDEX idx_login_time (`login_time`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

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
  INDEX idx_created_at (`created_at`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

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
    INDEX idx_status (`command_status`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;",

["Roles"] = @"CREATE TABLE IF NOT EXISTS `Roles` (
  `RoleId` int(11) NOT NULL AUTO_INCREMENT,
  `RoleName` varchar(100) NOT NULL,
  `Description` varchar(500) NOT NULL,
  `HierarchyLevel` int(11) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`RoleId`),
  UNIQUE KEY `uk_RoleName` (`RoleName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",

["SubuserRoles"] = @"CREATE TABLE IF NOT EXISTS `SubuserRoles` (
  `SubuserId` int(11) NOT NULL,
  `RoleId` int(11) NOT NULL,
  `AssignedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `AssignedByEmail` longtext NOT NULL,
  PRIMARY KEY (`SubuserId`, `RoleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",

["UserRoles"] = @"CREATE TABLE IF NOT EXISTS `UserRoles` (
  `UserId` int(11) NOT NULL,
  `RoleId` int(11) NOT NULL,
  `AssignedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `AssignedByEmail` longtext NOT NULL,
  PRIMARY KEY (`UserId`, `RoleId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",

["Routes"] = @"CREATE TABLE IF NOT EXISTS `Routes` (
  `RouteId` int(11) NOT NULL AUTO_INCREMENT,
  `RoutePath` varchar(500) NOT NULL,
  `HttpMethod` varchar(10) NOT NULL,
  `Description` varchar(200) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`RouteId`),
  INDEX `idx_RoutePath` (`RoutePath`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",

["Permissions"] = @"CREATE TABLE IF NOT EXISTS `Permissions` (
  `PermissionId` int(11) NOT NULL AUTO_INCREMENT,
  `PermissionName` varchar(100) NOT NULL,
  `Description` varchar(500) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`PermissionId`),
  UNIQUE KEY `uk_PermissionName` (`PermissionName`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;",

["RolePermissions"] = @"CREATE TABLE IF NOT EXISTS `RolePermissions` (
  `RoleId` int(11) NOT NULL,
  `PermissionId` int(11) NOT NULL,
  PRIMARY KEY (`RoleId`, `PermissionId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;"
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

        public async Task<PrivateCloudDatabase?> GetUserPrivateDatabaseAsync(string userEmail)
        {
            // ✅ STEP 1: Check if this is a regular user with private cloud
            var privateDbConfig = await _mainContext.Set<PrivateCloudDatabase>()
                .AsNoTracking()
                .FirstOrDefaultAsync(db => db.UserEmail == userEmail && db.IsActive);
            
            if (privateDbConfig != null)
            {
                _logger.LogDebug("🔍 GetUserPrivateDatabaseAsync: Found config for user {Email}", userEmail);
                return privateDbConfig;
            }
            
            // ✅ STEP 2: Check if this email is a SUBUSER in Main DB
            var subuserRecord = await _mainContext.subuser
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.subuser_email == userEmail);
            
            if (subuserRecord != null)
            {
                // Found subuser in Main DB - get parent's private cloud config
                var parentConfig = await _mainContext.Set<PrivateCloudDatabase>()
                    .AsNoTracking()
                    .FirstOrDefaultAsync(db => db.UserEmail == subuserRecord.user_email && db.IsActive);
                
                _logger.LogDebug("🔍 GetUserPrivateDatabaseAsync: {Email} is subuser in Main DB, parent config found: {Found}", 
                    userEmail, parentConfig != null);
                return parentConfig;
            }
            
            // ✅ STEP 3: Subuser NOT in Main DB - search all Private Cloud DBs
            _logger.LogDebug("🔍 GetUserPrivateDatabaseAsync: {Email} not in Main DB, searching Private Cloud DBs...", userEmail);
            
            var privateCloudUsers = await _mainContext.Users
                .AsNoTracking()
                .Where(u => u.is_private_cloud == true)
                .ToListAsync();
            
            foreach (var pcUser in privateCloudUsers)
            {
                try
                {
                    // Get private cloud database config
                    var pcConfig = await _mainContext.Set<PrivateCloudDatabase>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(db => db.UserEmail == pcUser.user_email && db.IsActive && db.SchemaInitialized);
                    
                    if (pcConfig == null) continue;
                    
                    // Create context for private cloud DB
                    var connectionString = DecryptConnectionString(pcConfig.ConnectionString);
                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)), 
                        mysqlOptions => {
                            mysqlOptions.CommandTimeout(3);
                            mysqlOptions.EnableRetryOnFailure(1, TimeSpan.FromSeconds(1), null);
                        });
                    
                    using var pcContext = new ApplicationDbContext(optionsBuilder.Options);
                    
                    var subuserInPrivate = await pcContext.subuser
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.subuser_email == userEmail);
                    
                    if (subuserInPrivate != null)
                    {
                        _logger.LogInformation("✅ GetUserPrivateDatabaseAsync: Found {Email} in Private Cloud DB of parent {Parent}", 
                            userEmail, pcUser.user_email);
                        return pcConfig; // Return parent's private cloud config
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "⚠️ Error checking Private Cloud DB for user {Email}", pcUser.user_email);
                    continue;
                }
            }
            
            _logger.LogDebug("🔍 GetUserPrivateDatabaseAsync: No private cloud config found for {Email}", userEmail);
            return null;
        }

        public async Task<bool> IsPrivateCloudUserAsync(string userEmail)
        {
            // ✅ STEP 1: Check if this is a regular user first
            var user = await _mainContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.user_email == userEmail);
            
            if (user != null)
            {
                _logger.LogDebug("🔍 IsPrivateCloudUserAsync: {Email} is a regular user, is_private_cloud = {IsPrivate}", userEmail, user.is_private_cloud);
                return user.is_private_cloud == true;
            }
            
            // ✅ STEP 2: Check if this email is a SUBUSER in Main DB
            var subuserRecord = await _mainContext.subuser
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.subuser_email == userEmail);
            
            if (subuserRecord != null)
            {
                // Found subuser in Main DB - check parent's private cloud status
                var parentUser = await _mainContext.Users
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.user_email == subuserRecord.user_email);
                
                var isPrivate = parentUser?.is_private_cloud == true;
                _logger.LogDebug("🔍 IsPrivateCloudUserAsync: {Email} is a subuser in Main DB, parent {Parent}, is_private_cloud = {IsPrivate}", 
                    userEmail, subuserRecord.user_email, isPrivate);
                return isPrivate;
            }
            
            // ✅ STEP 3: Subuser NOT in Main DB - check all Private Cloud DBs
            _logger.LogDebug("🔍 IsPrivateCloudUserAsync: {Email} not found in Main DB, searching Private Cloud DBs...", userEmail);
            
            var privateCloudUsers = await _mainContext.Users
                .AsNoTracking()
                .Where(u => u.is_private_cloud == true)
                .ToListAsync();
            
            foreach (var pcUser in privateCloudUsers)
            {
                try
                {
                    // Get private cloud database config
                    var pcConfig = await _mainContext.Set<PrivateCloudDatabase>()
                        .AsNoTracking()
                        .FirstOrDefaultAsync(db => db.UserEmail == pcUser.user_email && db.IsActive && db.SchemaInitialized);
                    
                    if (pcConfig == null) continue;
                    
                    // Create context for private cloud DB
                    var connectionString = DecryptConnectionString(pcConfig.ConnectionString);
                    var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                    optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)), 
                        mysqlOptions => {
                            mysqlOptions.CommandTimeout(3);
                            mysqlOptions.EnableRetryOnFailure(1, TimeSpan.FromSeconds(1), null);
                        });
                    
                    using var pcContext = new ApplicationDbContext(optionsBuilder.Options);
                    
                    var subuserInPrivate = await pcContext.subuser
                        .AsNoTracking()
                        .FirstOrDefaultAsync(s => s.subuser_email == userEmail);
                    
                    if (subuserInPrivate != null)
                    {
                        _logger.LogInformation("✅ IsPrivateCloudUserAsync: Found {Email} in Private Cloud DB of parent {Parent}", 
                            userEmail, pcUser.user_email);
                        return true; // Subuser found in private cloud DB = parent has private cloud
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "⚠️ Error checking Private Cloud DB for user {Email}", pcUser.user_email);
                    continue;
                }
            }
            
            _logger.LogDebug("🔍 IsPrivateCloudUserAsync: {Email} not found anywhere, returning false", userEmail);
            return false;
        }

 public async Task<bool> SetupPrivateDatabaseAsync(PrivateCloudDatabaseDto dto)
     {
   try
 {
  _logger.LogInformation(" === SETUP PRIVATE DATABASE START ===");
         _logger.LogInformation("User Email: {Email}", dto.UserEmail);

   var user = await _mainContext.Users
        .FirstOrDefaultAsync(u => u.user_email == dto.UserEmail);

       if (user == null)
       {
           _logger.LogError("❌ User {Email} NOT FOUND", dto.UserEmail);
 return false;
                }

      if (user.is_private_cloud != true)
        {
        _logger.LogError("❌ User {Email} is_private_cloud is NOT TRUE", dto.UserEmail);
   return false;
              }

                var connectionString = BuildConnectionString(dto);
    var testResult = await TestConnectionAsync(connectionString, dto.DatabaseType);

      if (!testResult.Success)
       {
        _logger.LogError("❌ Connection test FAILED: {Error}", testResult.Error);
         return false;
           }

      var existingConfig = await _mainContext.Set<PrivateCloudDatabase>()
             .FirstOrDefaultAsync(db => db.UserEmail == dto.UserEmail);

    if (existingConfig != null)
           {
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

          await _mainContext.SaveChangesAsync();
    _logger.LogInformation("✅ === SETUP COMPLETE ===");
         return true;
            }
   catch (Exception ex)
  {
     _logger.LogError(ex, "❌ EXCEPTION IN SETUP: {Message}", ex.Message);
                return false;
        }
        }

        public async Task<bool> SetupPrivateDatabaseFromConnectionStringAsync(
            string userEmail, string connectionString, string? databaseType = null, string? notes = null)
        {
        try
  {
   _logger.LogInformation("=== SIMPLIFIED SETUP START ===");

var user = await _mainContext.Users
         .FirstOrDefaultAsync(u => u.user_email == userEmail);

       if (user == null || user.is_private_cloud != true)
            {
      _logger.LogError("❌ User not found or not private cloud user");
        return false;
         }

    databaseType ??= DetectDatabaseType(connectionString);
       var parsedComponents = ParseConnectionStringComponents(connectionString, databaseType);
  var testResult = await TestConnectionAsync(parsedComponents.StandardConnectionString, databaseType);

 if (!testResult.Success)
       {
      _logger.LogError("❌ Connection test FAILED: {Error}", testResult.Error);
    return false;
                }

         var existingConfig = await _mainContext.Set<PrivateCloudDatabase>()
           .FirstOrDefaultAsync(db => db.UserEmail == userEmail);

              if (existingConfig != null)
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
     }
 else
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
   }

   await _mainContext.SaveChangesAsync();
    _logger.LogInformation("✅ === SIMPLIFIED SETUP COMPLETE ===");
   return true;
        }
            catch (Exception ex)
            {
    _logger.LogError(ex, "❌ EXCEPTION IN SIMPLIFIED SETUP: {Message}", ex.Message);
         return false;
 }
        }

   public async Task<DatabaseTestResult> TestDatabaseConnectionAsync(string userEmail)
    {
        try
            {
   var config = await GetUserPrivateDatabaseAsync(userEmail);
        if (config == null)
                {
    return new DatabaseTestResult { Success = false, Message = "Configuration not found" };
         }

   var connectionString = DecryptConnectionString(config.ConnectionString);
          return await TestConnectionAsync(connectionString, config.DatabaseType);
   }
        catch (Exception ex)
         {
      _logger.LogError(ex, "Error testing connection for {Email}", userEmail);
        return new DatabaseTestResult { Success = false, Message = "Error", Error = ex.Message };
  }
        }

        public async Task<bool> InitializeDatabaseSchemaAsync(string userEmail)
        {
            try
    {
    _logger.LogInformation("=== INITIALIZING DATABASE SCHEMA ===");
         _logger.LogInformation("User Email: {Email}", userEmail);

    var config = await GetUserPrivateDatabaseAsync(userEmail);
     if (config == null)
  {
     _logger.LogError("❌ Private database configuration not found for {Email}", userEmail);
         return false;
        }

     _logger.LogInformation("✅ Found config - Host: {Host}, Database: {Database}",
      config.ServerHost, config.DatabaseName);

      if (config.SchemaInitialized)
                {
         _logger.LogInformation("✅ Schema already initialized for {Email}, skipping", userEmail);
       return true;
       }

       string connectionString;
         try
{
      connectionString = DecryptConnectionString(config.ConnectionString);
  _logger.LogInformation("✅ Connection string decrypted successfully");
    }
  catch (Exception decryptEx)
      {
            _logger.LogError(decryptEx, "❌ Failed to decrypt connection string for {Email}", userEmail);
    return false;
   }

                _logger.LogInformation("📋 Creating database schema tables...");
  var success = await CreateDatabaseSchemaAsync(connectionString, config.DatabaseType);

     if (success)
    {
          _logger.LogInformation("✅ Schema creation successful, updating config...");
        config.SchemaInitialized = true;
           config.SchemaInitializedAt = DateTime.UtcNow;
      config.UpdatedAt = DateTime.UtcNow;
    await _mainContext.SaveChangesAsync();
             _logger.LogInformation("✅ === SCHEMA INITIALIZATION COMPLETE for {Email} ===", userEmail);
            }
              else
                {
    _logger.LogError("❌ Schema creation returned false for {Email}", userEmail);
           }

    return success;
     }
    catch (Exception ex)
 {
                _logger.LogError(ex, "❌ Error initializing database schema for {Email}: {Message}", userEmail, ex.Message);
   return false;
   }
        }

        public async Task<string> GetConnectionStringAsync(string userEmail)
        {
            var config = await GetUserPrivateDatabaseAsync(userEmail);
            if (config == null)
       {
         throw new InvalidOperationException($"Private database not configured for {userEmail}");
            }
            return DecryptConnectionString(config.ConnectionString);
        }

     public async Task<DbContext> GetUserDbContextAsync(string userEmail)
        {
            if (_dbContextCache.TryGetValue(userEmail, out var cachedContext))
   {
       return cachedContext;
            }

        var connectionString = await GetConnectionStringAsync(userEmail);
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
   optionsBuilder.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 21)));

     var context = new ApplicationDbContext(optionsBuilder.Options);
            _dbContextCache[userEmail] = context;
   return context;
     }

        public async Task<SchemaValidationResult> ValidateDatabaseSchemaAsync(string userEmail)
        {
        try
   {
    var config = await GetUserPrivateDatabaseAsync(userEmail);
   if (config == null)
  {
  return new SchemaValidationResult { IsValid = false, Message = "No configuration found" };
     }

          var connectionString = DecryptConnectionString(config.ConnectionString);
     using var connection = new MySqlConnection(connectionString);
   await connection.OpenAsync();

   var existingTables = await GetExistingTablesAsync(connection);
    var requiredTables = _tableSchemas.Keys.ToList();
  
     // ✅ FIX: Case-insensitive comparison for table names
     var missingTables = requiredTables.Where(t => 
         !existingTables.Any(e => e.Equals(t, StringComparison.OrdinalIgnoreCase))
     ).ToList();

        await connection.CloseAsync();

         return new SchemaValidationResult
 {
    IsValid = missingTables.Count == 0,
        Message = missingTables.Count == 0 ? "All tables exist" : $"Missing: {string.Join(", ", missingTables)}",
      ExistingTables = existingTables,
          MissingTables = missingTables,
   RequiredTables = requiredTables
      };
   }
 catch (Exception ex)
            {
      _logger.LogError(ex, "Error validating schema");
   return new SchemaValidationResult { IsValid = false, Message = $"Error: {ex.Message}" };
   }
  }

      public async Task<List<string>> GetRequiredTablesAsync()
        {
       return await Task.FromResult(_tableSchemas.Keys.ToList());
        }

        public async Task<bool> DeletePrivateDatabaseConfigAsync(string userEmail)
        {
    try
        {
     var config = await GetUserPrivateDatabaseAsync(userEmail);
       if (config == null) return false;

                if (_dbContextCache.ContainsKey(userEmail))
  {
          await _dbContextCache[userEmail].DisposeAsync();
        _dbContextCache.Remove(userEmail);
                }

          config.IsActive = false;
       config.UpdatedAt = DateTime.UtcNow;
     _mainContext.Entry(config).State = EntityState.Modified;
              await _mainContext.SaveChangesAsync();

           _logger.LogInformation("✅ Configuration deleted for {Email}", userEmail);
        return true;
  }
            catch (Exception ex)
            {
        _logger.LogError(ex, "❌ Error deleting configuration for {Email}", userEmail);
    return false;
   }
        }

        #region Private Helper Methods

        private string DetectDatabaseType(string connectionString)
        {
        if (connectionString.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase) ||
    connectionString.Contains("Server=", StringComparison.OrdinalIgnoreCase))
     return "mysql";
   if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
      return "postgresql";
            return "mysql";
    }

        private ConnectionStringComponents ParseConnectionStringComponents(string connectionString, string databaseType)
{
            var components = new ConnectionStringComponents();

 if (connectionString.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase))
      {
      var uriWithoutProtocol = connectionString.Replace("mysql://", "");
    var parts = uriWithoutProtocol.Split('@');
       if (parts.Length == 2)
    {
     var credentials = parts[0].Split(':');
 components.Username = credentials[0];
    var password = credentials.Length > 1 ? credentials[1] : "";

      var hostAndDb = parts[1].Split('?')[0];
        var hostPortDb = hostAndDb.Split('/');

            if (hostPortDb.Length > 0)
      {
     var hostPort = hostPortDb[0].Split(':');
         components.Host = hostPort[0];
    components.Port = hostPort.Length > 1 && int.TryParse(hostPort[1], out var port) ? port : 3306;
           }

           if (hostPortDb.Length > 1)
   components.Database = hostPortDb[1];

     components.StandardConnectionString =
   $"Server={components.Host};Port={components.Port};Database={components.Database};User={components.Username};Password={password};AllowUserVariables=true;SslMode=Required;";
   }
            }
       else
       {
 var keyValues = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries);
       foreach (var kv in keyValues)
       {
        var kvParts = kv.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
               if (kvParts.Length == 2)
  {
      var key = kvParts[0].Trim().ToLower();
             var value = kvParts[1].Trim();

          switch (key)
 {
              case "server":
               case "host":
          components.Host = value;
  break;
        case "port":
    int.TryParse(value, out var p);
            components.Port = p > 0 ? p : 3306;
          break;
                case "database":
             components.Database = value;
            break;
        case "user":
     case "userid":
           case "username":
         components.Username = value;
         break;
       }
        }
   }
           components.StandardConnectionString = connectionString;
    if (!connectionString.Contains("SslMode", StringComparison.OrdinalIgnoreCase))
           components.StandardConnectionString += ";SslMode=Required;";
       }

  return components;
        }

        private string BuildConnectionString(PrivateCloudDatabaseDto dto)
        {
  return dto.DatabaseType.ToLower() switch
{
"mysql" => $"server={dto.ServerHost};port={dto.ServerPort};database={dto.DatabaseName};user={dto.DatabaseUsername};password={dto.DatabasePassword};AllowUserVariables=true;SslMode=Required;",
             "postgresql" => $"Host={dto.ServerHost};Port={dto.ServerPort};Database={dto.DatabaseName};Username={dto.DatabaseUsername};Password={dto.DatabasePassword};SslMode=Require;",
         _ => throw new NotSupportedException($"Database type {dto.DatabaseType} not supported")
    };
   }

        private async Task<DatabaseTestResult> TestConnectionAsync(string connectionString, string databaseType)
        {
     var result = new DatabaseTestResult();
            var startTime = DateTime.UtcNow;

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

      var tables = await GetExistingTablesAsync(connection);
   
               // ✅ FIX: Case-insensitive comparison for missing tables
     result.MissingTables = _tableSchemas.Keys.Where(t => 
        !tables.Any(e => e.Equals(t, StringComparison.OrdinalIgnoreCase))
             ).ToList();
        result.SchemaExists = result.MissingTables.Count == 0;

        await connection.CloseAsync();
     }
   else
     {
       result.Success = false;
    result.Message = $"Database type {databaseType} not supported";
     }
     }
      catch (MySqlException mysqlEx)
     {
      _logger.LogError(mysqlEx, "MySQL error: {Number} - {Message}", mysqlEx.Number, mysqlEx.Message);
            result.Success = false;
     result.Message = "Connection failed";
     result.Error = mysqlEx.Message;
     }
     catch (Exception ex)
   {
       _logger.LogError(ex, "Connection error: {Message}", ex.Message);
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
          tables.Add(reader.GetString(0).ToLower());
    }

 return tables;
        }

        private async Task<bool> CreateDatabaseSchemaAsync(string connectionString, string databaseType)
     {
            try
     {
     _logger.LogInformation("=== CREATING DATABASE SCHEMA ===");

    if (databaseType.ToLower() != "mysql")
        {
           _logger.LogError("Schema creation not supported for {Type}", databaseType);
        return false;
       }

 using var connection = new MySqlConnection(connectionString);
 await connection.OpenAsync();
_logger.LogInformation("✅ Connected to database for schema creation");

  // Create tables in dependency order (independent tables first, then dependent tables)
  var tableOrder = new[] { 
      "users", 
   "groups", 
      "Roles",
      "Permissions",
      "Routes",
      "subuser", 
      "machines", 
      "AuditReports", 
      "sessions", 
      "logs", 
"commands",
      "UserRoles",
      "SubuserRoles",
      "RolePermissions"
  };

      foreach (var tableName in tableOrder)
        {
      if (_tableSchemas.TryGetValue(tableName, out var schema))
          {
    try
   {
  _logger.LogInformation("📋 Creating table: {TableName}", tableName);

         var command = connection.CreateCommand();
    command.CommandText = schema;
     command.CommandTimeout = 60;
    await command.ExecuteNonQueryAsync();

 _logger.LogInformation("✅ Table created: {TableName}", tableName);
   }
            catch (MySqlException mysqlEx)
      {
     if (mysqlEx.Number == 1050)
  {
     _logger.LogInformation("⏭️ Table already exists: {TableName}", tableName);
   continue;
  }

                  _logger.LogError(mysqlEx, "❌ MySQL error creating {TableName}: #{Number} - {Message}",
 tableName, mysqlEx.Number, mysqlEx.Message);
  }
catch (Exception tableEx)
   {
       _logger.LogError(tableEx, "❌ Error creating table {TableName}", tableName);
     }
            }
       }

  await connection.CloseAsync();
    _logger.LogInformation("✅ === SCHEMA CREATION COMPLETE ===");
        return true;
     }
   catch (Exception ex)
    {
    _logger.LogError(ex, "❌ Error creating database schema: {Message}", ex.Message);
return false;
        }
        }

   private string EncryptConnectionString(string connectionString)
{
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

    public class SchemaValidationResult
    {
   public bool IsValid { get; set; }
 public string Message { get; set; } = string.Empty;
        public List<string> ExistingTables { get; set; } = new();
     public List<string> MissingTables { get; set; } = new();
        public List<string> RequiredTables { get; set; } = new();
    }

    internal class ConnectionStringComponents
    {
      public string Host { get; set; } = string.Empty;
        public int Port { get; set; } = 3306;
     public string Database { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string StandardConnectionString { get; set; } = string.Empty;
    }
}
