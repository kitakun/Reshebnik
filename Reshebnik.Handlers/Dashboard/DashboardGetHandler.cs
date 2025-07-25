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
                PeriodType = (FillmentPeriodWrapper)ind.FillmentPeriod
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
            .ToDictionary(z => z.First.Id, z => z.Second?.Average ?? 0);

        dto.BestEmployees = employees
            .Select(e => new DashboardEmployeeDto
            {
                Id = e.Id,
                Fio = e.FIO,
                Average = employeeAverages.GetValueOrDefault(e.Id, 0)
            })
            .OrderByDescending(e => e.Average)
            .Take(3)
            .ToList();

        dto.WorstEmployees = employees
            .Select(e => new DashboardEmployeeDto
            {
                Id = e.Id,
                Fio = e.FIO,
                Average = employeeAverages.GetValueOrDefault(e.Id, 0)
            })
            .OrderBy(e => e.Average)
            .Take(3)
            .ToList();

        var rootIds = await db.Departments
            .AsNoTracking()
            .Where(d => d.CompanyId == companyId && d.IsFundamental && !d.IsDeleted)
            .Select(d => d.Id)
            .ToListAsync(ct);

        var level1Ids = await db.DepartmentSchemas
            .AsNoTracking()
            .Where(s => rootIds.Contains(s.AncestorDepartmentId) && s.Depth == 1 && s.DepartmentId != s.AncestorDepartmentId)
            .Select(s => s.DepartmentId)
            .Distinct()
            .ToListAsync(ct);

        var departments = await db.Departments
            .AsNoTracking()
            .Where(d => level1Ids.Contains(d.Id) && !d.IsDeleted)
            .Select(d => new { d.Id, d.Name })
            .ToListAsync(ct);

        var links = await db.EmployeeDepartmentLinks
            .AsNoTracking()
            .Where(l => level1Ids.Contains(l.DepartmentId))
            .ToListAsync(ct);

        var departmentAverages = links
            .GroupBy(l => l.DepartmentId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(l => employeeAverages.GetValueOrDefault(l.EmployeeId, 0d))
                    .DefaultIfEmpty(0)
                    .Average());

        foreach (var dept in departments)
        {
            dto.Departments.Add(new DashboardDepartmentDto
            {
                Id = dept.Id,
                Name = dept.Name,
                Average = departmentAverages.GetValueOrDefault(dept.Id, 0)
            });
        }

        dto.DepartmentsAverage = dto.Departments.Count > 0
            ? dto.Departments.Average(d => d.Average)
            : 0;

        return dto;
    }
}
