namespace Reshebnik.Domain.Models.Metric;

public class CompanyPreviewMetricItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int[] PlanData { get; set; } = [];
    public int[] FactData { get; set; } = [];
}
