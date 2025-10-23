using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Metric;

public class MetricDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public MetricUnitEnum Unit { get; set; }
    public MetricTypeEnum Type { get; set; }
    public PeriodTypeEnum PeriodType { get; set; }
    public WeekTypeEnum WeekType { get; set; }
    public DateTime? WeekStartDate { get; set; }
    public bool ShowGrowthPercent { get; set; }
    public int[] DepartmentIds { get; set; } = [];
    public int[] EmployeeIds { get; set; } = [];
    public decimal? Plan { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public bool Visible { get; set; }
    public bool IsArchived { get; set; }
    public ArchiveMetricTypeEnum MetricType { get; set; }
}
