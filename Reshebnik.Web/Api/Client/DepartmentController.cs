using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Handlers.Department;
using Reshebnik.Web.DTO.Department;

namespace Reshebnik.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class DepartmentController : ControllerBase
{
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAsync(
        int id,
        [FromServices] DepartmentGetByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> PutAsync(
        [FromBody] DepartmentDto request,
        [FromServices] DepartmentPutOneHandler handler,
        CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(request, cancellationToken);
        return Ok(new { id });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(
        int id,
        [FromServices] DepartmentDeleteHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteManyAsync(
        [FromQuery] int[] ids,
        [FromServices] DepartmentDeleteHandler handler,
        CancellationToken cancellationToken)
    {
        if (ids.Length == 0) return Ok();
        await handler.HandleManyAsync(ids, cancellationToken);
        return Ok();
    }
}
