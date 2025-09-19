using Reshebnik.GPT.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ZLogger;
using Reshebnik.GPT.Logging;

namespace Reshebnik.GPT.Services;

public sealed class GptService : IDisposable
{
    private readonly HttpConnector _httpConnector;
    private readonly string _apiKey;
    private readonly string _baseUrl;
    private readonly ILogger<GptService> _logger;
    private bool _disposed;

    public GptService(IConfiguration configuration, ILogger<GptService> logger)
    {
        _apiKey = configuration["Gpt:ApiKey"] ?? throw new InvalidOperationException("Gpt:ApiKey not found in configuration");
        _baseUrl = configuration["Gpt:BaseUrl"] ?? "https://api.openai.com/v1";
        var proxyUrl = configuration["Gpt:ProxyUrl"];
        _httpConnector = new HttpConnector(proxyUrl);
        _logger = logger;
        
        _logger.GptServiceInitialized(_baseUrl, proxyUrl);
    }

    public async Task<GptResponse?> HelloWorldAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.ZLogInformation($"Starting HelloWorld request");

        var request = new GptRequest(
            Model: "gpt-5-nano",
            Input: "write a haiku about ai",
            Store: true
        );

        var url = $"{_baseUrl}/responses";
        _logger.SendingPostRequest(url);

        try
        {
            var response = await _httpConnector.PostAsync<GptRequest, GptResponse>(
                url,
                request,
                _apiKey,
                cancellationToken);

            _logger.ZLogInformation($"HelloWorld request completed successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"HelloWorld request failed");
            throw;
        }
    }

    public async Task<GptResponse?> SendRequestAsync(
        GptRequest request, 
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        _logger.ZLogInformation($"Starting custom request with model {request.Model}");

        var url = $"{_baseUrl}/responses";
        _logger.SendingPostRequest(url);

        try
        {
            var response = await _httpConnector.PostAsync<GptRequest, GptResponse>(
                url,
                request,
                _apiKey,
                cancellationToken);

            _logger.ZLogInformation($"Custom request completed successfully");
            return response;
        }
        catch (Exception ex)
        {
            _logger.ZLogError(ex, $"Custom request failed");
            throw;
        }
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(GptService));
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _httpConnector?.Dispose();
            _disposed = true;
        }
    }
}
