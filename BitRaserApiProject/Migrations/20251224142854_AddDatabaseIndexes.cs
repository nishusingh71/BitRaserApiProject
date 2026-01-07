using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace DSecureApi.Migrations
{
    /// <inheritdoc />
    public partial class AddDatabaseIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccessLevel",
                table: "subuser");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "subuser",
                newName: "status");

            migrationBuilder.RenameColumn(
                name: "LastLoginAt",
                table: "subuser",
                newName: "last_logout");

            migrationBuilder.RenameColumn(
                name: "JobTitle",
                table: "subuser",
                newName: "timezone");

            migrationBuilder.AddColumn<string>(
                name: "activity_status",
                table: "Users",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "domain",
                table: "Users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "is_domain_admin",
                table: "Users",
                type: "tinyint(1)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "last_logout",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "organization_name",
                table: "Users",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "private_db_connection_string",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "private_db_created_at",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "private_db_last_validated",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "private_db_schema_version",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "private_db_status",
                table: "Users",
                type: "varchar(20)",
                maxLength: 20,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "timezone",
                table: "Users",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AlterColumn<int>(
                name: "superuser_id",
                table: "subuser",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "status",
                table: "subuser",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "activity_status",
                table: "subuser",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "domain",
                table: "subuser",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "last_login",
                table: "subuser",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "license_allocation",
                table: "subuser",
                type: "int",
                nullable: true,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "organization_name",
                table: "subuser",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "subuser_group",
                table: "subuser",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "machine_details_json",
                table: "Machines",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "user_email",
                table: "Commands",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "database_routing_cache",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserEmail = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ParentEmail = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TargetDatabase = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ConnectionStringHash = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CachedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_database_routing_cache", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "forgot_password_requests",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "user")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    otp = table.Column<string>(type: "varchar(6)", maxLength: 6, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    reset_token = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_used = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ip_address = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_agent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_forgot_password_requests", x => x.id);
                    table.ForeignKey(
                        name: "FK_forgot_password_requests_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "license_usage_logs",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    license_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hwid = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    action = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_edition = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    new_edition = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    old_expiry_days = table.Column<int>(type: "int", nullable: true),
                    new_expiry_days = table.Column<int>(type: "int", nullable: true),
                    ip_address = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_agent = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_license_usage_logs", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "licenses",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    license_key = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    hwid = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    expiry_days = table.Column<int>(type: "int", nullable: false),
                    edition = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    server_revision = table.Column<int>(type: "int", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    last_seen = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    user_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_licenses", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "private_cloud_databases",
                columns: table => new
                {
                    config_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_id = table.Column<int>(type: "int", nullable: false),
                    user_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    connection_string = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    database_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    server_host = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    server_port = table.Column<int>(type: "int", nullable: false),
                    database_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    database_username = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    selected_tables = table.Column<string>(type: "json", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    test_status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_tested_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    schema_initialized = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    schema_initialized_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    notes = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_private_cloud_databases", x => x.config_id);
                    table.ForeignKey(
                        name: "FK_private_cloud_databases_Users_user_id",
                        column: x => x.user_id,
                        principalTable: "Users",
                        principalColumn: "user_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "private_db_audit_log",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    UserEmail = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Operation = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    OperationStatus = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Details = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PerformedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    PerformedBy = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_private_db_audit_log", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8241));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8245));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8249));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8252));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8256));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8259));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8263));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8268));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8271));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8274));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8278));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8281));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8285));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8288));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8291));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8294));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8297));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8301));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8304));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8307));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8310));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8313));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8316));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8319));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 25,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8322));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 26,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8326));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 27,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8329));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 28,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8332));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 29,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8335));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 30,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8338));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 31,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8342));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 32,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8346));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 33,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8349));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 34,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8352));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 35,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8355));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 36,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8358));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 37,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8362));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 38,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8365));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 39,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8368));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 40,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8371));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 41,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8374));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 42,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8377));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 43,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8380));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 44,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8384));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 45,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8387));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 46,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8390));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 47,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8393));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 48,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8397));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 49,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8400));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 50,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8403));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 51,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8406));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 52,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8409));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 53,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8538));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 54,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8541));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 55,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8545));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 56,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8548));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 57,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8551));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 58,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8554));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 59,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8557));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 60,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8560));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 61,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8564));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 62,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8567));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 63,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8570));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 64,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8573));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 65,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8576));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 66,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8580));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 67,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8583));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 68,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8587));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 69,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8590));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 70,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8593));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 71,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8597));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 72,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8600));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 73,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8603));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 74,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8607));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 75,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8610));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 76,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8613));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 77,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8616));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 78,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8619));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 79,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8623));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 80,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8626));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 81,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8629));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 82,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8633));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 83,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8636));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 84,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8639));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 85,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8642));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 86,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8646));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 87,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8649));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 88,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8652));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 89,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8655));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 90,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8658));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 91,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8662));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 92,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8665));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 93,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8668));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 94,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8672));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 95,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8675));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 96,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8678));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 97,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8682));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 98,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8685));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 99,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8688));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 100,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8691));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 101,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8694));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 102,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8698));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 103,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8701));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 104,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8704));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 105,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8708));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 106,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8711));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 107,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8714));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 108,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(8717));

            migrationBuilder.InsertData(
                table: "RolePermissions",
                columns: new[] { "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 32, 2 },
                    { 32, 3 },
                    { 32, 4 }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(7509));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(7516));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(7521));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(7713));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 12, 24, 14, 28, 53, 258, DateTimeKind.Utc).AddTicks(7717));

            migrationBuilder.CreateIndex(
                name: "IX_users_is_private_cloud",
                table: "Users",
                column: "is_private_cloud");

            migrationBuilder.CreateIndex(
                name: "IX_subuser_status",
                table: "subuser",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_subuser_user_email",
                table: "subuser",
                column: "user_email");

            migrationBuilder.CreateIndex(
                name: "IX_subuser_user_email_status",
                table: "subuser",
                columns: new[] { "user_email", "status" });

            migrationBuilder.CreateIndex(
                name: "IX_sessions_login_time",
                table: "Sessions",
                column: "login_time");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_status",
                table: "Sessions",
                column: "session_status");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_user_email",
                table: "Sessions",
                column: "user_email");

            migrationBuilder.CreateIndex(
                name: "IX_sessions_user_email_login_time",
                table: "Sessions",
                columns: new[] { "user_email", "login_time" });

            migrationBuilder.CreateIndex(
                name: "IX_machines_user_email",
                table: "Machines",
                column: "user_email");

            migrationBuilder.CreateIndex(
                name: "IX_logs_created_at",
                table: "logs",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_logs_log_level",
                table: "logs",
                column: "log_level");

            migrationBuilder.CreateIndex(
                name: "IX_logs_user_email",
                table: "logs",
                column: "user_email");

            migrationBuilder.CreateIndex(
                name: "IX_logs_user_email_created_at",
                table: "logs",
                columns: new[] { "user_email", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_reports_client_email",
                table: "AuditReports",
                column: "client_email");

            migrationBuilder.CreateIndex(
                name: "IX_audit_reports_client_email_datetime",
                table: "AuditReports",
                columns: new[] { "client_email", "report_datetime" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_reports_report_datetime",
                table: "AuditReports",
                column: "report_datetime");

            migrationBuilder.CreateIndex(
                name: "IX_forgot_password_requests_email_expires_at",
                table: "forgot_password_requests",
                columns: new[] { "email", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "IX_forgot_password_requests_reset_token",
                table: "forgot_password_requests",
                column: "reset_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_forgot_password_requests_user_id_user_type",
                table: "forgot_password_requests",
                columns: new[] { "user_id", "user_type" });

            migrationBuilder.CreateIndex(
                name: "IX_private_cloud_databases_user_email",
                table: "private_cloud_databases",
                column: "user_email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_private_cloud_databases_user_id",
                table: "private_cloud_databases",
                column: "user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "database_routing_cache");

            migrationBuilder.DropTable(
                name: "forgot_password_requests");

            migrationBuilder.DropTable(
                name: "license_usage_logs");

            migrationBuilder.DropTable(
                name: "licenses");

            migrationBuilder.DropTable(
                name: "private_cloud_databases");

            migrationBuilder.DropTable(
                name: "private_db_audit_log");

            migrationBuilder.DropIndex(
                name: "IX_users_is_private_cloud",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_subuser_status",
                table: "subuser");

            migrationBuilder.DropIndex(
                name: "IX_subuser_user_email",
                table: "subuser");

            migrationBuilder.DropIndex(
                name: "IX_subuser_user_email_status",
                table: "subuser");

            migrationBuilder.DropIndex(
                name: "IX_sessions_login_time",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_sessions_status",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_sessions_user_email",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_sessions_user_email_login_time",
                table: "Sessions");

            migrationBuilder.DropIndex(
                name: "IX_machines_user_email",
                table: "Machines");

            migrationBuilder.DropIndex(
                name: "IX_logs_created_at",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "IX_logs_log_level",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "IX_logs_user_email",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "IX_logs_user_email_created_at",
                table: "logs");

            migrationBuilder.DropIndex(
                name: "IX_audit_reports_client_email",
                table: "AuditReports");

            migrationBuilder.DropIndex(
                name: "IX_audit_reports_client_email_datetime",
                table: "AuditReports");

            migrationBuilder.DropIndex(
                name: "IX_audit_reports_report_datetime",
                table: "AuditReports");

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 32, 2 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 32, 3 });

            migrationBuilder.DeleteData(
                table: "RolePermissions",
                keyColumns: new[] { "PermissionId", "RoleId" },
                keyValues: new object[] { 32, 4 });

            migrationBuilder.DropColumn(
                name: "activity_status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "domain",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "is_domain_admin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "last_logout",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "organization_name",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "private_db_connection_string",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "private_db_created_at",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "private_db_last_validated",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "private_db_schema_version",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "private_db_status",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "timezone",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "activity_status",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "domain",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "last_login",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "license_allocation",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "organization_name",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "subuser_group",
                table: "subuser");

            migrationBuilder.DropColumn(
                name: "machine_details_json",
                table: "Machines");

            migrationBuilder.DropColumn(
                name: "user_email",
                table: "Commands");

            migrationBuilder.RenameColumn(
                name: "status",
                table: "subuser",
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "timezone",
                table: "subuser",
                newName: "JobTitle");

            migrationBuilder.RenameColumn(
                name: "last_logout",
                table: "subuser",
                newName: "LastLoginAt");

            migrationBuilder.AlterColumn<int>(
                name: "superuser_id",
                table: "subuser",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.UpdateData(
                table: "subuser",
                keyColumn: "Status",
                keyValue: null,
                column: "Status",
                value: "");

            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "subuser",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldMaxLength: 50,
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "AccessLevel",
                table: "subuser",
                type: "varchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "")
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
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3069));

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

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3083));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3085));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3088));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3090));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3093));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3096));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3098));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3101));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3103));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3106));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3109));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3111));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3114));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3116));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3119));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3121));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3124));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 25,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3126));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 26,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3129));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 27,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3132));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 28,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3256));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 29,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3259));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 30,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3261));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 31,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3264));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 32,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3267));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 33,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3269));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 34,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3272));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 35,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3275));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 36,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3277));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 37,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3280));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 38,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3282));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 39,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3285));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 40,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3288));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 41,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3290));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 42,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3293));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 43,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3295));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 44,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3298));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 45,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3301));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 46,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3303));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 47,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3306));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 48,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3308));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 49,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3311));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 50,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3314));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 51,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3316));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 52,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3319));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 53,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3322));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 54,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3324));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 55,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3327));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 56,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3329));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 57,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3332));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 58,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3335));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 59,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3337));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 60,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3340));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 61,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3342));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 62,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3345));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 63,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3347));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 64,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3350));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 65,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3352));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 66,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3355));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 67,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3357));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 68,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3360));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 69,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3363));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 70,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3365));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 71,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3368));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 72,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3370));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 73,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3373));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 74,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3375));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 75,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3378));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 76,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3381));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 77,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3383));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 78,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3386));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 79,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3388));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 80,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3391));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 81,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3393));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 82,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3396));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 83,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3399));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 84,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3401));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 85,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3404));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 86,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3406));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 87,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3409));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 88,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3411));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 89,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3414));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 90,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3417));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 91,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3419));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 92,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3501));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 93,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3503));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 94,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3506));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 95,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3509));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 96,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3511));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 97,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3514));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 98,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3516));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 99,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3519));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 100,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3521));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 101,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3524));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 102,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3527));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 103,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3529));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 104,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3532));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 105,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3534));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 106,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3537));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 107,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3539));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 108,
                column: "CreatedAt",
                value: new DateTime(2025, 10, 24, 4, 30, 34, 896, DateTimeKind.Utc).AddTicks(3542));

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
        }
    }
}
