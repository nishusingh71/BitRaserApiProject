using Microsoft.EntityFrameworkCore;
using DSecureApi.Models;

namespace DSecureApi.Services
{
    public interface IDynamicPermissionService
    {
        Task<PermissionOperationResult> EnsurePermissionsExistAsync();
        Task<RoleMappingResult> CreateDynamicRolePermissionMappingsAsync();
        Task<List<string>> GetUserPermissionsAsync(string userEmail);
        Task<bool> HasPermissionAsync(string userEmail, string permissionName);
    }

    public class DynamicPermissionService : IDynamicPermissionService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DynamicPermissionService> _logger;

        public DynamicPermissionService(ApplicationDbContext context, ILogger<DynamicPermissionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<PermissionOperationResult> EnsurePermissionsExistAsync()
        {
            try
            {
                var requiredPermissions = GetRequiredSystemPermissions();
                var existingPermissions = await _context.Permissions
                    .Select(p => p.PermissionName)
                    .ToListAsync();

                var newPermissions = requiredPermissions
                    .Where(rp => !existingPermissions.Contains(rp.Name))
                    .ToList();

                if (newPermissions.Any())
                {
                    var permissionEntities = newPermissions.Select(np => new Permission
                    {
                        PermissionName = np.Name,
                        Description = np.Description,
                        CreatedAt = DateTime.UtcNow
                    }).ToList();

                    _context.Permissions.AddRange(permissionEntities);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Created {Count} new permissions", newPermissions.Count);
                    
                    return new PermissionOperationResult
                    {
                        Success = true,
                        Message = $"Successfully created {newPermissions.Count} new permissions",
                        PermissionsCreated = newPermissions.Count,
                        CreatedPermissions = newPermissions.Select(p => p.Name).ToList()
                    };
                }

                return new PermissionOperationResult
                {
                    Success = true,
                    Message = "All required permissions already exist",
                    PermissionsCreated = 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error ensuring permissions exist");
                return new PermissionOperationResult
                {
                    Success = false,
                    Message = $"Error creating permissions: {ex.Message}"
                };
            }
        }

        public async Task<RoleMappingResult> CreateDynamicRolePermissionMappingsAsync()
        {
            try
            {
                var roles = await _context.Roles.ToListAsync();
                var permissions = await _context.Permissions.ToListAsync();
                var existingMappings = await _context.RolePermissions.ToListAsync();

                var mappingsCreated = 0;
                var roleMappings = GetRolePermissionMappings();

                foreach (var mapping in roleMappings)
                {
                    var role = roles.FirstOrDefault(r => r.RoleName == mapping.RoleName);
                    if (role == null) continue;

                    foreach (var permissionName in mapping.Permissions)
                    {
                        var permission = permissions.FirstOrDefault(p => p.PermissionName == permissionName);
                        if (permission == null) continue;

                        if (existingMappings.Any(em => em.RoleId == role.RoleId && em.PermissionId == permission.PermissionId))
                            continue;

                        _context.RolePermissions.Add(new RolePermission
                        {
                            RoleId = role.RoleId,
                            PermissionId = permission.PermissionId
                        });
                        mappingsCreated++;
                    }
                }

                if (mappingsCreated > 0)
                {
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Created {Count} new role-permission mappings", mappingsCreated);
                }

                return new RoleMappingResult
                {
                    Success = true,
                    Message = $"Created {mappingsCreated} new role-permission mappings",
                    MappingsCreated = mappingsCreated
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating role-permission mappings");
                return new RoleMappingResult
                {
                    Success = false,
                    Message = $"Error creating mappings: {ex.Message}"
                };
            }
        }

        public async Task<List<string>> GetUserPermissionsAsync(string userEmail)
        {
            try
            {
                var user = await _context.Users
                    .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                    .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
                    .FirstOrDefaultAsync(u => u.user_email == userEmail);

                if (user == null)
                    return new List<string>();

                return user.UserRoles
                    .SelectMany(ur => ur.Role.RolePermissions)
                    .Select(rp => rp.Permission.PermissionName)
                    .Distinct()
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user permissions for {UserEmail}", userEmail);
                return new List<string>();
            }
        }

        public async Task<bool> HasPermissionAsync(string userEmail, string permissionName)
        {
            try
            {
                var userPermissions = await GetUserPermissionsAsync(userEmail);
                return userPermissions.Contains(permissionName) || userPermissions.Contains("FullAccess");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking permission {Permission} for {UserEmail}", permissionName, userEmail);
                return false;
            }
        }

        private List<RequiredPermission> GetRequiredSystemPermissions()
        {
            return new List<RequiredPermission>
            {
                // Original permissions
                new("FullAccess", "Complete system access"),
                new("UserManagement", "Manage users and subusers"),
                new("ReportAccess", "Access and manage audit reports"),
                new("MachineManagement", "Manage machines"),
                new("ViewOnly", "Read-only access"),
                new("LicenseManagement", "Manage licenses"),
                new("SystemLogs", "Access system logs"),

                // Enhanced Machine permissions
                new("READ_ALL_MACHINES", "View all machines in the system"),
                new("READ_MACHINE", "View individual machine details"),
                new("CREATE_MACHINE", "Register new machines"),
                new("UPDATE_MACHINE", "Update machine information"),
                new("UPDATE_ALL_MACHINES", "Update any machine in system"),
                new("DELETE_MACHINE", "Remove machines from system"),
                new("DELETE_ALL_MACHINES", "Remove any machine in system"),
                new("MANAGE_MACHINE_LICENSES", "Activate, deactivate, and renew licenses"),
                new("MANAGE_ALL_MACHINE_LICENSES", "Manage licenses for any machine"),
                new("READ_MACHINE_STATISTICS", "View machine analytics and statistics"),
                new("READ_MANAGED_USER_MACHINES", "View machines for managed users"),

                // Enhanced User permissions
                new("READ_ALL_USERS", "View all users in the system"),
                new("READ_USER", "View individual user details"),
                new("READ_MANAGED_USERS", "View users in management hierarchy"),
                new("CREATE_USER", "Create new user accounts"),
                new("UPDATE_USER", "Update user information"),
                new("DELETE_USER", "Remove user accounts"),
                new("ASSIGN_ROLES", "Assign and remove user roles"),
                new("CHANGE_USER_PASSWORDS", "Change user passwords"),
                new("READ_USER_STATISTICS", "View user analytics and statistics"),
                new("MANAGE_USER_HIERARCHY", "Manage user reporting relationships"),

                // Enhanced Subuser permissions
                new("READ_ALL_SUBUSERS", "View all subusers in the system"),
                new("READ_SUBUSER", "View individual subuser details"),
                new("READ_USER_SUBUSERS", "View subusers for managed users"),
                new("CREATE_SUBUSER", "Create new subuser accounts"),
                new("CREATE_SUBUSERS_FOR_OTHERS", "Create subusers for other users"),
                new("UPDATE_SUBUSER", "Update subuser information"),
                new("UPDATE_ALL_SUBUSERS", "Update any subuser in system"),
                new("DELETE_SUBUSER", "Remove subuser accounts"),
                new("DELETE_ALL_SUBUSERS", "Remove any subuser in system"),
                new("CHANGE_SUBUSER_PASSWORD", "Change subuser passwords"),
                new("CHANGE_ALL_SUBUSER_PASSWORDS", "Change any subuser password"),
                new("ASSIGN_SUBUSER_ROLES", "Assign roles to subusers"),
                new("ASSIGN_ALL_SUBUSER_ROLES", "Assign roles to any subuser"),
                new("REMOVE_SUBUSER_ROLES", "Remove roles from subusers"),
                new("REMOVE_ALL_SUBUSER_ROLES", "Remove roles from any subuser"),
                new("REASSIGN_SUBUSERS", "Reassign subusers to different parents"),
                new("READ_SUBUSER_STATISTICS", "View subuser analytics and statistics"),
                new("READ_ALL_SUBUSER_STATISTICS", "View system-wide subuser analytics"),

                // Enhanced Report permissions
                new("READ_ALL_REPORTS", "View all audit reports"),
                new("READ_REPORT", "View individual report details"),
                new("READ_USER_REPORTS", "View reports for managed users"),
                new("CREATE_REPORTS_FOR_OTHERS", "Create reports on behalf of other users"),
                new("UPDATE_ALL_REPORTS", "Update any report in system"),
                new("UPDATE_REPORT", "Update own reports"),
                new("DELETE_ALL_REPORTS", "Delete any report in system"),
                new("DELETE_REPORT", "Delete own reports"),
                new("EXPORT_REPORTS", "Export reports to PDF/CSV"),
                new("EXPORT_ALL_REPORTS", "Export any reports in system"),
                new("READ_REPORT_STATISTICS", "View report analytics"),
                new("READ_ALL_REPORT_STATISTICS", "View system-wide report analytics"),

                // Enhanced Commands permissions
                new("READ_ALL_COMMANDS", "View all system commands"),
                new("READ_COMMAND", "View individual command details"),
                new("CREATE_COMMAND", "Create new commands"),
                new("UPDATE_COMMAND", "Update command information"),
                new("UPDATE_COMMAND_STATUS", "Update command execution status"),
                new("DELETE_COMMAND", "Remove commands from system"),
                new("MANAGE_COMMANDS", "Full command management access"),
                new("READ_COMMAND_STATISTICS", "View command analytics and statistics"),
                new("BULK_UPDATE_COMMANDS", "Bulk update multiple commands"),

                // Enhanced Sessions permissions
                new("READ_ALL_SESSIONS", "View all user sessions"),
                new("READ_SESSION", "View individual session details"),
                new("READ_USER_SESSIONS", "View sessions for managed users"),
                new("END_SESSION", "End own sessions"),
                new("END_ALL_SESSIONS", "End any user session"),
                new("END_USER_SESSIONS", "End sessions for managed users"),
                new("EXTEND_SESSION", "Extend session duration"),
                new("READ_SESSION_STATISTICS", "View session analytics"),
                new("READ_ALL_SESSION_STATISTICS", "View system-wide session analytics"),
                new("CLEANUP_SESSIONS", "Clean up expired sessions"),

                // Enhanced Logs permissions
                new("READ_ALL_LOGS", "View all system logs"),
                new("READ_LOG", "View individual log entries"),
                new("READ_USER_LOGS", "View logs for managed users"),
                new("CREATE_LOG", "Create new log entries"),
                new("DELETE_LOG", "Remove log entries"),
                new("SEARCH_LOGS", "Advanced log search capabilities"),
                new("EXPORT_LOGS", "Export logs to CSV/other formats"),
                new("EXPORT_ALL_LOGS", "Export any logs in system"),
                new("READ_LOG_STATISTICS", "View log analytics"),
                new("READ_ALL_LOG_STATISTICS", "View system-wide log analytics"),
                new("CLEANUP_LOGS", "Clean up old log entries"),

                // Update Management permissions (NEW)
                new("READ_ALL_UPDATES", "View all software updates"),
                new("READ_UPDATE", "View individual update details"),
                new("CREATE_UPDATES", "Create new software updates"),
                new("UPDATE_UPDATES", "Update existing software updates"),
                new("DELETE_UPDATES", "Delete software updates"),
                new("MANAGE_ALL_UPDATES", "Full update management access"),
                new("PUBLISH_UPDATES", "Publish updates for distribution"),
                new("DEPRECATE_UPDATES", "Mark updates as deprecated"),
                new("VIEW_UPDATE_STATISTICS", "View update analytics and statistics"),
                new("MANAGE_UPDATE_LIFECYCLE", "Manage update lifecycle (active, deprecated, recalled)"),
                new("VALIDATE_UPDATE_CHECKSUMS", "Validate and generate update checksums"),
                new("MANAGE_UPDATE_PLATFORMS", "Manage platform-specific update settings"),
                new("SCHEDULE_UPDATES", "Schedule automatic update deployments"),
                new("ROLLBACK_UPDATES", "Manage update rollback operations"),
                new("VIEW_UPDATE_DOWNLOADS", "View update download statistics and logs"),

                // Profile Management permissions (NEW)
                new("VIEW_PROFILE", "View own profile information"),
                new("UPDATE_PROFILE", "Update own profile information"),
                new("VIEW_USER_PROFILE", "View other user profiles"),
                new("VIEW_SENSITIVE_PROFILE_INFO", "View sensitive profile information"),
                new("VIEW_HIERARCHY", "View user hierarchy and reporting relationships"),
                new("VIEW_ORGANIZATION_HIERARCHY", "View complete organizational hierarchy"),
                new("MANAGE_HIERARCHY", "Manage user hierarchy and reporting relationships"),
                new("ASSIGN_DIRECT_REPORTS", "Assign direct reports to managers"),
                new("SEARCH_USERS", "Search users across the system"),
                new("VIEW_PROFILE_ANALYTICS", "View profile analytics and statistics"),
                new("MANAGE_USER_RELATIONSHIPS", "Manage relationships between users"),
                new("VIEW_USER_ACTIVITY", "View user activity and recent actions"),
                new("EXPORT_USER_DATA", "Export user profile and activity data"),
                new("VIEW_SUBORDINATE_PROFILES", "View profiles of subordinate users"),
                new("MANAGE_TEAM_MEMBERS", "Manage team member profiles and assignments"),

                // System administration permissions
                new("SYSTEM_ADMIN", "System administration access"),
                new("DATABASE_MANAGEMENT", "Database management operations"),
                new("BACKUP_RESTORE", "Backup and restore operations"),
                new("AUDIT_TRAIL", "Access audit trail information"),
                new("SECURITY_MANAGEMENT", "Security configuration management")
            };
        }

        private List<RolePermissionMapping> GetRolePermissionMappings()
        {
            return new List<RolePermissionMapping>
            {
                new("SuperAdmin", new[]
                {
                    "FullAccess", "SystemLogs", "SYSTEM_ADMIN", "DATABASE_MANAGEMENT", 
                    "BACKUP_RESTORE", "AUDIT_TRAIL", "SECURITY_MANAGEMENT",
                    "READ_ALL_MACHINES", "CREATE_MACHINE", "UPDATE_MACHINE", "DELETE_MACHINE",
                    "MANAGE_MACHINE_LICENSES", "READ_MACHINE_STATISTICS",
                    "READ_ALL_USERS", "CREATE_USER", "UPDATE_USER", "DELETE_USER",
                    "ASSIGN_ROLES", "CHANGE_USER_PASSWORDS", "READ_USER_STATISTICS", "MANAGE_USER_HIERARCHY",
                    "READ_ALL_REPORTS", "CREATE_REPORTS_FOR_OTHERS", "UPDATE_ALL_REPORTS", 
                    "DELETE_ALL_REPORTS", "EXPORT_ALL_REPORTS", "READ_ALL_REPORT_STATISTICS",
                    "READ_ALL_COMMANDS", "CREATE_COMMAND", "UPDATE_COMMAND", "DELETE_COMMAND", 
                    "MANAGE_COMMANDS", "READ_COMMAND_STATISTICS", "BULK_UPDATE_COMMANDS",
                    "READ_ALL_SESSIONS", "END_ALL_SESSIONS", "READ_ALL_SESSION_STATISTICS", "CLEANUP_SESSIONS",
                    "READ_ALL_LOGS", "CREATE_LOG", "DELETE_LOG", "SEARCH_LOGS", 
                    "EXPORT_ALL_LOGS", "READ_ALL_LOG_STATISTICS", "CLEANUP_LOGS",
                    // Update Management (SuperAdmin)
                    "READ_ALL_UPDATES", "CREATE_UPDATES", "UPDATE_UPDATES", "DELETE_UPDATES", "MANAGE_ALL_UPDATES",
                    "PUBLISH_UPDATES", "DEPRECATE_UPDATES", "VIEW_UPDATE_STATISTICS", "MANAGE_UPDATE_LIFECYCLE",
                    "VALIDATE_UPDATE_CHECKSUMS", "MANAGE_UPDATE_PLATFORMS", "SCHEDULE_UPDATES", 
                    "ROLLBACK_UPDATES", "VIEW_UPDATE_DOWNLOADS",
                    // Profile Management (SuperAdmin)
                    "VIEW_PROFILE", "UPDATE_PROFILE", "VIEW_USER_PROFILE", "VIEW_SENSITIVE_PROFILE_INFO",
                    "VIEW_HIERARCHY", "VIEW_ORGANIZATION_HIERARCHY", "MANAGE_HIERARCHY", "ASSIGN_DIRECT_REPORTS",
                    "SEARCH_USERS", "VIEW_PROFILE_ANALYTICS", "MANAGE_USER_RELATIONSHIPS", "VIEW_USER_ACTIVITY",
                    "EXPORT_USER_DATA", "VIEW_SUBORDINATE_PROFILES", "MANAGE_TEAM_MEMBERS"
                }),

                new("Admin", new[]
                {
                    "UserManagement", "ReportAccess", "MachineManagement", "LicenseManagement", "SystemLogs",
                    "READ_ALL_MACHINES", "CREATE_MACHINE", "UPDATE_MACHINE", "MANAGE_MACHINE_LICENSES", "READ_MACHINE_STATISTICS",
                    "READ_ALL_USERS", "CREATE_USER", "UPDATE_USER", "ASSIGN_ROLES", "CHANGE_USER_PASSWORDS", 
                    "READ_USER_STATISTICS", "MANAGE_USER_HIERARCHY",
                    "READ_ALL_REPORTS", "UPDATE_ALL_REPORTS", "DELETE_ALL_REPORTS", 
                    "EXPORT_ALL_REPORTS", "READ_ALL_REPORT_STATISTICS",
                    "READ_ALL_COMMANDS", "CREATE_COMMAND", "UPDATE_COMMAND", "READ_COMMAND_STATISTICS", "BULK_UPDATE_COMMANDS",
                    "READ_ALL_SESSIONS", "END_ALL_SESSIONS", "READ_ALL_SESSION_STATISTICS",
                    "READ_ALL_LOGS", "CREATE_LOG", "SEARCH_LOGS", "EXPORT_ALL_LOGS", "READ_ALL_LOG_STATISTICS",
                    // Update Management (Admin)
                    "READ_ALL_UPDATES", "CREATE_UPDATES", "UPDATE_UPDATES", "DELETE_UPDATES", "MANAGE_ALL_UPDATES",
                    "PUBLISH_UPDATES", "DEPRECATE_UPDATES", "VIEW_UPDATE_STATISTICS", "MANAGE_UPDATE_LIFECYCLE",
                    "VALIDATE_UPDATE_CHECKSUMS", "MANAGE_UPDATE_PLATFORMS", "VIEW_UPDATE_DOWNLOADS",
                    // Profile Management (Admin)
                    "VIEW_PROFILE", "UPDATE_PROFILE", "VIEW_USER_PROFILE", "VIEW_SENSITIVE_PROFILE_INFO",
                    "VIEW_HIERARCHY", "VIEW_ORGANIZATION_HIERARCHY", "MANAGE_HIERARCHY", "ASSIGN_DIRECT_REPORTS",
                    "SEARCH_USERS", "VIEW_PROFILE_ANALYTICS", "MANAGE_USER_RELATIONSHIPS", "VIEW_USER_ACTIVITY",
                    "EXPORT_USER_DATA", "VIEW_SUBORDINATE_PROFILES", "MANAGE_TEAM_MEMBERS"
                }),

                new("Manager", new[]
                {
                    "ReportAccess", "MachineManagement", "ViewOnly",
                    "READ_ALL_MACHINES", "UPDATE_MACHINE", "MANAGE_MACHINE_LICENSES", "READ_MACHINE_STATISTICS",
                    "READ_USER", "UPDATE_USER", "READ_USER_STATISTICS",
                    "READ_ALL_REPORTS", "READ_USER_REPORTS", "UPDATE_REPORT", "EXPORT_REPORTS", "READ_REPORT_STATISTICS",
                    "READ_COMMAND", "MANAGE_COMMANDS", "UPDATE_COMMAND_STATUS", "READ_COMMAND_STATISTICS",
                    "READ_USER_SESSIONS", "END_USER_SESSIONS", "READ_SESSION_STATISTICS",
                    "READ_USER_LOGS", "SEARCH_LOGS", "READ_LOG_STATISTICS",
                    // Update Management (Manager) - Limited permissions
                    "READ_ALL_UPDATES", "CREATE_UPDATES", "UPDATE_UPDATES", "VIEW_UPDATE_STATISTICS",
                    "PUBLISH_UPDATES", "VIEW_UPDATE_DOWNLOADS",
                    // Profile Management (Manager)
                    "VIEW_PROFILE", "UPDATE_PROFILE", "VIEW_USER_PROFILE", "VIEW_HIERARCHY", 
                    "ASSIGN_DIRECT_REPORTS", "SEARCH_USERS", "VIEW_USER_ACTIVITY", 
                    "VIEW_SUBORDINATE_PROFILES", "MANAGE_TEAM_MEMBERS"
                }),

                new("Support", new[]
                {
                    "ReportAccess", "ViewOnly", "SystemLogs",
                    "READ_MACHINE", "READ_MACHINE_STATISTICS",
                    "READ_USER", "READ_USER_STATISTICS",
                    "READ_REPORT", "READ_USER_REPORTS", "EXPORT_REPORTS", "READ_REPORT_STATISTICS",
                    "READ_COMMAND", "UPDATE_COMMAND_STATUS", "READ_COMMAND_STATISTICS",
                    "READ_SESSION", "READ_USER_SESSIONS", "READ_SESSION_STATISTICS",
                    "READ_LOG", "READ_USER_LOGS", "SEARCH_LOGS", "READ_LOG_STATISTICS",
                    // Update Management (Support) - Read-only access
                    "READ_ALL_UPDATES", "VIEW_UPDATE_STATISTICS", "VIEW_UPDATE_DOWNLOADS",
                    // Profile Management (Support)
                    "VIEW_PROFILE", "UPDATE_PROFILE", "VIEW_USER_PROFILE", "VIEW_HIERARCHY", 
                    "SEARCH_USERS", "VIEW_USER_ACTIVITY"
                }),

                new("User", new[]
                {
                    "ViewOnly",
                    "READ_MACHINE",
                    "READ_USER",
                    "READ_REPORT", "UPDATE_REPORT", "EXPORT_REPORTS",
                    "READ_SESSION", "END_SESSION", "EXTEND_SESSION",
                    "READ_LOG",
                    // Update Management (User) - Basic read access only
                    "READ_UPDATE",
                    // Profile Management (User) - Basic profile access only
                    "VIEW_PROFILE", "UPDATE_PROFILE"
                }),

                new("SubUser", new[]
                {
                    "ViewOnly",
                    "READ_MACHINE",
                    "READ_REPORT", "EXPORT_REPORTS",
                    "READ_SESSION", "END_SESSION",
                    "READ_LOG",
                    // Update Management (SubUser) - Very limited access
                    "READ_UPDATE",
                    // Profile Management (SubUser) - Basic profile access only
                    "VIEW_PROFILE"
                })
            };
        }
    }

    public class RequiredPermission
    {
        public string Name { get; }
        public string Description { get; }

        public RequiredPermission(string name, string description)
        {
            Name = name;
            Description = description;
        }
    }

    public class RolePermissionMapping
    {
        public string RoleName { get; }
        public string[] Permissions { get; }

        public RolePermissionMapping(string roleName, string[] permissions)
        {
            RoleName = roleName;
            Permissions = permissions;
        }
    }

    // Simplified result classes
    public class PermissionOperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int PermissionsCreated { get; set; }
        public List<string> CreatedPermissions { get; set; } = new();
    }

    public class RoleMappingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int MappingsCreated { get; set; }
    }
}