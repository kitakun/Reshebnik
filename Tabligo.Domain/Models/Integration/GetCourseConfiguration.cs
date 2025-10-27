using System.Text.Json.Serialization;

namespace Tabligo.Domain.Models.Integration;

public class GetCourseConfiguration
{
    [JsonPropertyName("accountName")]
    public string AccountName { get; set; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("importType")]
    public string? ImportType { get; set; } // "users", "groups", "orders", "payments", "all"

    [JsonPropertyName("testConnection")]
    public bool TestConnection { get; set; } = false;

    [JsonPropertyName("importUsers")]
    public bool ImportUsers { get; set; } = true;

    [JsonPropertyName("importGroups")]
    public bool ImportGroups { get; set; } = true;

    [JsonPropertyName("importOrders")]
    public bool ImportOrders { get; set; } = true;

    [JsonPropertyName("importPayments")]
    public bool ImportPayments { get; set; } = true;

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; } = false;
}
