using Microsoft.EntityFrameworkCore;
using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Inv = Inventory.Domain.Constants;

namespace Inventory.Infrastructure.Services;

/// <summary>
/// Applies unit-level tracking (Serial / Lot) side-effects when an inventory document is posted.
/// <para>
/// The quantity ledger (<see cref="InventoryTransaction"/> / <see cref="InventoryBalance"/>) still owns
/// the numeric on-hand; this service adds/advances the <b>identity</b> records:
/// <see cref="ItemSerial"/> (one per physical unit) and <see cref="ItemBatch"/> + <see cref="ItemLotStock"/>
/// (per batch/lot), plus <see cref="ItemSerialHistory"/> audit rows, and stamps the resulting
/// transactions with their serial/batch FKs.
/// </para>
/// Works on the caller's scoped <see cref="InventoryDbContext"/> (tracked, NOT saved) so the whole
/// posting commits atomically in the caller's single SaveChanges.
/// </summary>
public interface IItemTrackingService
{
    /// <summary>
    /// For a posted document line, creates the ledger transaction(s) and any Serial/Lot registry
    /// changes, and adds them all to the tracked context. Returns the created transactions.
    /// <list type="bullet">
    /// <item>Serial item with captured serials → one qty-±1 transaction per serial + ItemSerial changes.</item>
    /// <item>Lot item with a batch number → one transaction linked to the (created/updated) ItemBatch.</item>
    /// <item>Otherwise → a single plain transaction for the whole line (legacy behaviour).</item>
    /// </list>
    /// Does NOT call SaveChanges and does NOT touch InventoryBalance (the caller applies the movement).
    /// </summary>
    Task<IReadOnlyList<InventoryTransaction>> ApplyPostingAsync(
        InventoryDocument document,
        InventoryDocumentLine line,
        string trackingType,
        decimal signedQty,
        string transactionType,
        DateTime postingDate,
        Guid userId,
        CancellationToken ct = default);
}

public sealed class ItemTrackingService : IItemTrackingService
{
    private readonly InventoryDbContext _context;

    public ItemTrackingService(InventoryDbContext context) => _context = context;

    public async Task<IReadOnlyList<InventoryTransaction>> ApplyPostingAsync(
        InventoryDocument document,
        InventoryDocumentLine line,
        string trackingType,
        decimal signedQty,
        string transactionType,
        DateTime postingDate,
        Guid userId,
        CancellationToken ct = default)
    {
        var isInbound = signedQty >= 0;
        var qtyAbs = Math.Abs(signedQty);
        var txns = new List<InventoryTransaction>();

        // ── Serial-tracked: one identity + one qty-±1 transaction per captured unit ──
        if (trackingType == Inv.ItemTrackingType.Serial && line.LineSerials.Count > 0)
        {
            foreach (var ls in line.LineSerials)
            {
                var unitSigned = isInbound ? 1m : -1m;
                var txn = BuildTransaction(document, line, unitSigned, transactionType, postingDate, userId);

                if (isInbound)
                {
                    var serial = new ItemSerial
                    {
                        CompanyId = document.CompanyId,
                        BranchId = document.BranchId,
                        BusinessUnitId = document.BusinessUnitId,
                        CreatedByUserId = userId,
                        CreatedAt = postingDate,
                        ItemId = line.ItemId,
                        VariantId = line.VariantId,
                        SerialNumber = ls.SerialNumber,
                        Imei = ls.Imei,
                        Imei2 = ls.Imei2,
                        MacAddress = ls.MacAddress,
                        Status = Inv.SerialStatus.InStock,
                        WarehouseId = line.WarehouseId,
                        BinId = line.BinId,
                        UnitCost = line.UnitCost,
                        ReceiptDocumentId = document.Id,
                        ReceiptDate = postingDate,
                    };
                    _context.Set<ItemSerial>().Add(serial);
                    txn.ItemSerialId = serial.Id;
                    txn.SerialNumber = serial.SerialNumber;
                    serial.ReceiptTransactionId = txn.Id;
                    AddHistory(document, userId, serial.Id, Inv.SerialEventType.Received, null,
                        Inv.SerialStatus.InStock, line.WarehouseId, postingDate);
                }
                else
                {
                    var serial = await FindSerialAsync(document, line.ItemId, ls.SerialNumber, ct);
                    txn.SerialNumber = ls.SerialNumber;
                    if (serial != null)
                    {
                        var from = serial.Status;
                        serial.Status = Inv.SerialStatus.Sold;
                        serial.SoldDocumentId = document.Id;
                        serial.SoldDate = postingDate;
                        txn.ItemSerialId = serial.Id;
                        AddHistory(document, userId, serial.Id, Inv.SerialEventType.Sold, from,
                            Inv.SerialStatus.Sold, line.WarehouseId, postingDate);
                    }
                }

                _context.Set<InventoryTransaction>().Add(txn);
                txns.Add(txn);
            }
            return txns;
        }

        // ── Lot-tracked: create/advance the batch + per-warehouse lot stock ──
        if (trackingType == Inv.ItemTrackingType.Lot && !string.IsNullOrWhiteSpace(line.BatchNumber))
        {
            var txn = BuildTransaction(document, line, signedQty, transactionType, postingDate, userId);
            var batchNumber = line.BatchNumber!.Trim();

            var batch = await FindBatchAsync(document, line.ItemId, batchNumber, ct);
            if (batch == null)
            {
                batch = new ItemBatch
                {
                    CompanyId = document.CompanyId,
                    BranchId = document.BranchId,
                    BusinessUnitId = document.BusinessUnitId,
                    CreatedByUserId = userId,
                    CreatedAt = postingDate,
                    ItemId = line.ItemId,
                    VariantId = line.VariantId,
                    BatchNumber = batchNumber,
                    ManufactureDate = line.ManufactureDate,
                    ExpiryDate = line.ExpiryDate,
                    UnitCost = line.UnitCost,
                    Status = Inv.BatchStatus.Active,
                };
                _context.Set<ItemBatch>().Add(batch);
            }

            if (isInbound)
            {
                batch.ReceivedQuantity += qtyAbs;
                batch.RemainingQuantity += qtyAbs;
                await AdjustLotStockAsync(document, batch.Id, line.WarehouseId, line.BinId, qtyAbs, userId, postingDate, ct);
            }
            else
            {
                batch.RemainingQuantity -= qtyAbs;
                if (batch.RemainingQuantity <= 0)
                {
                    batch.RemainingQuantity = 0;
                    batch.Status = Inv.BatchStatus.Consumed;
                }
                await AdjustLotStockAsync(document, batch.Id, line.WarehouseId, line.BinId, -qtyAbs, userId, postingDate, ct);
            }

            txn.ItemBatchId = batch.Id;
            txn.BatchNumber = batch.BatchNumber;
            txn.ExpiryDate = batch.ExpiryDate;

            _context.Set<InventoryTransaction>().Add(txn);
            txns.Add(txn);
            return txns;
        }

        // ── Untracked (or tracked but nothing captured): single plain transaction ──
        var plain = BuildTransaction(document, line, signedQty, transactionType, postingDate, userId);
        _context.Set<InventoryTransaction>().Add(plain);
        txns.Add(plain);
        return txns;
    }

    private static InventoryTransaction BuildTransaction(
        InventoryDocument document, InventoryDocumentLine line, decimal signedQty,
        string transactionType, DateTime postingDate, Guid userId) => new()
    {
        CompanyId = document.CompanyId,
        BranchId = document.BranchId,
        BusinessUnitId = document.BusinessUnitId,
        CreatedByUserId = userId,
        CreatedAt = postingDate,
        ItemId = line.ItemId,
        WarehouseId = line.WarehouseId,
        BinId = line.BinId,
        VariantId = line.VariantId,
        TransactionType = transactionType,
        Quantity = signedQty,
        UnitId = line.UnitId,
        UnitCost = line.UnitCost,
        TotalCost = signedQty * line.UnitCost,
        DocumentId = document.Id,
        DocumentLineId = line.Id,
        TransactionDate = postingDate,
    };

    private void AddHistory(
        InventoryDocument document, Guid userId, Guid serialId, string eventType,
        string? fromStatus, string? toStatus, Guid? warehouseId, DateTime when) =>
        _context.Set<ItemSerialHistory>().Add(new ItemSerialHistory
        {
            CompanyId = document.CompanyId,
            BranchId = document.BranchId,
            BusinessUnitId = document.BusinessUnitId,
            CreatedByUserId = userId,
            CreatedAt = when,
            ItemSerialId = serialId,
            EventType = eventType,
            FromStatus = fromStatus,
            ToStatus = toStatus,
            WarehouseId = warehouseId,
            DocumentId = document.Id,
            EventDate = when,
        });

    private async Task<ItemSerial?> FindSerialAsync(InventoryDocument d, Guid itemId, string serial, CancellationToken ct)
        => _context.Set<ItemSerial>().Local
               .FirstOrDefault(s => s.ItemId == itemId && s.SerialNumber == serial)
           ?? await _context.Set<ItemSerial>()
               .FirstOrDefaultAsync(s => s.ItemId == itemId && s.SerialNumber == serial &&
                                         s.CompanyId == d.CompanyId && s.BranchId == d.BranchId &&
                                         s.BusinessUnitId == d.BusinessUnitId && !s.IsDeleted, ct);

    private async Task<ItemBatch?> FindBatchAsync(InventoryDocument d, Guid itemId, string batchNumber, CancellationToken ct)
        => _context.Set<ItemBatch>().Local
               .FirstOrDefault(b => b.ItemId == itemId && b.BatchNumber == batchNumber)
           ?? await _context.Set<ItemBatch>()
               .FirstOrDefaultAsync(b => b.ItemId == itemId && b.BatchNumber == batchNumber &&
                                         b.CompanyId == d.CompanyId && b.BranchId == d.BranchId &&
                                         b.BusinessUnitId == d.BusinessUnitId && !b.IsDeleted, ct);

    private async Task AdjustLotStockAsync(
        InventoryDocument d, Guid batchId, Guid warehouseId, Guid? binId, decimal delta,
        Guid userId, DateTime when, CancellationToken ct)
    {
        var lot = _context.Set<ItemLotStock>().Local
                      .FirstOrDefault(x => x.ItemBatchId == batchId && x.WarehouseId == warehouseId && x.BinId == binId)
                  ?? await _context.Set<ItemLotStock>()
                      .FirstOrDefaultAsync(x => x.ItemBatchId == batchId && x.WarehouseId == warehouseId &&
                                                x.BinId == binId && !x.IsDeleted, ct);
        if (lot == null)
        {
            lot = new ItemLotStock
            {
                CompanyId = d.CompanyId,
                BranchId = d.BranchId,
                BusinessUnitId = d.BusinessUnitId,
                CreatedByUserId = userId,
                CreatedAt = when,
                ItemBatchId = batchId,
                WarehouseId = warehouseId,
                BinId = binId,
                Quantity = 0,
            };
            _context.Set<ItemLotStock>().Add(lot);
        }
        lot.Quantity += delta;
        if (lot.Quantity < 0) lot.Quantity = 0;
    }
}
