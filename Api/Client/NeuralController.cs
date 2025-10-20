using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Reshebnik.Neural.Interfaces;

namespace Reshebnik.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class NeuralController(ITabligoNeuralAgent neuralAgent) : ControllerBase
{
    /// <summary>
    /// Process a file and get AI suggestions for entities to create
    /// </summary>
    /// <param name="file">The uploaded file to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Neural response with suggested entities</returns>
    [HttpPost("process-file")]
    public async Task<IActionResult> ProcessFileAsync(
        IFormFile file,
        CancellationToken cancellationToken = default)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file provided");
        }

        if (file.Length > 10 * 1024 * 1024) // 10MB limit
        {
            return BadRequest("File size exceeds 10MB limit");
        }

        try
        {
            using var reader = new StreamReader(file.OpenReadStream());
            var fileContent = await reader.ReadToEndAsync(cancellationToken);

            var result = await neuralAgent.ProcessFileAsync(fileContent, file.FileName, cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error processing file", details = ex.Message });
        }
    }

    /// <summary>
    /// Process text content and get AI suggestions for entities to create
    /// </summary>
    /// <param name="request">Request containing text content to analyze</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Neural response with suggested entities</returns>
    [HttpPost("process-text")]
    public async Task<IActionResult> ProcessTextAsync(
        [FromBody] ProcessTextRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return BadRequest("Content cannot be empty");
        }

        try
        {
            var result = await neuralAgent.ProcessFileAsync(request.Content, request.FileName ?? "text.txt", cancellationToken);
            
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error processing text", details = ex.Message });
        }
    }
}

public class ProcessTextRequest
{
    public string Content { get; set; } = string.Empty;
    public string? FileName { get; set; }
}
