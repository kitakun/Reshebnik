using System.Text.Json.Serialization;

namespace Tabligo.Domain.Models.Integration;

public class PowerBIConfiguration
{
    [JsonPropertyName("tenantId")]
    public string TenantId { get; set; } = string.Empty;

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("workspaceId")]
    public string? WorkspaceId { get; set; }

    [JsonPropertyName("importWorkspaces")]
    public bool ImportWorkspaces { get; set; } = true;

    [JsonPropertyName("importDatasets")]
    public bool ImportDatasets { get; set; } = true;

    [JsonPropertyName("importReports")]
    public bool ImportReports { get; set; } = true;

    [JsonPropertyName("importDashboards")]
    public bool ImportDashboards { get; set; } = true;

    [JsonPropertyName("importUsers")]
    public bool ImportUsers { get; set; } = true;

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; } = false;
}
