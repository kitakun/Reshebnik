using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Entities;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Department;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Department;

public class DepartmentEmployeesUpsertHandler(TabligoContext db)
{
    public async Task HandleAsync(DepartmentEmployeesUpsertDto dto, CancellationToken ct = default)
    {
        var department = await db.Departments
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.Id == dto.DepartmentId && !d.IsDeleted, ct);
        if (department == null) return;

        var allIds = dto.SupervisorIds.Concat(dto.EmployeeIds).Distinct().ToList();
        var existingLinks = await db.EmployeeDepartmentLinks
            .Where(l => l.DepartmentId == dto.DepartmentId && allIds.Contains(l.EmployeeId))
            .ToListAsync(ct);

        foreach (var id in dto.SupervisorIds.Distinct())
        {
            var link = existingLinks.FirstOrDefault(l => l.EmployeeId == id);
            if (link == null)
            {
                db.EmployeeDepartmentLinks.Add(new EmployeeDepartmentLinkEntity
                {
                    EmployeeId = id,
                    DepartmentId = dto.DepartmentId,
                    Type = EmployeeTypeEnum.Supervisor
                });
            }
            else
            {
                link.Type = EmployeeTypeEnum.Supervisor;
            }
        }

        foreach (var id in dto.EmployeeIds.Distinct())
        {
            var link = existingLinks.FirstOrDefault(l => l.EmployeeId == id);
            if (link == null)
            {
                db.EmployeeDepartmentLinks.Add(new EmployeeDepartmentLinkEntity
                {
                    EmployeeId = id,
                    DepartmentId = dto.DepartmentId,
                    Type = EmployeeTypeEnum.Employee
                });
            }
            else
            {
                link.Type = EmployeeTypeEnum.Employee;
            }
        }

        await db.SaveChangesAsync(ct);
    }
}
