using SignalBooster.Infrastructure.OrderClient;

public class ExternalOrderRequestFormatter_AhiQualifierTests
{
    // helper: expose qualifier via a tiny wrapper since method is private
    private static string? Qualify(int? ahi, DateOnly? dob) =>
        typeof(ExternalOrderRequestFormatter)
            .GetMethod("BuildAhiQualifier", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, new object?[] { ahi, dob }) as string;

    [Fact]
    public void Returns_null_when_ahi_is_null()
    {
        var q = Qualify(null, new DateOnly(1980, 1, 1));
        Assert.Null(q);
    }

    [Theory]
    // Adult thresholds
    [InlineData(4, "AHI < 5 (normal, adult)")]
    [InlineData(5, "AHI > 5 (mild, adult)")]
    [InlineData(15, "AHI > 15 (moderate, adult)")]
    [InlineData(30, "AHI > 30 (severe, adult)")]
    public void Adult_rules_apply_when_age_18_or_over(int ahi, string expected)
    {
        var dob = new DateOnly(DateTime.Today.Year - 18, 1, 1); // exactly 18
        var q = Qualify(ahi, dob);
        Assert.Equal(expected, q);
    }

    [Theory]
    // Pediatric thresholds
    [InlineData(0, "AHI < 1 (normal, pediatric)")]
    [InlineData(1, "AHI > 1 (mild, pediatric)")]
    [InlineData(5, "AHI > 5 (moderate, pediatric)")]
    [InlineData(10, "AHI > 10 (severe, pediatric)")]
    public void Pediatric_rules_apply_when_under_18(int ahi, string expected)
    {
        var dob = new DateOnly(DateTime.Today.Year - 10, 1, 1); // clearly pediatric
        var q = Qualify(ahi, dob);
        Assert.Equal(expected, q);
    }

    [Fact]
    public void Unknown_dob_defaults_to_adult_rules()
    {
        var q = Qualify(4, null); // treat as adult per existing behavior
        Assert.Equal("AHI < 5 (normal, adult)", q);
    }
}
