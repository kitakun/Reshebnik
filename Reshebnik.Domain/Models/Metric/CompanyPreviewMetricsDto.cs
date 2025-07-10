using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Models.Metric;

public class CompanyPreviewMetricsDto
{
    public CompanyPreviewMetricItemDto Metrics { get; set; } = new();
    public string CategoryName { get; set; } = string.Empty;
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public PeriodTypeEnum PeriodType { get; set; }
}
