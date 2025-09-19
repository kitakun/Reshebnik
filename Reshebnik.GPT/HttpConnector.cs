using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLogger;
using Reshebnik.GPT.Logging;

namespace Reshebnik.GPT;

public sealed class HttpConnector : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<HttpConnector> _logger;
    private bool _disposed;

    public HttpConnector(string? proxyUrl = null, ILogger<HttpConnector>? logger = null)
    {
        _logger = logger ?? NullLogger<HttpConnector>.Instance;
        
        var handler = new HttpClientHandler();
        
        // Configure proxy if provided
               if (!string.IsNullOrEmpty(proxyUrl))
               {
                   handler.Proxy = new System.Net.WebProxy(proxyUrl);
                   handler.UseProxy = true;
                   _logger.ZLogInformation($"HttpConnector configured with proxy: {proxyUrl}");
               }
               else
               {
                   _logger.ZLogInformation($"HttpConnector configured without proxy");
               }

        _httpClient = new HttpClient(handler);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Reshebnik.GPT/1.0");
        
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false, // Better performance without indentation
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            PropertyNameCaseInsensitive = true
        };
        
        _logger.ZLogDebug($"HttpConnector initialized successfully");
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string url, 
        TRequest request, 
        string? authorizationHeader = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.SendingPostRequest(url);
        
        try
        {
            using var jsonStream = new MemoryStream();
            await JsonSerializer.SerializeAsync(jsonStream, request, _jsonOptions, cancellationToken);
            jsonStream.Position = 0;
            
            using var content = new StreamContent(jsonStream);
            content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

                    if (!string.IsNullOrEmpty(authorizationHeader))
                    {
                        _httpClient.DefaultRequestHeaders.Authorization = 
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authorizationHeader);
                        _logger.ZLogDebug($"Authorization header added");
                    }

            using var response = await _httpClient.PostAsync(url, content, cancellationToken);
            
            _logger.ReceivedResponse((int)response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var result = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, _jsonOptions, cancellationToken);
                _logger.SuccessfullyDeserializedResponse();
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.HttpRequestFailed((int)response.StatusCode, errorContent);
                throw new HttpRequestException($"HTTP {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex) when (!(ex is HttpRequestException))
        {
            _logger.FailedToSendRequest(url);
            throw new InvalidOperationException($"Failed to send request to {url}", ex);
        }
    }

    public async Task<TResponse?> GetAsync<TResponse>(
        string url,
        string? authorizationHeader = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.SendingGetRequest(url);
        
        try
        {
            if (!string.IsNullOrEmpty(authorizationHeader))
            {
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", authorizationHeader);
                _logger.ZLogDebug($"Authorization header added");
            }

            using var response = await _httpClient.GetAsync(url, cancellationToken);
            
            _logger.ReceivedResponse((int)response.StatusCode);
            
            if (response.IsSuccessStatusCode)
            {
                using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var result = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, _jsonOptions, cancellationToken);
                _logger.SuccessfullyDeserializedResponse();
                return result;
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.HttpRequestFailed((int)response.StatusCode, errorContent);
                throw new HttpRequestException($"HTTP {response.StatusCode}: {errorContent}");
            }
        }
        catch (Exception ex) when (!(ex is HttpRequestException))
        {
            _logger.FailedToGetRequest(url);
            throw new InvalidOperationException($"Failed to get request from {url}", ex);
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(HttpConnector));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpClient?.Dispose();
            _disposed = true;
        }
    }
}
