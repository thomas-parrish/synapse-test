using SignalBooster.AppServices.Extractors.Parsing.Prescriptions;
using SignalBooster.Domain;

namespace SignalBooster.AppServices.Tests.Extractors.Parsing.Prescriptions;

public class PrescriptionParsingTests
{
    [Theory]
    [InlineData("IPAP: 16 cm H2O", @"IPAP\s*[:=]?\s*(\d{1,2})", "16")]
    [InlineData("seat width: 18", @"seat\s*width\s*[:=]?\s*(\d{2})", "18")]
    public void MatchGroup_ReturnsGroup_WhenFound(string raw, string pattern, string expected)
    {
        Assert.Equal(expected, PrescriptionParsing.MatchGroup(raw, pattern));
    }

    [Fact]
    public void MatchGroup_ReturnsNull_WhenNotFound()
    {
        Assert.Null(PrescriptionParsing.MatchGroup("no numbers", @"(\d{2})"));
    }

    [Theory]
    [InlineData("EPAP= 8 cmH2O", @"EPAP\s*[:=]?\s*(\d{1,2})", 8)]
    [InlineData("backup rate: 12", @"backup\s*rate\s*[:=]?\s*(\d{1,2})", 12)]
    public void ParseFirstInt_parses_int_when_present(string raw, string pattern, int expected)
    {
        Assert.Equal(expected, PrescriptionParsing.ParseFirstInt(raw, pattern));
    }

    [Fact]
    public void ParseFirstInt_ReturnsNull_WhenNoMatch()
    {
        Assert.Null(PrescriptionParsing.ParseFirstInt("value: X", @"value\s*[:=]?\s*(\d+)"));
    }

    [Theory]
    [InlineData("cpap with full face mask", MaskType.FullFace)]
    [InlineData("nasal pillow interface preferred", MaskType.NasalPillow)]
    [InlineData("nasal mask recommended", MaskType.Nasal)]
    [InlineData("interface unspecified", MaskType.Unknown)]
    public void ParseMaskType_Maps_Keywords(string hint, MaskType expected)
    {
        Assert.Equal(expected, PrescriptionParsing.ParseMaskType(hint));
    }
}