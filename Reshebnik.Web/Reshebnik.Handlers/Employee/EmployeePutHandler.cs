using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Employee;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Auth;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Employee;

public class EmployeePutHandler(
    ReshebnikContext db,
    SecurityHandler securityHandler,
    CompanyContextHandler companyContext)
{
    public async ValueTask<int> HandleAsync(EmployeePutDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(dto.Fio)) throw new ArgumentNullException(nameof(dto.Fio));

        EmployeeEntity entity;
        if (dto.Id != 0)
        {
            entity = await db
                .Employees
                .Include(i => i.DepartmentLinks)
                .FirstOrDefaultAsync(e => e.Id == dto.Id, ct) ?? throw new Exception("not found");

            var departmentLink = entity.DepartmentLinks.FirstOrDefault(f => f.DepartmentId == dto.DepartmentId);
            if (departmentLink != null)
            {
                departmentLink.DepartmentId = dto.DepartmentId!.Value;
                departmentLink.Type = dto.IsSupervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee;
            }
            else
            {
                db.EmployeeDepartmentLinkEntities.Add(new EmployeeDepartmentLinkEntity
                {
                    EmployeeId = dto.Id,
                    DepartmentId = dto.DepartmentId!.Value,
                    Type = dto.IsSupervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee,
                });
            }
        }
        else
        {
            entity = new EmployeeEntity();

            var pass = securityHandler.GenerateSaltedHash(Guid.NewGuid().ToString("N"), out var salt);
            entity.Password = pass;
            entity.Salt = salt;
            entity.CreatedAt = DateTime.UtcNow;
            entity.DefaultRole = dto.IsSupervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee;

            db.Employees.Add(entity);

            entity.CompanyId = await companyContext.CurrentCompanyIdAsync;
            if (dto.DepartmentId.HasValue)
            {
                entity.DepartmentLinks.Add(new EmployeeDepartmentLinkEntity
                {
                    Employee = entity,
                    Type = dto.IsSupervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee
                });
            }
        }

        entity.FIO = dto.Fio;
        entity.JobTitle = dto.JobTitle;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.Comment = dto.Comment;
        entity.IsActive = dto.IsActive;

        if (dto.SendEmail)
        {
            entity.EmailInvitationCode = Guid.NewGuid().ToString("N");
        }

        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}