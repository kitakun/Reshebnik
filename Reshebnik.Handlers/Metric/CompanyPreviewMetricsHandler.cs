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

        var data = await fetchHandler.HandleAsync(
            range,
            indicator.Id,
            (FillmentPeriodWrapper)indicator.FillmentPeriod,
            (FillmentPeriodWrapper)indicator.FillmentPeriod,
            ct);

        dto.Metrics = new CompanyPreviewMetricItemDto
        {
            Id = indicator.Id,
            Name = indicator.Name,
            PlanData = data.PlanData,
            FactData = data.FactData,
            TotalPlanData = data.TotalPlanData,
            TotalFactData = data.TotalFactData,
            Last12PointsPlan = data.Last12PointsPlan,
            Last12PointsFact = data.Last12PointsFact
        };

        return dto;
    }
}