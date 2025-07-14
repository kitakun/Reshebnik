using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class UniqueEmailIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_employees_company_id_email",
                table: "employees");

            migrationBuilder.CreateIndex(
                name: "IX_employees_email",
                table: "employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_companies_email",
                table: "companies",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_special_invitations_email",
                table: "special_invitations",
                column: "email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_special_invitations_email",
                table: "special_invitations");

            migrationBuilder.DropIndex(
                name: "IX_companies_email",
                table: "companies");

            migrationBuilder.DropIndex(
                name: "IX_employees_email",
                table: "employees");

            migrationBuilder.CreateIndex(
                name: "IX_employees_company_id_email",
                table: "employees",
                columns: new[] { "company_id", "email" });
        }
    }
}
