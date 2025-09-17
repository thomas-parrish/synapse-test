using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;
using SignalBooster.Infrastructure.OrderClient;

namespace SignalBooster.Infrastructure.Tests.OrderClient;

public class ExternalOrderRequestFormatterTests
{
    private readonly ExternalOrderRequestFormatter _sut = new();

    [Fact]
    public async Task Formats_OxygenPrescription()
    {
        var note = new PhysicianNote
        {
            PatientName = "Harold Finch",
            PatientDateOfBirth = new DateOnly(1952, 4, 12),
            Diagnosis = "COPD",
            OrderingPhysician = "Dr. Cuddy",
            Prescription = new OxygenPrescription(
                FlowLitersPerMinute: 2m,
                Usage: UsageContext.Sleep | UsageContext.Exertion)
        };

        var json = _sut.Format(note);

        await VerifyJson(json)
            .DontScrubDateTimes();
    }

    [Fact]
    public async Task Formats_CpapPrescription()
    {
        var note = new PhysicianNote
        {
            PatientName = "Lisa Turner",
            PatientDateOfBirth = new DateOnly(1984, 9, 23),
            Diagnosis = "Severe sleep apnea",
            OrderingPhysician = "Dr. Foreman",
            Prescription = new CpapPrescription(
                MaskType: MaskType.FullFace,
                HeatedHumidifier: true,
                Ahi: 28)
        };

        var json = _sut.Format(note);

        await VerifyJson(json)
            .DontScrubDateTimes();
    }
}