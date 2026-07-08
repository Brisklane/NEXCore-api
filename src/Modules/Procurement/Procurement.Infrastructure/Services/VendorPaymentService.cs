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

public class VendorPaymentService : IVendorPaymentService
{
    private readonly IVendorPaymentRepository _payments;
    private readonly IPurchaseInvoiceRepository _invoices;
    private readonly IDocumentSequenceService _sequences;
    private readonly IEventPublisher _events;
    private readonly ILogger<VendorPaymentService> _logger;

    public VendorPaymentService(
        IVendorPaymentRepository payments,
        IPurchaseInvoiceRepository invoices,
        IDocumentSequenceService sequences,
        IEventPublisher events,
        ILogger<VendorPaymentService> logger)
    {
        _payments  = payments;
        _invoices  = invoices;
        _sequences = sequences;
        _events    = events;
        _logger    = logger;
    }

    public async Task<PaginatedResponse<VendorPaymentDto>> GetAllAsync(PaginationParams pagination)
    {
        var search = pagination.SearchTerm?.Trim().ToLower();
        var (items, total) = await _payments.GetPagedAsync(
            pagination.PageNumber, pagination.PageSize,
            predicate: p =>
                (string.IsNullOrEmpty(search) ||
                    p.PaymentNumber.ToLower().Contains(search) ||
                    (p.VendorName != null && p.VendorName.ToLower().Contains(search))) &&
                (pagination.Status == null || (int)p.Status == pagination.Status),
            orderBy: q => q.ApplyOrderNewestFirst(pagination.SortBy,
                string.IsNullOrWhiteSpace(pagination.SortBy) ? "desc" : pagination.SortDirection, "CreatedAt"));
        return PaginatedResponse<VendorPaymentDto>.Ok(items.Select(MapToDto), total, pagination.PageNumber, pagination.PageSize);
    }

    public async Task<VendorPaymentDto?> GetByIdAsync(Guid id)
    {
        var p = await _payments.GetWithAllocationsAsync(id);
        return p is null ? null : MapToDto(p);
    }

    public async Task<VendorPaymentDto?> GetByNumberAsync(string paymentNumber)
    {
        var p = await _payments.GetByNumberAsync(paymentNumber);
        return p is null ? null : MapToDto(p);
    }

    public async Task<List<VendorPaymentDto>> GetByStatusAsync(VendorPaymentStatus status)
        => (await _payments.GetByStatusAsync(status)).Select(MapToDto).ToList();

    public async Task<List<VendorPaymentDto>> GetByVendorAsync(Guid vendorId)
        => (await _payments.GetByVendorAsync(vendorId)).Select(MapToDto).ToList();

    public async Task<VendorPaymentDto> CreateAsync(CreateVendorPaymentDto dto)
    {
        var payment = new VendorPayment
        {
            PaymentNumber               = await _sequences.GetNextNumberAsync(ProcurementDocumentType.VendorPayment),
            VendorId                    = dto.VendorId,
            PaymentDate                 = dto.PaymentDate,
            ValueDate                   = dto.ValueDate,
            PaymentMethod               = dto.PaymentMethod,
            CompanyBankAccountId        = dto.CompanyBankAccountId,
            VendorBankAccountId         = dto.VendorBankAccountId,
            CurrencyCode                = dto.CurrencyCode,
            ExchangeRate                = dto.ExchangeRate,
            TotalAmount                 = dto.TotalAmount,
            AllocatedAmount             = 0,
            UnallocatedAmount           = dto.TotalAmount,
            BankReferenceNumber         = dto.BankReferenceNumber,
            CheckNumber                 = dto.CheckNumber,
            FiscalPeriodId              = dto.FiscalPeriodId,
            WithholdingTaxAmount        = dto.WithholdingTaxAmount,
            WithholdingTaxLedgerAccountId = dto.WithholdingTaxLedgerAccountId,
            Notes                       = dto.Notes,
            Status                      = VendorPaymentStatus.Draft
        };

        decimal allocatedTotal = 0;
        foreach (var alloc in dto.Allocations)
        {
            var invoice = await _invoices.GetByIdAsync(alloc.InvoiceId)
                ?? throw new KeyNotFoundException($"Invoice {alloc.InvoiceId} not found");

            payment.Allocations.Add(new VendorPaymentLine
            {
                PaymentId                = payment.Id,
                InvoiceId                = alloc.InvoiceId,
                InvoiceTotalAmount       = invoice.TotalAmount,
                InvoiceOutstandingAmount = invoice.OutstandingAmount,
                AllocatedAmount          = alloc.AllocatedAmount,
                DiscountTaken            = alloc.DiscountTaken,
                WriteOffAmount           = alloc.WriteOffAmount,
                FXGainLossLedgerAccountId = alloc.FXGainLossLedgerAccountId,
                Notes                    = alloc.Notes
            });

            allocatedTotal += alloc.AllocatedAmount;
        }

        payment.AllocatedAmount   = allocatedTotal;
        payment.UnallocatedAmount = payment.TotalAmount - allocatedTotal;

        await _payments.AddAsync(payment);
        await _payments.SaveChangesAsync();
        _logger.LogInformation("Created payment {Number} for vendor {VendorId}", payment.PaymentNumber, payment.VendorId);
        return MapToDto(payment);
    }

    public async Task<VendorPaymentDto> UpdateAsync(Guid id, UpdateVendorPaymentDto dto)
    {
        var payment = await _payments.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Payment {id} not found");

        if (payment.Status != VendorPaymentStatus.Draft)
            throw new InvalidOperationException($"Cannot update a payment in status {payment.Status}");

        if (dto.PaymentDate.HasValue)          payment.PaymentDate          = dto.PaymentDate.Value;
        if (dto.ValueDate.HasValue)            payment.ValueDate            = dto.ValueDate;
        if (dto.VendorBankAccountId.HasValue)  payment.VendorBankAccountId  = dto.VendorBankAccountId;
        if (dto.BankReferenceNumber is not null) payment.BankReferenceNumber = dto.BankReferenceNumber;
        if (dto.CheckNumber is not null)       payment.CheckNumber          = dto.CheckNumber;
        if (dto.Notes is not null)             payment.Notes                = dto.Notes;

        _payments.Update(payment);
        await _payments.SaveChangesAsync();
        return MapToDto(payment);
    }

    public async Task<VendorPaymentDto> ApproveAsync(Guid id, Guid approvedByUserId)
    {
        var payment = await _payments.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Payment {id} not found");

        if (payment.Status != VendorPaymentStatus.Draft)
            throw new InvalidOperationException($"Cannot approve a payment in status {payment.Status}");

        payment.Status           = VendorPaymentStatus.Approved;
        payment.ApprovedByUserId = approvedByUserId;
        payment.ApprovedAt       = DateTime.UtcNow;

        _payments.Update(payment);
        await _payments.SaveChangesAsync();
        return MapToDto(payment);
    }

    public async Task<VendorPaymentDto> MarkSentAsync(Guid id, string? transactionReference = null)
    {
        var payment = await _payments.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Payment {id} not found");

        if (payment.Status != VendorPaymentStatus.Approved &&
            payment.Status != VendorPaymentStatus.Processing)
            throw new InvalidOperationException($"Cannot mark as sent a payment in status {payment.Status}");

        payment.Status               = VendorPaymentStatus.Sent;
        payment.TransactionReference = transactionReference ?? payment.TransactionReference;

        _payments.Update(payment);
        await _payments.SaveChangesAsync();
        return MapToDto(payment);
    }

    public async Task<VendorPaymentDto> ClearAsync(Guid id, string? bankReferenceNumber = null)
    {
        var payment = await _payments.GetWithAllocationsAsync(id)
            ?? throw new KeyNotFoundException($"Payment {id} not found");

        payment.Status              = VendorPaymentStatus.Cleared;
        payment.ClearedAt           = DateTime.UtcNow;
        payment.BankReferenceNumber = bankReferenceNumber ?? payment.BankReferenceNumber;

        // Update invoices' paid amounts. Use the allocation's already-tracked Invoice navigation
        // (loaded via GetWithAllocationsAsync) — re-fetching the same invoice and calling Update
        // would attach a second instance with the same key and throw a tracking conflict.
        foreach (var alloc in payment.Allocations)
        {
            var invoice = alloc.Invoice;
            if (invoice is null) continue;

            invoice.PaidAmount        += alloc.AllocatedAmount;
            invoice.OutstandingAmount  = invoice.TotalAmount - invoice.PaidAmount;
            invoice.PaymentStatus = invoice.OutstandingAmount <= 0
                ? InvoicePaymentStatus.FullyPaid
                : InvoicePaymentStatus.PartiallyPaid;

            if (invoice.PaymentStatus == InvoicePaymentStatus.FullyPaid)
                invoice.Status = PurchaseInvoiceStatus.FullyPaid;
            else if (invoice.Status == PurchaseInvoiceStatus.Posted)
                invoice.Status = PurchaseInvoiceStatus.PartiallyPaid;
        }

        // The payment and the tracked invoices share one DbContext — a single save persists both.
        _payments.Update(payment);
        await _payments.SaveChangesAsync();

        // Settle the payable in Accounting (DR Accounts Payable, CR Bank/Cash).
        var amountPaid = payment.Allocations.Sum(a => a.AllocatedAmount);
        if (amountPaid > 0m)
        {
            await _events.PublishAsync(new VendorPaymentClearedEvent
            {
                PaymentId       = payment.Id,
                PaymentNumber   = payment.PaymentNumber,
                VendorId        = payment.VendorId,
                CompanyId       = payment.CompanyId,
                BranchId        = payment.BranchId,
                BusinessUnitId  = payment.BusinessUnitId,
                CreatedByUserId = payment.CreatedByUserId,
                PaymentDate     = payment.PaymentDate,
                AmountPaid      = amountPaid,
                IsCash          = payment.PaymentMethod == VendorPaymentMethod.Cash,
                CurrencyCode    = payment.CurrencyCode,
                ExchangeRate    = payment.ExchangeRate,
            });
        }

        _logger.LogInformation("Payment {Number} cleared", payment.PaymentNumber);
        return MapToDto(payment);
    }

    public async Task<VendorPaymentDto> CancelAsync(Guid id)
    {
        var payment = await _payments.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Payment {id} not found");

        if (payment.Status == VendorPaymentStatus.Cleared)
            throw new InvalidOperationException("Cannot cancel a cleared payment");

        payment.Status = VendorPaymentStatus.Cancelled;
        _payments.Update(payment);
        await _payments.SaveChangesAsync();
        return MapToDto(payment);
    }

    public async Task DeleteAsync(Guid id)
    {
        var payment = await _payments.GetByIdAsync(id)
            ?? throw new KeyNotFoundException($"Payment {id} not found");

        if (payment.Status != VendorPaymentStatus.Draft)
            throw new InvalidOperationException("Only draft payments can be deleted");

        _payments.SoftDelete(payment);
        await _payments.SaveChangesAsync();
    }

    // ── Mapping ───────────────────────────────────────────────────────────────

    private static VendorPaymentDto MapToDto(VendorPayment p) => new()
    {
        Id                           = p.Id,
        PaymentNumber                = p.PaymentNumber,
        VendorId                     = p.VendorId,
        VendorName                   = p.VendorName ?? p.Vendor?.Name,
        PaymentDate                  = p.PaymentDate,
        ValueDate                    = p.ValueDate,
        ClearedAt                    = p.ClearedAt,
        Status                       = p.Status,
        PaymentMethod                = p.PaymentMethod,
        CompanyBankAccountId         = p.CompanyBankAccountId,
        VendorBankAccountId          = p.VendorBankAccountId,
        CurrencyCode                 = p.CurrencyCode,
        ExchangeRate                 = p.ExchangeRate,
        TotalAmount                  = p.TotalAmount,
        AllocatedAmount              = p.AllocatedAmount,
        UnallocatedAmount            = p.UnallocatedAmount,
        BankReferenceNumber          = p.BankReferenceNumber,
        CheckNumber                  = p.CheckNumber,
        TransactionReference         = p.TransactionReference,
        AccountingJournalEntryId     = p.AccountingJournalEntryId,
        FiscalPeriodId               = p.FiscalPeriodId,
        WithholdingTaxAmount         = p.WithholdingTaxAmount,
        WithholdingTaxLedgerAccountId = p.WithholdingTaxLedgerAccountId,
        ApprovedByUserId             = p.ApprovedByUserId,
        ApprovedAt                   = p.ApprovedAt,
        Notes                        = p.Notes,
        Allocations = p.Allocations.Select(a => new VendorPaymentAllocationDto
        {
            Id                        = a.Id,
            InvoiceId                 = a.InvoiceId,
            InvoiceNumber             = a.Invoice?.InvoiceNumber,
            VendorInvoiceNumber       = a.Invoice?.VendorInvoiceNumber,
            InvoiceTotalAmount        = a.InvoiceTotalAmount,
            InvoiceOutstandingAmount  = a.InvoiceOutstandingAmount,
            AllocatedAmount           = a.AllocatedAmount,
            DiscountTaken             = a.DiscountTaken,
            WriteOffAmount            = a.WriteOffAmount,
            FXGainLossAmount          = a.FXGainLossAmount,
            FXGainLossLedgerAccountId = a.FXGainLossLedgerAccountId,
            Notes                     = a.Notes
        }).ToList()
    };
}
