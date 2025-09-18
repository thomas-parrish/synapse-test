using System.Globalization;
using System.Text.RegularExpressions;

namespace SignalBooster.AppServices.Extractors.Parsing;

/// <summary>
/// Utility for extracting oxygen flow rates (in liters per minute) from free-text physician notes.
/// </summary>
/// <remarks>
/// This parser handles multiple common notations such as <c>L/min</c>, <c>LPM</c>, and implicit <c>L</c>.
/// If no recognizable flow rate is found, it returns <c>null</c>.
/// </remarks>
internal static class FlowRateParser
{
    /// <summary>
    /// Attempts to parse an oxygen flow rate value from raw physician note text.
    /// </summary>
    /// <param name="raw">The free-text input (e.g., "Oxygen at 2 L/min while sleeping").</param>
    /// <returns>
    /// A <see cref="decimal"/> representing the flow rate in liters per minute, 
    /// or <c>null</c> if no match is found.
    /// </returns>
    /// <example>
    /// Examples that successfully parse:
    /// <list type="bullet">
    ///   <item><description><c>"2 L/min"</c> → <c>2.0</c></description></item>
    ///   <item><description><c>"2.5 L/min"</c> → <c>2.5</c></description></item>
    ///   <item><description><c>"2 L per min"</c> → <c>2.0</c></description></item>
    ///   <item><description><c>"2 L"</c> → <c>2.0</c> (assumed liters per minute)</description></item>
    ///   <item><description><c>"2.5 LPM"</c> → <c>2.5</c></description></item>
    /// </list>
    /// </example>
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
