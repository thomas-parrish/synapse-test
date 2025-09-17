using SignalBooster.Domain.Prescriptions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

internal sealed class BiPapParser : IPrescriptionParser
{
    public bool Matches(string hint)
    {
        // Common phrases: "bipap", "bi-pap", "bilevel"
        return hint.Contains("bipap") || hint.Contains("bi-pap") || hint.Contains("bilevel");
    }

    public IDevicePrescription? Parse(Dictionary<string, string> fields, string fullText, string hint)
    {
        // IPAP/EPAP patterns:
        //  - "IPAP: 16 cm H2O" or "IPAP=16 cmH2O"
        //  - "EPAP: 8 cm H2O"
        var ipap = IPrescriptionParser.ParseFirstInt(fullText, @"\bIPAP\s*[:=]?\s*(\d{1,2})\s*cm\s*H2O\b");
        var epap = IPrescriptionParser.ParseFirstInt(fullText, @"\bEPAP\s*[:=]?\s*(\d{1,2})\s*cm\s*H2O\b");

        // Backup rate (optional): "backup rate: 12"
        var backup = IPrescriptionParser.ParseFirstInt(fullText, @"\bbackup\s*rate\s*[:=]?\s*(\d{1,2})\b");

        var mask = IPrescriptionParser.ParseMaskType(hint);
        var heated = hint.Contains("heated humidifier");
        var ahi = AhiParser.Parse(KeyValueParser.Get(fields, "AHI"), fullText);

        return new BiPapPrescription(ipap, epap, backup, mask, heated, ahi);
    }
}