using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;
using Tabligo.EntityFramework;
using Tabligo.Integrations.Integrations.GetCourse;
using Tabligo.Integrations.Integrations.PowerBI;
using Tabligo.Integrations.Integrations.Ozon;
using Tabligo.Integrations.Integrations.GoogleSheets;

namespace Tabligo.Handlers.Integration;

public class IntegrationSettingsHandler(
    TabligoContext db,
    GetCourseSettingsHandler getCourseHandler,
    PowerBISettingsHandler powerBIHandler,
    OzonSettingsHandler ozonHandler,
    GoogleSheetsSettingsHandler googleSheetsHandler)
{
    public async Task<object> SaveIntegrationSettingsAsync(int companyId, IntegrationTypeEnum integrationType, JsonDocument settings, CancellationToken ct = default)
    {
        var integration = await db.Integrations
            .FirstOrDefaultAsync(i => i.CompanyId == companyId && i.Type == integrationType, ct);

        if (integration == null)
        {
            throw new InvalidOperationException($"Integration of type {integrationType} not found for company {companyId}");
        }

        // Parse the current configuration
        var existingConfig = ParseConfiguration(integration.Configuration, integrationType);

        // Update configuration with new settings
        var updatedConfig = UpdateConfiguration(existingConfig, settings, integrationType);

        // Save updated configuration
        integration.Configuration = JsonSerializer.Serialize(updatedConfig);
        integration.IsActivated = GetIsActiveFromSettings(settings);
        await db.SaveChangesAsync(ct);

        return new { success = true, message = "Настройки успешно сохранены" };
    }

    public async Task<object> GetIntegrationSettingsAsync(int companyId, IntegrationTypeEnum integrationType, CancellationToken ct = default)
    {
        var integration = await db.Integrations
            .FirstOrDefaultAsync(i => i.CompanyId == companyId && i.Type == integrationType, ct);

        if (integration == null)
        {
            return GetDefaultSettings(integrationType);
        }

        // Parse configuration
        var config = ParseConfiguration(integration.Configuration, integrationType);

        // Get last sync date from job operations
        var lastJob = await db.JobOperations
            .Where(j => j.CompanyId == companyId && j.Type == "IntegrationImport" && j.Status == JobOperationStatusEnum.Finished)
            .OrderByDescending(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);

        return CreateSettingsResponse(integration, config, lastJob?.CreatedAt, integrationType);
    }

    private object GetDefaultSettings(IntegrationTypeEnum integrationType)
    {
        return integrationType switch
        {
            IntegrationTypeEnum.GetCourse => getCourseHandler.GetDefaultSettings(),
            IntegrationTypeEnum.PowerBI => powerBIHandler.GetDefaultSettings(),
            IntegrationTypeEnum.Ozon => ozonHandler.GetDefaultSettings(),
            IntegrationTypeEnum.GoogleSheets => googleSheetsHandler.GetDefaultSettings(),
            _ => throw new NotSupportedException($"Integration type {integrationType} is not supported")
        };
    }

    private object? ParseConfiguration(string? configuration, IntegrationTypeEnum integrationType)
    {
        if (string.IsNullOrEmpty(configuration))
            return null;

        try
        {
            return integrationType switch
            {
                IntegrationTypeEnum.GetCourse => JsonSerializer.Deserialize<GetCourseConfiguration>(configuration),
                IntegrationTypeEnum.PowerBI => JsonSerializer.Deserialize<PowerBIConfiguration>(configuration),
                IntegrationTypeEnum.Ozon => JsonSerializer.Deserialize<OzonConfiguration>(configuration),
                IntegrationTypeEnum.GoogleSheets => JsonSerializer.Deserialize<GoogleSheetsConfiguration>(configuration),
                _ => null
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private object CreateSettingsResponse(Domain.Entities.IntegrationEntity integration, object? config, DateTime? lastSyncDate, IntegrationTypeEnum integrationType)
    {
        return integrationType switch
        {
            IntegrationTypeEnum.GetCourse => getCourseHandler.CreateSettings(integration, config as GetCourseConfiguration, lastSyncDate),
            IntegrationTypeEnum.PowerBI => powerBIHandler.CreateSettings(integration, config as PowerBIConfiguration, lastSyncDate),
            IntegrationTypeEnum.Ozon => ozonHandler.CreateSettings(integration, config as OzonConfiguration, lastSyncDate),
            IntegrationTypeEnum.GoogleSheets => googleSheetsHandler.CreateSettings(integration, config as GoogleSheetsConfiguration, lastSyncDate),
            _ => throw new NotSupportedException($"Integration type {integrationType} is not supported")
        };
    }

    private object? UpdateConfiguration(object? existingConfig, JsonDocument settings, IntegrationTypeEnum integrationType)
    {
        return integrationType switch
        {
            IntegrationTypeEnum.GetCourse => getCourseHandler.UpdateConfiguration(existingConfig as GetCourseConfiguration, settings),
            IntegrationTypeEnum.PowerBI => powerBIHandler.UpdateConfiguration(existingConfig as PowerBIConfiguration, settings),
            IntegrationTypeEnum.Ozon => ozonHandler.UpdateConfiguration(existingConfig as OzonConfiguration, settings),
            IntegrationTypeEnum.GoogleSheets => googleSheetsHandler.UpdateConfiguration(existingConfig as GoogleSheetsConfiguration, settings),
            _ => throw new NotSupportedException($"Integration type {integrationType} is not supported")
        };
    }


    private bool GetIsActiveFromSettings(JsonDocument settings)
    {
        if (settings.RootElement.TryGetProperty("isActive", out var isActive))
            return isActive.GetBoolean();
        return false;
    }
}
