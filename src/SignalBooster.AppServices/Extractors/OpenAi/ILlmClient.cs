namespace SignalBooster.AppServices.Extractors.OpenAi;

public interface ILlmClient
{
    Task<string> GetJsonAsync(string systemPrompt, string userContent, CancellationToken ct = default);
}
