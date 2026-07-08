using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Inventory.Infrastructure.Events;

/// <summary>
/// Handles stock deduction events published by the Sales module.
///
/// TWO triggers:
///   PosTransactionCompletedEvent  → POS immediate sale (walk-in / Android / WPF POS)
///   DeliveryPostedEvent           → Delivery shipped (online order / B2B)
///
/// For each product line the handler:
///   1. Finds or creates the InventoryDocument (Issue type)
///   2. Creates an InventoryDocumentLine per product (variant-aware)
///   3. Creates an InventoryTransaction (TransactionType = "OUT", Quantity = negative)
///   4. Updates InventoryBalance via the shared <see cref="IInventoryBalanceService"/> so the
///      balance key (Company + Warehouse + Item + Variant + Bin) and moving-average costing match
///      the inbound (GRN / adjustment) paths exactly.
/// </summary>
public class SalesStockDeductionHandler :
    IEventHandler<PosTransactionCompletedEvent>,
    IEventHandler<DeliveryPostedEvent>
{
    private readonly InventoryDbContext _ctx;
    private readonly IInventoryBalanceService _balances;
    private readonly ILogger<SalesStockDeductionHandler> _logger;

    public SalesStockDeductionHandler(
        InventoryDbContext ctx,
        IInventoryBalanceService balances,
        ILogger<SalesStockDeductionHandler> logger)
    {
        _ctx      = ctx;
        _balances = balances;
        _logger   = logger;
    }

    // ── POS immediate sale ──────────────────────────────────────────────────

    public async Task HandleAsync(
        PosTransactionCompletedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Stock deduction: POS Transaction {TxId} — {LineCount} line(s)",
            domainEvent.PosTransactionId, domainEvent.Lines.Count);

        await DeductStockAsync(
            referenceId:    domainEvent.PosTransactionId,
            referenceType:  "POSSale",
            documentNumber: $"POS-{domainEvent.PosTransactionId:N}",
            warehouseId:    domainEvent.WarehouseId,
            companyId:      domainEvent.CompanyId,
            branchId:       domainEvent.BranchId,
            businessUnitId: domainEvent.BusinessUnitId,
            userId:         domainEvent.CreatedByUserId,
            lines:          domainEvent.Lines,
            cancellationToken);
    }

    // ── Delivery shipment ───────────────────────────────────────────────────

    public async Task HandleAsync(
        DeliveryPostedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Stock deduction: Delivery {DeliveryId} (SO {SoId}) — {LineCount} line(s)",
            domainEvent.DeliveryId, domainEvent.SalesOrderId, domainEvent.Lines.Count);

        await DeductStockAsync(
            referenceId:    domainEvent.DeliveryId,
            referenceType:  "SalesDelivery",
            documentNumber: $"DN-{domainEvent.DeliveryId:N}",
            warehouseId:    domainEvent.WarehouseId,
            companyId:      domainEvent.CompanyId,
            branchId:       domainEvent.BranchId,
            businessUnitId: domainEvent.BusinessUnitId,
            userId:         domainEvent.CreatedByUserId,
            lines:          domainEvent.Lines,
            cancellationToken);
    }

    // ── Core deduction logic ────────────────────────────────────────────────

    private async Task DeductStockAsync(
        Guid referenceId,
        string referenceType,
        string documentNumber,
        Guid warehouseId,
        Guid companyId,
        Guid branchId,
        Guid businessUnitId,
        Guid userId,
        IReadOnlyList<StockDeductionLine> lines,
        CancellationToken ct)
    {
        // Company-wide fallback unit, used when a line carries no UoM and the item has no base unit.
        // Note: the seeded "each" unit has Code "PCS" (there is no "EA" unit), so a hardcoded "EA"
        // lookup returned null and every POS line — which arrives with no UoM — was being skipped,
        // silently leaving stock undeducted. Prefer EA/PCS, then fall back to ANY company unit.
        var defaultUnit = await _ctx.Units
            .FirstOrDefaultAsync(u => u.CompanyId == companyId
                                   && (u.Code == "EA" || u.Code == "PCS"), ct)
            ?? await _ctx.Units.FirstOrDefaultAsync(u => u.CompanyId == companyId, ct);

        // Create one InventoryDocument per deduction event
        var document = new InventoryDocument
        {
            Id             = Guid.NewGuid(),
            CompanyId      = companyId,
            BranchId       = branchId,
            BusinessUnitId = businessUnitId,
            CreatedByUserId = userId,
            DocumentNumber = documentNumber,
            DocumentType   = "Issue",
            DocumentDate   = DateTime.UtcNow,
            Status         = "Posted",
            ReferenceId    = referenceId,
            ReferenceType  = referenceType,
            FromWarehouseId = warehouseId,
        };
        _ctx.InventoryDocuments.Add(document);

        var lineNumber = 0;
        var deducted = 0;
        foreach (var line in lines)
        {
            lineNumber++;
            var resolvedWarehouseId = line.WarehouseId ?? warehouseId;

            // The POS terminal does not send a per-line warehouse and the POS store's default
            // warehouse is optional, so a sale can arrive with no warehouse at all. Rather than
            // silently skip the deduction (leaving on-hand untouched), fall back to the warehouse
            // where this item actually holds stock — that is where the sale physically draws from.
            if (resolvedWarehouseId == Guid.Empty)
                resolvedWarehouseId = await ResolveFallbackWarehouseAsync(companyId, line.ProductId, line.VariantId, ct);

            // Guard: still no warehouse (item has no stock anywhere and the company has no warehouse).
            // Skipping avoids a phantom negative balance at Guid.Empty that no read path can surface.
            if (resolvedWarehouseId == Guid.Empty)
            {
                _logger.LogError(
                    "Stock deduction skipped for Product {ProductId} ({ProductCode}) on {RefType} {RefId}: " +
                    "no warehouse resolved (line and event warehouse are both empty, and no fallback warehouse " +
                    "could be found). Configure the POS store's default warehouse or set the order line warehouse.",
                    line.ProductId, line.ProductCode, referenceType, referenceId);
                continue;
            }

            // Resolve unit — match the line UoM, else the item's own base unit (POS lines arrive with
            // no UoM), else the company-wide default. Only skip if the company has no units at all.
            var unit = string.IsNullOrEmpty(line.UnitOfMeasure)
                ? null
                : await _ctx.Units.FirstOrDefaultAsync(
                    u => u.CompanyId == companyId && u.Code == line.UnitOfMeasure, ct);

            if (unit is null)
            {
                var baseUnitId = await _ctx.Items
                    .Where(i => i.Id == line.ProductId)
                    .Select(i => i.BaseUnitId)
                    .FirstOrDefaultAsync(ct);
                if (baseUnitId != Guid.Empty)
                    unit = await _ctx.Units.FirstOrDefaultAsync(u => u.Id == baseUnitId, ct);
            }

            unit ??= defaultUnit;

            if (unit is null)
            {
                _logger.LogWarning(
                    "No unit found for UoM '{UoM}' (CompanyId:{CompanyId}) and item has no base unit. " +
                    "Skipping line for Product:{ProductId}",
                    line.UnitOfMeasure, companyId, line.ProductId);
                continue;
            }

            var docLine = new InventoryDocumentLine
            {
                Id             = Guid.NewGuid(),
                CompanyId      = companyId,
                BranchId       = branchId,
                BusinessUnitId = businessUnitId,
                CreatedByUserId = userId,
                DocumentId     = document.Id,
                LineNumber     = lineNumber,
                ItemId         = line.ProductId,
                WarehouseId    = resolvedWarehouseId,
                BinId          = line.BinId,
                VariantId      = line.VariantId,
                UnitId         = unit.Id,
                Quantity       = line.Quantity,
                UnitCost       = line.UnitCost,
                TotalCost      = line.Quantity * line.UnitCost,
            };
            _ctx.InventoryDocumentLines.Add(docLine);

            // Stock ledger entry — negative quantity = outbound
            var transaction = new InventoryTransaction
            {
                Id              = Guid.NewGuid(),
                CompanyId       = companyId,
                BranchId        = branchId,
                BusinessUnitId  = businessUnitId,
                CreatedByUserId = userId,
                ItemId          = line.ProductId,
                WarehouseId     = resolvedWarehouseId,
                BinId           = line.BinId,
                VariantId       = line.VariantId,
                UnitId          = unit.Id,
                TransactionType = "OUT",
                Quantity        = -line.Quantity,   // negative = deduction
                UnitCost        = line.UnitCost,
                TotalCost       = line.Quantity * line.UnitCost,
                DocumentId      = document.Id,
                DocumentLineId  = docLine.Id,
                TransactionDate = DateTime.UtcNow,
            };
            _ctx.InventoryTransactions.Add(transaction);

            // Update InventoryBalance through the shared service (same keying + costing as inbound).
            // Also release the reservation this issue fulfills (ReservationDelta = -qty). For a POS sale
            // nothing was reserved, so the clamp at ≥ 0 leaves Reserved untouched; for a delivery the
            // reservation made at order-confirm is released here as the goods physically ship.
            await _balances.ApplyMovementAsync(new StockMovement(
                ItemId:         line.ProductId,
                WarehouseId:    resolvedWarehouseId,
                BinId:          line.BinId,
                VariantId:      line.VariantId,
                SignedQuantity: -line.Quantity,     // negative = issue
                UnitCost:       line.UnitCost,
                CompanyId:      companyId,
                BranchId:       branchId,
                BusinessUnitId: businessUnitId,
                CreatedByUserId: userId,
                ReservationDelta: -line.Quantity), ct);

            deducted++;
        }

        await _ctx.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Stock deduction committed: Document {DocNumber} — {Count}/{Total} line(s) deducted",
            documentNumber, deducted, lines.Count);
    }

    /// <summary>
    /// Resolves a warehouse to deduct from when the sale carried none (no per-line warehouse and
    /// the POS store has no default warehouse configured).
    ///
    /// Preference order:
    ///   1. The warehouse where this item/variant currently holds the most on-hand stock — the sale
    ///      physically draws from wherever the goods actually are.
    ///   2. Any warehouse that has a balance row for the item (even if zero/negative).
    ///   3. The company's Retail warehouse, then Main, then any active warehouse — a last resort so a
    ///      brand-new item with no balance yet still books the issue against a real location.
    /// Returns <see cref="Guid.Empty"/> only when the company has no warehouse at all.
    /// </summary>
    private async Task<Guid> ResolveFallbackWarehouseAsync(
        Guid companyId, Guid itemId, Guid? variantId, CancellationToken ct)
    {
        // 1 + 2: warehouse(s) where the item already has a balance, richest stock first.
        var stockedWarehouseId = await _ctx.InventoryBalances
            .Where(b => b.CompanyId == companyId
                     && b.ItemId == itemId
                     && b.VariantId == variantId)
            .OrderByDescending(b => b.QuantityOnHand)
            .Select(b => b.WarehouseId)
            .FirstOrDefaultAsync(ct);

        if (stockedWarehouseId != Guid.Empty)
            return stockedWarehouseId;

        // 3: no balance anywhere — pick a real warehouse for the company (Retail → Main → any).
        var warehouses = await _ctx.Warehouses
            .Where(w => w.CompanyId == companyId && w.IsActive)
            .Select(w => new { w.Id, w.WarehouseType })
            .ToListAsync(ct);

        var chosen = warehouses.FirstOrDefault(w => w.WarehouseType == "Retail")
                  ?? warehouses.FirstOrDefault(w => w.WarehouseType == "Main")
                  ?? warehouses.FirstOrDefault();

        return chosen?.Id ?? Guid.Empty;
    }
}
