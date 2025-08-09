using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Handlers.Department;
using Reshebnik.Domain.Models;
using Reshebnik.Domain.Models.Department;
using Reshebnik.Domain.Extensions;
using Reshebnik.Domain.Enums;

namespace Reshebnik.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class DepartmentsController : ControllerBase
{
    /// <summary>
    /// technical tree from root
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAsync(
        [FromServices] DepartmentGetHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Get specified department
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetByIdAsync(
        int id,
        [FromServices] DepartmentGetByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    [HttpGet("{id}/preview")]
    public async Task<IActionResult> PreviewAsync(
        [FromRoute] int id,
        [FromQuery] DateTime from,
        [FromQuery] DateTime to,
        [FromQuery] PeriodTypeEnum periodType,
        [FromServices] DepartmentPreviewHandler handler,
        CancellationToken cancellationToken)
    {
        var range = new DateRange(from.ToUtcFromClient(), to.ToUtcFromClient());
        var result = await handler.HandleAsync(id, range, periodType, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Search through all departments
    /// </summary>
    [HttpGet("typeahead")]
    public async Task<IActionResult> TypeaheadAsync(
        [FromQuery] TypeaheadRequest request,
        [FromServices] DepartmentTypeaheadHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Create or update department
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> PutAsync(
        [FromBody] DepartmentDto request,
        [FromServices] DepartmentPutOneHandler handler,
        CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(request, cancellationToken);
        return Ok(new { id });
    }

    /// <summary>
    /// Delete specified department
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteAsync(
        int id,
        [FromServices] DepartmentDeleteHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Delete multiple departments
    /// </summary>
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

    /// <summary>
    /// Get department form data by id
    /// </summary>
    [HttpGet("form")]
    public async Task<IActionResult> GetFormAsync(
        [FromQuery] int id,
        [FromServices] DepartmentFormGetByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(id, cancellationToken);
        if (result == null) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Update department by form
    /// </summary>
    [HttpPut("form")]
    public async Task<IActionResult> PutFormAsync(
        [FromQuery] int? id,
        [FromBody] DepartmentFormDto request,
        [FromServices] DepartmentFormPutHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(id, request, cancellationToken);
        return Ok();
    }

    /// <summary>
    /// Add or update employees in department
    /// </summary>
    [HttpPut("upsert-employees-to-department")]
    public async Task<IActionResult> UpsertEmployeesToDepartmentAsync(
        [FromBody] DepartmentEmployeesUpsertDto request,
        [FromServices] DepartmentEmployeesUpsertHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);
        return Ok();
    }
}
