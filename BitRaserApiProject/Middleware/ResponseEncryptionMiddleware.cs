using System.Text;
using BitRaserApiProject.Services;

namespace BitRaserApiProject.Middleware
{
    /// <summary>
    /// Middleware to encrypt HTTP responses using AES-256-CBC
    /// Intercepts response body and encrypts it before sending to client
    /// </summary>
    public class ResponseEncryptionMiddleware
    {
        private readonly RequestDelegate _next;
      private readonly ILogger<ResponseEncryptionMiddleware> _logger;
    private readonly IConfiguration _configuration;
        private readonly string _encryptionKey;
        private readonly bool _encryptionEnabled;

        public ResponseEncryptionMiddleware(
            RequestDelegate next,
            ILogger<ResponseEncryptionMiddleware> logger,
            IConfiguration configuration)
   {
            _next = next;
            _logger = logger;
   _configuration = configuration;

            // Check if encryption is enabled
            _encryptionEnabled = _configuration.GetValue<bool>("Encryption:Enabled", true);
      
          if (!_encryptionEnabled)
            {
      _logger.LogWarning("⚠️ Response encryption is DISABLED in configuration");
                _encryptionKey = string.Empty;
                return;
 }

  // Get encryption key from configuration
     _encryptionKey = _configuration["Encryption:ResponseKey"]
   ?? _configuration["Encryption:Key"]
      ?? throw new InvalidOperationException(
      "Encryption key not found in configuration. " +
   "Please set 'Encryption:ResponseKey' or 'Encryption:Key' in appsettings.json");

            // Validate key length
       if (_encryptionKey.Length < 32)
      {
_logger.LogWarning(
   "⚠️ Encryption key is shorter than 32 characters. " +
    "It will be padded, but consider using a 32-character key for AES-256.");
     }
  
     _logger.LogInformation("✅ Response encryption is ENABLED");
}

        public async Task InvokeAsync(HttpContext context)
{
            // Skip all encryption if disabled in configuration
       if (!_encryptionEnabled)
 {
              await _next(context);
       return;
            }

            // Skip encryption for specific paths
          if (ShouldSkipEncryption(context))
            {
        await _next(context);
  return;
   }

  // Store original response body stream
            var originalBodyStream = context.Response.Body;

      try
            {
  // Create a new memory stream to capture the response
         using var responseBodyStream = new MemoryStream();
     
     // Replace response body stream with our memory stream
       context.Response.Body = responseBodyStream;

   // Call the next middleware in the pipeline
      await _next(context);

                // Reset stream position to beginning for reading
      responseBodyStream.Seek(0, SeekOrigin.Begin);

      // Read the response body
     var responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();

                // Only encrypt if response is successful and has content
              if (context.Response.StatusCode == 200 && !string.IsNullOrEmpty(responseBody))
       {
  try
   {
    // ✅ Calculate original size for logging
    var originalSize = Encoding.UTF8.GetByteCount(responseBody);

        // Encrypt the response body (now includes Gzip compression)
      var encryptedResponse = EncryptionHelper.Encrypt(responseBody, _encryptionKey);

      // ✅ SIMPLIFIED: Only return data field
  var encryptedWrapper = new
      {
       data = encryptedResponse
   };

  // Serialize encrypted wrapper to JSON
          var encryptedJson = System.Text.Json.JsonSerializer.Serialize(encryptedWrapper);
    var encryptedBytes = Encoding.UTF8.GetBytes(encryptedJson);

   // Update content type and length
   context.Response.ContentType = "application/json; charset=utf-8";
   context.Response.ContentLength = encryptedBytes.Length;

     // Write encrypted response to original stream
      responseBodyStream.Seek(0, SeekOrigin.Begin);
  await originalBodyStream.WriteAsync(encryptedBytes, 0, encryptedBytes.Length);

// ✅ Calculate compression ratio
var compressionRatio = originalSize > 0 ? (1 - ((double)encryptedBytes.Length / originalSize)) * 100 : 0;

_logger.LogDebug(
 "✅ Response compressed+encrypted for {Method} {Path} - Original: {OriginalSize} bytes, Final: {EncryptedSize} bytes, Compression: {Ratio:F1}%",
      context.Request.Method,
    context.Request.Path,
        originalSize,
   encryptedBytes.Length,
   compressionRatio);
        }
      catch (Exception ex)
   {
     _logger.LogError(ex, "❌ Failed to encrypt response for {Method} {Path}",
        context.Request.Method,
        context.Request.Path);

      // Fall back to sending unencrypted response
 responseBodyStream.Seek(0, SeekOrigin.Begin);
      await responseBodyStream.CopyToAsync(originalBodyStream);
         }
   }
     else
    {
          // For non-200 responses or empty content, send as-is
            responseBodyStream.Seek(0, SeekOrigin.Begin);
       await responseBodyStream.CopyToAsync(originalBodyStream);

           if (context.Response.StatusCode != 200)
         {
      _logger.LogDebug(
           "⏭️ Skipping encryption for {Method} {Path} - Status: {StatusCode}",
      context.Request.Method,
              context.Request.Path,
          context.Response.StatusCode);
  }
    }
   }
 finally
       {
                // Restore original response body stream
      context.Response.Body = originalBodyStream;
            }
        }

    /// <summary>
        /// Determine if encryption should be skipped for this request
  /// </summary>
        private bool ShouldSkipEncryption(HttpContext context)
  {
       var path = context.Request.Path.Value?.ToLowerInvariant() ?? string.Empty;

  // Skip encryption for these paths
  var skipPaths = new[]
   {
  "/swagger",        // Swagger UI
         "/swagger/",
       "/swagger/v1/swagger.json",
     "/health",     // Health checks
  "/api/health",
            "/metrics",      // Metrics endpoints
         "/favicon.ico",   // Static files
     "/.well-known",   // Well-known URIs"
            };

        if (skipPaths.Any(p => path.StartsWith(p)))
    {
return true;
       }

       // ✅ NEW: Skip encryption for JWT token endpoints (login, register, refresh-token)
    var authPaths = new[]
  {
     "/api/auth/login",
     "/api/time/server-time",
         "/time/server-time"
   };

  if (authPaths.Any(p => path.Equals(p, StringComparison.OrdinalIgnoreCase)))
    {
     _logger.LogDebug("⏭️ Skipping encryption for auth endpoint: {Path}", path);
      return true;
  }

      // ✅ NEW: Skip encryption for PDF downloads
         if (path.Contains("/pdf", StringComparison.OrdinalIgnoreCase) ||
      path.Contains("/export-pdf", StringComparison.OrdinalIgnoreCase) ||
          path.Contains("/download", StringComparison.OrdinalIgnoreCase) ||
      context.Response.ContentType?.Contains("application/pdf", StringComparison.OrdinalIgnoreCase) == true)
            {
              _logger.LogDebug("⏭️ Skipping encryption for PDF/download: {Path}", path);
     return true;
         }

   // ✅ NEW: Skip encryption for file downloads (any binary content)
 var binaryContentTypes = new[]
            {
 "application/pdf",
    "application/octet-stream",
                "application/zip",
 "application/x-zip-compressed",
      "image/",
              "video/",
    "audio/"
    };

  if (!string.IsNullOrEmpty(context.Response.ContentType) &&
        binaryContentTypes.Any(ct => context.Response.ContentType.Contains(ct, StringComparison.OrdinalIgnoreCase)))
       {
          _logger.LogDebug("⏭️ Skipping encryption for binary content: {ContentType}", context.Response.ContentType);
           return true;
            }

// Skip encryption if explicitly disabled via query parameter
   if (context.Request.Query.ContainsKey("no-encrypt"))
  {
     _logger.LogDebug("⏭️ Encryption disabled via query parameter for {Path}", path);
      return true;
     }

// Skip encryption if explicitly disabled via header
            if (context.Request.Headers.ContainsKey("X-No-Encryption"))
        {
          _logger.LogDebug("⏭️ Encryption disabled via header for {Path}", path);
      return true;
 }

      // Check if client supports encryption (optional check)
         // You can implement custom logic here
   // For example, check for a specific header or query parameter

         return false;
      }
    }

 /// <summary>
    /// Extension method to register ResponseEncryptionMiddleware
    /// </summary>
    public static class ResponseEncryptionMiddlewareExtensions
    {
      public static IApplicationBuilder UseResponseEncryption(this IApplicationBuilder builder)
      {
  return builder.UseMiddleware<ResponseEncryptionMiddleware>();
        }
    }
}
