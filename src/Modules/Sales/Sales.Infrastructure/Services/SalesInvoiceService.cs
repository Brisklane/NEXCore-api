using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

/// <summary>
/// Central invoice + payment service. Owns the single mechanism for creating, posting, and paying
/// invoices so that the manual Sales flow and the POS checkout flow behave identically.
///
/// Tenant context (company / branch / business unit / user) is read from the persisted entity
/// rather than the HTTP request, so the same code works whether the caller is a controller action
/// or another service (e.g. POS checkout).
/// </summary>
public class SalesInvoiceService : ISalesInvoiceService
{
    private readonly ISalesInvoiceRepository _invoices;
    private readonly ISalesOrderRepository _orders;
    private readonly ISalesOrderLineRepository _orderLines;
    private readonly ISalesPaymentRepository _payments;
    private readonly IPaymentAllocationRepository _allocations;
    private readonly IDocumentSequenceService _sequences;
    private readonly IEventPublisher _events;
    private readonly ILogger<SalesInvoiceService> _logger;

    public SalesInvoiceService(
        ISalesInvoiceRepository invoices,
        ISalesOrderRepository orders,
        ISalesOrderLineRepository orderLines,
        ISalesPaymentRepository payments,
        IPaymentAllocationRepository allocations,
        IDocumentSequenceService sequences,
        IEventPublisher events,
        ILogger<SalesInvoiceService> logger)
    {
        _invoices    = invoices;
        _orders      = orders;
        _orderLines  = orderLines;
        _payments    = payments;
        _allocations = allocations;
        _sequences   = sequences;
        _events      = events;
        _logger      = logger;
    }

    // ── Create (manual draft) ──────────────────────────────────────────────────

    public async Task<SalesInvoiceDto> CreateAsync(CreateSalesInvoiceDto dto)
    {
        var invSeq = await _sequences.GetNextNumberAsync(DocumentType.Invoice);
        var invoice = new SalesInvoice
        {
            InvoiceNumber        = invSeq.Code,
            Code                 = invSeq.Code,
            CodeInt              = invSeq.CodeInt,
            SalesOrderId         = dto.SalesOrderId,
            ContactId            = dto.ContactId,
            DeliveryId           = dto.DeliveryId,
            DueDate              = dto.DueDate,
            CurrencyCode         = dto.CurrencyCode,
            ExchangeRate         = dto.ExchangeRate,
            PaymentTerms         = dto.PaymentTerms,
            BillToName           = dto.BillToName,
            BillToStreet         = dto.BillToStreet,
            BillToCity           = dto.BillToCity,
            BillToState          = dto.BillToState,
            BillToPostalCode     = dto.BillToPostalCode,
            BillToCountry        = dto.BillToCountry,
            Notes                = dto.Notes,
            CustomerReference    = dto.CustomerReference,
            SalesRepId           = dto.SalesRepId,
            SalesRepName         = dto.SalesRepName,
            RecipientBankAccount = dto.RecipientBankAccount,
            DeliveryDate         = dto.DeliveryDate,
            FiscalPositionId     = dto.FiscalPositionId,
            PaymentMethod        = dto.PaymentMethod,
            AutoPost             = dto.AutoPost,
            Status               = InvoiceStatus.Draft,
            PaymentStatus        = InvoicePaymentStatus.NotPaid,
            Lines = dto.Lines.Select((l, i) => new SalesInvoiceLine
            {
                LineNumber       = i + 1,
                SalesOrderLineId = l.SalesOrderLineId,
                ProductId        = l.ProductId,
                ProductCode      = l.ProductCode,
                ProductName      = l.ProductName,
                Quantity         = l.Quantity,
                UnitOfMeasure    = l.UnitOfMeasure,
                UnitPrice        = l.UnitPrice,
                DiscountAmount   = l.DiscountAmount,
                LineAmount       = (l.UnitPrice - l.DiscountAmount) * l.Quantity,
                TaxCategory      = l.TaxCategory,
                TaxRate          = l.TaxRate,
                TaxAmount        = (l.UnitPrice * l.Quantity - l.DiscountAmount) * l.TaxRate / 100m,
                TotalAmount      = (l.UnitPrice * l.Quantity) - l.DiscountAmount
                                       + ((l.UnitPrice * l.Quantity - l.DiscountAmount) * l.TaxRate / 100m),
            }).ToList(),
        };

        invoice.SubtotalAmount = invoice.Lines.Sum(l => l.UnitPrice * l.Quantity);
        invoice.TaxAmount      = invoice.Lines.Sum(l => l.TaxAmount);
        invoice.TotalAmount    = invoice.Lines.Sum(l => l.TotalAmount);
        invoice.BalanceDue     = invoice.TotalAmount;

        await _invoices.AddAsync(invoice);
        await _invoices.SaveChangesAsync();

        await UpdateInvoicedQuantitiesAsync(invoice);

        return MapToDto(invoice);
    }

    // ── Post (confirm) ─────────────────────────────────────────────────────────

    public async Task<SalesInvoiceDto> PostAsync(Guid invoiceId)
    {
        var invoice = await _invoices.GetWithLinesAsync(invoiceId)
            ?? throw new InvalidOperationException("Invoice not found");
        if (invoice.Status != InvoiceStatus.Draft)
            throw new InvalidOperationException("Only Draft invoices can be confirmed");

        invoice.Status            = InvoiceStatus.Issued;
        invoice.InvoiceDate       = DateTime.UtcNow;
        invoice.PaymentReference ??= invoice.InvoiceNumber;   // Odoo "Standard communication"
        _invoices.Update(invoice);
        await _invoices.SaveChangesAsync();

        await PublishInvoicePostedEventAsync(invoice);

        _logger.LogInformation("Invoice {InvoiceNumber} confirmed (Posted)", invoice.InvoiceNumber);
        return MapToDto(invoice);
    }

    // ── Register payment ───────────────────────────────────────────────────────

    public async Task<SalesInvoiceDto> RegisterPaymentAsync(Guid invoiceId, RegisterInvoicePaymentDto dto)
    {
        var invoice = await _invoices.GetWithLinesAsync(invoiceId)
            ?? throw new InvalidOperationException("Invoice not found");
        if (invoice.Status != InvoiceStatus.Issued && invoice.Status != InvoiceStatus.PartiallyPaid)
            throw new InvalidOperationException("Only Posted invoices can be paid");
        if (invoice.BalanceDue <= 0)
            throw new InvalidOperationException("Invoice is already fully paid");

        var amount = dto.Amount ?? invoice.BalanceDue;
        if (amount <= 0)
            throw new InvalidOperationException("Payment amount must be positive");
        if (amount > invoice.BalanceDue)
            throw new InvalidOperationException($"Amount ({amount:N2}) exceeds balance due ({invoice.BalanceDue:N2})");

        var order = await _orders.GetWithLinesAsync(invoice.SalesOrderId)
            ?? throw new InvalidOperationException("Order not found");

        var paySeq = await _sequences.GetNextNumberAsync(DocumentType.Payment);
        var payment = new SalesPayment
        {
            PaymentNumber   = paySeq.Code,
            Code            = paySeq.Code,
            CodeInt         = paySeq.CodeInt,
            SalesOrderId    = invoice.SalesOrderId,
            ContactId       = invoice.ContactId,
            Amount          = amount,
            CurrencyCode    = invoice.CurrencyCode,
            ExchangeRate    = invoice.ExchangeRate,
            PaymentMethod   = dto.PaymentMethod,
            ReferenceNumber = dto.ReferenceNumber ?? dto.Memo ?? invoice.InvoiceNumber,
            BankName        = dto.BankName,
            PaymentDate     = dto.PaymentDate == default ? DateTime.UtcNow : dto.PaymentDate,
            ConfirmedAt     = DateTime.UtcNow,
            Notes           = dto.Memo,
        };
        await _payments.AddAsync(payment);
        await _payments.SaveChangesAsync();

        await _allocations.AddAsync(new PaymentAllocation
        {
            SalesPaymentId  = payment.Id,
            SalesInvoiceId  = invoice.Id,
            AllocatedAmount = amount,
            AllocatedAt     = DateTime.UtcNow,
            Notes           = dto.Memo,
        });

        // Update invoice payment state
        invoice.PaidAmount += amount;
        invoice.BalanceDue  = invoice.TotalAmount - invoice.PaidAmount;
        if (invoice.BalanceDue <= 0)
        {
            invoice.Status        = InvoiceStatus.Paid;
            invoice.PaymentStatus = InvoicePaymentStatus.Paid;
        }
        else
        {
            invoice.Status        = InvoiceStatus.PartiallyPaid;
            invoice.PaymentStatus = InvoicePaymentStatus.Partial;
        }
        _invoices.Update(invoice);

        // Update order financials
        order.PaidAmount += amount;
        order.BalanceDue  = order.TotalAmount - order.PaidAmount;
        if (order.BalanceDue <= 0)
        {
            order.Status     = SalesOrderStatus.PaidAndClosed;
            order.ClosedDate = DateTime.UtcNow;
        }
        else if (order.PaidAmount > 0)
        {
            order.Status = SalesOrderStatus.PartiallyPaid;
        }
        _orders.Update(order);

        await _allocations.SaveChangesAsync();

        await PublishPaymentEventAsync(payment, invoice, amount,
            dto.CashGlAccountNumber, dto.BankGlAccountNumber);

        _logger.LogInformation(
            "Payment {PaymentNumber} ({Amount:N2}) registered against invoice {InvoiceNumber}",
            payment.PaymentNumber, amount, invoice.InvoiceNumber);

        return MapToDto(invoice);
    }

    // ── Private helpers (moved from SalesInvoiceController) ─────────────────────

    private async Task UpdateInvoicedQuantitiesAsync(SalesInvoice invoice)
    {
        try
        {
            foreach (var line in invoice.Lines.Where(l => l.SalesOrderLineId.HasValue))
            {
                var orderLine = await _orderLines.GetByIdAsync(line.SalesOrderLineId!.Value);
                if (orderLine == null) continue;
                orderLine.InvoicedQuantity += line.Quantity;
            }

            var order = await _orders.GetWithLinesAsync(invoice.SalesOrderId);
            if (order != null)
            {
                RecalculateOrderInvoiceStatus(order);
                _orders.Update(order);
            }

            await _orders.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update invoiced quantities for invoice {Id}", invoice.Id);
        }
    }

    private static void RecalculateOrderInvoiceStatus(SalesOrder order)
    {
        if (!order.Lines.Any() ||
            order.Status == SalesOrderStatus.Draft ||
            order.Status == SalesOrderStatus.Cancelled)
        {
            order.InvoiceStatus = OrderInvoiceStatus.NothingToInvoice;
            return;
        }

        var invoicedLines = order.Lines.Count(l => l.InvoicedQuantity >= l.OrderedQuantity);
        var partialLines  = order.Lines.Count(l => l.InvoicedQuantity > 0 && l.InvoicedQuantity < l.OrderedQuantity);
        var totalLines    = order.Lines.Count;

        order.InvoiceStatus = invoicedLines == totalLines
            ? OrderInvoiceStatus.FullyInvoiced
            : (invoicedLines > 0 || partialLines > 0)
                ? OrderInvoiceStatus.PartiallyInvoiced
                : OrderInvoiceStatus.ToInvoice;
    }

    private async Task PublishInvoicePostedEventAsync(SalesInvoice invoice)
    {
        try
        {
            var accountingLines = new List<SalesAccountingLine>();
            foreach (var l in invoice.Lines)
            {
                ItemGlData glData = ItemGlData.Empty;
                if (l.ProductId != null && l.ProductId != Guid.Empty)
                {
                    var lookup = new ItemGlLookupEvent { ProductId = l.ProductId.Value };
                    await _events.PublishAsync(lookup);
                    glData = await lookup.Result.Task;
                }

                var lineNet = (l.UnitPrice * l.Quantity) - l.DiscountAmount;
                accountingLines.Add(new SalesAccountingLine
                {
                    ProductId            = l.ProductId ?? Guid.Empty,
                    ProductCode          = l.ProductCode,
                    ProductName          = l.ProductName,
                    Quantity             = l.Quantity,
                    UnitPrice            = l.UnitPrice,
                    UnitCost             = 0m,
                    DiscountAmount       = l.DiscountAmount,
                    TaxAmount            = l.TaxAmount,
                    LineTotal            = lineNet,
                    SalesGlAccountId     = glData.SalesAccountId,
                    CogsGlAccountId      = glData.CogsAccountId,
                    InventoryGlAccountId = glData.InventoryAccountId,
                });
            }

            await _events.PublishAsync(new SalesInvoicePostedEvent
            {
                InvoiceId       = invoice.Id,
                InvoiceNumber   = invoice.InvoiceNumber,
                SalesOrderId    = invoice.SalesOrderId,
                ContactId       = invoice.ContactId,
                ContactName     = invoice.ContactName,
                SubtotalAmount  = invoice.SubtotalAmount,
                TaxAmount       = invoice.TaxAmount,
                ShippingAmount  = invoice.ShippingAmount,
                TotalAmount     = invoice.TotalAmount,
                CurrencyCode    = invoice.CurrencyCode,
                ExchangeRate    = invoice.ExchangeRate,
                InvoiceDate     = invoice.InvoiceDate,
                DueDate         = invoice.DueDate,
                Lines           = accountingLines,
                CompanyId       = invoice.CompanyId,
                BranchId        = invoice.BranchId,
                BusinessUnitId  = invoice.BusinessUnitId,
                CreatedByUserId = invoice.CreatedByUserId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Accounting event failed for Invoice {InvoiceId}", invoice.Id);
        }
    }

    private async Task PublishPaymentEventAsync(
        SalesPayment payment, SalesInvoice invoice, decimal amount,
        string? cashGlAccountNumber = null, string? bankGlAccountNumber = null)
    {
        try
        {
            await _events.PublishAsync(new SalesPaymentReceivedEvent
            {
                PaymentId          = payment.Id,
                PaymentNumber      = payment.PaymentNumber,
                SalesOrderId       = payment.SalesOrderId,
                ContactId          = payment.ContactId,
                AmountPaid         = payment.Amount,
                PaymentMethod      = payment.PaymentMethod ?? "Unknown",
                CurrencyCode       = payment.CurrencyCode,
                ExchangeRate       = payment.ExchangeRate,
                PaymentDate        = payment.PaymentDate,
                InvoiceAllocations = [new PaymentInvoiceAllocation(invoice.Id, amount)],
                CompanyId          = invoice.CompanyId,
                BranchId           = invoice.BranchId,
                BusinessUnitId     = invoice.BusinessUnitId,
                CreatedByUserId    = invoice.CreatedByUserId,
                CashAccountNumber  = cashGlAccountNumber,
                BankAccountNumber  = bankGlAccountNumber,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Accounting event failed for Payment {PaymentId}", payment.Id);
        }
    }

    // ── Mapping (shared with SalesInvoiceController read endpoints) ─────────────

    public static SalesInvoiceDto MapToDto(SalesInvoice i) => new()
    {
        Id               = i.Id,
        InvoiceNumber    = i.InvoiceNumber,
        SalesOrderId     = i.SalesOrderId,
        ContactId        = i.ContactId,
        ContactName      = i.ContactName,
        DeliveryId       = i.DeliveryId,
        Status           = i.Status,
        PaymentStatus    = i.PaymentStatus,
        InvoiceDate      = i.InvoiceDate,
        DueDate          = i.DueDate,
        CurrencyCode     = i.CurrencyCode,
        ExchangeRate     = i.ExchangeRate,
        PaymentTerms     = i.PaymentTerms,
        SubtotalAmount   = i.SubtotalAmount,
        DiscountAmount   = i.DiscountAmount,
        TaxAmount        = i.TaxAmount,
        ShippingAmount   = i.ShippingAmount,
        TotalAmount      = i.TotalAmount,
        PaidAmount       = i.PaidAmount,
        BalanceDue       = i.BalanceDue,
        BillToName       = i.BillToName,
        BillToStreet     = i.BillToStreet,
        BillToCity       = i.BillToCity,
        BillToState      = i.BillToState,
        BillToPostalCode = i.BillToPostalCode,
        BillToCountry    = i.BillToCountry,
        CustomerReference    = i.CustomerReference,
        SalesRepId           = i.SalesRepId,
        SalesRepName         = i.SalesRepName,
        PaymentReference     = i.PaymentReference,
        RecipientBankAccount = i.RecipientBankAccount,
        DeliveryDate         = i.DeliveryDate,
        FiscalPositionId     = i.FiscalPositionId,
        PaymentMethod        = i.PaymentMethod,
        AutoPost             = i.AutoPost,
        LastReminderDate     = i.LastReminderDate,
        ReminderCount        = i.ReminderCount,
        AccountingJournalEntryId = i.AccountingJournalEntryId,
        Notes            = i.Notes,
        Lines = i.Lines.Select(l => new SalesInvoiceLineDto
        {
            Id               = l.Id,
            LineNumber       = l.LineNumber,
            SalesOrderLineId = l.SalesOrderLineId ?? Guid.Empty,
            ProductId        = l.ProductId ?? Guid.Empty,
            ProductCode      = l.ProductCode,
            ProductName      = l.ProductName,
            Quantity         = l.Quantity,
            UnitOfMeasure    = l.UnitOfMeasure,
            UnitPrice        = l.UnitPrice,
            DiscountAmount   = l.DiscountAmount,
            LineAmount       = l.LineAmount,
            TaxCategory      = l.TaxCategory,
            TaxRate          = l.TaxRate,
            TaxAmount        = l.TaxAmount,
            TotalAmount      = l.TotalAmount,
            IsDownPayment    = l.IsDownPayment,
        }).ToList(),
    };
}
