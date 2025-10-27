using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;
using Tabligo.Domain.Models.JobOperation;

namespace Tabligo.Integrations.Integrations.Ozon;

public class OzonProvider : IIntegrationProvider
{
    private readonly OzonApiClient _apiClient;
    private readonly OzonDataTransformer _transformer;
    private readonly ILogger<OzonProvider> _logger;

    public OzonProvider(
        OzonApiClient apiClient, 
        OzonDataTransformer transformer,
        ILogger<OzonProvider> logger)
    {
        _apiClient = apiClient;
        _transformer = transformer;
        _logger = logger;
    }

    public IntegrationTypeEnum Type => IntegrationTypeEnum.Ozon;

    public async Task<bool> TestConnectionAsync(string configuration, CancellationToken ct)
    {
        var result = await TestConnectionDetailedAsync(configuration, ct);
        return result.IsSuccess;
    }

    public async Task<IntegrationTestConnectionResult> TestConnectionDetailedAsync(string configuration, CancellationToken ct)
    {
        try
        {
            var config = JsonSerializer.Deserialize<OzonConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.ClientId) || 
                string.IsNullOrEmpty(config.ApiKey))
            {
                return new IntegrationTestConnectionResult
                {
                    IsSuccess = false,
                    Reason = "Неверная конфигурация: отсутствует clientId или apiKey",
                    Endpoint = "Проверка конфигурации",
                    IntegrationType = IntegrationTypeEnum.Ozon
                };
            }

            var isSuccess = await _apiClient.TestConnectionAsync(config, ct);
            
            // Update configuration with validation result
            config.IsValid = isSuccess;
            
            return new IntegrationTestConnectionResult
            {
                IsSuccess = isSuccess,
                Reason = isSuccess ? "Тест соединения с Ozon успешен" : "Тест соединения с Ozon не удался",
                Endpoint = "Ozon Seller API",
                IntegrationType = IntegrationTypeEnum.Ozon
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ozon connection test failed");
            return new IntegrationTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Тест соединения с Ozon не удался с исключением: {ex.Message}",
                Endpoint = "Обработка исключений",
                IntegrationType = IntegrationTypeEnum.Ozon
            };
        }
    }

    public async Task<bool> ActivateAsync(int companyId, string configuration, CancellationToken ct)
    {
        try
        {
            var config = JsonSerializer.Deserialize<OzonConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.ClientId) || 
                string.IsNullOrEmpty(config.ApiKey))
            {
                return false;
            }

            // Test connection to ensure configuration is valid
            return await _apiClient.TestConnectionAsync(config, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ozon activation failed");
            return false;
        }
    }

    public Task<bool> DeactivateAsync(int companyId, CancellationToken ct)
    {
        // Ozon doesn't require any cleanup for deactivation
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
            var config = JsonSerializer.Deserialize<OzonConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.ClientId) || 
                string.IsNullOrEmpty(config.ApiKey))
            {
                throw new InvalidOperationException("Invalid Ozon configuration");
            }

            var allRequests = new List<IntegrationImportRequest>();

            // Fetch products
            if (config.ImportProducts)
            {
                _logger.LogInformation("Fetching Ozon products for company {CompanyId}", companyId);
                var products = await _apiClient.GetProductsAsync(config, ct);
                allRequests.AddRange(_transformer.TransformProductsToMetrics(products));
            }

            // Fetch postings (sales)
            if (config.ImportPostings)
            {
                _logger.LogInformation("Fetching Ozon postings for company {CompanyId}", companyId);
                var postings = await _apiClient.GetPostingsAsync(config, ct);
                allRequests.AddRange(_transformer.TransformPostingsToMetrics(postings));
            }

            // Fetch returns
            if (config.ImportReturns)
            {
                _logger.LogInformation("Fetching Ozon returns for company {CompanyId}", companyId);
                var returns = await _apiClient.GetReturnsAsync(config, ct);
                allRequests.AddRange(_transformer.TransformReturnsToMetrics(returns));
            }

            // Fetch actions (promotions)
            if (config.ImportActions)
            {
                _logger.LogInformation("Fetching Ozon actions for company {CompanyId}", companyId);
                var actions = await _apiClient.GetActionsAsync(config, ct);
                allRequests.AddRange(_transformer.TransformActionsToMetrics(actions));

                // Fetch action products for each action
                foreach (var action in actions)
                {
                    try
                    {
                        var actionProducts = await _apiClient.GetActionProductsAsync(config, action.Id, ct);
                        // Action products are already included in the action transformation
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to fetch products for action {ActionId}", action.Id);
                    }
                }
            }

            // Fetch financial reports
            if (config.ImportFinancialReports)
            {
                _logger.LogInformation("Fetching Ozon financial reports for company {CompanyId}", companyId);
                var financialReports = await _apiClient.GetFinancialReportsAsync(config, ct);
                allRequests.AddRange(_transformer.TransformFinancialReportsToMetrics(financialReports));
            }

            _logger.LogInformation("Successfully fetched {Count} items from Ozon for company {CompanyId}", 
                allRequests.Count, companyId);

            return allRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from Ozon for company {CompanyId}", companyId);
            throw;
        }
    }

    public async Task<object?> ProcessAsync(
        Domain.Entities.JobOperationEntity job,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        if (job.InputData == null)
        {
            throw new InvalidOperationException("Job does not contain input data");
        }

        // Deserialize input data
        var inputData = JsonSerializer.Deserialize<IntegrationSyncInputData>(
            job.InputData.RootElement.GetRawText(),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (inputData == null)
        {
            throw new InvalidOperationException("Failed to deserialize input data");
        }

        // Fetch data using FetchDataAsync
        var importRequests = await FetchDataAsync(
            inputData.CompanyId,
            inputData.Configuration,
            inputData.Filters,
            cancellationToken);

        return importRequests;
    }

    private class IntegrationSyncInputData
    {
        public int CompanyId { get; set; }
        public string Configuration { get; set; } = string.Empty;
        public Dictionary<string, object>? Filters { get; set; }
    }
}
