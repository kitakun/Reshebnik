using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class EmailQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "email_messages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    to = table.Column<string>(type: "text", nullable: false),
                    subject = table.Column<string>(type: "text", nullable: false),
                    body = table.Column<string>(type: "text", nullable: false),
                    is_html = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    error = table.Column<string>(type: "text", nullable: true),
                    from = table.Column<string>(type: "text", nullable: true, defaultValue: "no-reply@mydoc.com"),
                    Cc = table.Column<string>(type: "text", nullable: false),
                    Bcc = table.Column<string>(type: "text", nullable: false),
                    enqueued_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_sent = table.Column<bool>(type: "boolean", nullable: false),
                    sent_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    SentByUserId = table.Column<int>(type: "integer", nullable: false),
                    SentByCompanyId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_email_messages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_email_messages_companies_SentByCompanyId",
                        column: x => x.SentByCompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_email_messages_employees_SentByUserId",
                        column: x => x.SentByUserId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmailAttachments",
                columns: table => new
                {
                    FileName = table.Column<string>(type: "text", nullable: false),
                    EmailMessageId = table.Column<int>(type: "integer", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Content = table.Column<byte[]>(type: "bytea", nullable: false),
                    ContentType = table.Column<string>(type: "text", nullable: false, defaultValue: "application/octet-stream")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmailAttachments", x => new { x.EmailMessageId, x.FileName });
                    table.ForeignKey(
                        name: "FK_EmailAttachments_email_messages_EmailMessageId",
                        column: x => x.EmailMessageId,
                        principalTable: "email_messages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_email_messages_SentByCompanyId",
                table: "email_messages",
                column: "SentByCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_email_messages_SentByUserId",
                table: "email_messages",
                column: "SentByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EmailAttachments");

            migrationBuilder.DropTable(
                name: "email_messages");
        }
    }
}
