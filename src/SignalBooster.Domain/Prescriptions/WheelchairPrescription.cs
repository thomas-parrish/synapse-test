namespace SignalBooster.Domain.Prescriptions;

public sealed record WheelchairPrescription(
    string? Type,                 // "manual", "power", "transport"
    int? SeatWidthIn,             // e.g., "18"
    int? SeatDepthIn,             // e.g., "16"
    string? LegRests,             // "elevating", "swing-away"
    string? Cushion,              // "gel", "foam", etc.
    string? Justification         // brief functional need / dx link
) : IDevicePrescription
{
    public DeviceType Device => DeviceType.Wheelchair;
}