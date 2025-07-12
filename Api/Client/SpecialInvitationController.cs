using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models.SpecialInvitation;
using Reshebnik.Handlers.SpecialInvitation;

namespace Reshebnik.Web.Api.Client;

[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/[controller]")]
public class SpecialInvitationController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] SpecialInvitationCreateDto request,
        [FromServices] SpecialInvitationCreateHandler handler,
        CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(request, cancellationToken);
        return Ok(new { id });
    }
}
