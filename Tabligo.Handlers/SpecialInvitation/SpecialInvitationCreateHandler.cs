using Tabligo.Domain.Entities;
using Tabligo.Domain.Models.SpecialInvitation;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.SpecialInvitation;

public class SpecialInvitationCreateHandler(TabligoContext db)
{
    public async Task<int> HandleAsync(SpecialInvitationCreateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new SpecialInvitationEntity
        {
            FIO = dto.Fio,
            Email = dto.Email,
            CompanyName = dto.CompanyName,
            CompanyDescription = dto.CompanyDescription,
            CompanySize = dto.CompanySize,
            CreatedAt = DateTime.UtcNow,
            Granted = false
        };
        db.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
