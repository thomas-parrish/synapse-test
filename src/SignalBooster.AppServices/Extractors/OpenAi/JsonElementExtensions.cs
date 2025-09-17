using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SignalBooster.AppServices.Extractors.OpenAi;

/// <summary>
/// Provides extension methods for <see cref="JsonElement"/> to simplify safe extraction
/// of typed values from JSON documents returned by an LLM.
/// </summary>
internal static class JsonElementExtensions
{
    /// <summary>
    /// Gets the string value of the specified property, or <c>null</c> if it does not exist
    /// or is not a string.
    /// </summary>
    /// <param name="element">The JSON element to search.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The string value, or <c>null</c> if not found or not a string.</returns>
    public static string? GetStringOrNull(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop) &&
            prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }

        return null;
    }

    /// <summary>
    /// Gets the boolean value of the specified property, or returns a default value
    /// if the property is not present or not a boolean.
    /// </summary>
    /// <param name="element">The JSON element to search.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <param name="defaultValue">The default value to return if the property is missing or invalid.</param>
    /// <returns>The boolean value of the property, or <paramref name="defaultValue"/>.</returns>
    public static bool GetBoolOrDefault(this JsonElement element, string propertyName, bool defaultValue = false)
    {
        if (element.TryGetProperty(propertyName, out var prop) &&
            (prop.ValueKind == JsonValueKind.True || prop.ValueKind == JsonValueKind.False))
        {
            return prop.GetBoolean();
        }

        return defaultValue;
    }

    /// <summary>
    /// Gets the integer value of the specified property, or <c>null</c> if it does not exist
    /// or cannot be parsed.
    /// </summary>
    /// <param name="element">The JSON element to search.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The integer value, or <c>null</c> if not found or not a valid number.</returns>
    public static int? GetIntOrNull(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetInt32(out var n))
            {
                return n;
            }

            if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out n))
            {
                return n;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the decimal value of the specified property, or <c>null</c> if it does not exist
    /// or cannot be parsed.
    /// </summary>
    /// <param name="element">The JSON element to search.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>The decimal value, or <c>null</c> if not found or not a valid number.</returns>
    public static decimal? GetDecimalOrNull(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.Number && prop.TryGetDecimal(out var d))
            {
                return d;
            }

            if (prop.ValueKind == JsonValueKind.String &&
                decimal.TryParse(prop.GetString(), NumberStyles.Any, CultureInfo.InvariantCulture, out d))
            {
                return d;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the <see cref="DateOnly"/> value of the specified property, or <c>null</c> if it does not exist
    /// or cannot be parsed.
    /// </summary>
    /// <param name="element">The JSON element to search.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <returns>
    /// The parsed <see cref="DateOnly"/> value, or <c>null</c> if not found or not a valid date.
    /// </returns>
    /// <remarks>
    /// Dates are parsed using <see cref="CultureInfo.InvariantCulture"/>.
    /// </remarks>
    public static DateOnly? GetDateOnlyOrNull(this JsonElement element, string propertyName)
    {
        if (element.TryGetProperty(propertyName, out var prop))
        {
            if (prop.ValueKind == JsonValueKind.String &&
                DateOnly.TryParse(prop.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.None, out var dob))
            {
                return dob;
            }
        }

        return null;
    }

    /// <summary>
    /// Gets the enum value of type <typeparamref name="TEnum"/> for the specified property,
    /// or returns a default value if not found or invalid.
    /// </summary>
    /// <typeparam name="TEnum">The enum type to parse into.</typeparam>
    /// <param name="element">The JSON element to search.</param>
    /// <param name="propertyName">The name of the property to retrieve.</param>
    /// <param name="defaultValue">The default value to return if parsing fails.</param>
    /// <returns>
    /// The parsed enum value if successful; otherwise <paramref name="defaultValue"/>.
    /// </returns>
    /// <remarks>
    /// Normalizes strings by removing spaces, underscores, and hyphens.  
    /// For enums decorated with <see cref="FlagsAttribute"/>, supports multi-value
    /// strings like "sleep and exertion" or "sleep/exertion" by converting them
    /// into combined flags.
    /// </remarks>
    public static TEnum GetEnumOrDefault<TEnum>(
        this JsonElement element,
        string propertyName,
        TEnum defaultValue) where TEnum : struct, Enum
    {
        if (!element.TryGetProperty(propertyName, out var prop) ||
            prop.ValueKind != JsonValueKind.String)
        {
            return defaultValue;
        }

        var raw = prop.GetString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            return defaultValue;
        }

        var normalized = NormalizeEnumValue<TEnum>(raw!);

        return Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var val)
            ? val
            : defaultValue;
    }

    /// <summary>
    /// Normalizes a raw string to improve compatibility with enum parsing.
    /// - Removes spaces, underscores, and hyphens.
    /// - For [Flags] enums, replaces connectors like "and", "&amp;", "/" with commas.
    /// - Collapses multiple whitespace characters.
    /// </summary>
    /// <typeparam name="TEnum">The target enum type.</typeparam>
    /// <param name="s">The raw string value to normalize.</param>
    /// <returns>A normalized string suitable for <see cref="Enum.TryParse{TEnum}(string,bool,out TEnum)"/>.</returns>
    private static string NormalizeEnumValue<TEnum>(string s) where TEnum : struct, Enum
    {
        // Trim
        s = s.Trim();

        var isFlags = typeof(TEnum).IsDefined(typeof(FlagsAttribute), inherit: false);

        if (isFlags)
        {
            // For flags: normalize connectors to comma and strip extra spaces
            // "sleep and exertion" | "sleep/exertion" | "sleep & exertion" -> "sleep,exertion"
            s = Regex.Replace(s, @"\s*(and|&|/)\s*", ",", RegexOptions.IgnoreCase);
            // Collapse whitespace around commas
            s = Regex.Replace(s, @"\s*,\s*", ",");
            // For each token, remove hyphens/underscores/spaces inside the token
            var parts = s.Split(',', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = Regex.Replace(parts[i], @"[\s_\-]+", "");
            }
            s = string.Join(',', parts);
        }
        else
        {
            // Simple enums: remove spaces, underscores, hyphens
            s = Regex.Replace(s, @"[\s_\-]+", "");
        }

        return s;
    }
}
