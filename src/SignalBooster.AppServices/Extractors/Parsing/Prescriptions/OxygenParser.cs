using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

internal sealed class OxygenParser : IPrescriptionParser
{
    public bool Matches(string hint)
    {
        return hint.Contains("oxygen");
    }

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