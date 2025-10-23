using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Models.Company;
using Tabligo.Domain.Models.Employee;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Department;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Employee;

public class EmployeeGetByIdHandler(TabligoContext db)
{
    public async ValueTask<EmployeeFullDto?> HandleAsync(int id, CancellationToken ct = default)
    {
        var entity = await db.Employees
            .AsNoTracking()
            .Include(i => i.Company)
            .Include(employeeEntity => employeeEntity.DepartmentLinks)
            .Select(entity => new EmployeeFullDto
            {
                Id = entity.Id,
                Fio = entity.FIO,
                JobTitle = entity.JobTitle,
                Email = entity.Email,
                Phone = entity.Phone,
                Comment = entity.Comment,
                IsActive = entity.IsActive,
                IsSupervisor = entity.DefaultRole == EmployeeTypeEnum.Supervisor,
                Role = entity.Role,
                CreatedAt = entity.CreatedAt,
                LastLoginAt = entity.LastLoginAt,
                Departments = entity.DepartmentLinks.Select(s => new DepartmentShortDto(s.Department.Id, s.Department.Name)).ToArray(),
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
            })
            .FirstOrDefaultAsync(w => w.Id == id, ct);
        return entity;
    }
}