using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Models.Metric;

public class MetricPutDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public MetricUnitEnum Unit { get; set; }
    public MetricTypeEnum Type { get; set; }
    public int? DepartmentId { get; set; }
    public int? EmployeeId { get; set; }
    public decimal? Plan { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public bool Visible { get; set; }
}
