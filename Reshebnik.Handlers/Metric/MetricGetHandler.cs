using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using System.Linq;

namespace Reshebnik.Handlers.Metric;

public class MetricGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<MetricDto> HandleAsync(int id, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var metrics = await db.Metrics
            .AsNoTracking()
            .Include(m => m.DepartmentLinks)
            .Include(m => m.EmployeeLinks)
            .FirstAsync(m => m.CompanyId == companyId && m.Id == id, ct);

        return new MetricDto
        {
            Id = metrics.Id,
            Name = metrics.Name,
            Description = metrics.Description,
            Unit = metrics.Unit,
            Type = metrics.Type,
            PeriodType = metrics.PeriodType,
            DepartmentIds = metrics.DepartmentLinks.Select(l => l.DepartmentId).ToArray(),
            EmployeeIds = metrics.EmployeeLinks.Select(l => l.EmployeeId).ToArray(),
            Plan = metrics.Plan,
            Min = metrics.Min,
            Max = metrics.Max,
            Visible = metrics.Visible
        };
    }
    
    public async ValueTask<List<MetricDto>> HandleAsync(CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var metrics = await db.Metrics
            .AsNoTracking()
            .Include(m => m.DepartmentLinks)
            .Include(m => m.EmployeeLinks)
            .Where(m => m.CompanyId == companyId)
            .ToListAsync(ct);

        return metrics.Select(m => new MetricDto
        {
            Id = m.Id,
            Name = m.Name,
            Description = m.Description,
            Unit = m.Unit,
            Type = m.Type,
            PeriodType = m.PeriodType,
            DepartmentIds = m.DepartmentLinks.Select(l => l.DepartmentId).ToArray(),
            EmployeeIds = m.EmployeeLinks.Select(l => l.EmployeeId).ToArray(),
            Plan = m.Plan,
            Min = m.Min,
            Max = m.Max,
            Visible = m.Visible
        }).ToList();
    }
}
