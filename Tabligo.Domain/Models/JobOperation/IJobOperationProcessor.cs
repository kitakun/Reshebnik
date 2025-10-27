namespace Tabligo.Domain.Models.JobOperation;

/// <summary>
/// Interface for job operation processors
/// </summary>
public interface IJobOperationProcessor
{
    /// <summary>
    /// The job type this processor handles
    /// </summary>
    string JobType { get; }

    /// <summary>
    /// Processes a job operation
    /// </summary>
    /// <param name="job">The job to process</param>
    /// <param name="serviceProvider">Service provider for dependency injection</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The result of processing</returns>
    Task<object?> ProcessAsync(
        Entities.JobOperationEntity job,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken);
}
