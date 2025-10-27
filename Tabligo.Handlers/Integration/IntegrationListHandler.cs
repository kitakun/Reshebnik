using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;
using Tabligo.EntityFramework;
using Tabligo.Handlers.Company;
using Tabligo.Integrations;

namespace Tabligo.Handlers.Integration;

public class IntegrationListHandler(TabligoContext db, CompanyContextHandler companyContext)
{
    public async Task<IntegrationListResponse> HandleAsync(CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var activatedIntegrations = await db.Integrations
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(ct);
        
        var result = new IntegrationListResponse();
        foreach (var type in Enum.GetValues<IntegrationTypeEnum>())
        {
            var activated = activatedIntegrations.FirstOrDefault(x => x.Type == type);
            var metadata = IntegrationMetadata.Data[type];
            
            result.Integrations.Add(new IntegrationDto
            {
                Id = activated?.Id ?? 0,
                Type = type,
                Name = metadata.Name,
                Description = metadata.Description,
                IsActivated = activated?.IsActivated ?? false,
                IsAvailable = metadata.IsImplemented,
                ConfigurationStatus = activated != null ? "Configured" : "Not Configured"
            });
        }
        
        return result;
    }

    public async Task<IntegrationSearchResponse> SearchAsync(
        IntegrationSearchRequest request, 
        CancellationToken ct = default)
    {
        var companyId = await companyContext.CurrentCompanyIdAsync;
        var activatedIntegrations = await db.Integrations
            .Where(x => x.CompanyId == companyId)
            .ToListAsync(ct);

        // Build all integrations with metadata
        var allIntegrations = new List<IntegrationDto>();
        foreach (var type in Enum.GetValues<IntegrationTypeEnum>())
        {
            var activated = activatedIntegrations.FirstOrDefault(x => x.Type == type);
            var metadata = IntegrationMetadata.Data[type];
            
            allIntegrations.Add(new IntegrationDto
            {
                Id = activated?.Id ?? 0,
                Type = type,
                Name = metadata.Name,
                Description = metadata.Description,
                IsActivated = activated?.IsActivated ?? false,
                IsAvailable = metadata.IsImplemented,
                ConfigurationStatus = activated != null ? "Configured" : "Not Configured"
            });
        }

        // Apply search filters
        var filteredIntegrations = allIntegrations.AsQueryable();

        // Apply query filter (search in name and description)
        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            var query = request.Query.ToLowerInvariant();
            filteredIntegrations = filteredIntegrations.Where(x => 
                x.Name.ToLowerInvariant().Contains(query) ||
                x.Description.ToLowerInvariant().Contains(query) ||
                x.Type.ToString().ToLowerInvariant().Contains(query));
        }

        // Apply activation filter
        if (request.IsActivated.HasValue)
        {
            filteredIntegrations = filteredIntegrations.Where(x => x.IsActivated == request.IsActivated.Value);
        }

        // Apply availability filter
        if (request.IsAvailable.HasValue)
        {
            filteredIntegrations = filteredIntegrations.Where(x => x.IsAvailable == request.IsAvailable.Value);
        }

        // Get total count before pagination
        var totalCount = filteredIntegrations.Count();

        // Apply pagination
        var pagedIntegrations = filteredIntegrations
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .ToList();

        return new IntegrationSearchResponse
        {
            Items = pagedIntegrations,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}


