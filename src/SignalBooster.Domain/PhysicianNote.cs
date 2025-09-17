using SignalBooster.Domain.Prescriptions;

namespace SignalBooster.Domain;

public record PhysicianNote
{
    public string? OrderingPhysician { get; set; }
    public string? PatientName { get; set; }
    public DateOnly? PatientDateOfBirth { get; set; }
    public string? Diagnosis { get; set; }
    public IDevicePrescription? Prescription { get; set; }
}
