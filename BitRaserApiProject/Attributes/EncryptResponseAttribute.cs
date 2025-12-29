using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Text.Json;
using BitRaserApiProject.Services;

namespace BitRaserApiProject.Attributes
{
    /// <summary>
    /// Attribute to mark endpoints that should return encrypted + compressed responses
    /// Use on sensitive endpoints like quota-status, license-info, etc.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false)]
    public class EncryptResponseAttribute : ActionFilterAttribute
    {
        public override async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var resultContext = await next();

            // Only encrypt successful JSON responses
            if (resultContext.Result is ObjectResult objectResult && 
                objectResult.Value != null &&
                (objectResult.StatusCode == null || objectResult.StatusCode < 400))
            {
                try
                {
                    // Get encryption key from configuration
                    var configuration = context.HttpContext.RequestServices.GetRequiredService<IConfiguration>();
                    var encryptionKey = configuration["Encryption:Key"] 
                        ?? Environment.GetEnvironmentVariable("ENCRYPTION_KEY")
                        ?? "default-encryption-key-32chars!";

                    // Serialize the response to JSON
                    var jsonResponse = JsonSerializer.Serialize(objectResult.Value, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    });

                    // Encrypt using existing EncryptionHelper (Gzip + AES-256)
                    var encryptedData = EncryptionHelper.Encrypt(jsonResponse, encryptionKey);

                    // Return encrypted response with metadata
                    objectResult.Value = new EncryptedResponse
                    {
                        Data = encryptedData,
                        IsEncrypted = true,
                        IsCompressed = EncryptionHelper.ShouldCompress(jsonResponse.Length),
                        Timestamp = DateTime.UtcNow
                    };
                }
                catch (Exception ex)
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<EncryptResponseAttribute>>();
                    logger?.LogError(ex, "Failed to encrypt response, returning plain response");
                    // On error, return original response (fail-open for availability)
                }
            }
        }
    }

    /// <summary>
    /// Wrapper for encrypted API responses
    /// </summary>
    public class EncryptedResponse
    {
        public string Data { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
        public bool IsCompressed { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
