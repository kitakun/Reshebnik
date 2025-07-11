using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models.Dashboard;
using Reshebnik.Domain.Models;
using Reshebnik.Handlers.Dashboard;

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
        var range = new DateRange(from, to);
        var result = await handler.HandleAsync(range, cancellationToken);
        return Ok(result);
    }
}
