using System.Text.Json;
using Tabligo.Domain.Entities;
using Tabligo.Domain.Extensions;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Handlers.Integration.GetCourse;

public class GetCourseSettingsHandler
{
    public GetCourseImportSettings GetDefaultSettings()
    {
        return new GetCourseImportSettings
        {
            ImportUsers = true,
            ImportOrders = true,
            ImportGroups = true,
            ImportPayments = true
        };
    }

    public GetCourseConfiguration GetDefaultConfiguration()
    {
        return new GetCourseConfiguration
        {
            AccountName = string.Empty,
            ApiKey = string.Empty,
            ImportUsers = true,
            ImportGroups = true,
            ImportOrders = true,
            ImportPayments = true,
            IsValid = false
        };
    }

    public GetCourseIntegrationSettings CreateSettings(IntegrationEntity integration, GetCourseConfiguration? config, DateTime? lastSyncDate)
    {
        return new GetCourseIntegrationSettings
        {
            Id = integration.Id.ToString(),
            AccountName = config?.AccountName ?? string.Empty,
            ApiKey = config?.ApiKey?.Mask() ?? string.Empty,
            IsActive = integration.IsActivated,
            LastSyncDate = lastSyncDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ImportSettings = new GetCourseImportSettings
            {
                ImportUsers = config?.ImportUsers ?? true,
                ImportOrders = config?.ImportOrders ?? true,
                ImportGroups = config?.ImportGroups ?? true,
                ImportPayments = config?.ImportPayments ?? true
            }
        };
    }

    public GetCourseConfiguration? UpdateConfiguration(GetCourseConfiguration? existingConfig, JsonDocument settings)
    {
        // Clone existing config or create new one
        var config = existingConfig != null 
            ? JsonSerializer.Deserialize<GetCourseConfiguration>(JsonSerializer.Serialize(existingConfig)) 
            : new GetCourseConfiguration();
        
        if (config == null)
            config = new GetCourseConfiguration();
        
        // Only update properties that are present in settings
        if (settings.RootElement.TryGetProperty("accountName", out var accountName))
            config.AccountName = accountName.GetString() ?? string.Empty;
        
        if (settings.RootElement.TryGetProperty("apiKey", out var apiKey))
            config.ApiKey = apiKey.GetString() ?? string.Empty;

        // Update import settings from top-level properties
        if (settings.RootElement.TryGetProperty("importUsers", out var importUsers))
            config.ImportUsers = importUsers.GetBoolean();
        if (settings.RootElement.TryGetProperty("importOrders", out var importOrders))
            config.ImportOrders = importOrders.GetBoolean();
        if (settings.RootElement.TryGetProperty("importGroups", out var importGroups))
            config.ImportGroups = importGroups.GetBoolean();
        if (settings.RootElement.TryGetProperty("importPayments", out var importPayments))
            config.ImportPayments = importPayments.GetBoolean();

        return config;
    }
}
