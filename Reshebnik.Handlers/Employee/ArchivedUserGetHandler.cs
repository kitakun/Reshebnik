using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Models.Employee;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Employee;

public class ArchivedUserGetHandler(
    ReshebnikContext db,
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
