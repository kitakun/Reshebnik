using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Models.Employee;
using Tabligo.EntityFramework;
using Tabligo.Domain.Enums;

namespace Tabligo.Handlers.Employee;

public class EmployeeUpdateHandler(TabligoContext db)
{
    public async Task HandleAsync(EmployeeFullDto dto, CancellationToken ct = default)
    {
        var entity = await db.Employees.FirstOrDefaultAsync(e => e.Id == dto.Id, ct);
        if (entity == null) return;

        entity.FIO = dto.Fio;
        entity.JobTitle = dto.JobTitle;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.Comment = dto.Comment;
        entity.IsActive = dto.IsActive;
        entity.DefaultRole = dto.IsSupervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee;
        // entity.EmailInvitationCode = dto.EmailInvitationCode;
        entity.Role = dto.Role;
        entity.CreatedAt = dto.CreatedAt;
        entity.LastLoginAt = dto.LastLoginAt;

        await db.SaveChangesAsync(ct);
    }
}
