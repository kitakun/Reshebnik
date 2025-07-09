using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Handlers.Indicator;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Indicator;

namespace Reshebnik.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class IndicatorController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromServices] IndicatorGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(
        [FromRoute] int id,
        [FromServices] IndicatorGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpGet("typeahead")]
    public async Task<IActionResult> TypeaheadAsync(
        [FromQuery] TypeaheadRequest request,
        [FromServices] IndicatorTypeaheadHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> PutAsync(
        [FromBody] IndicatorPutDto request,
        [FromServices] IndicatorPutHandler handler,
        CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(request, cancellationToken);
        return Ok(new { id });
    }
}
