using SignalBooster.AppServices.Extractors.Parsing;
using SignalBooster.AppServices.Extractors.Parsing.Prescriptions;
using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;
using System.Text.Json;

namespace SignalBooster.AppServices.Extractors.Simple;

/// <summary>
/// A simple <see cref="INoteExtractor"/> implementation that parses structured or semi-structured
/// physician notes without the use of an LLM.
/// </summary>
/// <remarks>
/// <para>
/// Extraction is based on key-value pairs (via <see cref="KeyValueParser"/>) and
/// regex-driven heuristics inside specific <see cref="IPrescriptionParser"/> implementations.
/// </para>
/// <para>
/// Supports: CPAP, BiPAP, Oxygen, and Wheelchair prescriptions.  
/// If multiple parsers could match the same hint, ordering matters: the most specific parser
/// should be listed first in <see cref="_parsers"/>.
/// </para>
/// </remarks>
public sealed partial class SimpleNoteExtractor : INoteExtractor
{
    private readonly IReadOnlyList<IPrescriptionParser> _parsers;

    /// <summary>
    /// Initializes the extractor with a default ordered list of prescription parsers.
    /// </summary>
    /// <remarks>
    /// Current order: BiPAP → Oxygen → CPAP → Wheelchair.
    /// </remarks>
    public SimpleNoteExtractor()
    {
        _parsers =
        [
            new BiPapParser(),
            new OxygenParser(),
            new CpapParser(),
            new WheelchairParser()
        ];
    }

    /// <summary>
    /// Extracts a <see cref="PhysicianNote"/> domain model from raw note text.
    /// </summary>
    /// <param name="text">The raw physician note text, which may be plain text or JSON-wrapped.</param>
    /// <returns>
    /// A task resolving to a populated <see cref="PhysicianNote"/> object.
    /// If <paramref name="text"/> is null/whitespace, returns an empty note.
    /// </returns>
    /// <remarks>
    /// <list type="bullet">
    ///   <item>Attempts to unwrap JSON with a <c>data</c> property before parsing.</item>
    ///   <item>Patient fields are parsed from key-value lines (e.g. "Patient Name: John Doe").</item>
    ///   <item>Device-specific prescriptions are delegated to an <see cref="IPrescriptionParser"/>.</item>
    /// </list>
    /// </remarks>
    public Task<PhysicianNote> ExtractAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return Task.FromResult(EmptyNote());
        }

        var unwrappedText = UnwrapDataIfJson(text);
        var fields = KeyValueParser.Parse(unwrappedText);

        // Core patient + header fields
        var name = KeyValueParser.Get(fields, "PatientName", "Patient Name", "Name");
        var dobStr = KeyValueParser.Get(fields, "dob", "DateOfBirth", "Date Of Birth");
        var dx = KeyValueParser.Get(fields, "Diagnosis", "Dx");
        var phys = KeyValueParser.Get(fields, "OrderingPhysician", "Ordering Physician", "Physician", "Doctor", "Provider");

        var dob = DateParser.Parse(dobStr);

        // Heuristic “hint” string for device & details (free text + any rx field)
        var recOrRx = KeyValueParser.Get(fields, "Recommendation", "Prescription", "Device");
        var hint = $"{recOrRx} {unwrappedText}".ToLowerInvariant();

        IDevicePrescription? prescription = null;

        // Resolve a parser by hint, then parse into a concrete prescription
        foreach (var parser in _parsers)
        {
            if (parser.Matches(hint))
            {
                prescription = parser.Parse(fields, unwrappedText, hint);
                if (prescription is not null)
                {
                    break;
                }
            }
        }

        return Task.FromResult(new PhysicianNote
        {
            PatientName = name,
            PatientDateOfBirth = dob,
            Diagnosis = dx,
            OrderingPhysician = phys,
            Prescription = prescription
        });
    }

    /// <summary>
    /// Attempts to unwrap a note if it is JSON of the form <c>{ "data": "..." }</c>.
    /// If not JSON or not in the expected shape, returns the original raw string.
    /// </summary>
    /// <param name="raw">The raw note text, possibly JSON-wrapped.</param>
    /// <returns>The inner text if unwrapped; otherwise the original raw string.</returns>
    private static string UnwrapDataIfJson(string raw)
    {
        var s = raw.Trim();
        if (s.Length > 1 && s[0] == '{')
        {
            try
            {
                using var doc = JsonDocument.Parse(s);
                if (doc.RootElement.TryGetProperty("data", out var dataProp) &&
                    dataProp.ValueKind == JsonValueKind.String)
                {
                    return dataProp.GetString() ?? string.Empty;
                }
            }
            catch
            {
                // Not JSON or unexpected shape; fall through to raw.
            }
        }

        return raw;
    }

    /// <summary>
    /// Creates a new empty <see cref="PhysicianNote"/> with all properties set to null.
    /// </summary>
    private static PhysicianNote EmptyNote()
    {
        return new PhysicianNote
        {
            PatientName = null,
            PatientDateOfBirth = null,
            Diagnosis = null,
            OrderingPhysician = null,
            Prescription = null
        };
    }
}
