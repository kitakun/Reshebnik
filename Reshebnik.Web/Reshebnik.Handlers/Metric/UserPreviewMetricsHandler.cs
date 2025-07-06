using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Clickhouse.Handlers;

namespace Reshebnik.Handlers.Metric;

public class UserPreviewMetricsHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    FetchUserMetricsHandler fetchHandler)
{
    public async ValueTask<UserPreviewMetricsDto?> HandleAsync(
        int userId,
        DateRange range,
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == userId && e.CompanyId == companyId, ct);
        if (employee == null) return null;

        var metrics = await db.Metrics
            .AsNoTracking()
            .Where(m => m.CompanyId == companyId && m.EmployeeId == userId)
            .ToListAsync(ct);

        var result = new UserPreviewMetricsDto
        {
            Fio = employee.FIO,
            UserComment = employee.Comment
        };

        double sumAvg = 0;
        foreach (var metric in metrics)
        {
            var data = await fetchHandler.HandleAsync(
                range,
                metric.ClickHouseKey,
                metric.PeriodType,
                metric.PeriodType,
                ct);
            var avg = data.FactData.Length > 0 ? data.FactData.Average() : 0;
            sumAvg += avg;
            result.Metrics.Add(new UserPreviewMetricItemDto
            {
                Id = metric.Id,
                Name = metric.Name,
                PlanData = data.PlanData,
                FactData = data.FactData,
                Average = avg
            });
        }

        if (result.Metrics.Count > 0)
            result.Average = sumAvg / result.Metrics.Count;

        return result;
    }
}

