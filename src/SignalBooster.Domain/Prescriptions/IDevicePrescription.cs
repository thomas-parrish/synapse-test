using System.Text.Json.Serialization;

namespace SignalBooster.Domain.Prescriptions;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "device")]
[JsonDerivedType(typeof(CpapPrescription), "CPAP")]
[JsonDerivedType(typeof(OxygenPrescription), "Oxygen Tank")]
[JsonDerivedType(typeof(BiPapPrescription), "BIPAP")]
[JsonDerivedType(typeof(WheelchairPrescription), "Wheelchair")]
public interface IDevicePrescription
{
    DeviceType Device { get; }
}
