using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Metric;

public class PutPreviewMetricsDto
{
    public List<PutPreviewMetricItemDto> Metrics { get; set; } = new();
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public PeriodTypeEnum PeriodType { get; set; }
}