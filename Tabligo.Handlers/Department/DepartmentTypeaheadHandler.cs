using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Models;
using Tabligo.Domain.Models.Department;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Department;

public class DepartmentTypeaheadHandler(
    TabligoContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<PaginationDto<DepartmentDto>> HandleAsync(TypeaheadRequest request, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        const int COUNT = 25;

        var rootIds = await db.Departments
            .Where(w => w.IsFundamental && w.CompanyId == companyId && !w.IsDeleted)
            .Select(s => s.Id)
            .ToListAsync(ct);

        var query = db.Departments
            .AsNoTracking()
            .Where(d => !d.IsDeleted && db.DepartmentSchemas.Any(s => rootIds.Contains(s.FundamentalDepartmentId) && s.DepartmentId == d.Id));

        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(q));
        }

        var page = Math.Max(request.Page, 1);

        var departments = await query
            .Skip((page - 1) * COUNT)
            .Take(COUNT)
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
