namespace Reshebnik.GPT.Models;

public readonly record struct GptRequest(
    string Model,
    string Input,
    bool Store = false
);

public readonly record struct GptResponse(
    string? Id = null,
    string? Object = null,
    long? Created = null,
    string? Model = null,
    GptChoice[]? Choices = null,
    GptUsage? Usage = null
);

public readonly record struct GptChoice(
    int? Index = null,
    GptMessage? Message = null,
    string? FinishReason = null
);

public readonly record struct GptMessage(
    string? Role = null,
    string? Content = null
);

public readonly record struct GptUsage(
    int? PromptTokens = null,
    int? CompletionTokens = null,
    int? TotalTokens = null
);
