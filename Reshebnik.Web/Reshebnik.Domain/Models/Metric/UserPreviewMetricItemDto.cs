namespace Reshebnik.Domain.Models.Metric;

public class UserPreviewMetricItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int[] PlanData { get; set; } = Array.Empty<int>();
    public int[] FactData { get; set; } = Array.Empty<int>();
    public double Average { get; set; }
}