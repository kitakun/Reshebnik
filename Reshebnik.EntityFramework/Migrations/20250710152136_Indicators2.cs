using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class Indicators2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_indicators_created_by",
                table: "indicators",
                column: "created_by");

            migrationBuilder.AddForeignKey(
                name: "FK_indicators_companies_created_by",
                table: "indicators",
                column: "created_by",
                principalTable: "companies",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_indicators_companies_created_by",
                table: "indicators");

            migrationBuilder.DropIndex(
                name: "IX_indicators_created_by",
                table: "indicators");
        }
    }
}
