using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Indicator;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Clickhouse.Handlers;

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
            var last12Range = periodType switch
            {
                PeriodTypeEnum.Day => new DateRange(range.To.AddDays(-11), range.To),
                PeriodTypeEnum.Week => new DateRange(range.From.AddDays(-5), range.To),
                PeriodTypeEnum.Quartal or PeriodTypeEnum.Year => new DateRange(new DateTime(range.To.Year, 1, 1), new DateTime(range.To.Year, 12, 31)),
                _ => range
            };

            var data = await fetchHandler.HandleAsync(
                last12Range,
                ind.Id,
                periodType == PeriodTypeEnum.Week ? PeriodTypeEnum.Day : periodType,
                (FillmentPeriodWrapper)ind.FillmentPeriod,
                ct);

            var planSum = data.PlanData.Sum();
            var factSum = data.FactData.Sum();
            var avg = planSum != 0 ? factSum / (double)planSum * 100 : 0;

            dto.Metrics.Add(new IndicatorCategoryMetricDto
            {
                Id = ind.Id,
                Name = ind.Name,
                UnitType = ind.UnitType,
                ValueType = ind.ValueType,
                HasEmployees = ind.EmployeeId != null || ind.DepartmentId != null,
                Metrics = new KeyIndicatorMetricsDto
                {
                    Plan = data.PlanData,
                    Fact = data.FactData,
                    Average = avg,
                    Period = ind.FillmentPeriod
                }
            });
        }

        return dto;
    }
}
