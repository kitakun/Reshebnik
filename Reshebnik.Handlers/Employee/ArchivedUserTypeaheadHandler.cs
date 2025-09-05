using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Employee;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Employee;

public class ArchivedUserTypeaheadHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<PaginationDto<ArchivedUserDto>> HandleAsync(TypeaheadRequest request, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        const int COUNT = 25;

        var query = db.ArchivedUsers
            .AsNoTracking()
            .IgnoreQueryFilters()
            .Where(a => a.CompanyId == companyId);

        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(a => a.Employee.FIO.ToLower().Contains(q));
        }

        query = query.Include(a => a.Employee);

        var page = Math.Max(request.Page, 1);

        var itemsQuery = query
            .Skip((page - 1) * COUNT)
            .Take(COUNT);

        var users = await itemsQuery
            .Select(a => new ArchivedUserDto
            {
                Id = a.Id,
                Name = a.Employee.FIO,
                ArchivedAt = a.ArchivedAt,
                JobTitle = a.Employee.JobTitle,
                EmployeeId = a.EmployeeId
            })
            .ToListAsync(ct);
        var count = await query.CountAsync(ct);

        return new PaginationDto<ArchivedUserDto>(users, count, (int)Math.Ceiling((float)count / COUNT));
    }
}
