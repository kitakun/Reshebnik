using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Tabligo.Handlers.JobOperation;
using Tabligo.Domain.Models.JobOperation;
using Tabligo.Handlers.Company;

using static Tabligo.Domain.Models.JobOperation.JobOperationTypes;

namespace Tabligo.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class NeuralController(
    IJobOperationQueue jobQueue,
    CompanyContextHandler companyContext,
    JobOperationGetHandler jobGetHandler) : ControllerBase
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
            var currentCompany = await companyContext.CurrentCompanyAsync;
            if (currentCompany == null)
            {
                return BadRequest("No company context found");
            }

            using var reader = new StreamReader(file.OpenReadStream());
            var fileContent = await reader.ReadToEndAsync(cancellationToken);

            var inputData = new
            {
                FileContent = fileContent,
                FileName = file.FileName
            };

            var jobId = await jobQueue.EnqueueAsync(
                currentCompany.Id,
                NeuralFileProcess,
                file.FileName,
                inputData,
                cancellationToken);

            return Ok(new JobOperationEnqueueResponse
            {
                JobId = jobId,
                Message = "File processing job created successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error creating processing job", details = ex.Message });
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
            var currentCompany = await companyContext.CurrentCompanyAsync;
            if (currentCompany == null)
            {
                return BadRequest("No company context found");
            }

            var inputData = new
            {
                FileContent = request.Content,
                FileName = request.FileName ?? "text.txt"
            };

            var jobId = await jobQueue.EnqueueAsync(
                currentCompany.Id,
                NeuralFileProcess,
                request.FileName ?? "text.txt",
                inputData,
                cancellationToken);

            return Ok(new JobOperationEnqueueResponse
            {
                JobId = jobId,
                Message = "Text processing job created successfully"
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error creating processing job", details = ex.Message });
        }
    }

    /// <summary>
    /// Get the status of a job operation
    /// </summary>
    /// <param name="jobId">The ID of the job to check</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Job status information</returns>
    [HttpGet("job-status/{jobId}")]
    public async Task<IActionResult> GetJobStatusAsync(
        int jobId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var currentCompany = await companyContext.CurrentCompanyAsync;
            if (currentCompany == null)
            {
                return BadRequest("No company context found");
            }

            var status = await jobGetHandler.GetJobStatusAsync(jobId, currentCompany.Id, cancellationToken);
            return Ok(status);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Error getting job status", details = ex.Message });
        }
    }
}

public class ProcessTextRequest
{
    public string Content { get; set; } = string.Empty;
    public string? FileName { get; set; }
}
