using System.Globalization;
using System.Text.RegularExpressions;

namespace SignalBooster.AppServices.Extractors.Parsing;

internal static class FlowRateParser
{

    // -------------------------
    // Device-specific parsing
    // -------------------------

    public static decimal? Parse(string raw)
    {
        // Matches formats like:
        //   "2 L/min"
        //   "2.5 L/min"
        //   "2 L per min"
        var m = Regex.Match(
            raw,
            @"\b(\d+(?:\.\d+)?)\s*L\s*(?:/|per)\s*min\b",
            RegexOptions.IgnoreCase);

        if (m.Success)
        {
            return decimal.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        // Matches formats like:
        //   "2 L"
        //   "2.5 L"
        // Note: no explicit "per minute" → we still assume flow in liters/minute
        var m2 = Regex.Match(
            raw,
            @"\b(\d+(?:\.\d+)?)\s*L\b",
            RegexOptions.IgnoreCase);

        if (m2.Success)
        {
            return decimal.Parse(m2.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        // Matches shorthand like:
        //   "2 LPM"
        //   "2.5 LPM"
        // (LPM = Liters Per Minute)
        var m3 = Regex.Match(
            raw,
            @"\b(\d+(?:\.\d+)?)\s*LPM\b",
            RegexOptions.IgnoreCase);

        if (m3.Success)
        {
            return decimal.Parse(m3.Groups[1].Value, CultureInfo.InvariantCulture);
        }

        // Nothing matched
        return null;
    }
}