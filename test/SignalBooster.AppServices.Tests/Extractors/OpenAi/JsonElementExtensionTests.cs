using SignalBooster.AppServices.Extractors.OpenAi;
using SignalBooster.Domain;
using System.Text.Json;

namespace SignalBooster.AppServices.Tests.Extractors.OpenAi;

public class JsonElementExtensionsTests
{
    private static JsonElement Parse(string json) => JsonDocument.Parse(json).RootElement;

    // ---------- GetStringOrNull ----------
    [Fact]
    public void GetStringOrNull_ReturnsString_WhenPresent()
    {
        var root = Parse("""{ "s": "hello" }""");
        Assert.Equal("hello", root.GetStringOrNull("s"));
    }

    [Fact]
    public void GetStringOrNull_ReturnsNull_WhenMissingOrNotString()
    {
        var root = Parse("""{ "n": 1, "b": true }""");
        Assert.Null(root.GetStringOrNull("missing"));
        Assert.Null(root.GetStringOrNull("n"));
        Assert.Null(root.GetStringOrNull("b"));
    }

    // ---------- GetBoolOrDefault ----------
    [Theory]
    [InlineData("""{ "val": true }""", "val", true, false, true)]
    [InlineData("""{ "val": false }""", "val", false, true, false)]
    public void GetBoolOrDefault_Parses_WhenValid(
        string json,
        string propertyName,
        bool expected,
        bool defaultValue,
        bool expectedWithDefault)
    {
        var root = Parse(json);

        // Case without default override
        Assert.Equal(expected, root.GetBoolOrDefault(propertyName));

        // Case with explicit defaultValue provided
        Assert.Equal(expectedWithDefault, root.GetBoolOrDefault(propertyName, defaultValue));
    }

    [Theory]
    [InlineData("""{ "s": "yes", "n": 0 }""", "missing", true)]
    [InlineData("""{ "s": "yes", "n": 0 }""", "s", false)]
    [InlineData("""{ "s": "yes", "n": 0 }""", "n", true)]
    public void GetBoolOrDefault_ReturnsDefault_WhenInvalid(
        string json,
        string propertyName,
        bool expected)
    {
        var root = Parse(json);

        Assert.Equal(expected, root.GetBoolOrDefault(propertyName, defaultValue: expected));
    }

    // ---------- GetIntOrNull ----------
    [Fact]
    public void GetIntOrNull_ParsesNumberToken_WhenValid()
    {
        var root = Parse("""{ "n": 42 }""");
        Assert.Equal(42, root.GetIntOrNull("n"));
    }

    [Fact]
    public void GetIntOrNull_ParsesStringNumber_WhenValid()
    {
        var root = Parse("""{ "n": "27" }""");
        Assert.Equal(27, root.GetIntOrNull("n"));
    }

    [Theory]
    [InlineData("""{ "s": "abc", "b": false }""", "s")]
    [InlineData("""{ "s": "abc", "b": false }""", "b")]
    [InlineData("""{ "s": "abc", "b": false }""", "missing")]
    public void GetIntOrNull_ReturnsNull_WhenNotNumeric(string json, string propertyName)
    {
        var root = Parse(json);

        Assert.Null(root.GetIntOrNull(propertyName));
    }

    // ---------- GetDecimalOrNull ----------
    [Theory]
    [InlineData("""{ "a": 2.5 }""", "a", 2.5)]
    [InlineData("""{ "b": "3.75" }""", "b", 3.75)]
    public void GetDecimalOrNull_ParsesNumberAndString_WhenValid(string json, string propertyName, decimal expected)
    {
        var root = Parse(json);

        Assert.Equal(expected, root.GetDecimalOrNull(propertyName));
    }

    [Theory]
    [InlineData("""{ "s": "NaN", "o": {} }""", "s")]
    [InlineData("""{ "s": "NaN", "o": {} }""", "o")]
    [InlineData("""{ "s": "NaN", "o": {} }""", "missing")]
    public void GetDecimalOrNull_ReturnsNull_WhenInvalid(string json, string propertyName)
    {
        var root = Parse(json);

        Assert.Null(root.GetDecimalOrNull(propertyName));
    }

    // ---------- GetDateOnlyOrNull ----------
    [Fact]
    public void GetDateOnlyOrNull_Parses_MMddyyyy()
    {
        var root = Parse("""{ "dob": "09/23/1984" }""");
        Assert.Equal(new DateOnly(1984, 9, 23), root.GetDateOnlyOrNull("dob"));
    }

    [Theory]
    [InlineData("""{ "dob": "23/09/1984", "n": 123 }""", "dob")]      // wrong format (expects MM/dd/yyyy)
    [InlineData("""{ "dob": "23/09/1984", "n": 123 }""", "n")]        // not a string
    [InlineData("""{ "dob": "23/09/1984", "n": 123 }""", "missing")]  // missing property
    public void GetDateOnlyOrNull_ReturnsNull_WhenInvalid(string json, string propertyName)
    {
        var root = Parse(json);

        Assert.Null(root.GetDateOnlyOrNull(propertyName));
    }

    // ---------- GetEnumOrDefault (with normalization & flags) ----------
    [Fact]
    public void GetEnumOrDefault_Parses_SimpleEnum_With_Spaces()
    {
        var root = Parse("""{ "mask_type": "full face" }""");
        // after NormalizeEnumValue: "fullface" -> TryParse("FullFace") succeeds
        var val = root.GetEnumOrDefault("mask_type", MaskType.Unknown);
        Assert.Equal(MaskType.FullFace, val);
    }

    [Theory]
    [InlineData("""{ "mask_type": "???" }""", "mask_type")]   // unknown value
    [InlineData("""{ "mask_type": "nasal" }""", "missing")]   // missing property
    public void GetEnumOrDefault_ReturnsDefault_WhenMissingOrUnknown(string json, string propertyName)
    {
        var root = Parse(json);

        Assert.Equal(MaskType.Unknown, root.GetEnumOrDefault(propertyName, MaskType.Unknown));
    }

    [Theory]
    [InlineData("""{ "usage": "sleep and exertion" }""")]
    [InlineData("""{ "usage": "sleep/exertion" }""")]
    [InlineData("""{ "usage": "sleep & exertion" }""")]
    public void GetEnumOrDefault_ParsesFlags_WhenConnectorsPresent(string json)
    {
        var root = Parse(json);
        var usage = root.GetEnumOrDefault("usage", UsageContext.None);
        Assert.Equal(UsageContext.Sleep | UsageContext.Exertion, usage);
    }
}