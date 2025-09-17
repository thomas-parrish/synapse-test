namespace SignalBooster.Domain.Prescriptions;

public sealed record OxygenPrescription(
    decimal? FlowLitersPerMinute,
    UsageContext Usage
) : IDevicePrescription
{
    public DeviceType Device => DeviceType.OxygenTank;
}
