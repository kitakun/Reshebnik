using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models;
using Reshebnik.Handlers.Company;

namespace Reshebnik.Web.Api.Super;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Super")]
[Route("api/super/[controller]")]
public class CompaniesController : ControllerBase
{
    [HttpGet("typeahead")]
    public async Task<IActionResult> TypeaheadAsync(
        [FromQuery] TypeaheadRequest request,
        [FromServices] SuTypeaheadCompaniesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandlerAsync(request, cancellationToken);
        if (result == null) return Forbid();
        return Ok(result);
    }

    [HttpGet("ids")]
    public async Task<IActionResult> GetIdsAsync(
        [FromServices] SuAllCompanyIdsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }
}
