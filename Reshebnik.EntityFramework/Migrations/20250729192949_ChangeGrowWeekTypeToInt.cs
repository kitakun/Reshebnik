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
            migrationBuilder.AlterColumn<int>(
                name: "week_start_date",
                table: "metrics",
                type: "integer",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "week_start_date",
                table: "metric_templates",
                type: "integer",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "week_start_date",
                table: "metrics",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "week_start_date",
                table: "metric_templates",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);
        }
    }
}
