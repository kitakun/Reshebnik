using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reshebnik.EntityFramework;

namespace Reshebnik.Web.Api.Client;

[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly ReshebnikContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(ReshebnikContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Check database connectivity
            await _context.Database.CanConnectAsync();
            
            // Check if we can execute a simple query
            await _context.Database.ExecuteSqlRawAsync("SELECT 1");
            
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
            _logger.LogError(ex, "Health check failed");
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
            await _context.Database.CanConnectAsync();
            
            return Ok(new
            {
                Status = "Ready",
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Readiness check failed");
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
