using Microsoft.Extensions.Logging;
using SignalBooster.Domain;
using System.Text;
using System.Text.Json;

namespace SignalBooster.Infrastructure.OrderClient;

public sealed class ExternalOrderClient : IExternalOrderClient
{
    private readonly HttpClient _http;
    private readonly IOrderRequestFormatter _formatter;
    private readonly ILogger<ExternalOrderClient> _logger;

    public ExternalOrderClient(
        HttpClient http,
        IOrderRequestFormatter formatter,
        ILogger<ExternalOrderClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> SendAsync(PhysicianNote note, Uri endpoint, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(note);
        ArgumentNullException.ThrowIfNull(endpoint);

        var payload = _formatter.Format(note);
        using var content = new StringContent(payload, Encoding.UTF8, "application/json");

        try
        {
            //Normally I wouldn't do this but I wanted a quick way to just show the reviewer the request format
            //in a readable fashion while leaving the request itself optimized
            if (_logger.IsEnabled(LogLevel.Debug))
            {
                try
                {
                    using var jdoc = JsonDocument.Parse(payload);
                    var pretty = JsonSerializer.Serialize(
                        jdoc,
                        new JsonSerializerOptions { WriteIndented = true });

                    _logger.LogDebug("POST payload:\n{Payload}", pretty);
                }
                catch
                {
                    // If it's not valid JSON, just log as-is
                    _logger.LogDebug("POST payload (raw): {Payload}", payload);
                }
            }

            _logger.LogInformation("POST {Endpoint} payloadLength={Length}", endpoint, payload.Length);

            using var resp = await _http.PostAsync(endpoint, content, ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                var body = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
                _logger.LogError("POST {Endpoint} failed: {Status} {Body}", endpoint, resp.StatusCode, body);
                return false;
            }

            _logger.LogInformation("POST {Endpoint} OK: {Status}", endpoint, resp.StatusCode);
            return true;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("POST {Endpoint} canceled.", endpoint);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "POST {Endpoint} threw exception.", endpoint);
            return false;
        }
    }
}
