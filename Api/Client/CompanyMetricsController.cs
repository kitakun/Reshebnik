using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.Handlers.Metric;

namespace Reshebnik.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class CompanyMetricsController : ControllerBase
{
    [HttpGet("{id}/preview")]
    [ProducesResponseType(typeof(CompanyPreviewMetricsDto), 200)]
    public async Task<IActionResult> PreviewAsync(
        [FromRoute] int id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromServices] CompanyPreviewMetricsHandler handler,
        CancellationToken cancellationToken)
    {
        var range = new DateRange(from, to);
        var result = await handler.HandleAsync(id, range, cancellationToken);

        // Fetch totals for the last year relative to today
        var now = DateTime.UtcNow.Date;
        var totalsRange = new DateRange(now.AddYears(-1), now);
        var totals = await handler.HandleAsync(id, totalsRange, cancellationToken);

        result.Metrics.TotalPlanData = totals.Metrics.TotalPlanData;
        result.Metrics.TotalFactData = totals.Metrics.TotalFactData;
        return Ok(result);
    }

    [HttpPut("{id}/preview")]
    public async Task<IActionResult> PutPreviewAsync(
        [FromRoute] int id,
        [FromBody] PutCompanyPreviewMetricsDto request,
        [FromServices] CompanyPreviewMetricsPutHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);
        return Ok();
    }
}
