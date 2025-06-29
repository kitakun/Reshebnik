using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models.Company;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Company;

public class CompanyGetHandler(
    ReshebnikContext db,
    CompanyContextHandler companyContext)
{
    public async ValueTask<CompanySettingsDto?> HandleAsync(CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
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
            LegalType = entity.Type.ToString(),
            CompanyEmail = entity.Email,
            CompanyPhone = entity.Phone ?? string.Empty,
            NotifEmail = entity.NotifyAboutLoweringMetrics,
            NotifFrequency = entity.NotificationType.ToString(),
            UiLanguage = entity.LanguageCode
        };
    }
}
