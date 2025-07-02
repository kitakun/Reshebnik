using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Department;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Department;

public class DepartmentPutHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async Task HandleAsync(DepartmentTreeDto request, CancellationToken cancellationToken = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var company = await db.Companies
            .Include(i => i.Departments)
            .FirstAsync(f => f.Id == companyId, cancellationToken);

        await UpsertAsync(request, null, company, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task UpsertAsync(DepartmentTreeDto dto, int? parentId, CompanyEntity company, CancellationToken ct)
    {
        var isRoot = parentId == null;
        DepartmentEntity entity;
        if (dto.Id.HasValue)
        {
            entity = await db
                .Departments
                .Include(i => i.LinkEntities)
                .FirstOrDefaultAsync(f => f.Id == dto.Id.Value, ct) ?? new DepartmentEntity();
            if (entity.Id == 0) db.Departments.Add(entity);
        }
        else
        {
            entity = new DepartmentEntity();
            db.Departments.Add(entity);
            entity.CompanyId = company.Id;
        }

        entity.Name = dto.Name;
        entity.Comment = dto.Comment;
        entity.IsActive = dto.IsActive;
        entity.IsFundamental = isRoot;

        if (isRoot && !company.Departments.Contains(entity))
        {
            company.Departments.Add(entity);
        }

        await db.SaveChangesAsync(ct);
        dto.Id = entity.Id;

        if (isRoot)
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
            }
            await db.SaveChangesAsync(ct);
        }
        else if (parentId.HasValue)
        {
            await AddSchemeAsync(entity.Id, parentId.Value, company, ct);
        }

        await UpsertUsersAsync(entity, dto.Users, companyId: company.Id, ct);

        foreach (var child in dto.Children)
        {
            await UpsertAsync(child, entity.Id, company, ct);
        }
    }

    private async Task UpsertUsersAsync(DepartmentEntity department, List<DepartmentUserDto> users, int companyId, CancellationToken ct)
    {
        var existingLinks = await db.EmployeeDepartmentLinkEntities
            .Include(l => l.Employee)
            .Where(l => l.DepartmentId == department.Id)
            .ToListAsync(ct);

        var processedIds = new HashSet<int>();

        foreach (var userDto in users)
        {
            EmployeeEntity employee;
            if (userDto.Id.HasValue)
            {
                employee = await db.Employees.FirstOrDefaultAsync(f => f.Id == userDto.Id.Value, ct) ?? new EmployeeEntity();
                if (employee.Id == 0) db.Employees.Add(employee);
            }
            else
            {
                employee = new EmployeeEntity
                {
                    CompanyId = companyId,
                    Password = string.Empty,
                    Salt = string.Empty,
                    Role = RootRolesEnum.Employee,
                    CreatedAt = DateTime.UtcNow,
                    LastLoginAt = DateTime.UtcNow
                };
                db.Employees.Add(employee);
            }

            employee.CompanyId = companyId;
            employee.FIO = userDto.Fio;
            employee.JobTitle = userDto.JobTitle;
            employee.Email = userDto.Email;
            employee.Phone = userDto.Phone;
            employee.Comment = userDto.Comment;
            employee.IsActive = userDto.IsActive;

            await db.SaveChangesAsync(ct);
            userDto.Id = employee.Id;
            processedIds.Add(employee.Id);

            var link = existingLinks.FirstOrDefault(l => l.EmployeeId == employee.Id);
            if (link == null)
            {
                link = new EmployeeDepartmentLinkEntity
                {
                    EmployeeId = employee.Id,
                    DepartmentId = department.Id
                };
                db.EmployeeDepartmentLinkEntities.Add(link);
            }
            link.Type = userDto.Type;
            await db.SaveChangesAsync(ct);
        }

        foreach (var link in existingLinks)
        {
            if (!processedIds.Contains(link.EmployeeId))
            {
                db.EmployeeDepartmentLinkEntities.Remove(link);
                var hasOtherLinks = await db
                    .EmployeeDepartmentLinkEntities
                    .AnyAsync(l => l.EmployeeId == link.EmployeeId && l.Id != link.Id, ct);
                if (!hasOtherLinks)
                {
                    db.Employees.Remove(link.Employee);
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private async Task AddSchemeAsync(int deptId, int parentId, CompanyEntity company, CancellationToken ct)
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
