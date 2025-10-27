namespace Tabligo.Clickhouse.Models;

public class IndicatorValue
{
    public DateTime Date { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal PaidAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string ExternalId { get; set; } = string.Empty;
}
