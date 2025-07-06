using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Models.MetricTemplate;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.MetricTemplate;

public class MetricTemplateGetAllHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<List<MetricTemplateDto>> HandleAsync(CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var entities = await db.MetricTemplates
            .AsNoTracking()
            .Where(t => t.CompanyId == companyId)
            .ToListAsync(ct);

        return entities.Select(e => new MetricTemplateDto
        {
            Id = e.Id,
            Name = e.Name,
            CreatedAt = e.CreatedAt
        }).ToList();
    }
}
