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

        entity.Type = dto.LegalType;

        entity.Email = dto.CompanyEmail;
        entity.Phone = dto.CompanyPhone;
        entity.NotifyAboutLoweringMetrics = dto.NotifEmail;

        entity.NotificationType = dto.NotifFrequency;

        entity.LanguageCode = dto.UiLanguage;

        entity.Period = dto.Period;
        entity.DefaultMetrics = dto.DefaultMetric;
        entity.ShowNewMetrics = dto.ShowNewMetrics;
        entity.AllowForEmployeesEditMetrics = dto.AllowForEmployeesEditMetrics;
        entity.EnableNotificationsInApp = dto.NotifInApp;
        entity.AutoUpdateByApi = dto.AutoUpdateFromAPI;

        await db.SaveChangesAsync(ct);
    }
}