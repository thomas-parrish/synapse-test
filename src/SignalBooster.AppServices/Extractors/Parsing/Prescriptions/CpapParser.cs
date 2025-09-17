using SignalBooster.Domain.Prescriptions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

internal sealed class CpapParser : IPrescriptionParser
{
    public bool Matches(string hint)
    {
        return hint.Contains("cpap");
    }

    public IDevicePrescription? Parse(Dictionary<string, string> fields, string fullText, string hint)
    {
        var mask = PrescriptionParsing.ParseMaskType(hint);
        var heated = hint.Contains("heated humidifier");
        var ahi = AhiParser.Parse(KeyValueParser.Get(fields, "AHI"), fullText);

        return new CpapPrescription(mask, heated, ahi);
    }
}