using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models.Employee;
using Reshebnik.Domain.Enums;
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
        var employees = db.Employees
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId);

        return await employees.Select(e => new EmployeeDto
        {
            Id = e.Id,
            Fio = e.FIO,
            JobTitle = e.JobTitle,
            Email = e.Email,
            Phone = e.Phone,
            Comment = e.Comment,
            IsActive = e.IsActive,
            IsSupervisor = e.DefaultRole == EmployeeTypeEnum.Supervisor,
            DefaultRole = e.DefaultRole,
            DepartmentName = e.DepartmentLinks.First().Department.Name,
            DepartmentId = e.DepartmentLinks.First().DepartmentId
        }).ToListAsync(cancellationToken);
    }
}
