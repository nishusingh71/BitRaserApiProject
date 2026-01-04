using BitRaserApiProject.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Services.Implementations
{
    public class UserContextService : IUserContextService
    {
        private readonly ApplicationDbContext _context;
        private readonly ICacheService _cache;
        private readonly ILogger<UserContextService> _logger;

        private static readonly TimeSpan UserCacheDuration = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan SubuserCheckDuration = TimeSpan.FromMinutes(15);

        public UserContextService(
            ApplicationDbContext context,
            ICacheService cache,
            ILogger<UserContextService> logger)
        {
            _context = context;
            _cache = cache;
            _logger = logger;
        }

        public async Task<UserContext?> GetUserContextAsync(string email)
        {
            var normalizedEmail = email.ToLower();
            var cacheKey = $"userctx:{normalizedEmail}";

            return await _cache.GetOrCreateAsync(cacheKey, async () =>
            {
                // Check main users first
                var user = await _context.Users
                    .Where(u => u.user_email.ToLower() == normalizedEmail)
                    .Select(u => new UserContext
                    {
                        UserId = u.user_id,
                        Email = u.user_email,
                        Name = u.user_name,
                        IsSubuser = false,
                        PrivateApi = u.private_api ?? false
                    })
                    .FirstOrDefaultAsync();

                if (user != null) return user;

                // Check subusers
                var subuser = await _context.subuser
                    .Where(s => s.subuser_email.ToLower() == normalizedEmail)
                    .Select(s => new UserContext
                    {
                        SubuserId = s.subuser_id,
                        Email = s.subuser_email,
                        Name = s.subuser_email,
                        IsSubuser = true,
                        ParentEmail = s.user_email,
                        PrivateApi = false
                    })
                    .FirstOrDefaultAsync();

                return subuser;
            }, UserCacheDuration);
        }

        public async Task<bool> IsSubuserAsync(string email)
        {
            var normalizedEmail = email.ToLower();
            var cacheKey = $"issubuser:{normalizedEmail}";

            return await _cache.GetOrCreateAsync(cacheKey, async () =>
            {
                return await _context.subuser.AnyAsync(s => s.subuser_email.ToLower() == normalizedEmail);
            }, SubuserCheckDuration);
        }

        public void InvalidateUser(string email)
        {
            var normalizedEmail = email.ToLower();
            _cache.Remove($"userctx:{normalizedEmail}");
            _cache.Remove($"issubuser:{normalizedEmail}");
            _logger.LogInformation("Invalidated user context cache for {Email}", email);
        }
    }
}
