using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Models.Indicator;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.KeyIndicator;

public class KeyIndicatorGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<List<KeyIndicatorCategoryDto>> HandleAsync(
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var indicators = await db.Indicators
            .AsNoTracking()
            .Where(i => i.CreatedBy == companyId && i.ShowOnKeyIndicators)
            .ToListAsync(ct);

        var result = indicators
            .GroupBy(i => i.Category)
            .Where(g => g.Any())
            .Select(g => new KeyIndicatorCategoryDto
            {
                CategoryName = g.Key,
                Metrics = g.Select(ind => new KeyIndicatorItemDto
                {
                    Id = ind.Id,
                    Name = ind.Name,
                    UnitType = ind.UnitType,
                    ValueType = ind.ValueType,
                    Metrics = new KeyIndicatorMetricsDto
                    {
                        Plan = new[] { 0, 0, 0 },
                        Fact = new[] { 0, 0, 0 },
                        Average = 100,
                        Period = ind.FillmentPeriod
                    }
                }).ToList()
            }).ToList();

        return result;
    }
}
