using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Entities;
using Tabligo.Domain.Models.Department;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Department;

public class DepartmentPutOneHandler(
    TabligoContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<int> HandleAsync(DepartmentDto dto, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        DepartmentEntity entity;
        var isNew = dto.Id == 0;
        if (!isNew)
        {
            entity = await db.Departments.FirstOrDefaultAsync(d => d.Id == dto.Id, ct) ?? new DepartmentEntity
            {
                Comment = "",
                Name = "",
                CompanyId = companyId,
            };
            if (entity.Id == 0) db.Departments.Add(entity);
            if (entity.Id == 0) isNew = true;
        }
        else
        {
            entity = new DepartmentEntity
            {
                Comment = "",
                Name = "",
                CompanyId = companyId,
            };
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
                if (!await db.DepartmentSchemas.AnyAsync(s => s.DepartmentId == entity.Id && s.AncestorDepartmentId == entity.Id, ct))
                {
                    db.DepartmentSchemas.Add(new DepartmentSchemeEntity
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
        var parentSchemes = await db.DepartmentSchemas
            .Where(s => s.DepartmentId == parentId)
            .ToListAsync(ct);

        foreach (var sch in parentSchemes)
        {
            db.DepartmentSchemas.Add(new DepartmentSchemeEntity
            {
                FundamentalDepartmentId = sch.FundamentalDepartmentId,
                AncestorDepartmentId = sch.AncestorDepartmentId,
                DepartmentId = deptId,
                Depth = sch.Depth + 1
            });
        }

        db.DepartmentSchemas.Add(new DepartmentSchemeEntity
        {
            FundamentalDepartmentId = parentSchemes.First().FundamentalDepartmentId,
            AncestorDepartmentId = deptId,
            DepartmentId = deptId,
            Depth = 0
        });

        await db.SaveChangesAsync(ct);
    }
}
