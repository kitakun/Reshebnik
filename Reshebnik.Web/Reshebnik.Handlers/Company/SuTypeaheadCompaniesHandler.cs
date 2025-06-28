using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.EntityFramework;

using System.Security.Claims;

namespace Reshebnik.Handlers.Company;

public class SuTypeaheadCompaniesHandler(
    IHttpContextAccessor httpContextAccessor,
    ReshebnikContext dbContext)
{
    public async ValueTask<PaginationDto<CompanyEntity>?> HandlerAsync(TypeaheadRequest command, CancellationToken cancellationToken = default)
    {
        if (httpContextAccessor.HttpContext == null) return null;
        var roleIdClaim = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.Role);
        if (roleIdClaim == null) return null;
        if (roleIdClaim.Value != RootRolesEnum.SuperAdmin.ToString()) return null;

        const int COUNT = 50;

        var query = dbContext
            .Companies
            .AsNoTracking()
            .Take(COUNT);

        if (!string.IsNullOrEmpty(command.Query) && !string.IsNullOrEmpty(command.Query))
        {
            query = query.Where(w => w.Name.ToLower().Contains(command.Query.ToLower()));
        }

        var companies = await query
            .Skip(command.Page ?? 0 * COUNT)
            .ToListAsync(cancellationToken);

        var count = await query.CountAsync(cancellationToken: cancellationToken);
        return new PaginationDto<CompanyEntity>(
            companies,
            count,
            (int) Math.Ceiling((float) count / COUNT));
    }
}