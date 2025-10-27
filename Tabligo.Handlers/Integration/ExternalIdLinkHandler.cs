using Microsoft.EntityFrameworkCore;
using Tabligo.Domain.Enums;
using Tabligo.EntityFramework;

namespace Tabligo.Handlers.Integration;

public class ExternalIdLinkHandler(TabligoContext db)
{
    public async Task<int?> GetEntityIdAsync(int companyId, string externalId, IntegrationTypeEnum integrationType, string entityType, CancellationToken ct = default)
    {
        var link = await db.ExternalIdLinks
            .FirstOrDefaultAsync(
                l => l.CompanyId == companyId 
                     && l.ExternalId == externalId 
                     && l.IntegrationType == integrationType 
                     && l.EntityType == entityType,
                ct);
        
        return link?.EntityId;
    }
    
    public async Task LinkAsync(int companyId, string externalId, IntegrationTypeEnum integrationType, string entityType, int entityId, CancellationToken ct = default)
    {
        var existingLink = await db.ExternalIdLinks
            .FirstOrDefaultAsync(
                l => l.CompanyId == companyId 
                     && l.ExternalId == externalId 
                     && l.IntegrationType == integrationType 
                     && l.EntityType == entityType,
                ct);
        
        if (existingLink == null)
        {
            var link = new Tabligo.Domain.Entities.ExternalIdLinkEntity
            {
                CompanyId = companyId,
                ExternalId = externalId,
                IntegrationType = integrationType,
                EntityType = entityType,
                EntityId = entityId
            };
            db.ExternalIdLinks.Add(link);
        }
        else
        {
            existingLink.EntityId = entityId;
        }
        
        await db.SaveChangesAsync(ct);
    }
    
    public async Task<T?> FindEntityAsync<T>(int companyId, string externalId, IntegrationTypeEnum integrationType, CancellationToken ct = default) where T : class
    {
        var entityTypeName = GetEntityTypeName<T>();
        var entityId = await GetEntityIdAsync(companyId, externalId, integrationType, entityTypeName, ct);
        
        if (!entityId.HasValue)
            return null;
        
        return await db.Set<T>()
            .FirstOrDefaultAsync(e => EF.Property<int>(e, "Id") == entityId.Value, ct);
    }
    
    public async Task ClearTempLinksAsync(int companyId, CancellationToken ct = default)
    {
        var tempLinks = await db.ExternalIdLinks
            .Where(l => l.CompanyId == companyId && l.ExternalId.StartsWith("temp-"))
            .ToListAsync(ct);
        
        db.ExternalIdLinks.RemoveRange(tempLinks);
        await db.SaveChangesAsync(ct);
    }
    
    private static string GetEntityTypeName<T>()
    {
        return typeof(T).Name;
    }
}



