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

    [Fact]
    public async Task Formats_BiPapPrescription()
    {
        var note = new PhysicianNote
        {
            PatientName = "John Doe",
            PatientDateOfBirth = new DateOnly(1970, 1, 2),
            Diagnosis = "Severe OSA",
            OrderingPhysician = "Dr. House",
            Prescription = new BiPapPrescription(
                IpapCmH2O: 16,
                EpapCmH2O: 8,
                BackupRateBpm: 12,
                MaskType: MaskType.Nasal,
                HeatedHumidifier: true,
                Ahi: 22)
        };

        var json = _sut.Format(note);

        await VerifyJson(json)
            .DontScrubDateTimes();
    }

    [Fact]
    public async Task Formats_WheelchairPrescription()
    {
        var note = new PhysicianNote
        {
            PatientName = "Michael Andrews",
            PatientDateOfBirth = new DateOnly(1975, 7, 15),
            Diagnosis = "Multiple sclerosis with lower extremity weakness",
            OrderingPhysician = "Dr. Karen Blake",
            Prescription = new WheelchairPrescription(
                Type: "manual",
                SeatWidthIn: 18,
                SeatDepthIn: 16,
                LegRests: "elevating",
                Cushion: "gel",
                Justification: null)
        };

        var json = _sut.Format(note);

        await VerifyJson(json)
            .DontScrubDateTimes();
    }
}