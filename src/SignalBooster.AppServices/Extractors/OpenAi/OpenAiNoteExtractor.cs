using SignalBooster.AppServices.Extractors.Parsing;
using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;
using System.Text.Json;

namespace SignalBooster.AppServices.Extractors.OpenAi;

/// <summary>
/// INoteExtractor implementation that calls an LLM (via ILlmClient) to extract a PhysicianNote.
/// - Depends only on the ILlmClient port (no infra references).
/// - Uses a strict JSON-only prompt and maps returned JSON into domain models.
/// - Throws with clear context if the LLM returns invalid JSON.
/// </summary>
public sealed class OpenAiNoteExtractor : INoteExtractor
{
    private readonly ILlmClient _llmClient;

    public OpenAiNoteExtractor(ILlmClient llmClient)
    {
        _llmClient = llmClient ?? throw new ArgumentNullException(nameof(llmClient));
    }

    public async Task<PhysicianNote> ExtractAsync(string rawNote)
    {
        if (string.IsNullOrWhiteSpace(rawNote))
        {
            return EmptyNote();
        }

        var json = await _llmClient.GetJsonAsync(SystemPrompt, rawNote);
        return ParseNote(json);
    }

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

    private static IDevicePrescription MapCpap(JsonElement root)
    {
        var maskType = root.GetEnumOrDefault("mask_type", MaskType.Unknown);
        var heatedHumidifier = root.GetBoolOrDefault("heated_humidifier");
        var ahi = root.GetIntOrNull("ahi");

        return new CpapPrescription(maskType, heatedHumidifier, ahi);
    }

    private static IDevicePrescription MapBiPap(JsonElement root)
    {
        var ipap = root.GetIntOrNull("ipap_cm_h2o");
        var epap = root.GetIntOrNull("epap_cm_h2o");
        var backupRate = root.GetIntOrNull("backup_rate");
        var maskType = root.GetEnumOrDefault("mask_type", MaskType.Unknown);
        var heatedHumidifier = root.GetBoolOrDefault("heated_humidifier");
        var ahi = root.GetIntOrNull("ahi");

        return new BiPapPrescription(ipap, epap, backupRate, maskType, heatedHumidifier, ahi);
    }

    private static IDevicePrescription MapOxygen(JsonElement root)
    {
        var liters = root.GetDecimalOrNull("liters");
        var usage = root.GetEnumOrDefault("usage", UsageContext.None);

        return new OxygenPrescription(liters, usage);
    }

    private static IDevicePrescription MapWheelchair(JsonElement root)
    {
        var type = root.GetStringOrNull("chair_type");
        var seatWidth = root.GetIntOrNull("seat_width_in");
        var seatDepth = root.GetIntOrNull("seat_depth_in");
        var legRests = root.GetStringOrNull("leg_rests");
        var cushion = root.GetStringOrNull("cushion");
        var justification = root.GetStringOrNull("justification");

        return new WheelchairPrescription(type, seatWidth, seatDepth, legRests, cushion, justification);
    }

    private static PhysicianNote EmptyNote() => new()
    {
        PatientName = null,
        PatientDateOfBirth = null,
        Diagnosis = null,
        OrderingPhysician = null,
        Prescription = null
    };
    // -------------------------
    // Prompt & helpers
    // -------------------------

    private const string SystemPrompt = """
        You are a medical extraction assistant. Return ONLY a single JSON object (no prose/markdown).
        Extract these fields (use null if unknown). Dates must be MM/dd/yyyy. Keep values concise.

        {
          "patient_name": string|null,
          "dob": string|null,                    // MM/dd/yyyy
          "diagnosis": string|null,
          "ordering_physician": string|null,
          "prescription": {
            "device": "CPAP"|"BiPAP"|"Oxygen Tank"|"Wheelchair"|null,

            // CPAP/BiPAP
            "mask_type": "full face"|"nasal"|"nasal pillow"|null,
            "heated_humidifier": boolean|null,
            "ahi": number|null,

            // BiPAP specifics
            "ipap_cm_h2o": number|null,
            "epap_cm_h2o": number|null,
            "backup_rate": number|null,

            // Oxygen specifics
            "liters": number|null,               // liters per minute as a number (e.g., 2, 2.5)
            "usage": "sleep"|"exertion"|"sleep and exertion"|null,

            // Wheelchair specifics
            "chair_type": "manual"|"power"|"transport"|null,
            "seat_width_in": number|null,
            "seat_depth_in": number|null,
            "leg_rests": "elevating"|"swing-away"|"fixed"|"articulating"|null,
            "cushion": "gel"|"foam"|"air"|"roho"|null,
            "justification": string|null,
          }
        }
        """;

    private static string StripCodeFences(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return s;
        }

        s = s.Trim();

        // Strip ```json ... ``` fences if present
        if (s.StartsWith("```"))
        {
            var firstNewline = s.IndexOf('\n');
            var lastFence = s.LastIndexOf("```", StringComparison.Ordinal);
            if (firstNewline >= 0 && lastFence > firstNewline)
            {
                s = s.Substring(firstNewline, lastFence - firstNewline).Trim();
            }
        }

        return s;
    }
}
