using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models.Employee;
using Reshebnik.Handlers.Employee;
using Reshebnik.Domain.Exceptions;

namespace Reshebnik.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class EmployeeController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromQuery] int id,
        [FromServices] EmployeeGetByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> PutAsync(
        [FromBody] EmployeePutDto request,
        [FromServices] EmployeePutHandler handler,
        CancellationToken cancellationToken)
    {
        try
        {
            var id = await handler.HandleAsync(request, cancellationToken);
            return Ok(new { id });
        }
        catch (EmailAlreadyExistsException)
        {
            return BadRequest(new { error = "E-Mail уже занят" });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(
        int id,
        [FromServices] EmployeeDeleteHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return Ok();
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteManyAsync(
        [FromQuery] int[] ids,
        [FromServices] EmployeeDeleteHandler handler,
        CancellationToken cancellationToken)
    {
        if (ids.Length == 0) return Ok();
        await handler.HandleManyAsync(ids, cancellationToken);
        return Ok();
    }
}
