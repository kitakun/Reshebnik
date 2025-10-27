using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Tabligo.Domain.Models;
using Tabligo.Handlers.JobOperation;

namespace Tabligo.Web.Api.Client;

[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class JobOperationsController : ControllerBase
{
    [HttpGet("{jobType}/typeahead")]
    public async Task<ActionResult<PaginationDto<JobOperationDto>>> TypeaheadAsync(
        [FromRoute] string jobType,
        [FromQuery] TypeaheadRequest request,
        [FromServices] JobOperationsTypeaheadHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(jobType, request, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{jobId}/result")]
    public async Task<IActionResult> GetResultAsync(
        [FromRoute] int jobId,
        [FromServices] JobOperationGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(jobId, cancellationToken);
        if (result == null)
        {
            return NotFound(new { error = "Job not found" });
        }

        if (result.Data == null)
        {
            return Ok(null);
        }

        return Ok(result.Data);
    }
}
