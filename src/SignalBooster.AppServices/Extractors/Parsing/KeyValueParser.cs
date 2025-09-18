using System.Text;

namespace SignalBooster.AppServices.Extractors.Parsing;

/// <summary>
/// Utility for parsing structured key-value style physician note text into
/// normalized dictionaries and retrieving values by flexible key search.
/// </summary>
/// <remarks>
/// Keys are normalized by:
/// <list type="bullet">
///   <item>Removing all non-alphanumeric characters</item>
///   <item>Converting to lowercase</item>
/// </list>
/// This allows for resilient matching across variations like
/// <c>"AHI"</c>, <c>"A.H.I."</c>, <c>"ahi"</c>, etc.
/// </remarks>
internal static class KeyValueParser
{
    /// <summary>
    /// Attempts to retrieve a value from the dictionary given one or more candidate keys.
    /// </summary>
    /// <param name="keyValues">A dictionary of normalized keys and their values.</param>
    /// <param name="keysToSearch">One or more possible keys to search for (before normalization).</param>
    /// <returns>
    /// The trimmed value if found and non-empty; otherwise <c>null</c>.
    /// </returns>
    /// <example>
    /// <code>
    /// var fields = new Dictionary&lt;string, string&gt; { ["ahi"] = "22" };
    /// var result = KeyValueParser.Get(fields, "AHI", "ApneaHypopneaIndex");
    /// // result == "22"
    /// </code>
    /// </example>
    public static string? Get(IDictionary<string, string> keyValues, params string[] keysToSearch)
    {
        foreach (var key in keysToSearch)
        {
            var normalizedKey = NormalizeKey(key);
            if (keyValues.TryGetValue(normalizedKey, out var val) && !string.IsNullOrWhiteSpace(val))
            {
                return val.Trim();
            }
        }

        return null;
    }

    /// <summary>
    /// Parses free-text input into a normalized key-value dictionary.
    /// </summary>
    /// <param name="text">
    /// Raw input text, typically line-based, where each line has the form
    /// <c>"Key: Value"</c>.
    /// </param>
    /// <returns>
    /// A dictionary keyed by normalized keys with their corresponding values.
    /// Duplicate keys are ignored after the first occurrence.
    /// </returns>
    /// <example>
    /// <code>
    /// var text = "AHI: 22\nDiagnosis: OSA";
    /// var fields = KeyValueParser.Parse(text);
    /// // fields["ahi"] == "22"
    /// // fields["diagnosis"] == "OSA"
    /// </code>
    /// </example>
    public static Dictionary<string, string> Parse(string text)
    {
        var map = new Dictionary<string, string>(StringComparer.Ordinal);
        if (string.IsNullOrWhiteSpace(text))
        {
            return map;
        }

        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n")
                        .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        foreach (var rawLine in lines)
        {
            var line = rawLine.Trim();
            if (line.Length == 0)
            {
                continue;
            }

            var idx = line.IndexOf(':');
            if (idx <= 0)
            {
                continue;
            }

            var rawKey = line[..idx].Trim();
            var val = line[(idx + 1)..].Trim();
            if (rawKey.Length == 0)
            {
                continue;
            }

            var normalizedKey = NormalizeKey(rawKey);
            if (!map.ContainsKey(normalizedKey))
            {
                map[normalizedKey] = val;
            }
        }

        return map;
    }

    /// <summary>
    /// Normalizes a key by removing all non-alphanumeric characters
    /// and converting the result to lowercase.
    /// </summary>
    /// <param name="key">The raw key text.</param>
    /// <returns>A lowercase, alphanumeric-only version of the key.</returns>
    /// <example>
    /// <code>
    /// var normalized = KeyValueParser.NormalizeKey("A.H.I.");
    /// // normalized == "ahi"
    /// </code>
    /// </example>
    private static string NormalizeKey(string key)
    {
        var keyBuilder = new StringBuilder(key.Length);
        foreach (var character in key)
        {
            if (char.IsLetterOrDigit(character))
            {
                keyBuilder.Append(char.ToLowerInvariant(character));
            }
        }

        return keyBuilder.ToString();
    }
}
