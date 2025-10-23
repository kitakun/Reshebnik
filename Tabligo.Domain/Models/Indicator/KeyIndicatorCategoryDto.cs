namespace Tabligo.Domain.Models.Indicator;

public class KeyIndicatorCategoryDto
{
    public string CategoryName { get; set; } = string.Empty;
    public List<KeyIndicatorItemDto> Metrics { get; set; } = new();
}
