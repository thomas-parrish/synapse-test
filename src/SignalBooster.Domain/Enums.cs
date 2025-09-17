namespace SignalBooster.Domain;

public enum DeviceType
{
    Unknown = 0,
    Cpap,
    BiPap,
    OxygenTank,
    Wheelchair
}

[Flags]
public enum UsageContext
{
    None = 0,
    Sleep = 1,
    Exertion = 1 << 1,
    // add more later (e.g., Rest = 1<<2, Ambulation = 1<<3, etc.)
}

public enum MaskType 
{ 
    Unknown = 0,
    FullFace,
    Nasal,
    NasalPillow
}