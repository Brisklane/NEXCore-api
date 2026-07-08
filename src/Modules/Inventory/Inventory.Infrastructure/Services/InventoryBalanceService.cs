using Inventory.Domain.Constants;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Services;

/// <inheritdoc cref="IInventoryBalanceService"/>
public sealed class InventoryBalanceService : IInventoryBalanceService
{
    private readonly InventoryDbContext _ctx;
    private readonly ILogger<InventoryBalanceService> _logger;

    public InventoryBalanceService(InventoryDbContext ctx, ILogger<InventoryBalanceService> logger)
    {
        _ctx = ctx;
        _logger = logger;
    }

    public async Task<InventoryBalance?> FindBalanceAsync(
        Guid itemId, Guid warehouseId, Guid? binId, Guid? variantId, Guid companyId, CancellationToken ct = default)
    {
        // Check the change tracker first so repeated movements to the same key within ONE unit of work
        // (e.g. the same product on two lines of a single sale/document) accumulate on one balance row
        // instead of each inserting a duplicate — a LINQ query does not see Added-but-unsaved rows, and
        // two inserts with the same key would violate the unique index on SaveChanges.
        var tracked = _ctx.InventoryBalances.Local.FirstOrDefault(b =>
            b.ItemId == itemId
            && b.WarehouseId == warehouseId
            && b.CompanyId == companyId
            && b.BinId == binId
            && b.VariantId == variantId);
        if (tracked is not null)
            return tracked;

        // Explicit null-aware bin/variant matching — null means "the warehouse/item-level row",
        // NOT "any row". (The previous repository predicate `binId == null || b.BinId == binId`
        // collapsed to TRUE for a null bin and matched an arbitrary row.) Written as separate
        // Where clauses so it translates correctly on both SQL Server and the in-memory provider.
        var query = _ctx.InventoryBalances
            .Where(b => b.ItemId == itemId
                     && b.WarehouseId == warehouseId
                     && b.CompanyId == companyId);

        query = binId.HasValue
            ? query.Where(b => b.BinId == binId)
            : query.Where(b => b.BinId == null);

        query = variantId.HasValue
            ? query.Where(b => b.VariantId == variantId)
            : query.Where(b => b.VariantId == null);

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<decimal?> GetAvailableAsync(
        Guid itemId, Guid warehouseId, Guid? variantId, Guid companyId, CancellationToken ct = default)
    {
        var query = _ctx.InventoryBalances
            .Where(b => b.ItemId == itemId
                     && b.WarehouseId == warehouseId
                     && b.CompanyId == companyId);

        query = variantId.HasValue
            ? query.Where(b => b.VariantId == variantId)
            : query.Where(b => b.VariantId == null);

        // Sum available-to-promise (OnHand − Reserved) across bins. Materialize so we can distinguish
        // "no row at all" (untracked → null) from a genuine zero — a SumAsync over an empty set would
        // collapse both to 0 and wrongly block service / non-inventory / never-received items.
        var quantities = await query.Select(b => b.QuantityAvailable).ToListAsync(ct);
        return quantities.Count == 0 ? null : quantities.Sum();
    }

    public async Task<InventoryBalance> ApplyMovementAsync(StockMovement m, CancellationToken ct = default)
    {
        var balance = await FindBalanceAsync(m.ItemId, m.WarehouseId, m.BinId, m.VariantId, m.CompanyId, ct);

        if (balance is null)
        {
            // No balance row yet. For a receipt this is the opening row; for an issue it is a
            // shortage (selling stock we have no record of) — created negative and flagged.
            balance = new InventoryBalance
            {
                Id              = Guid.NewGuid(),
                CompanyId       = m.CompanyId,
                BranchId        = m.BranchId,
                BusinessUnitId  = m.BusinessUnitId,
                CreatedByUserId = m.CreatedByUserId,
                ItemId          = m.ItemId,
                WarehouseId     = m.WarehouseId,
                BinId           = m.BinId,
                VariantId       = m.VariantId,
                QuantityOnHand  = m.SignedQuantity,
                QuantityReserved = Math.Max(0m, m.ReservationDelta),
                QuantityAvailable = m.SignedQuantity - Math.Max(0m, m.ReservationDelta),
                AverageCost     = m.SignedQuantity != 0m ? m.UnitCost : 0m,
                TotalValue      = m.SignedQuantity * (m.SignedQuantity != 0m ? m.UnitCost : 0m),
                CostingMethod   = CostingMethod.MovingAverage,
                LastTransactionDate = m.OccurredAtUtc ?? DateTime.UtcNow,
            };
            _ctx.InventoryBalances.Add(balance);

            if (m.SignedQuantity < 0m)
                _logger.LogWarning(
                    "Stock shortage: no balance for Item {ItemId} Variant {VariantId} in Warehouse {WarehouseId} " +
                    "(Company {CompanyId}); created row at {Qty} (negative on-hand).",
                    m.ItemId, m.VariantId, m.WarehouseId, m.CompanyId, m.SignedQuantity);

            return balance;
        }

        if (m.SignedQuantity >= 0m)
        {
            // Receipt / positive adjustment → moving-average recompute using the incoming unit cost.
            var newQty   = balance.QuantityOnHand + m.SignedQuantity;
            var newValue = (balance.QuantityOnHand * balance.AverageCost) + (m.SignedQuantity * m.UnitCost);
            balance.AverageCost    = newQty > 0m ? newValue / newQty : balance.AverageCost;
            balance.QuantityOnHand = newQty;
            balance.TotalValue     = balance.QuantityOnHand * balance.AverageCost;
        }
        else
        {
            // Issue / sale / negative adjustment → remove at the current average cost (unchanged).
            balance.QuantityOnHand += m.SignedQuantity;   // SignedQuantity is negative
            balance.TotalValue      = balance.QuantityOnHand * balance.AverageCost;

            if (balance.QuantityOnHand < 0m)
                _logger.LogWarning(
                    "Negative stock: Item {ItemId} Variant {VariantId} in Warehouse {WarehouseId} " +
                    "(Company {CompanyId}) is now {Qty} on-hand after issuing {Issued}.",
                    m.ItemId, m.VariantId, m.WarehouseId, m.CompanyId, balance.QuantityOnHand, -m.SignedQuantity);
        }

        balance.QuantityReserved  = Math.Max(0m, balance.QuantityReserved + m.ReservationDelta);
        balance.QuantityAvailable = balance.QuantityOnHand - balance.QuantityReserved;
        balance.LastTransactionDate = m.OccurredAtUtc ?? DateTime.UtcNow;
        balance.UpdatedAt        = DateTime.UtcNow;
        balance.UpdatedByUserId  = m.CreatedByUserId;
        return balance;
    }
}
