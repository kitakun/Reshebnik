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
public class ArchiveController : ControllerBase
{
    [HttpPost("metrics/{id}")]
    public async Task<IActionResult> ArchiveAsync(
        [FromRoute] int id,
        [FromBody] MetricArchiveDto request,
        [FromServices] MetricArchiveHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, request, cancellationToken);
        return Ok();
    }

    [HttpGet("metrics/{id}")]
    [ProducesResponseType(typeof(ArchivedMetricGetDto), 200)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] int id,
        [FromServices] ArchivedMetricGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("metrics/typeahead")]
    [ProducesResponseType(typeof(PaginationDto<ArchivedMetricDto>), 200)]
    public async Task<IActionResult> ArchiveTypeaheadAsync(
        [FromQuery] TypeaheadRequest request,
        [FromServices] ArchivedMetricTypeaheadHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("metrics/{id}")]
    public async Task<IActionResult> UnarchiveAsync(
        [FromRoute] int id,
        [FromServices] MetricUnarchiveHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return Ok();
    }
}

