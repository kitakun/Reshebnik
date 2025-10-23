using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Indicator;

public class IndicatorCategoryMetricDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public IndicatorUnitTypeEnum UnitType { get; set; }
    public IndicatorValueTypeEnum ValueType { get; set; }
    public bool HasEmployees { get; set; }
    public bool IsArchived { get; set; }
    public KeyIndicatorMetricsDto Metrics { get; set; } = new();
}
