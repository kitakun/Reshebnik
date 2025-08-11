using System;
using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Entities;

public class ArchivedMetricEntity
{
    public int Id { get; set; }
    public int CompanyId { get; set; }
    public int MetricId { get; set; }
    public MetricEntity Metric { get; set; } = null!;
    public MetricTypeEnum MetricType { get; set; }
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
    public DateTime ArchivedAt { get; set; }
    public int ArchivedByUserId { get; set; }
    public EmployeeEntity ArchivedByUser { get; set; } = null!;
}
