using System;
using Reshebnik.Domain.Enums;
using Reshebnik.Domain.Models.Indicator;

namespace Reshebnik.Domain.Models.Metric;

public class ArchivedMetricGetDto
{
    public int Id { get; set; }
    public int MetricId { get; set; }
    public ArchiveMetricTypeEnum MetricType { get; set; }
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
    public DateTime ArchivedAt { get; set; }
    public int ArchivedByUserId { get; set; }
    public MetricDto Metric { get; set; } = new();
    public IndicatorDto Indicator { get; set; }
    public int[] Last12PointsPlan { get; set; } = [];
    public int[] Last12PointsFact { get; set; } = [];
}
