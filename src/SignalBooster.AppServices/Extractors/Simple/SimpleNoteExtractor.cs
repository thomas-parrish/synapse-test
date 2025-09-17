using SignalBooster.AppServices.Extractors.Parsing;
using SignalBooster.AppServices.Extractors.Parsing.Prescriptions;
using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;
using System.Text.Json;

namespace SignalBooster.AppServices.Extractors.Simple;

public sealed partial class SimpleNoteExtractor : INoteExtractor
{
    private readonly IReadOnlyList<IPrescriptionParser> _parsers;

    public SimpleNoteExtractor()
    {
        // Order matters if two parsers could match the same hint; put the most specific first.
        _parsers =
        [
            new BiPapParser(),
            new OxygenParser(),
            new CpapParser(),
            new WheelchairParser()
        ];
    }

    public PhysicianNote Extract(string rawNote)
    {
        if (string.IsNullOrWhiteSpace(rawNote))
        {
            return EmptyNote();
        }

        var text = UnwrapDataIfJson(rawNote);
        var fields = KeyValueParser.Parse(text);

        // Core patient + header fields
        var name = KeyValueParser.Get(fields, "PatientName", "Patient Name", "Name");
        var dobStr = KeyValueParser.Get(fields, "dob", "DateOfBirth", "Date Of Birth");
        var dx = KeyValueParser.Get(fields, "Diagnosis", "Dx");
        var phys = KeyValueParser.Get(fields, "OrderingPhysician", "Ordering Physician", "Physician", "Doctor", "Provider");

        var dob = DateParser.Parse(dobStr);

        // Heuristic “hint” string for device & details (free text + any rx field)
        var recOrRx = KeyValueParser.Get(fields, "Recommendation", "Prescription", "Device");
        var hint = $"{recOrRx} {text}".ToLowerInvariant();

        IDevicePrescription? prescription = null;

        // Resolve a parser by hint, then parse into a concrete prescription
        foreach (var parser in _parsers)
        {
            if (parser.Matches(hint))
            {
                prescription = parser.Parse(fields, text, hint);
                if (prescription is not null)
                {
                    break;
                }
            }
        }

        return new PhysicianNote
        {
            PatientName = name,
            PatientDateOfBirth = dob,
            Diagnosis = dx,
            OrderingPhysician = phys,
            Prescription = prescription
        };
    }

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