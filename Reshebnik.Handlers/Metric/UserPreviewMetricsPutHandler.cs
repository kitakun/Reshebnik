using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Clickhouse.Handlers;

namespace Reshebnik.Handlers.Metric;

public class UserPreviewMetricsPutHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    FetchUserMetricsHandler putHandler)
{
    public async Task<bool> HandleAsync(
        int userId,
        PutPreviewMetricsDto request,
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;

        var employee = await db.Employees
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == userId && e.CompanyId == companyId, ct);
        if (employee == null) return false;

        var metricIds = request.Metrics.Select(m => m.Id).ToList();
        var metrics = await db.MetricEmployeeLinks
            .Where(l => l.EmployeeId == userId && metricIds.Contains(l.MetricId) && l.Metric.CompanyId == companyId)
            .Include(l => l.Metric)
                .ThenInclude(m => m.DepartmentLinks)
            .Select(l => l.Metric)
            .ToListAsync(ct);

        await Parallel.ForEachAsync(metrics, ct, async (metric, cts) =>
        {
            var item = request.Metrics.FirstOrDefault(m => m.Id == metric.Id);
            if (item == null) return;

            for (var i = 0; i < item.FactData.Length; i++)
            {
                var date = AddOffset(request.From.Date, request.PeriodType, i);
                if (date > request.To.Date) break;

                var deptIds = metric.DepartmentLinks.Select(l => (int?)l.DepartmentId).DefaultIfEmpty(null);
                foreach (var deptId in deptIds)
                {
                    switch (metric.Type)
                    {
                        case MetricTypeEnum.PlanFact:
                            await putHandler.PutAsync(
                                metric.Id,
                                MetricValueTypeEnum.Fact,
                                userId,
                                companyId,
                                deptId,
                                request.PeriodType,
                                date,
                                item.FactData[i],
                                cts);
                            break;
                        case MetricTypeEnum.FactOnly:
                            await putHandler.PutAsync(
                                metric.Id,
                                MetricValueTypeEnum.Fact,
                                userId,
                                companyId,
                                deptId,
                                request.PeriodType,
                                date,
                                item.FactData[i],
                                cts);
                            break;
                        case MetricTypeEnum.Cumulative:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            for (var i = 0; i < item.PlanData.Length; i++)
            {
                var date = AddOffset(request.From.Date, request.PeriodType, i);
                if (date > request.To.Date) break;

                var deptIds = metric.DepartmentLinks.Select(l => (int?)l.DepartmentId).DefaultIfEmpty(null);
                foreach (var deptId in deptIds)
                {
                    switch (metric.Type)
                    {
                        case MetricTypeEnum.PlanFact:
                            await putHandler.PutAsync(
                                metric.Id,
                                MetricValueTypeEnum.Plan,
                                userId,
                                companyId,
                                deptId,
                                request.PeriodType,
                                date,
                                item.PlanData[i],
                                cts);
                            break;
                        case MetricTypeEnum.FactOnly:
                            break;
                        case MetricTypeEnum.Cumulative:
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }
        });

        await db.SaveChangesAsync(ct);
        return true;
    }

    private static DateTime AddOffset(DateTime start, PeriodTypeEnum period, int index)
    {
        // Приводим к началу периода
        start = period switch
        {
            PeriodTypeEnum.Day => start.Date,
            PeriodTypeEnum.Week => StartOfWeek(start, DayOfWeek.Monday),
            PeriodTypeEnum.Month => new DateTime(start.Year, start.Month, 1),
            PeriodTypeEnum.Quartal => new DateTime(start.Year, ((start.Month - 1) / 3) * 3 + 1, 1),
            PeriodTypeEnum.Year => new DateTime(start.Year, 1, 1),
            _ => start
        };

        // Добавляем смещение
        return period switch
        {
            PeriodTypeEnum.Day => start.AddDays(index),
            PeriodTypeEnum.Week => start.AddDays(7 * index),
            PeriodTypeEnum.Month => start.AddMonths(index),
            PeriodTypeEnum.Quartal => start.AddMonths(3 * index),
            PeriodTypeEnum.Year => start.AddYears(index),
            _ => start
        };
    }

    // Вспомогательный метод для получения начала недели (например, понедельника)
    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-1 * diff);
    }
}