using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;
using Tabligo.Domain.Models.JobOperation;

namespace Tabligo.Integrations;

/// <summary>
/// Interface for integration providers that can process data from external systems
/// </summary>
public interface IIntegrationProvider : IJobOperationProcessor
{
    /// <summary>
    /// The type of integration
    /// </summary>
    IntegrationTypeEnum Type { get; }

    /// <summary>
    /// The job type for background processing
    /// </summary>
    string IJobOperationProcessor.JobType => $"integration-sync-{Type}";
    
    /// <summary>
    /// Processes a job operation - default implementation
    /// </summary>
    Task<object?> IJobOperationProcessor.ProcessAsync(
        Domain.Entities.JobOperationEntity job,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken)
    {
        throw new NotSupportedException($"Job processing is not implemented for integration type {Type}. Provide a custom implementation.");
    }

    /// <summary>
    /// Tests the connection to the external system
    /// </summary>
    Task<bool> TestConnectionAsync(string configuration, CancellationToken ct);

    /// <summary>
    /// Tests the connection to the external system with detailed results
    /// </summary>
    Task<IntegrationTestConnectionResult> TestConnectionDetailedAsync(string configuration, CancellationToken ct);

    /// <summary>
    /// Activates the integration for a company
    /// </summary>
    Task<bool> ActivateAsync(int companyId, string configuration, CancellationToken ct);

    /// <summary>
    /// Deactivates the integration for a company
    /// </summary>
    Task<bool> DeactivateAsync(int companyId, CancellationToken ct);

    /// <summary>
    /// Fetches data from the external system and returns import requests
    /// This method should be called from a background job processor
    /// </summary>
    Task<List<IntegrationImportRequest>> FetchDataAsync(
        int companyId, 
        string configuration,
        Dictionary<string, object>? filters = null,
        CancellationToken ct = default);

    /// <summary>
    /// Imports data from the external system (called by background processor)
    /// </summary>
    Task<IntegrationImportResponse> ImportDataAsync(
        int companyId,
        string configuration,
        Dictionary<string, object>? filters = null,
        CancellationToken ct = default)
    {
        throw new NotSupportedException("ImportDataAsync is not implemented for this integration provider. Use FetchDataAsync instead.");
    }
}



