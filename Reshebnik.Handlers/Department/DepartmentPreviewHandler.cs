using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Department;
using Reshebnik.EntityFramework;
using Reshebnik.Clickhouse.Handlers;

namespace Reshebnik.Handlers.Department;

public class DepartmentPreviewHandler(
    ReshebnikContext db,
    FetchDepartmentCompletionHandler fetchHandler)
{
    private const int MAX_USERS = 5;

    public async ValueTask<DepartmentPreviewDto?> HandleAsync(int id, DateRange range, CancellationToken ct = default)
    {
        var departments = await db.Departments
            .AsNoTracking()
            .Where(d => !d.IsDeleted && (d.Id == id || db.DepartmentSchemaEntities.Any(s => s.AncestorDepartmentId == id && s.Depth == 1 && s.DepartmentId == d.Id)))
            .ToListAsync(ct);
        if (departments.Count == 0) return null;

        var childIds = departments.Where(d => d.Id != id).Select(d => d.Id).ToList();

        var deptIds = departments.Select(d => d.Id).ToList();

        var links = await db.EmployeeDepartmentLinkEntities
            .AsNoTracking()
            .Include(l => l.Employee)
            .Where(l => deptIds.Contains(l.DepartmentId))
            .ToListAsync(ct);

        var dict = departments.ToDictionary(d => d.Id, d => new DepartmentPreviewDto
        {
            Id = d.Id,
            Name = d.Name,
            CompletionPercent = 0
        });
        
        var metrics = await db.Metrics
            .AsNoTracking()
            .Where(m => m.DepartmentId != null && deptIds.Contains(m.DepartmentId.Value))
            .ToListAsync(ct);

        foreach (var d in departments)
        {
            var metricsForDept = metrics.Where(m => m.DepartmentId == d.Id).ToList();
            double sumPercent = 0;
            int count = 0;

            foreach (var metric in metricsForDept)
            {
                var avg = await fetchHandler.HandleAsync(d.CompanyId, d.Id, metric.ClickHouseKey, range, ct);
                if (double.IsNaN(avg) || double.IsInfinity(avg))
                    avg = 0;

                double percent = 0;
                if (metric.Min.HasValue && metric.Max.HasValue && metric.Max != metric.Min)
                {
                    var min = (double)metric.Min.Value;
                    var max = (double)metric.Max.Value;
                    percent = (avg - min) / (max - min) * 100;
                }

                sumPercent += percent;
                count++;
            }

            if (dict.TryGetValue(d.Id, out var dto))
                dto.CompletionPercent = count > 0 ? sumPercent / count : 0;
        }

        foreach (var link in links)
        {
            var userDto = new DepartmentPreviewUserDto
            {
                Id = link.EmployeeId,
                Fio = link.Employee.FIO,
                CompletionPercent = 0
            };

            if (link.Type == EmployeeTypeEnum.Supervisor)
                dict[link.DepartmentId].Supervisors.Add(userDto);
            else
                dict[link.DepartmentId].Employees.Add(userDto);
        }

        foreach (var d in dict.Values)
        {
            d.Supervisors = d.Supervisors
                .OrderByDescending(u => u.CompletionPercent)
                .Take(MAX_USERS)
                .ToList();
            d.Employees = d.Employees
                .OrderByDescending(u => u.CompletionPercent)
                .Take(MAX_USERS)
                .ToList();
        }

        var allEmployees = dict.Values.SelectMany(v => v.Employees).ToList();
        var best = allEmployees.OrderByDescending(e => e.CompletionPercent).Take(3).ToList();
        var worst = allEmployees.OrderBy(e => e.CompletionPercent).Take(3).ToList();

        var rootDto = dict[id];
        rootDto.Children = childIds.Select(cid => dict[cid]).ToList();
        rootDto.BestEmployees = best;
        rootDto.WorstEmployees = worst;

        return rootDto;
    }
}
