using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Tabligo.Domain.Models;
using Tabligo.Domain.Models.Company;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Auth;
using Tabligo.Handlers.Company;

using System.Security.Claims;

using Tabligo.Web.DTO.Auth;

namespace Tabligo.Web.Api.Super;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Super")]
[Route("api/super/[controller]")]
public class CompaniesController : ControllerBase
{
    [HttpGet("typeahead")]
    public async Task<IActionResult> TypeaheadAsync(
        [FromQuery] TypeaheadRequest request,
        [FromServices] SuTypeaheadCompaniesHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandlerAsync(request, cancellationToken);
        if (result == null) return Forbid();
        return Ok(result);
    }

    [HttpGet("ids")]
    public async Task<IActionResult> GetIdsAsync(
        [FromServices] SuAllCompanyIdsHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        return Ok(result);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync(
        [FromBody] CompanyDto request,
        [FromServices] CompanyUpdateHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(request, cancellationToken);
        return Ok();
    }


    [HttpPost("switch")]
    public async Task<IActionResult> SwitchAsync(
        [FromQuery] int? companyId,
        [FromServices] CreateJwtHandler handler,
        [FromServices] IHttpContextAccessor httpContextAccessor,
        [FromServices] TabligoContext dbContext,
        CancellationToken cancellationToken
    )
    {
        if (!companyId.HasValue) return BadRequest();
        if (httpContextAccessor.HttpContext == null) return BadRequest();
        var userId = httpContextAccessor.HttpContext.User.Claims.FirstOrDefault(claim => claim.Type == ClaimTypes.NameIdentifier);
        if (userId == null) return BadRequest();
        var userIdInt = int.Parse(userId.Value);
        var existingUser = await dbContext.Employees.AsNoTracking().Include(i => i.Company).FirstAsync(f => f.Id == userIdInt, cancellationToken);
        var result = handler.CreateToken(existingUser, companyId);
        return Ok(new AdminLoginResponse(result.User, result.Jwt, result.CurrentCompanyId));
    }
}
