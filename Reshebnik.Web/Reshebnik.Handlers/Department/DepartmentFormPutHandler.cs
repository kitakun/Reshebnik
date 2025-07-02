using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Department;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

using System.Data;

namespace Reshebnik.Handlers.Department;

public class DepartmentFormPutHandler(ReshebnikContext db, CompanyContextHandler contextHandler)
{
    public async Task HandleAsync(int? id, DepartmentFormDto dto, CancellationToken ct = default)
    {
        var isNew = id.HasValue == false;
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellationToken: ct);
        try
        {
            var department = !isNew
                ? await db.Departments
                    .Include(d => d.LinkEntities)
                    .FirstAsync(d => d.Id == id && !d.IsDeleted, ct)
                : db.Departments.Add(new DepartmentEntity
                {
                    CompanyId = await contextHandler.CurrentCompanyIdAsync,
                    Comment = dto.Comment,
                    Name = dto.Name,
                }).Entity;
            if (isNew)
            {
                await db.SaveChangesAsync(ct);
                id = department!.Id;
            }

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
                        DepartmentId = id!.Value,
                        Depth = sch.Depth + 1
                    });
                }

                db.DepartmentSchemaEntities.Add(new DepartmentSchemeEntity
                {
                    FundamentalDepartmentId = parentSchemes.First().FundamentalDepartmentId,
                    AncestorDepartmentId = id!.Value,
                    DepartmentId = id!.Value,
                    Depth = 0
                });
            }
            else
            {
                db.DepartmentSchemaEntities.Add(new DepartmentSchemeEntity
                {
                    FundamentalDepartmentId = id!.Value,
                    AncestorDepartmentId = id!.Value,
                    DepartmentId = id!.Value,
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
                    DepartmentId = id!.Value,
                    Type = EmployeeTypeEnum.Supervisor
                });
            }

            foreach (var empId in dto.EmployeeIds.Distinct())
            {
                db.EmployeeDepartmentLinkEntities.Add(new EmployeeDepartmentLinkEntity
                {
                    EmployeeId = empId,
                    DepartmentId = id!.Value,
                    Type = EmployeeTypeEnum.Employee
                });
            }

            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
    }
}
