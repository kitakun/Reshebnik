using Microsoft.EntityFrameworkCore;
using Reshebnik.EntityFramework;
using Reshebnik.Web.DTO.Employee;

namespace Reshebnik.Handlers.Employee;

public class EmployeeGetByIdHandler(ReshebnikContext db)
{
    public async ValueTask<EmployeeFullDto?> HandleAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id, ct);
        return entity == null
            ? null
            : new EmployeeFullDto
            {
                Id = entity.Id,
                CompanyId = entity.CompanyId,
                Fio = entity.FIO,
                JobTitle = entity.JobTitle,
                Email = entity.Email,
                Phone = entity.Phone,
                Comment = entity.Comment,
                IsActive = entity.IsActive,
                EmailInvitationCode = entity.EmailInvitationCode,
                Password = entity.Password,
                Salt = entity.Salt,
                Role = entity.Role,
                CreatedAt = entity.CreatedAt,
                LastLoginAt = entity.LastLoginAt
            };
    }
}
