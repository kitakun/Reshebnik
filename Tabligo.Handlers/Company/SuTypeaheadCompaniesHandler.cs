using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Entities;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Company;

public class SuTypeaheadCompaniesHandler(
    IHttpContextAccessor httpContextAccessor,
    TabligoContext dbContext)
{
    public async ValueTask<PaginationDto<CompanyEntity>?> HandlerAsync(TypeaheadRequest command, CancellationToken cancellationToken = default)
    {
        if (httpContextAccessor.HttpContext == null) return null;
        var roleIdClaim = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == "user-role");
        if (roleIdClaim == null) return null;
        if (roleIdClaim.Value != nameof(RootRolesEnum.SuperAdmin)) return null;

        const int COUNT = 25;

        var query = dbContext
            .Companies
            .AsNoTracking();

        if (!string.IsNullOrEmpty(command.Query))
        {
            var q = command.Query.ToLower();
            query = query.Where(w => w.Name.ToLower().Contains(q));
        }

        var page = Math.Max(command.Page, 1);

        var companies = await query
            .Skip((page - 1) * COUNT)
            .Take(COUNT)
            .ToListAsync(cancellationToken);

        var count = await query.CountAsync(cancellationToken);

        return new PaginationDto<CompanyEntity>(
            companies,
            count,
            (int)Math.Ceiling((float)count / COUNT));
    }
}