using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class ChangeGrowWeekTypeToInt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // For table "metrics"
            migrationBuilder.DropColumn(
                name: "week_start_date",
                table: "metrics");

            migrationBuilder.AddColumn<int>(
                name: "week_start_date",
                table: "metrics",
                type: "integer",
                nullable: true,
                defaultValue: 0);

            // For table "metric_templates"
            migrationBuilder.DropColumn(
                name: "week_start_date",
                table: "metric_templates");

            migrationBuilder.AddColumn<int>(
                name: "week_start_date",
                table: "metric_templates",
                type: "integer",
                nullable: true,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotImplementedException();
        }
    }
}
