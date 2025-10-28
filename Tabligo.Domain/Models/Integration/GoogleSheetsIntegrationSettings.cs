using System.Text.Json.Serialization;

namespace Tabligo.Domain.Models.Integration;

public class GoogleSheetsIntegrationSettings
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("spreadsheetId")]
    public string SpreadsheetId { get; set; } = string.Empty;

    [JsonPropertyName("sheetName")]
    public string? SheetName { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("lastSyncDate")]
    public string? LastSyncDate { get; set; }

    [JsonPropertyName("importSettings")]
    public GoogleSheetsImportSettings ImportSettings { get; set; } = new();
}

public class GoogleSheetsImportSettings : IImportSettingsModel
{
    [JsonPropertyName("importRows")]
    public bool ImportRows { get; set; }

    [JsonPropertyName("hasHeaderRow")]
    public bool HasHeaderRow { get; set; }

    [JsonPropertyName("range")]
    public string? Range { get; set; }
}

