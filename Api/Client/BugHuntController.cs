using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.Handlers.BugHunt;
using Tabligo.Domain.Models.BugHunt;

namespace Tabligo.Web.Api.Client;

[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class BugHuntController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> PostAsync(
        [FromBody] BugHuntCreateRequest request,
        [FromServices] BugHuntCreateHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);
        return Ok();
    }
}

