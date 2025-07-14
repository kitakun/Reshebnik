namespace Reshebnik.Domain.Models.Metric;

public class PutPreviewMetricItemDto
{
    public int Id { get; set; }
    public int[] PlanData { get; set; } = [];
    public int[] FactData { get; set; } = [];
}
