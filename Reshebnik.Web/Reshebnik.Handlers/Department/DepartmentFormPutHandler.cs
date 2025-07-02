using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Department;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Department;

public class DepartmentFormPutHandler(ReshebnikContext db)
{
    public async Task HandleAsync(int id, DepartmentFormDto dto, CancellationToken ct = default)
    {
        var department = await db.Departments
            .Include(d => d.LinkEntities)
            .FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, ct)
            ?? throw new Exception("not found");

        department.Name = dto.Name;
        department.Comment = dto.Comment;
        department.IsActive = dto.IsActive;
        department.IsFundamental = dto.ParentId == null;
        await db.SaveChangesAsync(ct);

        var existingSchemes = await db.DepartmentSchemaEntities
            .Where(s => s.DepartmentId == id)
            .ToListAsync(ct);
        db.DepartmentSchemaEntities.RemoveRange(existingSchemes);
        await db.SaveChangesAsync(ct);

        if (dto.ParentId.HasValue)
        {
            var parentSchemes = await db.DepartmentSchemaEntities
                .Where(s => s.DepartmentId == dto.ParentId.Value)
                .ToListAsync(ct);

            foreach (var sch in parentSchemes)
            {
                db.DepartmentSchemaEntities.Add(new DepartmentSchemeEntity
                {
                    FundamentalDepartmentId = sch.FundamentalDepartmentId,
                    AncestorDepartmentId = sch.AncestorDepartmentId,
                    DepartmentId = id,
                    Depth = sch.Depth + 1
                });
            }

            db.DepartmentSchemaEntities.Add(new DepartmentSchemeEntity
            {
                FundamentalDepartmentId = parentSchemes.First().FundamentalDepartmentId,
                AncestorDepartmentId = id,
                DepartmentId = id,
                Depth = 0
            });
        }
        else
        {
            db.DepartmentSchemaEntities.Add(new DepartmentSchemeEntity
            {
                FundamentalDepartmentId = id,
                AncestorDepartmentId = id,
                DepartmentId = id,
                Depth = 0
            });
        }

        await db.SaveChangesAsync(ct);

        var existingLinks = await db.EmployeeDepartmentLinkEntities
            .Where(l => l.DepartmentId == id)
            .ToListAsync(ct);
        db.EmployeeDepartmentLinkEntities.RemoveRange(existingLinks);
        await db.SaveChangesAsync(ct);

        if (dto.SupervisorId.HasValue)
        {
            db.EmployeeDepartmentLinkEntities.Add(new EmployeeDepartmentLinkEntity
            {
                EmployeeId = dto.SupervisorId.Value,
                DepartmentId = id,
                Type = EmployeeTypeEnum.Supervisor
            });
        }

        foreach (var empId in dto.EmployeeIds.Distinct())
        {
            db.EmployeeDepartmentLinkEntities.Add(new EmployeeDepartmentLinkEntity
            {
                EmployeeId = empId,
                DepartmentId = id,
                Type = EmployeeTypeEnum.Employee
            });
        }

        await db.SaveChangesAsync(ct);
    }
}
