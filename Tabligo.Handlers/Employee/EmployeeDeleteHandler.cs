using Microsoft.EntityFrameworkCore;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Employee;

public class EmployeeDeleteHandler(TabligoContext db)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Employees.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entity == null) return;
        db.Employees.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task HandleManyAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var entities = await db.Employees.Where(e => ids.Contains(e.Id)).ToListAsync(ct);
        db.Employees.RemoveRange(entities);
        await db.SaveChangesAsync(ct);
    }
}
