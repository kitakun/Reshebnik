using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Entities;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.BugHunt;

public class SuBugHuntTypeaheadHandler(IHttpContextAccessor accessor, TabligoContext db)
{
    public async ValueTask<PaginationDto<BugHuntEntity>?> HandleAsync(TypeaheadRequest request, CancellationToken cancellationToken = default)
    {
        if (accessor.HttpContext == null) return null;
        var role = accessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "user-role")?.Value;
        if (role != nameof(RootRolesEnum.SuperAdmin)) return null;

        const int COUNT = 25;
        IQueryable<BugHuntEntity> query = db.BugHunts.AsNoTracking();
        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(q) || b.Message.ToLower().Contains(q));
        }
        query = query.OrderByDescending(b => b.CreatedAt);
        var total = await query.CountAsync(cancellationToken);

        var page = Math.Max(request.Page, 1);
        var items = await query
            .Skip((page - 1) * COUNT)
            .Take(COUNT)
            .ToListAsync(cancellationToken);

        return new PaginationDto<BugHuntEntity>(items, total, (int)Math.Ceiling(total / (double)COUNT));
    }
}
