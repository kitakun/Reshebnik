﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Domain.Models;
using Reshebnik.Handlers.Auth;
using Reshebnik.Web.DTO.Auth;

using System.Text.Json;

namespace Reshebnik.Web.Api.Client;

[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class AuthController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("invite")]
    public async Task<IActionResult> GetInviteAdminUserAsync(
        [FromQuery] string code,
        [FromServices] AuthGetInviteHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(code, cancellationToken);
        if (result == null) return BadRequest();
        return Ok(new { name = result.Value.Name, email = result.Value.Email });
    }

    [AllowAnonymous]
    [HttpPost("invite")]
    public async Task<IActionResult> InviteAdminUserAsync(
        [FromQuery] string code,
        [FromBody] JsonElement request,
        [FromServices] AuthInviteHandler handler,
        CancellationToken cancellationToken)
    {
        var password = request.GetProperty("password").GetString();
        if (string.IsNullOrEmpty(password))
            return BadRequest("required password");

        var result = await handler.HandleAsync(code, password, cancellationToken);
        if (result == null) return BadRequest();
        return Ok(new AdminLoginResponse(result.Value.User, result.Value.Jwt, result.Value.CurrentCompanyId));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> LoginAsync(
        [FromBody] LoginRequest request,
        [FromServices] AuthLoginHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(request, cancellationToken);
        if (result == null) return Forbid();
        return Ok(new AdminLoginResponse(result.Value.User, result.Value.Jwt, result.Value.CurrentCompanyId));
    }
}