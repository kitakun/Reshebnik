using Tabligo.Domain.Enums;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.AmoCRM;

public class AmoCRMProvider : IIntegrationProvider
{
    public IntegrationTypeEnum Type => IntegrationTypeEnum.AmoCRM;

        public async Task<bool> TestConnectionAsync(string configuration, CancellationToken ct)
    {
        var result = await TestConnectionDetailedAsync(configuration, ct);
        return result.IsSuccess;
    }

    public async Task<IntegrationTestConnectionResult> TestConnectionDetailedAsync(string configuration, CancellationToken ct)
    {
        // TODO: Implement AmoCRM API connection test
        await Task.Delay(100, ct); // Placeholder
        return new IntegrationTestConnectionResult
        {
            IsSuccess = false,
            Reason = "Интеграция с AmoCRM еще не реализована",
            Endpoint = "AmoCRM API",
            IntegrationType = IntegrationTypeEnum.AmoCRM
        };
    }

    public async Task<bool> ActivateAsync(int companyId, string configuration, CancellationToken ct)
    {
        // TODO: Implement AmoCRM activation logic
        await Task.Delay(100, ct); // Placeholder
        return true;
    }

    public async Task<bool> DeactivateAsync(int companyId, CancellationToken ct)
    {
        // TODO: Implement AmoCRM deactivation logic
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


