using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Api;
using Nexcore.SharedKernel.Events;
using Nexcore.SharedKernel.Helpers;
using Procurement.Application.DTOs;
using Procurement.Application.Services.Interfaces;
using Procurement.Domain.Entities;
using Procurement.Domain.Enums;
using Procurement.Infrastructure.Persistence;
using Procurement.Infrastructure.Repositories.Interfaces;

namespace Procurement.Infrastructure.Services;

public class PurchaseInvoiceService : IPurchaseInvoiceService
{
    private readonly IPurchaseInvoiceRepository _invoices;
    private readonly IPurchaseOrderRepository _orders;
    private readonly IDocumentSequenceService _sequences;
    private readonly ProcurementDbContext _ctx;
    private readonly IEventPublisher _events;
    private readonly ILogger<PurchaseInvoiceService> _logger;

    public PurchaseInvoiceService(
        IPurchaseInvoiceRepository invoices,
        IPurchaseOrderRepository orders,
        IDocumentSequenceService sequences,
        ProcurementDbContext ctx,
        IEventPublisher events,
        ILogger<PurchaseInvoiceService> logger)
    {
        _invoices  = invoices;
        _orders    = orders;
        _sequences = sequences;
        _ctx       = ctx;
        _events    = events;
        _logger    = logger;
    }

    public async Task<PaginatedResponse<PurchaseInvoiceDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _invoices.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: i =>
                (string.IsNullOrEmpty(search) || i.InvoiceNumber.ToLower().Contains(search) ||
                    i.VendorInvoiceNumber.ToLower().Contains(search) ||
                    (i.VendorName != null && i.VendorName.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)i.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "CreatedAt"));
        return PaginatedResponse<PurchaseInvoiceDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PurchaseInvoiceDto?> GetByIdAsync(Guid id)
    {
        var i = await _invoices.GetWithFullDetailsAsync(id);
        return i is null ? null : MapToDto(i);
    }

    public async Task<PurchaseInvoiceDto?> GetByNumberAsync(string invoiceNumber)
    {
        var i = await _invoices.GetByNumberAsync(invoiceNumber);
        return i is null ? null : MapToDto(i);
    }

    public async Task<List<PurchaseInvoiceDto>> GetByStatusAsync(PurchaseInvoiceStatus status)
        => (await _invoices.GetByStatusAsync(status)).Select(MapToDto).ToList();

    public async Task<List<PurchaseInvoiceDto>> GetByVendorAsync(Guid vendorId)
        => (await _invoices.GetByVendorAsync(vendorId)).Select(MapToDto).ToList();

    public async Task<List<PurchaseInvoiceDto>> GetByPurchaseOrderAsync(Guid purchaseOrderId)
        => (await _invoices.GetByPurchaseOrderAsync(purchaseOrderId)).Select(MapToDto).ToList();

    public async Task<PaginatedResponse<PurchaseInvoiceDto>> GetOverdueAsync(PaginationParams pagination)
    {
        var today = DateTime.UtcNow;
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _invoices.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: i =>
                i.DueDate < today &&
                (i.PaymentStatus == InvoicePaymentStatus.NotPaid || i.PaymentStatus == InvoicePaymentStatus.PartiallyPaid) &&
                (string.IsNullOrEmpty(search) || i.InvoiceNumber.ToLower().Contains(search) ||
                    i.VendorInvoiceNumber.ToLower().Contains(search) ||
                    (i.VendorName != null && i.VendorName.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)i.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "asc" : pagination.SortDirection, "DueDate"));
        return PaginatedResponse<PurchaseInvoiceDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PaginatedResponse<PurchaseInvoiceDto>> GetPendingPaymentAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _invoices.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: i =>
                i.Status == PurchaseInvoiceStatus.Posted &&
                (i.PaymentStatus == InvoicePaymentStatus.NotPaid || i.PaymentStatus == InvoicePaymentStatus.PartiallyPaid) &&
                (string.IsNullOrEmpty(search) || i.InvoiceNumber.ToLower().Contains(search) ||
                    i.VendorInvoiceNumber.ToLower().Contains(search) ||
                    (i.VendorName != null && i.VendorName.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)i.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "asc" : pagination.SortDirection, "DueDate"));
        return PaginatedResponse<PurchaseInvoiceDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<PurchaseInvoiceDto> CreateAsync(CreatePurchaseInvoiceDto dto)
    {
        if (await _invoices.VendorInvoiceNumberExistsAsync(dto.VendorId, dto.VendorInvoiceNumber))
            throw new InvalidOperationException(
                $"Vendor invoice number '{dto.VendorInvoiceNumber}' already exists for this vendor");

        var invoice = new PurchaseInvoice
        {
            InvoiceNumber      = await _sequences.GetNextNumberAsync(ProcurementDocumentType.PurchaseInvoice),
            VendorInvoiceNumber = dto.VendorInvoiceNumber,
            VendorId           = dto.VendorId,
            PurchaseOrderId    = dto.PurchaseOrderId,
            InvoiceDate        = dto.InvoiceDate,
            DueDate            = dto.DueDate,
            PostingDate        = dto.PostingDate,
            CurrencyCode       = dto.CurrencyCode,
            ExchangeRate       = dto.ExchangeRate,
            PaymentTerms       = dto.PaymentTerms,
            FiscalPeriodId     = dto.FiscalPeriodId,
            DimensionSetId     = dto.DimensionSetId,
            Notes              = dto.Notes,
            InternalNotes      = dto.InternalNotes,
            Status             = PurchaseInvoiceStatus.Draft,
            MatchingStatus     = InvoiceMatchingStatus.NotMatched,
            PaymentStatus      = InvoicePaymentStatus.NotPaid
        };

        foreach (var lineDto in dto.Lines)
        {
            var subTotal   = lineDto.Quantity * lineDto.UnitPrice * (1 - lineDto.DiscountPercent / 100);
            var taxAmount  = subTotal * lineDto.TaxPercent / 100;

            invoice.Lines.Add(new PurchaseInvoiceLine
            {
                LineNumber            = dto.Lines.IndexOf(lineDto) + 1,
                PurchaseOrderLineId   = lineDto.PurchaseOrderLineId,
                GoodsReceiptLineId    = lineDto.GoodsReceiptLineId,
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
                TotalPrice            = subTotal + taxAmount,
                ProcurementCategoryId = lineDto.ProcurementCategoryId,
                LedgerAccountId       = lineDto.LedgerAccountId,
                DimensionSetId        = lineDto.DimensionSetId,
                Notes                 = lineDto.Notes
            });
        }

        invoice.SubTotalAmount  = invoice.Lines.Sum(l => l.SubTotal);
        invoice.TaxAmount       = invoice.Lines.Sum(l => l.TaxAmount);
        invoice.DiscountAmount  = invoice.Lines.Sum(l => l.DiscountAmount);
        invoice.TotalAmount     = invoice.SubTotalAmount + invoice.TaxAmount;
        invoice.OutstandingAmount = invoice.TotalAmount;

        await _invoices.AddAsync(invoice);
        await _invoices.SaveChangesAsync();
        _logger.LogInformation("Created invoice {Number} (vendor: {VendorInvoice})",
            invoice.InvoiceNumber, invoice.VendorInvoiceNumber);
        return MapToDto(invoice);
    }

    public async Task<PurchaseInvoiceDto?> CreateDraftFromGoodsReceiptAsync(Guid goodsReceiptId, Guid userId)
    {
        var receipt = await _ctx.GoodsReceipts
            .Include(r => r.Lines)
            .FirstOrDefaultAsync(r => r.Id == goodsReceiptId);
        if (receipt is null) return null;

        var order = await _orders.GetWithLinesAsync(receipt.PurchaseOrderId);
        var poLineById = order?.Lines.ToDictionary(l => l.Id) ?? new Dictionary<Guid, PurchaseOrderLine>();

        var invoice = new PurchaseInvoice
        {
            InvoiceNumber       = await _sequences.GetNextNumberAsync(ProcurementDocumentType.PurchaseInvoice),
            // Placeholder — the user replaces this with the vendor's real invoice number before posting.
            VendorInvoiceNumber = $"GRN-{receipt.ReceiptNumber}",
            VendorId            = receipt.VendorId,
            PurchaseOrderId     = receipt.PurchaseOrderId,
            InvoiceDate         = DateTime.UtcNow,
            DueDate             = DateTime.UtcNow.AddDays(30),
            CurrencyCode        = order?.CurrencyCode ?? "USD",
            ExchangeRate        = order?.ExchangeRate ?? 1m,
            Status              = PurchaseInvoiceStatus.Draft,
            MatchingStatus      = InvoiceMatchingStatus.NotMatched,
            PaymentStatus       = InvoicePaymentStatus.NotPaid,
            Notes               = $"Auto-created from GRN {receipt.ReceiptNumber}",
        };

        var lineNo = 1;
        foreach (var rl in receipt.Lines.Where(l => l.QuantityAccepted > 0))
        {
            poLineById.TryGetValue(rl.PurchaseOrderLineId, out var pol);
            var qty             = rl.QuantityAccepted;
            var unitPrice       = pol?.UnitPrice ?? 0m;
            var discountPercent = pol?.DiscountPercent ?? 0m;
            var taxPercent      = pol?.TaxPercent ?? 0m;
            var subTotal        = qty * unitPrice * (1 - discountPercent / 100);
            var taxAmount       = subTotal * taxPercent / 100;

            invoice.Lines.Add(new PurchaseInvoiceLine
            {
                LineNumber            = lineNo++,
                PurchaseOrderLineId   = rl.PurchaseOrderLineId,
                GoodsReceiptLineId    = rl.Id,
                ItemId                = rl.ItemId,
                ItemCode              = rl.ItemCode,
                ItemDescription       = rl.ItemDescription,
                Quantity              = qty,
                UnitOfMeasureId       = rl.UnitOfMeasureId,
                UnitOfMeasureName     = rl.UnitOfMeasureName,
                UnitPrice             = unitPrice,
                DiscountPercent       = discountPercent,
                DiscountAmount        = qty * unitPrice * discountPercent / 100,
                TaxPercent            = taxPercent,
                TaxAmount             = taxAmount,
                SubTotal              = subTotal,
                TotalPrice            = subTotal + taxAmount,
                LedgerAccountId       = pol?.LedgerAccountId,
                ProcurementCategoryId = pol?.ProcurementCategoryId,
            });
        }

        if (invoice.Lines.Count == 0) return null;

        invoice.SubTotalAmount    = invoice.Lines.Sum(l => l.SubTotal);
        invoice.TaxAmount         = invoice.Lines.Sum(l => l.TaxAmount);
        invoice.DiscountAmount    = invoice.Lines.Sum(l => l.DiscountAmount);
        invoice.TotalAmount       = invoice.SubTotalAmount + invoice.TaxAmount;
        invoice.OutstandingAmount = invoice.TotalAmount;

        await _invoices.AddAsync(invoice);
        await _invoices.SaveChangesAsync();
        _logger.LogInformation("Auto-created draft invoice {Number} from GRN {GRN}", invoice.InvoiceNumber, receipt.ReceiptNumber);
        return MapToDto(invoice);
    }

    public async Task<PurchaseInvoiceDto> UpdateAsync(Guid id, UpdatePurchaseInvoiceDto dto)
    {
        var invoice = await _invoices.GetWithFullDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != PurchaseInvoiceStatus.Draft)
            throw new InvalidOperationException($"Cannot update an invoice in status {invoice.Status}");

        if (dto.InvoiceDate.HasValue)    invoice.InvoiceDate    = dto.InvoiceDate.Value;
        if (dto.DueDate.HasValue)        invoice.DueDate        = dto.DueDate.Value;
        if (dto.FiscalPeriodId.HasValue) invoice.FiscalPeriodId = dto.FiscalPeriodId;
        if (dto.DimensionSetId.HasValue) invoice.DimensionSetId = dto.DimensionSetId;
        if (dto.Notes is not null)       invoice.Notes          = dto.Notes;
        if (dto.InternalNotes is not null) invoice.InternalNotes = dto.InternalNotes;

        if (dto.Lines is not null)
        {
            // Replace lines on the DbSet directly (new rows tracked as Added, not Modified
            // via Update(graph) which throws a phantom 0-row concurrency error). Lines are
            // also added to the navigation so the totals below see them.
            _ctx.Set<PurchaseInvoiceLine>().RemoveRange(invoice.Lines);
            invoice.Lines.Clear();
            var lineNumber = 1;
            foreach (var lineDto in dto.Lines)
            {
                var subTotal  = lineDto.Quantity * lineDto.UnitPrice * (1 - lineDto.DiscountPercent / 100);
                var taxAmount = subTotal * lineDto.TaxPercent / 100;

                var line = new PurchaseInvoiceLine
                {
                    InvoiceId             = invoice.Id,
                    LineNumber            = lineNumber++,
                    PurchaseOrderLineId   = lineDto.PurchaseOrderLineId,
                    GoodsReceiptLineId    = lineDto.GoodsReceiptLineId,
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
                    TotalPrice            = subTotal + taxAmount,
                    ProcurementCategoryId = lineDto.ProcurementCategoryId,
                    LedgerAccountId       = lineDto.LedgerAccountId,
                    DimensionSetId        = lineDto.DimensionSetId,
                    Notes                 = lineDto.Notes
                };
                _ctx.Set<PurchaseInvoiceLine>().Add(line);
                invoice.Lines.Add(line);
            }

            invoice.SubTotalAmount    = invoice.Lines.Sum(l => l.SubTotal);
            invoice.TaxAmount         = invoice.Lines.Sum(l => l.TaxAmount);
            invoice.DiscountAmount    = invoice.Lines.Sum(l => l.DiscountAmount);
            invoice.TotalAmount       = invoice.SubTotalAmount + invoice.TaxAmount;
            invoice.OutstandingAmount = invoice.TotalAmount - invoice.PaidAmount;
        }

        // invoice is tracked (loaded with Include) — header changes persist without Update(graph).
        await _ctx.SaveChangesAsync();
        return (await GetByIdAsync(id))!;
    }

    public async Task<PurchaseInvoiceDto> ApproveAsync(Guid id, Guid approvedByUserId)
    {
        var invoice = await _invoices.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != PurchaseInvoiceStatus.Draft &&
            invoice.Status != PurchaseInvoiceStatus.UnderApproval)
            throw new InvalidOperationException($"Cannot approve invoice in status {invoice.Status}");

        invoice.ApprovedByUserId = approvedByUserId;
        invoice.ApprovedAt       = DateTime.UtcNow;

        _invoices.Update(invoice);
        await _invoices.SaveChangesAsync();
        return MapToDto(invoice);
    }

    public async Task<PurchaseInvoiceDto> PostAsync(Guid id, Guid postedByUserId)
    {
        var invoice = await _invoices.GetWithLinesAsync(id)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != PurchaseInvoiceStatus.Draft &&
            invoice.Status != PurchaseInvoiceStatus.UnderApproval)
            throw new InvalidOperationException($"Cannot post invoice in status {invoice.Status}");

        invoice.Status        = PurchaseInvoiceStatus.Posted;
        invoice.PostedByUserId = postedByUserId;
        invoice.PostedAt      = DateTime.UtcNow;

        // Update PO invoiced quantities and amounts
        if (invoice.PurchaseOrderId.HasValue)
        {
            var order = await _orders.GetWithLinesAsync(invoice.PurchaseOrderId.Value);
            if (order is not null)
            {
                foreach (var invLine in invoice.Lines.Where(l => l.PurchaseOrderLineId.HasValue))
                {
                    var poLine = order.Lines.FirstOrDefault(l => l.Id == invLine.PurchaseOrderLineId);
                    if (poLine is not null)
                        poLine.QuantityInvoiced += invLine.Quantity;
                }

                order.InvoicedAmount += invoice.TotalAmount;
                order.OutstandingAmount = order.TotalAmount - order.PaidAmount;

                var allInvoiced = order.Lines.All(l => l.QuantityAccepted <= l.QuantityInvoiced);
                if (allInvoiced) order.Status = PurchaseOrderStatus.FullyInvoiced;
                else if (order.Lines.Any(l => l.QuantityInvoiced > 0))
                    order.Status = PurchaseOrderStatus.PartiallyInvoiced;

                _orders.Update(order);
                await _orders.SaveChangesAsync();
            }
        }

        _invoices.Update(invoice);
        await _invoices.SaveChangesAsync();

        // Recognise the payable in Accounting (DR Expense/Inventory, CR Accounts Payable).
        await _events.PublishAsync(new PurchaseInvoicePostedEvent
        {
            InvoiceId       = invoice.Id,
            InvoiceNumber   = invoice.InvoiceNumber,
            VendorId        = invoice.VendorId,
            VendorName      = invoice.VendorName,
            CompanyId       = invoice.CompanyId,
            BranchId        = invoice.BranchId,
            BusinessUnitId  = invoice.BusinessUnitId,
            CreatedByUserId = postedByUserId,
            InvoiceDate     = invoice.InvoiceDate,
            DueDate         = invoice.DueDate,
            TotalAmount     = invoice.TotalAmount,
            CurrencyCode    = invoice.CurrencyCode,
            ExchangeRate    = invoice.ExchangeRate,
            Lines           = invoice.Lines.Select(l => new PurchaseInvoiceAccountingLine
            {
                Amount           = l.TotalPrice,   // tax-inclusive (simple model)
                ExpenseAccountId = l.LedgerAccountId,
                Description      = l.ItemDescription,
            }).ToList(),
        });

        _logger.LogInformation("Invoice {Number} posted by {UserId}", invoice.InvoiceNumber, postedByUserId);
        return MapToDto(invoice);
    }

    public async Task<PurchaseInvoiceDto> HoldAsync(Guid id, HoldInvoiceDto dto)
    {
        var invoice = await _invoices.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        invoice.Status     = PurchaseInvoiceStatus.OnHold;
        invoice.HoldReason = dto.HoldReason;
        _invoices.Update(invoice);
        await _invoices.SaveChangesAsync();
        return MapToDto(invoice);
    }

    public async Task<PurchaseInvoiceDto> ReleaseHoldAsync(Guid id)
    {
        var invoice = await _invoices.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != PurchaseInvoiceStatus.OnHold)
            throw new InvalidOperationException("Invoice is not on hold");

        invoice.Status     = PurchaseInvoiceStatus.Draft;
        invoice.HoldReason = null;
        _invoices.Update(invoice);
        await _invoices.SaveChangesAsync();
        return MapToDto(invoice);
    }

    public async Task<PurchaseInvoiceDto> DisputeAsync(Guid id, DisputeInvoiceDto dto)
    {
        var invoice = await _invoices.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        invoice.Status        = PurchaseInvoiceStatus.Disputed;
        invoice.DisputeReason = dto.DisputeReason;
        _invoices.Update(invoice);
        await _invoices.SaveChangesAsync();
        return MapToDto(invoice);
    }

    public async Task<PurchaseInvoiceDto> ResolveDisputeAsync(Guid id)
    {
        var invoice = await _invoices.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != PurchaseInvoiceStatus.Disputed)
            throw new InvalidOperationException("Invoice is not disputed");

        invoice.Status        = PurchaseInvoiceStatus.Draft;
        invoice.DisputeReason = null;
        _invoices.Update(invoice);
        await _invoices.SaveChangesAsync();
        return MapToDto(invoice);
    }

    public async Task<PurchaseInvoiceDto> CancelAsync(Guid id)
    {
        var invoice = await _invoices.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status == PurchaseInvoiceStatus.FullyPaid)
            throw new InvalidOperationException("Cannot cancel a fully paid invoice");

        invoice.Status = PurchaseInvoiceStatus.Cancelled;
        _invoices.Update(invoice);
        await _invoices.SaveChangesAsync();
        return MapToDto(invoice);
    }

    public async Task DeleteAsync(Guid id)
    {
        var invoice = await _invoices.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        if (invoice.Status != PurchaseInvoiceStatus.Draft)
            throw new InvalidOperationException("Only draft invoices can be deleted");

        _invoices.SoftDelete(invoice);
        await _invoices.SaveChangesAsync();
    }

    public async Task<PurchaseInvoiceDto> RunThreeWayMatchAsync(Guid id)
    {
        var invoice = await _invoices.GetWithFullDetailsAsync(id)
            ?? throw new KeyNotFoundException($"Invoice {id} not found");

        // Simple quantity-based match: check each line against its PO line quantities
        var allMatched = true;
        foreach (var line in invoice.Lines.Where(l => l.PurchaseOrderLineId.HasValue))
        {
            var matchRecord = invoice.MatchRecords
                .FirstOrDefault(m => m.InvoiceLineId == line.Id)
                ?? new ThreeWayMatchRecord
                {
                    InvoiceId     = invoice.Id,
                    InvoiceLineId = line.Id,
                    PurchaseOrderLineId = line.PurchaseOrderLineId,
                    GoodsReceiptLineId  = line.GoodsReceiptLineId,
                    MatchedAt     = DateTime.UtcNow
                };

            matchRecord.InvoicedQuantity = line.Quantity;
            matchRecord.InvoiceUnitPrice = line.UnitPrice;

            var hasVariance = matchRecord.QuantityVariance != 0 || matchRecord.PriceVariance != 0;
            matchRecord.HasDiscrepancy = hasVariance;
            matchRecord.MatchStatus    = hasVariance
                ? InvoiceMatchingStatus.MatchException
                : InvoiceMatchingStatus.FullyMatched;

            if (hasVariance) allMatched = false;

            if (!invoice.MatchRecords.Contains(matchRecord))
                invoice.MatchRecords.Add(matchRecord);
        }

        invoice.MatchingStatus = allMatched
            ? InvoiceMatchingStatus.FullyMatched
            : InvoiceMatchingStatus.MatchException;

        _invoices.Update(invoice);
        await _invoices.SaveChangesAsync();
        _logger.LogInformation("3-way match for invoice {Number}: {Status}",
            invoice.InvoiceNumber, invoice.MatchingStatus);
        return MapToDto(invoice);
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static PurchaseInvoiceDto MapToDto(PurchaseInvoice i) => new()
    {
        Id                      = i.Id,
        InvoiceNumber           = i.InvoiceNumber,
        VendorInvoiceNumber     = i.VendorInvoiceNumber,
        VendorId                = i.VendorId,
        VendorName              = i.VendorName ?? i.Vendor?.Name,
        PurchaseOrderId         = i.PurchaseOrderId,
        PurchaseOrderNumber     = i.PurchaseOrder?.OrderNumber,
        InvoiceDate             = i.InvoiceDate,
        DueDate                 = i.DueDate,
        PostingDate             = i.PostingDate,
        Status                  = i.Status,
        MatchingStatus          = i.MatchingStatus,
        PaymentStatus           = i.PaymentStatus,
        CurrencyCode            = i.CurrencyCode,
        ExchangeRate            = i.ExchangeRate,
        SubTotalAmount          = i.SubTotalAmount,
        TaxAmount               = i.TaxAmount,
        DiscountAmount          = i.DiscountAmount,
        TotalAmount             = i.TotalAmount,
        PaidAmount              = i.PaidAmount,
        OutstandingAmount       = i.OutstandingAmount,
        RecoverableTaxAmount    = i.RecoverableTaxAmount,
        NonRecoverableTaxAmount = i.NonRecoverableTaxAmount,
        PaymentTerms            = i.PaymentTerms,
        AccountingJournalEntryId = i.AccountingJournalEntryId,
        FiscalPeriodId          = i.FiscalPeriodId,
        DimensionSetId          = i.DimensionSetId,
        ApprovedByUserId        = i.ApprovedByUserId,
        ApprovedAt              = i.ApprovedAt,
        PostedByUserId          = i.PostedByUserId,
        PostedAt                = i.PostedAt,
        HoldReason              = i.HoldReason,
        DisputeReason           = i.DisputeReason,
        Notes                   = i.Notes,
        InternalNotes           = i.InternalNotes,
        Lines = i.Lines.Select(l => new PurchaseInvoiceLineDto
        {
            Id                      = l.Id,
            LineNumber              = l.LineNumber,
            PurchaseOrderLineId     = l.PurchaseOrderLineId,
            GoodsReceiptLineId      = l.GoodsReceiptLineId,
            ItemId                  = l.ItemId,
            ItemCode                = l.ItemCode,
            ItemDescription         = l.ItemDescription,
            Quantity                = l.Quantity,
            UnitOfMeasureId         = l.UnitOfMeasureId,
            UnitOfMeasureName       = l.UnitOfMeasureName,
            UnitPrice               = l.UnitPrice,
            DiscountPercent         = l.DiscountPercent,
            DiscountAmount          = l.DiscountAmount,
            TaxCodeId               = l.TaxCodeId,
            TaxPercent              = l.TaxPercent,
            TaxAmount               = l.TaxAmount,
            RecoverableTaxAmount    = l.RecoverableTaxAmount,
            NonRecoverableTaxAmount = l.NonRecoverableTaxAmount,
            SubTotal                = l.SubTotal,
            TotalPrice              = l.TotalPrice,
            ProcurementCategoryId   = l.ProcurementCategoryId,
            LedgerAccountId         = l.LedgerAccountId,
            DimensionSetId          = l.DimensionSetId,
            Notes                   = l.Notes
        }).ToList()
    };
}
