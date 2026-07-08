namespace Manufacturing.Application.DTOs;

public class InspectionDto
{
    public Guid Id { get; set; }
    public Guid ProductionOrderId { get; set; }
    public decimal InspectedQty { get; set; }
    public decimal PassedQty { get; set; }
    public decimal RejectedQty { get; set; }
    public required string Status { get; set; }
    public Guid InspectedById { get; set; }
    public DateTime InspectedAt { get; set; }
    public string? Remarks { get; set; }
    public List<InspectionCharacteristicDto> Characteristics { get; set; } = [];
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateInspectionDto
{
    public Guid ProductionOrderId { get; set; }
    public decimal InspectedQty { get; set; }
    public decimal PassedQty { get; set; }
    public decimal RejectedQty { get; set; }
    public DateTime InspectedAt { get; set; }
    public string? Remarks { get; set; }
    public List<CreateInspectionCharacteristicDto> Characteristics { get; set; } = [];
}

public class UpdateInspectionDto
{
    public decimal? InspectedQty { get; set; }
    public decimal? PassedQty { get; set; }
    public decimal? RejectedQty { get; set; }
    public string? Status { get; set; }
    public string? Remarks { get; set; }
}

// ---- Inspection Characteristic ----
public class InspectionCharacteristicDto
{
    public Guid Id { get; set; }
    public Guid InspectionId { get; set; }
    public required string CharacteristicName { get; set; }
    public required string InspectionType { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? UpperTolerance { get; set; }
    public decimal? LowerTolerance { get; set; }
    public decimal? ActualValue { get; set; }
    public string? QualitativeResult { get; set; }
    public required string Result { get; set; }
    public bool IsCritical { get; set; }
    public int? SampleSize { get; set; }
    public string? Remarks { get; set; }
}

public class CreateInspectionCharacteristicDto
{
    public required string CharacteristicName { get; set; }
    public required string InspectionType { get; set; }
    public string? UnitOfMeasure { get; set; }
    public decimal? TargetValue { get; set; }
    public decimal? UpperTolerance { get; set; }
    public decimal? LowerTolerance { get; set; }
    public decimal? ActualValue { get; set; }
    public string? QualitativeResult { get; set; }
    public string Result { get; set; } = "Pending";
    public bool IsCritical { get; set; } = false;
    public int? SampleSize { get; set; }
    public string? Remarks { get; set; }
}

public class UpdateInspectionCharacteristicDto
{
    public decimal? ActualValue { get; set; }
    public string? QualitativeResult { get; set; }
    public string? Result { get; set; }
    public string? Remarks { get; set; }
}
