using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Indicator;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Clickhouse.Handlers;
using System.Linq;
using System.Collections.Generic;

namespace Reshebnik.Handlers.IndicatorCategory;

public class IndicatorCategoryGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    FetchCompanyMetricsHandler fetchHandler)
{
    public async ValueTask<IndicatorCategoryViewDto?> HandleAsync(
        string categoryName,
        DateRange range,
        PeriodTypeEnum periodType,
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var indicators = await db.Indicators
            .AsNoTracking()
            .Where(i => i.CreatedBy == companyId && i.Category == categoryName)
            .ToListAsync(ct);
        if (indicators.Count == 0) return null;

        var catRecord = await db.CategoryRecords
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.CompanyId == companyId && c.Name == categoryName, ct);

        var dto = new IndicatorCategoryViewDto
        {
            CategoryName = categoryName,
            Comment = catRecord?.Comment ?? string.Empty
        };

        foreach (var ind in indicators)
        {
            var metricPeriod = (PeriodTypeEnum)(FillmentPeriodWrapper)ind.FillmentPeriod;
            var needExpand = ComparePeriods(metricPeriod, periodType) > 0;
            var expected = needExpand ? metricPeriod : periodType;
            var last12Range = periodType == PeriodTypeEnum.Custom ? range : BuildRangeForPeriod(range.To, expected);

            var data = await fetchHandler.HandleAsync(
                last12Range,
                ind.Id,
                expected,
                (FillmentPeriodWrapper)ind.FillmentPeriod,
                ct);

            var plan = data.PlanData;
            var fact = data.FactData;

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

            var planSum = plan.Sum();
            var factSum = fact.Sum();
            var avg = planSum != 0 ? factSum / (double)planSum * 100 : 0;

            dto.Metrics.Add(new IndicatorCategoryMetricDto
            {
                Id = ind.Id,
                Name = ind.Name,
                UnitType = ind.UnitType,
                ValueType = ind.ValueType,
                HasEmployees = ind.EmployeeId != null || ind.DepartmentId != null,
                IsArchived = false,
                Metrics = new KeyIndicatorMetricsDto
                {
                    Plan = plan,
                    Fact = fact,
                    Average = Math.Round(avg, 0, MidpointRounding.ToZero),
                    Period = ind.FillmentPeriod
                }
            });
        }

        return dto;
    }

    private static DateRange BuildRangeForPeriod(DateTime to, PeriodTypeEnum period) => period switch
    {
        PeriodTypeEnum.Day or PeriodTypeEnum.Custom => new DateRange(to.Date.AddDays(-11), to.Date),
        PeriodTypeEnum.Week => ToWeekRange(to),
        PeriodTypeEnum.Month => ToMonthRange(to),
        PeriodTypeEnum.Quartal => ToQuartalRange(to),
        PeriodTypeEnum.Year => ToYearRange(to),
        _ => new DateRange(to.Date.AddDays(-11), to.Date)
    };

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
}
