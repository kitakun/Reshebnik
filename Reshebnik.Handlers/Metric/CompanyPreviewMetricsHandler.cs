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
            PeriodTypeEnum.Week => new DateRange(StartOfWeek(range.To.AddDays(-7 * 11), DayOfWeek.Monday), StartOfWeek(range.To, DayOfWeek.Monday)),
            PeriodTypeEnum.Quartal => new DateRange(new DateTime(range.To.AddMonths(-3 * 11).Year, ((range.To.AddMonths(-3 * 11).Month - 1) / 3) * 3 + 1, 1),
                                                 new DateTime(range.To.Year, ((range.To.Month - 1) / 3) * 3 + 3, DateTime.DaysInMonth(range.To.Year, ((range.To.Month - 1) / 3) * 3 + 3))),
            PeriodTypeEnum.Year => new DateRange(new DateTime(range.To.Year - 11, 1, 1), new DateTime(range.To.Year, 12, 31)),
            _ => range
        };

        var last12Data = await fetchHandler.HandleAsync(
            last12Range,
            indicator.Id,
            period == PeriodTypeEnum.Week ? PeriodTypeEnum.Week : period,
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

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-1 * diff);
    }
}