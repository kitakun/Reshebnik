using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Reshebnik.EntityFramework;

namespace Reshebnik.Web.Api.Client;

[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class LogsController : ControllerBase
{
    [HttpGet("emailsQueue")]
    public async Task<IActionResult> GetEmailsQueueAsync(
        [FromServices] ReshebnikContext db,
        CancellationToken cancellationToken)
    {
        var emails = await db.EmailQueue
            .AsNoTracking()
            .Where(e => !e.IsSent && e.Error == null)
            .OrderBy(e => e.EnqueuedAt)
            .ToListAsync(cancellationToken);

        return Ok(emails);
    }
}
