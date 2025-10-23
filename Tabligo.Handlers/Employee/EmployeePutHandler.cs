using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Entities;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Employee;
using Tabligo.Domain.Exceptions;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Auth;
using Tabligo.Handlers.Company;
using Tabligo.Handlers.Email;

namespace Tabligo.Handlers.Employee;

public class EmployeePutHandler(
    TabligoContext db,
    SecurityHandler securityHandler,
    IEmailQueue emailQueue,
    UserContextHandler userContextHandler,
    CompanyContextHandler companyContext)
{
    public async ValueTask<int> HandleAsync(EmployeePutDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(dto.Fio)) throw new ArgumentNullException(nameof(dto.Fio));

        if (!string.IsNullOrWhiteSpace(dto.Email))
        {
            var normalizedEmail = dto.Email.ToLower();
            var emailExists = await db.Employees
                .AsNoTracking()
                .AnyAsync(e => e.Email != null && e.Email.ToLower() == normalizedEmail && e.Id != dto.Id, ct);
            if (emailExists)
                throw new EmailAlreadyExistsException();
        }

        EmployeeEntity entity;
        if (dto.Id != 0)
        {
            entity = await db
                .Employees
                .Include(i => i.DepartmentLinks)
                .FirstOrDefaultAsync(e => e.Id == dto.Id, ct) ?? throw new Exception("not found");

            entity.DepartmentLinks.Clear();
            entity.DepartmentLinks.AddRange(dto.DepartmentIds.Select(s => new EmployeeDepartmentLinkEntity
            {
                Employee = entity,
                DepartmentId = s,
                Type = dto.IsSupervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee
            }));
        }
        else
        {
            entity = new EmployeeEntity();

            var pass = securityHandler.GenerateSaltedHash(Guid.NewGuid().ToString("N"), out var salt);
            entity.Password = pass;
            entity.Salt = salt;
            entity.CreatedAt = DateTime.UtcNow;
            entity.DefaultRole = dto.IsSupervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee;

            db.Employees.Add(entity);

            entity.CompanyId = await companyContext.CurrentCompanyIdAsync;
            if (dto.DepartmentIds.Length > 0)
            {
                entity.DepartmentLinks.AddRange(dto.DepartmentIds.Select(s => new EmployeeDepartmentLinkEntity
                {
                    Employee = entity,
                    DepartmentId = s,
                    Type = dto.IsSupervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee
                }));
            }
        }

        entity.FIO = dto.Fio;
        entity.JobTitle = dto.JobTitle;
        entity.Email = dto.Email;
        entity.Phone = dto.Phone;
        entity.Comment = dto.Comment;
        entity.IsActive = dto.IsActive;
        entity.DefaultRole = dto.IsSupervisor ? EmployeeTypeEnum.Supervisor : EmployeeTypeEnum.Employee;

        if (dto.SendEmail && !string.IsNullOrWhiteSpace(dto.Email))
        {
            entity.EmailInvitationCode = Guid.NewGuid().ToString("N");

            await emailQueue.EnqueueAsync(new EmailMessageEntity
            {
                To = dto.Email,
                Subject = "Регистрация tabligo.ru",
                SentByCompanyId = entity.CompanyId,
                SentByUserId = userContextHandler.CurrentUserId,
                IsHtml = true,
                Body = $"""
                       <h2>Добрый день!</h2>
                       <p>Вас зарегистрировали на портале tabligo.ru.</p>
                       
                       <p>Для продолжения регистрации перейдите по <a href="https://tabligo.ru/invite?code={entity.EmailInvitationCode}">ссылке</a> и введите ваш пароль для входа</p>
                       
                       tabligo {DateTime.Now.Year} 
                       """
            });
        }

        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}