using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Company;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Company;

public class CompanySettingsUpdateHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async Task HandleAsync(CompanySettingsDto dto, CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var entity = await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (entity == null) return;

        entity.Name = dto.CompanyName;
        entity.Industry = string.IsNullOrEmpty(dto.Industry) ? null : dto.Industry;
        if (int.TryParse(dto.Size, out var size))
            entity.EmployeesCount = size;
        if (Enum.TryParse<CompanyTypeEnum>(dto.LegalType, out var type))
            entity.Type = type;
        entity.Email = dto.CompanyEmail;
        entity.Phone = dto.CompanyPhone;
        entity.NotifyAboutLoweringMetrics = dto.NotifEmail;
        if (Enum.TryParse<SystemNotificationTypeEnum>(dto.NotifFrequency, out var nt))
            entity.NotificationType = nt;
        entity.LanguageCode = dto.UiLanguage;

        await db.SaveChangesAsync(ct);
    }
}
