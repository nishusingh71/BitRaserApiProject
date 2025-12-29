using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using BitRaserApiProject.Models;
using BitRaserApiProject.Factories;

namespace BitRaserApiProject.Services
{
    /// <summary>
    /// Service for logging user activities in Sessions table
    /// Uses DynamicDbContextFactory to support Private Cloud routing
    /// </summary>
    public class ActivityLogService : IActivityLogService
    {
        private readonly DynamicDbContextFactory _contextFactory;
        private readonly ILogger<ActivityLogService> _logger;

        public ActivityLogService(
            DynamicDbContextFactory contextFactory,
            ILogger<ActivityLogService> logger)
        {
            _contextFactory = contextFactory;
            _logger = logger;
        }

        public async Task LogActivityAsync(
            string userEmail,
            string activityType,
            string? resourceId = null,
            string? resourceType = null,
            object? details = null,
            string? ipAddress = null)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                var session = new Sessions
                {
                    user_email = userEmail,
                    login_time = DateTime.UtcNow,
                    session_status = "activity",
                    ip_address = ipAddress ?? "unknown",
                    device_info = "activity_log",
                    activity_type = activityType,
                    resource_id = resourceId,
                    resource_type = resourceType,
                    activity_details = details != null 
                        ? JsonSerializer.Serialize(details, new JsonSerializerOptions { WriteIndented = false })
                        : null
                };

                context.Sessions.Add(session);
                await context.SaveChangesAsync();

                _logger.LogInformation("📝 Activity logged: {Type} by {Email} on {ResourceType}:{ResourceId}",
                    activityType, userEmail, resourceType ?? "-", resourceId ?? "-");
            }
            catch (Exception ex)
            {
                // Don't throw - activity logging should not break main operations
                _logger.LogError(ex, "Failed to log activity {Type} for {Email}", activityType, userEmail);
            }
        }

        public async Task<List<Sessions>> GetUserActivitiesAsync(string userEmail, int limit = 50)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Sessions
                    .Where(s => s.user_email == userEmail && s.activity_type != null)
                    .OrderByDescending(s => s.login_time)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get activities for {Email}", userEmail);
                return new List<Sessions>();
            }
        }

        public async Task<List<Sessions>> GetActivitiesByTypeAsync(string activityType, int limit = 50)
        {
            try
            {
                using var context = await _contextFactory.CreateDbContextAsync();

                return await context.Sessions
                    .Where(s => s.activity_type == activityType)
                    .OrderByDescending(s => s.login_time)
                    .Take(limit)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get activities by type {Type}", activityType);
                return new List<Sessions>();
            }
        }
    }
}
