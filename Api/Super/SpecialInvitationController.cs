using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models;
using Reshebnik.Handlers.SpecialInvitation;

namespace Reshebnik.Web.Api.Super;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Super")]
[Route("api/super/[controller]")]
public class SpecialInvitationController : ControllerBase
{
    [HttpGet("typeahead")]
    public async Task<IActionResult> TypeaheadAsync(
        [FromQuery] TypeaheadRequest request,
        [FromServices] SuSpecialInvitationTypeaheadHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        if (result == null) return Forbid();
        return Ok(result);
    }

    [HttpPost("{id}/accept")]
    public async Task<IActionResult> AcceptAsync(
        int id,
        [FromServices] SuSpecialInvitationAcceptHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return Ok();
    }

    [HttpPost("{id}/reject")]
    public async Task<IActionResult> RejectAsync(
        int id,
        [FromServices] SuSpecialInvitationRejectHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return Ok();
    }
}
