using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.Domain.Models.Integration;
using Tabligo.Handlers.Integration;

namespace Tabligo.Web.Api.Client;

[ApiController]
[Authorize]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class IntegrationController : ControllerBase
{
    [HttpPost("import")]
    public async Task<ActionResult<IntegrationImportResponse>> ImportData(
        [FromBody] List<IntegrationImportRequest> requests,
        [FromServices] IntegrationImportHandler importHandler,
        CancellationToken ct = default)
    {
        if (requests == null || !requests.Any())
        {
            return BadRequest(new IntegrationImportResponse
            {
                Success = false,
                Message = "No import data provided"
            });
        }

        try
        {
            var result = await importHandler.HandleAsync(requests, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new IntegrationImportResponse
            {
                Success = false,
                Message = $"Internal server error: {ex.Message}"
            });
        }
    }
}