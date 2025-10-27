using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tabligo.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class ExternalIds : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_departments_companies_CompanyId",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "FK_indicators_archived_metrics_ArchiveMetricId",
                table: "indicators");

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

            migrationBuilder.RenameColumn(
                name: "ArchiveMetricId",
                table: "indicators",
                newName: "archive_metric_id");

            migrationBuilder.RenameIndex(
                name: "IX_indicators_ArchiveMetricId",
                table: "indicators",
                newName: "IX_indicators_archive_metric_id");

            migrationBuilder.RenameColumn(
                name: "Salt",
                table: "employees",
                newName: "salt");

            migrationBuilder.RenameColumn(
                name: "Password",
                table: "employees",
                newName: "password");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "employees",
                newName: "id");

            migrationBuilder.RenameColumn(
                name: "CompanyId",
                table: "departments",
                newName: "company_id");

            migrationBuilder.RenameIndex(
                name: "IX_departments_CompanyId",
                table: "departments",
                newName: "IX_departments_company_id");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "companies",
                newName: "id");

            migrationBuilder.CreateTable(
                name: "external_id_links",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    external_id = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    integration_type = table.Column<int>(type: "integer", nullable: false),
                    entity_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    entity_id = table.Column<int>(type: "integer", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: true),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    metric_id = table.Column<int>(type: "integer", nullable: true),
                    indicator_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_external_id_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_external_id_links_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_external_id_links_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_external_id_links_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_external_id_links_indicators_indicator_id",
                        column: x => x.indicator_id,
                        principalTable: "indicators",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_external_id_links_metrics_metric_id",
                        column: x => x.metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_external_id_links_department_id",
                table: "external_id_links",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_external_id_links_employee_id",
                table: "external_id_links",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_id_links_entity",
                table: "external_id_links",
                columns: new[] { "company_id", "entity_type", "entity_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_external_id_links_indicator_id",
                table: "external_id_links",
                column: "indicator_id");

            migrationBuilder.CreateIndex(
                name: "ix_external_id_links_lookup",
                table: "external_id_links",
                columns: new[] { "company_id", "external_id", "integration_type", "entity_type" });

            migrationBuilder.CreateIndex(
                name: "IX_external_id_links_metric_id",
                table: "external_id_links",
                column: "metric_id");

            migrationBuilder.AddForeignKey(
                name: "FK_departments_companies_company_id",
                table: "departments",
                column: "company_id",
                principalTable: "companies",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_indicators_archived_metrics_archive_metric_id",
                table: "indicators",
                column: "archive_metric_id",
                principalTable: "archived_metrics",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_departments_companies_company_id",
                table: "departments");

            migrationBuilder.DropForeignKey(
                name: "FK_indicators_archived_metrics_archive_metric_id",
                table: "indicators");

            migrationBuilder.DropTable(
                name: "external_id_links");

            migrationBuilder.RenameColumn(
                name: "archive_metric_id",
                table: "indicators",
                newName: "ArchiveMetricId");

            migrationBuilder.RenameIndex(
                name: "IX_indicators_archive_metric_id",
                table: "indicators",
                newName: "IX_indicators_ArchiveMetricId");

            migrationBuilder.RenameColumn(
                name: "salt",
                table: "employees",
                newName: "Salt");

            migrationBuilder.RenameColumn(
                name: "password",
                table: "employees",
                newName: "Password");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "employees",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "company_id",
                table: "departments",
                newName: "CompanyId");

            migrationBuilder.RenameIndex(
                name: "IX_departments_company_id",
                table: "departments",
                newName: "IX_departments_CompanyId");

            migrationBuilder.RenameColumn(
                name: "id",
                table: "companies",
                newName: "Id");

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

            migrationBuilder.AddForeignKey(
                name: "FK_departments_companies_CompanyId",
                table: "departments",
                column: "CompanyId",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_indicators_archived_metrics_ArchiveMetricId",
                table: "indicators",
                column: "ArchiveMetricId",
                principalTable: "archived_metrics",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
