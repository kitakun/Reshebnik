using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Reshebnik.SberGPT.Models;
using Reshebnik.SberGPT.Proto;
using Grpc.Core;
using Grpc.Net.Client;
using System.Text.Json;
using System.Security.Cryptography.X509Certificates;
using Message = Reshebnik.SberGPT.Proto.Message;

namespace Reshebnik.SberGPT.Services;

public class SberGptGrpcService(
    HttpClient httpClient,
    IOptions<SberGptOptions> options,
    ILogger<SberGptGrpcService> logger)
    : ISberGptService
{
    private readonly SberGptOptions _options = options.Value;
    private AuthorizationResult? _authorization;
    private ChatService.ChatServiceClient? _chatClient;

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

            // Initialize gRPC client if not already done
            await InitializeGrpcClientAsync();

            if (_chatClient == null)
            {
                return "Failed to initialize gRPC client";
            }

            // Create gRPC request
            var chatRequest = new ChatRequest
            {
                Model = _options.Model,
                Messages = { new Message { Role = "user", Content = request } },
                Options = new ChatOptions
                {
                    MaxTokens = _options.MaxTokens,
                    Temperature = (float)_options.Temperature,
                    TopP = (float)_options.TopP
                }
            };

            // Make gRPC call
            var response = await _chatClient.ChatAsync(chatRequest);

            // Extract content from response
            var content = response.Alternatives.FirstOrDefault()?.Message?.Content;
            return content ?? "No response received";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error in SberGptGrpcService.HandleAsync");
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

        // Initialize gRPC client if not already done
        await InitializeGrpcClientAsync();

        if (_chatClient == null)
        {
            yield return "Failed to initialize gRPC client";
            yield break;
        }

        // Create gRPC request for streaming
        var chatRequest = new ChatRequest
        {
            Model = _options.Model,
            Messages = { new Message { Role = "user", Content = request } },
            Options = new ChatOptions
            {
                MaxTokens = _options.MaxTokens,
                Temperature = (float)_options.Temperature,
                TopP = (float)_options.TopP
            }
        };

        // Make streaming gRPC call
        using var call = _chatClient.ChatStream(chatRequest);
        await foreach (var response in call.ResponseStream.ReadAllAsync(cancellationToken))
        {
            var content = response.Alternatives.FirstOrDefault()?.Message?.Content;
            if (!string.IsNullOrEmpty(content))
            {
                yield return content;
            }
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
            httpClient.DefaultRequestHeaders.Clear();
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeader}");
            httpClient.DefaultRequestHeaders.Add("RqUID", rqUid);
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");

            var response = await httpClient.PostAsync(_options.OAuthUrl, content);
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

    private Task InitializeGrpcClientAsync()
    {
        if (_chatClient != null)
            return Task.CompletedTask;

        try
        {
            // Create gRPC channel with SSL and Russian certificate
            var channelOptions = new GrpcChannelOptions
            {
                Credentials = ChannelCredentials.SecureSsl
            };

            // For local development, try to load the certificate explicitly
            var certificatePath = GetCertificatePath();
            if (!string.IsNullOrEmpty(certificatePath) && File.Exists(certificatePath))
            {
                logger.LogInformation("Using Russian certificate from: {CertificatePath}", certificatePath);
                
                // Load the certificate and configure SSL
                var certificate = new X509Certificate2(certificatePath);
                var httpHandler = new HttpClientHandler();
                httpHandler.ClientCertificates.Add(certificate);
                
                channelOptions.HttpHandler = httpHandler;
            }
            else
            {
                logger.LogInformation("Using system certificate store for SSL");
            }

            var channel = GrpcChannel.ForAddress($"https://{_options.GrpcEndpoint}:{_options.GrpcPort}", channelOptions);

            // Create client
            _chatClient = new ChatService.ChatServiceClient(channel);

            logger.LogInformation("gRPC client initialized for endpoint: {Endpoint}:{Port} with Russian certificate", 
                _options.GrpcEndpoint, _options.GrpcPort);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize gRPC client");
            throw;
        }
        
        return Task.CompletedTask;
    }

    private string GetCertificatePath()
    {
        // Try different paths for the certificate
        var possiblePaths = new[]
        {
            // Local development path
            Path.Combine(Directory.GetCurrentDirectory(), "certificates", "russian_trusted_root_ca_pem.crt"),
            // Docker path
            "/usr/local/share/ca-certificates/russian_trusted_root_ca_pem.crt",
            // Environment variable path
            Environment.GetEnvironmentVariable("GRPC_DEFAULT_SSL_ROOTS_FILE_PATH") ?? "",
            // Relative to project root
            Path.Combine("..", "certificates", "russian_trusted_root_ca_pem.crt"),
            Path.Combine("..", "..", "certificates", "russian_trusted_root_ca_pem.crt")
        };

        foreach (var path in possiblePaths)
        {
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                return path;
            }
        }

        return string.Empty;
    }
}
