using BitRaserApiProject.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BitRaserApiProject.Controllers
{
    /// <summary>
    /// Cache management and diagnostics controller.
    /// Admin-only endpoints for cache monitoring and manual invalidation.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CacheController : ControllerBase
    {
        private readonly ICacheService _cacheService;
        private readonly ILogger<CacheController> _logger;

        public CacheController(
            ICacheService cacheService,
            ILogger<CacheController> logger)
        {
            _cacheService = cacheService;
            _logger = logger;
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        [HttpGet("stats")]
        [ProducesResponseType(typeof(CacheStatistics), 200)]
        public IActionResult GetStatistics()
        {
            if (!IsAdminUser())
                return Forbid();

            var stats = _cacheService.GetStatistics();
            
            return Ok(new
            {
                statistics = stats,
                hitRatioFormatted = $"{stats.HitRatio:F1}%",
                uptimeMinutes = (DateTime.UtcNow - stats.LastResetTime).TotalMinutes,
                summary = new
                {
                    totalOperations = stats.TotalHits + stats.TotalMisses,
                    cacheEfficiency = stats.HitRatio > 70 ? "GOOD" : stats.HitRatio > 40 ? "MODERATE" : "POOR"
                }
            });
        }

        /// <summary>
        /// Get all cached keys (for debugging)
        /// </summary>
        [HttpGet("keys")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetAllKeys([FromQuery] string? prefix = null)
        {
            if (!IsAdminUser())
                return Forbid();

            var keys = _cacheService.GetAllKeys();
            
            if (!string.IsNullOrEmpty(prefix))
            {
                keys = keys.Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }

            var keysList = keys.ToList();

            return Ok(new
            {
                count = keysList.Count,
                prefix = prefix ?? "ALL",
                keys = keysList.Take(100) // Limit for safety
            });
        }

        /// <summary>
        /// Invalidate cache by prefix
        /// </summary>
        [HttpPost("invalidate")]
        [ProducesResponseType(200)]
        public IActionResult InvalidateByPrefix([FromQuery] string prefix)
        {
            if (!IsAdminUser())
                return Forbid();

            if (string.IsNullOrEmpty(prefix))
                return BadRequest(new { error = "Prefix is required" });

            var keysBefore = _cacheService.GetAllKeys().Count();
            _cacheService.RemoveByPrefix(prefix);
            var keysAfter = _cacheService.GetAllKeys().Count();

            _logger.LogWarning("⚠️ Cache invalidated by {User} - Prefix: {Prefix}", 
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value, 
                prefix);

            return Ok(new
            {
                message = $"Cache invalidated for prefix: {prefix}",
                keysRemoved = keysBefore - keysAfter,
                remainingKeys = keysAfter
            });
        }

        /// <summary>
        /// Invalidate specific key
        /// </summary>
        [HttpDelete("key")]
        [ProducesResponseType(200)]
        public IActionResult RemoveKey([FromQuery] string key)
        {
            if (!IsAdminUser())
                return Forbid();

            if (string.IsNullOrEmpty(key))
                return BadRequest(new { error = "Key is required" });

            var existed = _cacheService.Exists(key);
            _cacheService.Remove(key);

            return Ok(new
            {
                message = existed ? $"Key '{key}' removed from cache" : $"Key '{key}' was not in cache",
                keyExisted = existed
            });
        }

        /// <summary>
        /// Clear entire cache (use with caution!)
        /// </summary>
        [HttpPost("clear")]
        [ProducesResponseType(200)]
        public IActionResult ClearCache([FromQuery] bool confirm = false)
        {
            if (!IsAdminUser())
                return Forbid();

            if (!confirm)
                return BadRequest(new { error = "Add ?confirm=true to clear entire cache" });

            var keysBefore = _cacheService.GetAllKeys().Count();
            _cacheService.Clear();

            _logger.LogWarning("⚠️ CACHE CLEARED by {User} - {Count} keys removed", 
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value, 
                keysBefore);

            return Ok(new
            {
                message = "Cache cleared",
                keysRemoved = keysBefore
            });
        }

        /// <summary>
        /// Invalidate cache for specific tenant
        /// </summary>
        [HttpPost("invalidate-tenant")]
        [ProducesResponseType(200)]
        public IActionResult InvalidateTenant([FromQuery] string tenantId)
        {
            if (!IsAdminUser())
                return Forbid();

            if (string.IsNullOrEmpty(tenantId))
                return BadRequest(new { error = "TenantId is required" });

            var keysBefore = _cacheService.GetAllKeys().Count();
            _cacheService.RemoveByTenant(tenantId);
            var keysAfter = _cacheService.GetAllKeys().Count();

            return Ok(new
            {
                message = $"Cache invalidated for tenant: {tenantId}",
                keysRemoved = keysBefore - keysAfter
            });
        }

        /// <summary>
        /// Check if specific key exists
        /// </summary>
        [HttpGet("exists")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult CheckKeyExists([FromQuery] string key)
        {
            if (!IsAdminUser())
                return Forbid();

            var exists = _cacheService.Exists(key);

            return Ok(new
            {
                key,
                exists
            });
        }

        /// <summary>
        /// Get cache key prefixes reference
        /// </summary>
        [HttpGet("key-prefixes")]
        [ProducesResponseType(typeof(object), 200)]
        public IActionResult GetKeyPrefixes()
        {
            if (!IsAdminUser())
                return Forbid();

            return Ok(new
            {
                prefixes = new Dictionary<string, string>
                {
                    { "Users", CacheService.CacheKeys.User },
                    { "UserList", CacheService.CacheKeys.UserList },
                    { "Subusers", CacheService.CacheKeys.Subuser },
                    { "SubuserList", CacheService.CacheKeys.SubuserList },
                    { "Reports", CacheService.CacheKeys.Report },
                    { "ReportList", CacheService.CacheKeys.ReportList },
                    { "Machines", CacheService.CacheKeys.Machine },
                    { "MachineList", CacheService.CacheKeys.MachineList },
                    { "Groups", CacheService.CacheKeys.Group },
                    { "GroupList", CacheService.CacheKeys.GroupList },
                    { "Roles", CacheService.CacheKeys.Role },
                    { "RoleList", CacheService.CacheKeys.RoleList },
                    { "Permissions", CacheService.CacheKeys.Permission },
                    { "PermissionList", CacheService.CacheKeys.PermissionList },
                    { "License", CacheService.CacheKeys.License },
                    { "Sessions", CacheService.CacheKeys.Session },
                    { "Dashboard", CacheService.CacheKeys.Dashboard },
                    { "Logs", CacheService.CacheKeys.Logs },
                    { "PrivateCloud", CacheService.CacheKeys.PrivateCloud }
                },
                usage = "Use these prefixes with /api/cache/invalidate?prefix={prefix}"
            });
        }

        #region Private Methods

        private bool IsAdminUser()
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (userRole != null && 
                (userRole.Contains("Admin", StringComparison.OrdinalIgnoreCase) ||
                 userRole.Contains("Super", StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

#if DEBUG
            return User.Identity?.IsAuthenticated == true;
#else
            return false;
#endif
        }

        #endregion
    }
}
