using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Entities;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Auth;
using Reshebnik.Handlers.Company;
using System;

namespace Reshebnik.Handlers.Employee;

public class EmployeeArchiveHandler(
    ReshebnikContext db,
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
