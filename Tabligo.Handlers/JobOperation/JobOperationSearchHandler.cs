using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Entities;
using Tabligo.Domain.Models.JobOperation;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;
using System.Text.Json;

namespace Tabligo.Handlers.JobOperation;

public class JobOperationSearchHandler(TabligoContext db, CompanyContextHandler companyContext)
{
    public async Task<JobOperationSearchResponse> SearchAsync(
        JobOperationSearchRequest request, 
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        
        // Base query for company's job operations
        var query = db.JobOperations
            .Where(x => x.CompanyId == companyId)
            .AsQueryable();

        // Apply search filters
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var searchTerm = request.Query.ToLowerInvariant();
            query = query.Where(x => 
                x.Name.ToLowerInvariant().Contains(searchTerm) ||
                x.Type.ToLowerInvariant().Contains(searchTerm));
        }

        // Apply status filter
        if (request.Status.HasValue)
        {
            query = query.Where(x => x.Status == request.Status.Value);
        }

        // Apply type filter
        if (!string.IsNullOrWhiteSpace(request.Type))
        {
            query = query.Where(x => x.Type == request.Type);
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync(ct);

        // Apply pagination and ordering
        var jobOperations = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToListAsync(ct);

        // Convert to response DTOs
        var items = jobOperations.Select(MapToResponse).ToList();

        return new JobOperationSearchResponse
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }

    private static JobOperationStatusResponse MapToResponse(JobOperationEntity job)
    {
        var response = new JobOperationStatusResponse
        {
            Id = job.Id,
            Type = job.Type,
            Name = job.Name,
            Status = job.Status,
            CreatedAt = job.CreatedAt,
            CompletedAt = job.CompletedAt,
            RetryCount = job.RetryCount
        };

        // Don't send InputData and ResultData in the list endpoint for security
        // They should be fetched separately via the GetJobStatusAsync method if needed
        response.InputData = null;
        response.ResultData = null;

        return response;
    }
}
