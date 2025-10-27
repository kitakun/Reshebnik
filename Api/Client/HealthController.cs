using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using Tabligo.EntityFramework;

namespace Tabligo.Web.Api.Client;

[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/[controller]")]
public class HealthController(TabligoContext context, ILogger<HealthController> logger) : ControllerBase
{
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Check database connectivity
            await context.Database.CanConnectAsync();
            
            // Check if we can execute a simple query
            await context.Database.ExecuteSqlRawAsync("SELECT 1");
            
            return Ok(new
            {
                Status = "Healthy",
                Timestamp = DateTime.UtcNow,
                Database = "Connected",
                Version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed");
            return StatusCode(503, new
            {
                Status = "Unhealthy",
                Timestamp = DateTime.UtcNow,
                Database = "Disconnected",
                Error = ex.Message
            });
        }
    }

    [HttpGet("ready")]
    [AllowAnonymous]
    public async Task<IActionResult> Ready()
    {
        try
        {
            // Check if the application is ready to accept requests
            await context.Database.CanConnectAsync();
            
            return Ok(new
            {
                Status = "Ready",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Readiness check failed");
            return StatusCode(503, new
            {
                Status = "Not Ready",
                Timestamp = DateTime.UtcNow,
                Error = ex.Message
            });
        }
    }

    [HttpGet("live")]
    [AllowAnonymous]
    public IActionResult Live()
    {
        // Simple liveness check - just return OK if the application is running
        return Ok(new
        {
            Status = "Alive",
            Timestamp = DateTime.UtcNow
        });
    }
}

