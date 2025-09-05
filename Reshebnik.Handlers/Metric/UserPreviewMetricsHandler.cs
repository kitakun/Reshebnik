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
                    var needExpand = ComparePeriods(metric.PeriodType, periodType) > 0;
                    var expected = needExpand ? metric.PeriodType : periodType;
                    var last12Range = BuildLast12Range(periodType, metric.PeriodType, range);

                    var last12Data = await fetchHandler.HandleAsync(
                        last12Range,
                        metric.Id,
                        expected,
                        metric.PeriodType,
                        ct);

                    var plan = last12Data.PlanData;
                    var fact = last12Data.FactData;

                    if (needExpand)
                    {
                        plan = ExpandTo(plan, last12Range.From, range.To, expected, periodType);
                        fact = ExpandTo(fact, last12Range.From, range.To, expected, periodType);
                    }
                    else if (periodType != PeriodTypeEnum.Custom)
                    {
                        if (plan.Length != 12) Array.Resize(ref plan, 12);
                        if (fact.Length != 12) Array.Resize(ref fact, 12);
                    }

                    double?[] growth = [];
                    if (metric.ShowGrowthPercent)
                    {
                        growth = new double?[fact.Length];
                        if (metric.WeekType == WeekTypeEnum.Sliding)
                        {
                            for (var i = 7; i < growth.Length; i++)
                                growth[i] = fact[i - 7] - fact[i];
                        }
                        else
                        {
                            var offset = metric.WeekStartDate ?? 0;
                            for (var i = offset; i < growth.Length; i++)
                                growth[i] = fact[i - offset] - fact[i];
                        }
                    }

                    var yearRange = new DateRange(
                        new DateTime(range.To.Year, 1, 1),
                        new DateTime(range.To.Year, 12, 31));

                    var totalData = await fetchHandler.HandleAsync(
                        yearRange,
                        metric.Id,
                        PeriodTypeEnum.Month,
                        metric.PeriodType,
                        ct);

                    metricCalcCache[metric.Id] = new MetricCalcResult(
                        plan,
                        fact,
                        totalData.PlanData,
                        totalData.FactData,
                        growth
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

            foreach (var m in metrics)
            {
                if (!metricCalcCache.TryGetValue(m.Id, out var calc))
                    continue;

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
                    MetricType = ArchiveMetricTypeEnum.Employee
                });
            }

            result[e.Id] = dto;
        }

        return result;
    }

    public async ValueTask<Dictionary<int, List<UserPreviewMetricItemDto>>> HandleBulkItemsAsync(
        IEnumerable<int> userIds,
        DateRange range,
        PeriodTypeEnum periodType,
        CancellationToken ct = default)
    {
        // Reuse the existing bulk method to compute everything once
        var full = await HandleBulkAsync(userIds, range, periodType, ct);

        // Project to the lean structure expected by DepartmentPreviewHandler
        var result = new Dictionary<int, List<UserPreviewMetricItemDto>>(full.Count);
        foreach (var kv in full)
            result[kv.Key] = kv.Value.Metrics; // same list reference is fine (read-only downstream)

        return result;
    }

    private static DateRange BuildLast12Range(PeriodTypeEnum outer, PeriodTypeEnum metricPeriod, DateRange range)
    {
        // Logика идентична твоей, но вынесена в функцию для переиспользования
        return outer switch
        {
            PeriodTypeEnum.Day when metricPeriod == PeriodTypeEnum.Week =>
                new DateRange(
                    StartOfWeek(range.To.AddDays(-7 * 11), DayOfWeek.Monday),
                    StartOfWeek(range.To, DayOfWeek.Monday).AddDays(6)),

            PeriodTypeEnum.Day =>
                new DateRange(range.To.AddDays(-11), range.To),

            PeriodTypeEnum.Week =>
                new DateRange(
                    StartOfWeek(range.To.AddDays(-7 * 11), DayOfWeek.Monday),
                    StartOfWeek(range.To, DayOfWeek.Monday).AddDays(6)),

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

            PeriodTypeEnum.Custom when metricPeriod == PeriodTypeEnum.Week =>
                new DateRange(
                    StartOfWeek(range.From, DayOfWeek.Monday),
                    StartOfWeek(range.To, DayOfWeek.Monday).AddDays(6)),

            _ => range
        };
    }

    private static int ComparePeriods(PeriodTypeEnum a, PeriodTypeEnum b) => GetOrder(a).CompareTo(GetOrder(b));

    private static int GetOrder(PeriodTypeEnum p) => p switch
    {
        PeriodTypeEnum.Day or PeriodTypeEnum.Custom => 0,
        PeriodTypeEnum.Week => 1,
        PeriodTypeEnum.Month => 2,
        PeriodTypeEnum.Quartal => 3,
        PeriodTypeEnum.Year => 4,
        _ => 5
    };

    private static int[] ExpandTo(int[] data, DateTime rangeStart, DateTime rangeEnd, PeriodTypeEnum from, PeriodTypeEnum to)
    {
        if (to == PeriodTypeEnum.Custom)
        {
            var start = NormalizeStart(rangeStart, from);
            var offset = (int)(rangeStart.Date - start.Date).TotalDays;
            var endCount = CountPeriods(start, rangeEnd, to);
            var list = new List<int>(endCount);

            foreach (var value in data)
            {
                var next = AddPeriod(start, from, 1);
                var small = NormalizeStart(start, to);
                if (small < start)
                    small = AddPeriod(small, to, 1);

                for (; small < next && list.Count < endCount; small = AddPeriod(small, to, 1))
                    list.Add(value);

                if (list.Count >= endCount)
                    break;

                start = next;
            }

            if (list.Count < endCount)
            {
                var last = list.Count > 0 ? list[^1] : 0;
                while (list.Count < endCount)
                    list.Add(last);
            }

            if (offset > 0 && list.Count > offset)
                list.RemoveRange(0, Math.Min(offset, list.Count));

            var needed = CountPeriods(rangeStart, rangeEnd, to);
            if (list.Count > needed)
                list = list.Take(needed).ToList();

            return list.ToArray();
        }

        var endCountDefault = CountPeriods(rangeStart, rangeEnd, to);
        var listDefault = new List<int>(endCountDefault);
        var startNorm = NormalizeStart(rangeStart, from);

        foreach (var value in data)
        {
            var next = AddPeriod(startNorm, from, 1);
            var small = NormalizeStart(startNorm, to);
            if (small < startNorm)
                small = AddPeriod(small, to, 1);

            for (; small < next && listDefault.Count < endCountDefault; small = AddPeriod(small, to, 1))
                listDefault.Add(value);

            if (listDefault.Count >= endCountDefault)
                break;

            startNorm = next;
        }

        if (listDefault.Count < endCountDefault)
        {
            var last = listDefault.Count > 0 ? listDefault[^1] : 0;
            while (listDefault.Count < endCountDefault)
                listDefault.Add(last);
        }

        if (endCountDefault >= 12)
            return listDefault.Skip(endCountDefault - 12).Take(12).ToArray();

        var result = new int[12];
        listDefault.CopyTo(result, 12 - endCountDefault);
        return result;
    }

    private static DateTime AddPeriod(DateTime date, PeriodTypeEnum period, int amount) => period switch
    {
        PeriodTypeEnum.Day or PeriodTypeEnum.Custom => date.AddDays(amount),
        PeriodTypeEnum.Week => date.AddDays(7 * amount),
        PeriodTypeEnum.Month => date.AddMonths(amount),
        PeriodTypeEnum.Quartal => date.AddMonths(3 * amount),
        PeriodTypeEnum.Year => date.AddYears(amount),
        _ => date
    };

    private static int CountPeriods(DateTime from, DateTime to, PeriodTypeEnum period)
    {
        from = NormalizeStart(from, period);
        to = NormalizeStart(to, period);
        return period switch
        {
            PeriodTypeEnum.Day or PeriodTypeEnum.Custom => (int)(to - from).TotalDays + 1,
            PeriodTypeEnum.Week => (int)((to - from).TotalDays / 7) + 1,
            PeriodTypeEnum.Month => (to.Year - from.Year) * 12 + to.Month - from.Month + 1,
            PeriodTypeEnum.Quartal => ((to.Year - from.Year) * 12 + to.Month - from.Month) / 3 + 1,
            PeriodTypeEnum.Year => to.Year - from.Year + 1,
            _ => 1
        };
    }

    private static DateTime NormalizeStart(DateTime start, PeriodTypeEnum period) => period switch
    {
        PeriodTypeEnum.Week => StartOfWeek(start, DayOfWeek.Monday),
        PeriodTypeEnum.Month => new DateTime(start.Year, start.Month, 1),
        PeriodTypeEnum.Quartal => new DateTime(start.Year, ((start.Month - 1) / 3) * 3 + 1, 1),
        PeriodTypeEnum.Year => new DateTime(start.Year, 1, 1),
        _ => start.Date
    };

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
        double?[] Growth);
}