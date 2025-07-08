using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddMoreSettingsToCompanyEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowForEmployeesEditMetrics",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "AutoUpdateByApi",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "DefaultMetrics",
                table: "companies",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "EnableNotificationsInApp",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Period",
                table: "companies",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "ShowNewMetrics",
                table: "companies",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowForEmployeesEditMetrics",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "AutoUpdateByApi",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "DefaultMetrics",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "EnableNotificationsInApp",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "Period",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "ShowNewMetrics",
                table: "companies");
        }
    }
}
