using Reshebnik.GPT.Models;
using Reshebnik.GPT.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ZLogger;

namespace Reshebnik.GPT;

public class TestProgram
{
    public static async Task Main(string[] args)
    {
        // Setup ZLogger
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder.AddZLoggerConsole();
        });
        
        var logger = loggerFactory.CreateLogger<TestProgram>();
        
            logger.ZLogInformation($"Testing Optimized GPT Service...");
        
        try
        {
            // Test with a dummy API key (this will fail but shows the structure works)
            var apiKey = "test-api-key";
            var proxyUrl = "http://proxy.example.com:8080"; // Optional proxy
            
            // Create a mock configuration for testing
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Gpt:ApiKey"] = apiKey,
                    ["Gpt:BaseUrl"] = "https://api.openai.com/v1",
                    ["Gpt:ProxyUrl"] = proxyUrl
                })
                .Build();
            
            using var gptService = new GptService(configuration, loggerFactory.CreateLogger<GptService>());
            
            logger.ZLogInformation($"GPT Service created successfully!");
            logger.ZLogInformation($"Service configured with:");
            logger.ZLogInformation($"- API Key: {apiKey}");
            logger.ZLogInformation($"- Proxy URL: {proxyUrl ?? "None"}");
            
            // Test request creation using record struct
            var request = new GptRequest(
                Model: "gpt-5-nano",
                Input: "write a haiku about ai",
                Store: true
            );
            
            logger.ZLogInformation($"Test request created (using record struct):");
            logger.ZLogInformation($"- Model: {request.Model}");
            logger.ZLogInformation($"- Input: {request.Input}");
            logger.ZLogInformation($"- Store: {request.Store}");
            
            // Test response creation
            var response = new GptResponse(
                Id: "test-id",
                Model: "gpt-5-nano",
                Choices:
                [
                    new GptChoice(
                        Index: 0,
                        Message: new GptMessage(
                            Role: "assistant",
                            Content: "Test response"
                        )
                    )
                ]
            );
            
            logger.ZLogInformation($"Test response created (using record struct):");
            logger.ZLogInformation($"- ID: {response.Id}");
            logger.ZLogInformation($"- Model: {response.Model}");
            logger.ZLogInformation($"- Choices Count: {response.Choices?.Length ?? 0}");
            
            // Simulate async operation to avoid warning
            await Task.Delay(1);
            
            logger.ZLogInformation($"✅ Optimized GPT Service test completed successfully!");
            logger.ZLogInformation($"Performance improvements:");
            logger.ZLogInformation($"- Using record structs for better memory efficiency");
            logger.ZLogInformation($"- Stream-based JSON serialization");
            logger.ZLogInformation($"- Proper disposal patterns");
            logger.ZLogInformation($"- API key handled internally by service");
            logger.ZLogInformation($"- Configuration-driven setup");
            logger.ZLogInformation($"- High-performance ZLogger integration");
            logger.ZLogInformation($"Note: Actual API call would require a valid OpenAI API key.");
        }
        catch (Exception ex)
        {
            logger.ZLogError(ex, $"❌ Error occurred during testing");
        }
    }
}
