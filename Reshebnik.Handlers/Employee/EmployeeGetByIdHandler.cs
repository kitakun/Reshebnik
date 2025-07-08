using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Models.Company;
using Reshebnik.Domain.Models.Employee;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.Employee;

public class EmployeeGetByIdHandler(ReshebnikContext db)
{
    public async ValueTask<EmployeeFullDto?> HandleAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Employees
            .AsNoTracking()
            .Include(i => i.Company)
            .FirstOrDefaultAsync(e => e.Id == id, ct);
        return entity == null
            ? null
            : new EmployeeFullDto
            {
                Id = entity.Id,
                Fio = entity.FIO,
                JobTitle = entity.JobTitle,
                Email = entity.Email,
                Phone = entity.Phone,
                Comment = entity.Comment,
                IsActive = entity.IsActive,
                Role = entity.Role,
                CreatedAt = entity.CreatedAt,
                LastLoginAt = entity.LastLoginAt,
                Company = new CompanyDto
                {
                    Industry = entity.Company.Industry,
                    Name = entity.Company.Name,
                    Phone = entity.Company.Phone,
                    Type = entity.Company.Type,
                    EmployeesCount = entity.Company.EmployeesCount,
                    LanguageCode = entity.Company.LanguageCode,
                    NotificationType = entity.Company.NotificationType,
                    NotifyAboutLoweringMetrics = entity.Company.NotifyAboutLoweringMetrics,
                    Email = entity.Company.Email,
                }
            };
    }
}
