using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Helpers;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Repositories.Interfaces;

namespace Procurement.Infrastructure.Services;

public class GoodsReceiptService : IGoodsReceiptService
{
    private readonly IGoodsReceiptRepository _receipts;
    private readonly IPurchaseOrderRepository _orders;
    private readonly IDocumentSequenceService _sequences;
    private readonly IPurchaseInvoiceService _invoiceService;
    private readonly IEventPublisher _events;
    private readonly ILogger<GoodsReceiptService> _logger;

    public GoodsReceiptService(
        IGoodsReceiptRepository receipts,
        IPurchaseOrderRepository orders,
        IDocumentSequenceService sequences,
        IPurchaseInvoiceService invoiceService,
        IEventPublisher events,
        ILogger<GoodsReceiptService> logger)
    {
        _receipts  = receipts;
        _orders    = orders;
        _sequences = sequences;
        _invoiceService = invoiceService;
        _events    = events;
        _logger    = logger;
    }

    public async Task<PaginatedResponse<GoodsReceiptDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _receipts.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: r =>
                (string.IsNullOrEmpty(search) ||
                    r.ReceiptNumber.ToLower().Contains(search) ||
                    (r.VendorDeliveryNoteNumber != null && r.VendorDeliveryNoteNumber.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)r.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "CreatedAt"),
            include: q => q.Include(r => r.Vendor).Include(r => r.Lines));
        return PaginatedResponse<GoodsReceiptDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<GoodsReceiptDto?> GetByIdAsync(Guid id)
    {
        var r = await _receipts.GetWithLinesAsync(id);
        return r is null ? null : MapToDto(r);
    }

    public async Task<GoodsReceiptDto?> GetByNumberAsync(string receiptNumber)
    {
        var r = await _receipts.GetByNumberAsync(receiptNumber);
        return r is null ? null : MapToDto(r);
    }

    public async Task<List<GoodsReceiptDto>> GetByStatusAsync(GoodsReceiptStatus status)
        => (await _receipts.GetByStatusAsync(status)).Select(MapToDto).ToList();

    public async Task<List<GoodsReceiptDto>> GetByPurchaseOrderAsync(Guid purchaseOrderId)
        => (await _receipts.GetByPurchaseOrderAsync(purchaseOrderId)).Select(MapToDto).ToList();

    public async Task<List<GoodsReceiptDto>> GetByVendorAsync(Guid vendorId)
        => (await _receipts.GetByVendorAsync(vendorId)).Select(MapToDto).ToList();

    public async Task<GoodsReceiptDto> CreateAsync(CreateGoodsReceiptDto dto)
    {
        var order = await _orders.GetWithLinesAsync(dto.PurchaseOrderId)
            ?? throw new KeyNotFoundException($"Purchase Order {dto.PurchaseOrderId} not found");

        var receipt = new GoodsReceipt
        {
            ReceiptNumber             = await _sequences.GetNextNumberAsync(ProcurementDocumentType.GoodsReceipt),
            PurchaseOrderId           = dto.PurchaseOrderId,
            VendorId                  = order.VendorId,
            VendorDeliveryNoteNumber  = dto.VendorDeliveryNoteNumber,
            ReceiptDate               = dto.ReceiptDate,
            ReceiptType               = dto.ReceiptType,
            WarehouseId               = dto.WarehouseId,
            StorageLocationId         = dto.StorageLocationId,
            OriginalReceiptId         = dto.OriginalReceiptId,
            FiscalPeriodId            = dto.FiscalPeriodId,
            Notes                     = dto.Notes,
            InternalNotes             = dto.InternalNotes,
            Status                    = GoodsReceiptStatus.Draft
        };

        foreach (var lineDto in dto.Lines)
        {
            var orderLine = order.Lines.FirstOrDefault(l => l.Id == lineDto.PurchaseOrderLineId)
                ?? throw new KeyNotFoundException($"PO line {lineDto.PurchaseOrderLineId} not found");

            receipt.Lines.Add(new GoodsReceiptLine
            {
                PurchaseOrderLineId     = lineDto.PurchaseOrderLineId,
                LineNumber              = dto.Lines.IndexOf(lineDto) + 1,
                ItemId                  = orderLine.ItemId,
                ItemCode                = orderLine.ItemCode,
                ItemDescription         = orderLine.ItemDescription,
                QuantityOrdered         = orderLine.Quantity,
                QuantityReceived        = lineDto.QuantityReceived,
                QuantityAccepted        = lineDto.QuantityAccepted,
                QuantityRejected        = lineDto.QuantityRejected,
                UnitOfMeasureId         = orderLine.UnitOfMeasureId,
                UnitOfMeasureName       = orderLine.UnitOfMeasureName,
                StorageLocationId       = lineDto.StorageLocationId,
                LotNumber               = lineDto.LotNumber,
                SerialNumber            = lineDto.SerialNumber,
                ExpiryDate              = lineDto.ExpiryDate,
                ManufacturerBatchNumber = lineDto.ManufacturerBatchNumber,
                QualityStatus           = QualityInspectionStatus.Pending,
                Notes                   = lineDto.Notes
            });
        }

        await _receipts.AddAsync(receipt);
        await _receipts.SaveChangesAsync();
        _logger.LogInformation("Created GRN {Number} for PO {PO}", receipt.ReceiptNumber, order.OrderNumber);
        return MapToDto(receipt);
    }

    public async Task<GoodsReceiptDto?> CreateDraftFromPurchaseOrderAsync(Guid purchaseOrderId, Guid userId)
    {
        // Idempotent: don't auto-create a second receipt if the PO already has one.
        var existing = await _receipts.GetByPurchaseOrderAsync(purchaseOrderId);
        if (existing.Any(r => r.Status != GoodsReceiptStatus.Cancelled)) return null;

        var order = await _orders.GetWithLinesAsync(purchaseOrderId);
        if (order is null) return null;

        var receipt = new GoodsReceipt
        {
            ReceiptNumber   = await _sequences.GetNextNumberAsync(ProcurementDocumentType.GoodsReceipt),
            PurchaseOrderId = order.Id,
            VendorId        = order.VendorId,
            ReceiptDate     = DateTime.UtcNow,
            ReceiptType     = ReceiptType.Standard,
            Status          = GoodsReceiptStatus.Draft,
            Notes           = $"Auto-created from PO {order.OrderNumber}",
        };

        var lineNo = 1;
        foreach (var ol in order.Lines)
        {
            var remaining = ol.QuantityRemaining;
            if (remaining <= 0) continue;
            receipt.Lines.Add(new GoodsReceiptLine
            {
                PurchaseOrderLineId = ol.Id,
                LineNumber          = lineNo++,
                ItemId              = ol.ItemId,
                ItemCode            = ol.ItemCode,
                ItemDescription     = ol.ItemDescription,
                QuantityOrdered     = ol.Quantity,
                QuantityReceived    = remaining,   // pre-filled; user adjusts/confirms
                QuantityAccepted    = remaining,
                QuantityRejected    = 0,
                UnitOfMeasureId     = ol.UnitOfMeasureId,
                UnitOfMeasureName   = ol.UnitOfMeasureName,
                QualityStatus       = QualityInspectionStatus.Pending,
            });
        }

        if (receipt.Lines.Count == 0) return null;

        await _receipts.AddAsync(receipt);
        await _receipts.SaveChangesAsync();
        _logger.LogInformation("Auto-created draft GRN {Number} from confirmed PO {PO}", receipt.ReceiptNumber, order.OrderNumber);
        return MapToDto(receipt);
    }

    public async Task<GoodsReceiptDto> UpdateAsync(Guid id, UpdateGoodsReceiptDto dto)
    {
        var receipt = await _receipts.GetWithLinesAsync(id)
            ?? throw new KeyNotFoundException($"Goods Receipt {id} not found");

        if (receipt.Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException($"Cannot update a goods receipt in status {receipt.Status}");

        if (dto.VendorDeliveryNoteNumber is not null) receipt.VendorDeliveryNoteNumber = dto.VendorDeliveryNoteNumber;
        if (dto.ReceiptDate.HasValue)                 receipt.ReceiptDate              = dto.ReceiptDate.Value;
        if (dto.WarehouseId.HasValue)                 receipt.WarehouseId              = dto.WarehouseId;
        if (dto.StorageLocationId.HasValue)           receipt.StorageLocationId        = dto.StorageLocationId;
        if (dto.Notes is not null)                    receipt.Notes                    = dto.Notes;
        if (dto.InternalNotes is not null)            receipt.InternalNotes            = dto.InternalNotes;

        _receipts.Update(receipt);
        await _receipts.SaveChangesAsync();
        return MapToDto(receipt);
    }

    public async Task<GoodsReceiptDto> PostAsync(Guid id, Guid postedByUserId, Guid? fiscalPeriodId = null)
    {
        var receipt = await _receipts.GetWithLinesAsync(id)
            ?? throw new KeyNotFoundException($"Goods Receipt {id} not found");

        if (receipt.Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException($"Cannot post a receipt in status {receipt.Status}");

        receipt.Status          = GoodsReceiptStatus.Posted;
        receipt.PostedByUserId  = postedByUserId;
        receipt.PostedAt        = DateTime.UtcNow;
        if (fiscalPeriodId.HasValue)
            receipt.FiscalPeriodId = fiscalPeriodId;

        // Update PO line quantities
        var order = await _orders.GetWithLinesAsync(receipt.PurchaseOrderId);
        if (order is not null)
        {
            foreach (var grLine in receipt.Lines)
            {
                var poLine = order.Lines.FirstOrDefault(l => l.Id == grLine.PurchaseOrderLineId);
                if (poLine is null) continue;
                poLine.QuantityReceived += grLine.QuantityReceived;
                poLine.QuantityAccepted += grLine.QuantityAccepted;
                poLine.QuantityRejected += grLine.QuantityRejected;

                poLine.LineStatus = poLine.QuantityReceived >= poLine.Quantity
                    ? PurchaseOrderLineStatus.FullyReceived
                    : PurchaseOrderLineStatus.PartiallyReceived;
            }

            var allReceived  = order.Lines.All(l => l.LineStatus == PurchaseOrderLineStatus.FullyReceived);
            var anyReceived  = order.Lines.Any(l => l.QuantityReceived > 0);
            order.Status = allReceived ? PurchaseOrderStatus.FullyReceived
                         : anyReceived ? PurchaseOrderStatus.PartiallyReceived
                         : order.Status;

            _orders.Update(order);
            await _orders.SaveChangesAsync();
        }

        _receipts.Update(receipt);
        await _receipts.SaveChangesAsync();

        // Add the accepted stock to Inventory (inbound). Inventory consumes this and posts a "GRN"
        // document + on-hand increase. Unit cost comes from the matching PO line — the GRN line has none.
        var costByPoLine = order?.Lines.ToDictionary(l => l.Id, l => l.UnitPrice) ?? new Dictionary<Guid, decimal>();
        var receiptLines = receipt.Lines
            .Where(l => l.ItemId.HasValue && l.QuantityAccepted > 0)
            .Select(l => new StockReceiptLine
            {
                ProductId   = l.ItemId!.Value,
                WarehouseId = receipt.WarehouseId,
                UnitId      = l.UnitOfMeasureId,
                Quantity    = l.QuantityAccepted,
                UnitCost    = costByPoLine.TryGetValue(l.PurchaseOrderLineId, out var c) ? c : 0m,
            })
            .ToList();

        if (receiptLines.Count > 0)
        {
            await _events.PublishAsync(new GoodsReceiptPostedEvent
            {
                GoodsReceiptId  = receipt.Id,
                ReceiptNumber   = receipt.ReceiptNumber,
                WarehouseId     = receipt.WarehouseId ?? Guid.Empty,
                CompanyId       = receipt.CompanyId,
                BranchId        = receipt.BranchId,
                BusinessUnitId  = receipt.BusinessUnitId,
                CreatedByUserId = postedByUserId,
                Lines           = receiptLines,
            });
        }

        // Auto-create a draft vendor invoice from this receipt so the user just enters the vendor's
        // invoice number and confirms. Never let a failure here roll back the posted receipt.
        try
        {
            await _invoiceService.CreateDraftFromGoodsReceiptAsync(receipt.Id, postedByUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-create draft invoice from GRN {Number} failed", receipt.ReceiptNumber);
        }

        _logger.LogInformation("GRN {Number} posted by {UserId}", receipt.ReceiptNumber, postedByUserId);
        return MapToDto(receipt);
    }

    public async Task<GoodsReceiptDto> CancelAsync(Guid id)
    {
        var receipt = await _receipts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Goods Receipt {id} not found");

        if (receipt.Status == GoodsReceiptStatus.Cancelled)
            throw new InvalidOperationException("Receipt is already cancelled");

        receipt.Status = GoodsReceiptStatus.Cancelled;
        _receipts.Update(receipt);
        await _receipts.SaveChangesAsync();
        return MapToDto(receipt);
    }

    public async Task<GoodsReceiptDto> InspectLinesAsync(Guid id, List<InspectGoodsReceiptLineDto> inspections, Guid inspectedByUserId)
    {
        var receipt = await _receipts.GetWithLinesAsync(id)
            ?? throw new KeyNotFoundException($"Goods Receipt {id} not found");

        foreach (var inspection in inspections)
        {
            var line = receipt.Lines.FirstOrDefault(l => l.Id == inspection.LineId)
                ?? throw new KeyNotFoundException($"Receipt line {inspection.LineId} not found");

            line.QualityStatus    = inspection.QualityStatus;
            line.QualityNotes     = inspection.QualityNotes;
            line.QuantityAccepted = inspection.QuantityAccepted;
            line.QuantityRejected = inspection.QuantityRejected;
            line.InspectedByUserId = inspectedByUserId;
            line.InspectedAt      = DateTime.UtcNow;
        }

        _receipts.Update(receipt);
        await _receipts.SaveChangesAsync();
        return MapToDto(receipt);
    }

    public async Task DeleteAsync(Guid id)
    {
        var receipt = await _receipts.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Goods Receipt {id} not found");

        if (receipt.Status != GoodsReceiptStatus.Draft)
            throw new InvalidOperationException("Only draft goods receipts can be deleted");

        _receipts.SoftDelete(receipt);
        await _receipts.SaveChangesAsync();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static GoodsReceiptDto MapToDto(GoodsReceipt r) => new()
    {
        Id                       = r.Id,
        ReceiptNumber            = r.ReceiptNumber,
        VendorDeliveryNoteNumber = r.VendorDeliveryNoteNumber,
        PurchaseOrderId          = r.PurchaseOrderId,
        PurchaseOrderNumber      = r.PurchaseOrder?.OrderNumber,
        VendorId                 = r.VendorId,
        VendorName               = r.Vendor?.Name,
        ReceiptDate              = r.ReceiptDate,
        PostedAt                 = r.PostedAt,
        Status                   = r.Status,
        ReceiptType              = r.ReceiptType,
        WarehouseId              = r.WarehouseId,
        StorageLocationId        = r.StorageLocationId,
        PostedByUserId           = r.PostedByUserId,
        OriginalReceiptId        = r.OriginalReceiptId,
        AccountingJournalEntryId = r.AccountingJournalEntryId,
        FiscalPeriodId           = r.FiscalPeriodId,
        Notes                    = r.Notes,
        InternalNotes            = r.InternalNotes,
        Lines = r.Lines.Select(l => new GoodsReceiptLineDto
        {
            Id                      = l.Id,
            LineNumber              = l.LineNumber,
            PurchaseOrderLineId     = l.PurchaseOrderLineId,
            ItemId                  = l.ItemId,
            ItemCode                = l.ItemCode,
            ItemDescription         = l.ItemDescription,
            QuantityOrdered         = l.QuantityOrdered,
            QuantityReceived        = l.QuantityReceived,
            QuantityAccepted        = l.QuantityAccepted,
            QuantityRejected        = l.QuantityRejected,
            UnitOfMeasureId         = l.UnitOfMeasureId,
            UnitOfMeasureName       = l.UnitOfMeasureName,
            LotNumber               = l.LotNumber,
            SerialNumber            = l.SerialNumber,
            ExpiryDate              = l.ExpiryDate,
            ManufacturerBatchNumber = l.ManufacturerBatchNumber,
            StorageLocationId       = l.StorageLocationId,
            StorageLocationName     = l.StorageLocationName,
            QualityStatus           = l.QualityStatus,
            QualityNotes            = l.QualityNotes,
            InspectedByUserId       = l.InspectedByUserId,
            InspectedAt             = l.InspectedAt,
            Notes                   = l.Notes
        }).ToList()
    };
}
