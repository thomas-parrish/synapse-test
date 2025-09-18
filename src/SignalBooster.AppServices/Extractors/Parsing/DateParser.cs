using System.Globalization;

namespace SignalBooster.AppServices.Extractors.Parsing;

/// <summary>
/// Utility for parsing dates from free-text physician notes into <see cref="DateOnly"/> values.
/// </summary>
/// <remarks>
/// Attempts to handle a variety of common formats that may appear in medical notes.
/// If parsing fails, <c>null</c> is returned rather than throwing.
/// </remarks>
internal static class DateParser
{
    /// <summary>
    /// Attempts to parse a string into a <see cref="DateOnly"/> value.
    /// </summary>
    /// <param name="s">
    /// The input string containing a date, which may be in one of several common formats:
    /// <list type="bullet">
    ///   <item><description><c>MM/dd/yyyy</c> (e.g., 09/23/1984)</description></item>
    ///   <item><description><c>M/d/yyyy</c> (e.g., 9/3/1984)</description></item>
    ///   <item><description><c>MM-dd-yyyy</c> (e.g., 09-23-1984)</description></item>
    ///   <item><description><c>M-d-yyyy</c> (e.g., 9-3-1984)</description></item>
    ///   <item><description><c>yyyy-MM-dd</c> (ISO style, e.g., 1984-09-23)</description></item>
    ///   <item><description><c>yyyy/M/d</c> (e.g., 1984/9/23)</description></item>
    /// </list>
    /// </param>
    /// <returns>
    /// A <see cref="DateOnly"/> representing the parsed date, or <c>null</c> if parsing fails.
    /// </returns>
    /// <remarks>
    /// If the input does not match one of the explicit formats, a fallback to <see cref="DateTime.TryParse(string?, out DateTime)"/>
    /// is used to handle more loosely formatted dates.
    /// </remarks>
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
