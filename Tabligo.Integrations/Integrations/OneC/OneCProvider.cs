using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.OneC;

public class OneCProvider : IIntegrationProvider
{
    public IntegrationTypeEnum Type => IntegrationTypeEnum.OneC;

        public async Task<bool> TestConnectionAsync(string configuration, CancellationToken ct)
    {
        var result = await TestConnectionDetailedAsync(configuration, ct);
        return result.IsSuccess;
    }

    public async Task<IntegrationTestConnectionResult> TestConnectionDetailedAsync(string configuration, CancellationToken ct)
    {
        // TODO: Implement OneC API connection test
        await Task.Delay(100, ct); // Placeholder
        return new IntegrationTestConnectionResult
        {
            IsSuccess = false,
            Reason = "Интеграция с 1C еще не реализована",
            Endpoint = "OneC API",
            IntegrationType = IntegrationTypeEnum.OneC
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

