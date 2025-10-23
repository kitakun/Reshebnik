using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Tabligo.EntityFramework;

namespace Tabligo.Web.Api.Client;

[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class LogsController : ControllerBase
{
    [HttpGet("emailsQueue")]
    public async Task<IActionResult> GetEmailsQueueAsync(
        [FromServices] TabligoContext db,
        CancellationToken cancellationToken)
    {
        var emails = await db.EmailQueue
            .AsNoTracking()
            .Where(e => !e.IsSent && e.Error == null)
            .OrderBy(e => e.EnqueuedAt)
            .ToListAsync(cancellationToken);

        return Ok(emails);
    }

    [HttpGet("deployDate")]
    public IActionResult GetDeployDate()
    {
        var deployDate = Environment.GetEnvironmentVariable("DATETIME_NOW");
        return Ok(new { DeployDate = deployDate });
    }
}

