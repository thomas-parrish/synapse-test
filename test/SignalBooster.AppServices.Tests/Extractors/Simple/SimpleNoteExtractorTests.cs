using SignalBooster.AppServices.Extractors.Simple;
using SignalBooster.AppServices.ResourceLocator;

namespace SignalBooster.AppServices.Tests.Extractors.Simple;

public sealed class SimpleNoteExtractorTests
{
    [Fact]
    public async Task ExtractAsync_ReturnsEmptyNote_OnNullOrWhitespace()
    {
        var sut = new SimpleNoteExtractor();
        var note = await sut.ExtractAsync(string.Empty);

        await Verify(note);
    }

    [Fact]
    public async Task ExtractAsync_ExtractsOxygen_FromValidText()
    {
        var raw = await Resource.FromNamespaceOf(typeof(SimpleNoteExtractorTests))
                                .WithName("note.txt")
                                .ReadAsync();

        var sut = new SimpleNoteExtractor();
        var note = await sut.ExtractAsync(raw);

        await Verify(note)
            .DontScrubDateTimes();
    }

    [Fact]
    public async Task ExtractAsync_ExtractsCpap_FromValidText()
    {
        var raw = await Resource.FromNamespaceOf(typeof(SimpleNoteExtractorTests))
                                .WithName("note.json")
                                .ReadAsync();

        var sut = new SimpleNoteExtractor();
        var note = await sut.ExtractAsync(raw);

        await Verify(note)
            .DontScrubDateTimes();
    }
}
