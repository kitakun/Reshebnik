using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.OnlinePBX;

public class OnlinePBXProvider : IIntegrationProvider
{
    public IntegrationTypeEnum Type => IntegrationTypeEnum.OnlinePBX;

        public async Task<bool> TestConnectionAsync(string configuration, CancellationToken ct)
    {
        var result = await TestConnectionDetailedAsync(configuration, ct);
        return result.IsSuccess;
    }

    public async Task<IntegrationTestConnectionResult> TestConnectionDetailedAsync(string configuration, CancellationToken ct)
    {
        // TODO: Implement OnlinePBX API connection test
        await Task.Delay(100, ct); // Placeholder
        return new IntegrationTestConnectionResult
        {
            IsSuccess = false,
            Reason = "Интеграция с Online PBX еще не реализована",
            Endpoint = "OnlinePBX API",
            IntegrationType = IntegrationTypeEnum.OnlinePBX
        };
    }

    public async Task<bool> ActivateAsync(int companyId, string configuration, CancellationToken ct)
    {
        await Task.Delay(100, ct); // Placeholder
        return true;
    }

    public async Task<bool> DeactivateAsync(int companyId, CancellationToken ct)
    {
        await Task.Delay(100, ct); // Placeholder
        return true;
    }

    public async Task<List<IntegrationImportRequest>> FetchDataAsync(
        int companyId, 
        string configuration,
        Dictionary<string, object>? filters = null,
        CancellationToken ct = default)
    {
        await Task.Delay(100, ct); // Placeholder
        return new List<IntegrationImportRequest>();
    }
}

