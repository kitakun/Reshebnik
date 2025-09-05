using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models.Dashboard;
using Reshebnik.Domain.Models;
using Reshebnik.Handlers.Dashboard;
using Reshebnik.Domain.Enums;

namespace Reshebnik.Web.Api.Client;

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
