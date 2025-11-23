using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BitRaserApiProject.Models;
using BitRaserApiProject.Services;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace BitRaserApiProject.Controllers
{
  /// <summary>
  /// Private Cloud Database Management Controller
    /// Allows users with is_private_cloud=true to configure their own database
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class PrivateCloudController : ControllerBase
    {
        private readonly IPrivateCloudService _privateCloudService;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PrivateCloudController> _logger;

        public PrivateCloudController(
     IPrivateCloudService privateCloudService,
    ApplicationDbContext context,
  ILogger<PrivateCloudController> logger)
   {
    _privateCloudService = privateCloudService;
       _context = context;
            _logger = logger;
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
      var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
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
         // Get user email from JWT token
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
  if (string.IsNullOrEmpty(userEmail))
  {
     _logger.LogWarning("Unauthorized setup attempt - no user email in claims");
  return Unauthorized(new { message = "User email not found in token" });
  }

   // Check if user has private cloud enabled
var user = await _context.Users.FirstOrDefaultAsync(u => u.user_email == userEmail);
     if (user == null || user.is_private_cloud != true)
 {
 _logger.LogWarning("Private cloud not enabled for user: {Email}", userEmail);
  return BadRequest(new
     {
   message = "Private cloud feature not enabled for this user",
  hint = "Please contact administrator to enable private cloud access",
       userEmail = userEmail,
       isPrivateCloud = user?.is_private_cloud
       });
     }

     _logger.LogInformation("🔧 Simplified setup for user: {Email}", userEmail);
      _logger.LogInformation("📋 Connection string length: {Length}", dto.ConnectionString?.Length ?? 0);
      _logger.LogInformation("📋 Database type: {Type}", dto.DatabaseType ?? "Auto-detect");

       // Validate connection string
        if (string.IsNullOrWhiteSpace(dto.ConnectionString))
   {
     return BadRequest(new
    {
     message = "Connection string is required",
      detail = "Please provide a valid connection string"
      });
        }

// Call service to parse and setup
        _logger.LogInformation("📞 Calling SetupPrivateDatabaseFromConnectionStringAsync...");
        
  bool success;
        try
        {
            success = await _privateCloudService.SetupPrivateDatabaseFromConnectionStringAsync(
     userEmail,
     dto.ConnectionString,
    dto.DatabaseType,
        dto.Notes
     );
        }
        catch (FormatException formatEx)
     {
      _logger.LogError(formatEx, "Format exception during setup");
          return BadRequest(new
            {
          message = "Invalid connection string format",
        error = formatEx.Message,
     hint = "Expected format: mysql://user:pass@host:port/database or Server=...;Port=...;Database=...;User=...;Password=...;"
  });
  }
   catch (Exception serviceEx)
{
      _logger.LogError(serviceEx, "Service exception during setup");
            return StatusCode(500, new
  {
      message = "Error during database setup",
       error = serviceEx.Message,
           detail = serviceEx.InnerException?.Message,
    stackTrace = serviceEx.StackTrace
   });
  }

            if (success)
   {
       _logger.LogInformation("✅ Private database setup successful for {Email}", userEmail);
     return Ok(new
    {
      message = "Private database configured successfully",
   detail = "Connection string parsed and database configured",
  nextStep = "Test the connection using /test endpoint",
       userEmail = userEmail
     });
     }

       _logger.LogWarning("❌ Setup returned false for {Email}", userEmail);
     return BadRequest(new
      {
           message = "Failed to configure private database",
       detail = "Please check the application logs for more details",
  userEmail = userEmail,
     hint = "Verify your connection string format and database credentials"
     });
    }
   catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error in simplified setup for {Email}", 
      User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
       return StatusCode(500, new
      {
         message = "Unexpected error during setup",
  error = ex.Message,
    detail = ex.InnerException?.Message,
   stackTrace = ex.StackTrace
 });
         }
        }

  /// <summary>
        /// Test database connection
   /// </summary>
        [HttpPost("test")]
 public async Task<ActionResult<DatabaseTestResult>> TestConnection()
    {
  try
        {
           // ✅ FIX: Use ClaimTypes.NameIdentifier
       var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
      if (string.IsNullOrEmpty(userEmail))
       {
            _logger.LogWarning("Unauthorized test attempt - no user email in claims");
     return Unauthorized(new { message = "User email not found in token" });
     }

     var result = await _privateCloudService.TestDatabaseConnectionAsync(userEmail);

    return Ok(result);
            }
    catch (Exception ex)
            {
            _logger.LogError(ex, "Error testing database connection");
     return StatusCode(500, new { message = "Error testing connection", error = ex.Message });
            }
        }

        /// <summary>
      /// Initialize database schema in private database
        /// </summary>
        [HttpPost("initialize-schema")]
  public async Task<ActionResult> InitializeSchema()
     {
            try
        {
       // ✅ FIX: Use ClaimTypes.NameIdentifier
     var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userEmail))
          {
        _logger.LogWarning("Unauthorized schema init attempt - no user email in claims");
        return Unauthorized(new { message = "User email not found in token" });
                }

var success = await _privateCloudService.InitializeDatabaseSchemaAsync(userEmail);

    if (success)
    {
   return Ok(new 
         { 
          message = "Database schema initialized successfully",
       note = "All required tables have been created in your private database"
     });
         }

            return BadRequest(new { message = "Failed to initialize database schema" });
        }
            catch (Exception ex)
{
           _logger.LogError(ex, "Error initializing database schema");
                return StatusCode(500, new { message = "Error during schema initialization", error = ex.Message });
      }
        }

        /// <summary>
        /// Get setup wizard steps for frontend
        /// </summary>
        [HttpGet("setup-wizard")]
        public ActionResult GetSetupWizard()
     {
          var steps = new object[]
  {
    new 
     {
         step = 1,
    title = "Database Type",
   description = "Select your database type (MySQL, PostgreSQL, SQL Server)",
  fields = new object[] 
   {
      new { name = "databaseType", type = "select", options = new[] { "mysql", "postgresql", "sqlserver" }, required = true }
               }
   },
         new 
         {
    step = 2,
      title = "Database Connection",
  description = "Enter your database connection details",
           fields = new object[] 
  {
     new { name = "serverHost", type = "text", placeholder = "localhost or IP address", required = true },
           new { name = "serverPort", type = "number", placeholder = "3306 (MySQL) or 5432 (PostgreSQL)", required = true },
     new { name = "databaseName", type = "text", placeholder = "your_database_name", required = true },
               new { name = "databaseUsername", type = "text", placeholder = "database_user", required = true },
     new { name = "databasePassword", type = "password", placeholder = "database_password", required = true }
  }
     },
     new 
         {
        step = 3,
     title = "Test Connection",
     description = "Verify that the database connection is working",
          action = "POST /api/PrivateCloud/test"
      },
        new 
  {
       step = 4,
              title = "Initialize Schema",
 description = "Create required tables in your private database",
   action = "POST /api/PrivateCloud/initialize-schema"
              },
         new 
 {
     step = 5,
    title = "Complete Setup",
       description = "Your private database is ready to use!",
         note = "All your reports, subusers, and machines will now be stored in your private database"
     }
    };

          return Ok(new 
            { 
        steps,
         currentStep = 1,
    totalSteps = steps.Length
         });
        }

  /// <summary>
        /// Check if user has private cloud access
        /// </summary>
   [HttpGet("check-access")]
        public async Task<ActionResult> CheckAccess()
     {
            try
 {
   // ✅ FIX: Use ClaimTypes.NameIdentifier
       var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    if (string.IsNullOrEmpty(userEmail))
    {
          _logger.LogWarning("Unauthorized access check - no user email in claims");
         return Unauthorized(new { message = "User email not found in token" });
   }

       var hasAccess = await _privateCloudService.IsPrivateCloudUserAsync(userEmail);
                var config = await _privateCloudService.GetUserPrivateDatabaseAsync(userEmail);

 return Ok(new 
      {
              hasPrivateCloudAccess = hasAccess,
isConfigured = config != null,
   isSchemaInitialized = config?.SchemaInitialized ?? false,
    lastTested = config?.LastTestedAt,
       testStatus = config?.TestStatus,
   databaseType = config?.DatabaseType,
         currentUser = userEmail
     });
      }
            catch (Exception ex)
            {
    _logger.LogError(ex, "Error checking private cloud access");
    return StatusCode(500, new { message = "Error checking access", error = ex.Message });
            }
        }

        /// <summary>
        /// Delete private cloud configuration
   /// </summary>
        [HttpDelete("config")]
        public async Task<ActionResult> DeleteConfig()
      {
   try
            {
            // ✅ FIX: Use ClaimTypes.NameIdentifier
      var userEmail = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
          if (string.IsNullOrEmpty(userEmail))
   {
     _logger.LogWarning("Unauthorized delete attempt - no user email in claims");
  return Unauthorized(new { message = "User email not found in token" });
     }

     var config = await _privateCloudService.GetUserPrivateDatabaseAsync(userEmail);
      if (config == null)
               return NotFound(new { message = "No configuration found" });

    _context.Set<PrivateCloudDatabase>().Remove(config);
      await _context.SaveChangesAsync();

      return Ok(new { message = "Private database configuration removed successfully" });
          }
            catch (Exception ex)
            {
         _logger.LogError(ex, "Error deleting private cloud config");
            return StatusCode(500, new { message = "Error removing configuration", error = ex.Message });
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
      // ✅ FIX: Use ClaimTypes.NameIdentifier
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
}
