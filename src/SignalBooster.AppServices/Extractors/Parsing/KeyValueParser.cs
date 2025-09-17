using System.Text;

namespace SignalBooster.AppServices.Extractors.Parsing;

internal static class KeyValueParser
{

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