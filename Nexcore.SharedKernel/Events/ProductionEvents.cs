namespace Nexcore.SharedKernel.Events;

// ── Manufacturing → Inventory integration ───────────────────────────────────────

/// <summary>
/// Published by Manufacturing when a production run is completed (e.g. the POS "Produce"
/// express flow, or a normal production order finishing). Inventory.Infrastructure handles
/// this to move physical stock in a single posting:
///
///   ConsumedMaterials → OUT  (raw materials / BOM components leave stock — backflush)
///   ProducedGoods     → IN   (finished goods enter stock)
///
/// Mirrors the Sales <see cref="PosTransactionCompletedEvent"/> deduction path and the
/// Procurement <see cref="GoodsReceiptPostedEvent"/> receipt path, reusing
/// <see cref="StockDeductionLine"/> for both directions so the same warehouse/unit
/// resolution and moving-average costing apply.
/// </summary>
public class ProductionCompletedEvent
{
    public Guid ProductionOrderId { get; init; }
    public string OrderNumber { get; init; } = string.Empty;
    public Guid CompanyId { get; init; }
    public Guid BranchId { get; init; }
    public Guid BusinessUnitId { get; init; }
    public Guid CreatedByUserId { get; init; }

    /// <summary>Warehouse used when a line carries none (source for issues, destination for receipts).</summary>
    public Guid? WarehouseId { get; init; }

    /// <summary>
    /// Non-material conversion cost per finished unit (labour + machine + overhead) sourced from the
    /// product's Standard Cost. Added to the rolled-up material cost when valuing the finished goods,
    /// so recipe costs like "packaging / labour / other cost" reach COGS without being fake inventory.
    /// </summary>
    public decimal ConversionUnitCost { get; init; }

    /// <summary>Raw materials / BOM components consumed by the run — deducted from stock.</summary>
    public IReadOnlyList<StockDeductionLine> ConsumedMaterials { get; init; } = [];

    /// <summary>Finished goods produced by the run — added to stock.</summary>
    public IReadOnlyList<StockDeductionLine> ProducedGoods { get; init; } = [];

    public DateTime CompletedAt { get; init; } = DateTime.UtcNow;
}
