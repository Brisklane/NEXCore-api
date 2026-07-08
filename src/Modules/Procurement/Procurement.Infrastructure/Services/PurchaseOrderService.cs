using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Helpers;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;
using Procurement.Infrastructure.Repositories.Interfaces;

namespace Procurement.Infrastructure.Services;

public class PurchaseOrderService : IPurchaseOrderService
{
    private readonly IPurchaseOrderRepository _orders;
    private readonly IDocumentSequenceService _sequences;
    private readonly IGoodsReceiptService _goodsReceiptService;
    private readonly ProcurementDbContext _ctx;
    private readonly ILogger<PurchaseOrderService> _logger;

    public PurchaseOrderService(
        IPurchaseOrderRepository orders,
        IDocumentSequenceService sequences,
        IGoodsReceiptService goodsReceiptService,
        ProcurementDbContext ctx,
        ILogger<PurchaseOrderService> logger)
    {
        _orders    = orders;
        _sequences = sequences;
        _goodsReceiptService = goodsReceiptService;
        _ctx       = ctx;
        _logger    = logger;
    }

    public async Task<PaginatedResponse<PurchaseOrderDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _orders.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: o =>
                (string.IsNullOrEmpty(search) || o.OrderNumber.ToLower().Contains(search) ||
                    (o.VendorName != null && o.VendorName.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)o.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "CreatedAt"));
        return PaginatedResponse<PurchaseOrderDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PurchaseOrderDto?> GetByIdAsync(Guid id)
    {
        var o = await _orders.GetWithFullDetailsAsync(id);
        return o is null ? null : MapToDto(o);
    }

    public async Task<PurchaseOrderDto?> GetByNumberAsync(string orderNumber)
    {
        var o = await _orders.GetByNumberAsync(orderNumber);
        return o is null ? null : MapToDto(o);
    }

    public async Task<List<PurchaseOrderDto>> GetByStatusAsync(PurchaseOrderStatus status)
        => (await _orders.GetByStatusAsync(status)).Select(MapToDto).ToList();

    public async Task<List<PurchaseOrderDto>> GetByVendorAsync(Guid vendorId)
        => (await _orders.GetByVendorAsync(vendorId)).Select(MapToDto).ToList();

    public async Task<PaginatedResponse<PurchaseOrderDto>> GetPendingReceiptAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _orders.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: o =>
                (o.Status == PurchaseOrderStatus.Confirmed || o.Status == PurchaseOrderStatus.SentToVendor ||
                 o.Status == PurchaseOrderStatus.Acknowledged || o.Status == PurchaseOrderStatus.PartiallyReceived) &&
                (string.IsNullOrEmpty(search) || o.OrderNumber.ToLower().Contains(search) ||
                    (o.VendorName != null && o.VendorName.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)o.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "asc" : pagination.SortDirection, "ExpectedDeliveryDate"));
        return PaginatedResponse<PurchaseOrderDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<PurchaseOrderDto>> GetToInvoiceAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _orders.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: o =>
                (o.Status == PurchaseOrderStatus.PartiallyReceived || o.Status == PurchaseOrderStatus.FullyReceived ||
                 o.Status == PurchaseOrderStatus.PartiallyInvoiced) &&
                o.Lines.Any(l => l.QuantityAccepted > l.QuantityInvoiced) &&
                (string.IsNullOrEmpty(search) || o.OrderNumber.ToLower().Contains(search) ||
                    (o.VendorName != null && o.VendorName.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)o.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "asc" : pagination.SortDirection, "OrderDate"));
        return PaginatedResponse<PurchaseOrderDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PurchaseOrderDto> CreateAsync(CreatePurchaseOrderDto dto)
    {
        var order = new PurchaseOrder
        {
            OrderNumber            = await _sequences.GetNextNumberAsync(ProcurementDocumentType.PurchaseOrder),
            VendorId               = dto.VendorId,
            VendorReference        = dto.VendorReference,
            QuotationId            = dto.QuotationId,
            RequisitionId          = dto.RequisitionId,
            ContractId             = dto.ContractId,
            OrderDate              = dto.OrderDate,
            ExpectedDeliveryDate   = dto.ExpectedDeliveryDate,
            DeliveryAddressId      = dto.DeliveryAddressId,
            DeliveryStreet         = dto.DeliveryStreet,
            DeliveryCity           = dto.DeliveryCity,
            DeliveryState          = dto.DeliveryState,
            DeliveryPostalCode     = dto.DeliveryPostalCode,
            DeliveryCountry        = dto.DeliveryCountry,
            CurrencyCode           = dto.CurrencyCode,
            ExchangeRate           = dto.ExchangeRate,
            PaymentTerms           = dto.PaymentTerms,
            Incoterm               = dto.Incoterm,
            IncotermLocation       = dto.IncotermLocation,
            ShippingAmount         = dto.ShippingAmount,
            BudgetId               = dto.BudgetId,
            TermsAndConditions     = dto.TermsAndConditions,
            Notes                  = dto.Notes,
            InternalNotes          = dto.InternalNotes,
            Status                 = PurchaseOrderStatus.Draft
        };

        foreach (var lineDto in dto.Lines)
        {
            var subTotal   = lineDto.Quantity * lineDto.UnitPrice * (1 - lineDto.DiscountPercent / 100);
            var taxAmount  = subTotal * lineDto.TaxPercent / 100;
            var totalPrice = subTotal + taxAmount;

            order.Lines.Add(new PurchaseOrderLine
            {
                LineNumber             = dto.Lines.IndexOf(lineDto) + 1,
                RequisitionLineId      = lineDto.RequisitionLineId,
                ContractLineId         = lineDto.ContractLineId,
                ItemId                 = lineDto.ItemId,
                ItemCode               = lineDto.ItemCode,
                ItemDescription        = lineDto.ItemDescription,
                Quantity               = lineDto.Quantity,
                UnitOfMeasureId        = lineDto.UnitOfMeasureId,
                UnitOfMeasureName      = lineDto.UnitOfMeasureName,
                UnitPrice              = lineDto.UnitPrice,
                DiscountPercent        = lineDto.DiscountPercent,
                DiscountAmount         = lineDto.Quantity * lineDto.UnitPrice * lineDto.DiscountPercent / 100,
                TaxCodeId              = lineDto.TaxCodeId,
                TaxPercent             = lineDto.TaxPercent,
                TaxAmount              = taxAmount,
                SubTotal               = subTotal,
                TotalPrice             = totalPrice,
                ProcurementCategoryId  = lineDto.ProcurementCategoryId,
                LedgerAccountId        = lineDto.LedgerAccountId,
                CostCenterId           = lineDto.CostCenterId,
                DimensionSetId         = lineDto.DimensionSetId,
                ExpectedDeliveryDate   = lineDto.ExpectedDeliveryDate,
                DeliveryLocationId     = lineDto.DeliveryLocationId,
                Notes                  = lineDto.Notes
            });
        }

        RecalculateTotals(order);
        await _orders.AddAsync(order);
        await _orders.SaveChangesAsync();
        _logger.LogInformation("Created PO {Number} for vendor {VendorId}", order.OrderNumber, order.VendorId);
        return MapToDto(order);
    }

    public async Task<PurchaseOrderDto> UpdateAsync(Guid id, UpdatePurchaseOrderDto dto)
    {
        var order = await _orders.GetWithFullDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Purchase Order {id} not found");

        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot update a purchase order in status {order.Status}");

        if (dto.VendorReference is not null)      order.VendorReference      = dto.VendorReference;
        if (dto.ExpectedDeliveryDate.HasValue)    order.ExpectedDeliveryDate = dto.ExpectedDeliveryDate;
        if (dto.DeliveryStreet is not null)       order.DeliveryStreet       = dto.DeliveryStreet;
        if (dto.DeliveryCity is not null)         order.DeliveryCity         = dto.DeliveryCity;
        if (dto.DeliveryState is not null)        order.DeliveryState        = dto.DeliveryState;
        if (dto.DeliveryPostalCode is not null)   order.DeliveryPostalCode   = dto.DeliveryPostalCode;
        if (dto.DeliveryCountry is not null)      order.DeliveryCountry      = dto.DeliveryCountry;
        if (dto.TermsAndConditions is not null)   order.TermsAndConditions   = dto.TermsAndConditions;
        if (dto.Notes is not null)                order.Notes                = dto.Notes;
        if (dto.InternalNotes is not null)        order.InternalNotes        = dto.InternalNotes;

        if (dto.Lines is not null)
        {
            // Replace lines on the DbSet directly so new rows are tracked as Added (not
            // Modified via Update(graph), which throws a phantom 0-row concurrency error).
            // New lines are also added to the navigation so RecalculateTotals sees them.
            _ctx.Set<PurchaseOrderLine>().RemoveRange(order.Lines);
            order.Lines.Clear();
            var lineNumber = 1;
            foreach (var lineDto in dto.Lines)
            {
                var subTotal   = lineDto.Quantity * lineDto.UnitPrice * (1 - lineDto.DiscountPercent / 100);
                var taxAmount  = subTotal * lineDto.TaxPercent / 100;
                var totalPrice = subTotal + taxAmount;

                var line = new PurchaseOrderLine
                {
                    OrderId               = order.Id,
                    LineNumber            = lineNumber++,
                    RequisitionLineId     = lineDto.RequisitionLineId,
                    ContractLineId        = lineDto.ContractLineId,
                    ItemId                = lineDto.ItemId,
                    ItemCode              = lineDto.ItemCode,
                    ItemDescription       = lineDto.ItemDescription,
                    Quantity              = lineDto.Quantity,
                    UnitOfMeasureId       = lineDto.UnitOfMeasureId,
                    UnitOfMeasureName     = lineDto.UnitOfMeasureName,
                    UnitPrice             = lineDto.UnitPrice,
                    DiscountPercent       = lineDto.DiscountPercent,
                    DiscountAmount        = lineDto.Quantity * lineDto.UnitPrice * lineDto.DiscountPercent / 100,
                    TaxCodeId             = lineDto.TaxCodeId,
                    TaxPercent            = lineDto.TaxPercent,
                    TaxAmount             = taxAmount,
                    SubTotal              = subTotal,
                    TotalPrice            = totalPrice,
                    ProcurementCategoryId = lineDto.ProcurementCategoryId,
                    LedgerAccountId       = lineDto.LedgerAccountId,
                    CostCenterId          = lineDto.CostCenterId,
                    DimensionSetId        = lineDto.DimensionSetId,
                    ExpectedDeliveryDate  = lineDto.ExpectedDeliveryDate,
                    DeliveryLocationId    = lineDto.DeliveryLocationId,
                    Notes                 = lineDto.Notes
                };
                _ctx.Set<PurchaseOrderLine>().Add(line);
                order.Lines.Add(line);
            }
            RecalculateTotals(order);
        }

        // order is tracked (loaded with Include) — header changes persist without Update(graph).
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<PurchaseOrderDto> ConfirmAsync(Guid id, Guid confirmedByUserId)
    {
        var order = await _orders.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Purchase Order {id} not found");

        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException($"Cannot confirm a PO in status {order.Status}");

        order.Status          = PurchaseOrderStatus.Confirmed;
        order.ApprovedByUserId = confirmedByUserId;
        order.ApprovedAt      = DateTime.UtcNow;

        _orders.Update(order);
        await _orders.SaveChangesAsync();

        // Auto-create a draft Goods Receipt so the user only reviews/confirms quantities.
        // Never let a failure here roll back the confirmation.
        try
        {
            await _goodsReceiptService.CreateDraftFromPurchaseOrderAsync(id, confirmedByUserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Auto-create draft GRN from confirmed PO {Number} failed", order.OrderNumber);
        }

        _logger.LogInformation("PO {Number} confirmed by {UserId}", order.OrderNumber, confirmedByUserId);
        return MapToDto(order);
    }

    public async Task<PurchaseOrderDto> SendToVendorAsync(Guid id)
    {
        var order = await _orders.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Purchase Order {id} not found");

        if (order.Status != PurchaseOrderStatus.Confirmed)
            throw new InvalidOperationException($"Cannot send a PO in status {order.Status} to vendor");

        order.Status         = PurchaseOrderStatus.SentToVendor;
        order.SentToVendorAt = DateTime.UtcNow;

        _orders.Update(order);
        await _orders.SaveChangesAsync();
        return MapToDto(order);
    }

    public async Task<PurchaseOrderDto> AcknowledgeAsync(Guid id)
    {
        var order = await _orders.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Purchase Order {id} not found");

        if (order.Status != PurchaseOrderStatus.SentToVendor)
            throw new InvalidOperationException($"Cannot acknowledge a PO in status {order.Status}");

        order.Status         = PurchaseOrderStatus.Acknowledged;
        order.AcknowledgedAt = DateTime.UtcNow;

        _orders.Update(order);
        await _orders.SaveChangesAsync();
        return MapToDto(order);
    }

    public async Task<PurchaseOrderDto> CloseAsync(Guid id)
    {
        var order = await _orders.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Purchase Order {id} not found");

        order.Status   = PurchaseOrderStatus.Closed;
        order.ClosedAt = DateTime.UtcNow;

        _orders.Update(order);
        await _orders.SaveChangesAsync();
        return MapToDto(order);
    }

    public async Task<PurchaseOrderDto> CancelAsync(Guid id, CancelPurchaseOrderDto dto)
    {
        var order = await _orders.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Purchase Order {id} not found");

        if (order.Status == PurchaseOrderStatus.FullyReceived ||
            order.Status == PurchaseOrderStatus.FullyInvoiced ||
            order.Status == PurchaseOrderStatus.Closed)
            throw new InvalidOperationException($"Cannot cancel a PO in status {order.Status}");

        order.Status             = PurchaseOrderStatus.Cancelled;
        order.CancelledAt        = DateTime.UtcNow;
        order.CancellationReason = dto.CancellationReason;

        _orders.Update(order);
        await _orders.SaveChangesAsync();
        return MapToDto(order);
    }

    public async Task DeleteAsync(Guid id)
    {
        var order = await _orders.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Purchase Order {id} not found");

        if (order.Status != PurchaseOrderStatus.Draft)
            throw new InvalidOperationException("Only draft purchase orders can be deleted");

        // Soft-delete via ExecuteUpdate — order was read AsNoTracking, so re-attaching the whole
        // graph (SoftDelete -> DbSet.Update) can throw a phantom concurrency error; a targeted
        // SET avoids that and reliably persists the flag.
        await _ctx.PurchaseOrders
            .Where(o => o.Id == id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(o => o.IsDeleted, true)
                .SetProperty(o => o.DeletedAt, DateTime.UtcNow));
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void RecalculateTotals(PurchaseOrder order)
    {
        order.SubTotalAmount   = order.Lines.Sum(l => l.SubTotal);
        order.TaxAmount        = order.Lines.Sum(l => l.TaxAmount);
        order.DiscountAmount   = order.Lines.Sum(l => l.DiscountAmount);
        order.TotalAmount      = order.SubTotalAmount + order.TaxAmount + order.ShippingAmount;
        order.OutstandingAmount = order.TotalAmount - order.PaidAmount;
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    internal static PurchaseOrderDto MapToDtoStatic(PurchaseOrder o) => MapToDto(o);

    private static PurchaseOrderDto MapToDto(PurchaseOrder o) => new()
    {
        Id                     = o.Id,
        OrderNumber            = o.OrderNumber,
        VendorId               = o.VendorId,
        VendorName             = o.VendorName ?? o.Vendor?.Name,
        VendorReference        = o.VendorReference,
        QuotationId            = o.QuotationId,
        RequisitionId          = o.RequisitionId,
        ContractId             = o.ContractId,
        OrderDate              = o.OrderDate,
        ExpectedDeliveryDate   = o.ExpectedDeliveryDate,
        ConfirmedDeliveryDate  = o.ConfirmedDeliveryDate,
        SentToVendorAt         = o.SentToVendorAt,
        AcknowledgedAt         = o.AcknowledgedAt,
        ClosedAt               = o.ClosedAt,
        CancelledAt            = o.CancelledAt,
        Status                 = o.Status,
        DeliveryAddressId      = o.DeliveryAddressId,
        DeliveryStreet         = o.DeliveryStreet,
        DeliveryCity           = o.DeliveryCity,
        DeliveryState          = o.DeliveryState,
        DeliveryPostalCode     = o.DeliveryPostalCode,
        DeliveryCountry        = o.DeliveryCountry,
        CurrencyCode           = o.CurrencyCode,
        ExchangeRate           = o.ExchangeRate,
        PaymentTerms           = o.PaymentTerms,
        Incoterm               = o.Incoterm,
        IncotermLocation       = o.IncotermLocation,
        SubTotalAmount         = o.SubTotalAmount,
        TaxAmount              = o.TaxAmount,
        DiscountAmount         = o.DiscountAmount,
        ShippingAmount         = o.ShippingAmount,
        TotalAmount            = o.TotalAmount,
        InvoicedAmount         = o.InvoicedAmount,
        PaidAmount             = o.PaidAmount,
        OutstandingAmount      = o.OutstandingAmount,
        BudgetId               = o.BudgetId,
        AccountingCommitmentEntryId = o.AccountingCommitmentEntryId,
        ApprovedByUserId       = o.ApprovedByUserId,
        ApprovedAt             = o.ApprovedAt,
        TermsAndConditions     = o.TermsAndConditions,
        Notes                  = o.Notes,
        InternalNotes          = o.InternalNotes,
        CancellationReason     = o.CancellationReason,
        Lines = o.Lines.Select(l => new PurchaseOrderLineDto
        {
            Id                    = l.Id,
            LineNumber            = l.LineNumber,
            RequisitionLineId     = l.RequisitionLineId,
            ContractLineId        = l.ContractLineId,
            ItemId                = l.ItemId,
            ItemCode              = l.ItemCode,
            ItemDescription       = l.ItemDescription,
            Quantity              = l.Quantity,
            UnitOfMeasureId       = l.UnitOfMeasureId,
            UnitOfMeasureName     = l.UnitOfMeasureName,
            UnitPrice             = l.UnitPrice,
            DiscountPercent       = l.DiscountPercent,
            DiscountAmount        = l.DiscountAmount,
            TaxCodeId             = l.TaxCodeId,
            TaxPercent            = l.TaxPercent,
            TaxAmount             = l.TaxAmount,
            SubTotal              = l.SubTotal,
            TotalPrice            = l.TotalPrice,
            ProcurementCategoryId = l.ProcurementCategoryId,
            LedgerAccountId       = l.LedgerAccountId,
            CostCenterId          = l.CostCenterId,
            DimensionSetId        = l.DimensionSetId,
            ExpectedDeliveryDate  = l.ExpectedDeliveryDate,
            DeliveryLocationId    = l.DeliveryLocationId,
            QuantityReceived      = l.QuantityReceived,
            QuantityAccepted      = l.QuantityAccepted,
            QuantityRejected      = l.QuantityRejected,
            QuantityReturned      = l.QuantityReturned,
            QuantityInvoiced      = l.QuantityInvoiced,
            QuantityRemaining     = l.Quantity - l.QuantityReceived,
            QuantityToInvoice     = l.QuantityAccepted - l.QuantityInvoiced,
            LineStatus            = l.LineStatus,
            Notes                 = l.Notes
        }).ToList()
    };
}
