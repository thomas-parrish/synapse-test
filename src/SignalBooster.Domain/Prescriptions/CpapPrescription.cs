namespace SignalBooster.Domain.Prescriptions;

public sealed record CpapPrescription(
    MaskType MaskType,
    bool HeatedHumidifier,
    int? Ahi
) : IDevicePrescription
{
    public DeviceType Device => DeviceType.Cpap;
}
