using SignalBooster.Domain;
using SignalBooster.Domain.Prescriptions;
using System.Globalization;
using System.Text.Json;

namespace SignalBooster.Infrastructure.OrderClient;

public sealed class ExternalOrderRequestFormatter : IOrderRequestFormatter
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        WriteIndented = false,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    /// <summary>
    /// Produces legacy JSON like:
    /// {
    ///   "device": "Oxygen Tank",
    ///   "liters": "2 L",
    ///   "usage": "sleep and exertion",
    ///   "diagnosis": "COPD",
    ///   "ordering_provider": "Dr. Cuddy",
    ///   "patient_name": "Harold Finch",
    ///   "dob": "04/12/1952"
    /// }
    /// </summary>
    public string Format(PhysicianNote note)
    {
        ArgumentNullException.ThrowIfNull(note);

        var dto = BuildDictionary(note);

        // We intentionally use Dictionary -> JSON so we can control snake-case keys.
        var json = JsonSerializer.Serialize(dto, JsonSerializerOptions);

        return json;
    }

    private static Dictionary<string, object?> BuildDictionary(PhysicianNote note)
    {
        var dict = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["diagnosis"] = note.Diagnosis,
            ["ordering_provider"] = note.OrderingPhysician,
            ["patient_name"] = note.PatientName,
            ["dob"] = note.PatientDateOfBirth.HasValue
                                       ? note.PatientDateOfBirth.Value.ToString("MM/dd/yyyy", CultureInfo.InvariantCulture)
                                       : null
        };

        switch (note.Prescription)
        {
            case OxygenPrescription oxy:
                {
                    AddOxygenFields(dict, oxy);
                    break;
                }
            case CpapPrescription cpap:
                {
                    AddCpapFields(dict, cpap, note.PatientDateOfBirth);
                    break;
                }
            case BiPapPrescription bipap:
                {
                    AddBiPapFields(dict, bipap, note.PatientDateOfBirth);
                    break;
                }
            case WheelchairPrescription wc:
                {
                    AddWheelchairFields(dict, wc);
                    break;
                }
            default:
                {
                    dict["device"] = "Unknown";
                    break;
                }
        }

        return dict;
    }

    private static void AddOxygenFields(Dictionary<string, object?> dict, OxygenPrescription oxy)
    {
        dict["device"] = "Oxygen Tank";

        if (oxy.FlowLitersPerMinute.HasValue)
        {
            // The expected output file shows us an example in the format of "lpm L" so we convert to this format
            var lpm = oxy.FlowLitersPerMinute.Value;
            dict["liters"] = $"{TrimTrailingZeros(lpm)} L";
        }
        else
        {
            dict["liters"] = null;
        }

        dict["usage"] = UsageToString(oxy.Usage);
    }

    private static void AddCpapFields(Dictionary<string, object?> dict, CpapPrescription cpap, DateOnly? dob)
    {
        dict["device"] = "CPAP";

        dict["mask_type"] = cpap.MaskType switch
        {
            MaskType.FullFace => "full face",
            MaskType.Nasal => "nasal",
            MaskType.NasalPillow => "nasal pillow",
            _ => null
        };

        dict["add_ons"] = cpap.HeatedHumidifier
            ? new[] { "heated humidifier" }
            : null;

        dict["qualifier"] = BuildAhiQualifier(cpap.Ahi, dob);
    }

    private static void AddBiPapFields(Dictionary<string, object?> dict, BiPapPrescription bipap, DateOnly? dob)
    {
        dict["device"] = "BiPAP";

        dict["mask_type"] = bipap.MaskType switch
        {
            MaskType.FullFace => "full face",
            MaskType.Nasal => "nasal",
            MaskType.NasalPillow => "nasal pillow",
            _ => null
        };

        dict["add_ons"] = bipap.HeatedHumidifier ? new[] { "heated humidifier" } : null;

        // Keep the legacy "qualifier" concept aligned with CPAP (AHI-based)
        dict["qualifier"] = BuildAhiQualifier(bipap.Ahi, dob);

        // Include BiPAP pressures if present (legacy didn’t have these; safe to add)
        dict["ipap_cm_h2o"] = bipap.IpapCmH2O;
        dict["epap_cm_h2o"] = bipap.EpapCmH2O;
        dict["backup_rate"] = bipap.BackupRateBpm;
    }

    private static void AddWheelchairFields(Dictionary<string, object?> dict, WheelchairPrescription wc)
    {
        dict["device"] = "Wheelchair";
        dict["chair_type"] = wc.Type;                 // "manual" | "power" | "transport"
        dict["seat_width_in"] = wc.SeatWidthIn;       // int?
        dict["seat_depth_in"] = wc.SeatDepthIn;       // int?
        dict["leg_rests"] = wc.LegRests;              // "elevating" | "swing-away" | ...
        dict["cushion"] = wc.Cushion;                 // "gel" | "foam" | "air" | "roho"
        // No "justification" in legacy payload; omit unless API requires it
    }

    /// <summary>
    /// Builds an AHI qualifier string used for insurance coverage justification.
    /// Rules are based on AASM cutoffs, adjusted for adults vs pediatrics.
    /// </summary>
    private static string? BuildAhiQualifier(int? ahi, DateOnly? dob)
    {
        // I just googled these rules, because I was confused by the discrepency in the instructions
        // between the sample main file looking for a qualifier and the data sample that provided raw AHI
        if (ahi is null)
        {
            return null;
        }

        var today = DateOnly.FromDateTime(DateTime.Today);
        var age = CalculateAge(dob, today);
        var isAdult = age >= 18;

        return isAdult
            ? BuildAdultAhiQualifier(ahi.Value)
            : BuildPediatricAhiQualifier(ahi.Value);
    }

    private static string BuildAdultAhiQualifier(int ahi) =>
        ahi switch
        {
            >= 30 => "AHI > 30 (severe, adult)",
            >= 15 => "AHI > 15 (moderate, adult)",
            >= 5 => "AHI > 5 (mild, adult)",
            _ => "AHI < 5 (normal, adult)"
        };

    private static string BuildPediatricAhiQualifier(int ahi) =>
        ahi switch
        {
            >= 10 => "AHI > 10 (severe, pediatric)",
            >= 5 => "AHI > 5 (moderate, pediatric)",
            >= 1 => "AHI > 1 (mild, pediatric)",
            _ => "AHI < 1 (normal, pediatric)"
        };

    private static int CalculateAge(DateOnly? dob, DateOnly today)
    {
        if (dob is null)
        {
            return int.MaxValue; // Treat as adult if unknown
        }

        var age = today.Year - dob.Value.Year;
        if (today < dob.Value.AddYears(age))
        {
            age--;
        }

        return age;
    }

    private static string? UsageToString(UsageContext usage)
    {
        if (usage == (UsageContext.Sleep | UsageContext.Exertion))
        {
            return "sleep and exertion";
        }

        if (usage == UsageContext.Sleep)
        {
            return "sleep";
        }

        if (usage == UsageContext.Exertion)
        {
            return "exertion";
        }

        return null;
    }

    private static string TrimTrailingZeros(decimal value)
    {
        if (value % 1 == 0)
        {
            return ((int)value).ToString(CultureInfo.InvariantCulture);
        }

        return value.ToString(CultureInfo.InvariantCulture);
    }
}