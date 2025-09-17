using SignalBooster.AppServices.Extractors.Parsing;
using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;
using System.Text.Json;

namespace SignalBooster.AppServices.Extractors.OpenAi;

/// <summary>
/// INoteExtractor implementation that calls an LLM (via ILLMClient) to extract a PhysicianNote.
/// - Depends only on the ILLMClient port (no infra references).
/// - Uses a strict JSON-only prompt and maps returned JSON into domain models.
/// - Throws with clear context if the LLM returns invalid JSON.
/// </summary>
public sealed class OpenAiNoteExtractor : INoteExtractor
{
    private readonly ILlmClient _llm;

    public OpenAiNoteExtractor(ILlmClient llm)
    {
        _llm = llm ?? throw new ArgumentNullException(nameof(llm));
    }

    /// <summary>
    /// Synchronous INoteExtractor entrypoint. Prefer ExtractAsync for non-blocking calls.
    /// </summary>
    public PhysicianNote Extract(string rawNote)
    {
        return ExtractAsync(rawNote).GetAwaiter().GetResult();
    }

    public async Task<PhysicianNote> ExtractAsync(string rawNote, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rawNote))
        {
            return new PhysicianNote();
        }

        // Ask the LLM to return a single JSON object only (no prose/markdown).
        var json = await _llm.CompleteJsonAsync(SystemPrompt, rawNote, ct).ConfigureAwait(false);

        // Some models occasionally wrap content in code fences; callers of ILLMClient may already strip them,
        // but we defensively trim here as well.
        json = StripCodeFences(json);

        try
        {
            return MapToDomain(json);
        }
        catch (Exception ex)
        {
            // Surface parsing failures with the raw JSON for observability.
            throw new InvalidOperationException($"Failed to parse LLM JSON into PhysicianNote. JSON: {json}", ex);
        }
    }

    // -------------------------
    // Mapping
    // -------------------------

    private static PhysicianNote MapToDomain(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        var note = new PhysicianNote
        {
            PatientName = root.Prop("patient_name")?.GetString(),
            PatientDateOfBirth = DateParser.Parse(root.Prop("dob")?.GetString()),
            Diagnosis = root.Prop("diagnosis")?.GetString(),
            OrderingPhysician = root.Prop("ordering_physician")?.GetString(),
            Prescription = MapPrescription(root.Prop("prescription"))
        };

        return note;
    }

    private static IDevicePrescription? MapPrescription(JsonElement? presEl)
    {
        if (presEl is null || presEl.Value.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var p = presEl.Value;
        var device = p.Prop("device")?.GetString();

        if (string.Equals(device, "Oxygen Tank", StringComparison.OrdinalIgnoreCase))
        {
            // Oxygen Tank
            var lpm = p.Prop("liters")?.GetDecimalOrNull();
            var usage = p.Prop("usage")?.GetString();
            return new OxygenPrescription(
                lpm,
                usage switch
                {
                    "sleep and exertion" => UsageContext.Sleep | UsageContext.Exertion,
                    "sleep" => UsageContext.Sleep,
                    "exertion" => UsageContext.Exertion,
                    _ => UsageContext.None
                });
        }

        if (string.Equals(device, "CPAP", StringComparison.OrdinalIgnoreCase))
        {
            // CPAP
            var maskStr = p.Prop("mask_type")?.GetString();
            var heated = p.Prop("heated_humidifier")?.GetBooleanOrNull() ?? false;
            var ahi = p.Prop("ahi")?.GetInt32OrNull();

            return new CpapPrescription(
                maskStr switch
                {
                    "full face" => MaskType.FullFace,
                    "nasal" => MaskType.Nasal,
                    "nasal pillow" => MaskType.NasalPillow,
                    _ => MaskType.Unknown
                },
                heated,
                ahi);
        }

        if (string.Equals(device, "BiPAP", StringComparison.OrdinalIgnoreCase))
        {
            // BiPAP
            var maskStr = p.Prop("mask_type")?.GetString();
            var heated = p.Prop("heated_humidifier")?.GetBooleanOrNull() ?? false;
            var ahi = p.Prop("ahi")?.GetInt32OrNull();
            var ipap = p.Prop("ipap_cm_h2o")?.GetInt32OrNull();
            var epap = p.Prop("epap_cm_h2o")?.GetInt32OrNull();
            var backup = p.Prop("backup_rate")?.GetInt32OrNull();

            return new BiPapPrescription(
                ipap,
                epap,
                backup,
                maskStr is "full face" ? MaskType.FullFace :
                maskStr is "nasal" ? MaskType.Nasal :
                maskStr is "nasal pillow" ? MaskType.NasalPillow : MaskType.Unknown,
                heated,
                ahi);
        }

        if (string.Equals(device, "Wheelchair", StringComparison.OrdinalIgnoreCase))
        {
            // Wheelchair
            var type = p.Prop("chair_type")?.GetString();
            var seatW = p.Prop("seat_width_in")?.GetInt32OrNull();
            var seatD = p.Prop("seat_depth_in")?.GetInt32OrNull();
            var legs = p.Prop("leg_rests")?.GetString();
            var cushion = p.Prop("cushion")?.GetString();
            var justification = p.Prop("justification")?.GetString();

            return new WheelchairPrescription(type, seatW, seatD, legs, cushion, justification);
        }

        return null;
    }

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

        if (s.StartsWith("```", StringComparison.Ordinal))
        {
            var firstNl = s.IndexOf('\n');
            if (firstNl >= 0)
            {
                s = s[(firstNl + 1)..];
            }
            if (s.EndsWith("```", StringComparison.Ordinal))
            {
                s = s[..^3];
            }
            s = s.Trim();
        }

        return s;
    }
}

// Small JsonElement helpers to keep mapping tidy and null-safe.
internal static class JsonElExt
{
    public static JsonElement? Prop(this JsonElement el, string name)
    {
        return el.TryGetProperty(name, out var v) ? v : (JsonElement?)null;
    }

    public static int? GetInt32OrNull(this JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetInt32(out var n))
        {
            return n;
        }

        return null;
    }

    public static decimal? GetDecimalOrNull(this JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.Number && el.TryGetDecimal(out var n))
        {
            return n;
        }

        return null;
    }

    public static bool? GetBooleanOrNull(this JsonElement el)
    {
        if (el.ValueKind == JsonValueKind.True || el.ValueKind == JsonValueKind.False)
        {
            return el.GetBoolean();
        }

        return null;
    }
}
