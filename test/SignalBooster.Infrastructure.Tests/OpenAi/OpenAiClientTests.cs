using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SignalBooster.Infrastructure.OpenAi;

namespace SignalBooster.Infrastructure.Tests.OpenAi;

public sealed class OpenAiClientTests
{
    private static OpenAiClient CreateSut(HttpMessageHandler handler, OpenAiConfiguration? opts = null)
    {
        var http = new HttpClient(handler, disposeHandler: false);
        var options = Options.Create(opts ?? new OpenAiConfiguration
        {
            ApiKey = "sk-test",
            Model = "gpt-4.1-mini",
            Endpoint = "https://api.openai.com/v1/chat/completions"
        });

        return new OpenAiClient(http, options, NullLogger<OpenAiClient>.Instance);
    }

    private static string ChatCompletionPayload(string content)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WritePropertyName("choices");
            writer.WriteStartArray();
            writer.WriteStartObject();
            writer.WritePropertyName("message");
            writer.WriteStartObject();
            writer.WriteString("content", content);
            writer.WriteEndObject(); // message
            writer.WriteEndObject(); // choice 0
            writer.WriteEndArray();
            writer.WriteEndObject();
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    [Fact]
    public async Task CompleteJsonAsync_Returns_Content_OnSuccess()
    {
        // Arrange
        var expected = """{ "patient_name": "Test" }""";
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ChatCompletionPayload(expected), Encoding.UTF8, "application/json")
        });
        var sut = CreateSut(handler);

        // Act
        var result = await sut.GetJsonAsync("system", "note");

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public async Task CompleteJsonAsync_Strips_CodeFences()
    {
        // Arrange
        var fenced = """
    ```json
    { "patient_name": "Test" }
    ```
    """;
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ChatCompletionPayload(fenced), Encoding.UTF8, "application/json")
        });
        var sut = CreateSut(handler);

        // Act
        var result = await sut.GetJsonAsync("system", "note");

        // Assert
        Assert.Equal("""{ "patient_name": "Test" }""", result);
    }

    [Fact]
    public async Task CompleteJsonAsync_Throws_On_NonSuccess_WithStatusInMessage()
    {
        // Arrange
        var handler = new StubHandler(new HttpResponseMessage((HttpStatusCode)429)
        {
            Content = new StringContent("{\"error\":\"rate limited\"}", Encoding.UTF8, "application/json")
        });
        var sut = CreateSut(handler);

        // Act
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetJsonAsync("system", "note"));

        // Assert
        Assert.Contains("429", ex.Message);
        Assert.Contains("rate limited", ex.Message);
    }

    [Fact]
    public async Task CompleteJsonAsync_Throws_When_Content_Empty()
    {
        // Arrange: choices[0].message.content = null
        var empty = ChatCompletionPayload(content: null!); // will serialize as null
        var handler = new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(empty, Encoding.UTF8, "application/json")
        });
        var sut = CreateSut(handler);

        // Act + Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.GetJsonAsync("system", "note"));
    }

    [Fact]
    public void Ctor_Throws_When_ApiKey_Missing()
    {
        var http = new HttpClient(new StubHandler(new HttpResponseMessage(HttpStatusCode.OK)));
        var badOpts = Options.Create(new OpenAiConfiguration
        {
            ApiKey = "", // missing
            Model = "gpt-4.1-mini"
        });

        Assert.Throws<ArgumentException>(() =>
            new OpenAiClient(http, badOpts, NullLogger<OpenAiClient>.Instance));
    }

    [Fact]
    public async Task CompleteJsonAsync_Cancels_When_TokenCanceled()
    {
        // Arrange: a handler that never completes unless canceled
        var handler = new DelayingHandler(delayMs: 5_000);
        var sut = CreateSut(handler);

        using var cts = new CancellationTokenSource();
        cts.CancelAfter(50);

        // Act + Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            sut.GetJsonAsync("system", "note", cts.Token));
    }

    // --- Helpers ---

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response;

        public StubHandler(HttpResponseMessage response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(_response);
    }

    private sealed class DelayingHandler : HttpMessageHandler
    {
        private readonly int _delayMs;

        public DelayingHandler(int delayMs) => _delayMs = delayMs;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(_delayMs, cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ChatCompletionPayload("""{ "ok": true }"""), Encoding.UTF8, "application/json")
            };
        }
    }
}