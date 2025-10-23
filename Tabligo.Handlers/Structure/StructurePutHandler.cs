using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Models.Structure;
using Tabligo.Domain.Models.Department;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Department;
using Tabligo.Handlers.Company;

namespace Tabligo.Handlers.Structure;

public class StructurePutHandler(
    TabligoContext db,
    DepartmentPutHandler departmentHandler,
    DepartmentDeleteHandler deleteHandler,
    CompanyContextHandler companyContext)
{
    public async Task HandleAsync(OrgStructureDto dto, CancellationToken ct = default)
    {
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var companyId = await companyContext.CurrentCompanyIdAsync;

            var providedIds = new HashSet<int>();
            foreach (var root in dto.Departments)
            {
                CollectIds(root, providedIds);
            }

            var existingIds = await db.Departments
                .Where(d => d.CompanyId == companyId && !d.IsDeleted)
                .Select(d => d.Id)
                .ToListAsync(ct);

            var toDelete = existingIds.Where(id => !providedIds.Contains(id)).ToList();
            if (toDelete.Count > 0)
            {
                await deleteHandler.HandleManyAsync(toDelete, ct);
            }

            var companyDeptIds = await db.Departments
                .Where(d => d.CompanyId == companyId)
                .Select(d => d.Id)
                .ToListAsync(ct);

            await db.DepartmentSchemas
                .Where(s => companyDeptIds.Contains(s.FundamentalDepartmentId))
                .ExecuteDeleteAsync(ct);
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);


            foreach (var root in dto.Departments)
            {
                var mapped = MapUnit(root);
                await departmentHandler.HandleAsync(mapped, ct);
            }
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
        }
    }

    private static DepartmentTreeDto MapUnit(OrgUnitDto unit)
    {
        return new DepartmentTreeDto
        {
            Id = unit.Id == 0 ? null : unit.Id,
            Name = unit.Name,
            Comment = string.Empty,
            IsActive = true,
            IsFundamental = false,
            Children = unit.Children?.Select(MapUnit).ToList() ?? new()
        };
    }

    private static void CollectIds(OrgUnitDto unit, HashSet<int> set)
    {
        if (unit.Id != 0)
            set.Add(unit.Id);
        if (unit.Children == null) return;
        foreach (var child in unit.Children)
        {
            CollectIds(child, set);
        }
    }
}