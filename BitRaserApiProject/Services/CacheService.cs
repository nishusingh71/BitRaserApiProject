using Microsoft.Extensions.Caching.Memory;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Enterprise-grade caching service implementation.
    /// 
    /// KEY FEATURES:
    /// - Thread-safe with ConcurrentDictionary for key tracking
    /// - Prefix-based invalidation for bulk cache clearing
    /// - Tenant-aware cache keys for multi-tenant isolation
    /// - Statistics tracking for monitoring
    /// - Configurable TTL with sensible defaults
    /// 
    /// ARCHITECTURE DECISIONS:
    /// - Singleton lifetime (cache must persist across requests)
    /// - Key tracking enables prefix-based invalidation (IMemoryCache doesn't support this natively)
    /// - Uses PostEvictionCallback for automatic key cleanup
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IMemoryCache _cache;
        private readonly ILogger<CacheService> _logger;
        private readonly IHttpContextAccessor _httpContextAccessor;
        
        // Track all cache keys for prefix-based invalidation
        // IMemoryCache doesn't expose keys, so we track them ourselves
        private readonly ConcurrentDictionary<string, DateTime> _cacheKeys = new();
        
        // Statistics counters (thread-safe)
        private long _hits;
        private long _misses;
        private long _sets;
        private long _removals;
        private readonly DateTime _startTime = DateTime.UtcNow;

        // Default TTL values
        public static class CacheTTL
        {
            public static readonly TimeSpan VeryShort = TimeSpan.FromMinutes(1);
            public static readonly TimeSpan Short = TimeSpan.FromMinutes(3);
            public static readonly TimeSpan Default = TimeSpan.FromMinutes(5);
            public static readonly TimeSpan Medium = TimeSpan.FromMinutes(10);
            public static readonly TimeSpan Long = TimeSpan.FromMinutes(30);
            public static readonly TimeSpan VeryLong = TimeSpan.FromHours(1);
            public static readonly TimeSpan Day = TimeSpan.FromDays(1);
        }

        // Cache key prefixes for different entities
        public static class CacheKeys
        {
            // User-related
            public const string User = "user";
            public const string UserList = "users:list";
            public const string UserProfile = "user:profile";
            
            // Subuser-related
            public const string Subuser = "subuser";
            public const string SubuserList = "subusers:list";
            
            // Reports
            public const string Report = "report";
            public const string ReportList = "reports:list";
            public const string ReportExport = "reports:export";
            
            // Machines
            public const string Machine = "machine";
            public const string MachineList = "machines:list";
            
            // Groups
            public const string Group = "group";
            public const string GroupList = "groups:list";
            
            // Roles & Permissions
            public const string Role = "role";
            public const string RoleList = "roles:list";
            public const string Permission = "permission";
            public const string PermissionList = "permissions:list";
            
            // License
            public const string License = "license";
            public const string LicenseList = "licenses:list";
            
            // Sessions
            public const string Session = "session";
            public const string SessionList = "sessions:list";
            
            // Dashboard
            public const string Dashboard = "dashboard";
            public const string DashboardStats = "dashboard:stats";
            
            // Logs
            public const string Logs = "logs";
            public const string LogsList = "logs:list";
            
            // Private Cloud
            public const string PrivateCloud = "privatecloud";
            public const string PrivateCloudConfig = "privatecloud:config";
        }

        public CacheService(
            IMemoryCache cache,
            ILogger<CacheService> logger,
            IHttpContextAccessor httpContextAccessor)
        {
            _cache = cache;
            _logger = logger;
            _httpContextAccessor = httpContextAccessor;
        }

        #region Core Operations

        public T? Get<T>(string key)
        {
            if (_cache.TryGetValue(key, out T? value))
            {
                Interlocked.Increment(ref _hits);
                _logger.LogDebug("✅ Cache HIT: {Key}", key);
                return value;
            }

            Interlocked.Increment(ref _misses);
            _logger.LogDebug("❌ Cache MISS: {Key}", key);
            return default;
        }

        public bool TryGet<T>(string key, out T? value)
        {
            if (_cache.TryGetValue(key, out value))
            {
                Interlocked.Increment(ref _hits);
                _logger.LogDebug("✅ Cache HIT: {Key}", key);
                return true;
            }

            Interlocked.Increment(ref _misses);
            _logger.LogDebug("❌ Cache MISS: {Key}", key);
            value = default;
            return false;
        }

        public async Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null)
        {
            if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                Interlocked.Increment(ref _hits);
                _logger.LogDebug("✅ Cache HIT: {Key}", key);
                return cachedValue;
            }

            Interlocked.Increment(ref _misses);
            _logger.LogDebug("❌ Cache MISS: {Key} - Fetching from source", key);

            // Execute factory to get fresh data
            var value = await factory();

            // Cache the result
            Set(key, value, expiry);

            return value;
        }

        public T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiry = null)
        {
            if (_cache.TryGetValue(key, out T? cachedValue) && cachedValue != null)
            {
                Interlocked.Increment(ref _hits);
                _logger.LogDebug("✅ Cache HIT: {Key}", key);
                return cachedValue;
            }

            Interlocked.Increment(ref _misses);
            _logger.LogDebug("❌ Cache MISS: {Key} - Fetching from source", key);

            var value = factory();
            Set(key, value, expiry);

            return value;
        }

        public void Set<T>(string key, T value, TimeSpan? expiry = null)
        {
            var options = new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = expiry ?? CacheTTL.Default,
                Priority = CacheItemPriority.Normal
            };

            options.RegisterPostEvictionCallback(OnCacheItemEvicted);
            _cache.Set(key, value, options);
            _cacheKeys.TryAdd(key, DateTime.UtcNow);
            Interlocked.Increment(ref _sets);
            _logger.LogDebug("📦 Cache SET: {Key} (TTL: {TTL})", key, expiry ?? CacheTTL.Default);
        }

        public void SetWithSlidingExpiry<T>(string key, T value, TimeSpan slidingExpiry)
        {
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = slidingExpiry,
                Priority = CacheItemPriority.Normal
            };

            options.RegisterPostEvictionCallback(OnCacheItemEvicted);
            _cache.Set(key, value, options);
            _cacheKeys.TryAdd(key, DateTime.UtcNow);
            Interlocked.Increment(ref _sets);
            _logger.LogDebug("📦 Cache SET (Sliding): {Key} (TTL: {TTL})", key, slidingExpiry);
        }

        public void SetWithDualExpiry<T>(string key, T value, TimeSpan slidingExpiry, TimeSpan absoluteExpiry)
        {
            var options = new MemoryCacheEntryOptions
            {
                SlidingExpiration = slidingExpiry,
                AbsoluteExpirationRelativeToNow = absoluteExpiry,
                Priority = CacheItemPriority.Normal
            };

            options.RegisterPostEvictionCallback(OnCacheItemEvicted);
            _cache.Set(key, value, options);
            _cacheKeys.TryAdd(key, DateTime.UtcNow);
            Interlocked.Increment(ref _sets);
            _logger.LogDebug("📦 Cache SET (Dual): {Key} (Sliding: {Sliding}, Absolute: {Absolute})", 
                key, slidingExpiry, absoluteExpiry);
        }

        #endregion

        #region Removal Operations

        public void Remove(string key)
        {
            _cache.Remove(key);
            _cacheKeys.TryRemove(key, out _);
            Interlocked.Increment(ref _removals);

            _logger.LogDebug("🗑️ Cache REMOVED: {Key}", key);
        }

        public void RemoveByPrefix(string prefix)
        {
            var keysToRemove = _cacheKeys.Keys
                .Where(k => k.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
                Interlocked.Increment(ref _removals);
            }

            _logger.LogInformation("🗑️ Cache INVALIDATED by prefix: {Prefix} ({Count} keys removed)", 
                prefix, keysToRemove.Count);
        }

        public void RemoveByPrefixes(params string[] prefixes)
        {
            foreach (var prefix in prefixes)
            {
                RemoveByPrefix(prefix);
            }
        }

        public void RemoveByTenant(string tenantId)
        {
            // Tenant keys follow pattern: {tenantId}:{...}
            RemoveByPrefix($"{tenantId}:");
            _logger.LogInformation("🗑️ Cache INVALIDATED for tenant: {TenantId}", tenantId);
        }

        public void Clear()
        {
            var count = _cacheKeys.Count;
            
            foreach (var key in _cacheKeys.Keys.ToList())
            {
                _cache.Remove(key);
                _cacheKeys.TryRemove(key, out _);
            }

            Interlocked.Add(ref _removals, count);
            _logger.LogWarning("⚠️ Cache CLEARED: {Count} keys removed", count);
        }

        #endregion

        #region Utility Operations

        public bool Exists(string key)
        {
            return _cache.TryGetValue(key, out _);
        }

        public void Refresh(string key)
        {
            // Access the key to reset sliding expiration
            _cache.TryGetValue(key, out _);
        }

        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                TotalHits = Interlocked.Read(ref _hits),
                TotalMisses = Interlocked.Read(ref _misses),
                TotalSets = Interlocked.Read(ref _sets),
                TotalRemovals = Interlocked.Read(ref _removals),
                CurrentItemCount = _cacheKeys.Count,
                LastResetTime = _startTime
            };
        }

        public IEnumerable<string> GetAllKeys()
        {
            return _cacheKeys.Keys.ToList();
        }

        #endregion

        #region Tenant-Aware Keys

        public string BuildKey(string prefix, string tenantId, params string[] parts)
        {
            var keyParts = new List<string> { tenantId, prefix };
            keyParts.AddRange(parts);
            return string.Join(":", keyParts);
        }

        public string BuildTenantKey(string prefix, params string[] parts)
        {
            var tenantId = GetCurrentTenantId();
            return BuildKey(prefix, tenantId, parts);
        }

        private string GetCurrentTenantId()
        {
            var httpContext = _httpContextAccessor.HttpContext;
            if (httpContext == null)
                return "SYSTEM";

            // Try to get user email as tenant identifier
            var userEmail = httpContext.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userEmail))
                return userEmail;

            // Check if private cloud user
            var isPrivateCloud = httpContext.Items.TryGetValue("IsPrivateCloudUser", out var pcFlag) 
                && (bool?)pcFlag == true;
            
            if (isPrivateCloud)
            {
                var pcEmail = httpContext.Items.TryGetValue("PrivateCloudUserEmail", out var email) 
                    ? email?.ToString() 
                    : null;
                return !string.IsNullOrEmpty(pcEmail) ? $"PC:{pcEmail}" : "PC:UNKNOWN";
            }

            return "MAIN";
        }

        #endregion

        #region Private Methods

        private void OnCacheItemEvicted(object key, object? value, EvictionReason reason, object? state)
        {
            var keyStr = key?.ToString() ?? "";
            _cacheKeys.TryRemove(keyStr, out _);

            if (reason != EvictionReason.Removed && reason != EvictionReason.Replaced)
            {
                _logger.LogDebug("🕐 Cache EVICTED: {Key} (Reason: {Reason})", keyStr, reason);
            }
        }

        #endregion
    }

    /// <summary>
    /// Static helper class for entity-specific cache invalidation
    /// Use these methods when modifying data to ensure cache consistency
    /// </summary>
    public static class CacheInvalidation
    {
        /// <summary>
        /// Invalidate cache when a subuser is created, updated, or deleted
        /// </summary>
        public static void InvalidateSubuser(ICacheService cache, string subuserEmail, string parentEmail)
        {
            cache.RemoveByPrefix($"subuser:{subuserEmail.ToLower()}");
            cache.RemoveByPrefix($"subusers:list:parent:{parentEmail.ToLower()}");
            cache.RemoveByPrefix($"subuser:exists:{subuserEmail.ToLower()}");
            cache.RemoveByPrefix($"permission:{subuserEmail.ToLower()}");
            cache.RemoveByPrefix($"user:issubuser:{subuserEmail.ToLower()}");
        }

        /// <summary>
        /// Invalidate cache when a machine is created, updated, or deleted
        /// </summary>
        public static void InvalidateMachine(ICacheService cache, string userEmail, string? subuserEmail, string macAddress)
        {
            cache.Remove($"machine:{macAddress}");
            cache.RemoveByPrefix($"machine:byemail:{userEmail.ToLower()}");
            if (!string.IsNullOrEmpty(subuserEmail))
                cache.RemoveByPrefix($"machine:byemail:{subuserEmail.ToLower()}");
            cache.RemoveByPrefix("machines:list");
        }

        /// <summary>
        /// Invalidate cache when a user profile is updated
        /// </summary>
        public static void InvalidateUser(ICacheService cache, string userEmail)
        {
            cache.RemoveByPrefix($"user:{userEmail.ToLower()}");
            cache.RemoveByPrefix($"user:profile:{userEmail.ToLower()}");
            cache.RemoveByPrefix($"user:exists:{userEmail.ToLower()}");
            cache.RemoveByPrefix($"permission:{userEmail.ToLower()}");
            cache.RemoveByPrefix($"subuser:exists:{userEmail.ToLower()}");
        }

        /// <summary>
        /// Invalidate cache when reports are generated or modified
        /// </summary>
        public static void InvalidateReports(ICacheService cache, string userEmail)
        {
            cache.RemoveByPrefix($"reports:list:{userEmail.ToLower()}");
            cache.RemoveByPrefix($"reports:export:{userEmail.ToLower()}");
            cache.RemoveByPrefix($"report:{userEmail.ToLower()}");
        }

        /// <summary>
        /// Invalidate cache when user roles or permissions change
        /// </summary>
        public static void InvalidatePermissions(ICacheService cache, string userEmail)
        {
            cache.RemoveByPrefix($"permission:{userEmail.ToLower()}");
            cache.RemoveByPrefix($"role:{userEmail.ToLower()}");
        }

        /// <summary>
        /// Invalidate all caches for a user (use sparingly - high impact)
        /// </summary>
        public static void InvalidateAllUserData(ICacheService cache, string userEmail)
        {
            cache.RemoveByPrefix(userEmail.ToLower());
        }
    }

    /// <summary>
    /// Extension methods for cache service registration
    /// </summary>
    public static class CacheServiceExtensions
    {
        /// <summary>
        /// Add enterprise caching services
        /// </summary>
        public static IServiceCollection AddEnterpriseCaching(this IServiceCollection services, Action<MemoryCacheOptions>? configureOptions = null)
        {
            services.AddMemoryCache(options =>
            {
                options.CompactionPercentage = 0.20;
                options.ExpirationScanFrequency = TimeSpan.FromSeconds(30);
                configureOptions?.Invoke(options);
            });

            services.AddSingleton<ICacheService, CacheService>();
            return services;
        }
    }
}
