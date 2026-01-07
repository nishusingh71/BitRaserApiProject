using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using DSecureApi.Models;
using DSecureApi.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DSecureApi.Factories;
using MySql.Data.MySqlClient;  // ✅ Add this for MySqlException

namespace DSecureApi.Controllers
{
    /// <summary>
    /// Private Cloud Database Management Controller
    /// Allows users with is_private_cloud=true to configure their own database
    /// ✅ ENHANCED: Now integrates with TenantConnectionService for automatic routing
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrivateCloudController : ControllerBase
    {
        private readonly IPrivateCloudService _privateCloudService;
        private readonly ApplicationDbContext _context;
        private readonly ITenantConnectionService _tenantService;
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly ILogger<PrivateCloudController> _logger;
        private readonly ICacheService _cacheService;

        public PrivateCloudController(
            IPrivateCloudService privateCloudService,
      ApplicationDbContext context,
        ITenantConnectionService tenantService,
       DynamicDbContextFactory contextFactory,
         ILogger<PrivateCloudController> logger,
         ICacheService cacheService)
        {
   _privateCloudService = privateCloudService;
    _context = context;
   _tenantService = tenantService;
            _contextFactory = contextFactory;
          _logger = logger;
          _cacheService = cacheService;
        }

        /// <summary>
    /// Get private cloud database configuration for current user
        /// </summary>
        [HttpGet("config")]
public async Task<ActionResult<PrivateCloudDatabase>> GetConfig()
        {
   try
            {
              // ✅ FIX: Use ClaimTypes.NameIdentifier instead of User.Identity.Name
                var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
           if (string.IsNullOrEmpty(userEmail))
          {
          _logger.LogWarning("Unauthorized access attempt - no user email in claims");
             return Unauthorized(new { message = "User email not found in token" });
 }

        var config = await _privateCloudService.GetUserPrivateDatabaseAsync(userEmail);
      
      if (config == null)
     return NotFound(new { message = "No private database configured" });

                // Don't return sensitive connection string
                config.ConnectionString = "***ENCRYPTED***";
 
         return Ok(config);
    }
            catch (Exception ex)
     {
  _logger.LogError(ex, "Error getting private cloud config");
  return StatusCode(500, new { message = "Error retrieving configuration", error = ex.Message });
  }
        }

        /// <summary>
     /// Delete private cloud database configuration for current user
      /// ✅ NEW: Soft delete - marks configuration as inactive
        /// </summary>
[HttpDelete("config")]
        public async Task<ActionResult> DeleteConfig()
        {
    try
            {
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
                {
      _logger.LogWarning("Unauthorized delete attempt - no user email in claims");
        return Unauthorized(new { message = "User email not found in token" });
   }

             var config = await _privateCloudService.GetUserPrivateDatabaseAsync(userEmail);
         if (config == null)
        {
       return NotFound(new { message = "No private database configuration found" });
            }

      var success = await _privateCloudService.DeletePrivateDatabaseConfigAsync(userEmail);

       if (success)
       {
    _logger.LogInformation("✅ Private cloud configuration deleted for {Email}", userEmail);
    return Ok(new
       {
                 success = true,
              message = "Private cloud configuration deleted successfully",
        userEmail,
          note = "Your data in the private database has NOT been deleted. Only the connection configuration was removed.",
    nextSteps = new[]
          {
    "You can reconfigure private cloud using /setup endpoint",
      "Your data will continue to use the main database until reconfigured"
 }
 });
                }

 return BadRequest(new { message = "Failed to delete configuration" });
      }
       catch (Exception ex)
       {
                _logger.LogError(ex, "Error deleting private cloud config");
                return StatusCode(500, new { message = "Error deleting configuration", error = ex.Message });
          }
        }

  /// <summary>
        /// Setup or update private cloud database
        /// </summary>
     [HttpPost("setup")]
        public async Task<ActionResult> SetupPrivateDatabase([FromBody] PrivateCloudDatabaseDto dto)
      {
   try
 {
        // ✅ FIX: Use ClaimTypes.NameIdentifier
        var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     if (string.IsNullOrEmpty(userEmail))
    {
        _logger.LogWarning("Unauthorized setup attempt - no user email in claims");
     return Unauthorized(new { message = "User email not found in token" });
    }

           // Check if user has private cloud enabled
      var user = await _context.Users.Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
     if (user == null || user.is_private_cloud != true)
     {
    return BadRequest(new 
     { 
  message = "Private cloud feature not enabled for this user",
    hint = "Please contact administrator to enable private cloud access"
   });
        }

       dto.UserEmail = userEmail;

        // ✅ ENHANCED: Call service and capture detailed error
   _logger.LogInformation("Setting up private database for user: {Email}, DatabaseType: {Type}, Host: {Host}", 
 userEmail, dto.DatabaseType, dto.ServerHost);

  var success = await _privateCloudService.SetupPrivateDatabaseAsync(dto);

    if (success)
 {
   _logger.LogInformation("Private database setup successful for {Email}", userEmail);
    return Ok(new 
  { 
 message = "Private database configured successfully",
  nextStep = "Test the connection using /test endpoint"
  });
      }

    // ✅ ENHANCED: Return more detailed error
   _logger.LogWarning("Failed to configure private database for {Email}", userEmail);
  return BadRequest(new { 
    message = "Failed to configure private database",
     detail = "Please check the logs for more information or verify your database credentials",
  userEmail = userEmail,
       databaseType = dto.DatabaseType,
      serverHost = dto.ServerHost
      });
       }
          catch (Exception ex)
     {
  _logger.LogError(ex, "Error setting up private database for {Email}. Error: {Error}", 
      User.FindFirst(ClaimTypes.NameIdentifier)?.Value, ex.Message);
 return StatusCode(500, new { 
       message = "Error during setup", 
     error = ex.Message,
  stackTrace = ex.StackTrace,
   innerError = ex.InnerException?.Message
    });
  }
    }

 /// <summary>
        /// Setup private cloud - SIMPLIFIED VERSION (Connection String Only)
        /// User provides only connection string, backend auto-parses and processes
        /// </summary>
   [HttpPost("setup-simple")]
        public async Task<ActionResult> SetupPrivateDatabaseSimple([FromBody] SimplePrivateCloudSetupDto dto)
  {
  try
            {
           var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          if (string.IsNullOrEmpty(userEmail))
        {
        return Unauthorized(new { message = "User email not found in token" });
       }

     // Check if user has private cloud enabled
        var user = await _context.Users.Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
    if (user == null)
       {
    return NotFound(new { message = "User not found" });
    }

     if (user.is_private_cloud != true)
        {
      return BadRequest(new
     {
  message = "Private cloud feature not enabled for this user",
     hint = "Please contact administrator to enable private cloud access",
    userEmail = userEmail,
          isPrivateCloud = user.is_private_cloud
          });
   }

          _logger.LogInformation("🔧 Setting up private cloud for user: {Email}", userEmail);

             // Validate connection string
     if (string.IsNullOrWhiteSpace(dto.ConnectionString))
      {
      return BadRequest(new { message = "Connection string is required" });
}

      // Step 1: Setup private database configuration
      bool setupSuccess = await _privateCloudService.SetupPrivateDatabaseFromConnectionStringAsync(
          userEmail,
      dto.ConnectionString,
    dto.DatabaseType,
     dto.Notes
           );

       if (!setupSuccess)
  {
  return BadRequest(new
   {
 message = "Failed to configure private database",
      hint = "Check logs for details"
                  });
       }

     _logger.LogInformation("✅ Private database configured for {Email}", userEmail);

  // Step 2: Test the connection
                var testResult = await _privateCloudService.TestDatabaseConnectionAsync(userEmail);
    
       if (!testResult.Success)
        {
      return BadRequest(new
   {
            message = "Database configured but connection test failed",
         error = testResult.Error,
           hint = "Please verify your connection string"
   });
       }

      _logger.LogInformation("✅ Connection test passed for {Email}", userEmail);

  // Step 3: Initialize schema in private database
      bool schemaSuccess = await _privateCloudService.InitializeDatabaseSchemaAsync(userEmail);
                
       if (!schemaSuccess)
     {
           return BadRequest(new
        {
     message = "Database configured but schema initialization failed",
         hint = "You can retry using /initialize-schema endpoint"
           });
          }

                _logger.LogInformation("✅ Schema initialized for {Email}", userEmail);

    // Step 4: Verify tenant routing is working
  bool canRoute = await _tenantService.IsPrivateCloudUserAsync();
        
       return Ok(new
           {
       success = true,
   message = "Private cloud setup complete",
         details = new
         {
     databaseConfigured = true,
              connectionTested = true,
        schemaInitialized = true,
     tenantRoutingEnabled = canRoute
          },
      userEmail = userEmail,
        databaseType = dto.DatabaseType ?? "mysql",
      nextSteps = new []
      {
        "Your data will now automatically route to your private database",
      "Create audit reports - they will be stored in your private cloud",
                "Add subusers - they will be stored in your private database"
    }
      });
         }
  catch (Exception ex)
        {
  _logger.LogError(ex, "Error in simplified private cloud setup");
            return StatusCode(500, new
        {
     message = "Error during setup",
       error = ex.Message,
        detail = ex.InnerException?.Message
      });
       }
  }

        /// <summary>
        /// ✅ NEW: Test tenant routing after setup
        /// Verifies that data is routing to the correct database
        /// </summary>
        [HttpGet("test-routing")]
        public async Task<ActionResult> TestRouting()
        {
      try
            {
  var userEmail = _tenantService.GetCurrentUserEmail();
                if (string.IsNullOrEmpty(userEmail))
            {
       return Unauthorized(new { message = "User email not found" });
     }

    // Check if private cloud is enabled
             bool isPrivateCloud = await _tenantService.IsPrivateCloudUserAsync();

          // Get connection string that will be used
       string connectionString = await _tenantService.GetConnectionStringAsync();

 // Create dynamic context to test
           using var dynamicContext = await _contextFactory.CreateDbContextAsync();

        // Try to query database
       bool canConnect = await dynamicContext.Database.CanConnectAsync();

      // Get some stats
         var auditReportsCount = await dynamicContext.AuditReports
    .CountAsync(r => r.client_email == userEmail);

var subusersCount = await dynamicContext.subuser
           .CountAsync(s => s.user_email == userEmail);

     return Ok(new
      {
 routingStatus = "Working",
        userEmail,
           isPrivateCloud,
 canConnect,
    database = isPrivateCloud ? "Private Cloud" : "Main Database",
      statistics = new
       {
          auditReports = auditReportsCount,
         subusers = subusersCount
     },
              message = isPrivateCloud
           ? "✅ You are connected to your private cloud database"
     : "ℹ️ You are using the main shared database"
     });
            }
            catch (Exception ex)
            {
          _logger.LogError(ex, "Error testing routing");
    return StatusCode(500, new
             {
        message = "Error testing routing",
        error = ex.Message
     });
  }
        }

        /// <summary>
        /// ✅ NEW: Migrate existing data from Main DB to Private Cloud
        /// Copies user's audit reports and subusers to their private database
        /// </summary>
      [HttpPost("migrate-data")]
    public async Task<ActionResult> MigrateData()
        {
       try
      {
       var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
       if (string.IsNullOrEmpty(userEmail))
       {
         return Unauthorized();
        }

   _logger.LogInformation("🚀 Starting data migration for {Email}", userEmail);

        // Verify user has private cloud
             bool isPrivateCloud = await _tenantService.IsPrivateCloudUserAsync();
  if (!isPrivateCloud)
  {
              return BadRequest(new { message = "Private cloud not enabled" });
        }

          _logger.LogInformation("📊 Fetching data from Main Database...");

           // ✅ Get data from Main DB - detach from context to avoid tracking issues
        var mainReports = await _context.AuditReports
      .AsNoTracking()
            .Where(r => r.client_email == userEmail)
               .ToListAsync();

                var mainSubusers = await _context.subuser
        .AsNoTracking()
 .Where(s => s.user_email == userEmail)
     .ToListAsync();

      var mainMachines= await _context.Machines
        .AsNoTracking()
        .Where(m=> m.user_email == userEmail) .ToListAsync();

        _logger.LogInformation("📦 Found {ReportCount} reports and {SubuserCount} subusers ,{MainMachinesCount} machines in Main DB",
        mainReports.Count, mainSubusers.Count, mainMachines.Count);

if (mainReports.Count == 0 && mainSubusers.Count == 0 && mainMachines.Count == 0)
{
    return Ok(new
          {
         message = "No data to migrate",
            migrated = new { auditReports = 0, subusers = 0,machines=0},
 note = "No existing data found in main database"
         });
   }

        _logger.LogInformation("🔄 Creating Private Cloud context...");

     // ✅ Create private cloud context
             using var privateContext = await _contextFactory.CreateDbContextAsync();

      // Test connection
     var canConnect = await privateContext.Database.CanConnectAsync();
       if (!canConnect)
   {
      _logger.LogError("❌ Cannot connect to private cloud database");
      return StatusCode(500, new
    {
     message = "Error connecting to private cloud database",
     error = "Connection test failed"
  });
      }

     _logger.LogInformation("✅ Private cloud connection successful");
        _logger.LogInformation("📥 Migrating audit reports...");

           int reportsMigrated = 0;
        int subusersMigrated = 0;

        // Migrate reports in batches
                foreach (var report in mainReports)
       {
    try
        {
 var exists = await privateContext.AuditReports
              .AnyAsync(r => r.report_id == report.report_id);

        if (!exists)
        {
 // ✅ Add report directly (EF Core will handle property mapping)
 privateContext.AuditReports.Add(report);
    reportsMigrated++;

    // Save in batches
          if (reportsMigrated % 10 == 0)
 {
           await privateContext.SaveChangesAsync();
           _logger.LogInformation("💾 Saved batch: {Count} reports", reportsMigrated);
 }
           }
     }
     catch (Exception reportEx)
     {
            _logger.LogWarning(reportEx, "⚠️ Failed to migrate report {ReportId}", report.report_id);
    }
 }

          // Save remaining reports
       if (reportsMigrated % 10 != 0)
        {
        await privateContext.SaveChangesAsync();
         }

    _logger.LogInformation("✅ Migrated {Count} audit reports", reportsMigrated);
  _logger.LogInformation("📥 Migrating subusers...");

        // Migrate subusers in batches
                foreach (var subuser in mainSubusers)
    {
             try
      {
   var exists = await privateContext.subuser
   .AnyAsync(s => s.subuser_id == subuser.subuser_id);

      if (!exists)
    {
              // ✅ Add subuser directly
  privateContext.subuser.Add(subuser);
            subusersMigrated++;

        // Save in batches
if (subusersMigrated % 10 == 0)
     {
          await privateContext.SaveChangesAsync();
          _logger.LogInformation("💾 Saved batch: {Count} subusers", subusersMigrated);
        }
         }
    }
     catch (Exception subuserEx)
            {
      _logger.LogWarning(subuserEx, "⚠️ Failed to migrate subuser {SubuserId}", subuser.subuser_id);
       }
      }

             // Save remaining subusers
             if (subusersMigrated % 10 != 0)
      {
  await privateContext.SaveChangesAsync();
    }

     _logger.LogInformation("✅ Migrated {Count} subusers", subusersMigrated);
  _logger.LogInformation("📥 Migrating machines...");

        // Migrate machines in batches
  int machinesMigrated = 0;
        foreach (var machine in mainMachines)
   {
    try
            {
     var exists = await privateContext.Machines
      .AnyAsync(m => m.fingerprint_hash == machine.fingerprint_hash);

      if (!exists)
     {
   // ✅ Add machine directly
    privateContext.Machines.Add(machine);
    machinesMigrated++;

 // Save in batches
      if (machinesMigrated % 10 == 0)
    {
    await privateContext.SaveChangesAsync();
       _logger.LogInformation("💾 Saved batch: {Count} machines", machinesMigrated);
  }
       }
 }
          catch (Exception machineEx)
  {
     _logger.LogWarning(machineEx, "⚠️ Failed to migrate machine {Hash}", machine.fingerprint_hash);
 }
        }

  // Save remaining machines
        if (machinesMigrated % 10 != 0)
    {
 await privateContext.SaveChangesAsync();
   }

        _logger.LogInformation("✅ Migrated {Count} machines", machinesMigrated);
  _logger.LogInformation("🎉 Data migration complete for {Email}", userEmail);

       return Ok(new
 {
     message = "Data migrated successfully",
      migrated = new
      {
   auditReports = reportsMigrated,
     subusers = subusersMigrated,
  machines = machinesMigrated
     },
          found = new
   {
    auditReports = mainReports.Count,
       subusers = mainSubusers.Count,
            machines = mainMachines.Count
          },
             note = reportsMigrated > 0 || subusersMigrated > 0 || machinesMigrated > 0
    ? "Your existing data has been copied to your private database"
    : "No new data to migrate (already exists in private database)"
 });
   }
    catch (DbUpdateException dbEx)
   {
       _logger.LogError(dbEx, "❌ Database error during data migration");
    return StatusCode(500, new
            {
         message = "Database error during migration",
              error = dbEx.InnerException?.Message ?? dbEx.Message,
      hint = "Some data may already exist in the private database"
        });
            }
      catch (MySqlException mysqlEx)
            {
  _logger.LogError(mysqlEx, "❌ MySQL error during data migration");
       return StatusCode(500, new
  {
      message = "MySQL connection error",
              error = mysqlEx.Message,
     hint = "Please verify your private database connection is active"
      });
    }
    catch (Exception ex)
   {
                _logger.LogError(ex, "❌ Error during data migration");
           return StatusCode(500, new
              {
   message = "Error migrating data",
              error = ex.Message,
        innerError = ex.InnerException?.Message
 });
    }
   }

      /// <summary>
        /// ✅ NEW: Complete setup wizard - all steps in one call
        /// </summary>
        [HttpPost("complete-setup")]
 public async Task<ActionResult> CompleteSetup([FromBody] CompleteSetupDto dto)
    {
     try
 {
 var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
     if (string.IsNullOrEmpty(userEmail))
   {
  return Unauthorized(new { message = "User email not found in token" });
   }

      _logger.LogInformation("🚀 Starting complete setup for {Email}", userEmail);

         // Verify user has private cloud access
 var user = await _context.Users.Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
  if (user == null)
 {
    return NotFound(new { message = "User not found" });
           }

      if (user.is_private_cloud != true)
  {
   return BadRequest(new
{
    message = "Private cloud not enabled for this user",
  detail = "Please contact administrator to enable private cloud access"
      });
 }

      var steps = new List<object>();

   // Step 1: Configure database
   _logger.LogInformation("Step 1: Configuring database...");

     bool configSuccess;
string configError = string.Empty;

      // Prepare combined notes with selected tables
            string combinedNotes = dto.Notes ?? "";
    if (!string.IsNullOrEmpty(dto.SelectedTables))
        {
combinedNotes = $"{dto.Notes}|SELECTED_TABLES:{dto.SelectedTables}";
         }

 try
     {
 configSuccess = await _privateCloudService.SetupPrivateDatabaseFromConnectionStringAsync(
      userEmail, 
    dto.ConnectionString, 
     dto.DatabaseType, 
   combinedNotes  // Combined notes + selected tables
    );
 }
    catch (Exception configEx)
       {
       _logger.LogError(configEx, "Exception in Step 1: Configure Database");
 configSuccess = false;
   configError = configEx.Message;
}

    steps.Add(new
   {
  step = 1,
    name = "Configure Database",
      status = configSuccess ? "✅ Success" : "❌ Failed",
  success = configSuccess,
     error = configSuccess ? null : configError
          });

        if (!configSuccess)
    {
       _logger.LogWarning("Setup failed at Step 1: Configure Database");
  return BadRequest(new
{
    success = false,
         message = "Database configuration failed",
    userEmail,
    steps,
 summary = new
    {
        totalSteps = 1,
    successfulSteps = 0,
failedAt = "Configure Database",
        error = configError
    }
       });
         }

         // Step 2: Test connection
        _logger.LogInformation("Step 2: Testing connection...");
       var testResult = await _privateCloudService.TestDatabaseConnectionAsync(userEmail);

      steps.Add(new
         {
   step = 2,
      name = "Test Connection",
              status = testResult.Success ? "✅ Success" : "❌ Failed",
      success = testResult.Success,
         details = testResult.Message,
 error = testResult.Success ? null : testResult.Error
});

     if (!testResult.Success)
    {
        _logger.LogWarning("Setup failed at Step 2: Test Connection");
 return BadRequest(new
           {
  success = false,
        message = "Connection test failed",
          userEmail,
         steps,
          summary = new
      {
  totalSteps = 2,
       successfulSteps = 1,
        failedAt = "Test Connection",
              error = testResult.Error
          }
    });
          }

   // Step 3: Initialize schema
   _logger.LogInformation("Step 3: Initializing schema...");
 bool schemaSuccess = await _privateCloudService.InitializeDatabaseSchemaAsync(userEmail);

   steps.Add(new
    {
     step = 3,
       name = "Initialize Schema",
     status = schemaSuccess ? "✅ Success" : "❌ Failed",
  success = schemaSuccess
     });

     if (!schemaSuccess)
      {
 _logger.LogWarning("Setup failed at Step 3: Initialize Schema");
  return BadRequest(new
       {
      success = false,
  message = "Schema initialization failed",
 userEmail,
        steps,
        summary = new
           {
     totalSteps = 3,
              successfulSteps = 2,
     failedAt = "Initialize Schema"
       }
    });
     }

   // Step 4: Verify routing
     _logger.LogInformation("Step 4: Verifying tenant routing...");
   bool routingWorks = await _tenantService.IsPrivateCloudUserAsync();

  steps.Add(new
  {
    step = 4,
   name = "Verify Routing",
       status = routingWorks ? "✅ Success" : "❌ Failed",
  success = routingWorks
     });

          _logger.LogInformation("🎉 Complete setup finished for {Email}", userEmail);

         return Ok(new
         {
      success = true,
          message = "Private cloud setup completed successfully",
    userEmail,
        steps,
     summary = new
         {
 totalSteps = steps.Count,
        successfulSteps = steps.Count(s => ((dynamic)s).success),
    tenantRoutingEnabled = routingWorks
             },
            // ✅ NEW: Return selected tables info
tablesConfigured = !string.IsNullOrEmpty(dto.SelectedTables) 
   ? "Custom table selection applied" 
                : "Default tables will be created"
       });
   }
      catch (Exception ex)
       {
    _logger.LogError(ex, "Error in complete setup");
     return StatusCode(500, new
    {
        success = false,
  message = "Error during complete setup",
    error = ex.Message,
       detail = ex.InnerException?.Message,
     stackTrace = ex.StackTrace
      });
     }
        }

   /// <summary>
 /// Validate database schema
    /// </summary>
  [HttpPost("validate-schema")]
        public async Task<ActionResult> ValidateSchema()
        {
  try
            {
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userEmail))
      {
   _logger.LogWarning("Unauthorized validation attempt - no user email in claims");
           return Unauthorized(new { message = "User email not found in token" });
 }

  var result = await _privateCloudService.ValidateDatabaseSchemaAsync(userEmail);

    return Ok(result);
         }
       catch (Exception ex)
    {
 _logger.LogError(ex, "Error validating schema");
   return StatusCode(500, new { message = "Error validating schema", error = ex.Message });
          }
        }

        /// <summary>
    /// ✅ NEW: Migrate ALL 12 tables (data + schema) from Main DB to Private Cloud
     /// Migrates: AuditReports, subuser, Roles, Permissions, RolePermissions, SubuserRoles, Routes, UserRoles, users, Sessions, Commands, logs
 /// </summary>
        [HttpPost("migrate-all-tables")]
      public async Task<ActionResult> MigrateAllTables()
  {
        try
   {
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   if (string.IsNullOrEmpty(userEmail))
     {
    return Unauthorized(new { message = "User email not found in token" });
        }

      _logger.LogInformation("🚀 Starting migration of 12 tables for {Email}", userEmail);

    // Verify user has private cloud
     bool isPrivateCloud = await _tenantService.IsPrivateCloudUserAsync();
    if (!isPrivateCloud)
 {
         return BadRequest(new { message = "Private cloud not enabled" });
 }

   // Create private cloud context
     using var privateContext = await _contextFactory.CreateDbContextAsync();
      var canConnect = await privateContext.Database.CanConnectAsync();
     if (!canConnect)
 {
    _logger.LogError("❌ Cannot connect to private cloud database");
   return StatusCode(500, new { message = "Error connecting to private cloud database" });
           }

       var migrationResults = new Dictionary<string, object>();

  // Get user ID for filtering
        var user = await _context.Users.Where(u => u.user_email == userEmail).FirstOrDefaultAsync();
         if (user == null)
{
      return BadRequest(new { message = "User not found" });
        }

     // ===== STEP 1: Migrate User-Specific Data Tables =====
  
       // 1. Migrate users table (only current user)
        _logger.LogInformation("👤 Migrating users...");
      var users = await _context.Users
      .AsNoTracking()
   .Where(u => u.user_id == user.user_id)
  .ToListAsync();

        int usersMigrated = 0;
         foreach (var usr in users)
{
 var exists = await privateContext.Users
    .AnyAsync(u => u.user_id == usr.user_id);
       if (!exists)
     {
   privateContext.Users.Add(usr);
 usersMigrated++;
    }
       }
        await privateContext.SaveChangesAsync();
  migrationResults["users"] = new { total = users.Count, migrated = usersMigrated };
        _logger.LogInformation("✅ Migrated {Count} users", usersMigrated);

     // 2. Migrate AuditReports
       _logger.LogInformation("📊 Migrating AuditReports...");
   var reports = await _context.AuditReports
     .AsNoTracking()
  .Where(r => r.client_email == userEmail)
      .ToListAsync();

  int reportsMigrated = 0;
     foreach (var report in reports)
       {
   var exists = await privateContext.AuditReports
        .AnyAsync(r => r.report_id == report.report_id);
    if (!exists)
          {
 privateContext.AuditReports.Add(report);
  reportsMigrated++;
   }
       }
   await privateContext.SaveChangesAsync();
       migrationResults["AuditReports"] = new { total = reports.Count, migrated = reportsMigrated };
 _logger.LogInformation("✅ Migrated {Count} AuditReports", reportsMigrated);

      // 3. Migrate Subusers
 _logger.LogInformation("👥 Migrating Subusers...");
   var subusers = await _context.subuser
          .AsNoTracking()
             .Where(s => s.user_email == userEmail)
         .ToListAsync();

             int subusersMigrated = 0;
         foreach (var subuser in subusers)
      {
  var exists = await privateContext.subuser
   .AnyAsync(s => s.subuser_id == subuser.subuser_id);
        if (!exists)
           {
         privateContext.subuser.Add(subuser);
             subusersMigrated++;
          }
      }
 await privateContext.SaveChangesAsync();
          migrationResults["Subusers"] = new { total = subusers.Count, migrated = subusersMigrated };
    _logger.LogInformation("✅ Migrated {Count} Subusers", subusersMigrated);

        // 4. Migrate Machines
        _logger.LogInformation("🖥️ Migrating Machines...");
        var subuserEmails = subusers.Select(s => s.subuser_email).ToList();
   var machines = await _context.Machines
            .AsNoTracking()
     .Where(m => m.user_email == userEmail ||
     (m.subuser_email != null && subuserEmails.Contains(m.subuser_email)))
    .ToListAsync();

   int machinesMigrated = 0;
  foreach (var machine in machines)
   {
   var exists = await privateContext.Machines
      .AnyAsync(m => m.fingerprint_hash == machine.fingerprint_hash);
        if (!exists)
  {
         privateContext.Machines.Add(machine);
         machinesMigrated++;
       }
   }
      await privateContext.SaveChangesAsync();
  migrationResults["Machines"] = new { total = machines.Count, migrated = machinesMigrated };
   _logger.LogInformation("✅ Migrated {Count} Machines", machinesMigrated);

       // 5. Migrate Sessions
      _logger.LogInformation("🔐 Migrating Sessions...");
         var sessions = await _context.Sessions
  .AsNoTracking()
     .Where(s => s.user_email == userEmail)
 .ToListAsync();

  int sessionsMigrated = 0;
   foreach (var session in sessions)
  {
   var exists = await privateContext.Sessions
        .AnyAsync(s => s.session_id == session.session_id);
            if (!exists)
        {
   privateContext.Sessions.Add(session);
           sessionsMigrated++;
  }
 }
   await privateContext.SaveChangesAsync();
       migrationResults["Sessions"] = new { total = sessions.Count, migrated = sessionsMigrated };
        _logger.LogInformation("✅ Migrated {Count} Sessions", sessionsMigrated);

    // 6. Migrate Commands
           _logger.LogInformation("⚡ Migrating Commands...");
     var commands = await _context.Commands
 .AsNoTracking()
   .Where(c => c.user_email == userEmail)
      .ToListAsync();

         int commandsMigrated = 0;
         foreach (var command in commands)
         {
     var exists = await privateContext.Commands
   .AnyAsync(c => c.Command_id == command.Command_id);
    if (!exists)
     {
       privateContext.Commands.Add(command);
 commandsMigrated++;
      }
}
     await privateContext.SaveChangesAsync();
    migrationResults["Commands"] = new { total = commands.Count, migrated = commandsMigrated };
       _logger.LogInformation("✅ Migrated {Count} Commands", commandsMigrated);

      // 7. Migrate logs
_logger.LogInformation("📝 Migrating logs...");
var logs = await _context.logs
        .AsNoTracking()
  .Where(l => l.user_email == userEmail)
  .ToListAsync();

     int logsMigrated = 0;
foreach (var log in logs)
   {
         var exists = await privateContext.logs
     .AnyAsync(l => l.log_id == log.log_id);
  if (!exists)
{
           privateContext.logs.Add(log);
       logsMigrated++;
}
    }
     await privateContext.SaveChangesAsync();
      migrationResults["logs"] = new { total = logs.Count, migrated = logsMigrated };
   _logger.LogInformation("✅ Migrated {Count} logs", logsMigrated);

      // ===== STEP 2: Migrate System Tables (ALL data, not user-specific) =====

                // 8. Migrate Roles (ALL roles)
         _logger.LogInformation("🔧 Migrating Roles...");
        var roles = await _context.Roles.AsNoTracking().ToListAsync();
     int rolesMigrated = 0;
    foreach (var role in roles)
          {
       var exists = await privateContext.Roles
 .AnyAsync(r => r.RoleId == role.RoleId);
        if (!exists)
   {
       privateContext.Roles.Add(role);
      rolesMigrated++;
 }
         }
         await privateContext.SaveChangesAsync();
      migrationResults["Roles"] = new { total = roles.Count, migrated = rolesMigrated };
    _logger.LogInformation("✅ Migrated {Count} Roles", rolesMigrated);

        // 9. Migrate Permissions (ALL permissions)
    _logger.LogInformation("🔐 Migrating Permissions...");
      var permissions = await _context.Permissions.AsNoTracking().ToListAsync();
    int permissionsMigrated = 0;
     foreach (var permission in permissions)
   {
          var exists = await privateContext.Permissions
        .AnyAsync(p => p.PermissionId == permission.PermissionId);
         if (!exists)
  {
         privateContext.Permissions.Add(permission);
    permissionsMigrated++;
              }
 }
       await privateContext.SaveChangesAsync();
      migrationResults["Permissions"] = new { total = permissions.Count, migrated = permissionsMigrated };
       _logger.LogInformation("✅ Migrated {Count} Permissions", permissionsMigrated);

      // 10. Migrate RolePermissions (ALL mappings)
     _logger.LogInformation("🔗 Migrating RolePermissions...");
       var rolePermissions = await _context.RolePermissions.AsNoTracking().ToListAsync();
       int rolePermissionsMigrated = 0;
      foreach (var rp in rolePermissions)
       {
   var exists = await privateContext.RolePermissions
    .AnyAsync(r => r.RoleId == rp.RoleId && r.PermissionId == rp.PermissionId);
         if (!exists)
  {
      privateContext.RolePermissions.Add(rp);
            rolePermissionsMigrated++;
  }
    }
    await privateContext.SaveChangesAsync();
       migrationResults["RolePermissions"] = new { total = rolePermissions.Count, migrated = rolePermissionsMigrated };
    _logger.LogInformation("✅ Migrated {Count} RolePermissions", rolePermissionsMigrated);

       // 11. Migrate SubuserRoles (only for user's subusers)
          _logger.LogInformation("👥 Migrating SubuserRoles...");
     var subuserIds = subusers.Select(s => s.subuser_id).ToList();
      var subuserRoles = await _context.SubuserRoles
     .AsNoTracking()
           .Where(sr => subuserIds.Contains(sr.SubuserId))
       .ToListAsync();

  int subuserRolesMigrated = 0;
         foreach (var sr in subuserRoles)
        {
    var exists = await privateContext.SubuserRoles
    .AnyAsync(r => r.SubuserId == sr.SubuserId && r.RoleId == sr.RoleId);
              if (!exists)
          {
      privateContext.SubuserRoles.Add(sr);
 subuserRolesMigrated++;
          }
        }
     await privateContext.SaveChangesAsync();
        migrationResults["SubuserRoles"] = new { total = subuserRoles.Count, migrated = subuserRolesMigrated };
      _logger.LogInformation("✅ Migrated {Count} SubuserRoles", subuserRolesMigrated);

          // 12. Migrate UserRoles (only current user's roles)
  _logger.LogInformation("🔑 Migrating UserRoles...");
        var userRoles = await _context.UserRoles
  .AsNoTracking()
    .Where(ur => ur.UserId == user.user_id)
 .ToListAsync();

int userRolesMigrated = 0;
     foreach (var ur in userRoles)
       {
 var exists = await privateContext.UserRoles
      .AnyAsync(r => r.UserId == ur.UserId && r.RoleId == ur.RoleId);
  if (!exists)
    {
         privateContext.UserRoles.Add(ur);
     userRolesMigrated++;
 }
 }
        await privateContext.SaveChangesAsync();
        migrationResults["UserRoles"] = new { total = userRoles.Count, migrated = userRolesMigrated };
    _logger.LogInformation("✅ Migrated {Count} UserRoles", userRolesMigrated);

  // 13. Migrate Routes (ALL routes)
        _logger.LogInformation("🛣️ Migrating Routes...");
  var routes = await _context.Routes.AsNoTracking().ToListAsync();
 int routesMigrated = 0;
      foreach (var route in routes)
           {
  var exists = await privateContext.Routes
          .AnyAsync(r => r.RouteId == route.RouteId);
      if (!exists)
    {
    privateContext.Routes.Add(route);
     routesMigrated++;
}
    }
     await privateContext.SaveChangesAsync();
          migrationResults["Routes"] = new { total = routes.Count, migrated = routesMigrated };
  _logger.LogInformation("✅ Migrated {Count} Routes", routesMigrated);

 // ===== STEP 3: Assign Default Manager Role to User =====
  _logger.LogInformation("🔑 Assigning default Manager role to user...");
       
     var managerRole = await privateContext.Roles
 .Where(r => r.RoleName == "Manager").FirstOrDefaultAsync();
        
  int userRolesAssigned = 0;
     if (managerRole != null)
  {
var existingUserRole = await privateContext.UserRoles
   .AnyAsync(ur => ur.UserId == user.user_id && ur.RoleId == managerRole.RoleId);
       
 if (!existingUserRole)
     {
    privateContext.UserRoles.Add(new UserRole
  {
    UserId = user.user_id,
          RoleId = managerRole.RoleId,
   AssignedByEmail = "system",
        AssignedAt = DateTime.UtcNow
     });
        
          await privateContext.SaveChangesAsync();
      userRolesAssigned = 1;
     _logger.LogInformation("✅ Manager role assigned to {Email}", userEmail);
     }
   else
      {
    _logger.LogInformation("ℹ️ User already has Manager role");
         }
 }
  else
     {
       _logger.LogWarning("⚠️ Manager role not found in Private DB");
 }

          _logger.LogInformation("🎉 Migration of all 13 tables complete for {Email}", userEmail);

      return Ok(new
    {
success = true,
    message = "All 13 tables migrated successfully to Private Cloud",
   userEmail,
  migrationResults,
      summary = new
           {
     totalTables = 13,
  userSpecificTables = new[] { "users", "AuditReports", "subuser", "Machines", "Sessions", "Commands", "logs", "SubuserRoles", "UserRoles" },
  systemTablesWithAllData = new[] { "Roles", "Permissions", "RolePermissions", "Routes" },
        totalRecordsMigrated = usersMigrated + reportsMigrated + subusersMigrated + machinesMigrated +
    sessionsMigrated + commandsMigrated + logsMigrated + rolesMigrated + permissionsMigrated + 
rolePermissionsMigrated + subuserRolesMigrated + userRolesMigrated + routesMigrated,
    userRolesAssigned = userRolesAssigned
     }
    });
 }
    catch (Exception ex)
      {
       _logger.LogError(ex, "❌ Error during 13-table migration");
  return StatusCode(500, new
   {
  success = false,
   message = "Error migrating tables",
  error = ex.Message,
       innerError = ex.InnerException?.Message
       });
  }
   }

        /// <summary>
  /// Get required tables list
 /// </summary>
        [HttpGet("required-tables")]
   public async Task<ActionResult> GetRequiredTables()
  {
     try
 {
    var tables = await _privateCloudService.GetRequiredTablesAsync();

     return Ok(new
     {
       tables,
      totalCount = tables.Count,
      description = "These tables will be created in your private database"
      });
            }
     catch (Exception ex)
            {
            _logger.LogError(ex, "Error getting required tables");
     return StatusCode(500, new { message = "Error retrieving tables", error = ex.Message });
   }
  }
 }

  // DTOs
  public class CompleteSetupDto
 {
    public string ConnectionString { get; set; } = string.Empty;
      public string? DatabaseType { get; set; }
        public string? Notes { get; set; }
    
        /// <summary>
      /// ✅ NEW: JSON string containing user's table selection
      /// Example: {"AuditReports": true, "subuser": true, "Roles": true, "SubuserRoles": true, "machines": false}
        /// </summary>
 public string? SelectedTables { get; set; }
  
   public bool MigrateExistingData { get; set; } = false;
    }
}
