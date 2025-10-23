using Tabligo.Domain.Entities;
using Tabligo.Domain.Enums;

namespace Tabligo.Handlers.JobOperation;

public interface IJobOperationQueue
{
    Task<int> EnqueueAsync(int companyId, string type, string name, object data, CancellationToken ct);
    Task<JobOperationEntity?> DequeueAsync(CancellationToken ct);
    Task UpdateStatusAsync(int jobId, JobOperationStatusEnum status, object? resultData, CancellationToken ct);
}
