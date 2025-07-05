using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Metric;

public class MetricGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
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
            DepartmentId = m.DepartmentId,
            EmployeeId = m.EmployeeId,
            Plan = m.Plan,
            Min = m.Min,
            Max = m.Max,
            Visible = m.Visible
        }).ToList();
    }
}
