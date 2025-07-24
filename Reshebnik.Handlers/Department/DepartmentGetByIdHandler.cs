using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Models.Department;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Department;

public class DepartmentGetByIdHandler(ReshebnikContext db)
{
    public async ValueTask<DepartmentDto?> HandleAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id && !d.IsDeleted, ct);
        if (entity == null) return null;

        int? parentId = await db.DepartmentSchemas
            .Where(s => s.DepartmentId == entity.Id && s.Depth == 1)
            .Select(s => s.AncestorDepartmentId)
            .FirstOrDefaultAsync(ct);
        return new DepartmentDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Comment = entity.Comment,
            IsActive = entity.IsActive,
            IsFundamental = entity.IsFundamental,
            ParentId = parentId == 0 ? null : parentId
        };
    }
}
