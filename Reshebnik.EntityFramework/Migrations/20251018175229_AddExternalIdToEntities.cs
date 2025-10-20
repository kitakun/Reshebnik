using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddExternalIdToEntities : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "metrics",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "indicators",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "employees",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "departments",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "external_id",
                table: "companies",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_metrics_external_id_company_id",
                table: "metrics",
                columns: new[] { "external_id", "company_id" },
                unique: true,
                filter: "external_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_indicators_external_id_created_by",
                table: "indicators",
                columns: new[] { "external_id", "created_by" },
                unique: true,
                filter: "external_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_employees_external_id_company_id",
                table: "employees",
                columns: new[] { "external_id", "company_id" },
                unique: true,
                filter: "external_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_departments_external_id_CompanyId",
                table: "departments",
                columns: new[] { "external_id", "CompanyId" },
                unique: true,
                filter: "external_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_companies_external_id_Id",
                table: "companies",
                columns: new[] { "external_id", "Id" },
                unique: true,
                filter: "external_id IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_metrics_external_id_company_id",
                table: "metrics");

            migrationBuilder.DropIndex(
                name: "IX_indicators_external_id_created_by",
                table: "indicators");

            migrationBuilder.DropIndex(
                name: "IX_employees_external_id_company_id",
                table: "employees");

            migrationBuilder.DropIndex(
                name: "IX_departments_external_id_CompanyId",
                table: "departments");

            migrationBuilder.DropIndex(
                name: "IX_companies_external_id_Id",
                table: "companies");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "indicators");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "employees");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "departments");

            migrationBuilder.DropColumn(
                name: "external_id",
                table: "companies");
        }
    }
}
