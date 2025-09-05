using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models.Metric;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Enums;
using Reshebnik.Handlers.Metric;
using Reshebnik.Domain.Models.Employee;
using Reshebnik.Handlers.Employee;


namespace Reshebnik.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/metrics")]
public class EmployeeMetricsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromServices] MetricGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(
        [FromRoute] int id,
        [FromServices] MetricGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> PutAsync(
        [FromBody] MetricPutDto request,
        [FromServices] MetricPutHandler handler,
        CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(request, cancellationToken);
        return Ok(new { id });
    }

    [HttpGet("{id}/preview")]
    [ProducesResponseType<UserPreviewMetricsDto>(200)]
    public async Task<IActionResult> UserPreviewMetrics(
        [FromRoute] int id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] PeriodTypeEnum periodType,
        [FromServices] UserPreviewMetricsHandler handler,
        CancellationToken cancellationToken)
    {
        var range = new DateRange(from, to);
        var result = await handler.HandleAsync(id, range, periodType, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{id}/preview")]
    public async Task<IActionResult> PutUserPreviewMetrics(
        [FromRoute] int id,
        [FromBody] PutPreviewMetricsDto request,
        [FromServices] UserPreviewMetricsPutHandler handler,
        CancellationToken cancellationToken)
    {
        var ok = await handler.HandleAsync(id, request, cancellationToken);
        if (!ok) return NotFound();
        return Ok();
    }

    [HttpPut("{id}/save-comment")]
    public async Task<IActionResult> SaveCommentAsync(
        [FromRoute] int id,
        [FromBody] EmployeeCommentDto request,
        [FromServices] EmployeeCommentUpdateHandler handler,
        CancellationToken cancellationToken)
    {
        var ok = await handler.HandleAsync(id, request.Comment, cancellationToken);
        if (!ok) return NotFound();
        return Ok();
    }
}

