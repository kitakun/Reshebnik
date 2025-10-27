# PowerShell script to create placeholder integration providers

$integrations = @(
    "OneC",
    "OnlinePBX", 
    "YandexDirect",
    "VkAds",
    "TelegramAds",
    "FacebookAds",
    "Elama"
)

foreach ($integration in $integrations) {
    $className = $integration + "Provider"
    $enumValue = $integration
    
    $content = @"
using Tabligo.Domain.Enums;

namespace Tabligo.Integrations.Integrations.$integration;

public class $className : IIntegrationProvider
{
    public IntegrationTypeEnum Type => IntegrationTypeEnum.$enumValue;

    public async Task<bool> TestConnectionAsync(string configuration, CancellationToken ct)
    {
        await Task.Delay(100, ct); // Placeholder
        return true;
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
}
"@
    
    $filePath = "$integration\$className.cs"
    Set-Content -Path $filePath -Value $content
    Write-Host "Created $filePath"
}

Write-Host "All placeholder integration providers created!"


