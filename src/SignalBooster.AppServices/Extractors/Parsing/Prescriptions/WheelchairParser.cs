using SignalBooster.Domain.Prescriptions;
using System.Text.RegularExpressions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

/// <summary>
/// Parser for extracting wheelchair prescription details from unstructured physician notes.
/// </summary>
/// <remarks>
/// Recognizes wheelchair prescriptions by scanning for the keyword "wheelchair".
/// Extracts chair type (manual, power, transport), seat width/depth, accessories
/// (leg rests, cushion), and optional justification text.
/// </remarks>
internal sealed class WheelchairParser : IPrescriptionParser
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Determines whether the provided hint text suggests a wheelchair prescription.
    /// </summary>
    /// <param name="hint">A lowercased hint string (often derived from the note text).</param>
    /// <returns>
    /// <c>true</c> if the hint contains the keyword "wheelchair"; otherwise, <c>false</c>.
    /// </returns>
    public bool Matches(string hint)
    {
        return hint.Contains("wheelchair");
    }

    /// <summary>
    /// Parses wheelchair prescription details from the provided structured and unstructured text sources.
    /// </summary>
    /// <param name="fields">Structured key-value fields extracted from the note.</param>
    /// <param name="fullText">The full free-text content of the physician note.</param>
    /// <param name="hint">A lowercased hint string (often derived from the note text).</param>
    /// <returns>
    /// An instance of <see cref="WheelchairPrescription"/> populated with:
    /// <list type="bullet">
    ///   <item><description><b>Type</b>: Manual, power, or transport (inferred if not given explicitly).</description></item>
    ///   <item><description><b>Seat Width/Depth</b>: Numeric measurements in inches (parsed from text patterns).</description></item>
    ///   <item><description><b>Leg Rests</b>: Elevating, swing-away, fixed, or articulating (matched via regex or key/value).</description></item>
    ///   <item><description><b>Cushion</b>: Gel, foam, air, or roho (matched via regex or key/value).</description></item>
    ///   <item><description><b>Justification</b>: Optional rationale (e.g., functional need) if present in structured fields.</description></item>
    /// </list>
    /// Returns <c>null</c> if parsing fails.
    /// </returns>
    public IDevicePrescription? Parse(Dictionary<string, string> fields, string fullText, string hint)
    {
        // --- Type ---
        var type = KeyValueParser.Get(fields, "WheelchairType", "Chair Type", "Type")
                   ?? InferWheelchairType(fullText);

        // --- Measurements ---
        // Examples supported:
        //   "seat width 18\"", "seat width 18 in", "seat width: 18 inches"
        var seatWidth = PrescriptionParsing.ParseFirstInt(fullText, @"\bseat\s*width\s*[:=]?\s*(\d{1,2})\s*(?:\""|in(?:ches)?)?\b");
        var seatDepth = PrescriptionParsing.ParseFirstInt(fullText, @"\bseat\s*depth\s*[:=]?\s*(\d{1,2})\s*(?:\""|in(?:ches)?)?\b");

        // --- Accessories ---
        var legRests = FindLegRests(fullText);
        var cushion = FindCushion(fullText);

        // --- Justification (optional) ---
        var justification = KeyValueParser.Get(fields, "Justification", "Functional Need", "Reason");

        return new WheelchairPrescription(
            Type: type,
            SeatWidthIn: seatWidth,
            SeatDepthIn: seatDepth,
            LegRests: legRests,
            Cushion: cushion,
            Justification: justification
        );
    }

    /// <summary>
    /// Infers wheelchair type (manual, power, transport) from free-text descriptions.
    /// </summary>
    private static string? InferWheelchairType(string text)
    {
        // Matches:
        //  - "manual wheelchair", "power wheelchair", "transport wheelchair"
        //  - "manual wheel chair", "power chair", "transport chair"
        var m = Regex.Match(
            text,
            @"\b(manual|power|transport)\s+(?:wheel\s*chair|chair|wheelchair)\b",
            RegexOptions.IgnoreCase,
            RegexTimeout);

        if (m.Success)
        {
            return m.Groups[1].Value.ToLowerInvariant(); // "manual" | "power" | "transport"
        }

        return null;
    }

    /// <summary>
    /// Finds and normalizes leg-rest type (elevating, swing-away, fixed, articulating)
    /// from free-text or key/value pairs in the note.
    /// </summary>
    private static string? FindLegRests(string text)
    {
        // Match specific leg-rest terms only (avoid grabbing “and gel cushion”)
        // Supports: "elevating leg rests", "swing-away leg rests", "fixed leg rests", "articulating leg rests"
        if (Regex.IsMatch(text, @"\belevating\s+leg\s+rests?\b", RegexOptions.IgnoreCase, RegexTimeout))
        {
            return "elevating";
        }

        if (Regex.IsMatch(text, @"\bswing[- ]away\s+leg\s+rests?\b", RegexOptions.IgnoreCase, RegexTimeout))
        {
            return "swing-away";
        }

        if (Regex.IsMatch(text, @"\bfixed\s+leg\s+rests?\b", RegexOptions.IgnoreCase, RegexTimeout))
        {
            return "fixed";
        }

        if (Regex.IsMatch(text, @"\barticulating\s+leg\s+rests?\b", RegexOptions.IgnoreCase, RegexTimeout))
        {
            return "articulating";
        }

        // Key/value form: "Leg rests: elevating"
        var kv = Regex.Match(text, @"\bleg\s*rests?\s*[:=]\s*(elevating|swing[- ]away|fixed|articulating)\b", RegexOptions.IgnoreCase, RegexTimeout);
        if (kv.Success)
        {
            return kv.Groups[1].Value.ToLowerInvariant();
        }

        return null;
    }

    /// <summary>
    /// Finds and normalizes cushion type (gel, foam, air, roho) from free-text
    /// or key/value pairs in the note.
    /// </summary>
    private static string? FindCushion(string text)
    {
        // Match cushion when adjacent to the word "cushion" to avoid “and gel cushion” capturing into leg rests
        // Supports: "gel cushion", "foam cushion", "air cushion", "roho cushion"
        var m = Regex.Match(text, @"\b(gel|foam|air|roho)\s+cushion\b", RegexOptions.IgnoreCase, RegexTimeout);
        if (m.Success)
        {
            return m.Groups[1].Value.ToLowerInvariant();
        }

        // Key/value form: "Cushion: gel"
        var kv = Regex.Match(text, @"\bcushion\s*[:=]\s*(gel|foam|air|roho)\b", RegexOptions.IgnoreCase, RegexTimeout);
        if (kv.Success)
        {
            return kv.Groups[1].Value.ToLowerInvariant();
        }

        return null;
    }
}
