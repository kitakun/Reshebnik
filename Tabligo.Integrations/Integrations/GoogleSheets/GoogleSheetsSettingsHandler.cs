using System.Text.Json;
using Tabligo.Domain.Entities;
using Tabligo.Domain.Extensions;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.GoogleSheets;

public class GoogleSheetsSettingsHandler
{
    public GoogleSheetsImportSettings GetDefaultSettings()
    {
        return new GoogleSheetsImportSettings
        {
            ImportRows = true,
            HasHeaderRow = true,
            Range = null
        };
    }

    public GoogleSheetsConfiguration GetDefaultConfiguration()
    {
        return new GoogleSheetsConfiguration
        {
            SpreadsheetId = string.Empty,
            SheetName = null,
            AccessToken = string.Empty,
            RefreshToken = null,
            ClientId = string.Empty,
            ClientSecret = string.Empty,
            ImportRows = true,
            HasHeaderRow = true,
            Range = null,
            IsValid = false,
            IsPublic = false,
            PublicCsvUrl = null
        };
    }

    public GoogleSheetsIntegrationSettings CreateSettings(IntegrationEntity integration, GoogleSheetsConfiguration? config, DateTime? lastSyncDate)
    {
        return new GoogleSheetsIntegrationSettings
        {
            Id = integration.Id.ToString(),
            SpreadsheetId = config?.SpreadsheetId ?? string.Empty,
            SheetName = config?.SheetName,
            IsActive = integration.IsActivated,
            LastSyncDate = lastSyncDate?.ToString("yyyy-MM-ddTHH:mm:ssZ"),
            ImportSettings = new GoogleSheetsImportSettings
            {
                ImportRows = config?.ImportRows ?? true,
                HasHeaderRow = config?.HasHeaderRow ?? true,
                Range = config?.Range
            }
        };
    }

    public GoogleSheetsConfiguration? UpdateConfiguration(GoogleSheetsConfiguration? existingConfig, JsonDocument settings)
    {
        var updatedConfig = existingConfig ?? GetDefaultConfiguration();

        // Update SpreadsheetId
        if (settings.RootElement.TryGetProperty("spreadsheetId", out var spreadsheetId))
        {
            updatedConfig.SpreadsheetId = spreadsheetId.GetString() ?? string.Empty;
        }

        // Update SheetName
        if (settings.RootElement.TryGetProperty("sheetName", out var sheetName))
        {
            updatedConfig.SheetName = sheetName.ValueKind == JsonValueKind.Null ? null : sheetName.GetString();
        }

        // Update AccessToken
        if (settings.RootElement.TryGetProperty("accessToken", out var accessToken))
        {
            updatedConfig.AccessToken = accessToken.GetString() ?? string.Empty;
        }

        // Update RefreshToken
        if (settings.RootElement.TryGetProperty("refreshToken", out var refreshToken))
        {
            updatedConfig.RefreshToken = refreshToken.ValueKind == JsonValueKind.Null ? null : refreshToken.GetString();
        }

        // Update ClientId
        if (settings.RootElement.TryGetProperty("clientId", out var clientId))
        {
            updatedConfig.ClientId = clientId.GetString() ?? string.Empty;
        }

        // Update ClientSecret
        if (settings.RootElement.TryGetProperty("clientSecret", out var clientSecret))
        {
            updatedConfig.ClientSecret = clientSecret.GetString() ?? string.Empty;
        }

        // Update import settings
        if (settings.RootElement.TryGetProperty("importSettings", out var importSettings))
        {
            if (importSettings.TryGetProperty("importRows", out var importRows))
            {
                updatedConfig.ImportRows = importRows.GetBoolean();
            }

            if (importSettings.TryGetProperty("hasHeaderRow", out var hasHeaderRow))
            {
                updatedConfig.HasHeaderRow = hasHeaderRow.GetBoolean();
            }

            if (importSettings.TryGetProperty("range", out var range))
            {
                updatedConfig.Range = range.ValueKind == JsonValueKind.Null ? null : range.GetString();
            }
        }

        // Public access options
        if (settings.RootElement.TryGetProperty("isPublic", out var isPublic))
        {
            updatedConfig.IsPublic = isPublic.GetBoolean();
        }

        if (settings.RootElement.TryGetProperty("publicCsvUrl", out var publicCsvUrl))
        {
            updatedConfig.PublicCsvUrl = publicCsvUrl.ValueKind == JsonValueKind.Null ? null : publicCsvUrl.GetString();
        }

        return updatedConfig;
    }
}


