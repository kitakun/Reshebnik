using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.Domain.Models;
using Tabligo.Domain.Models.Metric;
using Tabligo.Domain.Models.Employee;
using Tabligo.Handlers.Metric;
using Tabligo.Handlers.Employee;

namespace Tabligo.Web.Api.Client;

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

    [HttpPost("users/{id}")]
    public async Task<IActionResult> ArchiveUserAsync(
        [FromRoute] int id,
        [FromServices] EmployeeArchiveHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return Ok();
    }

    [HttpGet("users/{id}")]
    [ProducesResponseType(typeof(ArchivedUserGetDto), 200)]
    public async Task<IActionResult> GetUserAsync(
        [FromRoute] int id,
        [FromServices] ArchivedUserGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("users/typeahead")]
    [ProducesResponseType(typeof(PaginationDto<ArchivedUserDto>), 200)]
    public async Task<IActionResult> ArchiveUsersTypeaheadAsync(
        [FromQuery] TypeaheadRequest request,
        [FromServices] ArchivedUserTypeaheadHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> UnarchiveUserAsync(
        [FromRoute] int id,
        [FromServices] EmployeeUnarchiveHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return Ok();
    }
}


