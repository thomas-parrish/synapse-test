using SignalBooster.Domain;

namespace SignalBooster.AppServices.Extractors;

public interface INoteExtractor
{
    public Task<PhysicianNote> ExtractAsync(string text);
}
