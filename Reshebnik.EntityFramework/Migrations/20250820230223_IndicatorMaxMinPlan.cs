using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class IndicatorMaxMinPlan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "max_value",
                table: "indicators",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "min_value",
                table: "indicators",
                type: "numeric",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "plan_value",
                table: "indicators",
                type: "numeric",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "max_value",
                table: "indicators");

            migrationBuilder.DropColumn(
                name: "min_value",
                table: "indicators");

            migrationBuilder.DropColumn(
                name: "plan_value",
                table: "indicators");
        }
    }
}
