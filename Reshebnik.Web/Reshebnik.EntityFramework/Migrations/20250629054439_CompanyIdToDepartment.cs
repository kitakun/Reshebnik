using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class CompanyIdToDepartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_departments_companies_CompanyEntityId",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "IX_departments_CompanyEntityId",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "CompanyEntityId",
                table: "departments");

            migrationBuilder.AddColumn<int>(
                name: "CompanyId",
                table: "departments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_departments_CompanyId",
                table: "departments",
                column: "CompanyId");

            migrationBuilder.AddForeignKey(
                name: "FK_departments_companies_CompanyId",
                table: "departments",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_departments_companies_CompanyId",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "IX_departments_CompanyId",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "CompanyId",
                table: "departments");

            migrationBuilder.AddColumn<int>(
                name: "CompanyEntityId",
                table: "departments",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_departments_CompanyEntityId",
                table: "departments",
                column: "CompanyEntityId");

            migrationBuilder.AddForeignKey(
                name: "FK_departments_companies_CompanyEntityId",
                table: "departments",
                column: "CompanyEntityId",
                principalTable: "companies",
                principalColumn: "Id");
        }
    }
}
