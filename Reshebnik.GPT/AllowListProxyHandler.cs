using System.Net;
using Microsoft.Extensions.Logging;

namespace Reshebnik.GPT;

public class AllowListProxyHandler : HttpMessageHandler
{
    private readonly string[] _allowedUrls;
    private readonly ILogger _logger;
    private readonly HttpClient _directClient;
    private readonly HttpClient _proxyClient;
    private NetworkCredential? _credentials;
    private readonly HttpClientHandler _proxyHandler;

    public AllowListProxyHandler(string[] allowedUrls, string proxyUrl, ILogger logger)
    {
        _allowedUrls = allowedUrls;
        _logger = logger;
        
        // Create direct client (no proxy)
        var directHandler = new HttpClientHandler();
        _directClient = new HttpClient(directHandler);
        
        // Create proxy client
        _proxyHandler = new HttpClientHandler();
        var proxy = new WebProxy(proxyUrl);
        _proxyHandler.Proxy = proxy;
        _proxyHandler.UseProxy = true;
        _proxyClient = new HttpClient(_proxyHandler);
    }

    public void SetCredentials(string username, string password)
    {
        _credentials = new NetworkCredential(username, password);
        if (_proxyHandler.Proxy is WebProxy webProxy)
        {
            webProxy.Credentials = _credentials;
        }
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;
        if (uri == null)
        {
            _logger.LogWarning("Request URI is null, using direct connection");
            return await _directClient.SendAsync(request, cancellationToken);
        }

        var host = uri.Host;
        var shouldUseProxy = ShouldUseProxy(host);

        if (shouldUseProxy)
        {
            _logger.LogDebug("Using proxy for request to: {Host}", host);
            return await _proxyClient.SendAsync(request, cancellationToken);
        }

        _logger.LogDebug("Using direct connection for request to: {Host}", host);
        return await _directClient.SendAsync(request, cancellationToken);
    }

    private bool ShouldUseProxy(string host)
    {
        foreach (var allowedUrl in _allowedUrls)
        {
            if (MatchesPattern(host, allowedUrl))
            {
                return true;
            }
        }
        return false;
    }

    private bool MatchesPattern(string host, string pattern)
    {
        // Simple pattern matching - can be enhanced for more complex patterns
        if (pattern.StartsWith("*."))
        {
            var domain = pattern[2..];
            return host.EndsWith(domain, StringComparison.OrdinalIgnoreCase);
        }

        if (pattern.StartsWith("*"))
        {
            var suffix = pattern[1..];
            return host.Contains(suffix, StringComparison.OrdinalIgnoreCase);
        }

        return string.Equals(host, pattern, StringComparison.OrdinalIgnoreCase);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _directClient?.Dispose();
            _proxyClient?.Dispose();
        }
        base.Dispose(disposing);
    }
}