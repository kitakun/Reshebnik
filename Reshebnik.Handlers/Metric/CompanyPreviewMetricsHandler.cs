using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Clickhouse.Handlers;
using Reshebnik.Domain.Enums;

namespace Reshebnik.Handlers.Metric;

public class CompanyPreviewMetricsHandler(
    ReshebnikContext db,
    FetchCompanyMetricsHandler fetchHandler)
{
    public async ValueTask<CompanyPreviewMetricsDto> HandleAsync(
        int id,
        DateRange range,
        CancellationToken ct = default)
    {
        var indicator = await db.Indicators
            .AsNoTracking()
            .FirstAsync(i => i.Id == id, ct);

        var dto = new CompanyPreviewMetricsDto
        {
            CategoryName = indicator.Category,
            From = range.From,
            To = range.To,
            PeriodType = (FillmentPeriodWrapper)indicator.FillmentPeriod
        };

        var period = (PeriodTypeEnum)(FillmentPeriodWrapper)indicator.FillmentPeriod;

        var last12Range = period switch
        {
            PeriodTypeEnum.Day => new DateRange(range.To.AddDays(-11), range.To),
            PeriodTypeEnum.Week => new DateRange(range.From.AddDays(-5), range.To),
            PeriodTypeEnum.Quartal or PeriodTypeEnum.Year => new DateRange(new DateTime(range.To.Year, 1, 1), new DateTime(range.To.Year, 12, 31)),
            _ => range
        };

        var last12Data = await fetchHandler.HandleAsync(
            last12Range,
            indicator.Id,
            period == PeriodTypeEnum.Week ? PeriodTypeEnum.Day : period,
            (FillmentPeriodWrapper)indicator.FillmentPeriod,
            ct);

        var totalsRange = new DateRange(
            new DateTime(range.From.Year, 1, 1),
            new DateTime(range.To.Year, 12, 31));

        var totalData = await fetchHandler.HandleAsync(
            totalsRange,
            indicator.Id,
            PeriodTypeEnum.Month,
            (FillmentPeriodWrapper)indicator.FillmentPeriod,
            ct);

        dto.Metrics = new CompanyPreviewMetricItemDto
        {
            Id = indicator.Id,
            Name = indicator.Name,
            Last12PointsPlan = last12Data.PlanData,
            Last12PointsFact = last12Data.FactData,
            TotalPlanData = totalData.PlanData,
            TotalFactData = totalData.FactData
        };

        return dto;
    }
}