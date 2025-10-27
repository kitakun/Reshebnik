using System.Text.Json.Serialization;

namespace Tabligo.Domain.Models.Integration;

public class PowerBIIntegrationSettings
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("lastSyncDate")]
    public string? LastSyncDate { get; set; }

    [JsonPropertyName("importSettings")]
    public PowerBIImportSettings ImportSettings { get; set; } = new();
}

public class PowerBIImportSettings : IImportSettingsModel
{
    [JsonPropertyName("importDatasets")]
    public bool ImportDatasets { get; set; }

    [JsonPropertyName("importReports")]
    public bool ImportReports { get; set; }

    [JsonPropertyName("importDashboards")]
    public bool ImportDashboards { get; set; }
}

public class OzonIntegrationSettings
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("lastSyncDate")]
    public string? LastSyncDate { get; set; }

    [JsonPropertyName("importSettings")]
    public OzonImportSettings ImportSettings { get; set; } = new();
}

public class OzonImportSettings : IImportSettingsModel
{
    [JsonPropertyName("importProducts")]
    public bool ImportProducts { get; set; }

    [JsonPropertyName("importPostings")]
    public bool ImportPostings { get; set; }

    [JsonPropertyName("importReturns")]
    public bool ImportReturns { get; set; }
}
