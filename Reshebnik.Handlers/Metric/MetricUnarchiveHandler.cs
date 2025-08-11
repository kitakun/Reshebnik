using Microsoft.EntityFrameworkCore;

using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Metric;

public class MetricUnarchiveHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var archived = await db.ArchivedMetrics
            .IgnoreQueryFilters()
            .Include(a => a.Metric)
            .FirstAsync(a => a.CompanyId == companyId && a.Id == id, ct);

        archived.Metric.IsArchived = false;
        archived.Metric.ArchivedMetric = null;

        db.ArchivedMetrics.Remove(archived);
        await db.SaveChangesAsync(ct);
    }
}
