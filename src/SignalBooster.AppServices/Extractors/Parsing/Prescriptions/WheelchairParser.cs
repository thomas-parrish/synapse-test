using SignalBooster.Domain.Prescriptions;
using System.Text.RegularExpressions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

internal sealed class WheelchairParser : IPrescriptionParser
{
    public bool Matches(string hint)
    {
        return hint.Contains("wheelchair");
    }

    public IDevicePrescription? Parse(Dictionary<string, string> fields, string fullText, string hint)
    {
        // --- Type ---
        var type = KeyValueParser.Get(fields, "WheelchairType", "Chair Type", "Type")
                   ?? InferWheelchairType(fullText);

        // --- Measurements ---
        // Examples supported:
        //   "seat width 18\"", "seat width 18 in", "seat width: 18 inches"
        var seatWidth = IPrescriptionParser.ParseFirstInt(fullText, @"\bseat\s*width\s*[:=]?\s*(\d{1,2})\s*(?:\""|in(?:ches)?)?\b");
        var seatDepth = IPrescriptionParser.ParseFirstInt(fullText, @"\bseat\s*depth\s*[:=]?\s*(\d{1,2})\s*(?:\""|in(?:ches)?)?\b");

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

    private static string? InferWheelchairType(string text)
    {
        // Matches:
        //  - "manual wheelchair", "power wheelchair", "transport wheelchair"
        //  - "manual wheel chair", "power chair", "transport chair"
        var m = Regex.Match(
            text,
            @"\b(manual|power|transport)\s+(?:wheel\s*chair|chair|wheelchair)\b",
            RegexOptions.IgnoreCase);

        if (m.Success)
        {
            return m.Groups[1].Value.ToLowerInvariant(); // "manual" | "power" | "transport"
        }

        return null;
    }


    private static string? FindLegRests(string text)
    {
        // Match specific leg-rest terms only (avoid grabbing “and gel cushion”)
        // Supports: "elevating leg rests", "swing-away leg rests", "fixed leg rests", "articulating leg rests"
        if (Regex.IsMatch(text, @"\belevating\s+leg\s+rests?\b", RegexOptions.IgnoreCase))
        {
            return "elevating";
        }

        if (Regex.IsMatch(text, @"\bswing[- ]away\s+leg\s+rests?\b", RegexOptions.IgnoreCase))
        {
            return "swing-away";
        }

        if (Regex.IsMatch(text, @"\bfixed\s+leg\s+rests?\b", RegexOptions.IgnoreCase))
        {
            return "fixed";
        }

        if (Regex.IsMatch(text, @"\barticulating\s+leg\s+rests?\b", RegexOptions.IgnoreCase))
        {
            return "articulating";
        }

        // Key/value form: "Leg rests: elevating"
        var kv = Regex.Match(text, @"\bleg\s*rests?\s*[:=]\s*(elevating|swing[- ]away|fixed|articulating)\b", RegexOptions.IgnoreCase);
        if (kv.Success)
        {
            return kv.Groups[1].Value.ToLowerInvariant();
        }

        return null;
    }

    private static string? FindCushion(string text)
    {
        // Match cushion when adjacent to the word "cushion" to avoid “and gel cushion” capturing into leg rests
        // Supports: "gel cushion", "foam cushion", "air cushion", "roho cushion"
        var m = Regex.Match(text, @"\b(gel|foam|air|roho)\s+cushion\b", RegexOptions.IgnoreCase);
        if (m.Success)
        {
            return m.Groups[1].Value.ToLowerInvariant();
        }

        // Key/value form: "Cushion: gel"
        var kv = Regex.Match(text, @"\bcushion\s*[:=]\s*(gel|foam|air|roho)\b", RegexOptions.IgnoreCase);
        if (kv.Success)
        {
            return kv.Groups[1].Value.ToLowerInvariant();
        }

        return null;
    }
}