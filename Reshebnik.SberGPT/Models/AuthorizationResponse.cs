using System.Text.Json.Serialization;

namespace Reshebnik.SberGPT.Models;

public class AuthorizationResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }
}

public class OAuthResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_at")]
    public long ExpiresAt { get; set; }
}

public class AuthorizationResult
{
    public bool AuthorizationSuccess { get; set; }
    public string ErrorTextIfFailed { get; set; } = string.Empty;
    public AuthorizationResponse? GigaChatAuthorizationResponse { get; set; }
}
