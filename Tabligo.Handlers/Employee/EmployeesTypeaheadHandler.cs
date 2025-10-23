using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Models;
using Tabligo.Domain.Models.Employee;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Department;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Employee;

public class EmployeesTypeaheadHandler(
    TabligoContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<PaginationDto<EmployeeDto>> HandleAsync(TypeaheadRequest request, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        const int COUNT = 25;

        var query = db.Employees
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId);

        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(e => e.FIO.ToLower().Contains(q) || (e.Email != null && e.Email.ToLower().Contains(q)));
        }

        var page = Math.Max(request.Page, 1);

        var employees = await query
            .Include(i => i.DepartmentLinks)
            .ThenInclude(i => i.Department)
            .Skip((page - 1) * COUNT)
            .Take(COUNT)
            .ToListAsync(ct);

        var count = await query.CountAsync(ct);

        var items = employees.Select(e => new EmployeeDto
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
            Departments = e.DepartmentLinks.Select(s => new DepartmentShortDto(s.Department.Id, s.Department.Name)).ToArray()
        }).ToList();

        return new PaginationDto<EmployeeDto>(items, count, (int)Math.Ceiling((float)count / COUNT));
    }
}
