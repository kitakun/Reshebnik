using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

using Tabligo.Domain.Entities;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;
using Tabligo.Handlers.JobOperation;
using Tabligo.Integrations;

namespace Tabligo.Handlers.Integration;

public class IntegrationPreviewHandler(
    TabligoContext db,
    CompanyContextHandler companyContext,
    IServiceProvider serviceProvider,
    IJobOperationQueue jobQueue)
{
    private static readonly Dictionary<IntegrationTypeEnum, Type> IntegrationProviderTypes = new()
    {
        { IntegrationTypeEnum.GetCourse, typeof(Tabligo.Integrations.Integrations.GetCourse.GetCourseProvider) },
        { IntegrationTypeEnum.PowerBI, typeof(Tabligo.Integrations.Integrations.PowerBI.PowerBIProvider) },
        { IntegrationTypeEnum.Ozon, typeof(Tabligo.Integrations.Integrations.Ozon.OzonProvider) },
        { IntegrationTypeEnum.GoogleSheets, typeof(Tabligo.Integrations.Integrations.GoogleSheets.GoogleSheetsProvider) }
    };
    public async Task<IntegrationPreviewResponse> CreatePreviewAsync(
        IntegrationTypeEnum integrationType,
        JsonElement? configuration = null,
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        
        // Get integration configuration from database
        var integration = await db.Integrations
            .FirstOrDefaultAsync(i => i.CompanyId == companyId && i.Type == integrationType, ct);

        if (integration == null)
        {
            // Create integration if it doesn't exist and make it active
            var defaultConfiguration = GetDefaultConfiguration(integrationType);
            integration = new IntegrationEntity
            {
                CompanyId = companyId,
                Type = integrationType,
                IsActivated = true,
                Configuration = JsonSerializer.Serialize(defaultConfiguration)
            };
            
            db.Integrations.Add(integration);
            await db.SaveChangesAsync(ct);
        }
        
        if (!integration.IsActivated)
        {
            // Activate the integration if it exists but is not activated
            integration.IsActivated = true;
            await db.SaveChangesAsync(ct);
        }

        // Get integration provider
        var provider = GetIntegrationProvider(integrationType);
        if (provider == null)
        {
            throw new InvalidOperationException($"Provider for integration {integrationType} not found");
        }

        var configJson = integration.Configuration ?? "{}";

        // Parse request body once
        Dictionary<string, object>? requestDict = null;
        if (configuration.HasValue)
        {
            var requestJson = configuration.Value.GetRawText();
            requestDict = JsonSerializer.Deserialize<Dictionary<string, object>>(requestJson);
        }

        // Check if this is just a connection test
        if (requestDict != null && requestDict.TryGetValue("testConnection", out var testConnectionValue) &&
            bool.TryParse(testConnectionValue?.ToString(), out bool testConnection) && testConnection)
        {
            // Use requestJson for testing the connection with the configuration from the request
            var testRequestJson = configuration?.GetRawText() ?? configJson;
            
            // Get detailed result for all integrations
            var detailedResult = await provider.TestConnectionDetailedAsync(testRequestJson, ct);
            
            // If test passed, update the configuration with isValid = true
            if (detailedResult.IsSuccess)
            {
                var updatedConfigDict = JsonSerializer.Deserialize<Dictionary<string, object>>(testRequestJson);
                if (updatedConfigDict != null)
                {
                    updatedConfigDict["isValid"] = true;
                    integration.Configuration = JsonSerializer.Serialize(updatedConfigDict);
                    await db.SaveChangesAsync(ct);
                }
            }
            
            return new IntegrationPreviewResponse
            {
                IsSuccess = detailedResult.IsSuccess,
                Message = detailedResult.IsSuccess ? "Тест соединения успешен" : $"Тест соединения не удался: {detailedResult.Reason}",
                Items = null
            };
        }

        // Extract filters from request body (these are custom filters, not stored config)
        Dictionary<string, object>? filters = null;
        if (requestDict != null && requestDict.TryGetValue("filters", out var filtersValue))
        {
            if (filtersValue is JsonElement filtersElement)
            {
                filters = JsonSerializer.Deserialize<Dictionary<string, object>>(filtersElement.GetRawText());
            }
        }

        // Create input data for the background job
        var inputData = new
        {
            CompanyId = companyId,
            Configuration = configJson,
            Filters = filters
        };

        // Enqueue background job to fetch data
        var jobId = await jobQueue.EnqueueAsync(
            companyId,
            provider.JobType,
            $"Предпросмотр интеграции {integrationType}",
            inputData,
            ct);

        return new IntegrationPreviewResponse
        {
            IsSuccess = true,
            Message = "Задача на предпросмотр создана успешно",
            JobId = jobId
        };
    }


    private IIntegrationProvider? GetIntegrationProvider(IntegrationTypeEnum integrationType)
    {
        if (IntegrationProviderTypes.TryGetValue(integrationType, out var providerType))
        {
            return serviceProvider.GetService(providerType) as IIntegrationProvider;
        }
        
        return null;
    }

    private object? GetDefaultConfiguration(IntegrationTypeEnum integrationType)
    {
        return integrationType switch
        {
            IntegrationTypeEnum.GetCourse => serviceProvider.GetService<Tabligo.Integrations.Integrations.GetCourse.GetCourseSettingsHandler>()?.GetDefaultConfiguration(),
            IntegrationTypeEnum.PowerBI => serviceProvider.GetService<Tabligo.Integrations.Integrations.PowerBI.PowerBISettingsHandler>()?.GetDefaultConfiguration(),
            IntegrationTypeEnum.Ozon => serviceProvider.GetService<Tabligo.Integrations.Integrations.Ozon.OzonSettingsHandler>()?.GetDefaultConfiguration(),
            IntegrationTypeEnum.GoogleSheets => serviceProvider.GetService<Tabligo.Integrations.Integrations.GoogleSheets.GoogleSheetsSettingsHandler>()?.GetDefaultConfiguration(),
            _ => null
        };
    }
}
