using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Tabligo.Domain.Models.Integration;

namespace Tabligo.Integrations.Integrations.PowerBI;

public class PowerBIApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PowerBIApiClient> _logger;
    private string? _accessToken;
    private DateTime _tokenExpiresAt = DateTime.MinValue;

    public PowerBIApiClient(HttpClient httpClient, ILogger<PowerBIApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> TestConnectionAsync(PowerBIConfiguration config, CancellationToken ct = default)
    {
        try
        {
            await EnsureValidTokenAsync(config, ct);
            return !string.IsNullOrEmpty(_accessToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PowerBI connection test failed");
            return false;
        }
    }

    public async Task<List<PowerBIWorkspace>> GetWorkspacesAsync(PowerBIConfiguration config, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(config, ct);
        
        var url = "https://api.powerbi.com/v1.0/myorg/groups";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<PowerBIWorkspaceResponse>(content);
        
        return result?.Value ?? new List<PowerBIWorkspace>();
    }

    public async Task<List<PowerBIDataset>> GetDatasetsAsync(PowerBIConfiguration config, string? workspaceId = null, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(config, ct);
        
        var url = string.IsNullOrEmpty(workspaceId) 
            ? "https://api.powerbi.com/v1.0/myorg/datasets"
            : $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/datasets";
            
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<PowerBIDatasetResponse>(content);
        
        return result?.Value ?? new List<PowerBIDataset>();
    }

    public async Task<List<PowerBIReport>> GetReportsAsync(PowerBIConfiguration config, string? workspaceId = null, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(config, ct);
        
        var url = string.IsNullOrEmpty(workspaceId) 
            ? "https://api.powerbi.com/v1.0/myorg/reports"
            : $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/reports";
            
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<PowerBIReportResponse>(content);
        
        return result?.Value ?? new List<PowerBIReport>();
    }

    public async Task<List<PowerBIDashboard>> GetDashboardsAsync(PowerBIConfiguration config, string? workspaceId = null, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(config, ct);
        
        var url = string.IsNullOrEmpty(workspaceId) 
            ? "https://api.powerbi.com/v1.0/myorg/dashboards"
            : $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/dashboards";
            
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<PowerBIDashboardResponse>(content);
        
        return result?.Value ?? new List<PowerBIDashboard>();
    }

    public async Task<List<PowerBIUser>> GetWorkspaceUsersAsync(PowerBIConfiguration config, string workspaceId, CancellationToken ct = default)
    {
        await EnsureValidTokenAsync(config, ct);
        
        var url = $"https://api.powerbi.com/v1.0/myorg/groups/{workspaceId}/users";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _accessToken);

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();

        var content = await response.Content.ReadAsStringAsync(ct);
        var result = JsonSerializer.Deserialize<PowerBIWorkspaceUserResponse>(content);
        
        return result?.Value ?? new List<PowerBIUser>();
    }

    private async Task EnsureValidTokenAsync(PowerBIConfiguration config, CancellationToken ct)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTime.UtcNow < _tokenExpiresAt.AddMinutes(-5))
        {
            return; // Token is still valid
        }

        await RefreshAccessTokenAsync(config, ct);
    }

    private async Task RefreshAccessTokenAsync(PowerBIConfiguration config, CancellationToken ct)
    {
        var tokenUrl = $"https://login.microsoftonline.com/{config.TenantId}/oauth2/v2.0/token";
        
        var formData = new List<KeyValuePair<string, string>>
        {
            new("client_id", config.ClientId),
            new("client_secret", config.ClientSecret),
            new("scope", "https://analysis.windows.net/powerbi/api/.default"),
            new("grant_type", "client_credentials")
        };

        var content = new FormUrlEncodedContent(formData);
        var response = await _httpClient.PostAsync(tokenUrl, content, ct);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(ct);
            throw new HttpRequestException($"Failed to get PowerBI access token: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(ct);
        var tokenResponse = JsonSerializer.Deserialize<PowerBIAccessTokenResponse>(responseContent);
        
        if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
        {
            throw new InvalidOperationException("Failed to parse PowerBI access token response");
        }

        _accessToken = tokenResponse.AccessToken;
        _tokenExpiresAt = DateTime.UtcNow.AddSeconds(tokenResponse.ExpiresIn);
        
        _logger.LogInformation("PowerBI access token refreshed successfully");
    }
}

// Response wrapper classes
public class PowerBIWorkspaceResponse
{
    [JsonPropertyName("value")]
    public List<PowerBIWorkspace> Value { get; set; } = new();
}

public class PowerBIDatasetResponse
{
    [JsonPropertyName("value")]
    public List<PowerBIDataset> Value { get; set; } = new();
}

public class PowerBIReportResponse
{
    [JsonPropertyName("value")]
    public List<PowerBIReport> Value { get; set; } = new();
}

public class PowerBIDashboardResponse
{
    [JsonPropertyName("value")]
    public List<PowerBIDashboard> Value { get; set; } = new();
}

public class PowerBIWorkspaceUserResponse
{
    [JsonPropertyName("value")]
    public List<PowerBIUser> Value { get; set; } = new();
}
