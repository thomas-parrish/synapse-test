using SignalBooster.Domain.Prescriptions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

/// <summary>
/// Parser for extracting CPAP (Continuous Positive Airway Pressure) prescription details
/// from unstructured physician notes.
/// </summary>
/// <remarks>
/// Recognizes CPAP prescriptions by scanning for the keyword "cpap" in the input text.
/// Extracts mask type, heated humidifier, and Apnea-Hypopnea Index (AHI) values.
/// </remarks>
internal sealed class CpapParser : IPrescriptionParser
{
    /// <summary>
    /// Determines whether the provided hint text suggests a CPAP prescription.
    /// </summary>
    /// <param name="hint">A lowercased hint string (often derived from the note text).</param>
    /// <returns>
    /// <c>true</c> if the hint contains the keyword "cpap"; otherwise, <c>false</c>.
    /// </returns>
    public bool Matches(string hint)
    {
        return hint.Contains("cpap");
    }

    /// <summary>
    /// Parses CPAP prescription details from the provided structured and unstructured text sources.
    /// </summary>
    /// <param name="fields">Structured key-value fields extracted from the note.</param>
    /// <param name="fullText">The full free-text content of the physician note.</param>
    /// <param name="hint">A lowercased hint string (often derived from the note text).</param>
    /// <returns>
    /// An instance of <see cref="CpapPrescription"/> populated with:
    /// <list type="bullet">
    ///   <item><description><b>Mask Type</b>: Full face, nasal, nasal pillow, or unknown</description></item>
    ///   <item><description><b>Heated Humidifier</b>: Boolean flag indicating whether a humidifier is prescribed</description></item>
    ///   <item><description><b>AHI</b>: Apnea-Hypopnea Index (numeric value parsed from note)</description></item>
    /// </list>
    /// Returns <c>null</c> if parsing fails.
    /// </returns>
    public IDevicePrescription? Parse(Dictionary<string, string> fields, string fullText, string hint)
    {
        var mask = PrescriptionParsing.ParseMaskType(hint);
        var heated = hint.Contains("heated humidifier");
        var ahi = AhiParser.Parse(KeyValueParser.Get(fields, "AHI"), fullText);

        return new CpapPrescription(mask, heated, ahi);
    }
}
