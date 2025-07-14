using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class AddCompanySizeToSpecialInvitation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "company_size",
                table: "special_invitations",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "company_size",
                table: "special_invitations");
        }
    }
}
