using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Reshebnik.EntityFramework.Migrations
{
    /// <inheritdoc />
    public partial class SuUser2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
migrationBuilder.Sql("""
                                 INSERT INTO public."companies" (
                                     "name",
                                     "industry",
                                     "employees_count",
                                     "type",
                                     "email",
                                     "phone",
                                     "notify_about_lowering_metrics",
                                     "notification_type",
                                     "language_code",
                                     "Period",
                                     "DefaultMetrics",
                                     "AutoUpdateByApi",
                                     "AllowForEmployeesEditMetrics",
                                     "EnableNotificationsInApp",
                                     "ShowNewMetrics"
                                 )
                                 SELECT
                                     'SuperCompany',
                                     'IT',
                                     2,
                                     'Unset',
                                     'info@supercompany.com',
                                     '+1234567890',
                                     true,
                                     'Email',
                                     'ru',
                                     1,
                                     '',
                                     false,
                                     false,
                                     false,
                                     false
                                 WHERE NOT EXISTS (
                                     SELECT 1 FROM public."companies" WHERE "name" = 'SuperCompany'
                                 );
                                 """);
            
            migrationBuilder.Sql("""
                             INSERT INTO public."employees" (
                                 "Id", 
                                 "company_id", 
                                 "fio", 
                                 "job_title", 
                                 "email", 
                                 "phone", 
                                 "comment", 
                                 "is_active", 
                                 "email_invitation_code", 
                                 "Password", 
                                 "Salt", 
                                 "role", 
                                 "created_at", 
                                 "last_login_at"
                             )
                             SELECT 
                                 nextval('employee_id_seq'),
                                 1,
                                 'Илья',
                                 'Team Lead',
                                 'ya@kitakun.ru',
                                 '+79999999999',
                                 'Комментарий',
                                 true,
                                 null,
                                 'K9cJw/bBtT3pHFO/yRXdb8tQk8LSLD271I7ODuquJ4Y=',
                                 'PMRcoGKmOGsQCTzojJC+iw==',
                                 'SuperAdmin', -- строковое значение RootRolesEnum
                                 CURRENT_TIMESTAMP AT TIME ZONE 'UTC',
                                 CURRENT_TIMESTAMP AT TIME ZONE 'UTC'
                             WHERE NOT EXISTS (
                                 SELECT 1 FROM public."employees" WHERE "email" = 'ya@kitakun.ru'
                             );
                             
                             """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
