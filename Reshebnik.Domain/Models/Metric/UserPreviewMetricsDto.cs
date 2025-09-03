namespace Reshebnik.Domain.Models.Metric;

public class UserPreviewMetricsDto
{
    public string Fio { get; set; } = null!;
    public string UserComment { get; set; } = string.Empty;
    public List<UserPreviewMetricItemDto> Metrics { get; set; } = new();
}

