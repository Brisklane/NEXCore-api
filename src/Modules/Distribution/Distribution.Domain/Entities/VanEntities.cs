using Distribution.Domain.Enums;
using Nexcore.SharedKernel;

namespace Distribution.Domain.Entities;

/// <summary>
/// A van, modelled as a moving warehouse that is also a till.
///
/// This is the design decision the whole van-sales feature rests on. A van is *not* a status flag
/// on an order — it holds stock, that stock has batches and expiry dates, it can be transferred
/// to another van, and it has to be counted at the end of the day. Anything less and van stock
/// becomes a spreadsheet nobody trusts. <see cref="WarehouseId"/> registers it in the Inventory
/// ledger so there is still exactly one stock ledger for the company.
/// </summary>
public class VanUnit : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    /// <summary>Inventory warehouse representing this van's stock. Every movement posts against it.</summary>
    public Guid? WarehouseId { get; set; }

    /// <summary>Warehouse it loads from and returns to.</summary>
    public Guid? HomeWarehouseId { get; set; }

    public Guid? VehicleId { get; set; }
    public Vehicle? Vehicle { get; set; }

    public Guid? FieldRepId { get; set; }
    public FieldRep? FieldRep { get; set; }

    public Guid? RouteId { get; set; }

    public decimal CapacityWeightKg { get; set; }
    public decimal CapacityVolumeM3 { get; set; }

    /// <summary>Refrigerated vans carry a cold-chain range and an excursion log.</summary>
    public bool IsRefrigerated { get; set; }
    public decimal? MinSafeCelsius { get; set; }
    public decimal? MaxSafeCelsius { get; set; }

    /// <summary>A van that does not return to base each night keeps stock across days.</summary>
    public bool IsMultiDay { get; set; }

    public DateTime? LastLoadedAt { get; set; }
    public DateTime? LastSettledAt { get; set; }

    public string? Note { get; set; }

    public ICollection<VanStockBalance> Balances { get; set; } = [];
}

/// <summary>
/// A load sheet: what goes onto the van, and what actually went on.
///
/// Suggested, approved, picked and loaded are four different quantities and conflating them is
/// how a van starts the day already short. Each is carried on the line.
/// </summary>
public class VanLoadSheet : BaseEntity
{
    public string LoadNumber { get; set; } = string.Empty;

    public Guid VanUnitId { get; set; }
    public VanUnit? VanUnit { get; set; }

    public Guid? RouteId { get; set; }
    public Guid? FieldRepId { get; set; }
    public Guid? FieldDayId { get; set; }
    public Guid? SourceWarehouseId { get; set; }

    public DateTime LoadDate { get; set; }
    public VanLoadStatus Status { get; set; } = VanLoadStatus.Draft;

    /// <summary>True when the lines were proposed by the suggestion engine rather than typed.</summary>
    public bool IsSuggested { get; set; }

    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public DateTime? LoadedAt { get; set; }
    public Guid? LoadedByUserId { get; set; }
    public string? RejectionReason { get; set; }

    public decimal TotalCostValue { get; set; }
    public decimal TotalSaleValue { get; set; }
    public decimal TotalWeightKg { get; set; }

    /// <summary>Lines where loaded quantity did not match picked quantity.</summary>
    public int VarianceLineCount { get; set; }

    public string? Note { get; set; }

    public ICollection<VanLoadLine> Lines { get; set; } = [];
}

/// <summary>One SKU on a load sheet, batch-specific because expiry travels with the goods.</summary>
public class VanLoadLine : BaseEntity
{
    public Guid LoadSheetId { get; set; }
    public VanLoadSheet? LoadSheet { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }

    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public string Uom { get; set; } = "PCS";

    /// <summary>Pieces per <see cref="Uom"/>, so a "case" line reconciles against a piece ledger.</summary>
    public decimal UomFactor { get; set; } = 1;

    public decimal SuggestedQuantity { get; set; }
    public decimal RequestedQuantity { get; set; }
    public decimal ApprovedQuantity { get; set; }
    public decimal PickedQuantity { get; set; }
    public decimal LoadedQuantity { get; set; }

    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineValue { get; set; }

    public VanCompartment Compartment { get; set; } = VanCompartment.Sellable;

    /// <summary>Mandatory when loaded differs from picked.</summary>
    public string? VarianceReason { get; set; }

    public int DisplayOrder { get; set; }
}

/// <summary>
/// What is on a van right now, per item, per batch, per compartment.
///
/// The compartment is part of the key on purpose: goods taken back at a door land in a returns
/// compartment and must not fall back into sellable stock by accident. That single distinction
/// is what stops a damaged case being resold at the next stop.
/// </summary>
public class VanStockBalance : BaseEntity
{
    public Guid VanUnitId { get; set; }
    public VanUnit? VanUnit { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? ItemCode { get; set; }

    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public VanCompartment Compartment { get; set; } = VanCompartment.Sellable;

    public string Uom { get; set; } = "PCS";
    public decimal Quantity { get; set; }

    /// <summary>Reserved against confirmed-but-not-yet-delivered lines on this trip.</summary>
    public decimal ReservedQuantity { get; set; }

    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }

    public DateTime? LastMovementAt { get; set; }
}

/// <summary>
/// Every quantity change on a van, as an append-only ledger.
///
/// The balance above is a projection of this. Keeping the movements means a settlement variance
/// can always be walked back to the transaction that caused it, which is the difference between
/// "the van is short forty units" and "the van is short forty units because of these two sales".
/// </summary>
public class VanStockMovement : BaseEntity
{
    public Guid VanUnitId { get; set; }
    public VanUnit? VanUnit { get; set; }

    public Guid? FieldDayId { get; set; }
    public Guid? VisitId { get; set; }

    public VanMovementKind Kind { get; set; }
    public DateTime OccurredAt { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public VanCompartment Compartment { get; set; } = VanCompartment.Sellable;

    public string Uom { get; set; } = "PCS";

    /// <summary>Signed: positive puts stock on the van, negative takes it off.</summary>
    public decimal Quantity { get; set; }

    public decimal UnitCost { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Value { get; set; }

    /// <summary>Balance after this movement, so the ledger reads without a running sum.</summary>
    public decimal BalanceAfter { get; set; }

    public Guid? ReferenceId { get; set; }
    public string? ReferenceType { get; set; }
    public string? ReferenceNumber { get; set; }

    public Guid? CounterpartyVanUnitId { get; set; }
    public string? Reason { get; set; }
    public Guid? FieldRepId { get; set; }
}

/// <summary>
/// A spot count of van stock, taken mid-route without closing the day.
///
/// Distinct from the end-of-day load-in: a driver who suspects a shortfall at eleven o'clock
/// should be able to prove it then, not discover it at seven in the evening.
/// </summary>
public class VanCycleCount : BaseEntity
{
    public string CountNumber { get; set; } = string.Empty;

    public Guid VanUnitId { get; set; }
    public VanUnit? VanUnit { get; set; }

    public Guid? FieldDayId { get; set; }
    public Guid? FieldRepId { get; set; }

    public DateTime CountedAt { get; set; }

    /// <summary>True at day end — the full reconciliation rather than a spot check.</summary>
    public bool IsFullCount { get; set; }

    /// <summary>Counter does not see the expected figure until they have entered theirs.</summary>
    public bool IsBlind { get; set; } = true;

    public int LineCount { get; set; }
    public int VarianceLineCount { get; set; }
    public decimal VarianceValue { get; set; }

    public bool IsApproved { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedByUserId { get; set; }
    public string? Note { get; set; }

    public ICollection<VanCycleCountLine> Lines { get; set; } = [];
}

/// <summary>One SKU on a van count. A variance without a reason blocks the count from closing.</summary>
public class VanCycleCountLine : BaseEntity
{
    public Guid CountId { get; set; }
    public VanCycleCount? Count { get; set; }

    public Guid ItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public Guid? BatchId { get; set; }
    public string? BatchNumber { get; set; }
    public DateTime? ExpiryDate { get; set; }

    public VanCompartment Compartment { get; set; } = VanCompartment.Sellable;
    public string Uom { get; set; } = "PCS";

    public decimal ExpectedQuantity { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal VarianceQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal VarianceValue { get; set; }

    /// <summary>Mandatory on any non-zero variance before the count can be approved.</summary>
    public Guid? ReasonCodeId { get; set; }
    public string? ReasonNote { get; set; }

    public bool IsAdjusted { get; set; }
}
