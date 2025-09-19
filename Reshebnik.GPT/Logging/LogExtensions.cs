using Microsoft.Extensions.Logging;
using ZLogger;

namespace Reshebnik.GPT.Logging;

/// <summary>
/// High-performance logging extensions using ZLogger source generator
/// </summary>
public static partial class LogExtensions
{
    // GPT Service logging
    [ZLoggerMessage(LogLevel.Information, "GptService initialized with BaseUrl: {baseUrl}, ProxyUrl: {proxyUrl}")]
    public static partial void GptServiceInitialized(this ILogger logger, string baseUrl, string? proxyUrl);

    [ZLoggerMessage(LogLevel.Debug, "Sending POST request to {url}")]
    public static partial void SendingPostRequest(this ILogger logger, string url);

    [ZLoggerMessage(LogLevel.Debug, "Received response with status code: {statusCode}")]
    public static partial void ReceivedResponse(this ILogger logger, int statusCode);

    [ZLoggerMessage(LogLevel.Debug, "Successfully deserialized response")]
    public static partial void SuccessfullyDeserializedResponse(this ILogger logger);

    [ZLoggerMessage(LogLevel.Error, "HTTP request failed with status {statusCode}: {errorContent}")]
    public static partial void HttpRequestFailed(this ILogger logger, int statusCode, string errorContent);

    [ZLoggerMessage(LogLevel.Error, "Failed to send request to {url}")]
    public static partial void FailedToSendRequest(this ILogger logger, string url);

    [ZLoggerMessage(LogLevel.Debug, "Sending GET request to {url}")]
    public static partial void SendingGetRequest(this ILogger logger, string url);

    [ZLoggerMessage(LogLevel.Error, "Failed to get request from {url}")]
    public static partial void FailedToGetRequest(this ILogger logger, string url);

    // GPT Controller logging
    [ZLoggerMessage(LogLevel.Information, "HelloWorld endpoint called")]
    public static partial void HelloWorldEndpointCalled(this ILogger logger);

    [ZLoggerMessage(LogLevel.Warning, "GPT service returned null response")]
    public static partial void GptServiceReturnedNull(this ILogger logger);

    [ZLoggerMessage(LogLevel.Error, "GPT API key not configured")]
    public static partial void GptApiKeyNotConfigured(this ILogger logger);

    [ZLoggerMessage(LogLevel.Information, "HelloWorld request completed successfully")]
    public static partial void HelloWorldCompleted(this ILogger logger);

    [ZLoggerMessage(LogLevel.Information, "SendRequest endpoint called with model {model}")]
    public static partial void SendRequestEndpointCalled(this ILogger logger, string model);

    [ZLoggerMessage(LogLevel.Warning, "GPT service returned null response for custom request")]
    public static partial void GptServiceReturnedNullCustom(this ILogger logger);

    [ZLoggerMessage(LogLevel.Information, "SendRequest completed successfully")]
    public static partial void SendRequestCompleted(this ILogger logger);

    [ZLoggerMessage(LogLevel.Error, "Error in HelloWorld endpoint")]
    public static partial void ErrorInHelloWorld(this ILogger logger);

    [ZLoggerMessage(LogLevel.Error, "Error in SendRequest endpoint")]
    public static partial void ErrorInSendRequest(this ILogger logger);
}
