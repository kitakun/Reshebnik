using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Enums;
using Tabligo.Domain.Models;
using Tabligo.Domain.Models.Metric;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Metric;

public class ArchivedMetricTypeaheadHandler(
    TabligoContext db,
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
            query = query.Where(a => (a.Metric != null ? a.Metric.Name ?? string.Empty : string.Empty).ToLower().Contains(q) || (a.Indicator != null ? a.Indicator.Name ?? string.Empty : string.Empty).ToLower().Contains(q));
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
                Name = a.MetricType == ArchiveMetricTypeEnum.Employee ? (a.Metric != null ? a.Metric.Name ?? string.Empty : string.Empty) : (a.Indicator != null ? a.Indicator.Name ?? string.Empty : string.Empty)
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

