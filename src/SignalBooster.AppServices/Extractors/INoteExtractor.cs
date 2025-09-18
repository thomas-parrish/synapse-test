using SignalBooster.Domain;

namespace SignalBooster.AppServices.Extractors;

/// <summary>
/// Defines a contract for extracting a structured <see cref="PhysicianNote"/> domain model
/// from raw physician note text.
/// </summary>
/// <remarks>
/// <para>
/// This abstraction allows different extraction strategies to be plugged in, e.g.:
/// <list type="bullet">
///   <item><see cref="Simple.SimpleNoteExtractor"/> — regex- and heuristic-based extractor.</item>
///   <item><see cref="OpenAi.OpenAiNoteExtractor"/> — LLM-based extractor.</item>
/// </list>
/// </para>
/// </remarks>
public interface INoteExtractor
{
    /// <summary>
    /// Extracts a <see cref="PhysicianNote"/> from raw input text asynchronously.
    /// </summary>
    /// <param name="text">
    /// The raw physician note. May be plain text or JSON-wrapped (e.g. <c>{ "data": "..." }</c>).
    /// </param>
    /// <returns>
    /// A task resolving to a <see cref="PhysicianNote"/>.  
    /// If the input is null or whitespace, implementations should return an empty note.
    /// </returns>
    Task<PhysicianNote> ExtractAsync(string text);
}
