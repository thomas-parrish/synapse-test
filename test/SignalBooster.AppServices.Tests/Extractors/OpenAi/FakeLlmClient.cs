using SignalBooster.AppServices.Extractors.OpenAi;

namespace SignalBooster.AppServices.Tests.Extractors.OpenAi;

public sealed class FakeLLMClient : ILlmClient
{
    private readonly Func<string, string, string> _responder;

    public FakeLLMClient(Func<string, string, string> responder)
    {
        _responder = responder ?? throw new ArgumentNullException(nameof(responder));
    }

    public Task<string> GetJsonAsync(string systemPrompt, string userContent, CancellationToken ct = default)
    {
        return Task.FromResult(_responder(systemPrompt, userContent));
    }
}