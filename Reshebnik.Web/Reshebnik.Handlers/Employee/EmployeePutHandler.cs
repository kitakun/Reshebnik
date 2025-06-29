using Microsoft.EntityFrameworkCore;
using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Models.Employee;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Handlers.Employee;

public class EmployeePutHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<int> HandleAsync(EmployeeFullDto dto, CancellationToken ct = default)
    {
        EmployeeEntity entity;
        if (dto.Id != 0)
        {
            entity = await db.Employees.FirstOrDefaultAsync(e => e.Id == dto.Id, ct) ?? new EmployeeEntity();
            if (entity.Id == 0) db.Employees.Add(entity);
        }
        else
        {
            entity = new EmployeeEntity();
            db.Employees.Add(entity);
        }

        entity.CompanyId = dto.CompanyId != 0 ? dto.CompanyId : await companyContext.CurrentCompanyIdAsync;
        entity.FIO = dto.Fio;
        entity.JobTitle = dto.JobTitle;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.Comment = dto.Comment;
        entity.IsActive = dto.IsActive;
        entity.EmailInvitationCode = dto.EmailInvitationCode;
        entity.Password = dto.Password;
        entity.Salt = dto.Salt;
        entity.Role = dto.Role;
        entity.CreatedAt = dto.CreatedAt;
        entity.LastLoginAt = dto.LastLoginAt;

        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
