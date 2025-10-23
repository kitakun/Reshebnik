using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Enums;
using Tabligo.Domain.Models;
using Tabligo.Domain.Models.Dashboard;
using Tabligo.EntityFramework;
using Tabligo.Clickhouse.Handlers;
using Tabligo.Domain.Models.Metric;
using Tabligo.Handlers.Company;
using Tabligo.Handlers.Metric;
using Tabligo.Domain.Extensions;
using Tabligo.Handlers.Cache;

namespace Tabligo.Handlers.Dashboard;

public class DashboardGetHandler(
    TabligoContext db,
    CompanyContextHandler companyContext,
    FetchCompanyMetricsHandler companyMetricsHandler,
    UserPreviewMetricsHandler userMetricsHandler,
    ICacheService cache)
{
    public async ValueTask<DashboardDto> HandleAsync(
        DateRange range,
        PeriodTypeEnum periodType,
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var cacheKey = $"dashboard:{companyId}:{range.From:yyyyMMdd}:{range.To:yyyyMMdd}:{periodType}";
        var cached = await cache.GetAsync<DashboardDto>(cacheKey, ct);
        if (cached is not null)
            return cached;

        var dto = new DashboardDto();
        var indicatorAverages = new List<double>();

        // ---------------------------
        // 1) Индикаторы + BULK по метрикам компании (1 запрос к CH)
        // ---------------------------
        var indicators = await db.Indicators
            .AsNoTracking()
            .Where(i => i.CreatedBy == companyId && i.ShowOnKeyIndicators && !i.IsArchived)
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
                var groupRange = periodType == PeriodTypeEnum.Custom ? range : BuildRangeForPeriod(range.To, group.Key);
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
                else if (periodType != PeriodTypeEnum.Custom)
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
                        IsArchived = false,
                        MetricType = ArchiveMetricTypeEnum.Company
                    });
                }
            }

            dto.KeyIndicatorAverage = indicatorAverages.Count > 0
                ? Math.Round(indicatorAverages.Average(), 0, MidpointRounding.ToZero)
                : 0;
        }

        // ---------------------------
        // 2) Сотрудники + BULK превью (используем ТЕКУЩИЙ periodType)
        // ---------------------------
        var employees = await db.Employees
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .Select(e => new { e.Id, e.FIO, e.JobTitle, e.DefaultRole })
            .ToListAsync(ct);

        var employeeIds = employees.Select(e => e.Id).ToList();

        var previewsMap = employeeIds.Count == 0
            ? new Dictionary<int, UserPreviewMetricsDto>()
            : await userMetricsHandler.HandleBulkAsync(employeeIds, range, periodType, ct); // was Month

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

                // user.CompletionPercent from DepartmentPreviewHandler
                employeeAverages[e.Id] = count > 0
                    ? Math.Round(sum / count, 0, MidpointRounding.ToZero)
                    : 0;
            }
        }

        // одна проекция → два отбора (best/worst) — как было
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

        var bestEmployees = employeeDtos.OrderByDescending(x => x.Average).Take(3).ToList();
        var worstEmployees = employeeDtos.Where(e => bestEmployees.All(b => b.Id != e.Id))
            .OrderBy(x => x.Average).Take(3).ToList();

        dto.BestEmployees = bestEmployees;
        dto.WorstEmployees = worstEmployees;

        // ---------------------------
        // 3) Департаменты: как в DepartmentPreviewHandler
        //    (child depth=1; per-dept avg -> root avg по детям)
        // ---------------------------
        // корни
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

        // direct children (Depth == 1)
        var directChildren = await db.DepartmentSchemas.AsNoTracking()
            .Where(s => rootIds.Contains(s.AncestorDepartmentId) && s.Depth == 1)
            .Select(s => new { ParentId = s.AncestorDepartmentId, ChildId = s.DepartmentId })
            .ToListAsync(ct);

        var childrenByRoot = directChildren
            .GroupBy(x => x.ParentId)
            .ToDictionary(g => g.Key, g => g.Select(v => v.ChildId).ToList());

        // нам нужны линк-типы для логики "если есть руководители → берём всех, иначе только сотрудников"
        var deptIdsForLinks = new HashSet<int>(rootIds);
        foreach (var list in childrenByRoot.Values)
        {
            foreach (var cid in list)
            {
                deptIdsForLinks.Add(cid);
            }
        }

        var links = await db.EmployeeDepartmentLinks.AsNoTracking()
            .Where(l => deptIdsForLinks.Contains(l.DepartmentId))
            .Select(l => new { l.DepartmentId, l.EmployeeId, l.Type })
            .ToListAsync(ct);

        var linksByDept = links.GroupBy(l => l.DepartmentId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var leafRootIds = rootDeptPairs
            .Where(r => !childrenByRoot.TryGetValue(r.RootId, out var ch) || ch.Count == 0)
            .Select(r => r.RootId)
            .ToList();

        // all userIds linked to leaf roots
        var leafUserIds = links
            .Where(l => leafRootIds.Contains(l.DepartmentId))
            .Select(l => l.EmployeeId)
            .Distinct()
            .ToList();

        // we already loaded active employees into employeeAverages;
        // bring in the missing ones (e.g., not in the active list) just for dept averages
        var missingUserIds = leafUserIds.Where(uid => !employeeAverages.ContainsKey(uid)).ToList();

        if (missingUserIds.Count > 0)
        {
            var extraPreviews = await userMetricsHandler.HandleBulkAsync(missingUserIds, range, periodType, ct);
            foreach (var kv in extraPreviews)
            {
                var preview = kv.Value;
                if (preview is { Metrics.Count: > 0 })
                {
                    double sum = 0;
                    int count = 0;
                    foreach (var m in preview.Metrics)
                    {
                        sum += m.GetCompletionPercent();
                        count++;
                    }

                    employeeAverages[kv.Key] = count > 0
                        ? Math.Round(sum / count, 0, MidpointRounding.ToZero)
                        : 0;
                }
                else
                {
                    employeeAverages[kv.Key] = 0;
                }
            }
        }

        // локальная функция: среднее по департаменту как в DepartmentPreviewHandler
        double DeptAvg(int deptId)
        {
            if (!linksByDept.TryGetValue(deptId, out var lnk) || lnk.Count == 0)
                return 0;

            // ALWAYS include supervisors + employees
            var userIds = lnk.Select(x => x.EmployeeId).Distinct();

            var vals = userIds
                .Select(uid => employeeAverages.GetValueOrDefault(uid, 0d))
                .ToList();

            if (vals.Count == 0) return 0;

            // mirror DepartmentPreviewHandler: round per-department
            return Math.Round(vals.Average(), 0, MidpointRounding.ToZero);
        }

        foreach (var root in rootDeptPairs)
        {
            var childIds = childrenByRoot.GetValueOrDefault(root.RootId, []);

            double avg;
            if (childIds.Count > 0)
            {
                // root = среднее по дочерним департаментам (равный вес детям)
                var childAverages = childIds.Select(DeptAvg).ToList();
                avg = childAverages.Count > 0
                    ? Math.Round(childAverages.Average(), 0, MidpointRounding.ToZero)
                    : 0;
            }
            else
            {
                // fallback: если нет детей — считаем по самому root’у
                avg = DeptAvg(root.RootId);
            }

            dto.Departments.Add(new DashboardDepartmentDto
            {
                Id = root.RootId,
                Name = root.RootName,
                Average = avg
            });
        }

        // Calculate DepartmentsAverage as the average of all department averages
        dto.DepartmentsAverage = dto.Departments.Count > 0
            ? Math.Round(dto.Departments.Average(d => d.Average), 0, MidpointRounding.ToZero)
            : 0;

        await cache.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), ct);
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
        if (to == PeriodTypeEnum.Custom)
        {
            var start = NormalizeStart(rangeStart, from);
            var offset = (int)(rangeStart.Date - start.Date).TotalDays;
            var list = new List<int>();

            foreach (var value in data)
            {
                var next = AddPeriod(start, from, 1);
                var small = NormalizeStart(start, to);
                if (small < start)
                    small = AddPeriod(small, to, 1);

                for (; small < next; small = AddPeriod(small, to, 1))
                    list.Add(value);

                start = next;
            }

            if (offset > 0 && list.Count > offset)
                list.RemoveRange(0, Math.Min(offset, list.Count));

            var needed = CountPeriods(rangeStart, rangeEnd, to);
            if (list.Count > needed)
                list = list.Take(needed).ToList();
            else if (list.Count < needed)
            {
                var last = list.Count > 0 ? list[^1] : 0;
                while (list.Count < needed)
                    list.Add(last);
            }

            return list.ToArray();
        }

        var listDefault = new List<int>();
        var startNorm = NormalizeStart(rangeStart, from);
        foreach (var value in data)
        {
            var next = AddPeriod(startNorm, from, 1);
            var small = NormalizeStart(startNorm, to);
            if (small < startNorm)
                small = AddPeriod(small, to, 1);
            for (; small < next; small = AddPeriod(small, to, 1))
            {
                listDefault.Add(value);
            }

            startNorm = next;
        }

        var endCount = Math.Min(listDefault.Count, CountPeriods(rangeStart, rangeEnd, to));
        if (endCount >= 12)
            return listDefault.Skip(endCount - 12).Take(12).ToArray();

        var result = new int[12];
        listDefault.Take(endCount).ToArray().CopyTo(result, 12 - endCount);
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