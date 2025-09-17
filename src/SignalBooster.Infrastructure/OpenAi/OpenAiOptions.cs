using System.ComponentModel.DataAnnotations;

namespace SignalBooster.Infrastructure.OpenAi;

public sealed class OpenAiConfiguration
{
    [Required] public string ApiKey { get; init; } = default!;
    public string Model { get; init; } = "gpt-4o-mini";
    public string Endpoint { get; init; } = "https://api.openai.com/v1/chat/completions";
    public int TimeoutSeconds { get; init; } = 30;
}

