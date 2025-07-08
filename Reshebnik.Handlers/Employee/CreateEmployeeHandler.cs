using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Company;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Models.Employee;

namespace Reshebnik.Handlers.Employee;

public class CreateEmployeeHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<int> HandleAsync(EmployeeCreateDto dto, CancellationToken ct = default)
    {
        var companyId = dto.CompanyId != 0 ? dto.CompanyId : await companyContext.CurrentCompanyIdAsync;

        var entity = new EmployeeEntity
        {
            CompanyId = companyId,
            FIO = dto.Fio,
            JobTitle = dto.JobTitle,
            Email = dto.Email,
            Phone = dto.Phone,
            Comment = dto.Comment,
            IsActive = dto.IsActive,
            EmailInvitationCode = dto.EmailInvitationCode,
            Salt = dto.Salt,
            Role = dto.Role,
            CreatedAt = dto.CreatedAt,
            LastLoginAt = dto.LastLoginAt,
            Password = string.Empty
        };

        db.Employees.Add(entity);
        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}
