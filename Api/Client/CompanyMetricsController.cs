using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Metric;
using Reshebnik.Handlers.Metric;
using Reshebnik.Domain.Enums;

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
        [FromQuery] PeriodTypeEnum periodType,
        [FromServices] CompanyPreviewMetricsHandler handler,
        CancellationToken cancellationToken)
    {
        var range = new DateRange(from, to);
        var result = await handler.HandleAsync(id, range, periodType, cancellationToken);
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
