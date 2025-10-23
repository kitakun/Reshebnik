using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Entities;
using Tabligo.Domain.Enums;
using Tabligo.EntityFramework;

using System.Security.Claims;

namespace Tabligo.Handlers.Auth;

public class UserContextHandler(IHttpContextAccessor httpContextAccessor, TabligoContext db)
{
    public int CurrentUserId
    {
        get
        {
            if (httpContextAccessor.HttpContext == null) throw new NullReferenceException(nameof(httpContextAccessor));
            var userId = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);
            if (userId == null) throw new NullReferenceException(nameof(userId));

            var userIdentity = int.Parse(userId.Value);
            return userIdentity;
        }
    }

    public RootRolesEnum Role
    {
        get
        {
            if (httpContextAccessor.HttpContext == null) throw new NullReferenceException(nameof(httpContextAccessor));
            var userRoleClaim = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == "user-role");
            if (userRoleClaim == null) throw new NullReferenceException(nameof(userRoleClaim));

            var roleType = Enum.Parse<RootRolesEnum>(userRoleClaim.Value);
            return roleType;
        }
    }

    public async ValueTask<EmployeeEntity> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        return await db
            .Employees
            .AsNoTracking()
            .FirstAsync(f => f.Id == CurrentUserId, cancellationToken: cancellationToken);
    }
}