using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Department;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Department;

public class DepartmentTypeaheadHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<PaginationDto<DepartmentDto>> HandleAsync(TypeaheadRequest request, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        const int COUNT = 50;

        var rootIds = await db.Departments
            .Where(w => w.IsFundamental && w.CompanyId == companyId && !w.IsDeleted)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var query = db.Departments
            .AsNoTracking()
            .Where(d => !d.IsDeleted && db.DepartmentSchemaEntities.Any(s => rootIds.Contains(s.FundamentalDepartmentId) && s.DepartmentId == d.Id))
            .Take(COUNT);

        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(q));
        }

        var departments = await query
            .Skip((request.Page ?? 0) * COUNT)
            .ToListAsync(ct);

        var count = await query.CountAsync(ct);

        var items = departments.Select(d => new DepartmentDto
        {
            Id = d.Id,
            Name = d.Name,
            Comment = d.Comment,
            IsActive = d.IsActive,
            IsFundamental = d.IsFundamental
        }).ToList();

        return new PaginationDto<DepartmentDto>(items, count, (int)Math.Ceiling((float)count / COUNT));
    }
}
