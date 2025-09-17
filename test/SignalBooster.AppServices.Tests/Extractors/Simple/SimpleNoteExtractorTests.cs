using SignalBooster.AppServices.Extractors.Simple;
using SignalBooster.AppServices.ResourceLocator;

namespace SignalBooster.AppServices.Tests.Extractors.Simple;

public sealed class SimpleNoteExtractorTests
{
    [Fact]
    public async Task Should_Extract_Oxygen_From_Valid_Text()
    {
        var raw = await Resource.FromNamespaceOf(typeof(SimpleNoteExtractorTests))
                                .WithName("note.txt")
                                .ReadAsync();

        var sut = new SimpleNoteExtractor();
        var note = sut.Extract(raw);

        await Verify(note)
            .DontScrubDateTimes();
    }

    [Fact]
    public async Task Should_Extract_Cpap_From_Valid_Json()
    {
        var raw = await Resource.FromNamespaceOf(typeof(SimpleNoteExtractorTests))
                                .WithName("note.json")
                                .ReadAsync();

        var sut = new SimpleNoteExtractor();
        var note = sut.Extract(raw);

        await Verify(note)
            .DontScrubDateTimes();
    }
}
