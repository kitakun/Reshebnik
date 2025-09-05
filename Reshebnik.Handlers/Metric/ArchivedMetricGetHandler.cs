using Microsoft.EntityFrameworkCore;

using Reshebnik.Clickhouse.Handlers;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Indicator;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Metric;

public class ArchivedMetricGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    FetchCompanyMetricsHandler companyMetricsHandler,
    FetchUserMetricsHandler userMetricsHandler)
{
    public async ValueTask<ArchivedMetricGetDto?> HandleAsync(int id, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;

        var archived = await db.ArchivedMetrics
            .AsNoTracking()
            .Where(a => a.CompanyId == companyId && a.Id == id)
            .Include(a => a.Metric)
            .ThenInclude(m => m.DepartmentLinks)
            .Include(a => a.Metric)
            .ThenInclude(m => m.EmployeeLinks)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(ct);

        if (archived == null)
            return null;

        var last = archived.LastDate;

        int[] plan;
        int[] fact;

        switch (archived.MetricType)
        {
            case ArchiveMetricTypeEnum.Employee:
                var metric = archived.Metric!;

                var last12Range = metric.PeriodType switch
                {
                    PeriodTypeEnum.Day => new DateRange(last.AddDays(-11), last),
                    PeriodTypeEnum.Week => new DateRange(StartOfWeek(last.AddDays(-7 * 11), DayOfWeek.Monday), StartOfWeek(last, DayOfWeek.Monday)),
                    PeriodTypeEnum.Month => new DateRange(new DateTime(last.AddMonths(-11).Year, last.AddMonths(-11).Month, 1), new DateTime(last.Year, last.Month, DateTime.DaysInMonth(last.Year, last.Month))),
                    PeriodTypeEnum.Quartal => new DateRange(new DateTime(last.AddMonths(-3 * 11).Year, ((last.AddMonths(-3 * 11).Month - 1) / 3) * 3 + 1, 1), new DateTime(last.Year, ((last.Month - 1) / 3) * 3 + 3, DateTime.DaysInMonth(last.Year, ((last.Month - 1) / 3) * 3 + 3))),
                    PeriodTypeEnum.Year => new DateRange(new DateTime(last.Year - 11, 1, 1), new DateTime(last.Year, 12, 31)),
                    PeriodTypeEnum.Custom => new DateRange(last.AddDays(-11), last),
                    _ => new DateRange(last.AddDays(-11), last)
                };

                var dataEmployee = await userMetricsHandler.HandleAsync(
                    last12Range,
                    metric.Id,
                    metric.PeriodType,
                    metric.PeriodType,
                    ct);
                plan = dataEmployee.PlanData;
                fact = dataEmployee.FactData;

                return new ArchivedMetricGetDto
                {
                    Id = archived.Id,
                    MetricId = archived.MetricId!.Value,
                    MetricType = archived.MetricType,
                    FirstDate = archived.FirstDate,
                    LastDate = archived.LastDate,
                    ArchivedAt = archived.ArchivedAt,
                    ArchivedByUserId = archived.ArchivedByUserId,
                    Metric = new MetricDto
                    {
                        Id = metric.Id,
                        Name = metric.Name,
                        Description = metric.Description,
                        Unit = metric.Unit,
                        Type = metric.Type,
                        PeriodType = metric.PeriodType,
                        DepartmentIds = metric.DepartmentLinks.Select(l => l.DepartmentId).ToArray(),
                        EmployeeIds = metric.EmployeeLinks.Select(l => l.EmployeeId).ToArray(),
                        Plan = metric.Plan,
                        Min = metric.Min,
                        Max = metric.Max,
                        Visible = metric.Visible,
                        IsArchived = metric.IsArchived,
                        WeekType = metric.WeekType,
                        WeekStartDate = metric.WeekStartDate.HasValue
                            ? DateTime.UtcNow.Date.AddDays(-metric.WeekStartDate.Value)
                            : null,
                        ShowGrowthPercent = metric.ShowGrowthPercent,
                        MetricType = ArchiveMetricTypeEnum.Employee
                    },
                    Last12PointsPlan = plan,
                    Last12PointsFact = fact
                };
            case ArchiveMetricTypeEnum.Company:
                var indicatorMetric = archived.Indicator!;

                var last12RangeCompany = indicatorMetric.FillmentPeriod switch
                {
                    FillmentPeriodEnum.Daily => new DateRange(last.AddDays(-11), last),
                    FillmentPeriodEnum.Weekly => new DateRange(StartOfWeek(last.AddDays(-7 * 11), DayOfWeek.Monday), StartOfWeek(last, DayOfWeek.Monday)),
                    FillmentPeriodEnum.Monthly => new DateRange(new DateTime(last.AddMonths(-11).Year, last.AddMonths(-11).Month, 1), new DateTime(last.Year, last.Month, DateTime.DaysInMonth(last.Year, last.Month))),
                    _ => new DateRange(last.AddDays(-11), last)
                };

                var dataCompany = await companyMetricsHandler.HandleAsync(
                    last12RangeCompany,
                    indicatorMetric.Id,
                    (FillmentPeriodWrapper)indicatorMetric.FillmentPeriod,
                    (FillmentPeriodWrapper)indicatorMetric.FillmentPeriod,
                    ct);
                plan = dataCompany.PlanData;
                fact = dataCompany.FactData;

                return new ArchivedMetricGetDto
                {
                    Id = archived.Id,
                    MetricId = archived.MetricId!.Value,
                    MetricType = archived.MetricType,
                    FirstDate = archived.FirstDate,
                    LastDate = archived.LastDate,
                    ArchivedAt = archived.ArchivedAt,
                    ArchivedByUserId = archived.ArchivedByUserId,
                    Indicator = new IndicatorDto
                    {
                        Id = indicatorMetric.Id,
                        Name = indicatorMetric.Name,
                        Description = indicatorMetric.Description,
                        UnitType = indicatorMetric.UnitType,
                        ValueType = indicatorMetric.ValueType,
                        FillmentPeriod = indicatorMetric.FillmentPeriod,
                        Category = indicatorMetric.Category,
                        CreatedAt = indicatorMetric.CreatedAt,
                        RejectionTreshold = indicatorMetric.RejectionTreshold,
                        ShowOnMainScreen = indicatorMetric.ShowOnMainScreen,
                        ShowOnKeyIndicators = indicatorMetric.ShowOnKeyIndicators,
                        Plan = indicatorMetric.Plan,
                        Min = indicatorMetric.Min,
                        Max = indicatorMetric.Max,
                        ShowToEmployees = indicatorMetric.ShowToEmployees,
                    },
                    Last12PointsPlan = plan,
                    Last12PointsFact = fact
                };
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-1 * diff);
    }
}