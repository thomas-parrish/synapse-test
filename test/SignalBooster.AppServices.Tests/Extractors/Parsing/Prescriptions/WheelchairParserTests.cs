using SignalBooster.AppServices.Extractors;
using SignalBooster.AppServices.Extractors.Parsing.Prescriptions;
using SignalBooster.AppServices.Extractors.Simple;
using SignalBooster.Domain.Prescriptions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SignalBooster.AppServices.Tests.Extractors.Parsing.Prescriptions;

public sealed class WheelchairParserTests
{
    private static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly INoteExtractor _extractor = new SimpleNoteExtractor();

    [Fact]
    public async Task WheelchairParser_ParsesManualWithElevatingLegRests_WhenIncluded()
    {
        var note = """
            Patient Name: Daniel Hayes
            DOB: 03/14/1950
            Diagnosis: Osteoarthritis of both knees
            Prescription: Manual wheelchair required for daily mobility.
            Wheelchair type: manual
            Leg rests: elevating
            Ordering Physician: Dr. Michelle Harper
            """;

        var model = await _extractor.ExtractAsync(note);
        var json = JsonSerializer.Serialize(model, Pretty);

        await VerifyJson(json);
    }

    [Fact]
    public async Task WheelchairParser_ParsesPowerWithSwingAwayLegRests_WhenIncluded()
    {
        var note = """
            Patient Name: Alice Martinez
            DOB: 11/05/1968
            Diagnosis: Advanced multiple sclerosis
            Recommendation: Patient requires power wheelchair for safe community ambulation.
            Wheelchair type: power
            Leg rests: swing-away
            Ordering Physician: Dr. Kevin Patel
            """;

        var model = await _extractor.ExtractAsync(note);
        var json = JsonSerializer.Serialize(model, Pretty);

        await VerifyJson(json);
    }

    [Fact]
    public async Task WheelchairParser_ParsesTransportWithFixedLegRests_WhenIncluded()
    {
        var note = """
            Patient Name: George White
            DOB: 07/22/1942
            Diagnosis: Post-stroke with limited endurance
            Prescription: Transport chair prescribed for caregiver-assisted transfers.
            Wheelchair type: transport
            Leg rests: fixed
            Ordering Physician: Dr. Emily Chen
            """;

        var model = await _extractor.ExtractAsync(note);
        var json = JsonSerializer.Serialize(model, Pretty);

        await VerifyJson(json);
    }

    [Fact]
    public async Task WheelchairParser_ParsesManualWithArticulatingLegRests_WhenIncluded()
    {
        var note = """
            Patient Name: Laura Smith
            DOB: 09/09/1977
            Diagnosis: Quadriceps contracture post-injury
            Recommendation: Manual wheelchair with articulating leg rests for knee flexion needs.
            Ordering Physician: Dr. Robert Green
            """;

        var model = await _extractor.ExtractAsync(note);
        var json = JsonSerializer.Serialize(model, Pretty);

        await VerifyJson(json);
    }

    [Fact]
    public async Task WheelchairParser_ParsesManualWithGelCushion_WhenIncluded()
    {
        var note = """
            Patient Name: Peter Johnson
            DOB: 02/18/1985
            Diagnosis: Spinal cord injury with risk of pressure ulcers
            Prescription: Manual wheelchair with gel cushion.
            Ordering Physician: Dr. Sarah Lopez
            """;

        var model = await _extractor.ExtractAsync(note);
        var json = JsonSerializer.Serialize(model, Pretty);

        await VerifyJson(json);
    }

    [Fact]
    public async Task WheelchairParser_ParsesFoamCushion_WhenIncluded()
    {
        var note = """
            Patient Name: Karen Wells
            DOB: 08/01/1960
            Diagnosis: Degenerative disc disease
            Recommendation: Manual wheelchair with foam cushion for added comfort.
            Ordering Physician: Dr. Anthony Reid
            """;

        var model = await _extractor.ExtractAsync(note);
        var json = JsonSerializer.Serialize(model, Pretty);

        await VerifyJson(json);
    }

    [Fact]
    public async Task WheelchairParser_ParsesAirCushion_WhenIncluded()
    {
        var note = """
            Patient Name: Henry Clark
            DOB: 01/11/1958
            Diagnosis: Severe scoliosis
            Prescription: Wheelchair with air cushion to redistribute seating pressure.
            Ordering Physician: Dr. Olivia King
            """;

        var model = await _extractor.ExtractAsync(note);
        var json = JsonSerializer.Serialize(model, Pretty);

        await VerifyJson(json);
    }

    [Fact]
    public async Task WheelchairParser_ParsesRohoCushion_WhenIncluded()
    {
        var note = """
            Patient Name: Susan Young
            DOB: 06/25/1970
            Diagnosis: Paraplegia
            Recommendation: Power wheelchair prescribed with Roho cushion.
            Ordering Physician: Dr. James Allen
            """;

        var model = await _extractor.ExtractAsync(note);
        var json = JsonSerializer.Serialize(model, Pretty);

        await VerifyJson(json);
    }

    private static WheelchairPrescription Parse(string text)
    {
        var parser = new WheelchairParser();
        var fields = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var hint = text.ToLowerInvariant();

        var rx = parser.Parse(fields, text, hint);
        return Assert.IsType<WheelchairPrescription>(rx);
    }

    [Theory]
    [InlineData("Patient requires a manual wheelchair with elevating leg rests due to edema.", "elevating")]
    [InlineData("Manual chair, swing-away leg rests to facilitate transfers.", "swing-away")]
    [InlineData("Power wheelchair ordered with fixed leg rests.", "fixed")]
    [InlineData("Transport chair with articulating leg rests as needed.", "articulating")]
    public void Finds_LegRests_By_Specific_Phrases(string note, string expected)
    {
        var wc = Parse(note);
        Assert.Equal(expected, wc.LegRests);
    }
}