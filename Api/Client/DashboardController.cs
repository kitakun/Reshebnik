using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models.Dashboard;
using Reshebnik.Domain.Models;
using Reshebnik.Handlers.Dashboard;
using Reshebnik.Domain.Extensions;

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
        [FromServices] DashboardGetHandler handler,
        CancellationToken cancellationToken)
    {
        var fromUtc = from;
        var toUtc = to;
        // Adjust range to ensure at most a 7-day period
        var adjustedTo = fromUtc.AddDays(6) < toUtc ? fromUtc.AddDays(6) : toUtc;
        var range = new DateRange(fromUtc, adjustedTo);
        var result = await handler.HandleAsync(range, cancellationToken);
        return Ok(result);
    }
}
