using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Entities;
using Tabligo.Domain.Enums;
using Tabligo.EntityFramework;
using System.Text.Json;

namespace Tabligo.Handlers.JobOperation;

public class JobOperationQueue(TabligoContext db) : IJobOperationQueue
{
    public async Task<int> EnqueueAsync(int companyId, string type, string name, object data, CancellationToken ct)
    {
        var job = new JobOperationEntity
        {
            CompanyId = companyId,
            Type = type,
            Name = name,
            Hash = data.GetHashCode(),
            CreatedAt = DateTime.UtcNow,
            Status = JobOperationStatusEnum.InQueue,
            RetryCount = 0,
            InputData = JsonDocument.Parse(JsonSerializer.Serialize(data)),
            Data = null
        };

        db.JobOperations.Add(job);
        await db.SaveChangesAsync(ct);
        return job.Id;
    }

    public async Task<JobOperationEntity?> DequeueAsync(CancellationToken ct)
    {
        var job = await db.JobOperations
            .Where(j => j.Status == JobOperationStatusEnum.InQueue)
            .OrderBy(j => j.CreatedAt)
            .FirstOrDefaultAsync(ct);

        if (job != null)
        {
            job.Status = JobOperationStatusEnum.Processing;
            await db.SaveChangesAsync(ct);
        }

        return job;
    }

    public async Task UpdateStatusAsync(int jobId, JobOperationStatusEnum status, object? resultData, CancellationToken ct)
    {
        var job = await db.JobOperations.FindAsync([jobId], ct);
        if (job == null) return;

        job.Status = status;

        if (resultData != null)
        {
            job.Data = JsonDocument.Parse(JsonSerializer.Serialize(resultData));
        }

        await db.SaveChangesAsync(ct);
    }
}
