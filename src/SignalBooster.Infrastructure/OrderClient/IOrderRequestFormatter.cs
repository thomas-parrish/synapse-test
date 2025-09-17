using SignalBooster.Domain;

namespace SignalBooster.Infrastructure.OrderClient;

public interface IOrderRequestFormatter
{
    /// <summary>
    /// Formats a PhysicianNote into the legacy request JSON expected by the external API.
    /// </summary>
    string Format(PhysicianNote note);
}
