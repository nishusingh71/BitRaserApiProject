using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using BitRaserApiProject.Services;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Middleware
{
    /// <summary>
    /// Middleware to automatically inject appropriate database context based on user's private cloud status
    /// </summary>
public class DatabaseContextMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DatabaseContextMiddleware> _logger;

  public DatabaseContextMiddleware(
     RequestDelegate next,
  ILogger<DatabaseContextMiddleware> logger)
    {
          _next = next;
_logger = logger;
        }

public async Task InvokeAsync(
          HttpContext context,
  IDatabaseContextFactory dbFactory)
     {
  try
         {
       // Get user email from JWT token
     var userEmail = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? context.User?.FindFirst(ClaimTypes.Email)?.Value
        ?? context.User?.FindFirst("email")?.Value
    ?? context.User?.FindFirst("unique_name")?.Value;

         if (!string.IsNullOrEmpty(userEmail))
    {
  _logger.LogDebug("🔍 DatabaseContextMiddleware: User detected - {Email}", userEmail);

     try
        {
          // Get appropriate database context for this user
        var dbContext = await dbFactory.GetDbContextForUserAsync(userEmail);

    // Store in HttpContext Items for controllers to access
      context.Items["UserDbContext"] = dbContext;
     context.Items["UserEmail"] = userEmail;
 context.Items["IsPrivateCloudUser"] = await dbFactory.IsPrivateCloudUserAsync(userEmail);

     _logger.LogDebug("✅ DB Context injected for {Email} (Private: {IsPrivate})", 
          userEmail, 
   context.Items["IsPrivateCloudUser"]);
    }
       catch (Exception dbEx)
  {
      _logger.LogError(dbEx, "❌ Error getting DB context for {Email}, using main DB. Error: {Message}", 
         userEmail, dbEx.Message);
          context.Items["UserDbContext"] = dbFactory.GetMainDbContext();
   context.Items["IsPrivateCloudUser"] = false;
   context.Items["DbContextError"] = dbEx.Message;
          }
          }
        else
    {
   _logger.LogDebug("⚠️ No user email found in claims, using main DB");
      context.Items["UserDbContext"] = dbFactory.GetMainDbContext();
  context.Items["IsPrivateCloudUser"] = false;
     }
  }
            catch (Exception ex)
       {
      _logger.LogError(ex, "❌ Critical error in DatabaseContextMiddleware: {Message}", ex.Message);
         context.Items["UserDbContext"] = dbFactory.GetMainDbContext();
context.Items["IsPrivateCloudUser"] = false;
    context.Items["DbContextError"] = ex.Message;
     }

   await _next(context);
        }
    }

    /// <summary>
    /// Extension method to register middleware
    /// </summary>
    public static class DatabaseContextMiddlewareExtensions
    {
        public static IApplicationBuilder UseDatabaseContextMiddleware(this IApplicationBuilder builder)
        {
       return builder.UseMiddleware<DatabaseContextMiddleware>();
    }
    }
}
