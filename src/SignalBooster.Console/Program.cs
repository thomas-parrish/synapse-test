using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.Extensions.Http;
using SignalBooster.AppServices.Extractors;
using SignalBooster.AppServices.Extractors.OpenAi;
using SignalBooster.AppServices.Extractors.Simple;
using SignalBooster.Infrastructure.OpenAiClient;
using SignalBooster.Infrastructure.OrderClient;
using System.Text.Json;
using System.Text.RegularExpressions;

Console.WriteLine("Hello, World!");
var config = new ConfigurationBuilder()
           .AddJsonFile("appsettings.json", optional: true)
           .AddUserSecrets<Program>(optional: false)
           .AddEnvironmentVariables()
           .Build();

// --- 2. Setup DI ---
var services = new ServiceCollection();

services.AddLogging(builder => builder.AddConsole());

services.AddOptions<OpenAiConfiguration>()
        .Bind(config.GetSection("OpenAi"))
        .ValidateDataAnnotations()
        .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "OpenAI:ApiKey is required.")
        .ValidateOnStart();

// HttpClient + optional retry + timeout from options
services.AddHttpClient<ILlmClient, OpenAiClient>((sp, client) =>
{
    var opts = sp.GetRequiredService<IOptions<OpenAiConfiguration>>().Value;
    client.Timeout = TimeSpan.FromSeconds(Math.Max(1, opts.TimeoutSeconds));
    client.DefaultRequestHeaders.UserAgent.ParseAdd("SignalBooster/1.0");
})
// Optional transient policy
.AddPolicyHandler(HttpPolicyExtensions
    .HandleTransientHttpError()
    .OrResult(r => (int)r.StatusCode == 429)
    .WaitAndRetryAsync(
    [
        TimeSpan.FromMilliseconds(250),
        TimeSpan.FromMilliseconds(500),
        TimeSpan.FromSeconds(1)
    ]));

services.AddSingleton<IConfiguration>(config);
services.AddSingleton<IOrderRequestFormatter, ExternalOrderRequestFormatter>();
services.AddHttpClient<IExternalOrderClient, ExternalOrderClient>();

// Choose extractor
if (config.GetValue<bool>("Extraction:UseOpenAI"))
{
    services.AddSingleton<INoteExtractor, OpenAiNoteExtractor>();
}
else
{
    services.AddSingleton<INoteExtractor, SimpleNoteExtractor>();
}

var provider = services.BuildServiceProvider();

var log = provider.GetRequiredService<ILogger<Program>>();
var extractor = provider.GetRequiredService<INoteExtractor>();
var externalClient = provider.GetRequiredService<IExternalOrderClient>();

// --- 3. Prompt user for file path ---
Console.WriteLine("Enter full path to physician note file:");
var path = ResolvePath(Console.ReadLine());

Console.WriteLine(path);

if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
{
    Console.WriteLine("Invalid path. Exiting.");
    return;
}

// --- 4. Read file ---
var raw = await File.ReadAllTextAsync(path);

// --- 5. Extract domain object ---
var note = extractor.Extract(raw);

// --- 6. Print JSON representation of domain object ---
Console.WriteLine("Extracted domain model:");
Console.WriteLine(JsonSerializer.Serialize(note, new JsonSerializerOptions
{
    WriteIndented = true,
}));

// --- 7. Call external service ---
var endpointUrl = config["ExternalOrder:ApiUrl"];
if (string.IsNullOrWhiteSpace(endpointUrl))
{
    Console.WriteLine("Missing ExternalOrder:ApiUrl in configuration. Exiting.");
    return;
}

var endpoint = new Uri(endpointUrl);

Console.WriteLine($"Sending order to {endpoint}...");
var success = await externalClient.SendAsync(note, endpoint);

Console.WriteLine(success
    ? "✅ Request succeeded."
    : "❌ Request failed. Check logs for details.");


static string ResolvePath(string? input)
{
    if (string.IsNullOrWhiteSpace(input))
    {
        throw new ArgumentException("Path cannot be empty.", nameof(input));
    }

    // Windows drive letter (C:\...) or UNC (\\server\...)
    var windowsPattern = @"^(?:[a-zA-Z]:\\|\\\\)";
    // Unix-style absolute path (/home/..., /usr/...)
    var unixPattern = @"^/";

    if (Regex.IsMatch(input, windowsPattern) || Regex.IsMatch(input, unixPattern))
    {
        // Fully-qualified already
        return input;
    }

    // Otherwise treat as relative to the app base directory
    return Path.Combine(AppContext.BaseDirectory, input);
}