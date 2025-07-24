using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Dashboard;
using Reshebnik.EntityFramework;
using Reshebnik.Clickhouse.Handlers;
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

        var employeeAverages = new Dictionary<int, double>(employees.Count);
        foreach (var emp in employees)
        {
            var preview = await userMetricsHandler.HandleAsync(emp.Id, range, PeriodTypeEnum.Month, ct);
            employeeAverages[emp.Id] = preview?.Average ?? 0;
        }

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

        foreach (var dept in departments)
        {
            var empIds = links
                .Where(l => l.DepartmentId == dept.Id)
                .Select(l => l.EmployeeId)
                .ToList();

            double sum = 0;
            int count = 0;
            foreach (var id in empIds)
            {
                if (employeeAverages.TryGetValue(id, out var avg))
                {
                    sum += avg;
                    count++;
                }
            }

            var average = count > 0 ? sum / count : 0;

            dto.Departments.Add(new DashboardDepartmentDto
            {
                Id = dept.Id,
                Name = dept.Name,
                Average = average
            });
        }

        return dto;
    }
}
