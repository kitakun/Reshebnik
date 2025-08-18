using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Models.Indicator;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Clickhouse.Handlers;
using System.Linq;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.KeyIndicator;

public class KeyIndicatorGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    FetchCompanyMetricsHandler fetchHandler)
{
    public async ValueTask<List<KeyIndicatorCategoryDto>> HandleAsync(
        DateTime from,
        DateTime to,
        PeriodTypeEnum type,
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var indicators = await db.Indicators
            .AsNoTracking()
            .Where(i => i.CreatedBy == companyId && i.ShowOnKeyIndicators)
            .ToListAsync(ct);

        var range = new DateRange(from, to);
        var last12Range = type switch
        {
            PeriodTypeEnum.Day => new DateRange(to.AddDays(-11), to),
            PeriodTypeEnum.Week => new DateRange(StartOfWeek(to.AddDays(-7 * 11), DayOfWeek.Monday), StartOfWeek(to, DayOfWeek.Monday)),
            PeriodTypeEnum.Month => new DateRange(
                new DateTime(to.AddMonths(-11).Year, to.AddMonths(-11).Month, 1),
                new DateTime(to.Year, to.Month, DateTime.DaysInMonth(to.Year, to.Month))),
            PeriodTypeEnum.Quartal => new DateRange(
                new DateTime(to.AddMonths(-3 * 11).Year, ((to.AddMonths(-3 * 11).Month - 1) / 3) * 3 + 1, 1),
                new DateTime(to.Year, ((to.Month - 1) / 3) * 3 + 3, DateTime.DaysInMonth(to.Year, ((to.Month - 1) / 3) * 3 + 3))),
            PeriodTypeEnum.Year => new DateRange(new DateTime(to.Year - 11, 1, 1), new DateTime(to.Year, 12, 31)),
            _ => range
        };

        var result = new List<KeyIndicatorCategoryDto>();
        foreach (var group in indicators.GroupBy(i => i.Category).Where(g => g.Any()))
        {
            var categoryDto = new KeyIndicatorCategoryDto
            {
                CategoryName = group.Key,
                Metrics = new List<KeyIndicatorItemDto>()
            };

            foreach (var ind in group)
            {
                var data = await fetchHandler.HandleAsync(
                    last12Range,
                    ind.Id,
                    type,
                    (FillmentPeriodWrapper)ind.FillmentPeriod,
                    ct);

                var planSum = data.PlanData.Sum();
                var factSum = data.FactData.Sum();
                var avg = planSum != 0 ? factSum / (double)planSum * 100 : 0;

                categoryDto.Metrics.Add(new KeyIndicatorItemDto
                {
                    Id = ind.Id,
                    Name = ind.Name,
                    UnitType = ind.UnitType,
                    ValueType = ind.ValueType,
                    IsArchived = false,
                    Metrics = new KeyIndicatorMetricsDto
                    {
                        Plan = data.PlanData,
                        Fact = data.FactData,
                        Average = Math.Round(avg, 0, MidpointRounding.ToZero),
                        Period = ind.FillmentPeriod
                    }
                });
            }

            result.Add(categoryDto);
        }

        return result;
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-1 * diff);
    }
}
