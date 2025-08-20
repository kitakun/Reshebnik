using Microsoft.EntityFrameworkCore;

using Reshebnik.Clickhouse.Handlers;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using System.Linq;

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

        var metric = archived.Metric;

        var last = archived.LastDate;
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

        int[] plan;
        int[] fact;
        if (archived.MetricType == ArchiveMetricTypeEnum.Employee)
        {
            var data = await userMetricsHandler.HandleAsync(
                last12Range,
                metric.Id,
                metric.PeriodType,
                metric.PeriodType,
                ct);
            plan = data.PlanData;
            fact = data.FactData;
        }
        else
        {
            var data = await companyMetricsHandler.HandleAsync(
                last12Range,
                metric.Id,
                metric.PeriodType,
                metric.PeriodType,
                ct);
            plan = data.PlanData;
            fact = data.FactData;
        }

        return new ArchivedMetricGetDto
        {
            Id = archived.Id,
            MetricId = archived.MetricId,
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
                ShowGrowthPercent = metric.ShowGrowthPercent
            },
            Last12PointsPlan = plan,
            Last12PointsFact = fact
        };
    }

    private static DateTime StartOfWeek(DateTime date, DayOfWeek startOfWeek)
    {
        int diff = (7 + (date.DayOfWeek - startOfWeek)) % 7;
        return date.Date.AddDays(-1 * diff);
    }
}

