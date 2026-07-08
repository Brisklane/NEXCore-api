using Inventory.Domain.Entities;

namespace Inventory.Infrastructure.Services;

/// <summary>
/// A single stock movement to apply to <see cref="InventoryBalance"/>.
/// <para>
/// <see cref="SignedQuantity"/> is signed: positive = receipt/positive adjustment (recomputes the
/// moving-average cost using <see cref="UnitCost"/>); negative = issue/sale/negative adjustment
/// (removes quantity at the balance's current average cost — the unit cost is "locked in" at receipt).
/// </para>
/// </summary>
public sealed record StockMovement(
    Guid ItemId,
    Guid WarehouseId,
    Guid? BinId,
    Guid? VariantId,
    decimal SignedQuantity,
    decimal UnitCost,
    Guid CompanyId,
    Guid BranchId,
    Guid BusinessUnitId,
    Guid CreatedByUserId,
    /// <summary>Document/posting date to stamp on the balance row. Null = now (wall-clock).</summary>
    DateTime? OccurredAtUtc = null,
    /// <summary>
    /// Signed change to QuantityReserved (soft allocation): positive = reserve more,
    /// negative = release. Clamped at ≥ 0. Most movements pass 0. A sale/shipment passes a
    /// negative value to release the reservation it is fulfilling.
    /// </summary>
    decimal ReservationDelta = 0m);

/// <summary>
/// Single source of truth for reading and mutating <see cref="InventoryBalance"/> rows.
///
/// Both the inbound paths (GRN / adjustment posting in InventoryDocumentController) and the outbound
/// path (POS / delivery stock deduction in SalesStockDeductionHandler) go through this service, so the
/// balance KEY (Company + Warehouse + Item + Variant + Bin) and the moving-average costing are applied
/// identically everywhere — eliminating the keying drift that previously let POS deductions miss the
/// real balance and silently create phantom negative rows.
///
/// Mutations operate on the caller's (scoped) <c>InventoryDbContext</c> and are TRACKED but NOT saved —
/// the caller owns the transaction and calls <c>SaveChangesAsync</c> once alongside its document/ledger
/// writes, so the whole posting commits atomically.
/// </summary>
public interface IInventoryBalanceService
{
    /// <summary>
    /// Finds the matching balance (tracked) and applies <paramref name="movement"/> with moving-average
    /// costing, creating the balance row if it does not yet exist. Does NOT call SaveChanges.
    /// A movement that would drive on-hand negative is applied and logged as a shortage (the caller
    /// decides whether to block beforehand — see the POS availability check).
    /// </summary>
    Task<InventoryBalance> ApplyMovementAsync(StockMovement movement, CancellationToken ct = default);

    /// <summary>
    /// Returns the tracked balance row keyed by Company + Warehouse + Item + Variant + Bin, or null.
    /// Bin/Variant use exact null-aware matching (null means the warehouse/item-level row, NOT "any").
    /// </summary>
    Task<InventoryBalance?> FindBalanceAsync(
        Guid itemId, Guid warehouseId, Guid? binId, Guid? variantId, Guid companyId, CancellationToken ct = default);

    /// <summary>
    /// Available-to-promise quantity (On Hand − Reserved) for an item (and optional variant) in a
    /// warehouse, summed across bins — used by the POS stock-availability check so committed/reserved
    /// stock is not sold twice. Read-only.
    /// Returns null when NO balance row exists for the key (the item is not stocked/tracked here, or was
    /// never received) so the caller can tell "untracked → don't enforce" apart from a real zero.
    /// </summary>
    Task<decimal?> GetAvailableAsync(
        Guid itemId, Guid warehouseId, Guid? variantId, Guid companyId, CancellationToken ct = default);
}
