using SignalBooster.Domain.Prescriptions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

/// <summary>
/// Parser for extracting BiPAP (Bilevel Positive Airway Pressure) prescription details
/// from unstructured physician notes.
/// </summary>
/// <remarks>
/// Recognizes common BiPAP terms such as "bipap", "bi-pap", or "bilevel".
/// Extracts key parameters including IPAP, EPAP, optional backup rate, mask type,
/// heated humidifier, and AHI (Apnea-Hypopnea Index).
/// </remarks>
internal sealed class BiPapParser : IPrescriptionParser
{
    /// <summary>
    /// Determines whether the provided hint text suggests a BiPAP prescription.
    /// </summary>
    /// <param name="hint">A lowercased hint string (often derived from the note text).</param>
    /// <returns>
    /// <c>true</c> if the hint contains keywords such as "bipap", "bi-pap", or "bilevel";
    /// otherwise, <c>false</c>.
    /// </returns>
    public bool Matches(string hint)
    {
        // Common phrases: "bipap", "bi-pap", "bilevel"
        return hint.Contains("bipap") || hint.Contains("bi-pap") || hint.Contains("bilevel");
    }

    /// <summary>
    /// Parses BiPAP prescription details from the provided structured and unstructured text sources.
    /// </summary>
    /// <param name="fields">Structured key-value fields extracted from the note.</param>
    /// <param name="fullText">The full free-text content of the physician note.</param>
    /// <param name="hint">A lowercased hint string (often derived from the note text).</param>
    /// <returns>
    /// An instance of <see cref="BiPapPrescription"/> populated with:
    /// <list type="bullet">
    ///   <item><description><b>IPAP</b>: Inspiratory Positive Airway Pressure (cm H₂O)</description></item>
    ///   <item><description><b>EPAP</b>: Expiratory Positive Airway Pressure (cm H₂O)</description></item>
    ///   <item><description><b>Backup Rate</b>: Optional backup breaths per minute</description></item>
    ///   <item><description><b>Mask Type</b>: Parsed via <see cref="PrescriptionParsing.ParseMaskType"/></description></item>
    ///   <item><description><b>Heated Humidifier</b>: Inferred from hint text</description></item>
    ///   <item><description><b>AHI</b>: Apnea-Hypopnea Index (from fields or free text)</description></item>
    /// </list>
    /// Returns <c>null</c> if parsing fails.
    /// </returns>
    public IDevicePrescription? Parse(Dictionary<string, string> fields, string fullText, string hint)
    {
        // IPAP/EPAP patterns:
        //  - "IPAP: 16 cm H2O" or "IPAP=16 cmH2O"
        //  - "EPAP: 8 cm H2O"
        var ipap = PrescriptionParsing.ParseFirstInt(fullText, @"\bIPAP\s*[:=]?\s*(\d{1,2})\s*cm\s*H2O\b");
        var epap = PrescriptionParsing.ParseFirstInt(fullText, @"\bEPAP\s*[:=]?\s*(\d{1,2})\s*cm\s*H2O\b");

        // Backup rate (optional): "backup rate: 12"
        var backup = PrescriptionParsing.ParseFirstInt(fullText, @"\bbackup\s*rate\s*[:=]?\s*(\d{1,2})\b");

        var mask = PrescriptionParsing.ParseMaskType(hint);
        var heated = hint.Contains("heated humidifier");
        var ahi = AhiParser.Parse(KeyValueParser.Get(fields, "AHI"), fullText);

        return new BiPapPrescription(ipap, epap, backup, mask, heated, ahi);
    }
}
