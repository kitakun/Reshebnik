using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.GoogleSheets;

public class GoogleSheetsApiClient(HttpClient httpClient, ILogger<GoogleSheetsApiClient> logger)
{
    private const string GoogleOAuthTokenUrl = "https://oauth2.googleapis.com/token";
    private const string GoogleSheetsApiBaseUrl = "https://sheets.googleapis.com/v4/spreadsheets";

    public async Task<List<List<string>>> GetSpreadsheetDataAsync(
        GoogleSheetsConfiguration config,
        string? range = null,
        CancellationToken ct = default)
    {
        try
        {
            // Public mode: read published CSV without credentials
            if (config.IsPublic)
            {
                var csvUrl = BuildPublicCsvUrl(config, range);
                logger.LogInformation($"Fetching data from Google Sheets (public CSV): {csvUrl}");
                var csvResponse = await httpClient.GetStringAsync(csvUrl, ct);
                return ParseCsvToValues(csvResponse);
            }

            // Private mode: Ensure we have a valid access token
            var token = await EnsureValidAccessTokenAsync(config, ct);

            // Build the range parameter
            var sheetRange = range ?? config.Range;
            if (string.IsNullOrEmpty(sheetRange) && !string.IsNullOrEmpty(config.SheetName))
            {
                sheetRange = config.SheetName;
            }

            // Build the URL
            var url = $"{GoogleSheetsApiBaseUrl}/{config.SpreadsheetId}/values/{sheetRange}";
            
            logger.LogInformation($"Fetching data from Google Sheets: {url}");

            // Create request with authorization header
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogError($"Google Sheets API request failed: {response.StatusCode} - {responseContent}");
                throw new HttpRequestException($"Failed to fetch spreadsheet data: {response.StatusCode}");
            }

            // Parse the response
            var sheetResponse = JsonSerializer.Deserialize<GoogleSheetsValueResponse>(responseContent);
            
            if (sheetResponse?.Values == null)
            {
                return new List<List<string>>();
            }

            return sheetResponse.Values;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to fetch data from Google Sheets");
            throw;
        }
    }

    public async Task<GoogleSheetsTestConnectionResult> TestConnectionAsync(
        GoogleSheetsConfiguration config,
        CancellationToken ct = default)
    {
        try
        {
            if (string.IsNullOrEmpty(config.SpreadsheetId))
            {
                return new GoogleSheetsTestConnectionResult
                {
                    IsSuccess = false,
                    Reason = "Spreadsheet ID is required"
                };
            }

            if (config.IsPublic)
            {
                try
                {
                    var csvUrl = BuildPublicCsvUrl(config, config.Range);
                    var head = new HttpRequestMessage(HttpMethod.Head, csvUrl);
                    var resp = await httpClient.SendAsync(head, ct);
                    if (!resp.IsSuccessStatusCode)
                    {
                        return new GoogleSheetsTestConnectionResult
                        {
                            IsSuccess = false,
                            Reason = $"Public CSV not accessible: {resp.StatusCode}"
                        };
                    }

                    return new GoogleSheetsTestConnectionResult
                    {
                        IsSuccess = true,
                        Reason = "Public CSV accessible",
                        SpreadsheetTitle = null
                    };
                }
                catch (Exception e)
                {
                    return new GoogleSheetsTestConnectionResult
                    {
                        IsSuccess = false,
                        Reason = $"Public access failed: {e.Message}"
                    };
                }
            }

            // Private mode: Try to get spreadsheet metadata to test connection
            var token = await EnsureValidAccessTokenAsync(config, ct);
            var url = $"{GoogleSheetsApiBaseUrl}/{config.SpreadsheetId}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await httpClient.SendAsync(request, ct);
            var responseContent = await response.Content.ReadAsStringAsync(ct);

            if (response.IsSuccessStatusCode)
            {
                var metadata = JsonSerializer.Deserialize<GoogleSheetsMetadataResponse>(responseContent);
                return new GoogleSheetsTestConnectionResult
                {
                    IsSuccess = true,
                    Reason = "Successfully connected to Google Sheets",
                    SpreadsheetTitle = metadata?.Properties?.Title
                };
            }

            return new GoogleSheetsTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Failed to connect: {response.StatusCode} - {responseContent}"
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Google Sheets connection test failed");
            return new GoogleSheetsTestConnectionResult
            {
                IsSuccess = false,
                Reason = $"Connection test failed: {ex.Message}"
            };
        }
    }

    private string BuildPublicCsvUrl(GoogleSheetsConfiguration config, string? range)
    {
        if (!string.IsNullOrEmpty(config.PublicCsvUrl))
        {
            return config.PublicCsvUrl;
        }

        // If no explicit URL provided, build default export link based on SpreadsheetId and optional gid
        // Note: range selection is not supported on CSV export URL; users can publish specific sheets via gid
        var gid = ExtractGidFromRangeOrSheet(config, range);
        var baseUrl = $"https://docs.google.com/spreadsheets/d/{config.SpreadsheetId}/export?format=csv";
        if (!string.IsNullOrEmpty(gid))
        {
            baseUrl += $"&gid={gid}";
        }
        return baseUrl;
    }

    private string? ExtractGidFromRangeOrSheet(GoogleSheetsConfiguration config, string? range)
    {
        // Expect users to pass gid via Range like "gid=123456" or store SheetName as gid prefixed
        var candidate = range ?? config.Range ?? config.SheetName;
        if (string.IsNullOrEmpty(candidate)) return null;
        var trimmed = candidate.Trim();
        if (trimmed.StartsWith("gid=", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed.Substring(4);
        }
        return null;
    }

    private List<List<string>> ParseCsvToValues(string csv)
    {
        var result = new List<List<string>>();
        using var reader = new StringReader(csv);
        while (reader.ReadLine() is { } line)
        {
            // Basic CSV split; handles quoted fields with commas
            var row = new List<string>();
            var current = new StringBuilder();
            var inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++; // skip escaped quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == ',' && !inQuotes)
                {
                    row.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }
            row.Add(current.ToString());
            result.Add(row);
        }
        return result;
    }

    private async Task<string> EnsureValidAccessTokenAsync(GoogleSheetsConfiguration config, CancellationToken ct)
    {
        // For now, return the access token from config
        // In a production environment, you should check if the token is expired
        // and refresh it using the refresh token if available
        if (!string.IsNullOrEmpty(config.AccessToken))
        {
            return config.AccessToken;
        }

        // If access token is missing, throw an exception
        throw new InvalidOperationException("Access token is required but not provided in configuration");
    }

    private async Task<string> RefreshAccessTokenAsync(GoogleSheetsConfiguration config, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(config.RefreshToken) || 
            string.IsNullOrEmpty(config.ClientId) || 
            string.IsNullOrEmpty(config.ClientSecret))
        {
            throw new InvalidOperationException("Refresh token, client ID, and client secret are required for token refresh");
        }

        var requestBody = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", config.RefreshToken },
            { "client_id", config.ClientId },
            { "client_secret", config.ClientSecret }
        };

        var content = new FormUrlEncodedContent(requestBody);
        var response = await httpClient.PostAsync(GoogleOAuthTokenUrl, content, ct);
        var responseContent = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Failed to refresh access token: {response.StatusCode} - {responseContent}");
        }

        var tokenResponse = JsonSerializer.Deserialize<GoogleOAuthTokenResponse>(responseContent);
        return tokenResponse?.AccessToken ?? throw new InvalidOperationException("Access token not found in response");
    }
}

// Response models for Google Sheets API
public class GoogleSheetsValueResponse
{
    [JsonPropertyName("range")]
    public string? Range { get; set; }

    [JsonPropertyName("values")]
    public List<List<string>>? Values { get; set; }
}

public class GoogleSheetsMetadataResponse
{
    [JsonPropertyName("properties")]
    public GoogleSheetsProperties? Properties { get; set; }
}

public class GoogleSheetsProperties
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class GoogleOAuthTokenResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }
}

public class GoogleSheetsTestConnectionResult
{
    public bool IsSuccess { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? SpreadsheetTitle { get; set; }
}
