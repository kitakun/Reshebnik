using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Department;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.Domain.Extensions;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Metric;

namespace Reshebnik.Handlers.Department;

public class DepartmentPreviewHandler(
    ReshebnikContext db,
    UserPreviewMetricsHandler userMetricsHandler)
{
    public async ValueTask<DepartmentPreviewDto?> HandleAsync(int id, DateRange range, PeriodTypeEnum periodType, CancellationToken ct = default)
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
            Depth = d.Id == id ? 0 : 1,
            CompletionPercent = 0,
            Metrics = new DepartmentPreviewMetricsDto()
        });

        foreach (var link in links)
        {
            var userDto = new DepartmentPreviewUserDto
            {
                Id = link.EmployeeId,
                Fio = link.Employee.FIO,
                JobTitle = link.Employee.JobTitle,
                IsSupervisor = link.Type == EmployeeTypeEnum.Supervisor,
                CompletionPercent = 0
            };

            if (link.Type == EmployeeTypeEnum.Supervisor)
                dict[link.DepartmentId].Supervisors.Add(userDto);
            else
                dict[link.DepartmentId].Employees.Add(userDto);
        }

        double[]? planSums = null;
        double[]? factSums = null;
        var metricsCount = 0;

        foreach (var dto in dict.Values)
        {
            var allUsers = dto.Supervisors.Concat(dto.Employees).ToList();
            foreach (var user in allUsers)
            {
                var metricsDto = await userMetricsHandler.HandleAsync(user.Id, range, periodType, ct);
                if (metricsDto != null && metricsDto.Metrics.Count > 0)
                {
                    double userSum = 0;
                    var userMetricCount = 0;

                    foreach (var metric in metricsDto.Metrics)
                    {
                        var plan = metric.Last12PointsPlan;
                        var fact = metric.Last12PointsFact;
                        planSums ??= new double[plan.Length];
                        factSums ??= new double[fact.Length];
                        var len = Math.Min(planSums.Length, Math.Min(plan.Length, fact.Length));
                        for (var i = 0; i < len; i++)
                        {
                            planSums[i] += plan[i];
                            factSums[i] += fact[i];
                        }

                        userSum += metric.GetCompletionPercent();
                        metricsCount++;
                        userMetricCount++;
                    }

                    user.CompletionPercent = userMetricCount > 0
                        ? Math.Round(userSum / userMetricCount, 0, MidpointRounding.ToZero)
                        : 0;
                }
            }

            dto.CompletionPercent = allUsers.Count > 0
                ? Math.Round(allUsers.Average(u => u.CompletionPercent), 0, MidpointRounding.ToZero)
                : 0;

            dto.Supervisors = dto.Supervisors
                .OrderByDescending(u => u.CompletionPercent)
                .ToList();
            dto.Employees = dto.Employees
                .OrderByDescending(u => u.CompletionPercent)
                .ToList();
        }

        var allEmployees = dict.Values
            .SelectMany(v => v.Employees)
            .Concat(dict.Values
                .SelectMany(v => v.Supervisors))
            .ToList();
        var best = allEmployees.OrderByDescending(e => e.CompletionPercent).Take(3).ToList();
        var worst = allEmployees.OrderBy(e => e.CompletionPercent).Take(3).ToList();

        var rootDto = dict[id];
        rootDto.Children = childIds.Select(cid => dict[cid]).ToList();
        rootDto.BestEmployees = best;
        rootDto.WorstEmployees = worst;

        if (metricsCount > 0 && planSums != null && factSums != null)
        {
            var planAvg = planSums.Select(s => (int)Math.Round(s / metricsCount, 0, MidpointRounding.ToZero)).ToArray();
            var factAvg = factSums.Select(s => (int)Math.Round(s / metricsCount, 0, MidpointRounding.ToZero)).ToArray();
            rootDto.Metrics = new DepartmentPreviewMetricsDto
            {
                PlanData = planAvg,
                FactData = factAvg
            };
        }

        return rootDto;
    }

}

