using Microsoft.EntityFrameworkCore;
using DSecureApi.Models;

namespace DSecureApi.Services
{
    /// <summary>
    /// Factory to create appropriate database context based on user's private cloud status
    /// </summary>
    public interface IDatabaseContextFactory
    {
        /// <summary>
        /// Get database context for specific user (private or shared)
        /// </summary>
    Task<ApplicationDbContext> GetDbContextForUserAsync(string userEmail);
        
        /// <summary>
        /// Get main/shared database context
        /// </summary>
        ApplicationDbContext GetMainDbContext();
        
        /// <summary>
        /// Check if user has private cloud enabled
        /// </summary>
        Task<bool> IsPrivateCloudUserAsync(string userEmail);
    }

    public class DatabaseContextFactory : IDatabaseContextFactory
    {
        private readonly ApplicationDbContext _mainContext;
        private readonly IPrivateCloudService _privateCloudService;
        private readonly ILogger<DatabaseContextFactory> _logger;
        private readonly IConfiguration _configuration;

        // Cache for private DB contexts (per user)
   private static readonly Dictionary<string, ApplicationDbContext> _privateContextCache = new();

public DatabaseContextFactory(
            ApplicationDbContext mainContext,
            IPrivateCloudService privateCloudService,
   ILogger<DatabaseContextFactory> logger,
            IConfiguration configuration)
      {
    _mainContext = mainContext;
            _privateCloudService = privateCloudService;
 _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
      /// Get appropriate database context for user
        /// Returns private DB context if user has private cloud enabled, otherwise main DB
   /// </summary>
        public async Task<ApplicationDbContext> GetDbContextForUserAsync(string userEmail)
     {
   try
            {
    _logger.LogInformation("Getting DB context for user: {Email}", userEmail);

   // Check if user has private cloud enabled
        var isPrivateCloudUser = await IsPrivateCloudUserAsync(userEmail);

              if (!isPrivateCloudUser)
                {
   _logger.LogInformation("User {Email} is NOT private cloud user, using main DB", userEmail);
    return _mainContext;
         }

             // Check cache first
       if (_privateContextCache.TryGetValue(userEmail, out var cachedContext))
    {
         _logger.LogInformation("Using cached private DB context for {Email}", userEmail);
   return cachedContext;
    }

       // Get user's private database configuration
    var privateDbConfig = await _privateCloudService.GetUserPrivateDatabaseAsync(userEmail);

      if (privateDbConfig == null || !privateDbConfig.IsActive)
        {
   _logger.LogWarning("User {Email} has private cloud enabled but no active config found, using main DB", userEmail);
   return _mainContext;
         }

                if (!privateDbConfig.SchemaInitialized)
    {
              _logger.LogWarning("User {Email} private DB schema not initialized, using main DB", userEmail);
     return _mainContext;
 }

      // Create private DB context
         _logger.LogInformation("Creating private DB context for {Email}", userEmail);
       var privateContext = await CreatePrivateDbContextAsync(userEmail, privateDbConfig);

    // Cache it
    _privateContextCache[userEmail] = privateContext;

                _logger.LogInformation("✅ RETURNING PRIVATE DB CONTEXT for {Email} (Host: {Host}, DB: {Db})", 
                    userEmail, privateDbConfig.ServerHost, privateDbConfig.DatabaseName);
                return privateContext;
       }
     catch (Exception ex)
      {
        _logger.LogError(ex, "Error getting DB context for {Email}, falling back to main DB", userEmail);
     return _mainContext;
  }
        }

        /// <summary>
        /// Get main/shared database context
    /// </summary>
        public ApplicationDbContext GetMainDbContext()
        {
            return _mainContext;
        }

        /// <summary>
   /// Check if user is private cloud user
     /// </summary>
        public async Task<bool> IsPrivateCloudUserAsync(string userEmail)
      {
            return await _privateCloudService.IsPrivateCloudUserAsync(userEmail);
    }

        /// <summary>
        /// Create ApplicationDbContext for user's private database
        /// </summary>
        private async Task<ApplicationDbContext> CreatePrivateDbContextAsync(
 string userEmail, 
    PrivateCloudDatabase privateDbConfig)
        {
     try
     {
     // Get decrypted connection string
                var connectionString = await _privateCloudService.GetConnectionStringAsync(userEmail);

            // ✅ Parse MySQL URI format if needed (e.g., mysql://user:pass@host:port/db)
     connectionString = ParseConnectionString(connectionString, privateDbConfig.DatabaseType);

  _logger.LogInformation("Creating DB context for private database: {Type} - {Host}", 
        privateDbConfig.DatabaseType, privateDbConfig.ServerHost);

    // Create options builder
      var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

    // Configure based on database type
  switch (privateDbConfig.DatabaseType.ToLower())
 {
     case "mysql":
    optionsBuilder.UseMySql(
  connectionString,
 new MySqlServerVersion(new Version(8, 0, 21)),
      options => options.EnableRetryOnFailure(
       maxRetryCount: 3,
  maxRetryDelay: TimeSpan.FromSeconds(5),
  errorNumbersToAdd: null
 )
  );
       break;

    case "postgresql":
       // PostgreSQL support - requires Npgsql.EntityFrameworkCore.PostgreSQL package
           // To enable: dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
         _logger.LogWarning("PostgreSQL support not configured. Install Npgsql.EntityFrameworkCore.PostgreSQL package.");
         throw new NotSupportedException("PostgreSQL support requires Npgsql package. Use MySQL for now.");
  // Uncomment after installing package:
           // optionsBuilder.UseNpgsql(connectionString);
 break;

       case "sqlserver":
  // SQL Server support - requires Microsoft.EntityFrameworkCore.SqlServer package
   // To enable: dotnet add package Microsoft.EntityFrameworkCore.SqlServer
         _logger.LogWarning("SQL Server support not configured. Install Microsoft.EntityFrameworkCore.SqlServer package.");
        throw new NotSupportedException("SQL Server support requires SqlServer package. Use MySQL for now.");
  // Uncomment after installing package:
  // optionsBuilder.UseSqlServer(connectionString);
  break;

      default:
 throw new NotSupportedException($"Database type {privateDbConfig.DatabaseType} not supported");
              }

 // Enable sensitive data logging in development
          if (_configuration.GetValue<string>("ASPNETCORE_ENVIRONMENT") == "Development")
      {
      optionsBuilder.EnableSensitiveDataLogging();
      optionsBuilder.EnableDetailedErrors();
   }

       var context = new ApplicationDbContext(optionsBuilder.Options);

            _logger.LogInformation("✅ Private DB context created successfully for {Email}", userEmail);

  return context;
    }
            catch (Exception ex)
  {
   _logger.LogError(ex, "Error creating private DB context for {Email}", userEmail);
          throw;
       }
      }

        /// <summary>
        /// Parse connection string - supports both standard and URI formats
        /// Converts MySQL URI format (mysql://user:pass@host:port/db) to standard format
        /// </summary>
     private string ParseConnectionString(string connectionString, string databaseType)
        {
            try
      {
     // Check if it's a URI format (starts with mysql://, postgresql://, etc.)
           if (connectionString.StartsWith("mysql://", StringComparison.OrdinalIgnoreCase) ||
       connectionString.StartsWith("mariadb://", StringComparison.OrdinalIgnoreCase))
 {
           _logger.LogInformation("Detected MySQL URI format connection string, converting...");
        return ConvertMySqlUriToStandard(connectionString);
}
      else if (connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase) ||
   connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase))
         {
    _logger.LogInformation("Detected PostgreSQL URI format connection string");
   // PostgreSQL URIs are natively supported by Npgsql
        return connectionString;
   }
           else
         {
           // Already in standard format (server=xxx;database=xxx;...)
         _logger.LogInformation("Connection string is in standard format");
    return connectionString;
                }
            }
      catch (Exception ex)
       {
   _logger.LogError(ex, "Error parsing connection string, using as-is");
    return connectionString;
   }
        }

    /// <summary>
        /// Convert MySQL URI format to standard connection string
        /// Format: mysql://username:password@host:port/database?options
      /// Example: mysql://root:password@localhost:3306/mydb?ssl-mode=Required
        /// </summary>
        private string ConvertMySqlUriToStandard(string uri)
        {
         try
         {
// Remove protocol prefix
      var uriWithoutProtocol = uri.Replace("mysql://", "").Replace("mariadb://", "");

    // Parse components
            string username = "";
  string password = "";
      string host = "";
     int port = 3306; // Default MySQL port
        string database = "";
   var options = new Dictionary<string, string>();

                // Split by @ to separate credentials from host
           var parts = uriWithoutProtocol.Split('@');
    
                if (parts.Length == 2)
      {
    // Parse credentials (username:password)
          var credentials = parts[0];
       var credParts = credentials.Split(':');
             username = credParts[0];
  password = credParts.Length > 1 ? credParts[1] : "";

                    // Parse host and database
        var hostAndDb = parts[1];
            
          // Check for query parameters
          var queryIndex = hostAndDb.IndexOf('?');
       if (queryIndex > 0)
            {
       // Parse query parameters
      var queryString = hostAndDb.Substring(queryIndex + 1);
     hostAndDb = hostAndDb.Substring(0, queryIndex);
          
    foreach (var param in queryString.Split('&'))
 {
          var keyValue = param.Split('=');
    if (keyValue.Length == 2)
     {
             options[keyValue[0]] = keyValue[1];
          }
  }
        }

     // Parse host:port/database
         var hostPortDb = hostAndDb.Split('/');
        if (hostPortDb.Length > 0)
   {
              var hostPort = hostPortDb[0].Split(':');
             host = hostPort[0];
   if (hostPort.Length > 1 && int.TryParse(hostPort[1], out var parsedPort))
    {
        port = parsedPort;
      }
           }
            
      if (hostPortDb.Length > 1)
         {
   database = hostPortDb[1];
        }
 }
          else
         {
   throw new FormatException("Invalid MySQL URI format. Expected: mysql://user:pass@host:port/db");
     }

     // Build standard connection string
       var connStrBuilder = new System.Text.StringBuilder();
     connStrBuilder.Append($"Server={host};");
    connStrBuilder.Append($"Port={port};");
    connStrBuilder.Append($"Database={database};");
      connStrBuilder.Append($"User={username};");
   connStrBuilder.Append($"Password={password};");
                connStrBuilder.Append("AllowUserVariables=true;");
             
       // Add SSL mode (default to Required for TiDB)
        if (options.ContainsKey("ssl-mode"))
   {
      connStrBuilder.Append($"SslMode={options["ssl-mode"]};");
            }
 else
    {
                  connStrBuilder.Append("SslMode=Required;");
         }

        // Add other common options
        if (options.ContainsKey("charset"))
                {
    connStrBuilder.Append($"CharSet={options["charset"]};");
       }

                var standardConnStr = connStrBuilder.ToString();
       
                _logger.LogInformation("✅ Converted MySQL URI to standard format");
    _logger.LogDebug("Original: {Original}", uri);
         _logger.LogDebug("Converted: {Converted}", 
      System.Text.RegularExpressions.Regex.Replace(standardConnStr, 
       @"(password|pwd)=([^;]*)", "$1=***", 
        System.Text.RegularExpressions.RegexOptions.IgnoreCase));

     return standardConnStr;
          }
            catch (Exception ex)
   {
        _logger.LogError(ex, "Failed to parse MySQL URI: {Uri}", uri);
          throw new FormatException($"Invalid MySQL URI format: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Clear cache for specific user (call when user's private DB config changes)
        /// </summary>
     public static void ClearCacheForUser(string userEmail)
        {
 if (_privateContextCache.ContainsKey(userEmail))
    {
       _privateContextCache[userEmail].Dispose();
   _privateContextCache.Remove(userEmail);
         }
        }

      /// <summary>
        /// Clear all cached contexts
     /// </summary>
   public static void ClearAllCache()
    {
        foreach (var context in _privateContextCache.Values)
            {
           context.Dispose();
            }
            _privateContextCache.Clear();
   }
    }
}
