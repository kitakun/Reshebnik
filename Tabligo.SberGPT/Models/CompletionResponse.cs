using System.Text.Json.Serialization;

namespace Tabligo.SberGPT.Models;

public class CompletionResponse
{
    [JsonPropertyName("choices")]
    public List<Choice> Choices { get; set; } = new();
}

public class Choice
{
    [JsonPropertyName("message")]
    public Message Message { get; set; } = new();
}

public class Message
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}

public class CompletionResult
{
    public bool RequestSuccessed { get; set; }
    public string ErrorTextIfFailed { get; set; } = string.Empty;
    public CompletionResponse? GigaChatCompletionResponse { get; set; }
}
