using Microsoft.EntityFrameworkCore;

using Reshebnik.EntityFramework;
using Reshebnik.Web.DTO.Company;

namespace Reshebnik.Handlers.Company;

public class CompanyUpdateHandler(ReshebnikContext db)
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
