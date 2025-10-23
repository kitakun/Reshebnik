using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.Handlers.BugHunt;
using Tabligo.Domain.Models;

namespace Tabligo.Web.Api.Super;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Super")]
[Route("api/super/[controller]")]
public class BugHuntController : ControllerBase
{
    [HttpGet("typeahead")]
    public async Task<IActionResult> TypeaheadAsync(
        [FromQuery] TypeaheadRequest request,
        [FromServices] SuBugHuntTypeaheadHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        if (result == null) return Forbid();
        return Ok(result);
    }
}

