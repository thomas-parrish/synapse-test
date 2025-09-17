using SignalBooster.AppServices.Extractors.Parsing;

namespace SignalBooster.AppServices.Tests.Extractors.Parsing;

public class FlowRateParserTests
{
    [Theory]
    [InlineData("2 L/min", 2.0)]
    [InlineData("2.5 L/min", 2.5)]
    [InlineData("2 L per min", 2.0)]
    [InlineData("2 L", 2.0)]
    [InlineData("2.5 LPM", 2.5)]
    public void Parses_Common_Forms(string input, decimal expected)
    {
        var actual = FlowRateParser.Parse(input);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void Returns_Null_When_Not_Present()
    {
        var actual = FlowRateParser.Parse("No oxygen settings provided.");
        Assert.Null(actual);
    }
}