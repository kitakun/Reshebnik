using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

using Reshebnik.Domain.Entities;
using Reshebnik.Domain.Enums;
using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Auth;

using System.Security.Claims;

namespace Reshebnik.Handlers.Company;

public class CompanyContextHandler
{
    private readonly IHttpContextAccessor _httpContent;
    private readonly UserContextHandler _userContext;

    private readonly Lazy<ValueTask<CompanyEntity?>> _companyEntity;
    public ValueTask<CompanyEntity?> CurrentCompanyAsync => _companyEntity.Value;

    public CompanyContextHandler(IHttpContextAccessor httpContent, UserContextHandler userContext, ReshebnikContext db)
    {
        _httpContent = httpContent;
        _userContext = userContext;
        _companyEntity = new(async () =>
        {
            var companyId = await CurrentCompanyIdAsync;
            if (companyId == 0) return null;

            return await db
                .Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == companyId, httpContent.HttpContext!.RequestAborted);
        });
    }

    public Task<int> CurrentCompanyIdAsync => Task.Run(async () =>
    {
        if (_httpContent.HttpContext == null) return 0;
        var userId = _httpContent.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);
        if (userId == null) return 0;

        var existingUser = await _userContext.GetCurrentEmployeeAsync();

        var userRoleClaim = _httpContent.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == "user-role");
        if (userRoleClaim == null) return 0;
        var userRole = Enum.Parse<RootRolesEnum>(userRoleClaim.Value);

        if (userRole != RootRolesEnum.SuperAdmin)
        {
            return existingUser.CompanyId;
        }

        var companyId = _httpContent.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == "company");
        if (companyId == null) return 0;

        if (!int.TryParse(companyId.Value, out int companyIdInt))
        {
            return 0;
        }

        return companyIdInt;
    });
}