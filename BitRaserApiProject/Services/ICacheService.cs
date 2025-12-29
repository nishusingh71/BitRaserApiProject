namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Enterprise-grade caching service interface.
    /// Provides abstraction over IMemoryCache with:
    /// - Tenant-aware caching
    /// - Prefix-based invalidation
    /// - TTL management
    /// - Cache statistics
    /// </summary>
    public interface ICacheService
    {
        #region Core Operations

        /// <summary>
        /// Get item from cache
        /// </summary>
        /// <typeparam name="T">Type of cached item</typeparam>
        /// <param name="key">Cache key</param>
        /// <returns>Cached item or default</returns>
        T? Get<T>(string key);

        /// <summary>
        /// Try to get item from cache
        /// </summary>
        bool TryGet<T>(string key, out T? value);

        /// <summary>
        /// Get or create - returns cached value or executes factory and caches result
        /// </summary>
        Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiry = null);

        /// <summary>
        /// Synchronous version of GetOrCreate
        /// </summary>
        T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiry = null);

        /// <summary>
        /// Set item in cache with optional expiry
        /// </summary>
        void Set<T>(string key, T value, TimeSpan? expiry = null);

        /// <summary>
        /// Set item with sliding expiration (resets on each access)
        /// </summary>
        void SetWithSlidingExpiry<T>(string key, T value, TimeSpan slidingExpiry);

        /// <summary>
        /// Set item with both absolute and sliding expiry
        /// </summary>
        void SetWithDualExpiry<T>(string key, T value, TimeSpan slidingExpiry, TimeSpan absoluteExpiry);

        #endregion

        #region Removal Operations

        /// <summary>
        /// Remove single item from cache
        /// </summary>
        void Remove(string key);

        /// <summary>
        /// Remove all items matching prefix (e.g., "users:" removes all user-related cache)
        /// </summary>
        void RemoveByPrefix(string prefix);

        /// <summary>
        /// Remove all items matching multiple prefixes
        /// </summary>
        void RemoveByPrefixes(params string[] prefixes);

        /// <summary>
        /// Remove all items for a specific tenant
        /// </summary>
        void RemoveByTenant(string tenantId);

        /// <summary>
        /// Clear entire cache (use with caution)
        /// </summary>
        void Clear();

        #endregion

        #region Utility Operations

        /// <summary>
        /// Check if key exists in cache
        /// </summary>
        bool Exists(string key);

        /// <summary>
        /// Refresh sliding expiry for a key
        /// </summary>
        void Refresh(string key);

        /// <summary>
        /// Get cache statistics
        /// </summary>
        CacheStatistics GetStatistics();

        /// <summary>
        /// Get all cached keys (for debugging)
        /// </summary>
        IEnumerable<string> GetAllKeys();

        #endregion

        #region Tenant-Aware Keys

        /// <summary>
        /// Build tenant-aware cache key
        /// </summary>
        string BuildKey(string prefix, string tenantId, params string[] parts);

        /// <summary>
        /// Build cache key for current tenant (from HttpContext)
        /// </summary>
        string BuildTenantKey(string prefix, params string[] parts);

        #endregion
    }

    /// <summary>
    /// Cache statistics for monitoring
    /// </summary>
    public class CacheStatistics
    {
        public long TotalHits { get; set; }
        public long TotalMisses { get; set; }
        public long TotalSets { get; set; }
        public long TotalRemovals { get; set; }
        public int CurrentItemCount { get; set; }
        public DateTime LastResetTime { get; set; }
        public double HitRatio => TotalHits + TotalMisses > 0 
            ? (double)TotalHits / (TotalHits + TotalMisses) * 100 
            : 0;
    }
}
