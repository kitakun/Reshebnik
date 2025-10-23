using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Department;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Department;

public class DepartmentFormGetByIdHandler(TabligoContext db)
{
    public async ValueTask<DepartmentFormDto?> HandleAsync(int id, CancellationToken ct = default)
    {
        var dto = await db.Departments
            .AsNoTracking()
            .Where(d => d.Id == id && !d.IsDeleted)
            .Select(d => new DepartmentFormDto
            {
                Name = d.Name,
                SupervisorIds = db.EmployeeDepartmentLinks
                    .Where(l => l.DepartmentId == d.Id && l.Type == EmployeeTypeEnum.Supervisor)
                    .Select(l => l.EmployeeId)
                    .ToList(),
                ParentId = db.DepartmentSchemas
                    .Where(s => s.DepartmentId == d.Id && s.Depth == 1)
                    .Select(s => s.AncestorDepartmentId == 0 ? (int?)null : s.AncestorDepartmentId)
                    .FirstOrDefault(),
                EmployeeIds = db.EmployeeDepartmentLinks
                    .Where(l => l.DepartmentId == d.Id && l.Type == EmployeeTypeEnum.Employee)
                    .Select(l => l.EmployeeId)
                    .ToList(),
                Comment = d.Comment,
                IsActive = d.IsActive
            })
            .FirstOrDefaultAsync(ct);

        return dto;
    }
}
