using SignalBooster.AppServices.Extractors.OpenAi;

namespace SignalBooster.AppServices.Tests.Extractors.OpenAi;

public sealed class OpenAiNoteExtractorTests
{
    [Fact]
    public async Task CPAP_is_mapped_correctly()
    {
        var json = """
        {
          "patient_name": "Lisa Turner",
          "dob": "09/23/1984",
          "diagnosis": "Severe sleep apnea",
          "ordering_physician": "Dr. Foreman",
          "prescription": {
            "device": "CPAP",
            "mask_type": "full face",
            "heated_humidifier": true,
            "ahi": 28
          }
        }
        """;

        var extractor = new OpenAiNoteExtractor(new FakeLLMClient((_, _) => json));

        var note = await extractor.ExtractAsync("""(raw CPAP note text not used by fake)""");

        await Verify(note);
    }

    [Fact]
    public async Task Oxygen_is_mapped_correctly()
    {
        var json = """
        {
          "patient_name": "Harold Finch",
          "dob": "04/12/1952",
          "diagnosis": "COPD",
          "ordering_physician": "Dr. Cuddy",
          "prescription": {
            "device": "Oxygen Tank",
            "liters": 2,
            "usage": "sleep and exertion"
          }
        }
        """;

        var extractor = new OpenAiNoteExtractor(new FakeLLMClient((_, _) => json));

        var note = await extractor.ExtractAsync("(oxygen note)");

        await Verify(note);
    }

    [Fact]
    public async Task BiPap_is_mapped_correctly()
    {
        var json = """
        {
          "patient_name": "John Doe",
          "dob": "01/02/1970",
          "diagnosis": "Severe OSA",
          "ordering_physician": "Dr. House",
          "prescription": {
            "device": "BiPAP",
            "mask_type": "nasal",
            "heated_humidifier": true,
            "ahi": 22,
            "ipap_cm_h2o": 16,
            "epap_cm_h2o": 8,
            "backup_rate": 12
          }
        }
        """;

        var extractor = new OpenAiNoteExtractor(new FakeLLMClient((_, _) => json));

        var note = await extractor.ExtractAsync("(bipap note)");

        await Verify(note);
    }

    [Fact]
    public async Task Wheelchair_is_mapped_correctly()
    {
        var json = """
        {
          "patient_name": "Michael Andrews",
          "dob": "07/15/1975",
          "diagnosis": "Multiple sclerosis",
          "ordering_physician": "Dr. Karen Blake",
          "prescription": {
            "device": "Wheelchair",
            "chair_type": "manual",
            "seat_width_in": 18,
            "seat_depth_in": 16,
            "leg_rests": "elevating",
            "cushion": "gel"
          }
        }
        """;

        var extractor = new OpenAiNoteExtractor(new FakeLLMClient((_, _) => json));

        var note = await extractor.ExtractAsync("(wheelchair note)");

        await Verify(note);
    }

    [Fact]
    public async Task Code_fenced_JSON_is_accepted()
    {
        var fenced = """
        ```json
        {
          "patient_name": "Test",
          "dob": "01/01/2000",
          "diagnosis": null,
          "ordering_physician": null,
          "prescription": { "device": null }
        }
        ```
        """;

        var extractor = new OpenAiNoteExtractor(new FakeLLMClient((_, _) => fenced));

        var note = await extractor.ExtractAsync("(any)");

        await Verify(note)
            .DontScrubDateTimes()
            .AddExtraSettings(_ => _.DefaultValueHandling = Argon.DefaultValueHandling.Include);
    }

    [Fact]
    public async Task Bad_JSON_throws_with_context()
    {
        var bad = "{ not valid json";

        var extractor = new OpenAiNoteExtractor(new FakeLLMClient((_, _) => bad));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => extractor.ExtractAsync("(bad)"));
        Assert.Contains("Failed to parse LLM JSON", ex.Message);
        Assert.Contains("not valid json", ex.Message);
    }
}