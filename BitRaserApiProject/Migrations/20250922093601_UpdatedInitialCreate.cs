using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitRaserApiProject.Migrations
{
    /// <inheritdoc />
    public partial class UpdatedInitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "hash_password",
                table: "Users",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "hash_password",
                table: "Users");
        }
    }
}
