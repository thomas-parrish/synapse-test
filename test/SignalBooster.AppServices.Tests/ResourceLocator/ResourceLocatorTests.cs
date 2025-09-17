using SignalBooster.AppServices.ResourceLocator;
using SignalBooster.AppServices.Tests.Extractors.Simple;

namespace SignalBooster.AppServices.Tests.ResourceLocator;

public class ResourceLocatorTests
{
    [Fact]
    public void FromNamespaceOf_SetsAssemblyAndBaseNamespace()
    {
        // Act
        var loc = Resource.FromNamespaceOf(typeof(ResourceLocatorTests));

        // Assert (no exception & fluent chaining works)
        var chained = loc.WithName("does-not-need-to-exist.txt");
        Assert.NotNull(chained);
    }

    [Theory]
    [InlineData(null)]
    public void FromNamespaceOf_Throws_WhenTypeIsNull(Type? t)
    {
        Assert.Throws<ArgumentNullException>(() => Resource.FromNamespaceOf(t!));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void WithName_Throws_WhenNameIsNullOrEmpty(string? name)
    {
        var loc = Resource.FromNamespaceOf(typeof(ResourceLocatorTests));
        Assert.Throws<ArgumentException>(() => loc.WithName(name!));
    }

    [Fact]
    public async Task ReadAsync_LoadsEmbeddedText_WhenValid()
    {
        var text = await Resource
            .FromNamespaceOf(typeof(SimpleNoteExtractorTests))
            .WithName("note.txt")
            .ReadAsync();

        Assert.False(string.IsNullOrWhiteSpace(text));
        Assert.Contains("Patient", text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ReadAsync_Throws_When_Resource_Not_Found()
    {
        var loc = Resource
            .FromNamespaceOf(typeof(ResourceLocatorTests))
            .WithName("does-not-exist.txt");

        var ex = await Assert.ThrowsAsync<FileNotFoundException>(() => loc.ReadAsync());
        Assert.Contains("does-not-exist.txt", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
