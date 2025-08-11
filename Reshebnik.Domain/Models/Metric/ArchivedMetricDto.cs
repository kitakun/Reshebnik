using System;

namespace Reshebnik.Domain.Models.Metric;

public class ArchivedMetricDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime FirstDate { get; set; }
    public DateTime LastDate { get; set; }
}

