using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Metric;

public class MetricPutHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<int> HandleAsync(MetricPutDto dto, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        MetricEntity entity;
        if (dto.Id != 0)
        {
            entity = await db.Metrics.FirstOrDefaultAsync(m => m.Id == dto.Id && m.CompanyId == companyId, ct) ?? new MetricEntity { CompanyId = companyId };
            if (entity.Id == 0) db.Metrics.Add(entity);
        }
        else
        {
            entity = new MetricEntity { CompanyId = companyId };
            db.Metrics.Add(entity);
        }

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Unit = dto.Unit;
        entity.Type = dto.Type;
        entity.DepartmentId = dto.DepartmentId;
        entity.EmployeeId = dto.EmployeeId;
        entity.Plan = dto.Plan;
        entity.Min = dto.Min;
        entity.Max = dto.Max;
        entity.Visible = dto.Visible;

        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
