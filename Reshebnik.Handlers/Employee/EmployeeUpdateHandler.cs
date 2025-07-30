using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models.Employee;
using Reshebnik.EntityFramework;
using Reshebnik.Domain.Extensions;

namespace Reshebnik.Handlers.Employee;

public class EmployeeUpdateHandler(ReshebnikContext db)
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
        // entity.EmailInvitationCode = dto.EmailInvitationCode;
        entity.Role = dto.Role;
        entity.CreatedAt = dto.CreatedAt.ToUtcFromClient();
        entity.LastLoginAt = dto.LastLoginAt.ToUtcFromClient();

        await db.SaveChangesAsync(ct);
    }
}
