using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Handlers.BugHunt;
using Reshebnik.Domain.Models.BugHunt;

namespace Reshebnik.Web.Api.Client;

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
