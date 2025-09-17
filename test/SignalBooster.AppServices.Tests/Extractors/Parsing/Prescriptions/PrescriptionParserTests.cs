using SignalBooster.AppServices.Extractors.Parsing;
using SignalBooster.AppServices.Extractors.Parsing.Prescriptions;

namespace SignalBooster.AppServices.Tests.Extractors.Parsing.Prescriptions;

public sealed class PrescriptionParserTests
{
    // Small helper to build fields + hint the same way the extractor does.
    private static (Dictionary<string, string> fields, string fullText, string hint) Prep(string raw, string? recOrRxKey = null)
    {
        var fullText = raw;
        var fields = KeyValueParser.Parse(raw);

        string? recOrRx = null;
        if (!string.IsNullOrWhiteSpace(recOrRxKey))
        {
            recOrRx = KeyValueParser.Get(fields, recOrRxKey);
        }
        else
        {
            recOrRx = KeyValueParser.Get(fields, "Recommendation", "Prescription", "Device");
        }

        var hint = $"{recOrRx} {fullText}".ToLowerInvariant();
        return (fields, fullText, hint);
    }

    [Fact]
    public async Task OxygenParser_Parses_Flow_And_Usage()
    {
        var raw = """
        Patient Name: Harold Finch
        Diagnosis: COPD
        Prescription: Requires a portable oxygen tank delivering 2 L per minute.
        Usage: During sleep and exertion.
        Ordering Physician: Dr. Cuddy
        """;

        var (fields, text, hint) = Prep(raw);

        var sut = new OxygenParser();
        Assert.True(sut.Matches(hint));

        var result = sut.Parse(fields, text, hint);

        await Verify(result);
    }

    [Fact]
    public async Task CpapParser_Parses_Mask_Humidifier_And_Ahi()
    {
        var raw = """
        Patient Name: Lisa Turner
        DOB: 09/23/1984
        Diagnosis: Severe sleep apnea
        Recommendation: CPAP therapy with full face mask and heated humidifier.
        AHI: 28
        Ordering Physician: Dr. Foreman
        """;

        var (fields, text, hint) = Prep(raw);

        var sut = new CpapParser();
        Assert.True(sut.Matches(hint));

        var result = sut.Parse(fields, text, hint);

        await Verify(result);
    }

    [Fact]
    public async Task BiPapParser_Parses_Ipap_Epap_Backup_Mask_Ahi()
    {
        var raw = """
        Patient Name: John Doe
        Diagnosis: Severe OSA
        Recommendation: BiPAP indicated with heated humidifier and nasal mask.
        Settings: IPAP: 16 cm H2O, EPAP: 8 cm H2O, backup rate: 12
        AHI: 22
        """;

        var (fields, text, hint) = Prep(raw);

        var sut = new BiPapParser();
        Assert.True(sut.Matches(hint));

        var result = sut.Parse(fields, text, hint);

        await Verify(result);
    }

    [Fact]
    public async Task WheelchairParser_Parses_Type_Fit_And_Accessories()
    {
        var raw = """
        Patient Name: Jane Roe
        DOB: 01/22/1960
        Diagnosis: CVA with left hemiparesis
        Prescription: Manual wheelchair, seat width 18", seat depth 16", with elevating leg rests and gel cushion.
        Ordering Physician: Dr. Smith
        """;

        var (fields, text, hint) = Prep(raw);

        var sut = new WheelchairParser();
        Assert.True(sut.Matches(hint));

        var result = sut.Parse(fields, text, hint);

        await Verify(result);
    }

    [Fact]
    public void Parsers_Do_Not_Match_Irrelevant_Hint()
    {
        var raw = "Recommendation: Nebulizer therapy as needed.";
        var (_, _, hint) = Prep(raw);

        Assert.False(new OxygenParser().Matches(hint));
        Assert.False(new CpapParser().Matches(hint));
        Assert.False(new BiPapParser().Matches(hint));
        Assert.False(new WheelchairParser().Matches(hint));
    }
}