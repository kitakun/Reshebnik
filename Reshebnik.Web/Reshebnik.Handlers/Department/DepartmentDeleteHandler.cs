using Microsoft.EntityFrameworkCore;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Department;

public class DepartmentDeleteHandler(ReshebnikContext db)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Departments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (entity == null) return;
        db.Departments.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task HandleManyAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var entities = await db.Departments.Where(d => ids.Contains(d.Id)).ToListAsync(ct);
        db.Departments.RemoveRange(entities);
        await db.SaveChangesAsync(ct);
    }
}
