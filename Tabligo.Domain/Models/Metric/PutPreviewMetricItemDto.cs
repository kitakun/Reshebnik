using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Metric;

public class PutPreviewMetricItemDto
{
    public int Id { get; set; }
    public int[] PlanData { get; set; } = [];
    public int[] FactData { get; set; } = [];
    public bool IsArchived { get; set; }
    public ArchiveMetricTypeEnum MetricType { get; set; }
}
