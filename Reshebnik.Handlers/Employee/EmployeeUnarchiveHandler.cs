using Microsoft.EntityFrameworkCore;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Employee;

public class EmployeeUnarchiveHandler(ReshebnikContext db)
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
