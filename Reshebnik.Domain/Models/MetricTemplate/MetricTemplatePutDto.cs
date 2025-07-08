using Reshebnik.Domain.Enums;

namespace Reshebnik.Domain.Models.MetricTemplate;

public class MetricTemplatePutDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string Description { get; set; } = string.Empty;
    public MetricUnitEnum Unit { get; set; }
    public MetricTypeEnum Type { get; set; }
    public PeriodTypeEnum PeriodType { get; set; }
    public decimal? Plan { get; set; }
    public decimal? Min { get; set; }
    public decimal? Max { get; set; }
    public bool Visible { get; set; }
}
