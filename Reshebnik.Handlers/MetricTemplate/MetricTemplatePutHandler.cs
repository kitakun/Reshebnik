using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Models.MetricTemplate;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.MetricTemplate;

public class MetricTemplatePutHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<int> HandleAsync(MetricTemplatePutDto dto, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        MetricTemplateEntity entity;
        if (dto.Id != 0)
        {
            entity = await db.MetricTemplates.FirstOrDefaultAsync(m => m.Id == dto.Id && m.CompanyId == companyId, ct) ?? new MetricTemplateEntity { CompanyId = companyId, CreatedAt = DateTime.UtcNow };
            if (entity.Id == 0) db.MetricTemplates.Add(entity);
        }
        else
        {
            entity = new MetricTemplateEntity { CompanyId = companyId, CreatedAt = DateTime.UtcNow };
            db.MetricTemplates.Add(entity);
        }

        entity.Name = dto.Name;
        entity.Description = dto.Description;
        entity.Unit = dto.Unit;
        entity.Type = dto.Type;
        entity.PeriodType = dto.PeriodType;
        entity.Plan = dto.Plan;
        entity.Min = dto.Min;
        entity.Max = dto.Max;
        entity.Visible = dto.Visible;

        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
