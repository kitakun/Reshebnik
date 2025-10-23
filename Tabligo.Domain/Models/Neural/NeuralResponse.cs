namespace Tabligo.Domain.Models.Neural;

public class NeuralResponse
{
    public List<SuggestedEntity> SuggestedEntities { get; set; } = new();
    public string AnalysisSummary { get; set; } = string.Empty;
    public bool IsSuccessful { get; set; }
    public string? ErrorMessage { get; set; }
}

public class SuggestedEntity
{
    public EntityType EntityType { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, object> Properties { get; set; } = new();
    public double Confidence { get; set; }
    public string Reasoning { get; set; } = string.Empty;
}

public enum EntityType
{
    Company,
    Department,
    Employee,
    Metric,
    Indicator
}

