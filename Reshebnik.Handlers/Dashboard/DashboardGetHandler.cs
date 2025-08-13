using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Dashboard;
using Reshebnik.EntityFramework;
using Reshebnik.Clickhouse.Handlers;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.Handlers.Company;
using Reshebnik.Handlers.Metric;

using TaskExtensions = Reshebnik.Domain.Extensions.TaskExtensions;

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

        var indicators = await db.Indicators
            .AsNoTracking()
            .Where(i => i.CreatedBy == companyId && i.ShowOnMainScreen)
            .ToListAsync(ct);

        foreach (var ind in indicators)
        {
            var data = await companyMetricsHandler.HandleAsync(
                range,
                ind.Id,
                (FillmentPeriodWrapper)ind.FillmentPeriod,
                (FillmentPeriodWrapper)ind.FillmentPeriod,
                ct);

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

        var employees = await db.Employees
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId && e.IsActive)
            .ToListAsync(ct);

        var previews = new List<UserPreviewMetricsDto?>();
        foreach (var employee in employees
                     .Select(e => userMetricsHandler.HandleAsync(e.Id, range, PeriodTypeEnum.Month, ct)))
        {
            previews.Add(await employee);
        }

        var employeeAverages = employees
            .Zip(previews)
            .Where(z => z.Second is { Metrics.Count: > 0 })
            .ToDictionary(z => z.First.Id, z => z.Second!.Average);

        dto.BestEmployees = employees
            .Select(e => new DashboardEmployeeDto
            {
                Id = e.Id,
                Fio = e.FIO,
                JobTitle = e.JobTitle,
                IsSupervisor = e.DefaultRole == EmployeeTypeEnum.Supervisor,
                Average = Math.Round(employeeAverages.GetValueOrDefault(e.Id, 0), 0, MidpointRounding.ToZero)
            })
            .OrderByDescending(e => e.Average)
            .Take(3)
            .ToList();

        dto.WorstEmployees = employees
            .Select(e => new DashboardEmployeeDto
            {
                Id = e.Id,
                Fio = e.FIO,
                JobTitle = e.JobTitle,
                IsSupervisor = e.DefaultRole == EmployeeTypeEnum.Supervisor,
                Average = Math.Round(employeeAverages.GetValueOrDefault(e.Id, 0), 0, MidpointRounding.ToZero)
            })
            .OrderBy(e => e.Average)
            .Take(3)
            .ToList();

        var rootIds = await db.DepartmentSchemas
            .AsNoTracking()
            .Where(s => s.FundamentalDepartmentId == s.DepartmentId && s.Depth == 0)
            .Where(s => db.Departments.Any(d => d.CompanyId == companyId && d.Id == s.DepartmentId && !d.IsDeleted))
            .Select(s => s.DepartmentId)
            .Distinct()
            .ToListAsync(ct);

        var rootDepartments = await db.Departments
            .AsNoTracking()
            .Where(d => rootIds.Contains(d.Id))
            .Select(d => new { d.Id, d.Name })
            .ToListAsync(ct);

        var schemas = await db.DepartmentSchemas
            .AsNoTracking()
            .Where(s => rootIds.Contains(s.FundamentalDepartmentId))
            .Select(s => new { s.FundamentalDepartmentId, s.DepartmentId })
            .ToListAsync(ct);

        var allDeptIds = schemas.Select(s => s.DepartmentId).Distinct().ToList();

        var links = await db.EmployeeDepartmentLinks
            .AsNoTracking()
            .Where(l => allDeptIds.Contains(l.DepartmentId))
            .ToListAsync(ct);

        foreach (var root in rootDepartments)
        {
            var deptIds = schemas
                .Where(s => s.FundamentalDepartmentId == root.Id)
                .Select(s => s.DepartmentId)
                .Distinct()
                .ToList();

            var values = links
                .Where(l => deptIds.Contains(l.DepartmentId))
                .Select(l => employeeAverages.TryGetValue(l.EmployeeId, out var avg) ? (double?)avg : null)
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            dto.Departments.Add(new DashboardDepartmentDto
            {
                Id = root.Id,
                Name = root.Name,
                Average = Math.Round(values.Count > 0 ? values.Average() : 0d, 0, MidpointRounding.ToZero)
            });
        }

        dto.DepartmentsAverage = dto.Departments.Count > 0
            ? Math.Round(dto.Departments.Average(d => d.Average), 0, MidpointRounding.ToZero)
            : 0;

        return dto;
    }
}
