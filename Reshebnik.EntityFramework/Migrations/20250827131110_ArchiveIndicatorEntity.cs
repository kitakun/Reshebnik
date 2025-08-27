using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class ArchiveIndicatorEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ArchiveMetricId",
                table: "indicators",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_archived",
                table: "indicators",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "indicator_id",
                table: "archived_metrics",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_indicators_ArchiveMetricId",
                table: "indicators",
                column: "ArchiveMetricId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_indicators_archived_metrics_ArchiveMetricId",
                table: "indicators",
                column: "ArchiveMetricId",
                principalTable: "archived_metrics",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_indicators_archived_metrics_ArchiveMetricId",
                table: "indicators");

            migrationBuilder.DropIndex(
                name: "IX_indicators_ArchiveMetricId",
                table: "indicators");

            migrationBuilder.DropColumn(
                name: "ArchiveMetricId",
                table: "indicators");

            migrationBuilder.DropColumn(
                name: "is_archived",
                table: "indicators");

            migrationBuilder.DropColumn(
                name: "indicator_id",
                table: "archived_metrics");
        }
    }
}
