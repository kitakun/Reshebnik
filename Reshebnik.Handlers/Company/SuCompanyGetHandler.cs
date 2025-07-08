using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Company;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Auth;

namespace Reshebnik.Handlers.Company;

public class SuCompanyGetHandler(
    ReshebnikContext db,
    UserContextHandler userContextHandler)
{
    public async ValueTask<CompanySettingsDto?> HandleAsync(int targetCompanyId, CancellationToken ct = default)
    {
        var suUser = await userContextHandler.GetCurrentUserAsync(ct);
        if (suUser.Role != RootRolesEnum.SuperAdmin)
        {
            // ! HACKER
            return null;
        }
        var companyId = targetCompanyId;
        if (companyId <= 0)
            return null;

        var entity = await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId, ct);
        if (entity == null)
            return null;

        return new CompanySettingsDto
        {
            CompanyName = entity.Name,
            Industry = entity.Industry ?? string.Empty,
            Size = entity.EmployeesCount.ToString(),
            LegalType = entity.Type,
            CompanyEmail = entity.Email,
            CompanyPhone = entity.Phone ?? string.Empty,
            NotifEmail = entity.NotifyAboutLoweringMetrics,
            NotifFrequency = entity.NotificationType,
            UiLanguage = entity.LanguageCode,
            //
            Period = entity.Period,
            AutoUpdateFromAPI = entity.AutoUpdateByApi,
            DefaultMetric = entity.DefaultMetrics,
            AllowForEmployeesEditMetrics = entity.AllowForEmployeesEditMetrics,
            NotifInApp = entity.EnableNotificationsInApp,
            ShowNewMetrics = entity.ShowNewMetrics,
        };
    }
}
