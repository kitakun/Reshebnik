using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Auth;
using Reshebnik.Handlers.Email;

namespace Reshebnik.Handlers.SpecialInvitation;

public class SuSpecialInvitationAcceptHandler(
    ReshebnikContext db,
    SecurityHandler securityHandler,
    IEmailQueue emailQueue,
    UserContextHandler userContextHandler)
{
    public async Task HandleAsync(int id, CancellationToken cancellationToken = default)
    {
        var invitation = await db.SpecialInvitations.FirstOrDefaultAsync(i => i.Id == id, cancellationToken);
        if (invitation == null) return;
        if (invitation.Granted) return;

        var company = new CompanyEntity
        {
            Name = invitation.CompanyName,
            Industry = invitation.CompanyDescription,
            EmployeesCount = invitation.CompanySize,
            Type = CompanyTypeEnum.Unset,
            Email = invitation.Email,
            NotifyAboutLoweringMetrics = false,
            NotificationType = SystemNotificationTypeEnum.Unset,
            LanguageCode = "ru",
            Period = PeriodTypeEnum.Month,
            DefaultMetrics = string.Empty,
            AutoUpdateByApi = false,
            AllowForEmployeesEditMetrics = false,
            EnableNotificationsInApp = false,
            ShowNewMetrics = false
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync(cancellationToken);

        var password = securityHandler.GenerateSaltedHash(Guid.NewGuid().ToString("N"), out var salt);
        var employee = new EmployeeEntity
        {
            CompanyId = company.Id,
            FIO = invitation.FIO,
            JobTitle = "Owner",
            Email = invitation.Email,
            Phone = string.Empty,
            Comment = invitation.CompanyDescription,
            IsActive = true,
            EmailInvitationCode = Guid.NewGuid().ToString("N"),
            Password = password,
            Salt = salt,
            Role = RootRolesEnum.CompanyOwner,
            CreatedAt = DateTime.UtcNow
        };
        db.Employees.Add(employee);
        await db.SaveChangesAsync(cancellationToken);

        await emailQueue.EnqueueAsync(new EmailMessageEntity
        {
            To = invitation.Email,
            Subject = "Регистрация одобрена",
            SentByCompanyId = company.Id,
            SentByUserId = userContextHandler.CurrentUserId,
            IsHtml = true,
            Body = $"<p>Ваша заявка одобрена. Для завершения регистрации перейдите по <a href=\"https://tabligo.ru/invite?code={employee.EmailInvitationCode}\">ссылке</a></p>"
        });

        invitation.Granted = true;
        await db.SaveChangesAsync(cancellationToken);
    }
}
