using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models.Employee;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Employee;

public class EmployeesGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<List<EmployeeDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var employees = await db.Employees
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId)
            .ToListAsync(cancellationToken);

        return employees.Select(e => new EmployeeDto
        {
            Id = e.Id,
            Fio = e.FIO,
            JobTitle = e.JobTitle,
            Email = e.Email,
            Phone = e.Phone,
            Comment = e.Comment,
            IsActive = e.IsActive
        }).ToList();
    }
}
