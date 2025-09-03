using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Dashboard;
using Reshebnik.EntityFramework;
using Reshebnik.Clickhouse.Handlers;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.Handlers.Company;
using Reshebnik.Handlers.Metric;
using Reshebnik.Domain.Extensions;

namespace Reshebnik.Handlers.Dashboard;

public class DashboardGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    FetchCompanyMetricsHandler companyMetricsHandler,
    UserPreviewMetricsHandler userMetricsHandler)
{
    public async ValueTask<DashboardDto> HandleAsync(
        DateRange range,
        PeriodTypeEnum periodType,
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var dto = new DashboardDto();
        var indicatorAverages = new List<double>();

        // ---------------------------
        // 1) Индикаторы + BULK по метрикам компании (1 запрос к CH)
        // ---------------------------
        var indicators = await db.Indicators
            .AsNoTracking()
            .Where(i => i.CreatedBy == companyId && i.ShowOnKeyIndicators)
            .Select(i => new { i.Id, i.Name, i.FillmentPeriod, i.ShowOnMainScreen })
            .ToListAsync(ct);

        if (indicators.Count > 0)
        {
            var metricInfos = indicators
                .Select(ind =>
                {
                    var source = (PeriodTypeEnum)(FillmentPeriodWrapper)ind.FillmentPeriod;
                    var expected = ComparePeriods(source, periodType) > 0 ? source : periodType;
                    var request = new FetchCompanyMetricsHandler.MetricRequest(
                        ind.Id,
                        expected,
                        source);
                    return new { Indicator = ind, Request = request };
                })
                .ToList();

            var bulkMetrics = new Dictionary<int, FetchCompanyMetricsHandler.MetricsDataResponse>();
            var rangeByPeriod = new Dictionary<PeriodTypeEnum, DateRange>();

            foreach (var group in metricInfos.GroupBy(x => x.Request.ExpectedValues))
            {
                var groupRange = BuildRangeForPeriod(range.To, group.Key);
                rangeByPeriod[group.Key] = groupRange;
                var groupMetrics = await companyMetricsHandler.HandleBulkAsync(
                    groupRange,
                    group.Select(x => x.Request),
                    ct);
                foreach (var (metricId, data) in groupMetrics)
                {
                    bulkMetrics[metricId] = data;
                }
            }

            foreach (var info in metricInfos)
            {
                var ind = info.Indicator;
                if (!bulkMetrics.TryGetValue(ind.Id, out var data))
                {
                    data = new FetchCompanyMetricsHandler.MetricsDataResponse(new int[12], new int[12]);
                }

                var plan = data.PlanData;
                var fact = data.FactData;

                if (ComparePeriods(info.Request.SourcePeriod, periodType) > 0)
                {
                    var groupRange = rangeByPeriod[info.Request.ExpectedValues];
                    plan = ExpandTo(plan, groupRange.From, range.To, info.Request.ExpectedValues, periodType);
                    fact = ExpandTo(fact, groupRange.From, range.To, info.Request.ExpectedValues, periodType);
                }
                else
                {
                    if (plan.Length != 12) Array.Resize(ref plan, 12);
                    if (fact.Length != 12) Array.Resize(ref fact, 12);
                }

                var factAvg = fact.Length > 0 ? fact.Average() : 0d;
                var planAvg = plan.Length > 0 ? plan.Average() : 0d;
                var avgPercent = planAvg != 0 ? (factAvg / planAvg) * 100 : 0d;
                indicatorAverages.Add(avgPercent);

                if (ind.ShowOnMainScreen)
                {
                    dto.Metrics.Add(new DashboardMetricDto
                    {
                        Id = ind.Id,
                        Name = ind.Name,
                        Plan = plan,
                        Fact = fact,
                        PeriodType = periodType,
                        IsArchived = false
                    });
                }
            }

            dto.DepartmentsAverage = indicatorAverages.Count > 0
                ? Math.Round(indicatorAverages.Average(), 0, MidpointRounding.ToZero)
                : 0;
        }

        // ---------------------------
        // 2) Сотрудники + BULK превью (0 N+1, контролируемая агрегация)
        // ---------------------------
        var employees = await db.Employees
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .Select(e => new { e.Id, e.FIO, e.JobTitle, e.DefaultRole })
            .ToListAsync(ct);

        var employeeIds = employees.Select(e => e.Id).ToList();

        var previewsMap = employeeIds.Count == 0
            ? new Dictionary<int, UserPreviewMetricsDto>()
            : await userMetricsHandler.HandleBulkAsync(employeeIds, range, PeriodTypeEnum.Month, ct);

        var employeeAverages = new Dictionary<int, double>(employees.Count);
        foreach (var e in employees)
        {
            if (previewsMap.TryGetValue(e.Id, out var preview) && preview is { Metrics.Count: > 0 })
            {
                double sum = 0;
                int count = 0;
                foreach (var metric in preview.Metrics)
                {
                    sum += metric.GetCompletionPercent();
                    count++;
                }
                employeeAverages[e.Id] = count > 0
                    ? Math.Round(sum / count, 0, MidpointRounding.ToZero)
                    : 0;
            }
        }

        // одна проекция → два отбора (best/worst)
        var employeeDtos = employees.Select(e =>
        {
            var avg = employeeAverages.GetValueOrDefault(e.Id, 0d);
            return new DashboardEmployeeDto
            {
                Id = e.Id,
                Fio = e.FIO,
                JobTitle = e.JobTitle,
                IsSupervisor = e.DefaultRole == EmployeeTypeEnum.Supervisor,
                Average = Math.Round(avg, 0, MidpointRounding.ToZero)
            };
        });

        dto.BestEmployees = employeeDtos.OrderByDescending(x => x.Average).Take(3).ToList();
        dto.WorstEmployees = employeeDtos.OrderBy(x => x.Average).Take(3).ToList();

        // ---------------------------
        // 3) Департаменты: минимизируем запросы и Contains на List
        //    (один join для корней, HashSet для связей)
        // ---------------------------
        var rootDeptPairs = await (
            from s in db.DepartmentSchemas.AsNoTracking()
            join d in db.Departments.AsNoTracking() on s.DepartmentId equals d.Id
            where s.FundamentalDepartmentId == s.DepartmentId
               && s.Depth == 0
               && d.CompanyId == companyId
               && !d.IsDeleted
            select new { RootId = d.Id, RootName = d.Name }
        ).Distinct().ToListAsync(ct);

        var rootIds = rootDeptPairs.Select(x => x.RootId).ToHashSet();

        var schemas = await db.DepartmentSchemas.AsNoTracking()
            .Where(s => rootIds.Contains(s.FundamentalDepartmentId))
            .Select(s => new { s.FundamentalDepartmentId, s.DepartmentId })
            .ToListAsync(ct);

        var allDeptIds = schemas.Select(s => s.DepartmentId).ToHashSet();

        var links = await db.EmployeeDepartmentLinks.AsNoTracking()
            .Where(l => allDeptIds.Contains(l.DepartmentId))
            .Select(l => new { l.DepartmentId, l.EmployeeId })
            .ToListAsync(ct);

        // root -> set(deptIds)
        var rootToDeptIds = schemas
            .GroupBy(s => s.FundamentalDepartmentId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DepartmentId).ToHashSet());

        foreach (var root in rootDeptPairs)
        {
            if (!rootToDeptIds.TryGetValue(root.RootId, out var deptIdsForRoot) || deptIdsForRoot.Count == 0)
            {
                dto.Departments.Add(new DashboardDepartmentDto { Id = root.RootId, Name = root.RootName, Average = 0 });
                continue;
            }

            // берем avg сотрудников, привязанных к отделам root-а
            var employeeIdsInRoot = links
                .Where(l => deptIdsForRoot.Contains(l.DepartmentId))
                .Select(l => l.EmployeeId)
                .Distinct();

            var values = employeeIdsInRoot
                .Select(id => employeeAverages.TryGetValue(id, out var avg) ? avg : 0d)
                .ToList();

            var avgDept = values.Count > 0 ? values.Average() : 0d;

            dto.Departments.Add(new DashboardDepartmentDto
            {
                Id = root.RootId,
                Name = root.RootName,
                Average = Math.Round(avgDept, 0, MidpointRounding.ToZero)
            });
        }

        return dto;
    }

    private static DateRange BuildRangeForPeriod(DateTime to, PeriodTypeEnum period)
    {
        return period switch
        {
            PeriodTypeEnum.Day or PeriodTypeEnum.Custom =>
                new DateRange(to.Date.AddDays(-11), to.Date),
            PeriodTypeEnum.Week =>
                ToWeekRange(to),
            PeriodTypeEnum.Month =>
                ToMonthRange(to),
            PeriodTypeEnum.Quartal =>
                ToQuartalRange(to),
            PeriodTypeEnum.Year =>
                ToYearRange(to),
            _ => new DateRange(to.Date.AddDays(-11), to.Date)
        };
    }

    private static DateRange ToWeekRange(DateTime to)
    {
        var end = StartOfWeek(to, DayOfWeek.Monday);
        return new DateRange(end.AddDays(-7 * 11), end);
    }

    private static DateRange ToMonthRange(DateTime to)
    {
        var end = new DateTime(to.Year, to.Month, DateTime.DaysInMonth(to.Year, to.Month));
        var start = new DateTime(end.AddMonths(-11).Year, end.AddMonths(-11).Month, 1);
        return new DateRange(start, end);
    }

    private static DateRange ToQuartalRange(DateTime to)
    {
        var endMonth = ((to.Month - 1) / 3) * 3 + 3;
        var end = new DateTime(to.Year, endMonth, DateTime.DaysInMonth(to.Year, endMonth));
        var startMonth = ((end.AddMonths(-3 * 11).Month - 1) / 3) * 3 + 1;
        var start = new DateTime(end.AddMonths(-3 * 11).Year, startMonth, 1);
        return new DateRange(start, end);
    }

    private static DateRange ToYearRange(DateTime to)
    {
        var end = new DateTime(to.Year, 12, 31);
        var start = new DateTime(end.Year - 11, 1, 1);
        return new DateRange(start, end);
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
        var list = new List<int>();
        var start = NormalizeStart(rangeStart, from);
        foreach (var value in data)
        {
            var next = AddPeriod(start, from, 1);
            var small = NormalizeStart(start, to);
            if (small < start)
                small = AddPeriod(small, to, 1);
            for (; small < next; small = AddPeriod(small, to, 1))
            {
                list.Add(value);
            }
            start = next;
        }

        var endCount = Math.Min(list.Count, CountPeriods(rangeStart, rangeEnd, to));
        if (endCount >= 12)
            return list.Skip(endCount - 12).Take(12).ToArray();

        var result = new int[12];
        list.Take(endCount).ToArray().CopyTo(result, 12 - endCount);
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
}

