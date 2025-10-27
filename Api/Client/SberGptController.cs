using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.SberGPT.Services;

namespace Tabligo.Web.Api.Client;

[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class SberGptController(ISberGptService sberGptService, ILogger<SberGptController> logger) : ControllerBase
{
    [HttpPost("hello-world-2")]
    public async Task<IActionResult> HelloWorld2Async(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SberGPT Hello World 2 endpoint called");
        
        try
        {
            var response = await sberGptService.HandleAsync("Привет! Как дела?");
            
            if (string.IsNullOrEmpty(response))
            {
                logger.LogWarning("SberGPT service returned empty response");
                return BadRequest("Failed to get response from SberGPT service");
            }
            
            logger.LogInformation("SberGPT Hello World 2 completed successfully");
            return Ok(new { message = response });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SberGPT Hello World 2");
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("send-request")]
    public async Task<IActionResult> SendRequestAsync(
        [FromBody] SberGptRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation("SberGPT Send Request endpoint called with message: {Message}", request.Message);
        
        try
        {
            var response = await sberGptService.HandleAsync(request.Message);
            
            if (string.IsNullOrEmpty(response))
            {
                logger.LogWarning("SberGPT service returned empty response");
                return BadRequest("Failed to get response from SberGPT service");
            }
            
            logger.LogInformation("SberGPT Send Request completed successfully");
            return Ok(new { message = response });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SberGPT Send Request");
            return StatusCode(500, new { error = ex.Message });
        }
    }
}

public class SberGptRequest
{
    public string Message { get; set; } = string.Empty;
}
