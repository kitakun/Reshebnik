using Tabligo.Domain.Enums;

namespace Tabligo.Domain.Models.Indicator;

public class KeyIndicatorMetricsDto
{
    public int[] Plan { get; set; } = [0, 0, 0];
    public int[] Fact { get; set; } = [0, 0, 0];
    public double Average { get; set; } = 100;
    public FillmentPeriodEnum Period { get; set; }
}
