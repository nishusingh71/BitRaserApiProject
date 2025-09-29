using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitRaserApiProject.Migrations
{
    /// <inheritdoc />
    public partial class EnhancedPermissionsAndRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(2290));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(2293));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(2295));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(2297));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(2298));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(2300));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(2302));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(1519));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(1524));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(1527));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(1529));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 35, 25, 860, DateTimeKind.Utc).AddTicks(1531));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(1184));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(1185));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(1187));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(1188));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(1190));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(1191));

            migrationBuilder.UpdateData(
                table: "Permissions",
                keyColumn: "PermissionId",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(1193));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(939));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(942));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(944));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(945));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "RoleId",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2025, 9, 26, 8, 29, 1, 424, DateTimeKind.Utc).AddTicks(947));
        }
    }
}
