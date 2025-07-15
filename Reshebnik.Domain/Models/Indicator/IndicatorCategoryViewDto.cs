namespace Reshebnik.Domain.Models.Indicator;

public class IndicatorCategoryViewDto
{
    public string CategoryName { get; set; } = string.Empty;
    public string Comment { get; set; } = string.Empty;
    public List<IndicatorCategoryMetricDto> Metrics { get; set; } = new();
}
