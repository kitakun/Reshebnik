using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.GoogleSheets;

public class GoogleSheetsProvider(
    GoogleSheetsApiClient apiClient,
    GoogleSheetsDataTransformer transformer,
    ILogger<GoogleSheetsProvider> logger)
    : IIntegrationProvider
{
    public IntegrationTypeEnum Type => IntegrationTypeEnum.GoogleSheets;

    public async Task<bool> TestConnectionAsync(string configuration, CancellationToken ct)
    {
        var result = await TestConnectionDetailedAsync(configuration, ct);
        return result.IsSuccess;
    }

    public async Task<IntegrationTestConnectionResult> TestConnectionDetailedAsync(string configuration, CancellationToken ct)
    {
        try
        {
            var config = JsonSerializer.Deserialize<GoogleSheetsConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.SpreadsheetId))
            {
                return new IntegrationTestConnectionResult
                {
                    IsSuccess = false,
                    Reason = "Неверная конфигурация: отсутствует Spreadsheet ID",
                    Endpoint = "Проверка конфигурации",
                    IntegrationType = IntegrationTypeEnum.GoogleSheets
                };
            }

            var result = await apiClient.TestConnectionAsync(config, ct);

            // Update configuration with validation result
            config.IsValid = result.IsSuccess;

            return new IntegrationTestConnectionResult
            {
                IsSuccess = result.IsSuccess,
                Reason = result.Reason,
                Endpoint = "Google Sheets API",
                Response = result.SpreadsheetTitle,
                IntegrationType = IntegrationTypeEnum.GoogleSheets
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Sheets connection test failed");
            return new IntegrationTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Тест соединения не удался с исключением: {ex.Message}",
                Endpoint = "Обработка исключений",
                IntegrationType = IntegrationTypeEnum.GoogleSheets
            };
        }
    }

    public async Task<bool> ActivateAsync(int companyId, string configuration, CancellationToken ct)
    {
        try
        {
            var config = JsonSerializer.Deserialize<GoogleSheetsConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.SpreadsheetId))
            {
                return false;
            }

            // Test connection to ensure configuration is valid
            var result = await apiClient.TestConnectionAsync(config, ct);
            return result.IsSuccess;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Sheets activation failed");
            return false;
        }
    }

    public Task<bool> DeactivateAsync(int companyId, CancellationToken ct)
    {
        // Google Sheets doesn't require any cleanup for deactivation
        return Task.FromResult(true);
    }

    public async Task<List<IntegrationImportRequest>> FetchDataAsync(
        int companyId,
        string configuration,
        Dictionary<string, object>? filters = null,
        CancellationToken ct = default)
    {
        try
        {
            var config = JsonSerializer.Deserialize<GoogleSheetsConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.SpreadsheetId))
            {
                throw new InvalidOperationException("Invalid Google Sheets configuration");
            }

            // Extract range from filters if provided
            string? range = null;
            if (filters != null && filters.TryGetValue("range", out var rangeFilter))
            {
                range = rangeFilter?.ToString();
            }

            logger.LogInformation("Fetching data from Google Sheets for company {CompanyId}", companyId);

            // Fetch spreadsheet data
            var rows = await apiClient.GetSpreadsheetDataAsync(config, range, ct);

            if (rows.Count == 0)
            {
                logger.LogWarning("No data found in Google Sheets for company {CompanyId}", companyId);
                return new List<IntegrationImportRequest>();
            }

            // Transform rows to import requests
            var importRequests = transformer.TransformSpreadsheetRowsToImportRequests(
                rows,
                config.HasHeaderRow);

            logger.LogInformation("Successfully fetched {Count} items from Google Sheets for company {CompanyId}",
                importRequests.Count, companyId);

            return importRequests;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch data from Google Sheets for company {CompanyId}", companyId);
            throw;
        }
    }
}


