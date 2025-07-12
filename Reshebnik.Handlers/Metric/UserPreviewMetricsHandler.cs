using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Clickhouse.Handlers;
using Reshebnik.Domain.Enums;

namespace Reshebnik.Handlers.Metric;

public class UserPreviewMetricsHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    FetchUserMetricsHandler fetchHandler)
{
    public async ValueTask<UserPreviewMetricsDto?> HandleAsync(
        int userId,
        DateRange range,
        PeriodTypeEnum periodType,
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
                metric.Id,
                periodType,
                metric.PeriodType,
                ct);

            var factAvg = data.FactData.Length > 0 ? data.FactData.Average() : 0;

            double avgPercent = 0;
            if (metric.Min.HasValue && metric.Max.HasValue && metric.Max != metric.Min)
            {
                var min = (double)metric.Min.Value;
                var max = (double)metric.Max.Value;
                avgPercent = (factAvg - min) / (max - min) * 100;
            }

            sumAvg += avgPercent;
            result.Metrics.Add(new UserPreviewMetricItemDto
            {
                Id = metric.Id,
                Name = metric.Name,
                PlanData = data.PlanData,
                FactData = data.FactData,
                TotalPlanData = data.TotalPlanData,
                TotalFactData = data.TotalFactData,
                Period = metric.PeriodType,
                Type = metric.Type,
                Average = avgPercent
            });
        }

        if (result.Metrics.Count > 0)
            result.Average = sumAvg / result.Metrics.Count;

        return result;
    }
}

