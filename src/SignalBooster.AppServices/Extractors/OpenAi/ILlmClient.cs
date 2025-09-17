namespace SignalBooster.AppServices.Extractors.OpenAi;

public interface ILlmClient
{
    Task<string> CompleteJsonAsync(string systemPrompt, string userContent, CancellationToken ct = default);
}
