using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace BitRaserApiProject.Services
{
    /// <summary>
  /// Implementation of tenant connection resolver
    /// Determines which database connection to use based on user's is_private_cloud setting
    /// </summary>
  public class TenantConnectionService : ITenantConnectionService
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _mainDbContext;
        private readonly IConfiguration _configuration;
      private readonly ILogger<TenantConnectionService> _logger;

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

            if (string.IsNullOrEmpty(userEmail))
       {
        _logger.LogWarning("No authenticated user found, using default Main DB");
           return GetDefaultConnectionString();
            }

     return await GetConnectionStringForUserAsync(userEmail);
        }

        /// <summary>
        /// Gets connection string for specific user by email
        /// </summary>
   public async Task<string> GetConnectionStringForUserAsync(string userEmail)
 {
            try
  {
    _logger.LogInformation("Resolving connection string for user: {Email}", userEmail);

   // Query Main DB to get user configuration
      var user = await _mainDbContext.Users
  .AsNoTracking()
            .FirstOrDefaultAsync(u => u.user_email == userEmail);

  if (user == null)
      {
            _logger.LogWarning("User {Email} not found, using default Main DB", userEmail);
           return GetDefaultConnectionString();
           }

   // Check if user has private cloud enabled
                if (user.is_private_cloud == true)
        {
  _logger.LogInformation("User {Email} has private cloud enabled, checking for connection string", userEmail);

         // Try to get connection string from PrivateCloudDatabases table
          var privateCloudConfig = await _mainDbContext.PrivateCloudDatabases
       .AsNoTracking()
       .FirstOrDefaultAsync(p => p.UserEmail == userEmail && p.IsActive);

          if (privateCloudConfig != null && !string.IsNullOrEmpty(privateCloudConfig.ConnectionString))
           {
     try
     {
       // ✅ FIX: Decrypt the connection string before using it
    _logger.LogInformation("Decrypting private cloud connection for user {Email}", userEmail);
        var decryptedConnectionString = DecryptConnectionString(privateCloudConfig.ConnectionString);
           
           // Validate decrypted connection string
        if (string.IsNullOrWhiteSpace(decryptedConnectionString))
              {
          _logger.LogError("Decrypted connection string is empty for user {Email}", userEmail);
     return GetDefaultConnectionString();
    }

        _logger.LogInformation("✅ Using decrypted private cloud connection for user {Email}", userEmail);
        return decryptedConnectionString;
       }
     catch (FormatException formatEx)
       {
     _logger.LogError(formatEx, "❌ Invalid connection string format for user {Email}: {Message}", userEmail, formatEx.Message);
     return GetDefaultConnectionString();
            }
  catch (CryptographicException cryptoEx)
{
    _logger.LogError(cryptoEx, "❌ Failed to decrypt connection string for user {Email}: {Message}", userEmail, cryptoEx.Message);
  return GetDefaultConnectionString();
       }
       catch (Exception ex)
        {
         _logger.LogError(ex, "❌ Error using private cloud connection for user {Email}, falling back to Main DB", userEmail);
         return GetDefaultConnectionString();
       }
             }
  else
     {
       _logger.LogWarning("User {Email} has private cloud enabled but no configuration found, using Main DB", userEmail);
               return GetDefaultConnectionString();
         }
    }
      else
  {
        _logger.LogInformation("User {Email} using default Main DB", userEmail);
 return GetDefaultConnectionString();
         }
            }
    catch (Exception ex)
          {
   _logger.LogError(ex, "Error resolving connection string for user {Email}, using default Main DB", userEmail);
    return GetDefaultConnectionString();
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
        /// </summary>
        public async Task<bool> IsPrivateCloudUserAsync()
        {
       var userEmail = GetCurrentUserEmail();
            
  if (string.IsNullOrEmpty(userEmail))
         {
              return false;
    }

            var user = await _mainDbContext.Users
                .AsNoTracking()
              .FirstOrDefaultAsync(u => u.user_email == userEmail);

            return user?.is_private_cloud == true;
    }

      /// <summary>
        /// Gets the default Main Database connection string from configuration
 /// </summary>
        private string GetDefaultConnectionString()
        {
      var connectionString = _configuration.GetConnectionString("ApplicationDbContextConnection")
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
    throw new InvalidOperationException("Encryption key or IV not configured");
         }

          using var aes = Aes.Create();
         aes.Key = Encoding.UTF8.GetBytes(encryptionKey.PadRight(32).Substring(0, 32));
    aes.IV = Encoding.UTF8.GetBytes(encryptionIV.PadRight(16).Substring(0, 16));

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

             using var msDecrypt = new MemoryStream(Convert.FromBase64String(encryptedConnectionString));
              using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
    using var srDecrypt = new StreamReader(csDecrypt);

        return srDecrypt.ReadToEnd();
            }
            catch (Exception ex)
     {
                _logger.LogError(ex, "Error decrypting connection string");
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
