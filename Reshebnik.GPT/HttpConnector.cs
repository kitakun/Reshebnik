using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using ZLogger;
using Reshebnik.GPT.Logging;
using Reshebnik.GPT.Models;
using System.Net;

namespace Reshebnik.GPT;

public sealed class HttpConnector : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ILogger<HttpConnector> _logger;
    private bool _disposed;

    public HttpConnector(ProxyConfiguration? proxyConfig = null, ILogger<HttpConnector>? logger = null)
    {
        _logger = logger ?? NullLogger<HttpConnector>.Instance;
        
        HttpMessageHandler handler;
        
        // Configure proxy if provided
        if (proxyConfig != null && !string.IsNullOrEmpty(proxyConfig.ProxyUrl))
        {
            handler = CreateProxyHandler(proxyConfig);
            _logger.ZLogInformation($"HttpConnector configured with {proxyConfig.ProxyType} proxy: {proxyConfig.ProxyUrl}");
        }
        else
        {
            handler = new HttpClientHandler();
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

    private HttpMessageHandler CreateProxyHandler(ProxyConfiguration proxyConfig)
    {
        try
        {
            switch (proxyConfig.ProxyType)
            {
                case ProxyType.Http:
                case ProxyType.Https:
                    return CreateHttpProxyHandler(proxyConfig);
                case ProxyType.Socks4:
                case ProxyType.Socks5:
                    return CreateSocksProxyHandler(proxyConfig);
                default:
                    throw new ArgumentException($"Unsupported proxy type: {proxyConfig.ProxyType}");
            }
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Failed to configure proxy: {proxyConfig.ProxyUrl}");
            throw new InvalidOperationException($"Failed to configure proxy: {proxyConfig.ProxyUrl}", ex);
        }
    }

    private HttpMessageHandler CreateHttpProxyHandler(ProxyConfiguration proxyConfig)
    {
        if (proxyConfig is { UseAllowList: true, ProxyAllowList.Length: > 0 })
        {
            // Use allowlist approach - create custom handler
            return CreateAllowListProxyHandler(proxyConfig);
        }

        // Use traditional bypass list approach
        return CreateTraditionalProxyHandler(proxyConfig);
    }

    private HttpMessageHandler CreateTraditionalProxyHandler(ProxyConfiguration proxyConfig)
    {
        var handler = new HttpClientHandler();
        var proxy = new WebProxy(proxyConfig.ProxyUrl);
        
        if (!string.IsNullOrEmpty(proxyConfig.Username) && !string.IsNullOrEmpty(proxyConfig.Password))
        {
            proxy.Credentials = new NetworkCredential(proxyConfig.Username, proxyConfig.Password);
        }
        
        proxy.BypassProxyOnLocal = proxyConfig.BypassProxyOnLocal;
        
        handler.Proxy = proxy;
        handler.UseProxy = true;
        
        return handler;
    }

    private HttpMessageHandler CreateAllowListProxyHandler(ProxyConfiguration proxyConfig)
    {
        var handler = new AllowListProxyHandler(proxyConfig.ProxyAllowList!, proxyConfig.ProxyUrl!, _logger);
        
        if (!string.IsNullOrEmpty(proxyConfig.Username) && !string.IsNullOrEmpty(proxyConfig.Password))
        {
            handler.SetCredentials(proxyConfig.Username, proxyConfig.Password);
        }
        
        _logger.LogInformation("Using proxy allowlist approach. Proxy will be used only for: {AllowList}", string.Join(", ", proxyConfig.ProxyAllowList ?? Array.Empty<string>()));
        
        return handler;
    }

    private HttpMessageHandler CreateSocksProxyHandler(ProxyConfiguration proxyConfig)
    {
        if (proxyConfig.ProxyType == ProxyType.Socks5)
        {
            var proxyUri = new Uri(proxyConfig.ProxyUrl!);

            _logger.ZLogWarning($@"""SOCKS5 proxy requested. .NET's built-in WebProxy doesn't support SOCKS5 directly. 
                               For full SOCKS5 support, consider using a third-party library or configure your WireGuard 
                               to provide an HTTP proxy instead. Falling back to HTTP proxy configuration.""");
            
            // Fallback: Try to use as HTTP proxy if the URL format allows
            var httpProxyUrl = $"http://{proxyUri.Host}:{proxyUri.Port}";
            var handler = new HttpClientHandler();
            var proxy = new WebProxy(httpProxyUrl);
            
            if (!string.IsNullOrEmpty(proxyConfig.Username) && !string.IsNullOrEmpty(proxyConfig.Password))
            {
                proxy.Credentials = new NetworkCredential(proxyConfig.Username, proxyConfig.Password);
            }
            
            handler.Proxy = proxy;
            handler.UseProxy = true;
            
            _logger.ZLogInformation($"SOCKS5 proxy fallback configured as HTTP proxy: {httpProxyUrl}");
            return handler;
        }

        throw new NotSupportedException($"SOCKS4 proxy is not supported in this implementation");
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
                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var result = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, _jsonOptions, cancellationToken);
                _logger.SuccessfullyDeserializedResponse();
                return result;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.HttpRequestFailed((int)response.StatusCode, errorContent);
            throw new HttpRequestException($"HTTP {response.StatusCode}: {errorContent}");
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
                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var result = await JsonSerializer.DeserializeAsync<TResponse>(responseStream, _jsonOptions, cancellationToken);
                _logger.SuccessfullyDeserializedResponse();
                return result;
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.HttpRequestFailed((int)response.StatusCode, errorContent);
            throw new HttpRequestException($"HTTP {response.StatusCode}: {errorContent}");
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
