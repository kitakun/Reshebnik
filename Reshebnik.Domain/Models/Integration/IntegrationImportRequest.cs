using System.Text.Json.Serialization;

namespace Reshebnik.Domain.Models.Integration;

public class IntegrationImportRequest
{
    [JsonPropertyName("entityType")]
    public required string EntityType { get; set; }

    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();

    [JsonPropertyName("confidence")]
    public decimal Confidence { get; set; }

    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;
}
