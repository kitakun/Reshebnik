using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Models.Metric;

public class PutPreviewMetricsDto
{
    public UserPreviewMetricsDto Metrics { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public PeriodTypeEnum Period { get; set; }
}