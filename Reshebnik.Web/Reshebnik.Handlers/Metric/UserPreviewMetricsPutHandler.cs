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
        var metrics = await db.Metrics
            .Where(m => m.CompanyId == companyId && m.EmployeeId == userId && metricIds.Contains(m.Id))
            .ToListAsync(ct);

        var bdataTasks = new List<Task>();
        foreach (var metric in metrics)
        {
            var item = request.Metrics.FirstOrDefault(m => m.Id == metric.Id);
            if (item == null) continue;

            for (var i = 0; i < item.FactData.Length; i++)
            {
                var date = AddOffset(request.From.Date, request.PeriodType, i);
                if (date > request.To.Date) break;

                switch (metric.Type)
                {
                    case MetricTypeEnum.PlanFact:
                        bdataTasks.Add(putHandler.PutAsync(
                            metric.ClickHouseKey,
                            userId,
                            companyId,
                            metric.DepartmentId,
                            request.PeriodType,
                            date,
                            item.FactData[i],
                            ct));
                        bdataTasks.Add(putHandler.PutAsync(
                            metric.ClickHouseKey,
                            userId,
                            companyId,
                            metric.DepartmentId,
                            request.PeriodType,
                            date,
                            item.PlanData[i],
                            ct));
                        break;
                    case MetricTypeEnum.FactOnly:
                        bdataTasks.Add(putHandler.PutAsync(
                            metric.ClickHouseKey,
                            userId,
                            companyId,
                            metric.DepartmentId,
                            request.PeriodType,
                            date,
                            item.FactData[i],
                            ct));
                        break;
                    case MetricTypeEnum.Cumulative:
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        await Task.WhenAll(bdataTasks);

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