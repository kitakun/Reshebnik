using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Clickhouse.Handlers;
using System.Linq;
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

        var metrics = await db.MetricEmployeeLinks
            .AsNoTracking()
            .Where(l => l.EmployeeId == userId && l.Metric.CompanyId == companyId)
            .Include(l => l.Metric)
                .ThenInclude(m => m.DepartmentLinks)
            .Select(l => l.Metric)
            .ToListAsync(ct);

        var result = new UserPreviewMetricsDto
        {
            Fio = employee.FIO,
            UserComment = employee.Comment
        };

        double sumAvg = 0;
        foreach (var metric in metrics)
        {
            var last12Range = periodType switch
            {
                PeriodTypeEnum.Day => new DateRange(range.To.AddDays(-11), range.To),
                PeriodTypeEnum.Week => new DateRange(range.From.AddDays(-5), range.To),
                PeriodTypeEnum.Quartal or PeriodTypeEnum.Year => new DateRange(new DateTime(range.To.Year, 1, 1), new DateTime(range.To.Year, 12, 31)),
                _ => range
            };

            var last12Data = await fetchHandler.HandleAsync(
                last12Range,
                metric.Id,
                periodType == PeriodTypeEnum.Week ? PeriodTypeEnum.Day : periodType,
                metric.PeriodType,
                ct);

            var yearRange = new DateRange(
                new DateTime(range.To.Year, 1, 1),
                new DateTime(range.To.Year, 12, 31));

            var totalData = await fetchHandler.HandleAsync(
                yearRange,
                metric.Id,
                PeriodTypeEnum.Month,
                metric.PeriodType,
                ct);

            double?[] growth = new double?[last12Data.FactData.Length];
            if (metric.ShowGrowthPercent)
            {
                for (var i = 1; i < last12Data.FactData.Length; i++)
                {
                    var prev = last12Data.FactData[i - 1];
                    growth[i] = prev != 0
                        ? ((double)(last12Data.FactData[i] - prev) / prev) * 100
                        : null;
                }
            }

            var factAvg = last12Data.FactData.Length > 0 ? last12Data.FactData.Average() : 0;

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
                Last12PointsFact = last12Data.FactData,
                Last12PointsPlan = last12Data.PlanData,
                TotalPlanData = totalData.PlanData,
                TotalFactData = totalData.FactData,
                GrowthPercent = growth,
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

