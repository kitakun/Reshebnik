using Microsoft.EntityFrameworkCore;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Auth;
using Reshebnik.Handlers.Company;
using Reshebnik.Handlers.Department;
using Reshebnik.Handlers.Employee;
using Reshebnik.Handlers.Structure;
using Reshebnik.Handlers.Metric;
using Reshebnik.Handlers.Indicator;
using Reshebnik.Handlers.KeyIndicator;
using Reshebnik.Handlers.IndicatorCategory;
using Reshebnik.Handlers.Dashboard;
using Reshebnik.Clickhouse;
using Reshebnik.Clickhouse.Handlers;
using Reshebnik.Handlers;
using Reshebnik.Handlers.Cache;
using Reshebnik.Handlers.Email;
using Reshebnik.Handlers.SpecialInvitation;
using Reshebnik.Handlers.BugHunt;
using Reshebnik.GPT.Services;

namespace Reshebnik.Web.ProgramExtensions;

public static class ServiceRegistrations
{
    public static IServiceCollection AddReshebnikServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<ReshebnikContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        // Cache
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Authorization and HTTP Context
        services.AddAuthorization();
        services.AddHttpContextAccessor();

        // Auth Services
        services.AddSingleton<CreateJwtHandler>();
        services.AddSingleton<SecurityHandler>();
        services.AddScoped<UserContextHandler>();
        services.AddScoped<CompanyContextHandler>();
        services.AddScoped<AuthLoginHandler>();
        services.AddScoped<AuthInviteHandler>();
        services.AddScoped<AuthGetInviteHandler>();
        services.AddScoped<AuthResetPasswordHandler>();

        // Department Services
        services.AddScoped<DepartmentGetHandler>();
        services.AddScoped<DepartmentPreviewHandler>();
        services.AddScoped<DepartmentPutHandler>();
        services.AddScoped<DepartmentGetByIdHandler>();
        services.AddScoped<DepartmentPutOneHandler>();
        services.AddScoped<DepartmentDeleteHandler>();
        services.AddScoped<DepartmentFormGetByIdHandler>();
        services.AddScoped<DepartmentFormPutHandler>();
        services.AddScoped<EmployeesTypeaheadHandler>();
        services.AddScoped<DepartmentTypeaheadHandler>();

        // Employee Services
        services.AddScoped<EmployeeGetByIdHandler>();
        services.AddScoped<EmployeePutHandler>();
        services.AddScoped<EmployeeDeleteHandler>();
        services.AddScoped<EmployeeCommentUpdateHandler>();
        services.AddScoped<EmployeeArchiveHandler>();
        services.AddScoped<ArchivedUserGetHandler>();
        services.AddScoped<ArchivedUserTypeaheadHandler>();
        services.AddScoped<EmployeeUnarchiveHandler>();
        services.AddScoped<DepartmentEmployeesUpsertHandler>();

        // Company Services
        services.AddScoped<CompanyUpdateHandler>();
        services.AddScoped<CompanyGetHandler>();
        services.AddScoped<SuCompanyGetHandler>();
        services.AddScoped<CompanySettingsUpdateHandler>();
        services.AddScoped<SuCompanySettingsUpdateHandler>();

        // Structure Services
        services.AddScoped<StructureGetHandler>();
        services.AddScoped<StructurePutHandler>();

        // Metric Services
        services.AddScoped<MetricGetHandler>();
        services.AddScoped<MetricPutHandler>();
        services.AddScoped<MetricArchiveHandler>();
        services.AddScoped<ArchivedMetricGetHandler>();
        services.AddScoped<ArchivedMetricTypeaheadHandler>();
        services.AddScoped<MetricUnarchiveHandler>();

        // Indicator Services
        services.AddScoped<IndicatorGetHandler>();
        services.AddScoped<IndicatorPutHandler>();
        services.AddScoped<IndicatorTypeaheadHandler>();
        services.AddScoped<KeyIndicatorGetHandler>();
        services.AddScoped<IndicatorCategoryGetHandler>();
        services.AddScoped<IndicatorCategoryCommentUpdateHandler>();

        // Clickhouse Services
        services.Configure<ClickhouseOptions>(configuration.GetSection("Clickhouse"));
        services.AddScoped<FetchUserMetricsHandler>();
        services.AddScoped<FetchCompanyMetricsHandler>();
        services.AddScoped<FetchDepartmentCompletionHandler>();
        services.AddScoped<UserPreviewMetricsHandler>();
        services.AddScoped<UserPreviewMetricsPutHandler>();
        services.AddScoped<CompanyPreviewMetricsHandler>();
        services.AddScoped<CompanyPreviewMetricsPutHandler>();
        services.AddScoped<CopyUserMetricsToRelatedUsersMigration>();
        services.AddScoped<MigrateClickhouseDatabase>();

        // Dashboard Services
        services.AddScoped<DashboardGetHandler>();

        // Bug Hunt Services
        services.AddScoped<BugHuntCreateHandler>();

        // Welcome Services
        services.AddScoped<WelcomeHandler>();

        // Super User Services
        services.AddScoped<SuTypeaheadCompaniesHandler>();
        services.AddScoped<SuAllCompanyIdsHandler>();
        services.AddScoped<SpecialInvitationCreateHandler>();
        services.AddScoped<SuSpecialInvitationTypeaheadHandler>();
        services.AddScoped<SuSpecialInvitationAcceptHandler>();
        services.AddScoped<SuSpecialInvitationRejectHandler>();
        services.AddScoped<SuBugHuntTypeaheadHandler>();

        // Email Services
        services.AddScoped<IEmailQueue, EfEmailQueue>();
        services.AddSingleton<IEmailSender, SmtpEmailSender>();
        services.AddHostedService<EmailSenderService>();

        // GPT Services
        services.AddScoped<GptService>();

        return services;
    }
}
