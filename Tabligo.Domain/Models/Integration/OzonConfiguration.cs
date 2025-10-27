using System.Text.Json.Serialization;

namespace Tabligo.Domain.Models.Integration;

public class OzonConfiguration
{
    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("apiKey")]
    public string ApiKey { get; set; } = string.Empty;

    [JsonPropertyName("importProducts")]
    public bool ImportProducts { get; set; } = true;

    [JsonPropertyName("importPostings")]
    public bool ImportPostings { get; set; } = true;

    [JsonPropertyName("importReturns")]
    public bool ImportReturns { get; set; } = true;

    [JsonPropertyName("importActions")]
    public bool ImportActions { get; set; } = true;

    [JsonPropertyName("importFinancialReports")]
    public bool ImportFinancialReports { get; set; } = true;

    [JsonPropertyName("dateFrom")]
    public DateTime? DateFrom { get; set; }

    [JsonPropertyName("dateTo")]
    public DateTime? DateTo { get; set; }

    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 1000;

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; } = false;
}
