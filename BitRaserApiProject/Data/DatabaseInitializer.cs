using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject.Data
{
    /// <summary>
    /// Database Initialization Helper for Dynamic System
    /// Ensures the database is properly initialized with all required data
    /// </summary>
    public static class DatabaseInitializer
    {
        /// <summary>
        /// Initialize the database with required data for the dynamic system
        /// </summary>
        public static async Task InitializeAsync(IServiceProvider serviceProvider, ILogger logger)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            try
            {
                // Ensure database is created and up to date
                logger.LogInformation("🔄 Checking database migration status...");
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Database migrations completed successfully");

                // Verify essential data exists
                await VerifyEssentialDataAsync(context, logger);

                logger.LogInformation("🎉 Database initialization completed successfully!");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "❌ Error during database initialization");
                throw;
            }
        }

        /// <summary>
        /// Verify that essential system data exists
        /// </summary>
        private static async Task VerifyEssentialDataAsync(ApplicationDbContext context, ILogger logger)
        {
            // Check roles
            var roleCount = await context.Roles.CountAsync();
            logger.LogInformation("📊 Found {RoleCount} roles in database", roleCount);

            if (roleCount == 0)
            {
                logger.LogWarning("⚠️  No roles found - this might indicate a migration issue");
            }

            // Check permissions
            var permissionCount = await context.Permissions.CountAsync();
            logger.LogInformation("🔐 Found {PermissionCount} permissions in database", permissionCount);

            if (permissionCount == 0)
            {
                logger.LogWarning("⚠️  No permissions found - this might indicate a migration issue");
            }

            // Check role-permission mappings
            var mappingCount = await context.RolePermissions.CountAsync();
            logger.LogInformation("🔗 Found {MappingCount} role-permission mappings", mappingCount);

            // Check if SuperAdmin role exists
            var superAdmin = await context.Roles.FirstOrDefaultAsync(r => r.RoleName == "SuperAdmin");
            if (superAdmin != null)
            {
                logger.LogInformation("✅ SuperAdmin role found (ID: {RoleId})", superAdmin.RoleId);
            }
            else
            {
                logger.LogWarning("⚠️  SuperAdmin role not found - this is required for system administration");
            }

            // Check users
            var userCount = await context.Users.CountAsync();
            logger.LogInformation("👥 Found {UserCount} users in database", userCount);

            if (userCount > 0)
            {
                // Check if first user has SuperAdmin role
                var firstUser = await context.Users.FirstAsync();
                var userHasSuperAdmin = await context.UserRoles
                    .AnyAsync(ur => ur.UserId == firstUser.user_id && ur.Role.RoleName == "SuperAdmin");

                if (userHasSuperAdmin)
                {
                    logger.LogInformation("✅ First user has SuperAdmin role assigned");
                }
                else
                {
                    logger.LogWarning("⚠️  First user does not have SuperAdmin role - this will be auto-assigned");
                }
            }

            // Log system readiness
            var systemReady = roleCount > 0 && permissionCount > 0 && mappingCount > 0 && superAdmin != null;
            if (systemReady)
            {
                logger.LogInformation("🚀 Dynamic system is ready for use!");
            }
            else
            {
                logger.LogWarning("⚠️  System may need initialization - run dynamic system setup");
            }
        }

        /// <summary>
        /// Get system health summary
        /// </summary>
        public static async Task<SystemHealthSummary> GetSystemHealthAsync(ApplicationDbContext context)
        {
            return new SystemHealthSummary
            {
                RoleCount = await context.Roles.CountAsync(),
                PermissionCount = await context.Permissions.CountAsync(),
                UserCount = await context.Users.CountAsync(),
                SubuserCount = await context.subuser.CountAsync(),
                RouteCount = await context.Routes.CountAsync(),
                RolePermissionMappingCount = await context.RolePermissions.CountAsync(),
                SuperAdminExists = await context.Roles.AnyAsync(r => r.RoleName == "SuperAdmin"),
                DatabaseConnected = context.Database.CanConnect(),
                Timestamp = DateTime.UtcNow
            };
        }
    }

    /// <summary>
    /// System health summary information
    /// </summary>
    public class SystemHealthSummary
    {
        public int RoleCount { get; set; }
        public int PermissionCount { get; set; }
        public int UserCount { get; set; }
        public int SubuserCount { get; set; }
        public int RouteCount { get; set; }
        public int RolePermissionMappingCount { get; set; }
        public bool SuperAdminExists { get; set; }
        public bool DatabaseConnected { get; set; }
        public DateTime Timestamp { get; set; }

        public bool IsHealthy => 
            RoleCount > 0 && 
            PermissionCount > 0 && 
            SuperAdminExists && 
            DatabaseConnected &&
            RolePermissionMappingCount > 0;

        public string HealthStatus => IsHealthy ? "Healthy" : "Needs Attention";

        public List<string> GetIssues()
        {
            var issues = new List<string>();

            if (!DatabaseConnected) issues.Add("Database not connected");
            if (RoleCount == 0) issues.Add("No roles found");
            if (PermissionCount == 0) issues.Add("No permissions found");
            if (!SuperAdminExists) issues.Add("SuperAdmin role missing");
            if (RolePermissionMappingCount == 0) issues.Add("No role-permission mappings found");

            return issues;
        }

        public List<string> GetRecommendations()
        {
            var recommendations = new List<string>();

            if (!IsHealthy)
            {
                recommendations.Add("Run: POST /api/DynamicSystem/initialize-system");
                recommendations.Add("Check database migration status");
                recommendations.Add("Verify configuration settings");
            }
            else
            {
                recommendations.Add("System is healthy - continue monitoring");
                if (RouteCount == 0)
                {
                    recommendations.Add("Consider running route discovery: POST /api/DynamicSystem/discover-routes");
                }
            }

            return recommendations;
        }
    }
}