using BitRaserApiProject.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Services.Implementations
{
    public class PrivateCloudConfigCache : IPrivateCloudConfigCache
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cache;
        private readonly ILogger<PrivateCloudConfigCache> _logger;

        private static readonly TimeSpan ConfigCacheDuration = TimeSpan.FromMinutes(10);

        public PrivateCloudConfigCache(
            ApplicationDbContext context,
            ICacheService cache,
            ILogger<PrivateCloudConfigCache> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<PrivateCloudConfig?> GetConfigAsync(string userEmail)
        {
            var normalizedEmail = userEmail.ToLower();
            var cacheKey = $"pcconfig:{normalizedEmail}";

            return await _cache.GetOrCreateAsync(cacheKey, async () =>
            {
                var config = await _context.PrivateCloudDatabases
                    .Where(p => p.UserEmail.ToLower() == normalizedEmail && p.IsActive)
                    .Select(p => new PrivateCloudConfig
                    {
                        Id = p.ConfigId,
                        UserEmail = p.UserEmail,
                        DatabaseName = p.DatabaseName ?? "",
                        ConnectionString = p.ConnectionString,
                        IsActive = p.IsActive
                    })
                    .FirstOrDefaultAsync();

                return config;
            }, ConfigCacheDuration);
        }

        public void InvalidateConfig(string userEmail)
        {
            var normalizedEmail = userEmail.ToLower();
            _cache.Remove($"pcconfig:{normalizedEmail}");
            _logger.LogInformation("Invalidated private cloud config cache for {Email}", userEmail);
        }
    }
}
