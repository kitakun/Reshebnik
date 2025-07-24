namespace Reshebnik.Domain.Entities;

public class MetricDepartmentLinkEntity
{
    public int Id { get; set; }

    public int MetricId { get; set; }
    public MetricEntity Metric { get; set; } = null!;

    public int DepartmentId { get; set; }
    public DepartmentEntity Department { get; set; } = null!;
}
