using SignalBooster.AppServices.Extractors.Parsing;
using System.Globalization;

namespace SignalBooster.AppServices.Tests.Extractors.Parsing;

public sealed class DateParserTests
{
    [Theory]
    // Exact formats (InvariantCulture, exact match)
    [InlineData("09/23/1984", 1984, 9, 23)]  // MM/dd/yyyy
    [InlineData("9/3/1984", 1984, 9, 3)]   // M/d/yyyy
    [InlineData("04-12-1952", 1952, 4, 12)]  // MM-dd-yyyy
    [InlineData("4-5-1952", 1952, 4, 5)]   // M-d-yyyy
    [InlineData("1984-09-23", 1984, 9, 23)]  // yyyy-MM-dd
    [InlineData("1984/9/23", 1984, 9, 23)]  // yyyy/M/d
    public void Parse_Exact_Formats_Invariant(string input, int y, int m, int d)
    {
        DateOnly? actual = DateParser.Parse(input);
        Assert.Equal(new DateOnly(y, m, d), actual);
    }

    [Theory]
    [InlineData("  09/23/1984  ", 1984, 9, 23)]
    [InlineData("\t4-5-1952\r\n", 1952, 4, 5)]
    public void Parse_Trims_Whitespace(string input, int y, int m, int d)
    {
        DateOnly? actual = DateParser.Parse(input);
        Assert.Equal(new DateOnly(y, m, d), actual);
    }

    [Fact]
    public void Parse_Generic_Culture_Specific_Fallback_MonthName_Us()
    {
        // Force en-US so "September 23, 1984" parses deterministically
        using var scope = new CultureScope("en-US");

        DateOnly? actual = DateParser.Parse("September 23, 1984");
        Assert.Equal(new DateOnly(1984, 9, 23), actual);
    }

    [Fact]
    public void Parse_Null_Or_Empty_Returns_Null()
    {
        Assert.Null(DateParser.Parse(null));
        Assert.Null(DateParser.Parse(""));
        Assert.Null(DateParser.Parse("   "));
    }

    [Fact]
    public void Parse_Invalid_Returns_Null()
    {
        DateOnly? actual = DateParser.Parse("not a date");
        Assert.Null(actual);
    }
}

/// <summary>
/// Temporarily sets CurrentCulture and CurrentUICulture for the duration of a test.
/// Ensures parser fallback using DateTime.TryParse behaves deterministically.
/// </summary>
internal sealed class CultureScope : IDisposable
{
    private readonly CultureInfo _original;
    private readonly CultureInfo _originalUi;

    public CultureScope(string cultureName)
    {
        _original = CultureInfo.CurrentCulture;
        _originalUi = CultureInfo.CurrentUICulture;

        var ci = new CultureInfo(cultureName);
        CultureInfo.CurrentCulture = ci;
        CultureInfo.CurrentUICulture = ci;
    }

    public void Dispose()
    {
        CultureInfo.CurrentCulture = _original;
        CultureInfo.CurrentUICulture = _originalUi;
    }
}