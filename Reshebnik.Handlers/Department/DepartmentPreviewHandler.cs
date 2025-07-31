using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Department;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Metric;
using System.Linq;

namespace Reshebnik.Handlers.Department;

public class DepartmentPreviewHandler(
    ReshebnikContext db,
    UserPreviewMetricsHandler userMetricsHandler)
{
    private const int MAX_USERS = 5;

    public async ValueTask<DepartmentPreviewDto?> HandleAsync(int id, DateRange range, CancellationToken ct = default)
    {
        var departments = await db.Departments
            .AsNoTracking()
            .Where(d => !d.IsDeleted && (d.Id == id || db.DepartmentSchemas.Any(s => s.AncestorDepartmentId == id && s.Depth == 1 && s.DepartmentId == d.Id)))
            .ToListAsync(ct);
        if (departments.Count == 0) return null;

        var childIds = departments.Where(d => d.Id != id).Select(d => d.Id).ToList();

        var deptIds = departments.Select(d => d.Id).ToList();

        var links = await db.EmployeeDepartmentLinks
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

        foreach (var dto in dict.Values)
        {
            var allUsers = dto.Supervisors.Concat(dto.Employees).ToList();
            foreach (var user in allUsers)
            {
                var metricsDto = await userMetricsHandler.HandleAsync(user.Id, range, PeriodTypeEnum.Month, ct);
                if (metricsDto != null && metricsDto.Metrics.Count > 0)
                    user.CompletionPercent = Math.Round(metricsDto.Average, 0, MidpointRounding.ToZero);
            }

            dto.CompletionPercent = allUsers.Count > 0
                ? Math.Round(allUsers.Average(u => u.CompletionPercent), 0, MidpointRounding.ToZero)
                : 0;

            dto.Supervisors = dto.Supervisors
                .OrderByDescending(u => u.CompletionPercent)
                .Take(MAX_USERS)
                .ToList();
            dto.Employees = dto.Employees
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
