using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reshebnik.SberGPT.Models;
using System.Text;
using System.Text.Json;

namespace Reshebnik.SberGPT.Services;

public class SberGptService : ISberGptService
{
    private readonly HttpClient _httpClient;
    private readonly SberGptOptions _options;
    private readonly ILogger<SberGptService> _logger;
    private AuthorizationResult? _authorization;

    public SberGptService(
        HttpClient httpClient,
        IOptions<SberGptOptions> options,
        ILogger<SberGptService> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<string> HandleAsync(string request)
    {
        try
        {
            // Ensure we have a valid authorization token
            await EnsureAuthorizedAsync();

            if (_authorization?.AuthorizationSuccess != true)
            {
                return $"Authorization failed: {_authorization?.ErrorTextIfFailed}";
            }

            // Send completion request
            var completionResult = await SendCompletionRequestAsync(request);

            if (!completionResult.RequestSuccessed)
            {
                return $"Request failed: {completionResult.ErrorTextIfFailed}";
            }

            // Extract response content
            var response = completionResult.GigaChatCompletionResponse?.Choices?.LastOrDefault()?.Message?.Content;
            return response ?? "No response received";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SberGptService.HandleAsync");
            return $"Error: {ex.Message}";
        }
    }

    public async IAsyncEnumerable<string> HandleStreamAsync(string request, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // Ensure we have a valid authorization token
        await EnsureAuthorizedAsync();

        if (_authorization?.AuthorizationSuccess != true)
        {
            yield return $"Authorization failed: {_authorization?.ErrorTextIfFailed}";
            yield break;
        }

        // Send streaming completion request
        await foreach (var chunk in SendStreamingCompletionRequestAsync(request, cancellationToken))
        {
            yield return chunk;
        }
    }

    private async Task EnsureAuthorizedAsync()
    {
        if (_authorization?.AuthorizationSuccess == true && 
            _authorization.GigaChatAuthorizationResponse?.ExpiresAt > DateTimeOffset.UtcNow.ToUnixTimeSeconds())
        {
            return; // Token is still valid
        }

        await AuthorizeAsync();
    }

    private async Task AuthorizeAsync()
    {
        try
        {
            // Generate RqUID for the request
            var rqUid = Guid.NewGuid().ToString();
            
            // Create Basic auth header
            var authHeader = _options.AuthData;
            
            // Create form data for OAuth request
            var formData = new List<KeyValuePair<string, string>>
            {
                new("scope", "GIGACHAT_API_PERS")
            };
            var content = new FormUrlEncodedContent(formData);

            // Set headers
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeader}");
            _httpClient.DefaultRequestHeaders.Add("RqUID", rqUid);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await _httpClient.PostAsync(_options.OAuthUrl, content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var oauthResponse = JsonSerializer.Deserialize<OAuthResponse>(responseContent);
                if (oauthResponse != null)
                {
                    // Convert OAuth response to our internal format
                    var authResponse = new AuthorizationResponse
                    {
                        AccessToken = oauthResponse.AccessToken,
                        ExpiresAt = oauthResponse.ExpiresAt
                    };
                    
                    _authorization = new AuthorizationResult
                    {
                        AuthorizationSuccess = true,
                        GigaChatAuthorizationResponse = authResponse
                    };
                }
                else
                {
                    _authorization = new AuthorizationResult
                    {
                        AuthorizationSuccess = false,
                        ErrorTextIfFailed = "Failed to deserialize OAuth response"
                    };
                }
            }
            else
            {
                _authorization = new AuthorizationResult
                {
                    AuthorizationSuccess = false,
                    ErrorTextIfFailed = $"OAuth failed: {response.StatusCode} - {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            _authorization = new AuthorizationResult
            {
                AuthorizationSuccess = false,
                ErrorTextIfFailed = $"OAuth error: {ex.Message}"
            };
        }
    }

    private async Task<CompletionResult> SendCompletionRequestAsync(string prompt)
    {
        try
        {
            var completionRequest = new
            {
                model = _options.Model,
                messages = new[]
                {
                    new { role = "user", content = prompt }
                },
                max_tokens = _options.MaxTokens,
                temperature = _options.Temperature,
                top_p = _options.TopP
            };

            var json = JsonSerializer.Serialize(completionRequest);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Clear previous headers and set new ones for completion request
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = 
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authorization!.GigaChatAuthorizationResponse!.AccessToken);
            _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await _httpClient.PostAsync($"{_options.BaseUrl}/chat/completions", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                var completionResponse = JsonSerializer.Deserialize<CompletionResponse>(responseContent);
                return new CompletionResult
                {
                    RequestSuccessed = true,
                    GigaChatCompletionResponse = completionResponse
                };
            }
            else
            {
                return new CompletionResult
                {
                    RequestSuccessed = false,
                    ErrorTextIfFailed = $"Completion request failed: {response.StatusCode} - {responseContent}"
                };
            }
        }
        catch (Exception ex)
        {
            return new CompletionResult
            {
                RequestSuccessed = false,
                ErrorTextIfFailed = $"Completion request error: {ex.Message}"
            };
        }
    }

    private async IAsyncEnumerable<string> SendStreamingCompletionRequestAsync(string prompt, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting streaming request to SberGPT");
        
        var completionRequest = new
        {
            model = _options.Model,
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            max_tokens = _options.MaxTokens,
            temperature = _options.Temperature,
            top_p = _options.TopP,
            stream = true // Enable streaming as per SberGPT documentation
        };

        var json = JsonSerializer.Serialize(completionRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Clear previous headers and set new ones for completion request
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Authorization = 
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authorization!.GigaChatAuthorizationResponse!.AccessToken);
        _httpClient.DefaultRequestHeaders.Add("Accept", "text/event-stream");

        using var response = await _httpClient.PostAsync($"{_options.BaseUrl}/chat/completions", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            yield return $"Streaming request failed: {response.StatusCode} - {errorContent}";
            yield break;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);

        while (await reader.ReadLineAsync(cancellationToken) is { } line && !cancellationToken.IsCancellationRequested)
        {
            // Skip empty lines and comments
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith(":"))
                continue;

            // Handle data lines according to SSE format
            if (line.StartsWith("data: "))
            {
                var data = line.Substring(6).Trim(); // Remove "data: " prefix and trim
                
                // Check for end of stream marker
                if (data == "[DONE]")
                {
                    _logger.LogDebug("Received [DONE] marker, ending stream");
                    break;
                }

                // Parse and yield content
                var contentValue = ParseStreamingData(data);
                if (!string.IsNullOrEmpty(contentValue))
                {
                    yield return contentValue;
                }
            }
            else if (line.StartsWith("data:"))
            {
                // Handle data without space after colon
                var data = line.Substring(5).Trim();
                
                if (data == "[DONE]")
                {
                    _logger.LogDebug("Received [DONE] marker, ending stream");
                    break;
                }

                var contentValue = ParseStreamingData(data);
                if (!string.IsNullOrEmpty(contentValue))
                {
                    yield return contentValue;
                }
            }
        }
        
        _logger.LogInformation("Streaming request completed successfully");
    }

    private string? ParseStreamingData(string data)
    {
        try
        {
            // Parse the JSON data to extract content according to SberGPT format
            using var doc = JsonDocument.Parse(data);
            
            // Check for choices array
            if (doc.RootElement.TryGetProperty("choices", out var choices) && 
                choices.GetArrayLength() > 0)
            {
                var firstChoice = choices[0];
                
                // SberGPT format: choices[0].delta.content
                if (firstChoice.TryGetProperty("delta", out var delta) &&
                    delta.TryGetProperty("content", out var contentElement))
                {
                    var content = contentElement.GetString();
                    if (!string.IsNullOrEmpty(content))
                    {
                        _logger.LogDebug("Received streaming chunk: {Content}", content);
                        return content;
                    }
                }
            }
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to parse streaming data: {Data}", data);
        }
        
        return null;
    }
}
