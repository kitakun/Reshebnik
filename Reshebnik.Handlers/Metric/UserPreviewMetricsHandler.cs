using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Clickhouse.Handlers;
using Reshebnik.Domain.Enums;
using System.Collections.Concurrent;

namespace Reshebnik.Handlers.Metric;

public class UserPreviewMetricsHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    FetchUserMetricsHandler fetchHandler)
{
    private const int MAX_CONCURRENCY = 6;

    public async ValueTask<UserPreviewMetricsDto?> HandleAsync(
        int userId,
        DateRange range,
        PeriodTypeEnum periodType,
        CancellationToken ct = default)
    {
        var map = await HandleBulkAsync([userId], range, periodType, ct);
        return map.GetValueOrDefault(userId);
    }

    public async ValueTask<Dictionary<int, UserPreviewMetricsDto>> HandleBulkAsync(
        IEnumerable<int> userIds,
        DateRange range,
        PeriodTypeEnum periodType,
        CancellationToken ct = default)
    {
        var userSet = userIds?.Distinct().ToHashSet() ?? new HashSet<int>();
        if (userSet.Count == 0)
            return new Dictionary<int, UserPreviewMetricsDto>();

        var companyId = await companyContext.CurrentCompanyIdAsync;

        // 1) Подтягиваем сотрудников разом (только нужные поля)
        var employees = await db.Employees.AsNoTracking()
            .Where(e => userSet.Contains(e.Id) && e.CompanyId == companyId)
            .Select(e => new { e.Id, e.FIO, e.Comment })
            .ToListAsync(ct);

        var existingUserIds = employees.Select(e => e.Id).ToHashSet();
        if (existingUserIds.Count == 0)
            return new Dictionary<int, UserPreviewMetricsDto>();

        // 2) Связки "пользователь-метрика" с нужными полями метрики
        var links = await db.MetricEmployeeLinks.AsNoTracking()
            .Where(l => existingUserIds.Contains(l.EmployeeId) && l.Metric.CompanyId == companyId)
            .Select(l => new
            {
                l.EmployeeId,
                Metric = new
                {
                    l.Metric.Id,
                    l.Metric.Name,
                    l.Metric.Plan,
                    l.Metric.Min,
                    l.Metric.Max,
                    l.Metric.IsArchived,
                    l.Metric.PeriodType,
                    l.Metric.Type,
                    l.Metric.ShowGrowthPercent,
                    l.Metric.WeekType,
                    l.Metric.WeekStartDate
                }
            })
            .ToListAsync(ct);

        // 2.1) Группируем метрики по пользователям
        var userToMetrics = links
            .GroupBy(x => x.EmployeeId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.Metric).ToList());

        // 3) Уникальный набор метрик (по Id) для кэширования результатов fetch
        var distinctMetrics = links
            .Select(x => x.Metric)
            .GroupBy(m => m.Id)
            .Select(g => g.First())
            .ToList();

        // 4) Кэш расчётов по метрике: last12, total, growth и вспомогательные средние
        var metricCalcCache = new ConcurrentDictionary<int, MetricCalcResult>();

        // 4.1) Считаем для каждой метрики один раз (с ограничением параллелизма)
        using (var sem = new SemaphoreSlim(MAX_CONCURRENCY))
        {
            var tasks = distinctMetrics.Select(async metric =>
            {
                await sem.WaitAsync(ct);
                try
                {
                    var expected = GetExpectedPeriod(periodType, metric.PeriodType);
                    var last12Range = BuildLast12Range(periodType, metric.PeriodType, range);

                    var last12Data = await fetchHandler.HandleAsync(
                        last12Range,
                        metric.Id,
                        expected,
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

                    double?[] growth = [];
                    if (metric.ShowGrowthPercent)
                    {
                        growth = new double?[last12Data.FactData.Length];
                        if (metric.WeekType == WeekTypeEnum.Sliding)
                        {
                            for (var i = 7; i < growth.Length; i++)
                                growth[i] = last12Data.FactData[i - 7] - last12Data.FactData[i];
                        }
                        else
                        {
                            var offset = metric.WeekStartDate ?? 0;
                            for (var i = offset; i < growth.Length; i++)
                                growth[i] = last12Data.FactData[i - offset] - last12Data.FactData[i];
                        }
                    }

                    var factAvg = last12Data.FactData.Length > 0 ? last12Data.FactData.Average() : 0d;
                    var planAvg = last12Data.PlanData.Length > 0 ? last12Data.PlanData.Average() : 0d;
                    if (planAvg == 0 && metric.Plan.HasValue)
                        planAvg = (double)metric.Plan.Value;

                    var avgPercent = (int)(planAvg != 0 ? (factAvg / planAvg) * 100 : 0);

                    metricCalcCache[metric.Id] = new MetricCalcResult(
                        last12Data.PlanData,
                        last12Data.FactData,
                        totalData.PlanData,
                        totalData.FactData,
                        growth,
                        avgPercent
                    );
                }
                finally
                {
                    sem.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        // 5) Собираем DTO по каждому пользователю из кэша
        var result = new Dictionary<int, UserPreviewMetricsDto>(existingUserIds.Count);
        foreach (var e in employees)
        {
            var dto = new UserPreviewMetricsDto
            {
                Fio = e.FIO,
                UserComment = e.Comment
            };

            if (!userToMetrics.TryGetValue(e.Id, out var metrics))
            {
                result[e.Id] = dto;
                continue;
            }

            double sumAvg = 0d;
            int count = 0;

            foreach (var m in metrics)
            {
                if (!metricCalcCache.TryGetValue(m.Id, out var calc))
                    continue;

                var avgRounded = calc.AvgPercent;

                dto.Metrics.Add(new UserPreviewMetricItemDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Plan = m.Plan,
                    Min = m.Min,
                    Max = m.Max,
                    IsArchived = m.IsArchived,
                    Last12PointsPlan = calc.Last12Plan,
                    Last12PointsFact = calc.Last12Fact,
                    TotalPlanData = calc.TotalPlan,
                    TotalFactData = calc.TotalFact,
                    GrowthPercent = calc.Growth,
                    Period = m.PeriodType,
                    Type = m.Type,
                    Average = avgRounded
                });

                sumAvg += calc.AvgPercent;
                count++;
            }

            if (count > 0)
                dto.Average = Math.Round(sumAvg / count, 0, MidpointRounding.ToZero);

            result[e.Id] = dto;
        }

        return result;
    }

    private static PeriodTypeEnum GetExpectedPeriod(PeriodTypeEnum outer, PeriodTypeEnum metricPeriod)
    {
        if (metricPeriod == PeriodTypeEnum.Week && (outer == PeriodTypeEnum.Day || outer == PeriodTypeEnum.Custom))
            return PeriodTypeEnum.Week;
        return outer;
    }

    private static DateRange BuildLast12Range(PeriodTypeEnum outer, PeriodTypeEnum metricPeriod, DateRange range)
    {
        // Logика идентична твоей, но вынесена в функцию для переиспользования
        return outer switch
        {
            PeriodTypeEnum.Day when metricPeriod == PeriodTypeEnum.Week =>
                new DateRange(StartOfWeek(range.To.AddDays(-7 * 11), DayOfWeek.Monday), StartOfWeek(range.To, DayOfWeek.Monday)),

            PeriodTypeEnum.Day =>
                new DateRange(range.To.AddDays(-11), range.To),

            PeriodTypeEnum.Week =>
                new DateRange(StartOfWeek(range.To.AddDays(-7 * 11), DayOfWeek.Monday), StartOfWeek(range.To, DayOfWeek.Monday)),

            PeriodTypeEnum.Month =>
                new DateRange(new DateTime(range.To.AddMonths(-11).Year, range.To.AddMonths(-11).Month, 1),
                              new DateTime(range.To.Year, range.To.Month, DateTime.DaysInMonth(range.To.Year, range.To.Month))),

            PeriodTypeEnum.Quartal =>
                new DateRange(
                    new DateTime(range.To.AddMonths(-3 * 11).Year, ((range.To.AddMonths(-3 * 11).Month - 1) / 3) * 3 + 1, 1),
                    new DateTime(range.To.Year, ((range.To.Month - 1) / 3) * 3 + 3, DateTime.DaysInMonth(range.To.Year, ((range.To.Month - 1) / 3) * 3 + 3))
                ),

            PeriodTypeEnum.Year =>
                new DateRange(new DateTime(range.To.Year - 11, 1, 1), new DateTime(range.To.Year, 12, 31)),

            _ => range
        };
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-1 * diff);
    }

    private sealed record MetricCalcResult(
        int[] Last12Plan,
        int[] Last12Fact,
        int[] TotalPlan,
        int[] TotalFact,
        double?[] Growth,
        int AvgPercent);
}