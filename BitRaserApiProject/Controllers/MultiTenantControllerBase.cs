using Microsoft.AspNetCore.Mvc;
using DSecureApi.Models;

namespace DSecureApi.Controllers
{
    /// <summary>
    /// Base controller with multi-tenant database context support
    /// All enhanced controllers should inherit from this
 /// </summary>
    public abstract class MultiTenantControllerBase : ControllerBase
    {
  /// <summary>
        /// Get database context for current user (automatically routes to private or main DB)
      /// </summary>
    protected ApplicationDbContext GetUserDbContext()
        {
        var dbContext = HttpContext.Items["UserDbContext"] as ApplicationDbContext;
     
            if (dbContext == null)
{
        throw new InvalidOperationException(
   "Database context not found in HttpContext. " +
      "Ensure DatabaseContextMiddleware is registered.");
            }

   return dbContext;
        }

  /// <summary>
   /// Get current user's email from JWT token
        /// </summary>
        protected string? GetCurrentUserEmail()
        {
      return HttpContext.Items["UserEmail"] as string;
 }

  /// <summary>
   /// Check if current user is private cloud user
        /// </summary>
        protected bool IsCurrentUserPrivateCloud()
        {
            return HttpContext.Items["IsPrivateCloudUser"] as bool? ?? false;
   }

    /// <summary>
        /// Log database context info for debugging
        /// </summary>
        protected void LogDbContextInfo(string operation)
  {
     var email = GetCurrentUserEmail();
 var isPrivate = IsCurrentUserPrivateCloud();
   var dbContext = GetUserDbContext();
   var connectionString = dbContext.Database.ProviderName ?? "Unknown";
  
Console.WriteLine($"🔍 [{operation}] User: {email} | Private: {isPrivate} | Provider: {connectionString}");
        }
    }
}
