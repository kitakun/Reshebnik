using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Models.Employee;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Employee;

public class ArchivedUserGetHandler(
    TabligoContext db,
    CompanyContextHandler companyContext)
{
    public async Task<ArchivedUserGetDto?> HandleAsync(int id, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var archived = await db.ArchivedUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id && a.CompanyId == companyId, ct);
        if (archived == null) return null;

        return new ArchivedUserGetDto
        {
            Id = archived.Id,
            EmployeeId = archived.EmployeeId,
            ArchivedAt = archived.ArchivedAt,
            ArchivedByUserId = archived.ArchivedByUserId
        };
    }
}
