using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;
using Tabligo.Domain.Models.JobOperation;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Integration;

public class IntegrationApprovalHandler(
    TabligoContext db,
    IntegrationImportHandler importHandler)
{
    public async Task<IntegrationImportResponse> ApproveAndImportAsync(
        int jobOperationId,
        List<string>? entityIds = null,
        CancellationToken ct = default)
    {
        var jobOperation = await db.JobOperations
            .AsNoTracking()
            .FirstOrDefaultAsync(j => j.Id == jobOperationId, ct);

        if (jobOperation == null)
        {
            throw new InvalidOperationException($"Job operation {jobOperationId} not found");
        }

        if (jobOperation.Status != JobOperationStatusEnum.Finished)
        {
            throw new InvalidOperationException($"Job operation {jobOperationId} is not in pending status");
        }

        try
        {
            // Deserialize import requests from job data
            // Handle different formats: GetCourse returns List directly, neural-file-process returns an object with suggestedEntities
            List<IntegrationImportRequest> allImportRequests;
            
            if (jobOperation.Type == "neural-file-process")
            {
                // Neural file process returns { suggestedEntities: [...] }
                var wrapper = JsonSerializer.Deserialize<NeuralFileProcessResponse>(
                    jobOperation.Data?.RootElement.GetRawText() ?? "{}",
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                allImportRequests = wrapper?.SuggestedEntities ?? new List<IntegrationImportRequest>();
                
                // Set SourceSystem for all entities from neural file process
                foreach (var request in allImportRequests)
                {
                    request.SourceSystem = JobOperationTypes.NeuralFileProcess;
                }
            }
            else
            {
                // GetCourse returns List<IntegrationImportRequest> directly
                allImportRequests = JsonSerializer.Deserialize<List<IntegrationImportRequest>>(
                    jobOperation.Data?.RootElement.GetRawText() ?? "[]") ?? new List<IntegrationImportRequest>();
            }

            if (allImportRequests == null || !allImportRequests.Any())
            {
                throw new InvalidOperationException("No import data found in job operation");
            }

            // Filter import requests if entityIds are provided
            var importRequests = allImportRequests;
            if (entityIds != null && entityIds.Any())
            {
                importRequests = allImportRequests
                    .Where(r => entityIds.Contains(r.SourceId ?? string.Empty) || 
                                entityIds.Contains($"{r.EntityType}-{r.Name}"))
                    .ToList();
            }

            if (!importRequests.Any())
            {
                throw new InvalidOperationException("No matching entities found for the provided entityIds");
            }

            // Execute import using existing handler
            var importResponse = await importHandler.HandleAsync(importRequests, ct);

            return importResponse;
        }
        catch (Exception ex)
        {
            return new IntegrationImportResponse
            {
                Success = false,
                Message = $"Import failed: {ex.Message}",
                CreatedCount = 0,
                UpdatedCount = 0,
                ErrorCount = 1,
                Errors = new List<ImportError>
                {
                    new ImportError
                    {
                        EntityType = "System",
                        Name = "Import Process",
                        Error = ex.Message
                    }
                }
            };
        }
    }

    public async Task<bool> RejectAsync(int jobOperationId, CancellationToken ct = default)
    {
        var jobOperation = await db.JobOperations
            .FirstOrDefaultAsync(j => j.Id == jobOperationId, ct);

        if (jobOperation == null)
        {
            return false;
        }

        if (jobOperation.Status != JobOperationStatusEnum.InQueue)
        {
            return false;
        }

        jobOperation.Status = JobOperationStatusEnum.Failed; // Use Failed instead of Cancelled
        await db.SaveChangesAsync(ct);

        return true;
    }
}

// Wrapper class for neural-file-process response format
public class NeuralFileProcessResponse
{
    public string? ErrorMessage { get; set; }
    public bool IsSuccessful { get; set; }
    public string? AnalysisSummary { get; set; }
    public List<IntegrationImportRequest> SuggestedEntities { get; set; } = new();
}
