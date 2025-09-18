using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

/// <summary>
/// Parser for extracting Oxygen prescription details from unstructured physician notes.
/// </summary>
/// <remarks>
/// Recognizes oxygen prescriptions by scanning for the keyword "oxygen" in the input text.
/// Extracts prescribed flow rate in liters per minute (LPM) and usage context
/// (e.g., during sleep, exertion, or both).
/// </remarks>
internal sealed class OxygenParser : IPrescriptionParser
{
    /// <summary>
    /// Determines whether the provided hint text suggests an oxygen prescription.
    /// </summary>
    /// <param name="hint">A lowercased hint string (often derived from the note text).</param>
    /// <returns>
    /// <c>true</c> if the hint contains the keyword "oxygen"; otherwise, <c>false</c>.
    /// </returns>
    public bool Matches(string hint)
    {
        return hint.Contains("oxygen");
    }

    /// <summary>
    /// Parses oxygen prescription details from the provided structured and unstructured text sources.
    /// </summary>
    /// <param name="fields">Structured key-value fields extracted from the note (not used here).</param>
    /// <param name="fullText">The full free-text content of the physician note.</param>
    /// <param name="hint">A lowercased hint string (often derived from the note text).</param>
    /// <returns>
    /// An instance of <see cref="OxygenPrescription"/> populated with:
    /// <list type="bullet">
    ///   <item><description><b>FlowLitersPerMinute</b>: Numeric flow rate, parsed via <see cref="FlowRateParser"/>.</description></item>
    ///   <item><description><b>Usage</b>: Flags indicating when oxygen is required (e.g., <see cref="UsageContext.Sleep"/>, <see cref="UsageContext.Exertion"/>).</description></item>
    /// </list>
    /// Returns <c>null</c> if parsing fails.
    /// </returns>
    public IDevicePrescription? Parse(Dictionary<string, string> fields, string fullText, string hint)
    {
        var lpm = FlowRateParser.Parse(fullText);

        var usage = UsageContext.None;
        if (hint.Contains("sleep"))
        {
            usage |= UsageContext.Sleep;
        }

        if (hint.Contains("exertion"))
        {
            usage |= UsageContext.Exertion;
        }

        return new OxygenPrescription(lpm, usage);
    }
}
