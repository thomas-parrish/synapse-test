using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;
using System.Text.RegularExpressions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

internal interface IPrescriptionParser
{
    bool Matches(string hint);
    IDevicePrescription? Parse(Dictionary<string, string> fields, string fullText, string hint);

    protected static string? MatchGroup(string raw, string pattern)
    {
        var m = Regex.Match(raw, pattern, RegexOptions.IgnoreCase);
        if (m.Success)
        {
            return m.Groups[1].Value.Trim();
        }

        return null;
    }

    protected static int? ParseFirstInt(string raw, string pattern)
    {
        var m = Regex.Match(raw, pattern, RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
        {
            return n;
        }

        return null;
    }

    protected static MaskType ParseMaskType(string hint)
    {
        if (hint.Contains("full face"))
        {
            return MaskType.FullFace;
        }

        if (hint.Contains("nasal pillow"))
        {
            return MaskType.NasalPillow;
        }

        if (hint.Contains("nasal"))
        {
            return MaskType.Nasal;
        }

        return MaskType.Unknown;
    }
}