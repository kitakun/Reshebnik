using Microsoft.EntityFrameworkCore;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Auth;
using Tabligo.Handlers.Company;
using Tabligo.Handlers.Department;
using Tabligo.Handlers.Employee;
using Tabligo.Handlers.Structure;
using Tabligo.Handlers.Metric;
using Tabligo.Handlers.Indicator;
using Tabligo.Handlers.KeyIndicator;
using Tabligo.Handlers.IndicatorCategory;
using Tabligo.Handlers.Dashboard;
using Tabligo.Clickhouse;
using Tabligo.Clickhouse.Handlers;
using Tabligo.Handlers;
using Tabligo.Handlers.Cache;
using Tabligo.Handlers.Email;
using Tabligo.Handlers.SpecialInvitation;
using Tabligo.Handlers.BugHunt;
using Tabligo.GPT.Services;
using Tabligo.Handlers.Integration;
using Tabligo.Handlers.Integration.GetCourse;
using Tabligo.Handlers.Integration.PowerBI;
using Tabligo.Handlers.Integration.Ozon;
using Tabligo.Handlers.JobOperation;
using Tabligo.SberGPT.Extensions;
using Tabligo.Neural.Interfaces;
using Tabligo.Neural.Handlers;
using Tabligo.Domain.Services;
using Tabligo.Domain.Models.JobOperation;
using Tabligo.SignalR.Services;

namespace Tabligo.Web.ProgramExtensions;

public static class ServiceRegistrations
{
    public static IServiceCollection AddTabligoServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Database
        services.AddDbContext<TabligoContext>(options =>
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
        services.AddScoped<PutIndicatorValuesHandler>();
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

        // SberGPT Services
        services.AddSberGpt(configuration);

        // Neural Services
        services.AddScoped<ITabligoNeuralAgent, SberGptNeuralAgentHandler>();
        services.AddScoped<ExternalIdLinkHandler>();
        
        // SignalR Services
        services.AddSignalR();
        services.AddScoped<INotifier, SignalRNotifier>();
        services.AddScoped<IntegrationImportHandler>();
        services.AddScoped<IntegrationListHandler>();

        // GetCourse Integration Services
        services.AddScoped<Tabligo.Integrations.Integrations.GetCourse.GetCourseApiClient>();
        services.AddScoped<Tabligo.Integrations.Integrations.GetCourse.GetCourseDataTransformer>();
        services.AddScoped<Tabligo.Integrations.Integrations.GetCourse.GetCourseProvider>();
        services.AddScoped<IntegrationPreviewHandler>();
        services.AddScoped<IntegrationApprovalHandler>();
        services.AddScoped<IntegrationSettingsHandler>();
        
        // Integration Settings Handlers
        services.AddScoped<GetCourseSettingsHandler>();
        services.AddScoped<PowerBISettingsHandler>();
        services.AddScoped<OzonSettingsHandler>();
        services.AddHttpClient<Tabligo.Integrations.Integrations.GetCourse.GetCourseApiClient>();

        // PowerBI Integration Services
        services.AddScoped<Tabligo.Integrations.Integrations.PowerBI.PowerBIApiClient>();
        services.AddScoped<Tabligo.Integrations.Integrations.PowerBI.PowerBIDataTransformer>();
        services.AddScoped<Tabligo.Integrations.Integrations.PowerBI.PowerBIProvider>();
        services.AddHttpClient<Tabligo.Integrations.Integrations.PowerBI.PowerBIApiClient>();

        // Ozon Integration Services
        services.AddScoped<Tabligo.Integrations.Integrations.Ozon.OzonApiClient>();
        services.AddScoped<Tabligo.Integrations.Integrations.Ozon.OzonDataTransformer>();
        services.AddScoped<Tabligo.Integrations.Integrations.Ozon.OzonProvider>();
        services.AddHttpClient<Tabligo.Integrations.Integrations.Ozon.OzonApiClient>();

        // Job Operation Services
        services.AddScoped<IJobOperationQueue, JobOperationQueue>();
        services.AddScoped<JobOperationGetHandler>();
        services.AddScoped<JobOperationSearchHandler>();
        services.AddScoped<JobOperationsTypeaheadHandler>();
        
        // Register all job operation processors
        services.AddScoped<IJobOperationProcessor, NeuralJobProcessor>();
        
        // Register all integration providers as job operation processors
        services.AddScoped<IJobOperationProcessor>(provider => provider.GetRequiredService<Tabligo.Integrations.Integrations.GetCourse.GetCourseProvider>());
        services.AddScoped<IJobOperationProcessor>(provider => provider.GetRequiredService<Tabligo.Integrations.Integrations.PowerBI.PowerBIProvider>());
        services.AddScoped<IJobOperationProcessor>(provider => provider.GetRequiredService<Tabligo.Integrations.Integrations.Ozon.OzonProvider>());
        
        services.AddHostedService<JobOperationProcessorService>();

        return services;
    }
}
