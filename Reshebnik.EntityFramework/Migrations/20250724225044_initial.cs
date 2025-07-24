using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "companies_id_seq");

            migrationBuilder.CreateSequence<int>(
                name: "employee_id_seq");

            migrationBuilder.CreateTable(
                name: "bug_hunts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    title = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    message = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    screenshot = table.Column<string>(type: "text", nullable: true),
                    last_request_url = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    last_request_response = table.Column<string>(type: "text", nullable: true),
                    last_request_status = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_bug_hunts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "companies",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"companies_id_seq\"')"),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    industry = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    employees_count = table.Column<int>(type: "integer", nullable: false),
                    type = table.Column<string>(type: "text", nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    notify_about_lowering_metrics = table.Column<bool>(type: "boolean", nullable: false),
                    notification_type = table.Column<string>(type: "text", nullable: false),
                    language_code = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Period = table.Column<int>(type: "integer", nullable: false),
                    DefaultMetrics = table.Column<string>(type: "text", nullable: false),
                    AutoUpdateByApi = table.Column<bool>(type: "boolean", nullable: false),
                    AllowForEmployeesEditMetrics = table.Column<bool>(type: "boolean", nullable: false),
                    EnableNotificationsInApp = table.Column<bool>(type: "boolean", nullable: false),
                    ShowNewMetrics = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_companies", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "special_invitations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fio = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    company_name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    company_description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    company_size = table.Column<int>(type: "integer", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    granted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_special_invitations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "departments",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    is_fundamental = table.Column<bool>(type: "boolean", nullable: false),
                    is_deleted = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_departments", x => x.id);
                    table.ForeignKey(
                        name: "FK_departments_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "employees",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false, defaultValueSql: "nextval('\"employee_id_seq\"')"),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    fio = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    job_title = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    phone = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    comment = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false),
                    email_invitation_code = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Password = table.Column<string>(type: "text", nullable: false),
                    Salt = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    default_role = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    last_login_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_employees_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "log_exceptions",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    message = table.Column<string>(type: "text", nullable: false),
                    stacktrace = table.Column<string>(type: "text", nullable: false),
                    user_email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    company_id = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_log_exceptions", x => x.id);
                    table.ForeignKey(
                        name: "FK_log_exceptions_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "metric_templates",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    clickhouse_key = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    period_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    plan_value = table.Column<decimal>(type: "numeric", nullable: true),
                    min_value = table.Column<decimal>(type: "numeric", nullable: true),
                    max_value = table.Column<decimal>(type: "numeric", nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_templates", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_templates_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metrics",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    company_id = table.Column<int>(type: "integer", nullable: false),
                    clickhouse_key = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    unit = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    period_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    plan_value = table.Column<decimal>(type: "numeric", nullable: true),
                    min_value = table.Column<decimal>(type: "numeric", nullable: true),
                    max_value = table.Column<decimal>(type: "numeric", nullable: true),
                    visible = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metrics", x => x.id);
                    table.ForeignKey(
                        name: "FK_metrics_companies_company_id",
                        column: x => x.company_id,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "system_notifications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Caption = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "character varying(200000)", maxLength: 200000, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    CreaetedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompanyId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_system_notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_system_notifications_companies_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "department_scheme",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    fundamental_id = table.Column<int>(type: "integer", nullable: false),
                    ancestor_id = table.Column<int>(type: "integer", nullable: false),
                    department_id = table.Column<int>(type: "integer", nullable: false),
                    depth = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_department_scheme", x => x.id);
                    table.ForeignKey(
                        name: "FK_department_scheme_departments_ancestor_id",
                        column: x => x.ancestor_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_department_scheme_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_department_scheme_departments_fundamental_id",
                        column: x => x.fundamental_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

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
                name: "employee_department_links",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    type = table.Column<string>(type: "text", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: false),
                    department_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_employee_department_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_employee_department_links_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_employee_department_links_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "indicators",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    category = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    unit_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    fillment_period = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    value_type = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    rejection_treshold = table.Column<decimal>(type: "numeric", nullable: false),
                    show_to_employees = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_main_screen = table.Column<bool>(type: "boolean", nullable: false),
                    show_on_key_indicators = table.Column<bool>(type: "boolean", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: true),
                    department_id = table.Column<int>(type: "integer", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    created_by = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_indicators", x => x.id);
                    table.ForeignKey(
                        name: "FK_indicators_companies_created_by",
                        column: x => x.created_by,
                        principalTable: "companies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_indicators_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_indicators_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "metric_department_links",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    metric_id = table.Column<int>(type: "integer", nullable: false),
                    department_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_department_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_department_links_departments_department_id",
                        column: x => x.department_id,
                        principalTable: "departments",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_metric_department_links_metrics_metric_id",
                        column: x => x.metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metric_employee_links",
                columns: table => new
                {
                    id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    metric_id = table.Column<int>(type: "integer", nullable: false),
                    employee_id = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_metric_employee_links", x => x.id);
                    table.ForeignKey(
                        name: "FK_metric_employee_links_employees_employee_id",
                        column: x => x.employee_id,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_metric_employee_links_metrics_metric_id",
                        column: x => x.metric_id,
                        principalTable: "metrics",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_notifications",
                columns: table => new
                {
                    EmployeeId = table.Column<int>(type: "integer", nullable: false),
                    NotificationId = table.Column<int>(type: "integer", nullable: false),
                    IsRead = table.Column<bool>(type: "boolean", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_notifications", x => new { x.EmployeeId, x.NotificationId });
                    table.ForeignKey(
                        name: "FK_user_notifications_employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_notifications_system_notifications_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "system_notifications",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
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
                name: "IX_companies_email",
                table: "companies",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_department_scheme_ancestor_id",
                table: "department_scheme",
                column: "ancestor_id");

            migrationBuilder.CreateIndex(
                name: "IX_department_scheme_department_id",
                table: "department_scheme",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_department_scheme_fundamental_id_department_id_ancestor_id",
                table: "department_scheme",
                columns: new[] { "fundamental_id", "department_id", "ancestor_id" });

            migrationBuilder.CreateIndex(
                name: "IX_departments_CompanyId",
                table: "departments",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_email_messages_SentByCompanyId",
                table: "email_messages",
                column: "SentByCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_email_messages_SentByUserId",
                table: "email_messages",
                column: "SentByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_employee_department_links_department_id_employee_id",
                table: "employee_department_links",
                columns: new[] { "department_id", "employee_id" });

            migrationBuilder.CreateIndex(
                name: "IX_employee_department_links_employee_id",
                table: "employee_department_links",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_company_id",
                table: "employees",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_employees_email",
                table: "employees",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_indicators_created_by",
                table: "indicators",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_indicators_department_id",
                table: "indicators",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_indicators_employee_id",
                table: "indicators",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_log_exceptions_company_id",
                table: "log_exceptions",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_department_links_department_id",
                table: "metric_department_links",
                column: "department_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_department_links_metric_id_department_id",
                table: "metric_department_links",
                columns: new[] { "metric_id", "department_id" });

            migrationBuilder.CreateIndex(
                name: "IX_metric_employee_links_employee_id",
                table: "metric_employee_links",
                column: "employee_id");

            migrationBuilder.CreateIndex(
                name: "IX_metric_employee_links_metric_id_employee_id",
                table: "metric_employee_links",
                columns: new[] { "metric_id", "employee_id" });

            migrationBuilder.CreateIndex(
                name: "IX_metric_templates_company_id",
                table: "metric_templates",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_metrics_company_id",
                table: "metrics",
                column: "company_id");

            migrationBuilder.CreateIndex(
                name: "IX_special_invitations_email",
                table: "special_invitations",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_system_notifications_CompanyId",
                table: "system_notifications",
                column: "CompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_user_notifications_NotificationId",
                table: "user_notifications",
                column: "NotificationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "bug_hunts");

            migrationBuilder.DropTable(
                name: "department_scheme");

            migrationBuilder.DropTable(
                name: "EmailAttachments");

            migrationBuilder.DropTable(
                name: "employee_department_links");

            migrationBuilder.DropTable(
                name: "indicators");

            migrationBuilder.DropTable(
                name: "log_exceptions");

            migrationBuilder.DropTable(
                name: "metric_department_links");

            migrationBuilder.DropTable(
                name: "metric_employee_links");

            migrationBuilder.DropTable(
                name: "metric_templates");

            migrationBuilder.DropTable(
                name: "special_invitations");

            migrationBuilder.DropTable(
                name: "user_notifications");

            migrationBuilder.DropTable(
                name: "email_messages");

            migrationBuilder.DropTable(
                name: "departments");

            migrationBuilder.DropTable(
                name: "metrics");

            migrationBuilder.DropTable(
                name: "system_notifications");

            migrationBuilder.DropTable(
                name: "employees");

            migrationBuilder.DropTable(
                name: "companies");

            migrationBuilder.DropSequence(
                name: "companies_id_seq");

            migrationBuilder.DropSequence(
                name: "employee_id_seq");
        }
    }
}
