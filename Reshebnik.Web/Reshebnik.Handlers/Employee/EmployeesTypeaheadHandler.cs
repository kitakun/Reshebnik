using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Web.DTO.Employee;

namespace Reshebnik.Handlers.Employee;

public class EmployeesTypeaheadHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<PaginationDto<EmployeeDto>> HandleAsync(TypeaheadRequest request, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        const int COUNT = 50;

        var query = db.Employees
            .AsNoTracking()
            .Where(e => e.CompanyId == companyId)
            .Take(COUNT);

        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(e => e.FIO.ToLower().Contains(q));
        }

        var employees = await query
            .Skip((request.Page ?? 0) * COUNT)
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
            IsActive = e.IsActive
        }).ToList();

        return new PaginationDto<EmployeeDto>(items, count, (int)Math.Ceiling((float)count / COUNT));
    }
}
