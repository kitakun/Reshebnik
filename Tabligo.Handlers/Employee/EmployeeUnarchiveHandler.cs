using Microsoft.EntityFrameworkCore;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Employee;

public class EmployeeUnarchiveHandler(TabligoContext db)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var archived = await db.ArchivedUsers
            .Include(a => a.Employee)
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(a => a.Id == id, ct);
        if (archived == null) return;

        archived.Employee.IsArchived = false;
        archived.Employee.ArchivedUser = null;

        db.ArchivedUsers.Remove(archived);
        await db.SaveChangesAsync(ct);
    }
}
