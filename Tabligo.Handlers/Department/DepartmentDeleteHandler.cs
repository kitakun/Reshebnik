using Microsoft.EntityFrameworkCore;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Department;

public class DepartmentDeleteHandler(TabligoContext db)
{
    private async Task<List<int>> CollectIdsAsync(int id, CancellationToken ct)
    {
        var ids = await db.DepartmentSchemas
            .Where(s => s.AncestorDepartmentId == id)
            .Select(s => s.DepartmentId)
            .Distinct()
            .ToListAsync(ct);
        ids.Add(id);
        return ids;
    }

    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var ids = await CollectIdsAsync(id, ct);
        var departments = await db.Departments.Where(d => ids.Contains(d.Id)).ToListAsync(ct);
        if (departments.Count == 0) return;
        foreach (var d in departments) d.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }

    public async Task HandleManyAsync(IEnumerable<int> ids, CancellationToken ct = default)
    {
        var all = new HashSet<int>();
        foreach (var id in ids)
        {
            var collected = await CollectIdsAsync(id, ct);
            foreach (var cid in collected) all.Add(cid);
        }

        var departments = await db.Departments.Where(d => all.Contains(d.Id)).ToListAsync(ct);
        if (departments.Count == 0) return;
        foreach (var d in departments) d.IsDeleted = true;
        await db.SaveChangesAsync(ct);
    }
}
