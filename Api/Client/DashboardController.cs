using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.Domain.Models.Dashboard;
using Tabligo.Domain.Models;
using Tabligo.Handlers.Dashboard;
using Tabligo.Domain.Enums;

namespace Tabligo.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class DashboardController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(DashboardDto), 200)]
    public async Task<IActionResult> GetAsync(
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] PeriodTypeEnum periodType,
        [FromServices] DashboardGetHandler handler,
        CancellationToken cancellationToken)
    {
        var fromUtc = from;
        var toUtc = to;
        var range = new DateRange(fromUtc, toUtc);
        var result = await handler.HandleAsync(range, periodType, cancellationToken);
        return Ok(result);
    }
}
