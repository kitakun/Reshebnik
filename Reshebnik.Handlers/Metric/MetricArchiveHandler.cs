using Microsoft.EntityFrameworkCore;

using System;
using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Handlers.Auth;

namespace Reshebnik.Handlers.Metric;

public class MetricArchiveHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    UserContextHandler userContext)
{
    public async Task HandleAsync(int id, MetricArchiveDto dto, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var metric = await db.Metrics.FirstAsync(m => m.CompanyId == companyId && m.Id == id, ct);

        var archived = new ArchivedMetricEntity
        {
            CompanyId = companyId,
            MetricId = metric.Id,
            MetricType = metric.Type,
            FirstDate = dto.FirstDate,
            LastDate = dto.LastDate,
            ArchivedAt = DateTime.UtcNow,
            ArchivedByUserId = userContext.CurrentUserId
        };

        metric.IsArchived = true;
        metric.ArchivedMetric = archived;

        db.ArchivedMetrics.Add(archived);
        await db.SaveChangesAsync(ct);
    }
}

