using System.Text.Json;
using Tabligo.Domain.Entities;
using Tabligo.Domain.Extensions;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Handlers.Integration.Ozon;

public class OzonSettingsHandler
{
    public OzonImportSettings GetDefaultSettings()
    {
        return new OzonImportSettings
        {
            ImportProducts = true,
            ImportPostings = true,
            ImportReturns = true
        };
    }

    public OzonConfiguration GetDefaultConfiguration()
    {
        return new OzonConfiguration
        {
            ClientId = string.Empty,
            ApiKey = string.Empty,
            ImportProducts = true,
            ImportPostings = true,
            ImportReturns = true,
            ImportActions = true,
            ImportFinancialReports = true,
            Limit = 1000,
            IsValid = false
        };
    }

    public OzonIntegrationSettings CreateSettings(IntegrationEntity integration, OzonConfiguration? config, DateTime? lastSyncDate)
    {
        return new OzonIntegrationSettings
        {
            Id = integration.Id.ToString(),
            ClientId = config?.ClientId ?? string.Empty,
            ApiKey = config?.ApiKey?.Mask() ?? string.Empty,
            IsActive = integration.IsActivated,
            LastSyncDate = lastSyncDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ImportSettings = new OzonImportSettings
            {
                ImportProducts = config?.ImportProducts ?? true,
                ImportPostings = config?.ImportPostings ?? true,
                ImportReturns = config?.ImportReturns ?? true
            }
        };
    }

    public OzonConfiguration? UpdateConfiguration(OzonConfiguration? existingConfig, JsonDocument settings)
    {
        // Clone existing config or create new one
        var config = existingConfig != null 
            ? JsonSerializer.Deserialize<OzonConfiguration>(JsonSerializer.Serialize(existingConfig)) 
            : new OzonConfiguration();
        
        if (config == null)
            config = new OzonConfiguration();
        
        // Only update properties that are present in settings
        if (settings.RootElement.TryGetProperty("clientId", out var clientId))
            config.ClientId = clientId.GetString() ?? string.Empty;
        
        if (settings.RootElement.TryGetProperty("apiKey", out var apiKey))
            config.ApiKey = apiKey.GetString() ?? string.Empty;

        // Update import settings only if importSettings property exists
        if (settings.RootElement.TryGetProperty("importSettings", out var importSettings))
        {
            if (importSettings.TryGetProperty("importProducts", out var importProducts))
                config.ImportProducts = importProducts.GetBoolean();
            if (importSettings.TryGetProperty("importPostings", out var importPostings))
                config.ImportPostings = importPostings.GetBoolean();
            if (importSettings.TryGetProperty("importReturns", out var importReturns))
                config.ImportReturns = importReturns.GetBoolean();
        }

        return config;
    }
}
