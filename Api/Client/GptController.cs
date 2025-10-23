using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.GPT.Logging;
using Tabligo.GPT.Models;
using Tabligo.GPT.Services;

namespace Tabligo.Web.Api.Client;

[AllowAnonymous]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class GptController(GptService gptService, ILogger<GptController> logger) : ControllerBase
{
    [HttpPost("hello-world")]
    public async Task<IActionResult> HelloWorldAsync(CancellationToken cancellationToken = default)
    {
        logger.HelloWorldEndpointCalled();
        
        try
        {
            var response = await gptService.HelloWorldAsync(cancellationToken);
            
            if (response == null)
            {
                logger.GptServiceReturnedNull();
                return BadRequest("Failed to get response from GPT service");
            }
            
            logger.HelloWorldCompleted();
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Gpt:ApiKey"))
        {
            logger.GptApiKeyNotConfigured();
            return BadRequest("GPT API key not configured");
        }
        catch (Exception ex)
        {
            logger.ErrorInHelloWorld();
            return StatusCode(500, new { error = ex.Message });
        }
    }

    [HttpPost("send-request")]
    public async Task<IActionResult> SendRequestAsync(
        [FromBody] GptRequest request,
        CancellationToken cancellationToken = default)
    {
        logger.SendRequestEndpointCalled(request.Model);
        
        try
        {
            var response = await gptService.SendRequestAsync(request, cancellationToken);
            
            if (response == null)
            {
                logger.GptServiceReturnedNullCustom();
                return BadRequest("Failed to get response from GPT service");
            }
            
            logger.SendRequestCompleted();
            return Ok(response);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Gpt:ApiKey"))
        {
            logger.GptApiKeyNotConfigured();
            return BadRequest("GPT API key not configured");
        }
        catch (Exception ex)
        {
            logger.ErrorInSendRequest();
            return StatusCode(500, new { error = ex.Message });
        }
    }
}
