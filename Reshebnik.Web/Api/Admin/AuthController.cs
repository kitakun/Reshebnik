using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Reshebnik.EntityFramework;
using Reshebnik.Handlers.Auth;
using Reshebnik.Handlers.Company;

using System.Text.Json;

namespace Reshebnik.Web.Api.Admin;

[ApiController]
[ApiExplorerSettings(GroupName = "Admin")]
[Route("api/admin/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("invite")]
    public async Task<IActionResult> InviteAdminUserAsync(
        [FromQuery] string code,
        [FromBody] JsonElement request,
        [FromServices] ReshebnikContext context,
        [FromServices] CompanyContextHandler companyContext,
        [FromServices] SecurityHandler securityHandler,
        [FromServices] CreateJwtHandler jwtHandler,
        [FromServices] IConfiguration configuration,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.GetProperty("password").GetString()))
            return BadRequest("required password");
        
        var existingUser = await context.Employees.FirstOrDefaultAsync(f => f.EmailInvitationCode!.ToLower() == code.ToLower(), cancellationToken);
        if (existingUser != null)
        {
            if (!existingUser.IsActive)
                return Forbid();

            existingUser.Password = securityHandler.GenerateSaltedHash(request.GetProperty("password").GetString()!, out var generatedSalt);
            existingUser.Salt = generatedSalt;
            existingUser.EmailInvitationCode = string.Empty;

            existingUser.LastLoginAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            var curCompany = await companyContext.CurrentCompanyAsync;

            return Ok(jwtHandler.CreateToken(existingUser, configuration, curCompany?.Id));
        }

        return BadRequest();
    }

    [HttpGet("invite")]
    public async Task<IActionResult> GetInviteAdminUserAsync(
        [FromQuery] string code,
        [FromServices] ReshebnikContext context,
        CancellationToken cancellationToken)
    {
        var existingUser = await context.Employees.FirstOrDefaultAsync(f => f.EmailInvitationCode!.ToLower() == code.ToLower(), cancellationToken);
        if (existingUser != null)
        {
            if (!existingUser.IsActive)
                return Forbid();

            existingUser.LastLoginAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);

            return Ok(new
            {
                name = existingUser.FIO,
                email = existingUser.Email
            });
        }

        return BadRequest();
    }

    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] ReshebnikContext context,
        [FromServices] CompanyContextHandler companyContext,
        [FromServices] IConfiguration configuration,
        [FromServices] SecurityHandler securityHandler,
        [FromServices] CreateJwtHandler jwtHandler,
        CancellationToken cancellationToken)
    {
        var existingUser = await context.Employees.FirstOrDefaultAsync(f => f.Email.ToLower() == request.Email.ToLower(), cancellationToken);
        if (existingUser != null)
        {
            var isCorrect = securityHandler.VerifyPassword(request.Password, existingUser.Salt, existingUser.Password);
            if (!isCorrect)
                return Forbid();
            if (!existingUser.IsActive)
                return Forbid();

            existingUser.LastLoginAt = DateTime.UtcNow;
            await context.SaveChangesAsync(cancellationToken);
            var curCompany = await companyContext.CurrentCompanyAsync;
            return Ok(jwtHandler.CreateToken(existingUser, configuration, curCompany?.Id));
        }

        return Forbid();
    }
}