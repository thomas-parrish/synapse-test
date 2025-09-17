using SignalBooster.Domain;
using System.Text.RegularExpressions;

namespace SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

internal static class PrescriptionParsing
{
    /// <summary>Return first capture group if pattern matches; else null.</summary>
    public static string? MatchGroup(string raw, string pattern)
    {
        var m = Regex.Match(raw, pattern, RegexOptions.IgnoreCase);
        if (m.Success)
        {
            return m.Groups[1].Value.Trim();
        }

        return null;
    }

    /// <summary>Return first int capture group if pattern matches; else null.</summary>
    public static int? ParseFirstInt(string raw, string pattern)
    {
        var m = Regex.Match(raw, pattern, RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var n))
        {
            return n;
        }

        return null;
    }

    /// <summary>Maps mask keywords to MaskType enum.</summary>
    public static MaskType ParseMaskType(string hint)
    {
        if (string.IsNullOrWhiteSpace(hint))
        {
            return MaskType.Unknown;
        }

        var h = hint.ToLowerInvariant();

        if (h.Contains("full face"))
        {
            return MaskType.FullFace;
        }

        if (h.Contains("nasal pillow"))
        {
            return MaskType.NasalPillow;
        }

        if (h.Contains("nasal"))
        {
            return MaskType.Nasal;
        }

        return MaskType.Unknown;
    }
}
