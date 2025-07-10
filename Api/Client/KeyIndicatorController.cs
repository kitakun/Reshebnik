using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models.Indicator;
using Reshebnik.Handlers.KeyIndicator;

namespace Reshebnik.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class KeyIndicatorController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<KeyIndicatorCategoryDto>>(200)]
    public async Task<IActionResult> GetAsync(
        [FromServices] KeyIndicatorGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }
}
