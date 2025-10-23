using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.Handlers;

namespace Tabligo.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class WelcomeController(WelcomeHandler handler) : ControllerBase
{
    [HttpGet]
    public async Task<bool> GetWelcomeWasSeen()
    {
        return await handler.HandleGetStatus();
    }

    [HttpPut]
    public async Task SetWelcomeWasSeen()
    {
        await handler.HandleSetSeen();
    }
}
