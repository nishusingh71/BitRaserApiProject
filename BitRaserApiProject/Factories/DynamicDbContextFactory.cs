using Microsoft.EntityFrameworkCore;
using DSecureApi.Services;

namespace DSecureApi.Factories
{
    /// <summary>
    /// Factory for creating ApplicationDbContext with proper connection string resolution
    /// This ensures each request gets the correct database connection
    /// </summary>
    public class DynamicDbContextFactory
    {
        private readonly ITenantConnectionService _tenantConnectionService;
        private readonly ILogger<DynamicDbContextFactory> _logger;

        public DynamicDbContextFactory(
       ITenantConnectionService tenantConnectionService,
            ILogger<DynamicDbContextFactory> logger)
        {
            _tenantConnectionService = tenantConnectionService;
            _logger = logger;
        }

        /// <summary>
        /// Creates a new ApplicationDbContext instance with the appropriate connection string
        /// </summary>
        public async Task<ApplicationDbContext> CreateDbContextAsync()
        {
            try
            {
                // Resolve connection string based on current user
                var connectionString = await _tenantConnectionService.GetConnectionStringAsync();

                _logger.LogDebug("Creating ApplicationDbContext with resolved connection string");

                // Create options with the resolved connection string
                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

                optionsBuilder.UseMySql(
                    connectionString,
                 new MySqlServerVersion(new Version(8, 0, 21)),
                 mySqlOptions =>
                         {
                             mySqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                        maxRetryDelay: TimeSpan.FromSeconds(5),
                        errorNumbersToAdd: null
                    );
                             mySqlOptions.CommandTimeout(30); // ✅ FIXED: Reduced from 120s for faster failure detection
                         }
                );

                // Enable sensitive data logging in development
#if DEBUG
                optionsBuilder.EnableSensitiveDataLogging();
                optionsBuilder.EnableDetailedErrors();
#endif

                return new ApplicationDbContext(optionsBuilder.Options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ApplicationDbContext");
                throw;
            }
        }

        /// <summary>
        /// Creates an ApplicationDbContext for a specific user
        /// </summary>
        public async Task<ApplicationDbContext> CreateDbContextForUserAsync(string userEmail)
        {
            try
            {
                var connectionString = await _tenantConnectionService.GetConnectionStringForUserAsync(userEmail);

                _logger.LogDebug("Creating ApplicationDbContext for user {Email}", userEmail);

                var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

                optionsBuilder.UseMySql(
         connectionString,
       new MySqlServerVersion(new Version(8, 0, 21)),
         mySqlOptions =>
             {
                 mySqlOptions.EnableRetryOnFailure(
       maxRetryCount: 3,
         maxRetryDelay: TimeSpan.FromSeconds(5),
        errorNumbersToAdd: null
            );
                 mySqlOptions.CommandTimeout(30); // ✅ FIXED: Reduced from 120s for faster failure detection
             }
                   );

                return new ApplicationDbContext(optionsBuilder.Options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating ApplicationDbContext for user {Email}", userEmail);
                throw;
            }
        }

        internal ApplicationDbContext CreateDbContext(string connectionString)
        {
            throw new NotImplementedException();
        }
    }
}
