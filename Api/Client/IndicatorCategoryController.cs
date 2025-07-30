using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Indicator;
using Reshebnik.Handlers.IndicatorCategory;
using Reshebnik.Domain.Extensions;

namespace Reshebnik.Web.Api.Client;

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
        var range = new DateRange(from.ToUtcFromClient(), to.ToUtcFromClient());
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
