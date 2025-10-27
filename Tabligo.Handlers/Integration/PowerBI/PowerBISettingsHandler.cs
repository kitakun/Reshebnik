using System.Text.Json;
using Tabligo.Domain.Entities;
using Tabligo.Domain.Extensions;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Handlers.Integration.PowerBI;

public class PowerBISettingsHandler
{
    public PowerBIImportSettings GetDefaultSettings()
    {
        return new PowerBIImportSettings
        {
            ImportDatasets = true,
            ImportReports = true,
            ImportDashboards = true
        };
    }

    public PowerBIConfiguration GetDefaultConfiguration()
    {
        return new PowerBIConfiguration
        {
            TenantId = string.Empty,
            ClientId = string.Empty,
            ClientSecret = string.Empty,
            ImportWorkspaces = true,
            ImportDatasets = true,
            ImportReports = true,
            ImportDashboards = true,
            ImportUsers = true,
            IsValid = false
        };
    }

    public PowerBIIntegrationSettings CreateSettings(IntegrationEntity integration, PowerBIConfiguration? config, DateTime? lastSyncDate)
    {
        return new PowerBIIntegrationSettings
        {
            Id = integration.Id.ToString(),
            ClientId = config?.ClientId ?? string.Empty,
            ClientSecret = config?.ClientSecret?.Mask() ?? string.Empty,
            TenantId = config?.TenantId ?? string.Empty,
            IsActive = integration.IsActivated,
            LastSyncDate = lastSyncDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ImportSettings = new PowerBIImportSettings
            {
                ImportDatasets = config?.ImportDatasets ?? true,
                ImportReports = config?.ImportReports ?? true,
                ImportDashboards = config?.ImportDashboards ?? true
            }
        };
    }

    public PowerBIConfiguration? UpdateConfiguration(PowerBIConfiguration? existingConfig, JsonDocument settings)
    {
        // Clone existing config or create new one
        var config = existingConfig != null 
            ? JsonSerializer.Deserialize<PowerBIConfiguration>(JsonSerializer.Serialize(existingConfig)) 
            : new PowerBIConfiguration();
        
        if (config == null)
            config = new PowerBIConfiguration();
        
        // Only update properties that are present in settings
        if (settings.RootElement.TryGetProperty("clientId", out var clientId))
            config.ClientId = clientId.GetString() ?? string.Empty;
        
        if (settings.RootElement.TryGetProperty("clientSecret", out var clientSecret))
            config.ClientSecret = clientSecret.GetString() ?? string.Empty;
        
        if (settings.RootElement.TryGetProperty("tenantId", out var tenantId))
            config.TenantId = tenantId.GetString() ?? string.Empty;

        // Update import settings only if importSettings property exists
        if (settings.RootElement.TryGetProperty("importSettings", out var importSettings))
        {
            if (importSettings.TryGetProperty("importDatasets", out var importDatasets))
                config.ImportDatasets = importDatasets.GetBoolean();
            if (importSettings.TryGetProperty("importReports", out var importReports))
                config.ImportReports = importReports.GetBoolean();
            if (importSettings.TryGetProperty("importDashboards", out var importDashboards))
                config.ImportDashboards = importDashboards.GetBoolean();
        }

        return config;
    }
}
