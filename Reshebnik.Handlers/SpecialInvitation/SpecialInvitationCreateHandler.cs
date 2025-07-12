using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Models.SpecialInvitation;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.SpecialInvitation;

public class SpecialInvitationCreateHandler(ReshebnikContext db)
{
    public async Task<int> HandleAsync(SpecialInvitationCreateDto dto, CancellationToken cancellationToken = default)
    {
        var entity = new SpecialInvitationEntity
        {
            FIO = dto.Fio,
            Email = dto.Email,
            CompanyName = dto.CompanyName,
            CompanyDescription = dto.CompanyDescription,
            CreatedAt = DateTime.UtcNow,
            Granted = false
        };
        db.Add(entity);
        await db.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }
}
