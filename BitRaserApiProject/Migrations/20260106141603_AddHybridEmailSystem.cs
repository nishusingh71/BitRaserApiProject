using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitRaserApiProject.Migrations
{
    /// <inheritdoc />
    public partial class AddHybridEmailSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "is_domain_admin",
                table: "Users",
                type: "varchar(10)",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "tinyint(1)",
                oldNullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<DateTime>(
                name: "license_expiry_date",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_departments",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_groups",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_licenses",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "max_subusers",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "quota_synced_at",
                table: "Users",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "used_licenses",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "used_subusers",
                table: "Users",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "activity_details",
                table: "Sessions",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "activity_type",
                table: "Sessions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "resource_id",
                table: "Sessions",
                type: "varchar(100)",
                maxLength: 100,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "resource_type",
                table: "Sessions",
                type: "varchar(50)",
                maxLength: 50,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "admin_user_id",
                table: "Groups",
                type: "varchar(255)",
                maxLength: 255,
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "contact_form_submissions",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    company = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    business_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    solution_type = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    compliance_requirements = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    message = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    usage_type = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    source = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    submitted_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_agent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_read = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    read_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    read_by = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_contact_form_submissions", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "downloads",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    product_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    version = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    platform = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    architecture = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ip_address = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_agent = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    downloaded_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    file_size = table.Column<long>(type: "bigint", nullable: false),
                    download_completed = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    city = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    referrer = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    download_source = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_downloads", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmailLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProviderUsed = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    RecipientEmail = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Subject = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    EmailType = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SentAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    OrderId = table.Column<int>(type: "int", nullable: true),
                    SendDurationMs = table.Column<int>(type: "int", nullable: true),
                    RetryCount = table.Column<int>(type: "int", nullable: false),
                    HasAttachments = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailLogs", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "EmailQuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ProviderName = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AccountIdentifier = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DailyLimit = table.Column<int>(type: "int", nullable: false),
                    DailySent = table.Column<int>(type: "int", nullable: false),
                    MonthlyLimit = table.Column<int>(type: "int", nullable: false),
                    MonthlySent = table.Column<int>(type: "int", nullable: false),
                    LastDailyReset = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    LastMonthlyReset = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsHealthy = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastErrorMessage = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    LastSuccessAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastFailureAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ConsecutiveFailures = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailQuotas", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GroupMembers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    GroupId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserEmail = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    UserName = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Role = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Department = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AddedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GroupMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GroupMembers_Groups_GroupId",
                        column: x => x.GroupId,
                        principalTable: "Groups",
                        principalColumn: "group_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "orders",
                columns: table => new
                {
                    order_id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    polar_order_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    polar_checkout_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dodo_payment_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dodo_invoice_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    dodo_customer_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    card_last_four = table.Column<string>(type: "varchar(4)", maxLength: 4, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    card_network = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    card_type = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    tax_amount_cents = table.Column<int>(type: "int", nullable: false),
                    payment_link = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payment_provider = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    first_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    last_name = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    phone_number = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    company_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    product_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    product_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    license_count = table.Column<int>(type: "int", nullable: false),
                    license_years = table.Column<int>(type: "int", nullable: false),
                    amount_cents = table.Column<int>(type: "int", nullable: false),
                    currency = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    status = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    payment_method = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    billing_country = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    billing_address = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    billing_city = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    billing_state = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    billing_zip = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    license_keys = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    paid_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    license_expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    webhook_payload = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notes = table.Column<string>(type: "TEXT", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    plan_id = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    user_id = table.Column<int>(type: "int", nullable: true),
                    webhook_processed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    user_created = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    credentials_email_sent = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orders", x => x.order_id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "pdf_export_settings",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    user_email = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    report_title = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    header_text = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    technician_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    technician_dept = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    validator_name = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    validator_dept = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    header_left_logo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    header_right_logo = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    watermark_image = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    technician_signature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    validator_signature = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    is_active = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_pdf_export_settings", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3019));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3023));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3026));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3029));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3031));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3034));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3037));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 8,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3039));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 9,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3042));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 10,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3045));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 11,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3047));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 12,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3050));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 13,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3052));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 14,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3055));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 15,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3058));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 16,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3060));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 17,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3063));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 18,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3065));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 19,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3068));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 20,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3070));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 21,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3073));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 22,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3076));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 23,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3078));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 24,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3081));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 25,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3083));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 26,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3086));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 27,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3088));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 28,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3091));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 29,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3094));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 30,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3096));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 31,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3099));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 32,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3101));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 33,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3104));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 34,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3107));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 35,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3170));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 36,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3173));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 37,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3176));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 38,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3178));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 39,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3181));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 40,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3184));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 41,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3187));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 42,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3189));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 43,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3191));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 44,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3194));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 45,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3198));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 46,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3201));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 47,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3204));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 48,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3206));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 49,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3209));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 50,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3211));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 51,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3214));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 52,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3216));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 53,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3219));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 54,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3221));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 55,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3224));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 56,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3227));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 57,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3229));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 58,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3232));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 59,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3234));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 60,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3237));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 61,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3240));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 62,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3242));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 63,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3245));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 64,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3247));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 65,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3250));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 66,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3252));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 67,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3255));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 68,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3258));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 69,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3260));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 70,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3263));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 71,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3265));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 72,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3268));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 73,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3270));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 74,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3273));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 75,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3276));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 76,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3278));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 77,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3281));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 78,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3283));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 79,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3286));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 80,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3289));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 81,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3291));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 82,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3294));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 83,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3296));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 84,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3299));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 85,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3301));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 86,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3304));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 87,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3307));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 88,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3309));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 89,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3312));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 90,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3314));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 91,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3317));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 92,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3320));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 93,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3322));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 94,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3325));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 95,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3327));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 96,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3330));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 97,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3333));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 98,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3335));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 99,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3421));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 100,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3424));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 101,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3426));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 102,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3429));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 103,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3432));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 104,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3434));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 105,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3437));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 106,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3440));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 107,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3442));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 108,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(3445));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(2735));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(2740));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(2744));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(2747));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 1, 6, 14, 16, 2, 18, DateTimeKind.Utc).AddTicks(2750));

            migrationBuilder.CreateIndex(
                name: "IX_group_members_group_id",
                table: "GroupMembers",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_group_members_group_user_unique",
                table: "GroupMembers",
                columns: new[] { "GroupId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_group_members_user_id",
                table: "GroupMembers",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_orders_polar_order_id",
                table: "orders",
                column: "polar_order_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_orders_status",
                table: "orders",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_orders_user_email",
                table: "orders",
                column: "user_email");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "contact_form_submissions");

            migrationBuilder.DropTable(
                name: "downloads");

            migrationBuilder.DropTable(
                name: "EmailLogs");

            migrationBuilder.DropTable(
                name: "EmailQuotas");

            migrationBuilder.DropTable(
                name: "GroupMembers");

            migrationBuilder.DropTable(
                name: "orders");

            migrationBuilder.DropTable(
                name: "pdf_export_settings");

            migrationBuilder.DropColumn(
                name: "license_expiry_date",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "max_departments",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "max_groups",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "max_licenses",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "max_subusers",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "quota_synced_at",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "used_licenses",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "used_subusers",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "activity_details",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "activity_type",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "resource_id",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "resource_type",
                table: "Sessions");

            migrationBuilder.DropColumn(
                name: "admin_user_id",
                table: "Groups");

            migrationBuilder.AlterColumn<bool>(
                name: "is_domain_admin",
                table: "Users",
                type: "tinyint(1)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(10)",
                oldNullable: true)
                .OldAnnotation("MySql:CharSet", "utf8mb4");

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
        }
    }
}
