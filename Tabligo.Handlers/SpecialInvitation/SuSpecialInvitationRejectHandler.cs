using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Entities;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Auth;
using Tabligo.Handlers.Email;

namespace Tabligo.Handlers.SpecialInvitation;

public class SuSpecialInvitationRejectHandler(
    TabligoContext db,
    IEmailQueue emailQueue,
    UserContextHandler userContextHandler)
{
    public async Task HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var invitation = await db.SpecialInvitations.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invitation == null) return;
        if (invitation.Granted) return;

        var currentUser = await userContextHandler.GetCurrentUserAsync(cancellationToken);

        await emailQueue.EnqueueAsync(new EmailMessageEntity
        {
            To = invitation.Email,
            Subject = "Регистрация отклонена",
            SentByCompanyId = currentUser.CompanyId,
            SentByUserId = userContextHandler.CurrentUserId,
            IsHtml = true,
            Body = "<p>Ваша заявка отклонена.</p>"
        });

        db.SpecialInvitations.Remove(invitation);
        await db.SaveChangesAsync(cancellationToken);
    }
}
