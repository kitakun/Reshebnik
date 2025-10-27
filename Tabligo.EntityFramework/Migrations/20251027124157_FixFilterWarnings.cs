using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Tabligo.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class FixFilterWarnings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "EmployeeEntityId",
                table: "user_notifications",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_EmployeeEntityId",
                table: "user_notifications",
                column: "EmployeeEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_user_notifications_employees_EmployeeEntityId",
                table: "user_notifications",
                column: "EmployeeEntityId",
                principalTable: "employees",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_user_notifications_employees_EmployeeEntityId",
                table: "user_notifications");

            migrationBuilder.DropIndex(
                name: "IX_user_notifications_EmployeeEntityId",
                table: "user_notifications");

            migrationBuilder.DropColumn(
                name: "EmployeeEntityId",
                table: "user_notifications");
        }
    }
}
