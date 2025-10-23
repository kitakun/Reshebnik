using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Entities;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.SpecialInvitation;

public class SuSpecialInvitationTypeaheadHandler(IHttpContextAccessor accessor, TabligoContext db)
{
    public async ValueTask<PaginationDto<SpecialInvitationEntity>?> HandleAsync(TypeaheadRequest request, CancellationToken cancellationToken = default)
    {
        if (accessor.HttpContext == null) return null;
        var role = accessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "user-role")?.Value;
        if (role != nameof(RootRolesEnum.SuperAdmin)) return null;

        const int COUNT = 25;
        var query = db.SpecialInvitations.AsNoTracking();
        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(x => x.CompanyName.ToLower().Contains(q) || x.Email.ToLower().Contains(q) || x.FIO.ToLower().Contains(q));
        }
        var total = await query.CountAsync(cancellationToken);

        var page = Math.Max(request.Page, 1);
        var items = await query
            .Skip((page - 1) * COUNT)
            .Take(COUNT)
            .ToListAsync(cancellationToken);

        return new PaginationDto<SpecialInvitationEntity>(items, total, (int)Math.Ceiling(total / (double)COUNT));
    }
}
