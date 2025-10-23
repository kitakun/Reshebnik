namespace Tabligo.Domain.Entities;

public class MetricEmployeeLinkEntity
{
    public int Id { get; set; }

    public int MetricId { get; set; }
    public MetricEntity Metric { get; set; } = null!;

    public int EmployeeId { get; set; }
    public EmployeeEntity Employee { get; set; } = null!;
}
