using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Entities;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Email;

namespace Tabligo.Handlers.Auth;

public class AuthResetPasswordHandler(
    TabligoContext context,
    IEmailQueue emailQueue)
{
    public async ValueTask<bool> HandleAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;

        var existingUser = await context.Employees
            .FirstOrDefaultAsync(e => e.Email != null && e.Email.ToLower() == email.ToLower(), cancellationToken);
        if (existingUser is not { IsActive: true }) return false;

        existingUser.EmailInvitationCode = Guid.NewGuid().ToString("N");

        await emailQueue.EnqueueAsync(new EmailMessageEntity
        {
            To = existingUser.Email!,
            Subject = "Восстановление пароля tabligo.ru",
            SentByCompanyId = existingUser.CompanyId,
            SentByUserId = existingUser.Id,
            IsHtml = true,
            Body = $"<p>Для восстановления пароля перейдите по <a href=\"https://tabligo.ru/reset-password?code={existingUser.EmailInvitationCode}\">ссылке</a></p>"
        });

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }
}

