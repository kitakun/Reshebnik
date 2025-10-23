using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Models.Company;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Company;

public class CompanyUpdateHandler(TabligoContext db)
{
    public async Task HandleAsync(CompanyDto dto, CancellationToken ct = default)
    {
        var entity = await db.Companies.FirstOrDefaultAsync(c => c.Id == dto.Id, ct);
        if (entity == null) return;

        entity.Name = dto.Name;
        entity.Industry = dto.Industry;
        entity.EmployeesCount = dto.EmployeesCount;
        entity.Type = dto.Type;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.NotifyAboutLoweringMetrics = dto.NotifyAboutLoweringMetrics;
        entity.NotificationType = dto.NotificationType;
        entity.LanguageCode = dto.LanguageCode;

        await db.SaveChangesAsync(ct);
    }
}
