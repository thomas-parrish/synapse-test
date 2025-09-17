using SignalBooster.Domain;

namespace SignalBooster.AppServices.Extractors;

public interface INoteExtractor
{
    public PhysicianNote Extract(string text);
}
