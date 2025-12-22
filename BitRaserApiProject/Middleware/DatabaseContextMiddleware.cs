using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using BitRaserApiProject.Services;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Middleware
{
    /// <summary>
 /// Middleware to automatically inject appropriate database context based on user's private cloud status
    /// ✅ ENHANCED: Now handles subusers by checking parent user's is_private_cloud flag
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
       // Get user email and type from JWT token
    var userEmail = context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
     ?? context.User?.FindFirst(ClaimTypes.Email)?.Value
    ?? context.User?.FindFirst("email")?.Value
    ?? context.User?.FindFirst("unique_name")?.Value;

     var userType = context.User?.FindFirst("user_type")?.Value; // "user" or "subuser"

        if (!string.IsNullOrEmpty(userEmail))
    {
 _logger.LogDebug("🔍 DatabaseContextMiddleware: User detected - {Email} (Type: {UserType})", userEmail, userType ?? "user");

      try
      {
      string effectiveUserEmail = userEmail;
   bool isSubuser = userType == "subuser";

    // ✅ If subuser, find parent user and use their is_private_cloud status
    if (isSubuser)
        {
_logger.LogDebug("👤 Subuser detected, finding parent user...");
   
         // Get main DB context to find parent
        var mainDbContext = dbFactory.GetMainDbContext();
        
         // Try to find subuser in main DB first
      var subuserInMain = await mainDbContext.subuser
        .AsNoTracking()
      .FirstOrDefaultAsync(s => s.subuser_email == userEmail);

         if (subuserInMain != null)
 {
          effectiveUserEmail = subuserInMain.user_email; // Parent email
       _logger.LogDebug("✅ Found parent in Main DB: {ParentEmail}", effectiveUserEmail);
     }
           else
       {
     // Not in main DB - check all private cloud databases
   _logger.LogDebug("🔍 Subuser not in Main DB, checking Private Cloud DBs...");
       
    var privateCloudUsers = await mainDbContext.Users
   .AsNoTracking()
       .Where(u => u.is_private_cloud == true)
  .ToListAsync();

  foreach (var pcUser in privateCloudUsers)
      {
  try
           {
             // Try to find subuser in this private cloud DB
           var pcDbContext = await dbFactory.GetDbContextForUserAsync(pcUser.user_email);
    
        var subuserInPrivate = await pcDbContext.subuser
         .AsNoTracking()
      .FirstOrDefaultAsync(s => s.subuser_email == userEmail);

      if (subuserInPrivate != null)
       {
         effectiveUserEmail = pcUser.user_email; // Parent email
  _logger.LogInformation("✅ Found subuser in Private Cloud DB, parent: {ParentEmail}", effectiveUserEmail);
     break;
     }
   }
       catch (Exception ex)
           {
     _logger.LogWarning(ex, "⚠️ Failed to check private cloud DB for user {Email}", pcUser.user_email);
      }
           }
        }
 }

   // Get appropriate database context for effective user (parent for subusers)
        var dbContext = await dbFactory.GetDbContextForUserAsync(effectiveUserEmail);
      var isPrivateCloud = await dbFactory.IsPrivateCloudUserAsync(effectiveUserEmail);

// Store in HttpContext Items for controllers to access
       context.Items["UserDbContext"] = dbContext;
     context.Items["UserEmail"] = userEmail; // Original user email
   context.Items["EffectiveUserEmail"] = effectiveUserEmail; // Parent email for subusers
     context.Items["IsSubuser"] = isSubuser;
  context.Items["IsPrivateCloudUser"] = isPrivateCloud;
  context.Items["IsPrivateCloud"] = isPrivateCloud; // ✅ For PrivateCloudAwareController compatibility

 _logger.LogDebug("✅ DB Context injected for {Email} → {EffectiveEmail} (Private: {IsPrivate}, Subuser: {IsSubuser})", 
    userEmail, effectiveUserEmail, isPrivateCloud, isSubuser);
         }
     catch (Exception dbEx)
     {
       _logger.LogError(dbEx, "❌ Error getting DB context for {Email}, using main DB. Error: {Message}", 
         userEmail, dbEx.Message);
       context.Items["UserDbContext"] = dbFactory.GetMainDbContext();
context.Items["IsPrivateCloudUser"] = false;
context.Items["IsPrivateCloud"] = false;
 context.Items["DbContextError"] = dbEx.Message;
      }
     }
   else
     {
    _logger.LogDebug("⚠️ No user email found in claims, using main DB");
       context.Items["UserDbContext"] = dbFactory.GetMainDbContext();
      context.Items["IsPrivateCloudUser"] = false;
context.Items["IsPrivateCloud"] = false;
         }
    }
       catch (Exception ex)
     {
            _logger.LogError(ex, "❌ Critical error in DatabaseContextMiddleware: {Message}", ex.Message);
context.Items["UserDbContext"] = dbFactory.GetMainDbContext();
    context.Items["IsPrivateCloudUser"] = false;
context.Items["IsPrivateCloud"] = false;
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
