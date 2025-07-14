using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Models.Metric;

public class PutCompanyPreviewMetricsDto
{
    public PutPreviewMetricItemDto Metrics { get; set; } = new();
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public PeriodTypeEnum PeriodType { get; set; }
}
