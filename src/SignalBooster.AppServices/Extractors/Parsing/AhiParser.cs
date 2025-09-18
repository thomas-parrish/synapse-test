using System.Text.RegularExpressions;

namespace SignalBooster.AppServices.Extractors.Parsing;

internal static class AhiParser
{
    private static readonly TimeSpan RegexTimeout = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Extracts AHI as an integer, if present.
    /// Priority:
    ///   1) The explicit AHI field value (if provided) — first integer found.
    ///   2) Free-text scan in the raw note for patterns like:
    ///        "AHI: 28", "AHI > 20" (case-insensitive).
    /// Returns null if no integer AHI value can be found.
    /// </summary>
    public static int? Parse(string? ahiField, string raw)
    {
        if (!string.IsNullOrWhiteSpace(ahiField))
        {
            var d = Regex.Match(ahiField, @"\d+", RegexOptions.None, RegexTimeout);
            if (d.Success && int.TryParse(d.Value, out var n))
            {
                return n;
            }
        }

        var m = Regex.Match(raw, @"\bAHI\s*[:>]\s*(\d+)\b", RegexOptions.IgnoreCase, RegexTimeout);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n2))
        {
            return n2;
        }

        return null;
    }
}