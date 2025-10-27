using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;
using Tabligo.Domain.Models.JobOperation;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;
using Tabligo.Handlers.Integration;
using Tabligo.Handlers.JobOperation;

namespace Tabligo.Web.Api.Client;

[Authorize]
[ApiController]
[ApiExplorerSettings(GroupName = "Client")]
[Route("api/admin/[controller]")]
public class IntegrationsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAllAsync(
        [FromServices] JobOperationSearchHandler handler,
        [FromQuery] string? query = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] JobOperationStatusEnum? status = null,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = new JobOperationSearchRequest
        {
            Query = query,
            Page = page,
            PageSize = Math.Min(pageSize, 100), // Limit max page size to 100
            Status = status,
            Type = type
        };

        var result = await handler.SearchAsync(searchRequest, cancellationToken);
        return Ok(result);
    }
    
    [HttpGet("typeahead")]
    public async Task<IActionResult> GetTypeaheadAsync(
        [FromServices] JobOperationSearchHandler handler,
        [FromQuery] string? query = null,
        [FromQuery] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var searchRequest = new JobOperationSearchRequest
        {
            Query = query,
            Page = 1,
            PageSize = Math.Min(limit, 50), // Limit max results for typeahead
            Status = null,
            Type = null
        };

        var result = await handler.SearchAsync(searchRequest, cancellationToken);
        
        // Return simplified response for typeahead
        var typeaheadItems = result.Items.Select(x => new
        {
            x.Id,
            x.Type,
            x.Name,
            x.Status,
            x.CreatedAt,
            x.RetryCount
        }).ToList();

        return Ok(new
        {
            Items = typeaheadItems,
            TotalCount = result.TotalCount
        });
    }

    [HttpGet("activated")]
    public async Task<IActionResult> GetActivatedAsync(
        [FromServices] IntegrationListHandler handler,
        CancellationToken cancellationToken)
    {
        var result = await handler.HandleAsync(cancellationToken);
        var activated = result.Integrations.Where(x => x.IsActivated).ToList();
        return Ok(new IntegrationListResponse { Integrations = activated });
    }

    [HttpGet("isConfigured/{integrationType}")]
    public async Task<IActionResult> IsConfiguredAsync(
        IntegrationTypeEnum integrationType,
        [FromServices] TabligoContext db,
        [FromServices] CompanyContextHandler companyContext,
        CancellationToken ct)
    {
        try
        {
            var companyId = await companyContext.CurrentCompanyIdAsync;
            
            var integration = await db.Integrations
                .FirstOrDefaultAsync(i => i.CompanyId == companyId && i.Type == integrationType, ct);
            
            if (integration == null || string.IsNullOrEmpty(integration.Configuration))
            {
                return Ok(new { isConfigured = false });
            }
            
            // Parse configuration to check isValid property
            var configDict = JsonSerializer.Deserialize<Dictionary<string, object>>(integration.Configuration);
            if (configDict != null && configDict.TryGetValue("isValid", out var value))
            {
                var isValid = value?.ToString()?.ToLowerInvariant() == "true";
                return Ok(new { isConfigured = isValid });
            }
            
            return Ok(new { isConfigured = false });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{integrationType}/preview")]
    public async Task<IActionResult> PreviewImportAsync(
        IntegrationTypeEnum integrationType,
        [FromBody] JsonElement requestBody,
        [FromServices] IntegrationPreviewHandler handler,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.CreatePreviewAsync(integrationType, requestBody, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("jobs/{jobId}")]
    public async Task<IActionResult> GetJobPreviewAsync(
        int jobId,
        [FromServices] JobOperationGetHandler handler,
        [FromServices] CompanyContextHandler companyContext,
        CancellationToken ct)
    {
        try
        {
            var companyId = await companyContext.CurrentCompanyIdAsync;
            var result = await handler.GetJobStatusAsync(jobId, companyId, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("jobs/{jobId}/approve")]
    public async Task<IActionResult> ApproveImportAsync(
        int jobId,
        [FromBody] JsonElement? requestBody,
        [FromServices] IntegrationApprovalHandler handler,
        CancellationToken ct)
    {
        try
        {
            List<string>? entityIds = null;
            
            if (requestBody is { ValueKind: JsonValueKind.Object })
            {
                if (requestBody.Value.TryGetProperty("entityIds", out var entityIdsElement))
                {
                    entityIds = JsonSerializer.Deserialize<List<string>>(entityIdsElement.GetRawText());
                }
            }
            
            var result = await handler.ApproveAndImportAsync(jobId, entityIds, ct);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("jobs/{jobId}/reject")]
    public async Task<IActionResult> RejectImportAsync(
        int jobId,
        [FromServices] IntegrationApprovalHandler handler,
        CancellationToken ct)
    {
        try
        {
            var result = await handler.RejectAsync(jobId, ct);
            if (result)
            {
                return Ok(new { success = true, message = "Import rejected successfully" });
            }
            return BadRequest(new { error = "Failed to reject import" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{integrationType}/settings")]
    public async Task<IActionResult> GetIntegrationSettingsAsync(
        [FromRoute] IntegrationTypeEnum integrationType,
        [FromServices] IntegrationSettingsHandler handler,
        [FromServices] CompanyContextHandler companyContext,
        CancellationToken ct)
    {
        try
        {
            var companyId = await companyContext.CurrentCompanyIdAsync;
            var settings = await handler.GetIntegrationSettingsAsync(companyId, integrationType, ct);
            return Ok(settings);
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("{integrationType}/settings")]
    public async Task<IActionResult> SaveIntegrationSettingsAsync(
        [FromRoute] IntegrationTypeEnum integrationType,
        [FromBody] JsonDocument settings,
        [FromServices] IntegrationSettingsHandler handler,
        [FromServices] CompanyContextHandler companyContext,
        CancellationToken ct)
    {
        try
        {
            var companyId = await companyContext.CurrentCompanyIdAsync;
            var result = await handler.SaveIntegrationSettingsAsync(companyId, integrationType, settings, ct);
            return Ok(result);
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}



