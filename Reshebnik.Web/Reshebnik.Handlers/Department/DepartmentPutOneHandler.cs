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
        if (dto.Id != 0)
        {
            entity = await db.Departments.FirstOrDefaultAsync(d => d.Id == dto.Id, ct) ?? new DepartmentEntity();
            if (entity.Id == 0) db.Departments.Add(entity);
        }
        else
        {
            entity = new DepartmentEntity();
            db.Departments.Add(entity);
        }

        entity.CompanyId = companyId;
        entity.Name = dto.Name;
        entity.Comment = dto.Comment;
        entity.IsActive = dto.IsActive;
        entity.IsFundamental = dto.IsFundamental;

        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
