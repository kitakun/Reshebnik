using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

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
            .FirstAsync(m => m.CompanyId == companyId && m.Id == id, ct);

        return new MetricDto
        {
            Id = metrics.Id,
            Name = metrics.Name,
            Description = metrics.Description,
            Unit = metrics.Unit,
            Type = metrics.Type,
            PeriodType = metrics.PeriodType,
            DepartmentId = metrics.DepartmentId,
            EmployeeId = metrics.EmployeeId,
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
            DepartmentId = m.DepartmentId,
            EmployeeId = m.EmployeeId,
            Plan = m.Plan,
            Min = m.Min,
            Max = m.Max,
            Visible = m.Visible
        }).ToList();
    }
}
