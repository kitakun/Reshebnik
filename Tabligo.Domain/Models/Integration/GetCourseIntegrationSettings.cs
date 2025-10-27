using System.Text.Json.Serialization;

namespace Tabligo.Domain.Models.Integration;

public interface IImportSettingsModel
{
}

public class GetCourseIntegrationSettings
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("lastSyncDate")]
    public string? LastSyncDate { get; set; }

    [JsonPropertyName("importSettings")]
    public GetCourseImportSettings ImportSettings { get; set; } = new();
}

public class GetCourseImportSettings : IImportSettingsModel
{
    [JsonPropertyName("importUsers")]
    public bool ImportUsers { get; set; }

    [JsonPropertyName("importOrders")]
    public bool ImportOrders { get; set; }

    [JsonPropertyName("importGroups")]
    public bool ImportGroups { get; set; }

    [JsonPropertyName("importPayments")]
    public bool ImportPayments { get; set; }
}
