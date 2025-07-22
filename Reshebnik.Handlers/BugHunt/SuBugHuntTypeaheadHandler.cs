using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.EntityFramework;

namespace Reshebnik.Handlers.BugHunt;

public class SuBugHuntTypeaheadHandler(IHttpContextAccessor accessor, ReshebnikContext db)
{
    public async ValueTask<PaginationDto<BugHuntEntity>?> HandleAsync(TypeaheadRequest request, CancellationToken cancellationToken = default)
    {
        if (accessor.HttpContext == null) return null;
        var role = accessor.HttpContext.User.Claims.FirstOrDefault(c => c.Type == "user-role")?.Value;
        if (role != RootRolesEnum.SuperAdmin.ToString()) return null;

        const int COUNT = 50;
        IQueryable<BugHuntEntity> query = db.BugHunts.AsNoTracking();
        if (!string.IsNullOrEmpty(request.Query))
        {
            var q = request.Query.ToLower();
            query = query.Where(b => b.Title.ToLower().Contains(q) || b.Message.ToLower().Contains(q));
        }
        query = query.OrderByDescending(b => b.CreatedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query.Skip((request.Page ?? 0) * COUNT).Take(COUNT).ToListAsync(cancellationToken);
        return new PaginationDto<BugHuntEntity>(items, total, (int)Math.Ceiling(total / (double)COUNT));
    }
}
