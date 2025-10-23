using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Models.JobOperation;
using Tabligo.Domain.Enums;
using Tabligo.EntityFramework;
using System.Text.Json;

namespace Tabligo.Handlers.JobOperation;

public class JobOperationGetHandler(TabligoContext db)
{
    public async Task<JobOperationStatusResponse> GetJobStatusAsync(int jobId, int companyId, CancellationToken ct)
    {
        var job = await db.JobOperations
            .FirstOrDefaultAsync(j => j.Id == jobId && j.CompanyId == companyId, ct);

        if (job == null)
        {
            throw new InvalidOperationException($"Job {jobId} not found or does not belong to company {companyId}");
        }

        var response = new JobOperationStatusResponse
        {
            Id = job.Id,
            Type = job.Type,
            Name = job.Name,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            RetryCount = job.RetryCount
        };

        if (job.InputData != null)
        {
            response.InputData = JsonSerializer.Deserialize<object>(job.InputData.RootElement.GetRawText());
        }

        if (job.Data != null)
        {
            response.ResultData = JsonSerializer.Deserialize<object>(job.Data.RootElement.GetRawText());
        }

        return response;
    }
}
