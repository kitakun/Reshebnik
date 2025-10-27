using System.Text.Json;
using Microsoft.Extensions.Logging;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;
using Tabligo.Domain.Models.JobOperation;

namespace Tabligo.Integrations.Integrations.PowerBI;

public class PowerBIProvider : IIntegrationProvider
{
    private readonly PowerBIApiClient _apiClient;
    private readonly PowerBIDataTransformer _transformer;
    private readonly ILogger<PowerBIProvider> _logger;

    public PowerBIProvider(
        PowerBIApiClient apiClient, 
        PowerBIDataTransformer transformer,
        ILogger<PowerBIProvider> logger)
    {
        _apiClient = apiClient;
        _transformer = transformer;
        _logger = logger;
    }

    public IntegrationTypeEnum Type => IntegrationTypeEnum.PowerBI;

    public async Task<bool> TestConnectionAsync(string configuration, CancellationToken ct)
    {
        var result = await TestConnectionDetailedAsync(configuration, ct);
        return result.IsSuccess;
    }

    public async Task<IntegrationTestConnectionResult> TestConnectionDetailedAsync(string configuration, CancellationToken ct)
    {
        try
        {
            var config = JsonSerializer.Deserialize<PowerBIConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.TenantId) || 
                string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
            {
                return new IntegrationTestConnectionResult
                {
                    IsSuccess = false,
                    Reason = "Неверная конфигурация: отсутствует tenantId, clientId или clientSecret",
                    Endpoint = "Проверка конфигурации",
                    IntegrationType = IntegrationTypeEnum.PowerBI
                };
            }

            var isSuccess = await _apiClient.TestConnectionAsync(config, ct);
            
            // Update configuration with validation result
            config.IsValid = isSuccess;
            
            return new IntegrationTestConnectionResult
            {
                IsSuccess = isSuccess,
                Reason = isSuccess ? "Тест соединения с PowerBI успешен" : "Тест соединения с PowerBI не удался",
                Endpoint = "PowerBI OAuth2 Token Endpoint",
                IntegrationType = IntegrationTypeEnum.PowerBI
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PowerBI connection test failed");
            return new IntegrationTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Тест соединения с PowerBI не удался с исключением: {ex.Message}",
                Endpoint = "Обработка исключений",
                IntegrationType = IntegrationTypeEnum.PowerBI
            };
        }
    }

    public async Task<bool> ActivateAsync(int companyId, string configuration, CancellationToken ct)
    {
        try
        {
            var config = JsonSerializer.Deserialize<PowerBIConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.TenantId) || 
                string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
            {
                return false;
            }

            // Test connection to ensure configuration is valid
            return await _apiClient.TestConnectionAsync(config, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PowerBI activation failed");
            return false;
        }
    }

    public Task<bool> DeactivateAsync(int companyId, CancellationToken ct)
    {
        // PowerBI doesn't require any cleanup for deactivation
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
            var config = JsonSerializer.Deserialize<PowerBIConfiguration>(configuration);
            if (config == null || string.IsNullOrEmpty(config.TenantId) || 
                string.IsNullOrEmpty(config.ClientId) || string.IsNullOrEmpty(config.ClientSecret))
            {
                throw new InvalidOperationException("Invalid PowerBI configuration");
            }

            var allRequests = new List<IntegrationImportRequest>();

            // Fetch workspaces
            if (config.ImportWorkspaces)
            {
                _logger.LogInformation("Fetching PowerBI workspaces for company {CompanyId}", companyId);
                var workspaces = await _apiClient.GetWorkspacesAsync(config, ct);
                allRequests.AddRange(_transformer.TransformWorkspacesToDepartments(workspaces));

                // Fetch users for each workspace
                if (config.ImportUsers)
                {
                    var allUsers = new List<PowerBIUser>();
                    foreach (var workspace in workspaces)
                    {
                        try
                        {
                            var workspaceUsers = await _apiClient.GetWorkspaceUsersAsync(config, workspace.Id, ct);
                            allUsers.AddRange(workspaceUsers);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to fetch users for workspace {WorkspaceId}", workspace.Id);
                        }
                    }
                    allRequests.AddRange(_transformer.TransformUsersToEmployees(allUsers.DistinctBy(u => u.Identifier).ToList()));
                }
            }

            // Fetch datasets
            if (config.ImportDatasets)
            {
                _logger.LogInformation("Fetching PowerBI datasets for company {CompanyId}", companyId);
                var datasets = await _apiClient.GetDatasetsAsync(config, config.WorkspaceId, ct);
                allRequests.AddRange(_transformer.TransformDatasetsToMetrics(datasets));
            }

            // Fetch reports
            if (config.ImportReports)
            {
                _logger.LogInformation("Fetching PowerBI reports for company {CompanyId}", companyId);
                var reports = await _apiClient.GetReportsAsync(config, config.WorkspaceId, ct);
                allRequests.AddRange(_transformer.TransformReportsToMetrics(reports));
            }

            // Fetch dashboards
            if (config.ImportDashboards)
            {
                _logger.LogInformation("Fetching PowerBI dashboards for company {CompanyId}", companyId);
                var dashboards = await _apiClient.GetDashboardsAsync(config, config.WorkspaceId, ct);
                allRequests.AddRange(_transformer.TransformDashboardsToMetrics(dashboards));
            }

            _logger.LogInformation("Successfully fetched {Count} items from PowerBI for company {CompanyId}", 
                allRequests.Count, companyId);

            return allRequests;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch data from PowerBI for company {CompanyId}", companyId);
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


