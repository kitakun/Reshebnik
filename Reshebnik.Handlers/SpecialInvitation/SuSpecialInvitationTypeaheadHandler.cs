using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.SpecialInvitation;

public class SuSpecialInvitationTypeaheadHandler(IHttpContextAccessor accessor, ReshebnikContext db)
{
    public async ValueTask<PaginationDto<SpecialInvitationEntity>?> HandleAsync(TypeaheadRequest request, CancellationToken cancellationToken = default)
    {
        if (accessor.HttpContext == null) return null;
        var role = accessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "user-role")?.Value;
        if (role != RootRolesEnum.SuperAdmin.ToString()) return null;

        const int COUNT = 50;
        var query = db.SpecialInvitations.AsNoTracking();
        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(x => x.CompanyName.ToLower().Contains(q) || x.Email.ToLower().Contains(q) || x.FIO.ToLower().Contains(q));
        }
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Take(COUNT).Skip((request.Page ?? 0) * COUNT).ToListAsync(cancellationToken);
        return new PaginationDto<SpecialInvitationEntity>(items, total, (int)Math.Ceiling(total / (double)COUNT));
    }
}
