using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Enums;
using Tabligo.Domain.Models;
using Tabligo.Domain.Models.Department;
using Tabligo.Domain.Extensions;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Metric;

namespace Tabligo.Handlers.Department;

public class DepartmentPreviewHandler(
    TabligoContext db,
    UserPreviewMetricsHandler userMetricsHandler)
{
    public async ValueTask<DepartmentPreviewDto?> HandleAsync(
        int id,
        DateRange range,
        PeriodTypeEnum periodType,
        CancellationToken ct = default)
    {
        // 1) Fetch the root + its direct children (by schema) in one shot
        var childIdsQuery = db.DepartmentSchemas
            .AsNoTracking()
            .Where(s => s.AncestorDepartmentId == id && s.Depth == 1)
            .Select(s => s.DepartmentId);

        var departments = await db.Departments
            .AsNoTracking()
            .Where(d => !d.IsDeleted && (d.Id == id || childIdsQuery.Contains(d.Id)))
            .Select(d => new { d.Id, d.Name })
            .ToListAsync(ct);

        if (departments.Count == 0) return null;

        var deptIds = departments.Select(d => d.Id).ToArray();
        var childIds = departments.Where(d => d.Id != id).Select(d => d.Id).ToArray();

        // 2) Fetch links with only needed employee fields (no Include)
        var links = await db.EmployeeDepartmentLinks
            .AsNoTracking()
            .Where(l => deptIds.Contains(l.DepartmentId))
            .Select(l => new
            {
                l.DepartmentId,
                l.EmployeeId,
                l.Type,
                Fio = l.Employee.FIO,
                JobTitle = l.Employee.JobTitle
            })
            .ToListAsync(ct);

        // 3) Build base dict (no duplicates later)
        var dict = departments.ToDictionary(
            d => d.Id,
            d => new DepartmentPreviewDto
            {
                Id = d.Id,
                Name = d.Name,
                Depth = d.Id == id ? 0 : 1,
                CompletionPercent = 0,
                Metrics = new DepartmentPreviewMetricsDto()
            });

        // 4) Map unique users to departments
        //    Use HashSet per dept to dedupe quickly.
        var seenSupervisorByDept = deptIds.ToDictionary(k => k, _ => new HashSet<int>());
        var seenEmployeeByDept = deptIds.ToDictionary(k => k, _ => new HashSet<int>());

        foreach (var l in links)
        {
            var dto = new DepartmentPreviewUserDto
            {
                Id = l.EmployeeId,
                Fio = l.Fio,
                JobTitle = l.JobTitle,
                IsSupervisor = l.Type == EmployeeTypeEnum.Supervisor,
                CompletionPercent = 0
            };

            if (dto.IsSupervisor)
            {
                if (seenSupervisorByDept[l.DepartmentId].Add(dto.Id))
                    dict[l.DepartmentId].Supervisors.Add(dto);
            }
            else
            {
                if (seenEmployeeByDept[l.DepartmentId].Add(dto.Id))
                    dict[l.DepartmentId].Employees.Add(dto);
            }
        }

        // 5) Collect all distinct user ids once
        var allUserIds = dict.Values
            .SelectMany(v => v.Supervisors.Concat(v.Employees))
            .Select(u => u.Id)
            .Distinct()
            .ToArray();

        if (allUserIds.Length == 0)
        {
            var emptyRoot = dict[id];
            emptyRoot.Children = childIds.Select(cid => dict[cid]).ToList();
            emptyRoot.BestEmployees = [];
            emptyRoot.WorstEmployees = [];
            emptyRoot.CompletionPercent = 0;
            return emptyRoot;
        }

        // 6) Fetch metrics in parallel with bounded concurrency
        var userMetrics = await userMetricsHandler.HandleBulkItemsAsync(
            userIds: allUserIds,
            range,
            periodType,
            ct);

        // 7) Compute per-user completion and aggregate plan/fact for department+root
        double[]? planSums = null;
        double[]? factSums = null;
        var metricsCount = 0;

        // Make a quick lookup to avoid repeated LINQ
        var metricsByUser = userMetrics;

        foreach (var department in dict.Values)
        {
            var allUsers = department.Supervisors.Count == 0
                ? department.Employees
                : department.Supervisors.Concat(department.Employees).ToList();

            foreach (var user in allUsers)
            {
                if (!metricsByUser.TryGetValue(user.Id, out var userItems) || userItems.Count == 0)
                {
                    user.CompletionPercent = 0;
                    continue;
                }

                double userSum = 0;
                int userMetricCount = 0;

                foreach (var metric in userItems)
                {
                    var plan = metric.Last12PointsPlan;
                    var fact = metric.Last12PointsFact;

                    planSums ??= new double[plan.Length];
                    factSums ??= new double[fact.Length];

                    var len = Math.Min(planSums.Length, Math.Min(plan.Length, fact.Length));
                    for (int i = 0; i < len; i++)
                    {
                        planSums[i] += plan[i];
                        factSums[i] += fact[i];
                    }

                    userSum += metric.GetCompletionPercent();
                    userMetricCount++;
                    metricsCount++;
                }

                user.CompletionPercent = userMetricCount > 0
                    ? Math.Round(userSum / userMetricCount, 0, MidpointRounding.ToZero)
                    : 0;
            }

            department.CompletionPercent = allUsers.Count > 0
                ? Math.Round(allUsers.Average(u => u.CompletionPercent), 0, MidpointRounding.ToZero)
                : 0;

            // Sorted lists without extra GroupBy/First
            department.Supervisors.Sort((a, b) => b.CompletionPercent.CompareTo(a.CompletionPercent));
            department.Employees.Sort((a, b) => b.CompletionPercent.CompareTo(a.CompletionPercent));
        }

        // 8) Best / Worst across unique employees
        var allEmployees = dict.Values
            .SelectMany(v => v.Employees)
            .GroupBy(e => e.Id)
            .Select(g => g.First())
            .ToList();

        var best = allEmployees
            .OrderByDescending(e => e.CompletionPercent)
            .Take(3)
            .ToList();

        var bestIds = best.Select(b => b.Id).ToHashSet();

        var worst = allEmployees
            .Where(e => !bestIds.Contains(e.Id))
            .OrderBy(e => e.CompletionPercent)
            .Take(3)
            .ToList();

        // 9) Root assembly
        var rootDto = dict[id];
        rootDto.Children = childIds.Select(cid => dict[cid]).ToList();
        rootDto.BestEmployees = best;
        rootDto.WorstEmployees = worst;

        if (metricsCount > 0 && planSums is not null && factSums is not null)
        {
            var planAvg = Array.ConvertAll(planSums, s => (int)Math.Round(s / metricsCount, 0, MidpointRounding.ToZero));
            var factAvg = Array.ConvertAll(factSums, s => (int)Math.Round(s / metricsCount, 0, MidpointRounding.ToZero));
            rootDto.Metrics = new DepartmentPreviewMetricsDto
            {
                PlanData = planAvg,
                FactData = factAvg
            };
        }

        rootDto.CompletionPercent = rootDto.Children.Count > 0
            ? Math.Round(rootDto.Children.Average(c => c.CompletionPercent), 0, MidpointRounding.ToZero)
            : rootDto.CompletionPercent;

        return rootDto;
    }
}