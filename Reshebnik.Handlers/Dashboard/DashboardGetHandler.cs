using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Dashboard;
using Reshebnik.EntityFramework;
using Reshebnik.Clickhouse.Handlers;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.Handlers.Company;
using Reshebnik.Handlers.Metric;

namespace Reshebnik.Handlers.Dashboard;

public class DashboardGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext,
    FetchCompanyMetricsHandler companyMetricsHandler,
    UserPreviewMetricsHandler userMetricsHandler)
{
    public async ValueTask<DashboardDto> HandleAsync(DateRange range, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var dto = new DashboardDto();

        // ---------------------------
        // 1) Индикаторы + BULK по метрикам компании (1 запрос к CH)
        // ---------------------------
        var indicators = await db.Indicators
            .AsNoTracking()
            .Where(i => i.CreatedBy == companyId && i.ShowOnMainScreen)
            .Select(i => new { i.Id, i.Name, i.FillmentPeriod })
            .ToListAsync(ct);

        if (indicators.Count > 0)
        {
            var metricRequests = indicators
                .Select(ind => new FetchCompanyMetricsHandler.MetricRequest(
                    ind.Id,
                    (PeriodTypeEnum)ind.FillmentPeriod, // expected
                    (PeriodTypeEnum)ind.FillmentPeriod  // source
                ))
                .ToList();

            var bulkMetrics = await companyMetricsHandler.HandleBulkAsync(range, metricRequests, ct);

            foreach (var ind in indicators)
            {
                if (!bulkMetrics.TryGetValue(ind.Id, out var data)) continue;

                dto.Metrics.Add(new DashboardMetricDto
                {
                    Id = ind.Id,
                    Name = ind.Name,
                    Plan = data.PlanData,
                    Fact = data.FactData,
                    PeriodType = (FillmentPeriodWrapper)ind.FillmentPeriod,
                    IsArchived = false
                });
            }
        }

        // ---------------------------
        // 2) Сотрудники + BULK превью (0 N+1, контролируемая агрегация)
        // ---------------------------
        var employees = await db.Employees
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .Select(e => new { e.Id, e.FIO, e.JobTitle, e.DefaultRole })
            .ToListAsync(ct);

        var employeeIds = employees.Select(e => e.Id).ToList();

        var previewsMap = employeeIds.Count == 0
            ? new Dictionary<int, UserPreviewMetricsDto>()
            : await userMetricsHandler.HandleBulkAsync(employeeIds, range, PeriodTypeEnum.Month, ct);

        var employeeAverages = new Dictionary<int, double>(employees.Count);
        foreach (var e in employees)
        {
            if (previewsMap.TryGetValue(e.Id, out var preview) && preview is { Metrics.Count: > 0 })
                employeeAverages[e.Id] = preview.Average;
        }

        // одна проекция → два отбора (best/worst)
        var employeeDtos = employees.Select(e =>
        {
            var avg = employeeAverages.GetValueOrDefault(e.Id, 0d);
            return new DashboardEmployeeDto
            {
                Id = e.Id,
                Fio = e.FIO,
                JobTitle = e.JobTitle,
                IsSupervisor = e.DefaultRole == EmployeeTypeEnum.Supervisor,
                Average = Math.Round(avg, 0, MidpointRounding.ToZero)
            };
        });

        dto.BestEmployees = employeeDtos.OrderByDescending(x => x.Average).Take(3).ToList();
        dto.WorstEmployees = employeeDtos.OrderBy(x => x.Average).Take(3).ToList();

        // ---------------------------
        // 3) Департаменты: минимизируем запросы и Contains на List
        //    (один join для корней, HashSet для связей)
        // ---------------------------
        var rootDeptPairs = await (
            from s in db.DepartmentSchemas.AsNoTracking()
            join d in db.Departments.AsNoTracking() on s.DepartmentId equals d.Id
            where s.FundamentalDepartmentId == s.DepartmentId
               && s.Depth == 0
               && d.CompanyId == companyId
               && !d.IsDeleted
            select new { RootId = d.Id, RootName = d.Name }
        ).Distinct().ToListAsync(ct);

        var rootIds = rootDeptPairs.Select(x => x.RootId).ToHashSet();

        var schemas = await db.DepartmentSchemas.AsNoTracking()
            .Where(s => rootIds.Contains(s.FundamentalDepartmentId))
            .Select(s => new { s.FundamentalDepartmentId, s.DepartmentId })
            .ToListAsync(ct);

        var allDeptIds = schemas.Select(s => s.DepartmentId).ToHashSet();

        var links = await db.EmployeeDepartmentLinks.AsNoTracking()
            .Where(l => allDeptIds.Contains(l.DepartmentId))
            .Select(l => new { l.DepartmentId, l.EmployeeId })
            .ToListAsync(ct);

        // root -> set(deptIds)
        var rootToDeptIds = schemas
            .GroupBy(s => s.FundamentalDepartmentId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.DepartmentId).ToHashSet());

        foreach (var root in rootDeptPairs)
        {
            if (!rootToDeptIds.TryGetValue(root.RootId, out var deptIdsForRoot) || deptIdsForRoot.Count == 0)
            {
                dto.Departments.Add(new DashboardDepartmentDto { Id = root.RootId, Name = root.RootName, Average = 0 });
                continue;
            }

            // берем avg сотрудников, привязанных к отделам root-а
            var values = links
                .Where(l => deptIdsForRoot.Contains(l.DepartmentId))
                .Select(l => employeeAverages.TryGetValue(l.EmployeeId, out var avg) ? (double?)avg : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value);

            var avgDept = values.Any() ? values.Average() : 0d;

            dto.Departments.Add(new DashboardDepartmentDto
            {
                Id = root.RootId,
                Name = root.RootName,
                Average = Math.Round(avgDept, 0, MidpointRounding.ToZero)
            });
        }

        dto.DepartmentsAverage = dto.Departments.Count > 0
            ? Math.Round(dto.Departments.Average(d => d.Average), 0, MidpointRounding.ToZero)
            : 0;

        return dto;
    }
}
