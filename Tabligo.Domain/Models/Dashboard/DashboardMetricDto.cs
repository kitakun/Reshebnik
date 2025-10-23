using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Dashboard;

public class DashboardMetricDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public int[] Plan { get; set; } = [];
    public int[] Fact { get; set; } = [];
    public PeriodTypeEnum PeriodType { get; set; }
    public bool IsArchived { get; set; }
    public ArchiveMetricTypeEnum MetricType { get; set; }
}
