using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.EntityFramework;

using System.Security.Claims;

namespace Reshebnik.Handlers.Auth;

public class UserContextHandler(IHttpContextAccessor httpContextAccessor, ReshebnikContext db)
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

    public async ValueTask<EmployeeEntity> GetCurrentEmployeeAsync(CancellationToken cancellationToken = default)
    {
        return await db
            .Employees
            .AsNoTracking()
            .FirstAsync(f => f.Id == CurrentUserId, cancellationToken: cancellationToken);
    }
}