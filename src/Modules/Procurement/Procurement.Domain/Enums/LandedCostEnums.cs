namespace Procurement.Domain.Enums;

public enum LandedCostStatus
{
    Draft,
    Posted,
    Cancelled
}

public enum LandedCostType
{
    Freight,
    CustomsDuty,
    Insurance,
    Handling,
    Inspection,
    Storage,
    Other
}

/// <summary>
/// How a landed cost component is split across received goods lines.
/// Aligned with Odoo landed cost split method, Oracle allocation basis.
/// </summary>
public enum LandedCostAllocationMethod
{
    ByQuantity,
    ByValue,
    ByWeight,
    ByVolume,
    Equal
}
