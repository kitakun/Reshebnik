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
            PeriodType = (FillmentPeriodWrapper) indicator.FillmentPeriod
        };

        var data = await fetchHandler.HandleAsync(
            range,
            indicator.Id,
            (FillmentPeriodWrapper) indicator.FillmentPeriod,
            (FillmentPeriodWrapper) indicator.FillmentPeriod,
            ct);

        dto.Metrics = new CompanyPreviewMetricItemDto
        {
            Id = indicator.Id,
            Name = indicator.Name,
            PlanData = data.PlanData,
            FactData = data.FactData
        };

        /*
        var groups = indicators.GroupBy(i => i.Category);
        var result = new List<CompanyPreviewMetricsDto>();
        foreach (var group in groups)
        {
            var dto = new CompanyPreviewMetricsDto
            {
                CategoryName = group.Key,
                From = range.From,
                To = range.To,
                PeriodType = (FillmentPeriodWrapper) group.First().FillmentPeriod
            };

            foreach (var ind in group)
            {
                var data = await fetchHandler.HandleAsync(
                    range,
                    ind.Id,
                    (FillmentPeriodWrapper) group.First().FillmentPeriod,
                    (FillmentPeriodWrapper) group.First().FillmentPeriod,
                    ct);

                dto.Metrics.Add(new CompanyPreviewMetricItemDto
                {
                    Id = ind.Id,
                    Name = ind.Name,
                    Period = (FillmentPeriodWrapper) ind.FillmentPeriod,
                    ValueType = ind.ValueType,
                    Type = ind.UnitType,
                    Data = data
                });
            }

            result.Add(dto);
        }
        */
        return dto;
    }
}