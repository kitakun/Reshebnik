using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Models.MetricTemplate;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.MetricTemplate;

public class MetricTemplateGetAllHandler(
    TabligoContext db,
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
