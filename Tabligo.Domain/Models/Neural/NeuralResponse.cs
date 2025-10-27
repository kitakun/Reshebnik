using System.Text.Json.Serialization;

namespace Tabligo.Domain.Models.Neural;

public class NeuralResponse
{
    [JsonPropertyName("suggestedEntities")]
    public List<SuggestedEntity> SuggestedEntities { get; set; } = new();
    
    [JsonPropertyName("analysisSummary")]
    public string AnalysisSummary { get; set; } = string.Empty;
    
    [JsonPropertyName("isSuccessful")]
    public bool IsSuccessful { get; set; }
    
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage { get; set; }
}

public class SuggestedEntity
{
    [JsonPropertyName("entityType")]
    public EntityType EntityType { get; set; }
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("properties")]
    public Dictionary<string, object> Properties { get; set; } = new();
    
    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }
    
    [JsonPropertyName("reasoning")]
    public string Reasoning { get; set; } = string.Empty;
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EntityType
{
    Company,
    Department,
    Employee,
    Metric,
    Indicator
}

