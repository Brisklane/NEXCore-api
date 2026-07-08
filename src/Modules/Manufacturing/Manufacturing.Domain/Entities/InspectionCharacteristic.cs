using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Inspection Characteristic - individual measurement or attribute checked during quality inspection.
/// Supports both quantitative (measured values with tolerances) and qualitative (pass/fail) checks.
/// SAP Equivalent: QM Inspection Characteristic (QP01) | Oracle Equivalent: Quality Collection Plan Element
/// </summary>
public class InspectionCharacteristic : BaseEntity
{
    /// <summary>
    /// Inspection reference
    /// </summary>
    public Guid InspectionId { get; set; }

    /// <summary>
    /// Name of the characteristic being inspected (e.g. Diameter, Weight, Colour, Hardness)
    /// </summary>
    public required string CharacteristicName { get; set; }

    /// <summary>
    /// Type of inspection measurement
    /// </summary>
    public required string InspectionType { get; set; } // Quantitative, Qualitative, Visual

    /// <summary>
    /// Unit of measure for quantitative characteristics (e.g. mm, kg, °C)
    /// </summary>
    public string? UnitOfMeasure { get; set; }

    /// <summary>
    /// Target/nominal value for quantitative characteristics
    /// </summary>
    public decimal? TargetValue { get; set; }

    /// <summary>
    /// Upper tolerance limit (target + upper tolerance = upper spec limit)
    /// </summary>
    public decimal? UpperTolerance { get; set; }

    /// <summary>
    /// Lower tolerance limit (target - lower tolerance = lower spec limit)
    /// </summary>
    public decimal? LowerTolerance { get; set; }

    /// <summary>
    /// Actual measured/recorded value
    /// </summary>
    public decimal? ActualValue { get; set; }

    /// <summary>
    /// Qualitative result for visual/attribute inspections
    /// </summary>
    public string? QualitativeResult { get; set; } // e.g. Acceptable, NotAcceptable, Borderline

    /// <summary>
    /// Final result of this characteristic inspection
    /// </summary>
    public required string Result { get; set; } = "Pending"; // Pending, Pass, Fail, Rework, ConditionalPass

    /// <summary>
    /// Indicates if this characteristic is mandatory / critical
    /// </summary>
    public bool IsCritical { get; set; } = false;

    /// <summary>
    /// Number of samples inspected for this characteristic
    /// </summary>
    public int? SampleSize { get; set; }

    /// <summary>
    /// Remarks and findings
    /// </summary>
    public string? Remarks { get; set; }

    // Navigation properties
    public Inspection? Inspection { get; set; }
}
