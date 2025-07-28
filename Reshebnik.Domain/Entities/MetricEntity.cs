using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Entities;

public class MetricEntity
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public CompanyEntity Company { get; set; } = null!;

    public List<MetricDepartmentLinkEntity> DepartmentLinks { get; set; } = new();
    public List<MetricEmployeeLinkEntity> EmployeeLinks { get; set; } = new();

    public string ClickHouseKey { get; set; } = string.Empty;

    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;

    public MetricUnitEnum Unit { get; set; }
    public MetricTypeEnum Type { get; set; }
    public PeriodTypeEnum PeriodType { get; set; }

    public WeekTypeEnum WeekType { get; set; } = WeekTypeEnum.Calendar;
    public DateTime? WeekStartDate { get; set; }
    public bool ShowGrowthPercent { get; set; }

    public decimal? Plan { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }

    public bool Visible { get; set; }
}
