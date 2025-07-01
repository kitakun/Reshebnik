using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Models.Department;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Department;

public class DepartmentPutOneHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<int> HandleAsync(DepartmentDto dto, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        DepartmentEntity entity;
        var isNew = dto.Id == 0;
        if (!isNew)
        {
            entity = await db.Departments.FirstOrDefaultAsync(d => d.Id == dto.Id, ct) ?? new DepartmentEntity();
            if (entity.Id == 0) db.Departments.Add(entity);
            if (entity.Id == 0) isNew = true;
        }
        else
        {
            entity = new DepartmentEntity();
            db.Departments.Add(entity);
            isNew = true;
        }

        entity.CompanyId = companyId;
        entity.Name = dto.Name;
        entity.Comment = dto.Comment;
        entity.IsActive = dto.IsActive;
        entity.IsFundamental = dto.ParentId == null;
        entity.IsDeleted = false;

        await db.SaveChangesAsync(ct);
        dto.Id = entity.Id;

        if (isNew)
        {
            if (entity.IsFundamental)
            {
                if (!await db.DepartmentSchemaEntities.AnyAsync(s => s.DepartmentId == entity.Id && s.AncestorDepartmentId == entity.Id, ct))
                {
                    db.DepartmentSchemaEntities.Add(new DepartmentSchemeEntity
                    {
                        FundamentalDepartmentId = entity.Id,
                        AncestorDepartmentId = entity.Id,
                        DepartmentId = entity.Id,
                        Depth = 0
                    });
                    await db.SaveChangesAsync(ct);
                }
            }
            else if (dto.ParentId.HasValue)
            {
                await AddSchemeAsync(entity.Id, dto.ParentId.Value, ct);
            }
        }

        return entity.Id;
    }

    private async Task AddSchemeAsync(int deptId, int parentId, CancellationToken ct)
    {
        var parentSchemes = await db.DepartmentSchemaEntities
            .Where(s => s.DepartmentId == parentId)
            .ToListAsync(ct);

        foreach (var sch in parentSchemes)
        {
            db.DepartmentSchemaEntities.Add(new DepartmentSchemeEntity
            {
                FundamentalDepartmentId = sch.FundamentalDepartmentId,
                AncestorDepartmentId = sch.AncestorDepartmentId,
                DepartmentId = deptId,
                Depth = sch.Depth + 1
            });
        }

        db.DepartmentSchemaEntities.Add(new DepartmentSchemeEntity
        {
            FundamentalDepartmentId = parentSchemes.First().FundamentalDepartmentId,
            AncestorDepartmentId = deptId,
            DepartmentId = deptId,
            Depth = 0
        });

        await db.SaveChangesAsync(ct);
    }
}
