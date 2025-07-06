using Microsoft.EntityFrameworkCore;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.MetricTemplate;

public class MetricTemplateDeleteHandler(ReshebnikContext db)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.MetricTemplates.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity == null) return;
        db.MetricTemplates.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
