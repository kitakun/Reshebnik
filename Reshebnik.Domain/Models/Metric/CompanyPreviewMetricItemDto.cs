namespace Reshebnik.Domain.Models.Metric;

public class CompanyPreviewMetricItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int[] TotalPlanData { get; set; } = [];
    public int[] TotalFactData { get; set; } = [];
    public int[] Last12PointsPlan { get; set; } = [];
    public int[] Last12PointsFact { get; set; } = [];
    public bool IsArchived { get; set; }
}
