namespace SignalBooster.AppServices.Extractors.OpenAi;

/// <summary>
/// Abstraction for a Large Language Model (LLM) client capable of generating structured JSON
/// from a system prompt and user-provided input.
/// </summary>
public interface ILlmClient
{
    /// <summary>
    /// Sends a request to the LLM with the given system prompt and user content,
    /// and returns the raw JSON response as a string.
    /// </summary>
    /// <param name="systemPrompt">
    /// The system-level instructions (e.g., extraction schema or formatting rules) 
    /// that guide the LLM’s behavior.
    /// </param>
    /// <param name="userContent">
    /// The user-provided text to be processed (e.g., the raw physician note).
    /// </param>
    /// <param name="ct">
    /// A cancellation token that can be used to cancel the request.
    /// Defaults to <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A task that resolves to the raw JSON string returned by the LLM.
    /// </returns>
    /// <exception cref="OperationCanceledException">Thrown if the request is canceled via <paramref name="ct"/>.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the LLM response is malformed or unusable.</exception>
    Task<string> GetJsonAsync(string systemPrompt, string userContent, CancellationToken ct = default);
}
