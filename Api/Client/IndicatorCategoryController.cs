using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.Domain.Enums;
using Tabligo.Domain.Models;
using Tabligo.Domain.Models.Indicator;
using Tabligo.Handlers.IndicatorCategory;

namespace Tabligo.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class IndicatorCategoryController : ControllerBase
{
    [HttpGet("{categoryName}")]
    [ProducesResponseType(typeof(IndicatorCategoryViewDto), 200)]
    public async Task<IActionResult> GetAsync(
        [FromRoute] string categoryName,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] PeriodTypeEnum periodType,
        [FromServices] IndicatorCategoryGetHandler handler,
        CancellationToken cancellationToken)
    {
        var range = new DateRange(from, to);
        var result = await handler.HandleAsync(categoryName, range, periodType, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut("{categoryName}/save-comment")]
    public async Task<IActionResult> SaveCommentAsync(
        [FromRoute] string categoryName,
        [FromBody] IndicatorCategoryCommentDto request,
        [FromServices] IndicatorCategoryCommentUpdateHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(categoryName, request.Comment, cancellationToken);
        return Ok();
    }
}

