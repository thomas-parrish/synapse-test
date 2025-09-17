using SignalBooster.Domain.Prescriptions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

internal interface IPrescriptionParser
{
    bool Matches(string hint);
    IDevicePrescription? Parse(Dictionary<string, string> fields, string fullText, string hint);
}