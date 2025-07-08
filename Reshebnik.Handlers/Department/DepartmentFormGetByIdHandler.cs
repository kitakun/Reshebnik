using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Department;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Department;

public class DepartmentFormGetByIdHandler(ReshebnikContext db)
{
    public async ValueTask<DepartmentFormDto?> HandleAsync(int id, CancellationToken ct = default)
    {
        var dto = await db.Departments
            .AsNoTracking()
            .Where(d => d.Id == id && !d.IsDeleted)
            .Select(d => new DepartmentFormDto
            {
                Name = d.Name,
                SupervisorId = db.EmployeeDepartmentLinkEntities
                    .Where(l => l.DepartmentId == d.Id && l.Type == EmployeeTypeEnum.Supervisor)
                    .Select(l => (int?)l.EmployeeId)
                    .FirstOrDefault(),
                ParentId = db.DepartmentSchemaEntities
                    .Where(s => s.DepartmentId == d.Id && s.Depth == 1)
                    .Select(s => s.AncestorDepartmentId == 0 ? (int?)null : s.AncestorDepartmentId)
                    .FirstOrDefault(),
                EmployeeIds = db.EmployeeDepartmentLinkEntities
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
