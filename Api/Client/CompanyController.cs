using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Company;
using Tabligo.Handlers.Auth;
using Tabligo.Handlers.Company;

namespace Tabligo.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class CompanyController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int? companyId,
        [FromServices] UserContextHandler userContextHandler,
        [FromServices] CompanyGetHandler handler,
        [FromServices] SuCompanyGetHandler suHandler,
        CancellationToken cancellationToken)
    {
        // TODO to SuController
        if (companyId.HasValue && userContextHandler.Role == RootRolesEnum.SuperAdmin)
        {
            return Ok(await suHandler.HandleAsync(companyId.Value, cancellationToken));
        }

        var result = await handler.HandleAsync(cancellationToken);
        if (result == null) return Forbid();
        return Ok(result);
    }

    [HttpPut("settings")]
    public async Task<IActionResult> UpdateAsync(
        [FromQuery] int? companyId,
        [FromForm] CompanySettingsDto request,
        [FromServices] CompanySettingsUpdateHandler handler,
        [FromServices] UserContextHandler userContextHandler,
        [FromServices] SuCompanySettingsUpdateHandler suHandler,
        CancellationToken cancellationToken)
    {
        // TODO to SuController
        if (companyId.HasValue && userContextHandler.Role == RootRolesEnum.SuperAdmin)
        {
            await suHandler.HandleAsync(companyId.Value, request, cancellationToken);
            return Ok();
        }

        await handler.HandleAsync(request, cancellationToken);
        return Ok();
    }
}
