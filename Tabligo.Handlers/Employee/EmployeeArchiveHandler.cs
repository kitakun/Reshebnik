using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Entities;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Auth;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Employee;

public class EmployeeArchiveHandler(
    TabligoContext db,
    CompanyContextHandler companyContext,
    UserContextHandler userContext)
{
    public async Task HandleAsync(int id, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var employee = await db.Employees.FirstAsync(e => e.CompanyId == companyId && e.Id == id, ct);

        var archived = new ArchivedUserEntity
        {
            CompanyId = companyId,
            EmployeeId = employee.Id,
            ArchivedAt = DateTime.UtcNow,
            ArchivedByUserId = userContext.CurrentUserId
        };

        employee.IsArchived = true;
        employee.ArchivedUser = archived;

        db.ArchivedUsers.Add(archived);
        await db.SaveChangesAsync(ct);
    }
}
