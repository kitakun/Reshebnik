using Microsoft.EntityFrameworkCore;

using Reshebnik.EntityFramework;
using Reshebnik.Web.DTO.Employee;

namespace Reshebnik.Handlers.Employee;

public class EmployeeUpdateHandler(ReshebnikContext db)
{
    public async Task HandleAsync(EmployeeFullDto dto, CancellationToken ct = default)
    {
        var entity = await db.Employees.FirstOrDefaultAsync(e => e.Id == dto.Id, ct);
        if (entity == null) return;

        entity.CompanyId = dto.CompanyId;
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
    }
}
