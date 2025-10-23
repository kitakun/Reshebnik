using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Tabligo.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class JobOperation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "job_operations",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    hash = table.Column<int>(type: "integer", nullable: false),
                    name = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    input_data = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    data = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_job_operations", x => x.id);
                    table.ForeignKey(
                        name: "FK_job_operations_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_job_operations_company_id",
                table: "job_operations",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_job_operations_created_at",
                table: "job_operations",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_job_operations_hash",
                table: "job_operations",
                column: "hash");

            migrationBuilder.CreateIndex(
                name: "IX_job_operations_status",
                table: "job_operations",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_job_operations_type",
                table: "job_operations",
                column: "type");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "job_operations");
        }
    }
}
