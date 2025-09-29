using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using BitRaserApiProject.Models;
using Microsoft.EntityFrameworkCore;

namespace BitRaserApiProject
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        // DbSet representing the 'machines' table
        public DbSet<machines> Machines { get; set; }
        public DbSet<audit_reports> AuditReports { get; set; }
        public DbSet<users> Users { get; set; }
        public DbSet<Update> Updates { get; set; }
        public DbSet<Commands> Commands { get; set; }
        public DbSet<User_role_profile> User_role_profile { get; set; }
        public DbSet<Sessions> Sessions { get; set; }
        public DbSet<logs> logs { get; set; }
        public DbSet<subuser> subuser { get; set; }
        
        // New DbSets for role-based system
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<SubuserRole> SubuserRoles { get; set; }
        public DbSet<Route> Routes { get; set; }
        public DbSet<PermissionRoute> PermissionRoutes { get; set; }
        
        public static string HashLicenseKey(string licenseKey)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(licenseKey));
            return Convert.ToBase64String(bytes);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Machines Table
            modelBuilder.Entity<machines>()
                .HasKey(m => m.fingerprint_hash);

            modelBuilder.Entity<machines>()
                .Property(m => m.fingerprint_hash)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.mac_address)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.physical_drive_id)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.cpu_id)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.bios_serial)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<machines>()
                .Property(m => m.os_version)
                .HasMaxLength(255);

            modelBuilder.Entity<machines>()
                .Property(m => m.user_email)
                .HasMaxLength(255);

            modelBuilder.Entity<machines>()
                .Property(m => m.license_details_json)
                .HasColumnType("json");

            // Audit Reports Table
            modelBuilder.Entity<audit_reports>()
                .HasKey(a => a.report_id);

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.client_email)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.report_name)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.erasure_method)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<audit_reports>()
                .Property(a => a.report_details_json)
                .HasColumnType("json")
                .IsRequired();

            // Users Table
            modelBuilder.Entity<users>()
                .HasKey(u => u.user_id);

            modelBuilder.Entity<users>()
                .Property(u => u.user_name)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<users>()
                .Property(u => u.user_email)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<users>()
                .HasIndex(u => u.user_email)
                .IsUnique();

            modelBuilder.Entity<users>()
                .Property(u => u.user_password)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<users>()
                .Property(u => u.phone_number)
                .HasMaxLength(20);

            modelBuilder.Entity<users>()
                .Property(u => u.payment_details_json)
                .HasColumnType("json");

            modelBuilder.Entity<users>()
                .Property(u => u.license_details_json)
                .HasColumnType("json");

            // Commands table
            modelBuilder.Entity<Commands>()
                .HasKey(c => c.Command_id);

            modelBuilder.Entity<Commands>()
                .Property(c => c.command_text)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<Commands>()
                .Property(c => c.command_json)
                .HasColumnType("json");

            modelBuilder.Entity<Commands>()
                .Property(c => c.command_status)
                .HasMaxLength(100);

            // User role profile table
            modelBuilder.Entity<User_role_profile>()
                .HasKey(r => r.role_id);

            modelBuilder.Entity<User_role_profile>()
                .Property(r => r.role_name)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<User_role_profile>()
                .Property(r => r.role_email)
                .HasMaxLength(255);

            // Sessions table
            modelBuilder.Entity<Sessions>()
                .HasKey(s => s.session_id);

            modelBuilder.Entity<Sessions>()
                .Property(s => s.user_email)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<Sessions>()
                .Property(s => s.ip_address)
                .HasMaxLength(45); // Max length for IPv6

            modelBuilder.Entity<Sessions>()
                .Property(s => s.device_info)
                .HasMaxLength(1000);

            modelBuilder.Entity<Sessions>()
                .Property(s => s.session_status)
                .HasMaxLength(50)
                .IsRequired();

            // Logs table
            modelBuilder.Entity<logs>()
                .HasKey(l => l.log_id);

            modelBuilder.Entity<logs>()
                .Property(l => l.user_email)
                .HasMaxLength(255);

            modelBuilder.Entity<logs>()
                .Property(l => l.log_level)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<logs>()
                .Property(l => l.log_message)
                .HasMaxLength(2000)
                .IsRequired();

            modelBuilder.Entity<logs>()
                .Property(l => l.log_details_json)
                .HasColumnType("json");

            // Subuser table
            modelBuilder.Entity<subuser>()
                .HasKey(s => s.subuser_id);

            modelBuilder.Entity<subuser>()
                .Property(s => s.subuser_email)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<subuser>()
                .Property(s => s.subuser_password)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<subuser>()
                .Property(s => s.user_email)
                .HasMaxLength(255)
                .IsRequired();

            // Role-based system configurations
            
            // Roles table
            modelBuilder.Entity<Role>()
                .HasKey(r => r.RoleId);

            modelBuilder.Entity<Role>()
                .Property(r => r.RoleName)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            // Permissions table
            modelBuilder.Entity<Permission>()
                .HasKey(p => p.PermissionId);

            modelBuilder.Entity<Permission>()
                .Property(p => p.PermissionName)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Permission>()
                .HasIndex(p => p.PermissionName)
                .IsUnique();

            // Routes table
            modelBuilder.Entity<Route>()
                .HasKey(r => r.RouteId);

            modelBuilder.Entity<Route>()
                .Property(r => r.RoutePath)
                .HasMaxLength(500)
                .IsRequired();

            modelBuilder.Entity<Route>()
                .Property(r => r.HttpMethod)
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<Route>()
                .HasIndex(r => new { r.RoutePath, r.HttpMethod })
                .IsUnique();

            // RolePermission table (Many-to-Many)
            modelBuilder.Entity<RolePermission>()
                .HasKey(rp => new { rp.RoleId, rp.PermissionId });

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Role)
                .WithMany(r => r.RolePermissions)
                .HasForeignKey(rp => rp.RoleId);

            modelBuilder.Entity<RolePermission>()
                .HasOne(rp => rp.Permission)
                .WithMany(p => p.RolePermissions)
                .HasForeignKey(rp => rp.PermissionId);

            // UserRole table (Many-to-Many)
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // SubuserRole table (Many-to-Many)
            modelBuilder.Entity<SubuserRole>()
                .HasKey(sr => new { sr.SubuserId, sr.RoleId });

            modelBuilder.Entity<SubuserRole>()
                .HasOne(sr => sr.Subuser)
                .WithMany(s => s.SubuserRoles)
                .HasForeignKey(sr => sr.SubuserId);

            modelBuilder.Entity<SubuserRole>()
                .HasOne(sr => sr.Role)
                .WithMany(r => r.SubuserRoles)
                .HasForeignKey(sr => sr.RoleId);

            // PermissionRoute table (Many-to-Many)
            modelBuilder.Entity<PermissionRoute>()
                .HasKey(pr => new { pr.PermissionId, pr.RouteId });

            modelBuilder.Entity<PermissionRoute>()
                .HasOne(pr => pr.Permission)
                .WithMany(p => p.PermissionRoutes)
                .HasForeignKey(pr => pr.PermissionId);

            modelBuilder.Entity<PermissionRoute>()
                .HasOne(pr => pr.Route)
                .WithMany(r => r.PermissionRoutes)
                .HasForeignKey(pr => pr.RouteId);

            // Seed default roles
            modelBuilder.Entity<Role>().HasData(
                new Role { RoleId = 1, RoleName = "SuperAdmin", Description = "Full system access", HierarchyLevel = 1, CreatedAt = DateTime.UtcNow },
                new Role { RoleId = 2, RoleName = "Admin", Description = "Administrative access", HierarchyLevel = 2, CreatedAt = DateTime.UtcNow },
                new Role { RoleId = 3, RoleName = "Manager", Description = "Management access", HierarchyLevel = 3, CreatedAt = DateTime.UtcNow },
                new Role { RoleId = 4, RoleName = "Support", Description = "Support access", HierarchyLevel = 4, CreatedAt = DateTime.UtcNow },
                new Role { RoleId = 5, RoleName = "User", Description = "Basic user access", HierarchyLevel = 5, CreatedAt = DateTime.UtcNow }
            );

            // Seed enhanced permissions (85+ permissions from DynamicPermissionService)
            modelBuilder.Entity<Permission>().HasData(
                // Original permissions (7)
                new Permission { PermissionId = 1, PermissionName = "FullAccess", Description = "Complete system access", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 2, PermissionName = "UserManagement", Description = "Manage users and subusers", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 3, PermissionName = "ReportAccess", Description = "Access and manage audit reports", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 4, PermissionName = "MachineManagement", Description = "Manage machines", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 5, PermissionName = "ViewOnly", Description = "Read-only access", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 6, PermissionName = "LicenseManagement", Description = "Manage licenses", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 7, PermissionName = "SystemLogs", Description = "Access system logs", CreatedAt = DateTime.UtcNow },

                // Enhanced Machine permissions (8-18)
                new Permission { PermissionId = 8, PermissionName = "READ_ALL_MACHINES", Description = "View all machines in the system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 9, PermissionName = "READ_MACHINE", Description = "View individual machine details", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 10, PermissionName = "CREATE_MACHINE", Description = "Register new machines", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 11, PermissionName = "UPDATE_MACHINE", Description = "Update machine information", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 12, PermissionName = "UPDATE_ALL_MACHINES", Description = "Update any machine in system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 13, PermissionName = "DELETE_MACHINE", Description = "Remove machines from system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 14, PermissionName = "DELETE_ALL_MACHINES", Description = "Remove any machine in system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 15, PermissionName = "MANAGE_MACHINE_LICENSES", Description = "Activate, deactivate, and renew licenses", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 16, PermissionName = "MANAGE_ALL_MACHINE_LICENSES", Description = "Manage licenses for any machine", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 17, PermissionName = "READ_MACHINE_STATISTICS", Description = "View machine analytics and statistics", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 18, PermissionName = "READ_MANAGED_USER_MACHINES", Description = "View machines for managed users", CreatedAt = DateTime.UtcNow },

                // Enhanced User permissions (19-28)
                new Permission { PermissionId = 19, PermissionName = "READ_ALL_USERS", Description = "View all users in the system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 20, PermissionName = "READ_USER", Description = "View individual user details", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 21, PermissionName = "READ_MANAGED_USERS", Description = "View users in management hierarchy", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 22, PermissionName = "CREATE_USER", Description = "Create new user accounts", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 23, PermissionName = "UPDATE_USER", Description = "Update user information", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 24, PermissionName = "DELETE_USER", Description = "Remove user accounts", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 25, PermissionName = "ASSIGN_ROLES", Description = "Assign and remove user roles", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 26, PermissionName = "CHANGE_USER_PASSWORDS", Description = "Change user passwords", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 27, PermissionName = "READ_USER_STATISTICS", Description = "View user analytics and statistics", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 28, PermissionName = "MANAGE_USER_HIERARCHY", Description = "Manage user reporting relationships", CreatedAt = DateTime.UtcNow },

                // Enhanced Subuser permissions (29-46)
                new Permission { PermissionId = 29, PermissionName = "READ_ALL_SUBUSERS", Description = "View all subusers in the system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 30, PermissionName = "READ_SUBUSER", Description = "View individual subuser details", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 31, PermissionName = "READ_USER_SUBUSERS", Description = "View subusers for managed users", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 32, PermissionName = "CREATE_SUBUSER", Description = "Create new subuser accounts", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 33, PermissionName = "CREATE_SUBUSERS_FOR_OTHERS", Description = "Create subusers for other users", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 34, PermissionName = "UPDATE_SUBUSER", Description = "Update subuser information", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 35, PermissionName = "UPDATE_ALL_SUBUSERS", Description = "Update any subuser in system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 36, PermissionName = "DELETE_SUBUSER", Description = "Remove subuser accounts", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 37, PermissionName = "DELETE_ALL_SUBUSERS", Description = "Remove any subuser in system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 38, PermissionName = "CHANGE_SUBUSER_PASSWORD", Description = "Change subuser passwords", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 39, PermissionName = "CHANGE_ALL_SUBUSER_PASSWORDS", Description = "Change any subuser password", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 40, PermissionName = "ASSIGN_SUBUSER_ROLES", Description = "Assign roles to subusers", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 41, PermissionName = "ASSIGN_ALL_SUBUSER_ROLES", Description = "Assign roles to any subuser", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 42, PermissionName = "REMOVE_SUBUSER_ROLES", Description = "Remove roles from subusers", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 43, PermissionName = "REMOVE_ALL_SUBUSER_ROLES", Description = "Remove roles from any subuser", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 44, PermissionName = "REASSIGN_SUBUSERS", Description = "Reassign subusers to different parents", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 45, PermissionName = "READ_SUBUSER_STATISTICS", Description = "View subuser analytics and statistics", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 46, PermissionName = "READ_ALL_SUBUSER_STATISTICS", Description = "View system-wide subuser analytics", CreatedAt = DateTime.UtcNow },

                // Enhanced Report permissions (47-58)
                new Permission { PermissionId = 47, PermissionName = "READ_ALL_REPORTS", Description = "View all audit reports", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 48, PermissionName = "READ_REPORT", Description = "View individual report details", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 49, PermissionName = "READ_USER_REPORTS", Description = "View reports for managed users", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 50, PermissionName = "CREATE_REPORTS_FOR_OTHERS", Description = "Create reports on behalf of other users", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 51, PermissionName = "UPDATE_ALL_REPORTS", Description = "Update any report in system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 52, PermissionName = "UPDATE_REPORT", Description = "Update own reports", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 53, PermissionName = "DELETE_ALL_REPORTS", Description = "Delete any report in system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 54, PermissionName = "DELETE_REPORT", Description = "Delete own reports", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 55, PermissionName = "EXPORT_REPORTS", Description = "Export reports to PDF/CSV", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 56, PermissionName = "EXPORT_ALL_REPORTS", Description = "Export any reports in system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 57, PermissionName = "READ_REPORT_STATISTICS", Description = "View report analytics", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 58, PermissionName = "READ_ALL_REPORT_STATISTICS", Description = "View system-wide report analytics", CreatedAt = DateTime.UtcNow },

                // Enhanced Commands permissions (59-67)
                new Permission { PermissionId = 59, PermissionName = "READ_ALL_COMMANDS", Description = "View all system commands", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 60, PermissionName = "READ_COMMAND", Description = "View individual command details", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 61, PermissionName = "CREATE_COMMAND", Description = "Create new commands", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 62, PermissionName = "UPDATE_COMMAND", Description = "Update command information", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 63, PermissionName = "UPDATE_COMMAND_STATUS", Description = "Update command execution status", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 64, PermissionName = "DELETE_COMMAND", Description = "Remove commands from system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 65, PermissionName = "MANAGE_COMMANDS", Description = "Full command management access", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 66, PermissionName = "READ_COMMAND_STATISTICS", Description = "View command analytics and statistics", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 67, PermissionName = "BULK_UPDATE_COMMANDS", Description = "Bulk update multiple commands", CreatedAt = DateTime.UtcNow },

                // Enhanced Sessions permissions (68-77)
                new Permission { PermissionId = 68, PermissionName = "READ_ALL_SESSIONS", Description = "View all user sessions", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 69, PermissionName = "READ_SESSION", Description = "View individual session details", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 70, PermissionName = "READ_USER_SESSIONS", Description = "View sessions for managed users", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 71, PermissionName = "END_SESSION", Description = "End own sessions", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 72, PermissionName = "END_ALL_SESSIONS", Description = "End any user session", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 73, PermissionName = "END_USER_SESSIONS", Description = "End sessions for managed users", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 74, PermissionName = "EXTEND_SESSION", Description = "Extend session duration", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 75, PermissionName = "READ_SESSION_STATISTICS", Description = "View session analytics", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 76, PermissionName = "READ_ALL_SESSION_STATISTICS", Description = "View system-wide session analytics", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 77, PermissionName = "CLEANUP_SESSIONS", Description = "Clean up expired sessions", CreatedAt = DateTime.UtcNow },

                // Enhanced Logs permissions (78-88)
                new Permission { PermissionId = 78, PermissionName = "READ_ALL_LOGS", Description = "View all system logs", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 79, PermissionName = "READ_LOG", Description = "View individual log entries", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 80, PermissionName = "READ_USER_LOGS", Description = "View logs for managed users", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 81, PermissionName = "CREATE_LOG", Description = "Create new log entries", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 82, PermissionName = "DELETE_LOG", Description = "Remove log entries", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 83, PermissionName = "SEARCH_LOGS", Description = "Advanced log search capabilities", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 84, PermissionName = "EXPORT_LOGS", Description = "Export logs to CSV/other formats", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 85, PermissionName = "EXPORT_ALL_LOGS", Description = "Export any logs in system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 86, PermissionName = "READ_LOG_STATISTICS", Description = "View log analytics", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 87, PermissionName = "READ_ALL_LOG_STATISTICS", Description = "View system-wide log analytics", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 88, PermissionName = "CLEANUP_LOGS", Description = "Clean up old log entries", CreatedAt = DateTime.UtcNow },

                // Profile Management permissions (89-103)
                new Permission { PermissionId = 89, PermissionName = "VIEW_PROFILE", Description = "View own profile information", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 90, PermissionName = "UPDATE_PROFILE", Description = "Update own profile information", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 91, PermissionName = "VIEW_USER_PROFILE", Description = "View other user profiles", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 92, PermissionName = "VIEW_SENSITIVE_PROFILE_INFO", Description = "View sensitive profile information", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 93, PermissionName = "VIEW_HIERARCHY", Description = "View user hierarchy and reporting relationships", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 94, PermissionName = "VIEW_ORGANIZATION_HIERARCHY", Description = "View complete organizational hierarchy", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 95, PermissionName = "MANAGE_HIERARCHY", Description = "Manage user hierarchy and reporting relationships", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 96, PermissionName = "ASSIGN_DIRECT_REPORTS", Description = "Assign direct reports to managers", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 97, PermissionName = "SEARCH_USERS", Description = "Search users across the system", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 98, PermissionName = "VIEW_PROFILE_ANALYTICS", Description = "View profile analytics and statistics", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 99, PermissionName = "MANAGE_USER_RELATIONSHIPS", Description = "Manage relationships between users", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 100, PermissionName = "VIEW_USER_ACTIVITY", Description = "View user activity and recent actions", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 101, PermissionName = "EXPORT_USER_DATA", Description = "Export user profile and activity data", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 102, PermissionName = "VIEW_SUBORDINATE_PROFILES", Description = "View profiles of subordinate users", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 103, PermissionName = "MANAGE_TEAM_MEMBERS", Description = "Manage team member profiles and assignments", CreatedAt = DateTime.UtcNow },

                // System administration permissions (104-108)
                new Permission { PermissionId = 104, PermissionName = "SYSTEM_ADMIN", Description = "System administration access", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 105, PermissionName = "DATABASE_MANAGEMENT", Description = "Database management operations", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 106, PermissionName = "BACKUP_RESTORE", Description = "Backup and restore operations", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 107, PermissionName = "AUDIT_TRAIL", Description = "Access audit trail information", CreatedAt = DateTime.UtcNow },
                new Permission { PermissionId = 108, PermissionName = "SECURITY_MANAGEMENT", Description = "Security configuration management", CreatedAt = DateTime.UtcNow }
            );

            // Seed role-permission mappings
            modelBuilder.Entity<RolePermission>().HasData(
                // SuperAdmin gets all permissions
                new RolePermission { RoleId = 1, PermissionId = 1 },
                new RolePermission { RoleId = 1, PermissionId = 2 },
                new RolePermission { RoleId = 1, PermissionId = 3 },
                new RolePermission { RoleId = 1, PermissionId = 4 },
                new RolePermission { RoleId = 1, PermissionId = 5 },
                new RolePermission { RoleId = 1, PermissionId = 6 },
                new RolePermission { RoleId = 1, PermissionId = 7 },
                
                // Admin gets most permissions except FullAccess
                new RolePermission { RoleId = 2, PermissionId = 2 },
                new RolePermission { RoleId = 2, PermissionId = 3 },
                new RolePermission { RoleId = 2, PermissionId = 4 },
                new RolePermission { RoleId = 2, PermissionId = 5 },
                new RolePermission { RoleId = 2, PermissionId = 6 },
                new RolePermission { RoleId = 2, PermissionId = 7 },
                
                // Manager gets limited permissions
                new RolePermission { RoleId = 3, PermissionId = 3 },
                new RolePermission { RoleId = 3, PermissionId = 4 },
                new RolePermission { RoleId = 3, PermissionId = 5 },
                
                // Support gets support-related permissions
                new RolePermission { RoleId = 4, PermissionId = 3 },
                new RolePermission { RoleId = 4, PermissionId = 5 },
                new RolePermission { RoleId = 4, PermissionId = 7 },
                
                // User gets only view access
                new RolePermission { RoleId = 5, PermissionId = 5 }
            );
        }
    }

    // ...existing models...

    public class machines
    {
        [Key]
        [Required]
        public string fingerprint_hash { get; set; } // Unique machine identifier

        [Required, MaxLength(255)]
        public string mac_address { get; set; }

        [Required, MaxLength(255)]
        public string physical_drive_id { get; set; }

        [Required, MaxLength(255)]
        public string cpu_id { get; set; }

        [Required, MaxLength(255)]
        public string bios_serial { get; set; }

        [Required, MaxLength(255)]
        public string os_version { get; set; }

        [MaxLength(255)]
        public string user_email { get; set; }
        
        [MaxLength(255)]
        public string? subuser_email { get; set; }

        public bool license_activated { get; set; } // Activation status
        public DateTime? license_activation_date { get; set; } // Null if never activated
        public int license_days_valid { get; set; } = 0; // Number of valid days
        public string license_details_json { get; set; } // Stores license info
        public int demo_usage_count { get; set; } // Tracks demo usage count
        public DateTime created_at { get; set; } = DateTime.UtcNow; // Auto-set by DB
        public DateTime updated_at { get; set; } = DateTime.UtcNow; // Auto-updated by DB
        public string vm_status { get; set; } // 'physical' or 'vm'
    }

    public class audit_reports
    {
        [Key]
        public int report_id { get; set; } // Primary Key

        [Required, MaxLength(255)]
        public string client_email { get; set; } // Email of the client who performed erasure

        [Required, MaxLength(255)]
        public string report_name { get; set; } // Name of the report

        [Required, MaxLength(255)]
        public string erasure_method { get; set; } // Method used for erasure

        [Required]
        public DateTime report_datetime { get; set; } = DateTime.UtcNow; // Date and time of the report

        [Required]
        public string report_details_json { get; set; } // JSON containing detailed erasure process

        public bool synced { get; set; } = false; // Indicates if report is synced to cloud
    }

    public class users
    {
        [Key]
        public int user_id { get; set; } // Primary Key

        [Required, MaxLength(255)]
        public string user_name { get; set; } // Name of the user

        [Required, MaxLength(255)]
        public string user_email { get; set; } // Email (must be unique)

        [Required, MaxLength(255)]
        public string user_password { get; set; } // Plain password

        [JsonIgnore]
        public string? hash_password { get; set; } // Hashed password

        public bool is_private_cloud { get; set; } = false; // Private cloud flag
        public bool private_api { get; set; } = false; // Private API access flag

        [MaxLength(20)]
        public string phone_number { get; set; } // User's phone number
        public string payment_details_json { get; set; } // JSON storing payment details
        public string license_details_json { get; set; } // JSON storing license details
        
        public DateTime created_at { get; set; } = DateTime.UtcNow; // Account creation date
        public DateTime updated_at { get; set; } = DateTime.UtcNow; // Last update date
        
        // Navigation properties for role-based system - ignore in JSON to prevent circular references
        [JsonIgnore]
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }

    public class LoginRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }

    public class Update
    {
        [Key]
        public int version_id { get; set; }  // Primary Key

        [Required, MaxLength(20)]
        public string version_number { get; set; }  // e.g. "1.0.0"

        [Required]
        public string changelog { get; set; }  // Description of changes

        [Required, MaxLength(500)]
        public string download_link { get; set; }  // URL to installer

        public DateTime release_date { get; set; } = DateTime.UtcNow;  // Release timestamp

        public bool is_mandatory_update { get; set; } = false;  // Flag for mandatory update
    }

    public class subuser
    {
        [Key]
        public int subuser_id { get; set; } // Primary Key
        public int superuser_id { get; set; } // Reference to users.user_id (superuser)
        [Required, MaxLength(255)]
        public string subuser_email { get; set; } // Email of the subuser
        [Required, MaxLength(255)]
        public string subuser_password { get; set; } // Hashed password
        public string user_email { get; set; } // ID of the parent user
        
        // Navigation properties for role-based system - ignore in JSON to prevent circular references
        [JsonIgnore]
        public ICollection<SubuserRole> SubuserRoles { get; set; } = new List<SubuserRole>();
    }

    public class Sessions
    {
        [Key]
        public int session_id { get; set; } // Primary Key
        public string user_email { get; set; } // User email (instead of user_id)
        public DateTime login_time { get; set; } // Login timestamp
        public DateTime? logout_time { get; set; } // Logout timestamp (nullable)
        public string ip_address { get; set; } // User IP address
        public string device_info { get; set; } // Device/browser info
        public string session_status { get; set; } // Status: active, closed, expired
    }

    public class logs
    {
        [Key]
        public int log_id { get; set; } // Primary Key
        public string user_email { get; set; } // User email (nullable for system logs)
        public string log_level { get; set; } // e.g. Info, Warning, Error
        public string log_message { get; set; } // Log message
        public string log_details_json { get; set; } // Additional details in JSON
        public DateTime created_at { get; set; } = DateTime.UtcNow; // Timestamp of log creation
    }

    public class Commands
    {
        [Key]
        public int Command_id { get; set; } // Primary Key
        public string command_text { get; set; } // Command text
        public DateTime issued_at { get; set; } = DateTime.UtcNow; // When command was issued
        public string command_json { get; set; } // JSON parameters
        public string command_status { get; set; } // Status of command execution
    }

    public class User_role_profile
    {
        [Key]
        public int role_id { get; set; } // Primary Key
        public string user_email { get; set; } // User's email
        public int manage_user_id { get; set; } // User ID who manages this role
        public string role_name { get; set; } // Role name
        public string role_email { get; set; } // Role email
    }

    public class User_role
    {
        [Key]
        public int user_role_id { get; set; } // Primary Key
        public int user_id { get; set; } // Foreign Key to users table
        public int role_id { get; set; } // Foreign Key to User_role_profile table
        public DateTime assigned_at { get; set; } = DateTime.UtcNow; // Timestamp when role was assigned
        public DateTime updated_at { get; set; } // Timestamp when role was last updated    
    }

    // New Role-based System Models

    public class Role
    {
        [Key]
        public int RoleId { get; set; }
        
        [Required, MaxLength(100)]
        public string RoleName { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; }
        
        public int HierarchyLevel { get; set; } // 1 = highest (SuperAdmin), 5 = lowest (User)
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties - ignore in JSON to prevent circular references
        [JsonIgnore]
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        [JsonIgnore]
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        [JsonIgnore]
        public ICollection<SubuserRole> SubuserRoles { get; set; } = new List<SubuserRole>();
    }

    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }
        
        [Required, MaxLength(100)]
        public string PermissionName { get; set; }
        
        [MaxLength(500)]
        public string Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties - ignore in JSON to prevent circular references
        [JsonIgnore]
        public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
        [JsonIgnore]
        public ICollection<PermissionRoute> PermissionRoutes { get; set; } = new List<PermissionRoute>();
    }

    public class Route
    {
        [Key]
        public int RouteId { get; set; }
        
        [Required, MaxLength(500)]
        public string RoutePath { get; set; }
        
        [Required, MaxLength(10)]
        public string HttpMethod { get; set; }
        
        [MaxLength(200)]
        public string Description { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation properties - ignore in JSON to prevent circular references
        [JsonIgnore]
        public ICollection<PermissionRoute> PermissionRoutes { get; set; } = new List<PermissionRoute>();
    }

    // Junction tables
    public class RolePermission
    {
        public int RoleId { get; set; }
        public int PermissionId { get; set; }
        
        // Navigation properties - ignore in JSON to prevent circular references
        [JsonIgnore]
        public Role Role { get; set; }
        [JsonIgnore]
        public Permission Permission { get; set; }
    }

    public class UserRole
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedByEmail { get; set; } // Who assigned this role
        
        // Navigation properties - ignore in JSON to prevent circular references
        [JsonIgnore]
        public users User { get; set; }
        [JsonIgnore]
        public Role Role { get; set; }
    }

    public class SubuserRole
    {
        public int SubuserId { get; set; }
        public int RoleId { get; set; }
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public string AssignedByEmail { get; set; } // Who assigned this role
        
        // Navigation properties - ignore in JSON to prevent circular references
        [JsonIgnore]
        public subuser Subuser { get; set; }
        [JsonIgnore]
        public Role Role { get; set; }
    }

    public class PermissionRoute
    {
        public int PermissionId { get; set; }
        public int RouteId { get; set; }
        
        // Navigation properties - ignore in JSON to prevent circular references
        [JsonIgnore]
        public Permission Permission { get; set; }
        [JsonIgnore]
        public Route Route { get; set; }
    }

    public static class SecurityHelpers
    {
        public static string HashPassword(string password, out string salt)
        {
            salt = Convert.ToBase64String(RandomNumberGenerator.GetBytes(16));
            var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 100_000, HashAlgorithmName.SHA256);
            return Convert.ToBase64String(pbkdf2.GetBytes(32));
        }

        public static bool VerifyPassword(String password, String hash, String salt)
        {
            var pbkdf2 = new Rfc2898DeriveBytes(password, Convert.FromBase64String(salt), 100_000, HashAlgorithmName.SHA256);
            var computedHash = Convert.ToBase64String(pbkdf2.GetBytes(32));
            return CryptographicOperations.FixedTimeEquals(
                Convert.FromBase64String(hash),
                Convert.FromBase64String(computedHash)
            );
        }
    }
}