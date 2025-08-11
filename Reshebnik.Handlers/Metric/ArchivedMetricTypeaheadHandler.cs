using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using System.Linq;

namespace Reshebnik.Handlers.Metric;

public class ArchivedMetricTypeaheadHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<PaginationDto<ArchivedMetricDto>> HandleAsync(TypeaheadRequest request, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        const int COUNT = 50;

        var query = db.ArchivedMetrics
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .Include(a => a.Metric)
            .IgnoreQueryFilters();

        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(a => a.Metric.Name.ToLower().Contains(q));
        }

        var itemsQuery = query
            .Skip((request.Page ?? 0) * COUNT)
            .Take(COUNT);

        var metrics = await itemsQuery
            .Select(a => new
            {
                a.Id,
                a.FirstDate,
                a.LastDate,
                Name = a.Metric.Name
            })
            .ToListAsync(ct);
        var count = await query.CountAsync(ct);

        var items = metrics.Select(m => new ArchivedMetricDto
        {
            Id = m.Id,
            Name = m.Name,
            FirstDate = m.FirstDate,
            LastDate = m.LastDate
        }).ToList();

        return new PaginationDto<ArchivedMetricDto>(items, count, (int)Math.Ceiling((float)count / COUNT));
    }
}

