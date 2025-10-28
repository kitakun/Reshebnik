using System.Text.Json.Serialization;

namespace Tabligo.Domain.Models.Integration;

public class GoogleSheetsConfiguration
{
    [JsonPropertyName("spreadsheetId")]
    public string SpreadsheetId { get; set; } = string.Empty;

    [JsonPropertyName("sheetName")]
    public string? SheetName { get; set; }

    [JsonPropertyName("accessToken")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; } = string.Empty;

    [JsonPropertyName("importRows")]
    public bool ImportRows { get; set; } = true;

    [JsonPropertyName("hasHeaderRow")]
    public bool HasHeaderRow { get; set; } = true;

    [JsonPropertyName("range")]
    public string? Range { get; set; }

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; } = false;

    [JsonPropertyName("isPublic")]
    public bool IsPublic { get; set; } = false;

    // When using public access, prefer providing a published CSV URL
    // like: https://docs.google.com/spreadsheets/d/{id}/export?format=csv&gid={gid}
    [JsonPropertyName("publicCsvUrl")]
    public string? PublicCsvUrl { get; set; }
}
