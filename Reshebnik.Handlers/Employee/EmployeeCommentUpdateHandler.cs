using Microsoft.EntityFrameworkCore;

using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Employee;

public class EmployeeCommentUpdateHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async Task<bool> HandleAsync(int userId, string comment, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var employee = await db.Employees
            .FirstOrDefaultAsync(e => e.Id == userId && e.CompanyId == companyId, ct);
        if (employee == null) return false;

        employee.Comment = comment;
        await db.SaveChangesAsync(ct);
        return true;
    }
}
