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
        public DbSet<Group> Groups { get; set; }
        
        // New DbSets for role-based system
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<SubuserRole> SubuserRoles { get; set; }
        public DbSet<Models.Route> Routes { get; set; }
        public DbSet<PermissionRoute> PermissionRoutes { get; set; }
        
        // System Settings and Report Generation
        public DbSet<SystemSetting> SystemSettings { get; set; }
        public DbSet<GeneratedReport> GeneratedReports { get; set; }
        public DbSet<ReportTemplate> ReportTemplates { get; set; }
        public DbSet<ScheduledReport> ScheduledReports { get; set; }
        
        // Private Cloud Database Management
        public DbSet<PrivateCloudDatabase> PrivateCloudDatabases { get; set; }
        
        // ✅ Forgot Password Requests (NO EMAIL - OTP returned in API response)
        public DbSet<ForgotPasswordRequest> ForgotPasswordRequests { get; set; }
        
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
                .HasIndex(s => s.subuser_email)
                .IsUnique();

            modelBuilder.Entity<subuser>()
                .Property(s => s.subuser_password)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<subuser>()
                .Property(s => s.user_email)
                .HasMaxLength(255)
                .IsRequired();

            modelBuilder.Entity<subuser>()
                .Property(s => s.Name)
                .HasMaxLength(100);

            modelBuilder.Entity<subuser>()
                .Property(s => s.Phone)
                .HasMaxLength(20);

            modelBuilder.Entity<subuser>()
                .Property(s => s.Department)
                .HasMaxLength(100);

            modelBuilder.Entity<subuser>()
                .Property(s => s.Role)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<subuser>()
                .Property(s => s.PermissionsJson)
                .HasColumnType("json");

            modelBuilder.Entity<subuser>()
                .Property(s => s.MachineIdsJson)
                .HasColumnType("json");

            modelBuilder.Entity<subuser>()
                .Property(s => s.LicenseIdsJson)
                .HasColumnType("json");

            modelBuilder.Entity<subuser>()
                .Property(s => s.status)
                .HasMaxLength(50);

            modelBuilder.Entity<subuser>()
                .Property(s => s.LastLoginIp)
                .HasMaxLength(500);

            modelBuilder.Entity<subuser>()
                .Property(s => s.Notes)
                .HasMaxLength(500);

            // ✅ ADD: subuser_group string field configuration
            modelBuilder.Entity<subuser>()
                .Property(s => s.subuser_group)
                .HasMaxLength(100)
                .HasColumnName("subuser_group");  // Explicitly map to database column

            // ✅ ADD: license_allocation field configuration
            modelBuilder.Entity<subuser>()
                .Property(s => s.license_allocation)
                .HasColumnName("license_allocation")  // Explicitly map to database column
                .HasDefaultValue(0);

            // Group table
            modelBuilder.Entity<Group>()
                .HasKey(g => g.group_id);

            modelBuilder.Entity<Group>()
                .Property(g => g.name)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<Group>()
                .Property(g => g.description)
                .HasMaxLength(500);

            modelBuilder.Entity<Group>()
                .Property(g => g.status)
                .HasMaxLength(50);

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
            modelBuilder.Entity<Models.Route>()
                .HasKey(r => r.RouteId);

            modelBuilder.Entity<Models.Route>()
                .Property(r => r.RoutePath)
                .HasMaxLength(500)
                .IsRequired();

            modelBuilder.Entity<Models.Route>()
                .Property(r => r.HttpMethod)
                .HasMaxLength(10)
                .IsRequired();

            modelBuilder.Entity<Models.Route>()
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

            //modelBuilder.Entity<SubuserRole>()
                //.HasOne(sr => sr.Subuser)
                //.WithMany(s => s.SubuserRoles)
                //.HasForeignKey(sr => sr.SubuserId)
                //.OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<SubuserRole>()
                .HasOne(sr => sr.Role)
                .WithMany(r => r.SubuserRoles)
                .HasForeignKey(sr => sr.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

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

            // SystemSetting table
            modelBuilder.Entity<SystemSetting>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<SystemSetting>()
                .Property(s => s.SettingKey)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<SystemSetting>()
                .Property(s => s.Category)
                .HasMaxLength(50)
                .IsRequired();

            modelBuilder.Entity<SystemSetting>()
                .HasIndex(s => new { s.Category, s.SettingKey })
                .IsUnique();

            // GeneratedReport table
            modelBuilder.Entity<GeneratedReport>()
                .HasKey(g => g.Id);

            modelBuilder.Entity<GeneratedReport>()
                .Property(g => g.ReportId)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<GeneratedReport>()
                .HasIndex(g => g.ReportId)
                .IsUnique();

            // ReportTemplate table
            modelBuilder.Entity<ReportTemplate>()
                .HasKey(t => t.Id);

            modelBuilder.Entity<ReportTemplate>()
                .Property(t => t.TemplateName)
                .HasMaxLength(100)
                .IsRequired();

            // ScheduledReport table
            modelBuilder.Entity<ScheduledReport>()
                .HasKey(s => s.Id);

            modelBuilder.Entity<ScheduledReport>()
                .Property(s => s.ReportTitle)
                .HasMaxLength(200)
                .IsRequired();

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
                // ✅ Admin gets CREATE_SUBUSER permission
                new RolePermission { RoleId = 2, PermissionId = 32 },
       
                // Manager gets limited permissions
                new RolePermission { RoleId = 3, PermissionId = 3 },
                new RolePermission { RoleId = 3, PermissionId = 4 },
                new RolePermission { RoleId = 3, PermissionId = 5 },
     // ✅ Manager gets CREATE_SUBUSER permission
      new RolePermission { RoleId = 3, PermissionId = 32 },
       
       // Support gets support-related permissions
                new RolePermission { RoleId = 4, PermissionId = 3 },
                new RolePermission { RoleId = 4, PermissionId = 5 },
  new RolePermission { RoleId = 4, PermissionId = 7 },
       // ✅ Support gets CREATE_SUBUSER permission
new RolePermission { RoleId = 4, PermissionId = 32 },
    
         // User gets only view access (NO CREATE_SUBUSER)
   new RolePermission { RoleId = 5, PermissionId = 5 }
       );

            // ✅ ForgotPasswordRequest table configuration
            modelBuilder.Entity<ForgotPasswordRequest>()
         .HasKey(f => f.Id);

  modelBuilder.Entity<ForgotPasswordRequest>()
     .Property(f => f.Email)
  .HasMaxLength(255)
      .IsRequired();

     modelBuilder.Entity<ForgotPasswordRequest>()
     .Property(f => f.UserType)
    .HasMaxLength(20)
        .HasDefaultValue("user");

  modelBuilder.Entity<ForgotPasswordRequest>()
      .Property(f => f.Otp)
       .HasMaxLength(6)
     .IsRequired();

          modelBuilder.Entity<ForgotPasswordRequest>()
   .Property(f => f.ResetToken)
        .HasMaxLength(500)
         .IsRequired();

        modelBuilder.Entity<ForgotPasswordRequest>()
          .HasIndex(f => f.ResetToken)
  .IsUnique();

 modelBuilder.Entity<ForgotPasswordRequest>()
   .HasIndex(f => new { f.Email, f.ExpiresAt });

        modelBuilder.Entity<ForgotPasswordRequest>()
          .HasIndex(f => new { f.UserId, f.UserType });

       modelBuilder.Entity<ForgotPasswordRequest>()
.Property(f => f.IpAddress)
       .HasMaxLength(50);

        modelBuilder.Entity<ForgotPasswordRequest>()
  .Property(f => f.UserAgent)
     .HasMaxLength(500);

   // ✅ PrivateCloudDatabase table configuration
            modelBuilder.Entity<PrivateCloudDatabase>()
       .HasKey(p => p.ConfigId);

            modelBuilder.Entity<PrivateCloudDatabase>()
     .Property(p => p.UserEmail)
       .HasMaxLength(255)
    .IsRequired();

     modelBuilder.Entity<PrivateCloudDatabase>()
  .HasIndex(p => p.UserEmail)
  .IsUnique();

     modelBuilder.Entity<PrivateCloudDatabase>()
       .Property(p => p.ConnectionString)
       .IsRequired();

    modelBuilder.Entity<PrivateCloudDatabase>()
     .Property(p => p.DatabaseType)
        .HasMaxLength(50)
         .IsRequired();

    modelBuilder.Entity<PrivateCloudDatabase>()
         .Property(p => p.ServerHost)
       .HasMaxLength(255);

    modelBuilder.Entity<PrivateCloudDatabase>()
   .Property(p => p.DatabaseName)
  .HasMaxLength(255)
    .IsRequired();

     modelBuilder.Entity<PrivateCloudDatabase>()
      .Property(p => p.DatabaseUsername)
  .HasMaxLength(255)
          .IsRequired();

        // ✅ NEW: SelectedTables JSON field configuration
          modelBuilder.Entity<PrivateCloudDatabase>()
  .Property(p => p.SelectedTables)
         .HasColumnName("selected_tables")
 .HasColumnType("json");

modelBuilder.Entity<PrivateCloudDatabase>()
    .Property(p => p.TestStatus)
       .HasMaxLength(50);

            modelBuilder.Entity<PrivateCloudDatabase>()
  .Property(p => p.CreatedBy)
    .HasMaxLength(255);

 // Foreign key to users table
         modelBuilder.Entity<PrivateCloudDatabase>()
  .HasOne(p => p.User)
     .WithMany()
                .HasForeignKey(p => p.UserId)
        .OnDelete(DeleteBehavior.Cascade);
        }
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