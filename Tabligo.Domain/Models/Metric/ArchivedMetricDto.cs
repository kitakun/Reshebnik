using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Metric;

public class ArchivedMetricDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
    public int EntityId { get; set; }
    public ArchiveMetricTypeEnum MetricType { get; set; }
}

