using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class GworRate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "show_growth_percent",
                table: "metrics",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "week_start_date",
                table: "metrics",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "week_type",
                table: "metrics",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<bool>(
                name: "show_growth_percent",
                table: "metric_templates",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "week_start_date",
                table: "metric_templates",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "week_type",
                table: "metric_templates",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "show_growth_percent",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "week_start_date",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "week_type",
                table: "metrics");

            migrationBuilder.DropColumn(
                name: "show_growth_percent",
                table: "metric_templates");

            migrationBuilder.DropColumn(
                name: "week_start_date",
                table: "metric_templates");

            migrationBuilder.DropColumn(
                name: "week_type",
                table: "metric_templates");
        }
    }
}
