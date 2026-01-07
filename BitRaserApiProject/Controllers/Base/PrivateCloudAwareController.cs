using Microsoft.AspNetCore.Mvc;
using DSecureApi.Factories;
using DSecureApi.Services;
using System.Security.Claims;

namespace DSecureApi.Controllers.Base
{
    /// <summary>
    /// Base controller that provides automatic private cloud database routing
    /// All controllers should inherit from this to get automatic routing
    /// </summary>
 public abstract class PrivateCloudAwareController : ControllerBase
    {
  protected readonly ApplicationDbContext _mainContext;
        protected readonly DynamicDbContextFactory _contextFactory;
        protected readonly ILogger _logger;
 protected readonly ITenantConnectionService _tenantService;

        protected PrivateCloudAwareController(
  ApplicationDbContext mainContext,
 DynamicDbContextFactory contextFactory,
     ILogger logger,
        ITenantConnectionService tenantService)
        {
            _mainContext = mainContext;
    _contextFactory = contextFactory;
  _logger = logger;
   _tenantService = tenantService;
        }

        /// <summary>
        /// Gets the correct database context based on user's private cloud status
        /// Automatically detects if user/subuser should use private cloud DB
        /// </summary>
        protected async Task<ApplicationDbContext> GetDbContextAsync()
     {
            // Check if middleware already determined this is a private cloud request
            var isPrivateCloud = HttpContext.Items["IsPrivateCloud"] as bool?;
 
            if (isPrivateCloud == true)
        {
         var effectiveUserEmail = HttpContext.Items["EffectiveUserEmail"] as string;
     _logger.LogDebug("🔐 Using Private Cloud DB for user: {Email}", effectiveUserEmail);
      
   return await _contextFactory.CreateDbContextAsync();
    }
   
            _logger.LogDebug("📊 Using Main DB");
    return _mainContext;
        }

        /// <summary>
  /// Gets current user email from JWT token
 /// </summary>
   protected string? GetCurrentUserEmail()
        {
    return User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }

        /// <summary>
 /// Gets current user type (user or subuser)
/// </summary>
        protected string? GetCurrentUserType()
        {
         return User.FindFirst("user_type")?.Value;
  }

        /// <summary>
        /// Checks if current request should use private cloud database
        /// </summary>
        protected bool IsPrivateCloudRequest()
   {
            return HttpContext.Items["IsPrivateCloud"] as bool? == true;
   }

        /// <summary>
        /// Gets the effective user email (parent email for subusers)
/// </summary>
        protected string? GetEffectiveUserEmail()
        {
    return HttpContext.Items["EffectiveUserEmail"] as string ?? GetCurrentUserEmail();
        }

      /// <summary>
/// Gets the original user email (actual logged-in user email)
        /// </summary>
        protected string? GetOriginalUserEmail()
        {
    return HttpContext.Items["OriginalUserEmail"] as string ?? GetCurrentUserEmail();
        }
    }
}
