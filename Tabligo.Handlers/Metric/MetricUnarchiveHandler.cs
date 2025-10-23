using Microsoft.EntityFrameworkCore;

using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Metric;

public class MetricUnarchiveHandler(
    TabligoContext db,
    CompanyContextHandler companyContext)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var archived = await db.ArchivedMetrics
            .IgnoreQueryFilters()
            .Include(a => a.Metric)
            .FirstAsync(a => a.CompanyId == companyId && a.Id == id, ct);

        if (archived.Metric != null)
        {
            archived.Metric.IsArchived = false;
            archived.Metric.ArchivedMetric = null;
        }

        db.ArchivedMetrics.Remove(archived);
        await db.SaveChangesAsync(ct);
    }
}
