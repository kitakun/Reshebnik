using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Metric;

public class CompanyPreviewMetricItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public decimal? Plan { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public int[] TotalPlanData { get; set; } = [];
    public int[] TotalFactData { get; set; } = [];
    public int[] Last12PointsPlan { get; set; } = [];
    public int[] Last12PointsFact { get; set; } = [];
    public bool IsArchived { get; set; }
    public ArchiveMetricTypeEnum MetricType { get; set; }
}
