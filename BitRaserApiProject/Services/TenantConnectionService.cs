using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace DSecureApi.Services
{
    /// <summary>
  /// Implementation of tenant connection resolver
    /// Determines which database connection to use based on user's is_private_cloud setting
    /// ✅ OPTIMIZED: Uses caching to reduce database load
    /// </summary>
  public class TenantConnectionService : ITenantConnectionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _mainDbContext;
        private readonly IConfiguration _configuration;
      private readonly ILogger<TenantConnectionService> _logger;
      
        // ✅ CACHE: Store connection strings to avoid repeated lookups
        // Key: userEmail, Value: (connectionString, expireTime)
        private static readonly Dictionary<string, (string ConnectionString, DateTime ExpireTime)> _connectionCache = new();
        private static readonly object _cacheLock = new();
        private const int CACHE_DURATION_MINUTES = 5;

     public TenantConnectionService(
    IHttpContextAccessor httpContextAccessor,
  ApplicationDbContext mainDbContext,
        IConfiguration configuration,
            ILogger<TenantConnectionService> logger)
        {
         _httpContextAccessor = httpContextAccessor;
     _mainDbContext = mainDbContext;
       _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
  /// Gets connection string for current authenticated user
        /// </summary>
 public async Task<string> GetConnectionStringAsync()
        {
      var userEmail = GetCurrentUserEmail();
      
      _logger.LogInformation("🔍 GetConnectionStringAsync called - UserEmail from JWT: {Email}", userEmail ?? "NULL");

            if (string.IsNullOrEmpty(userEmail))
       {
        _logger.LogWarning("⚠️ No authenticated user found, using default Main DB");
           return GetDefaultConnectionString();
            }

     var result = await GetConnectionStringForUserAsync(userEmail);
     
     // Log which database was selected
     var mainConn = GetDefaultConnectionString();
     var isPrivateCloud = (result != mainConn && !string.IsNullOrWhiteSpace(result) && result.Contains("Server="));
     _logger.LogInformation("📌 GetConnectionStringAsync result for {Email}: Using {DbType}", 
         userEmail, isPrivateCloud ? "PRIVATE CLOUD DB" : "MAIN DB");
     
     return result;
        }

        /// <summary>
        /// Gets connection string for specific user by email
        /// ✅ ENHANCED: Now handles subusers by checking parent user's private cloud status
        /// </summary>
   public async Task<string> GetConnectionStringForUserAsync(string userEmail)
 {
            try
  {
    // ✅ CACHE CHECK: Return cached connection string if available
    lock (_cacheLock)
    {
        if (_connectionCache.TryGetValue(userEmail.ToLower(), out var cached))
        {
            if (DateTime.UtcNow < cached.ExpireTime)
            {
                _logger.LogDebug("✅ Cache HIT for {Email}", userEmail);
                return cached.ConnectionString;
            }
            else
            {
                _connectionCache.Remove(userEmail.ToLower());
            }
        }
    }
    
    _logger.LogDebug("🔍 Cache MISS for {Email}, resolving connection string...", userEmail);

    // ✅ STEP 1: Check if this email is a subuser in Main DB
    var subuserRecord = await _mainDbContext.subuser
        .AsNoTracking()
        .FirstOrDefaultAsync(s => s.subuser_email == userEmail);

    string effectiveEmail = userEmail;
    bool isSubuser = false;
    string? privateCloudConnectionString = null;

    if (subuserRecord != null)
    {
        // This is a subuser found in Main DB - use parent's email for private cloud lookup
        effectiveEmail = subuserRecord.user_email;
        isSubuser = true;
        _logger.LogInformation("👤 SUBUSER DETECTED in Main DB: {SubuserEmail} → Using parent: {ParentEmail}", userEmail, effectiveEmail);
    }
    else
    {
        _logger.LogInformation("👤 Not found as subuser in Main DB: {Email}, searching Private Cloud DBs...", userEmail);
        
        // ✅ NEW: Search Private Cloud databases for subuser
        var privateCloudUsers = await _mainDbContext.Users
            .AsNoTracking()
            .Where(u => u.is_private_cloud == true)
            .Select(u => new { u.user_email })
            .ToListAsync();
        
        foreach (var pcUser in privateCloudUsers)
        {
            try
            {
                var pcConfig = await _mainDbContext.PrivateCloudDatabases
                    .AsNoTracking()
                    .FirstOrDefaultAsync(p => p.UserEmail == pcUser.user_email && p.IsActive);
                
                if (pcConfig == null || string.IsNullOrEmpty(pcConfig.ConnectionString))
                    continue;
                
                var decryptedConn = DecryptConnectionString(pcConfig.ConnectionString);
                if (string.IsNullOrWhiteSpace(decryptedConn) || !decryptedConn.Contains("Server="))
                    continue;
                
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
                var serverVersion = new MySqlServerVersion(new Version(8, 0, 21));
                optionsBuilder.UseMySql(decryptedConn, serverVersion, mysqlOptions =>
                {
                    mysqlOptions.CommandTimeout(15); // ✅ FIXED: Increased from 5s for cold connections
                    mysqlOptions.EnableRetryOnFailure(2, TimeSpan.FromSeconds(3), null);
                });
                
                using var privateContext = new ApplicationDbContext(optionsBuilder.Options);
                privateContext.Database.SetCommandTimeout(15); // ✅ FIXED: Increased from 5s
                
                var pcSubuser = await privateContext.subuser
                    .AsNoTracking()
                    .FirstOrDefaultAsync(s => s.subuser_email == userEmail);
                
                if (pcSubuser != null)
                {
                    effectiveEmail = pcUser.user_email;
                    isSubuser = true;
                    privateCloudConnectionString = decryptedConn;
                    _logger.LogInformation("✅ SUBUSER FOUND in Private Cloud DB: {SubuserEmail} → Parent: {ParentEmail}", userEmail, effectiveEmail);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "⚠️ Failed to search Private Cloud DB for user {Email}", pcUser.user_email);
            }
        }
        
        // If found in Private Cloud DB, return that connection string directly
        if (privateCloudConnectionString != null)
        {
            _logger.LogInformation("🔌 Returning Private Cloud connection for subuser: {SubuserEmail}", userEmail);
            return CacheAndReturn(userEmail, privateCloudConnectionString);
        }
    }

   // ✅ STEP 2: Query Main DB to get user configuration (using effective email)
      var user = await _mainDbContext.Users
  .AsNoTracking()
            .FirstOrDefaultAsync(u => u.user_email == effectiveEmail);

  if (user == null)
      {
            _logger.LogWarning("❌ User {Email} not found in users table, using Main DB", effectiveEmail);
           return GetDefaultConnectionString();
           }

    _logger.LogInformation("✅ User found: {Email}, is_private_cloud = {IsPrivate}", effectiveEmail, user.is_private_cloud);

   // ✅ STEP 3: Check if user has private cloud enabled
                if (user.is_private_cloud == true)
        {
  _logger.LogInformation("User {Email} has private cloud enabled{SubuserInfo}, checking for connection string", 
      effectiveEmail, isSubuser ? $" (subuser: {userEmail})" : "");

         // ✅ Try to get connection string from PrivateCloudDatabases table (using effectiveEmail for parent)
         // ✅ FIX: Use case-insensitive comparison
         var privateCloudConfig = await _mainDbContext.PrivateCloudDatabases
             .AsNoTracking()
             .FirstOrDefaultAsync(p => p.UserEmail.ToLower() == effectiveEmail.ToLower() && p.IsActive);
         
         // ✅ If not found, try without IsActive check to see if config exists but is inactive
         if (privateCloudConfig == null)
         {
             var anyConfig = await _mainDbContext.PrivateCloudDatabases
                 .AsNoTracking()
                 .FirstOrDefaultAsync(p => p.UserEmail.ToLower() == effectiveEmail.ToLower());
             
             if (anyConfig != null)
             {
                 _logger.LogWarning("⚠️ Private Cloud config found for {Email} but IsActive={IsActive} (might be inactive)", effectiveEmail, anyConfig.IsActive);
             }
             else
             {
                 _logger.LogWarning("⚠️ No Private Cloud config found in PrivateCloudDatabases table for {Email}", effectiveEmail);
             }
         }
         else
         {
             _logger.LogInformation("✅ Private Cloud config found for {Email}: IsActive={IsActive}, HasConnectionString={HasConn}", 
                 effectiveEmail, privateCloudConfig.IsActive, !string.IsNullOrEmpty(privateCloudConfig.ConnectionString));
         }

          if (privateCloudConfig != null && !string.IsNullOrEmpty(privateCloudConfig.ConnectionString))
           {
     try
     {
       // ✅ FIX: Decrypt the connection string before using it
    _logger.LogInformation("Decrypting private cloud connection for user {Email}{SubuserInfo}", 
        effectiveEmail, isSubuser ? $" (subuser: {userEmail})" : "");
        var decryptedConnectionString = DecryptConnectionString(privateCloudConfig.ConnectionString);
           
           // Validate decrypted connection string
        if (string.IsNullOrWhiteSpace(decryptedConnectionString))
              {
          _logger.LogError("Decrypted connection string is empty for user {Email}", effectiveEmail);
     return CacheAndReturn(userEmail, GetDefaultConnectionString());
    }

        _logger.LogInformation("✅ Using decrypted private cloud connection for user {Email}{SubuserInfo}", 
            effectiveEmail, isSubuser ? $" (subuser: {userEmail})" : "");
        return CacheAndReturn(userEmail, decryptedConnectionString);
       }
     catch (FormatException formatEx)
       {
     _logger.LogError(formatEx, "❌ Invalid connection string format for user {Email}: {Message}", effectiveEmail, formatEx.Message);
     return CacheAndReturn(userEmail, GetDefaultConnectionString());
            }
  catch (CryptographicException cryptoEx)
{
    _logger.LogError(cryptoEx, "❌ Failed to decrypt connection string for user {Email}: {Message}", effectiveEmail, cryptoEx.Message);
  return CacheAndReturn(userEmail, GetDefaultConnectionString());
       }
       catch (Exception ex)
        {
         _logger.LogError(ex, "❌ Error using private cloud connection for user {Email}, falling back to Main DB", effectiveEmail);
         return CacheAndReturn(userEmail, GetDefaultConnectionString());
       }
             }
  else
     {
       _logger.LogWarning("User {Email} has private cloud enabled but no valid configuration found, using Main DB", effectiveEmail);
               return CacheAndReturn(userEmail, GetDefaultConnectionString());
         }
    }
      else
  {
        _logger.LogInformation("User {Email} using default Main DB", effectiveEmail);
 return CacheAndReturn(userEmail, GetDefaultConnectionString());
         }
            }
    catch (Exception ex)
          {
   _logger.LogError(ex, "Error resolving connection string for user {Email}, using default Main DB", userEmail);
    return CacheAndReturn(userEmail, GetDefaultConnectionString());
  }
        }
        
        /// <summary>
        /// ✅ HELPER: Cache the connection string and return it
        /// </summary>
        private string CacheAndReturn(string userEmail, string connectionString)
        {
            lock (_cacheLock)
            {
                _connectionCache[userEmail.ToLower()] = (connectionString, DateTime.UtcNow.AddMinutes(CACHE_DURATION_MINUTES));
            }
            return connectionString;
        }
        
        /// <summary>
        /// ✅ HELPER: Clear cache for a specific user (useful after profile updates)
        /// </summary>
        public void ClearCacheForUser(string userEmail)
        {
            lock (_cacheLock)
            {
                _connectionCache.Remove(userEmail.ToLower());
            }
        }

        /// <summary>
        /// Gets current user's email from JWT token claims
      /// </summary>
  public string? GetCurrentUserEmail()
        {
        var httpContext = _httpContextAccessor.HttpContext;
            
            if (httpContext?.User?.Identity?.IsAuthenticated != true)
  {
    _logger.LogWarning("No authenticated user in HttpContext");
                return null;
            }

       // Try multiple claim types for email
            var email = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
     ?? httpContext.User.FindFirst(ClaimTypes.Email)?.Value
         ?? httpContext.User.FindFirst("email")?.Value
?? httpContext.User.FindFirst("sub")?.Value
        ?? httpContext.User.FindFirst("user_email")?.Value;

         if (string.IsNullOrEmpty(email))
     {
   _logger.LogWarning("No email claim found in user token");
            }

  return email;
        }

  /// <summary>
 /// Checks if current user has private cloud enabled
 /// ✅ ENHANCED: Now handles subusers by checking parent user's is_private_cloud flag
        /// </summary>
        public async Task<bool> IsPrivateCloudUserAsync()
        {
       var userEmail = GetCurrentUserEmail();
            
  if (string.IsNullOrEmpty(userEmail))
         {
              return false;
    }

    // ✅ Check if this is a subuser and get parent email
    var subuserRecord = await _mainDbContext.subuser
        .AsNoTracking()
        .FirstOrDefaultAsync(s => s.subuser_email == userEmail);

    string effectiveEmail = subuserRecord != null ? subuserRecord.user_email : userEmail;

            var user = await _mainDbContext.Users
                .AsNoTracking()
              .FirstOrDefaultAsync(u => u.user_email == effectiveEmail);

            return user?.is_private_cloud == true;
    }

      /// <summary>
        /// Gets the default Main Database connection string from configuration
 /// </summary>
        private string GetDefaultConnectionString()
        {
      var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__ApplicationDbContextConnection")
          ?? _configuration.GetConnectionString("ApplicationDbContextConnection")
     ?? _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrEmpty(connectionString))
  {
      _logger.LogError("No default connection string found in configuration");
        throw new InvalidOperationException("Default connection string not configured");
}

  return connectionString;
        }

        /// <summary>
        /// Decrypts the encrypted connection string using AES encryption
        /// IMPORTANT: Implement your own encryption/decryption logic here
/// </summary>
        private string DecryptConnectionString(string encryptedConnectionString)
      {
            try
  {
   // Get encryption key and IV from configuration
     var encryptionKey = _configuration["Encryption:Key"];
        var encryptionIV = _configuration["Encryption:IV"];

           if (string.IsNullOrEmpty(encryptionKey) || string.IsNullOrEmpty(encryptionIV))
          {
 _logger.LogError("❌ Encryption key or IV not configured in appsettings.json");
    throw new InvalidOperationException("Encryption key or IV not configured");
         }

  _logger.LogDebug("🔐 Decryption Key length: {KeyLen}, IV length: {IVLen}", 
             encryptionKey.Length, encryptionIV.Length);

    using var aes = Aes.Create();
     aes.Key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
    aes.IV = Encoding.UTF8.GetBytes(encryptionIV.PadRight(16).Substring(0, 16));

         var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

     using var msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedConnectionString));
       using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
    using var srDecrypt = new StreamReader(csDecrypt);

        var decryptedConnectionString = srDecrypt.ReadToEnd();
         
        // ✅ Validate decrypted connection string format
       if (string.IsNullOrWhiteSpace(decryptedConnectionString))
       {
     _logger.LogError("❌ Decrypted connection string is empty");
         throw new InvalidOperationException("Decrypted connection string is empty");
             }
        
        // ✅ Basic validation - check if it looks like a connection string
      if (!decryptedConnectionString.Contains("="))
 {
    _logger.LogError("❌ Decrypted string doesn't look like a connection string");
 throw new FormatException("Decrypted string is not a valid connection string format");
    }
          
       _logger.LogDebug("✅ Connection string decrypted successfully (length: {Length})", decryptedConnectionString.Length);
    return decryptedConnectionString;
            }
   catch (FormatException formatEx)
   {
         _logger.LogError(formatEx, "❌ Invalid Base64 format in encrypted connection string");
            throw;
        }
            catch (CryptographicException cryptoEx)
            {
    _logger.LogError(cryptoEx, "❌ Cryptographic error decrypting connection string - possible key mismatch");
                throw;
   }
 catch (Exception ex)
     {
      _logger.LogError(ex, "❌ Error decrypting connection string");
     throw new InvalidOperationException("Failed to decrypt connection string", ex);
            }
  }

        /// <summary>
        /// Helper method to encrypt connection strings (for setup/configuration)
      /// </summary>
        public string EncryptConnectionString(string plainTextConnectionString)
        {
 try
  {
   var encryptionKey = _configuration["Encryption:Key"];
           var encryptionIV = _configuration["Encryption:IV"];

        if (string.IsNullOrEmpty(encryptionKey) || string.IsNullOrEmpty(encryptionIV))
        {
  throw new InvalidOperationException("Encryption key or IV not configured");
     }

       using var aes = Aes.Create();
            aes.Key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
                aes.IV = Encoding.UTF8.GetBytes(encryptionIV.PadRight(16).Substring(0, 16));

        var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
    
    using var msEncrypt = new MemoryStream();
   using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
      using (var swEncrypt = new StreamWriter(csEncrypt))
      {
         swEncrypt.Write(plainTextConnectionString);
        }

  return Convert.ToBase64String(msEncrypt.ToArray());
     }
    catch (Exception ex)
     {
          _logger.LogError(ex, "Error encrypting connection string");
 throw new InvalidOperationException("Failed to encrypt connection string", ex);
    }
  }
    }
}
