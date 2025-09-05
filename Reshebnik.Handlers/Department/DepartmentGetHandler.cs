using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models.Department;
using Reshebnik.Domain.Enums;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Department;

public class DepartmentGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<List<DepartmentTreeDto>> HandleAsync(CancellationToken cancellationToken = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        if (companyId <= 0) return [];

        var roots = await db.Departments
            .AsNoTracking()
            .Where(w => w.IsFundamental && w.CompanyId == companyId && !w.IsDeleted)
            .ToListAsync(cancellationToken);

        var result = new List<DepartmentTreeDto>();
        foreach (var root in roots)
        {
            var dto = await BuildTreeAsync(root.Id, cancellationToken);
            if (dto != null)
            {
                result.Add(dto);
            }
        }

        return result;
    }

    private async Task<DepartmentTreeDto?> BuildTreeAsync(int rootId, CancellationToken ct)
    {
        var departments = await db.Departments
            .AsNoTracking()
            .Where(d => !d.IsDeleted && db.DepartmentSchemas.Any(s => s.FundamentalDepartmentId == rootId && s.DepartmentId == d.Id))
            .ToListAsync(ct);

        var links = await db.DepartmentSchemas
            .AsNoTracking()
            .Where(s => s.FundamentalDepartmentId == rootId && s.Depth == 1 && s.DepartmentId != s.AncestorDepartmentId)
            .ToListAsync(ct);

        var usersLinks = await db.EmployeeDepartmentLinks
            .AsNoTracking()
            .Include(i => i.Employee)
            .Where(l => db.DepartmentSchemas.Any(s => s.FundamentalDepartmentId == rootId && s.DepartmentId == l.DepartmentId))
            .ToListAsync(ct);

        var dict = departments.ToDictionary(d => d.Id, d => new DepartmentTreeDto
        {
            Id = d.Id,
            Name = d.Name,
            Comment = d.Comment,
            IsActive = d.IsActive,
            IsFundamental = d.IsFundamental
        });

        foreach (var user in usersLinks)
        {
            var dto = dict[user.DepartmentId];
            dto.Users.Add(new DepartmentUserDto
            {
                Id = user.EmployeeId,
                Fio = user.Employee.FIO,
                JobTitle = user.Employee.JobTitle,
                Email = user.Employee.Email,
                Phone = user.Employee.Phone,
                Comment = user.Employee.Comment,
                IsActive = user.Employee.IsActive,
                IsSupervisor = user.Type == EmployeeTypeEnum.Supervisor,
                Type = user.Type
            });
        }

        foreach (var link in links)
        {
            if (dict.TryGetValue(link.AncestorDepartmentId, out var parent) && dict.TryGetValue(link.DepartmentId, out var child))
            {
                parent.Children.Add(child);
            }
        }

        return dict.GetValueOrDefault(rootId);
    }
}