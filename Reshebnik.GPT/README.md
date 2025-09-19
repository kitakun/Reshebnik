# Reshebnik.GPT

A high-performance .NET library for integrating with GPT APIs, featuring HTTP connector with proxy support and optimized data structures.

## Features

- **High Performance**: Uses record structs and stream-based JSON serialization
- **HTTP Connector**: Optimized HTTP client with proxy support and proper disposal
- **GPT Service**: Easy-to-use service with internal API key management
- **Proxy Support**: Built-in proxy configuration for enterprise environments
- **Type Safety**: Strongly typed request/response models using record structs
- **Memory Efficient**: Minimal allocations with proper disposal patterns

## Usage

### Basic Usage

```csharp
using Reshebnik.GPT.Services;
using Reshebnik.GPT.Models;

// Create service with API key (API key handled internally)
using var gptService = new GptService("your-api-key-here");

// Hello World example
var response = await gptService.HelloWorldAsync();

// Custom request using record struct
var request = new GptRequest(
    Model: "gpt-4",
    Input: "Hello, how are you?",
    Store: false
);

var customResponse = await gptService.SendRequestAsync(request);
```

### With Proxy

```csharp
// Create service with proxy
using var gptService = new GptService(
    apiKey: "your-api-key-here",
    proxyUrl: "http://proxy.company.com:8080"
);
```

### In ASP.NET Core Controller

```csharp
[ApiController]
[Route("api/[controller]")]
public class GptController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public GptController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("hello-world")]
    public async Task<IActionResult> HelloWorldAsync()
    {
        var proxyUrl = _configuration["Gpt:ProxyUrl"];
        
        // API key is handled internally by the service
        using var gptService = new GptService(_configuration, proxyUrl);
        var response = await gptService.HelloWorldAsync();
        
        return Ok(response);
    }
}
```

## Configuration

Add to your `appsettings.json`:

```json
{
  "Gpt": {
    "ApiKey": "your-openai-api-key-here",
    "ProxyUrl": "http://proxy.example.com:8080"
  }
}
```

## API Endpoints

The library is designed to work with the OpenAI API format:

- **Base URL**: `https://api.openai.com/v1`
- **Endpoint**: `/responses`
- **Method**: `POST`

## Models

All models use `readonly record struct` for optimal performance and memory efficiency.

### GptRequest
```csharp
public readonly record struct GptRequest(
    string Model,
    string Input,
    bool Store = false
);
```

### GptResponse
```csharp
public readonly record struct GptResponse(
    string? Id = null,
    string? Object = null,
    long? Created = null,
    string? Model = null,
    GptChoice[]? Choices = null,
    GptUsage? Usage = null
);
```

## Performance Optimizations

- **Record Structs**: All models use `readonly record struct` for minimal memory allocation
- **Stream-based JSON**: Uses `JsonSerializer.SerializeAsync` and `JsonSerializer.DeserializeAsync` for better performance
- **Proper Disposal**: Implements `IDisposable` with proper disposal patterns
- **Sealed Classes**: Classes are sealed to enable better JIT optimizations
- **Array instead of List**: Uses arrays for better performance in response models
- **High-performance Logging**: Uses ZLogger for zero-allocation structured logging with excellent performance

## Dependencies

- .NET 9.0
- System.Text.Json
- System.Net.Http
- ZLogger (for high-performance logging)
- Microsoft.Extensions.Configuration.Abstractions

## License

Part of the Reshebnik project.
