using System.Security.Claims;
using BitRaserApiProject.Services;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Middleware
{
    /// <summary>
    /// Middleware to automatically set tenant context for private cloud routing
    /// Ensures ALL requests from private cloud users/subusers are routed to their private database
    /// </summary>
    public class PrivateCloudRoutingMiddleware
    {
      private readonly RequestDelegate _next;
     private readonly ILogger<PrivateCloudRoutingMiddleware> _logger;

        public PrivateCloudRoutingMiddleware(
  RequestDelegate next,
  ILogger<PrivateCloudRoutingMiddleware> logger)
     {
     _next = next;
     _logger = logger;
      }

        public async Task InvokeAsync(HttpContext context)
        {
  try
     {
                // Get current user email from JWT token
       var userEmail = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
   var userType = context.User.FindFirst("user_type")?.Value;

                if (!string.IsNullOrEmpty(userEmail))
 {
        // Get services from DI
            var dbContext = context.RequestServices.GetRequiredService<ApplicationDbContext>();
           var tenantService = context.RequestServices.GetRequiredService<ITenantConnectionService>();

     string? effectiveUserEmail = userEmail;
      bool isPrivateCloud = false;

      // ✅ If subuser, find parent user and check their is_private_cloud flag
    if (userType == "subuser")
           {
     _logger.LogDebug("🔍 Subuser detected: {Email}, finding parent user...", userEmail);

          // Try main database first
        var subuserInMain = await dbContext.subuser
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
         var privateCloudUsers = await dbContext.Users
    .AsNoTracking()
   .Where(u => u.is_private_cloud == true)
              .Select(u => u.user_email)
   .ToListAsync();

         foreach (var pcUserEmail in privateCloudUsers)
{
      try
           {
   var connectionString = await tenantService.GetConnectionStringForUserAsync(pcUserEmail);
 var mainConnectionString = context.RequestServices
               .GetRequiredService<IConfiguration>()
     .GetConnectionString("ApplicationDbContextConnection");

     if (connectionString == mainConnectionString)
         continue;

         var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
          optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

              using var privateContext = new ApplicationDbContext(optionsBuilder.Options);
         var subuserInPrivate = await privateContext.subuser
      .AsNoTracking()
          .FirstOrDefaultAsync(s => s.subuser_email == userEmail);

                if (subuserInPrivate != null)
      {
       effectiveUserEmail = pcUserEmail; // Parent email
              _logger.LogInformation("✅ Found subuser in Private Cloud DB, parent: {ParentEmail}", effectiveUserEmail);
               break;
   }
               }
       catch (Exception ex)
    {
    _logger.LogWarning(ex, "⚠️ Failed to check private cloud DB for user {Email}", pcUserEmail);
     }
      }
           }
             }

          // ✅ Check if parent user has private cloud enabled
              var parentUser = await dbContext.Users
          .AsNoTracking()
     .FirstOrDefaultAsync(u => u.user_email == effectiveUserEmail);

          if (parentUser?.is_private_cloud == true)
      {
   isPrivateCloud = true;
     _logger.LogDebug("🔐 Private Cloud enabled for user: {Email} (original: {OriginalEmail})", 
       effectiveUserEmail, userEmail);

       // Store in HttpContext.Items for controllers to use
      context.Items["IsPrivateCloud"] = true;
   context.Items["EffectiveUserEmail"] = effectiveUserEmail;
         context.Items["OriginalUserEmail"] = userEmail;
    context.Items["IsSubuser"] = userType == "subuser";
     }
       else
           {
          _logger.LogDebug("📊 Using Main DB for user: {Email}", userEmail);
         context.Items["IsPrivateCloud"] = false;
  }
        }
  }
    catch (Exception ex)
     {
   _logger.LogError(ex, "❌ Error in PrivateCloudRoutingMiddleware");
      // Continue anyway - don't block the request
          }

            await _next(context);
        }
    }

    /// <summary>
    /// Extension method to register the middleware
    /// </summary>
 public static class PrivateCloudRoutingMiddlewareExtensions
    {
      public static IApplicationBuilder UsePrivateCloudRouting(this IApplicationBuilder builder)
        {
      return builder.UseMiddleware<PrivateCloudRoutingMiddleware>();
        }
    }
}
