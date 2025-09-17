using SignalBooster.AppServices.Extractors.Parsing;

namespace SignalBooster.AppServices.Tests.Extractors.Parsing;

public sealed class AhiParserTests
{
    [Theory]
    [InlineData("28", "", 28)]
    [InlineData("AHI 28", "", 28)]
    [InlineData("  31  ", "", 31)]
    [InlineData("28.5", "", 28)] // first integer token "28"
    public void FieldValue_Wins_When_Present(string field, string raw, int expected)
    {
        int? actual = AhiParser.Parse(field, raw);
        Assert.Equal(expected, actual);
    }

    [Theory]
    [InlineData(null, "AHI: 28", 28)]
    [InlineData("", "AHI: 28", 28)]
    [InlineData(null, "AHI > 20", 20)]
    [InlineData(null, "ahi: 33", 33)] // case-insensitive
    [InlineData(null, "Some text AHI: 17 elsewhere", 17)]
    public void Falls_Back_To_Raw_Text_When_Field_Missing(string? field, string raw, int expected)
    {
        int? actual = AhiParser.Parse(field, raw);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Prefers_Field_Over_Raw_When_Both_Present()
    {
        int? actual = AhiParser.Parse("18", "AHI: 29");
        Assert.Equal(18, actual);
    }

    [Theory]
    [InlineData(null, "AHI: N/A")]
    [InlineData("", "No apnea metric here.")]
    [InlineData("N/A", "")]
    public void Returns_Null_When_No_Parsable_Integer(string? field, string raw)
    {
        int? actual = AhiParser.Parse(field, raw);
        Assert.Null(actual);
    }
}