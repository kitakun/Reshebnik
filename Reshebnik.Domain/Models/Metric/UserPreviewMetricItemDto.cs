using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Models.Metric;

public class UserPreviewMetricItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public PeriodTypeEnum Period { get; set; }
    public MetricTypeEnum Type { get; set; }
    public int[] PlanData { get; set; } = [];
    public int[] FactData { get; set; } = [];
    public int[] TotalPlanData { get; set; } = [];
    public int[] TotalFactData { get; set; } = [];
    public double Average { get; set; }
}