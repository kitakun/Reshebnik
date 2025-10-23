using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.Domain.Models.Indicator;
using Tabligo.Domain.Enums;
using Tabligo.Handlers.KeyIndicator;

namespace Tabligo.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class KeyIndicatorController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<List<KeyIndicatorCategoryDto>>(200)]
    public async Task<IActionResult> GetAsync(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] PeriodTypeEnum periodType,
        [FromServices] KeyIndicatorGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(from, to, periodType, cancellationToken);
        return Ok(result);
    }
}

