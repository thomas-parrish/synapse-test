using System.Globalization;

namespace SignalBooster.AppServices.Extractors.Parsing;

internal static class DateParser
{
    public static DateOnly? Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        var formats = new[]
        {
            "MM/dd/yyyy",
            "M/d/yyyy",
            "MM-dd-yyyy",
            "M-d-yyyy",
            "yyyy-MM-dd",
            "yyyy/M/d"
        };

        if (DateTime.TryParseExact(s.Trim(), formats, CultureInfo.InvariantCulture,
                                   DateTimeStyles.None, out var dt))
        {
            return DateOnly.FromDateTime(dt);
        }

        if (DateTime.TryParse(s, out var any))
        {
            return DateOnly.FromDateTime(any);
        }

        return null;
    }
}