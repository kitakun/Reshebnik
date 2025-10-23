using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabligo.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class FixLinks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "welcome_was_seen",
                table: "employees",
                type: "boolean",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "welcome_was_seen",
                table: "employees");
        }
    }
}
