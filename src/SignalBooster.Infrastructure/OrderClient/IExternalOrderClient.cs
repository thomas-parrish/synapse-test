using SignalBooster.Domain;

namespace SignalBooster.Infrastructure.OrderClient;

public interface IExternalOrderClient
{
    Task<bool> SendAsync(PhysicianNote note, Uri endpoint, CancellationToken ct = default);
}
