using System.Text.Json;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations;

/// <summary>
/// Base implementation for integration providers with common functionality
/// This class is NOT abstract - it can be used as-is or inherited from
/// </summary>
public class BaseIntegrationProvider : IIntegrationProvider
{
    protected BaseIntegrationProvider()
    {
    }

    /// <summary>
    /// The integration type (must be implemented by derived classes)
    /// </summary>
    public virtual IntegrationTypeEnum Type => throw new NotImplementedException("Type must be overridden in derived class");

    /// <summary>
    /// The job type for background processing
    /// </summary>
    public string JobType => $"integration-sync-{Type}";

    /// <summary>
    /// Processes a job operation for this integration
    /// </summary>
    public virtual async Task<object?> ProcessAsync(
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

        // Fetch data using the integration provider's FetchDataAsync method
        var importRequests = await FetchDataAsync(
            inputData.CompanyId,
            inputData.Configuration,
            inputData.Filters,
            cancellationToken);

        return importRequests;
    }

    /// <summary>
    /// Test connection - must be implemented by derived classes
    /// </summary>
    public virtual Task<bool> TestConnectionAsync(string configuration, CancellationToken ct)
    {
        throw new NotImplementedException("TestConnectionAsync must be implemented in derived class");
    }

    /// <summary>
    /// Test connection with detailed results - must be implemented by derived classes
    /// </summary>
    public virtual Task<IntegrationTestConnectionResult> TestConnectionDetailedAsync(string configuration, CancellationToken ct)
    {
        throw new NotImplementedException("TestConnectionDetailedAsync must be implemented in derived class");
    }

    /// <summary>
    /// Activate integration - must be implemented by derived classes
    /// </summary>
    public virtual Task<bool> ActivateAsync(int companyId, string configuration, CancellationToken ct)
    {
        throw new NotImplementedException("ActivateAsync must be implemented in derived class");
    }

    /// <summary>
    /// Deactivate integration - must be implemented by derived classes
    /// </summary>
    public virtual Task<bool> DeactivateAsync(int companyId, CancellationToken ct)
    {
        throw new NotImplementedException("DeactivateAsync must be implemented in derived class");
    }

    /// <summary>
    /// Fetch data from external system - must be implemented by derived classes
    /// </summary>
    public virtual Task<List<IntegrationImportRequest>> FetchDataAsync(
        int companyId,
        string configuration,
        Dictionary<string, object>? filters = null,
        CancellationToken ct = default)
    {
        throw new NotImplementedException("FetchDataAsync must be implemented in derived class");
    }

    /// <summary>
    /// Import data - can be overridden by derived classes
    /// </summary>
    public virtual Task<IntegrationImportResponse> ImportDataAsync(
        int companyId,
        string configuration,
        Dictionary<string, object>? filters = null,
        CancellationToken ct = default)
    {
        throw new NotSupportedException("ImportDataAsync is not implemented for this integration provider. Use FetchDataAsync instead.");
    }

    protected class IntegrationSyncInputData
    {
        public int CompanyId { get; set; }
        public string Configuration { get; set; } = string.Empty;
        public Dictionary<string, object>? Filters { get; set; }
    }
}
