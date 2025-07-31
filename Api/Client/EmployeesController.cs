using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Handlers.Employee;
using Reshebnik.Domain.Models;

namespace Reshebnik.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class EmployeesController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromServices] EmployeesGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpGet("typeahead")]
    public async Task<IActionResult> TypeaheadAsync(
        [FromQuery] TypeaheadRequest request,
        [FromQuery] int? departmentId,
        [FromServices] EmployeesTypeaheadHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, departmentId, cancellationToken);
        return Ok(result);
    }
}
