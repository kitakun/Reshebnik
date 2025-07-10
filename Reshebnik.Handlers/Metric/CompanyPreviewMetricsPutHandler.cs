using Reshebnik.Domain.Models.Metric;
using Reshebnik.Domain.Enums;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Clickhouse.Handlers;

namespace Reshebnik.Handlers.Metric;

public class CompanyPreviewMetricsPutHandler(
    CompanyContextHandler companyContext,
    FetchCompanyMetricsHandler putHandler)
{
    public async Task HandleAsync(
        PutCompanyPreviewMetricsDto request,
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;

        var item = request.Metrics;
        var maxLength = Math.Max(item.FactData.Length, item.PlanData.Length);
        var tasks = new List<Task>();

        for (var i = 0; i < maxLength; i++)
        {
            var date = AddOffset(request.From.Date, request.PeriodType, i);
            if (date > request.To.Date) break;

            var iCopy = i;
            tasks.Add(Task.Run(async () =>
            {
                if (iCopy < item.PlanData.Length)
                {
                    await putHandler.PutAsync(
                        request.Metrics.Id,
                        MetricValueTypeEnum.Plan,
                        companyId,
                        request.PeriodType,
                        date,
                        item.PlanData[iCopy],
                        ct);
                }

                if (iCopy < item.FactData.Length)
                {
                    await putHandler.PutAsync(
                        request.Metrics.Id,
                        MetricValueTypeEnum.Fact,
                        companyId,
                        request.PeriodType,
                        date,
                        item.FactData[iCopy],
                        ct);
                }
            }, ct));
        }

        await Task.WhenAll(tasks);
    }

    private static DateTime AddOffset(DateTime start, PeriodTypeEnum period, int index)
    {
        start = period switch
        {
            PeriodTypeEnum.Day => start.Date,
            PeriodTypeEnum.Week => StartOfWeek(start, DayOfWeek.Monday),
            PeriodTypeEnum.Month => new DateTime(start.Year, start.Month, 1),
            PeriodTypeEnum.Quartal => new DateTime(start.Year, ((start.Month - 1) / 3) * 3 + 1, 1),
            PeriodTypeEnum.Year => new DateTime(start.Year, 1, 1),
            _ => start
        };

        return period switch
        {
            PeriodTypeEnum.Day => start.AddDays(index),
            PeriodTypeEnum.Week => start.AddDays(7 * index),
            PeriodTypeEnum.Month => start.AddMonths(index),
            PeriodTypeEnum.Quartal => start.AddMonths(3 * index),
            PeriodTypeEnum.Year => start.AddYears(index),
            _ => start
        };
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-1 * diff);
    }
}