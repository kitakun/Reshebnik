using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Metric;

public class ArchivedMetricTypeaheadHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<PaginationDto<ArchivedMetricDto>> HandleAsync(TypeaheadRequest request, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        const int COUNT = 25;

        var query = db.ArchivedMetrics
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId)
            .Include(a => a.Metric)
            .IgnoreQueryFilters();

        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(a => (a.Metric.Name ?? string.Empty).ToLower().Contains(q) || (a.Indicator.Name ?? string.Empty).ToLower().Contains(q));
        }

        var page = Math.Max(request.Page, 1);

        var itemsQuery = query
            .Skip((page - 1) * COUNT)
            .Take(COUNT);

        var metrics = await itemsQuery
            .Select(a => new
            {
                a.Id,
                a.FirstDate,
                a.LastDate,
                a.MetricType,
                entityId = a.MetricType == ArchiveMetricTypeEnum.Employee ? a.MetricId ?? 0 : a.IndicatorId ?? 0,
                Name = a.MetricType == ArchiveMetricTypeEnum.Employee ? a.Metric.Name ?? string.Empty : a.Indicator.Name ?? string.Empty
            })
            .ToListAsync(ct);
        var count = await query.CountAsync(ct);

        var items = metrics.Select(m => new ArchivedMetricDto
        {
            Id = m.Id,
            Name = m.Name,
            FirstDate = m.FirstDate,
            LastDate = m.LastDate,
            EntityId = m.entityId,
            MetricType = m.MetricType
        }).ToList();

        return new PaginationDto<ArchivedMetricDto>(items, count, (int)Math.Ceiling((float)count / COUNT));
    }
}

