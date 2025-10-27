using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Models;
using Tabligo.Domain.Enums;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.JobOperation;

public class JobOperationsTypeaheadHandler(
    TabligoContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<PaginationDto<JobOperationDto>> HandleAsync(string jobType, TypeaheadRequest request, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        const int COUNT = 25;

        var query = db.JobOperations
            .AsNoTracking()
            .Where(j => j.CompanyId == companyId && j.Type == jobType);

        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(j => j.Name.ToLower().Contains(q));
        }

        var page = Math.Max(request.Page, 1);

        var jobs = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((page - 1) * COUNT)
            .Take(COUNT)
            .ToListAsync(ct);

        var count = await query.CountAsync(ct);

        var items = jobs.Select(j => new JobOperationDto
        {
            Id = j.Id,
            Type = j.Type,
            Name = j.Name,
            Status = j.Status,
            CreatedAt = j.CreatedAt,
            CompletedAt = j.CompletedAt,
            RetryCount = j.RetryCount
        }).ToList();

        return new PaginationDto<JobOperationDto>(items, count, (int)Math.Ceiling((float)count / COUNT));
    }
}

public class JobOperationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public JobOperationStatusEnum Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int RetryCount { get; set; }
}
