using SignalBooster.AppServices.Extractors.Parsing;
using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;
using System.Text.Json;

namespace SignalBooster.AppServices.Extractors.OpenAi;

/// <summary>
/// Extracts structured physician notes by calling a Large Language Model (LLM) through <see cref="ILlmClient"/>.
/// </summary>
/// <remarks>
/// This extractor:
/// <list type="bullet">
///   <item>Depends only on the <see cref="ILlmClient"/> interface (no infrastructure references).</item>
///   <item>Uses a strict JSON-only system prompt to enforce structured output.</item>
///   <item>Maps returned JSON into domain models (<see cref="PhysicianNote"/> and prescription types).</item>
///   <item>Throws detailed exceptions when the LLM returns invalid or unparsable JSON.</item>
/// </list>
/// </remarks>
public sealed class OpenAiNoteExtractor : INoteExtractor
{
    private readonly ILlmClient _llmClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="OpenAiNoteExtractor"/> class.
    /// </summary>
    /// <param name="llmClient">An implementation of <see cref="ILlmClient"/> used to query the LLM.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="llmClient"/> is <c>null</c>.</exception>
    public OpenAiNoteExtractor(ILlmClient llmClient)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
    }

    /// <inheritdoc />
    /// <remarks>
    /// If <paramref name="rawNote"/> is blank, an empty <see cref="PhysicianNote"/> is returned.
    /// Otherwise, the note is sent to the LLM using a strict system prompt and parsed into a domain model.
    /// </remarks>
    public async Task<PhysicianNote> ExtractAsync(string rawNote)
    {
        if (string.IsNullOrWhiteSpace(rawNote))
        {
            return EmptyNote();
        }

        var json = await _llmClient.GetJsonAsync(SystemPrompt, rawNote);
        return ParseNote(json);
    }

    /// <summary>
    /// Parses JSON text into a <see cref="PhysicianNote"/> object.
    /// </summary>
    /// <param name="json">The JSON string returned by the LLM.</param>
    /// <returns>A populated <see cref="PhysicianNote"/> object.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the JSON is invalid or cannot be mapped into the expected schema.
    /// </exception>
    private static PhysicianNote ParseNote(string json)
    {
        try
        {
            var clean = StripCodeFences(json);

            using var doc = JsonDocument.Parse(clean);
            var root = doc.RootElement;

            var note = new PhysicianNote
            {
                PatientName = root.GetStringOrNull("patient_name"),
                PatientDateOfBirth = root.GetDateOnlyOrNull("dob"),
                Diagnosis = root.GetStringOrNull("diagnosis"),
                OrderingPhysician = root.GetStringOrNull("ordering_physician"),
                Prescription = MapPrescription(root.GetProperty("prescription"))
            };

            return note;
        }
        catch (Exception ex) when (ex is JsonException || ex is FormatException)
        {
            throw new InvalidOperationException(
                $"Failed to parse LLM JSON. Payload snippet: {json[..Math.Min(json.Length, 200)]}", ex);
        }
    }

    /// <summary>
    /// Maps the <c>prescription</c> element of the JSON into a concrete <see cref="IDevicePrescription"/>.
    /// </summary>
    /// <param name="root">The JSON element containing the prescription object.</param>
    /// <returns>
    /// A specific prescription object (<see cref="CpapPrescription"/>, <see cref="BiPapPrescription"/>,
    /// <see cref="OxygenPrescription"/>, or <see cref="WheelchairPrescription"/>), or <c>null</c> if not recognized.
    /// </returns>
    private static IDevicePrescription? MapPrescription(JsonElement root)
    {
        var device = root.GetStringOrNull("device")?.ToLowerInvariant();
        return device switch
        {
            "cpap" => MapCpap(root),
            "bipap" => MapBiPap(root),
            "oxygen tank" or "oxygen" => MapOxygen(root),
            "wheelchair" => MapWheelchair(root),
            _ => null
        };
    }

    /// <summary>
    /// Maps JSON fields into a <see cref="CpapPrescription"/>.
    /// </summary>
    private static CpapPrescription MapCpap(JsonElement root)
    {
        var maskType = root.GetEnumOrDefault("mask_type", MaskType.Unknown);
        var heatedHumidifier = root.GetBoolOrDefault("heated_humidifier");
        var ahi = root.GetIntOrNull("ahi");

        return new CpapPrescription(maskType, heatedHumidifier, ahi);
    }

    /// <summary>
    /// Maps JSON fields into a <see cref="BiPapPrescription"/>.
    /// </summary>
    private static BiPapPrescription MapBiPap(JsonElement root)
    {
        var ipap = root.GetIntOrNull("ipap_cm_h2o");
        var epap = root.GetIntOrNull("epap_cm_h2o");
        var backupRate = root.GetIntOrNull("backup_rate");
        var maskType = root.GetEnumOrDefault("mask_type", MaskType.Unknown);
        var heatedHumidifier = root.GetBoolOrDefault("heated_humidifier");
        var ahi = root.GetIntOrNull("ahi");

        return new BiPapPrescription(ipap, epap, backupRate, maskType, heatedHumidifier, ahi);
    }

    /// <summary>
    /// Maps JSON fields into an <see cref="OxygenPrescription"/>.
    /// </summary>
    private static OxygenPrescription MapOxygen(JsonElement root)
    {
        var liters = root.GetDecimalOrNull("liters");
        var usage = root.GetEnumOrDefault("usage", UsageContext.None);

        return new OxygenPrescription(liters, usage);
    }

    /// <summary>
    /// Maps JSON fields into a <see cref="WheelchairPrescription"/>.
    /// </summary>
    private static WheelchairPrescription MapWheelchair(JsonElement root)
    {
        var type = root.GetStringOrNull("chair_type");
        var seatWidth = root.GetIntOrNull("seat_width_in");
        var seatDepth = root.GetIntOrNull("seat_depth_in");
        var legRests = root.GetStringOrNull("leg_rests");
        var cushion = root.GetStringOrNull("cushion");
        var justification = root.GetStringOrNull("justification");

        return new WheelchairPrescription(type, seatWidth, seatDepth, legRests, cushion, justification);
    }

    /// <summary>
    /// Returns an empty <see cref="PhysicianNote"/> with all fields set to <c>null</c>.
    /// </summary>
    private static PhysicianNote EmptyNote() => new()
    {
        PatientName = null,
        PatientDateOfBirth = null,
        Diagnosis = null,
        OrderingPhysician = null,
        Prescription = null
    };

    /// <summary>
    /// System prompt used to instruct the LLM to return structured JSON only.
    /// </summary>
    private const string SystemPrompt = """ ... """;

    /// <summary>
    /// Removes Markdown code fences (```json ... ```) from the raw LLM response if present.
    /// </summary>
    /// <param name="s">The raw response string.</param>
    /// <returns>The response without code fences, or the original string if none are found.</returns>
    private static string StripCodeFences(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return s;
        }

        s = s.Trim();

        if (s.StartsWith("```"))
        {
            var firstNewline = s.IndexOf('\n');
            var lastFence = s.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
            {
                s = s[firstNewline..lastFence].Trim();
            }
        }

        return s;
    }
}
