namespace SignalBooster.Domain.Prescriptions;

public sealed record BiPapPrescription(
    int? IpapCmH2O,               // Inspiratory pressure
    int? EpapCmH2O,               // Expiratory pressure
    int? BackupRateBpm,           // Optional (ST/backup mode)
    MaskType MaskType,
    bool HeatedHumidifier,
    int? Ahi                      // Keep numeric
) : IDevicePrescription
{
    public DeviceType Device => DeviceType.BiPap;
}
