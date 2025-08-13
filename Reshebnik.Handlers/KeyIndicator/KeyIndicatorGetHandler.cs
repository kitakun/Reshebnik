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
                    range,
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
}
