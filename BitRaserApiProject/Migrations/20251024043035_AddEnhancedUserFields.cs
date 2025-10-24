using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BitRaserApiProject.Migrations
{
    /// <inheritdoc />
    public partial class AddEnhancedUserFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubuserRoles_Roles_RoleId",
                table: "SubuserRoles");

            migrationBuilder.AlterColumn<bool>(
                name: "private_api",
                table: "Users",
                type: "tinyint(1)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AlterColumn<string>(
                name: "phone_number",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "payment_details_json",
                table: "Users",
                type: "json",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<string>(
                name: "license_details_json",
                table: "Users",
                type: "json",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "json")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<bool>(
                name: "is_private_cloud",
                table: "Users",
                type: "tinyint(1)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)");

            migrationBuilder.AddColumn<string>(
                name: "department",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_login",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "license_allocation",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "status",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "user_group",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "user_role",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "auto_download_enabled",
                table: "Updates",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "checksum_md5",
                table: "Updates",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "checksum_sha256",
                table: "Updates",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "created_at",
                table: "Updates",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "created_by_email",
                table: "Updates",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "deprecation_date",
                table: "Updates",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "file_size_bytes",
                table: "Updates",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "installation_notes",
                table: "Updates",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "minimum_os_version",
                table: "Updates",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "requires_restart",
                table: "Updates",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "rollback_version",
                table: "Updates",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "security_notes",
                table: "Updates",
                type: "varchar(1000)",
                maxLength: 1000,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "supported_platforms",
                table: "Updates",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "update_status",
                table: "Updates",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "update_type",
                table: "Updates",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "updated_at",
                table: "Updates",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "AccessLevel",
                table: "subuser",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "AssignedMachines",
                table: "subuser",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "CanAssignLicenses",
                table: "subuser",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanCreateSubusers",
                table: "subuser",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanManageMachines",
                table: "subuser",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "CanViewReports",
                table: "subuser",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "subuser",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedBy",
                table: "subuser",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Department",
                table: "subuser",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "EmailNotifications",
                table: "subuser",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "FailedLoginAttempts",
                table: "subuser",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "subuser",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsEmailVerified",
                table: "subuser",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "JobTitle",
                table: "subuser",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LastLoginAt",
                table: "subuser",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastLoginIp",
                table: "subuser",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "LicenseIdsJson",
                table: "subuser",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "LockedUntil",
                table: "subuser",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MachineIdsJson",
                table: "subuser",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "MaxMachines",
                table: "subuser",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "subuser",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "subuser",
                type: "varchar(500)",
                maxLength: 500,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "PermissionsJson",
                table: "subuser",
                type: "json",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Phone",
                table: "subuser",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Role",
                table: "subuser",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "subuser",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "SystemAlerts",
                table: "subuser",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "subuser",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedBy",
                table: "subuser",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "subuser_username",
                table: "subuser",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GeneratedReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReportId = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReportTitle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReportType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FromDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ToDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Format = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfigurationJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FilePath = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    GeneratedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    GeneratedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsScheduled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    ScheduleId = table.Column<int>(type: "int", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedReports", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Groups",
                columns: table => new
                {
                    group_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    license_allocation = table.Column<int>(type: "int", nullable: false),
                    permissions_json = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Groups", x => x.group_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ReportTemplates",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TemplateName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReportType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfigurationJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsDefault = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportTemplates", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "ScheduledReports",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ReportTitle = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ReportType = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConfigurationJson = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Frequency = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DayOfWeek = table.Column<int>(type: "int", nullable: false),
                    DayOfMonth = table.Column<int>(type: "int", nullable: false),
                    HourOfDay = table.Column<int>(type: "int", nullable: false),
                    NextRunDate = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastRunDate = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RecipientEmails = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledReports", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "SystemSettings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SettingKey = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SettingValue = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    IsEncrypted = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    IsSystemSetting = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SystemSettings", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3062));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3066));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "Description" },
                values: new object[] { new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3069), "Access and manage audit reports" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3072));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3074));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3077));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3080));

            migrationBuilder.InsertData(
                table: "Permissions",
                columns: new[] { "PermissionId", "CreatedAt", "Description", "PermissionName" },
                values: new object[,]
                {
                    { 8, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3083), "View all machines in the system", "READ_ALL_MACHINES" },
                    { 9, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3085), "View individual machine details", "READ_MACHINE" },
                    { 10, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3088), "Register new machines", "CREATE_MACHINE" },
                    { 11, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3090), "Update machine information", "UPDATE_MACHINE" },
                    { 12, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3093), "Update any machine in system", "UPDATE_ALL_MACHINES" },
                    { 13, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3096), "Remove machines from system", "DELETE_MACHINE" },
                    { 14, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3098), "Remove any machine in system", "DELETE_ALL_MACHINES" },
                    { 15, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3101), "Activate, deactivate, and renew licenses", "MANAGE_MACHINE_LICENSES" },
                    { 16, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3103), "Manage licenses for any machine", "MANAGE_ALL_MACHINE_LICENSES" },
                    { 17, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3106), "View machine analytics and statistics", "READ_MACHINE_STATISTICS" },
                    { 18, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3109), "View machines for managed users", "READ_MANAGED_USER_MACHINES" },
                    { 19, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3111), "View all users in the system", "READ_ALL_USERS" },
                    { 20, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3114), "View individual user details", "READ_USER" },
                    { 21, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3116), "View users in management hierarchy", "READ_MANAGED_USERS" },
                    { 22, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3119), "Create new user accounts", "CREATE_USER" },
                    { 23, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3121), "Update user information", "UPDATE_USER" },
                    { 24, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3124), "Remove user accounts", "DELETE_USER" },
                    { 25, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3126), "Assign and remove user roles", "ASSIGN_ROLES" },
                    { 26, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3129), "Change user passwords", "CHANGE_USER_PASSWORDS" },
                    { 27, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3132), "View user analytics and statistics", "READ_USER_STATISTICS" },
                    { 28, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3256), "Manage user reporting relationships", "MANAGE_USER_HIERARCHY" },
                    { 29, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3259), "View all subusers in the system", "READ_ALL_SUBUSERS" },
                    { 30, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3261), "View individual subuser details", "READ_SUBUSER" },
                    { 31, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3264), "View subusers for managed users", "READ_USER_SUBUSERS" },
                    { 32, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3267), "Create new subuser accounts", "CREATE_SUBUSER" },
                    { 33, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3269), "Create subusers for other users", "CREATE_SUBUSERS_FOR_OTHERS" },
                    { 34, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3272), "Update subuser information", "UPDATE_SUBUSER" },
                    { 35, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3275), "Update any subuser in system", "UPDATE_ALL_SUBUSERS" },
                    { 36, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3277), "Remove subuser accounts", "DELETE_SUBUSER" },
                    { 37, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3280), "Remove any subuser in system", "DELETE_ALL_SUBUSERS" },
                    { 38, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3282), "Change subuser passwords", "CHANGE_SUBUSER_PASSWORD" },
                    { 39, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3285), "Change any subuser password", "CHANGE_ALL_SUBUSER_PASSWORDS" },
                    { 40, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3288), "Assign roles to subusers", "ASSIGN_SUBUSER_ROLES" },
                    { 41, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3290), "Assign roles to any subuser", "ASSIGN_ALL_SUBUSER_ROLES" },
                    { 42, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3293), "Remove roles from subusers", "REMOVE_SUBUSER_ROLES" },
                    { 43, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3295), "Remove roles from any subuser", "REMOVE_ALL_SUBUSER_ROLES" },
                    { 44, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3298), "Reassign subusers to different parents", "REASSIGN_SUBUSERS" },
                    { 45, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3301), "View subuser analytics and statistics", "READ_SUBUSER_STATISTICS" },
                    { 46, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3303), "View system-wide subuser analytics", "READ_ALL_SUBUSER_STATISTICS" },
                    { 47, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3306), "View all audit reports", "READ_ALL_REPORTS" },
                    { 48, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3308), "View individual report details", "READ_REPORT" },
                    { 49, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3311), "View reports for managed users", "READ_USER_REPORTS" },
                    { 50, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3314), "Create reports on behalf of other users", "CREATE_REPORTS_FOR_OTHERS" },
                    { 51, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3316), "Update any report in system", "UPDATE_ALL_REPORTS" },
                    { 52, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3319), "Update own reports", "UPDATE_REPORT" },
                    { 53, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3322), "Delete any report in system", "DELETE_ALL_REPORTS" },
                    { 54, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3324), "Delete own reports", "DELETE_REPORT" },
                    { 55, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3327), "Export reports to PDF/CSV", "EXPORT_REPORTS" },
                    { 56, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3329), "Export any reports in system", "EXPORT_ALL_REPORTS" },
                    { 57, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3332), "View report analytics", "READ_REPORT_STATISTICS" },
                    { 58, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3335), "View system-wide report analytics", "READ_ALL_REPORT_STATISTICS" },
                    { 59, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3337), "View all system commands", "READ_ALL_COMMANDS" },
                    { 60, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3340), "View individual command details", "READ_COMMAND" },
                    { 61, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3342), "Create new commands", "CREATE_COMMAND" },
                    { 62, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3345), "Update command information", "UPDATE_COMMAND" },
                    { 63, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3347), "Update command execution status", "UPDATE_COMMAND_STATUS" },
                    { 64, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3350), "Remove commands from system", "DELETE_COMMAND" },
                    { 65, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3352), "Full command management access", "MANAGE_COMMANDS" },
                    { 66, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3355), "View command analytics and statistics", "READ_COMMAND_STATISTICS" },
                    { 67, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3357), "Bulk update multiple commands", "BULK_UPDATE_COMMANDS" },
                    { 68, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3360), "View all user sessions", "READ_ALL_SESSIONS" },
                    { 69, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3363), "View individual session details", "READ_SESSION" },
                    { 70, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3365), "View sessions for managed users", "READ_USER_SESSIONS" },
                    { 71, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3368), "End own sessions", "END_SESSION" },
                    { 72, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3370), "End any user session", "END_ALL_SESSIONS" },
                    { 73, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3373), "End sessions for managed users", "END_USER_SESSIONS" },
                    { 74, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3375), "Extend session duration", "EXTEND_SESSION" },
                    { 75, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3378), "View session analytics", "READ_SESSION_STATISTICS" },
                    { 76, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3381), "View system-wide session analytics", "READ_ALL_SESSION_STATISTICS" },
                    { 77, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3383), "Clean up expired sessions", "CLEANUP_SESSIONS" },
                    { 78, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3386), "View all system logs", "READ_ALL_LOGS" },
                    { 79, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3388), "View individual log entries", "READ_LOG" },
                    { 80, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3391), "View logs for managed users", "READ_USER_LOGS" },
                    { 81, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3393), "Create new log entries", "CREATE_LOG" },
                    { 82, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3396), "Remove log entries", "DELETE_LOG" },
                    { 83, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3399), "Advanced log search capabilities", "SEARCH_LOGS" },
                    { 84, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3401), "Export logs to CSV/other formats", "EXPORT_LOGS" },
                    { 85, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3404), "Export any logs in system", "EXPORT_ALL_LOGS" },
                    { 86, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3406), "View log analytics", "READ_LOG_STATISTICS" },
                    { 87, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3409), "View system-wide log analytics", "READ_ALL_LOG_STATISTICS" },
                    { 88, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3411), "Clean up old log entries", "CLEANUP_LOGS" },
                    { 89, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3414), "View own profile information", "VIEW_PROFILE" },
                    { 90, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3417), "Update own profile information", "UPDATE_PROFILE" },
                    { 91, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3419), "View other user profiles", "VIEW_USER_PROFILE" },
                    { 92, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3501), "View sensitive profile information", "VIEW_SENSITIVE_PROFILE_INFO" },
                    { 93, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3503), "View user hierarchy and reporting relationships", "VIEW_HIERARCHY" },
                    { 94, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3506), "View complete organizational hierarchy", "VIEW_ORGANIZATION_HIERARCHY" },
                    { 95, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3509), "Manage user hierarchy and reporting relationships", "MANAGE_HIERARCHY" },
                    { 96, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3511), "Assign direct reports to managers", "ASSIGN_DIRECT_REPORTS" },
                    { 97, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3514), "Search users across the system", "SEARCH_USERS" },
                    { 98, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3516), "View profile analytics and statistics", "VIEW_PROFILE_ANALYTICS" },
                    { 99, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3519), "Manage relationships between users", "MANAGE_USER_RELATIONSHIPS" },
                    { 100, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3521), "View user activity and recent actions", "VIEW_USER_ACTIVITY" },
                    { 101, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3524), "Export user profile and activity data", "EXPORT_USER_DATA" },
                    { 102, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3527), "View profiles of subordinate users", "VIEW_SUBORDINATE_PROFILES" },
                    { 103, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3529), "Manage team member profiles and assignments", "MANAGE_TEAM_MEMBERS" },
                    { 104, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3532), "System administration access", "SYSTEM_ADMIN" },
                    { 105, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3534), "Database management operations", "DATABASE_MANAGEMENT" },
                    { 106, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3537), "Backup and restore operations", "BACKUP_RESTORE" },
                    { 107, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3539), "Access audit trail information", "AUDIT_TRAIL" },
                    { 108, new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3542), "Security configuration management", "SECURITY_MANAGEMENT" }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(2666));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(2671));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(2675));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(2679));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(2682));

            migrationBuilder.CreateIndex(
                name: "IX_subuser_subuser_email",
                table: "subuser",
                column: "subuser_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedReports_ReportId",
                table: "GeneratedReports",
                column: "ReportId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SystemSettings_Category_SettingKey",
                table: "SystemSettings",
                columns: new[] { "Category", "SettingKey" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_SubuserRoles_Roles_RoleId",
                table: "SubuserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SubuserRoles_Roles_RoleId",
                table: "SubuserRoles");

            migrationBuilder.DropTable(
                name: "GeneratedReports");

            migrationBuilder.DropTable(
                name: "Groups");

            migrationBuilder.DropTable(
                name: "ReportTemplates");

            migrationBuilder.DropTable(
                name: "ScheduledReports");

            migrationBuilder.DropTable(
                name: "SystemSettings");

            migrationBuilder.DropIndex(
                name: "IX_subuser_subuser_email",
                table: "subuser");

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 23);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 24);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 25);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 26);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 27);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 28);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 29);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 30);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 31);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 32);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 33);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 34);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 35);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 36);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 37);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 38);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 39);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 40);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 41);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 42);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 43);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 44);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 45);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 46);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 47);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 48);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 49);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 50);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 51);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 52);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 53);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 54);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 55);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 56);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 57);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 58);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 59);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 60);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 61);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 62);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 63);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 64);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 65);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 66);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 67);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 68);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 69);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 70);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 71);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 72);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 73);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 74);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 75);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 76);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 77);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 78);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 79);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 80);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 81);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 82);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 83);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 84);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 85);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 86);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 87);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 88);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 89);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 90);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 91);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 92);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 93);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 94);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 95);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 96);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 97);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 98);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 99);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 100);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 101);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 102);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 103);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 104);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 105);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 106);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 107);

            migrationBuilder.DeleteData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 108);

            migrationBuilder.DropColumn(
                name: "department",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "last_login",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "license_allocation",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "user_group",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "user_role",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "auto_download_enabled",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "checksum_md5",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "checksum_sha256",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "created_at",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "created_by_email",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "deprecation_date",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "file_size_bytes",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "installation_notes",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "minimum_os_version",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "requires_restart",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "rollback_version",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "security_notes",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "supported_platforms",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "update_status",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "update_type",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "updated_at",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "AssignedMachines",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "CanAssignLicenses",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "CanCreateSubusers",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "CanManageMachines",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "CanViewReports",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "Department",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "EmailNotifications",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "FailedLoginAttempts",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "IsEmailVerified",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "JobTitle",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "LastLoginAt",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "LastLoginIp",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "LicenseIdsJson",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "LockedUntil",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "MachineIdsJson",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "MaxMachines",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "PermissionsJson",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "Phone",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "Role",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "SystemAlerts",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "UpdatedBy",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "subuser_username",
                table: "subuser");

            migrationBuilder.AlterColumn<bool>(
                name: "private_api",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "phone_number",
                keyValue: null,
                column: "phone_number",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "phone_number",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(20)",
                oldMaxLength: 20,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "payment_details_json",
                keyValue: null,
                column: "payment_details_json",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "payment_details_json",
                table: "Users",
                type: "json",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Users",
                keyColumn: "license_details_json",
                keyValue: null,
                column: "license_details_json",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "license_details_json",
                table: "Users",
                type: "json",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "json",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<bool>(
                name: "is_private_cloud",
                table: "Users",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1979));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1981));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                columns: new[] { "CreatedAt", "Description" },
                values: new object[] { new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1983), "Access and manage reports" });

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1984));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1986));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1987));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1988));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1777));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1780));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1782));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1783));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 48, 37, 14, DateTimeKind.Utc).AddTicks(1785));

            migrationBuilder.AddForeignKey(
                name: "FK_SubuserRoles_Roles_RoleId",
                table: "SubuserRoles",
                column: "RoleId",
                principalTable: "Roles",
                principalColumn: "RoleId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
