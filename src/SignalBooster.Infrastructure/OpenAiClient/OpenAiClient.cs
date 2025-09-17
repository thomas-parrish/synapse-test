using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SignalBooster.AppServices.Extractors.OpenAi;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace SignalBooster.Infrastructure.OpenAiClient;

public sealed class OpenAiClient : ILlmClient
{
    private readonly HttpClient _http;
    private readonly OpenAiConfiguration _openAiConfiguration;
    private readonly ILogger<OpenAiClient> _logger;

    private readonly string _apiKey;
    private readonly string _model;
    private readonly string _endpoint; // default OpenAI public endpoint

    public OpenAiClient(HttpClient http, IOptions<OpenAiConfiguration> options, ILogger<OpenAiClient> logger)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
        _openAiConfiguration = options.Value;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> CompleteJsonAsync(string systemPrompt, string userContent, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(systemPrompt))
        {
            throw new ArgumentException("System prompt cannot be empty.", nameof(systemPrompt));
        }

        if (string.IsNullOrWhiteSpace(userContent))
        {
            throw new ArgumentException("User content cannot be empty.", nameof(userContent));
        }

        using var req = new HttpRequestMessage(HttpMethod.Post, _openAiConfiguration.Endpoint);
        req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _openAiConfiguration.ApiKey);

        var payload = new
        {
            model = _openAiConfiguration.Model,
            temperature = 0,
            response_format = new { type = "json_object" },
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user",   content = userContent }
            }
        };

        var body = JsonSerializer.Serialize(payload);
        req.Content = new StringContent(body, Encoding.UTF8, "application/json");

        try
        {
            _logger.LogDebug("OpenAI request model={Model} bytes={Len}", _openAiConfiguration.Model, body.Length);

            using var resp = await _http.SendAsync(req, ct).ConfigureAwait(false);
            var respText = await resp.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!resp.IsSuccessStatusCode)
            {
                _logger.LogWarning("OpenAI returned {Status}: {Snippet}", (int)resp.StatusCode, Truncate(respText, 500));
                throw new InvalidOperationException($"OpenAI error {(int)resp.StatusCode}: {Truncate(respText, 2000)}");
            }

            using var doc = JsonDocument.Parse(respText);
            var content = doc.RootElement.GetProperty("choices")[0]
                                         .GetProperty("message")
                                         .GetProperty("content")
                                         .GetString();

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new InvalidOperationException("OpenAI returned empty content.");
            }

            return StripCodeFences(content);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("OpenAI request canceled.");
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI request failed.");
            throw;
        }
    }

    private static string StripCodeFences(string s)
    {
        s = s.Trim();
        if (!s.StartsWith("```", StringComparison.Ordinal))
        {
            return s;
        }

        var firstNl = s.IndexOf('\n');
        if (firstNl >= 0)
        {
            s = s[(firstNl + 1)..];
        }

        if (s.EndsWith("```", StringComparison.Ordinal))
        {
            s = s[..^3];
        }

        return s.Trim();
    }

    private static string Truncate(string s, int max) => string.IsNullOrEmpty(s) || s.Length <= max ? s : s.Substring(0, max) + "…";
}