using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Handlers.Employee;
using Reshebnik.Web.DTO.Employee;

namespace Reshebnik.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class EmployeeController : ControllerBase
{
    [HttpPut]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] EmployeeFullDto request,
        [FromServices] EmployeeUpdateHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);
        return Ok();
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync(
        [FromBody] EmployeeCreateDto request,
        [FromServices] CreateEmployeeHandler handler,
        CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(request, cancellationToken);
        return Ok(new { id });
    }
}
