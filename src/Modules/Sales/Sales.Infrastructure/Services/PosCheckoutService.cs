using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;
using Sales.Application.DTOs;
using Sales.Application.Services.Interfaces;
using Sales.Domain.Entities;
using Sales.Domain.Enums;
using Sales.Infrastructure.Persistence;
using Sales.Infrastructure.Repositories.Interfaces;

namespace Sales.Infrastructure.Services;

/// <summary>
/// POS settlement orchestration. Wraps the SAME central Sales mechanism used by the manual flow
/// (SalesOrder → <see cref="ISalesInvoiceService"/> for invoice posting + payment) inside a
/// <see cref="PosTransaction"/>, so a POS sale produces identical order / invoice / payment records.
///
/// On checkout:
///   1. The scanned Draft order is confirmed and invoiced (or an open invoice is reused).
///   2. The invoice is posted via <see cref="ISalesInvoiceService.PostAsync"/> and, for
///      immediate-fulfillment sales, COGS is posted (no later delivery will fire it).
///   3. The tender is registered via <see cref="ISalesInvoiceService.RegisterPaymentAsync"/>.
///   4. A PosTransaction (+ lines + tenders) is written. ReceiptNumber = InvoiceNumber.
///   5. The session totals are updated and a stock-deduction event is fired.
///
/// Partial / credit sales are supported via <see cref="PosCheckoutDto.AllowCredit"/>.
/// </summary>
public class PosCheckoutService : IPosCheckoutService
{
    private readonly ISalesOrderService _orderService;
    private readonly ISalesInvoiceService _invoiceService;
    private readonly ISalesOrderRepository _orders;
    private readonly ISalesInvoiceRepository _invoices;
    private readonly IPosTransactionRepository _transactions;
    private readonly IPosSessionRepository _sessions;
    private readonly IPosStoreRepository _stores;
    private readonly IDocumentSequenceService _sequences;
    private readonly IEventPublisher _events;
    private readonly SalesDbContext _db;
    private readonly ILogger<PosCheckoutService> _logger;

    public PosCheckoutService(
        ISalesOrderService orderService,
        ISalesInvoiceService invoiceService,
        ISalesOrderRepository orders,
        ISalesInvoiceRepository invoices,
        IPosTransactionRepository transactions,
        IPosSessionRepository sessions,
        IPosStoreRepository stores,
        IDocumentSequenceService sequences,
        IEventPublisher events,
        SalesDbContext db,
        ILogger<PosCheckoutService> logger)
    {
        _orderService   = orderService;
        _invoiceService = invoiceService;
        _orders         = orders;
        _invoices       = invoices;
        _transactions   = transactions;
        _sessions       = sessions;
        _stores         = stores;
        _sequences      = sequences;
        _events         = events;
        _db             = db;
        _logger         = logger;
    }

    public async Task<PosCheckoutResultDto> CheckoutAsync(PosCheckoutDto dto)
    {
        if (dto.Tenders is null || dto.Tenders.Count == 0)
            throw new InvalidOperationException("At least one payment tender is required");

        var tendered = dto.Tenders.Sum(t => t.Amount);
        if (tendered <= 0)
            throw new InvalidOperationException("Tendered amount must be positive");

        var order = await _orders.GetWithFullDetailsAsync(dto.SalesOrderId)
            ?? throw new InvalidOperationException("Sales order not found");

        if (order.Status is SalesOrderStatus.PaidAndClosed or SalesOrderStatus.Closed
                          or SalesOrderStatus.Cancelled or SalesOrderStatus.Rejected)
            throw new InvalidOperationException($"Order '{order.OrderNumber}' is already {order.Status} and cannot be settled");

        if (order.Lines.Count == 0)
            throw new InvalidOperationException("Cannot settle an order with no lines");

        // Load POS settings once — used for the stock-enforcement policy here and the GL overrides below.
        var posSettings = await _db.PosSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.BranchId == dto.PosStoreId);

        // ── 0a. Stock availability — block an oversell BEFORE any invoice/GL side effect when the store
        // enforces it. This MUST run before GetOrCreateInvoiceAsync, which posts the invoice and (for
        // immediate sales) COGS journals; otherwise a strict-store shortage that the cashier abandons
        // would strand committed AR/COGS GL entries for a sale that never took payment or moved stock.
        if (!dto.SkipStockCheck)
            await EnforceStockAvailabilityAsync(order, dto, posSettings);

        // ── 0b. Finalize-time reprice (no-op once a payment exists) ─────────────────
        // Catches promotions/prices that changed between scanning and tapping Pay.
        await _orderService.RepriceAsync(order.Id);

        // ── 1. Invoice: reuse an open one, else create + post a fresh one ──────────
        var invoice = await GetOrCreateInvoiceAsync(order, dto);

        if (invoice.BalanceDue <= 0)
            throw new InvalidOperationException($"Invoice '{invoice.InvoiceNumber}' is already fully paid");

        // ── 2. Work out how much of the tender clears the invoice ─────────────────
        var balanceBefore = invoice.BalanceDue;
        var applied = Math.Min(tendered, balanceBefore);
        var change  = tendered - applied;

        if (applied < balanceBefore && !dto.AllowCredit)
            throw new InvalidOperationException(
                $"Tendered amount ({tendered:N2}) does not cover the balance due ({balanceBefore:N2}). " +
                "Enable credit to accept a partial payment.");

        // ── 3. Register the payment via the central mechanism ─────────────────────
        // (POS GL account overrides come from posSettings loaded above.)
        var firstRef = dto.Tenders.FirstOrDefault(t => !string.IsNullOrWhiteSpace(t.ReferenceNumber))?.ReferenceNumber;
        await _invoiceService.RegisterPaymentAsync(invoice.Id, new RegisterInvoicePaymentDto
        {
            PaymentMethod       = DescribeTenders(dto.Tenders),
            Amount              = applied,
            PaymentDate         = DateTime.UtcNow,
            Memo                = dto.Notes,
            ReferenceNumber     = firstRef ?? invoice.InvoiceNumber,
            CashGlAccountNumber = posSettings?.CashGlAccountNumber,
            BankGlAccountNumber = posSettings?.BankGlAccountNumber,
        });
        // RegisterPaymentAsync updates the same tracked invoice & order instances and their balances.

        // ── 4. Write the POS transaction (receipt no = invoice no) ────────────────
        var txSeq = await _sequences.GetNextNumberAsync(DocumentType.PosTransaction);
        var txn = new PosTransaction
        {
            TransactionNumber = txSeq.Code,
            Code              = txSeq.Code,
            CodeInt           = txSeq.CodeInt,
            ReceiptNumber     = invoice.InvoiceNumber,           // POS receipt no = invoice no
            PosSessionId      = dto.PosSessionId,
            PosTerminalId     = dto.PosTerminalId,
            PosStoreId        = dto.PosStoreId,
            PosCashierId      = dto.PosCashierId,
            ContactId         = order.ContactId,
            SalesOrderId      = order.Id,
            TransactionType   = PosTransactionType.Sale,
            Status            = PosTransactionStatus.Completed,
            TransactionDate   = DateTime.UtcNow,
            SubtotalAmount    = order.SubtotalAmount,
            DiscountAmount    = order.DiscountAmount,
            TaxAmount         = order.TaxAmount,
            TotalAmount       = order.TotalAmount,
            TenderedAmount    = tendered,
            ChangeAmount      = change,
            CouponCode        = order.CouponCode,
            CouponDiscountAmount = order.CouponDiscountAmount,
            Notes             = dto.Notes,
            Lines             = order.Lines.OrderBy(l => l.LineNumber).Select((l, i) => new PosTransactionLine
            {
                LineNumber         = i + 1,
                ProductId          = l.ProductId,
                ProductCode        = l.ProductCode,
                ProductName        = l.ProductName,
                VariantId          = l.VariantId,
                VariantName        = l.VariantName,
                Quantity           = l.OrderedQuantity,
                UnitOfMeasure      = l.UnitOfMeasure,
                UnitPrice          = l.UnitPrice,
                DiscountPercentage = l.DiscountPercentage,
                DiscountAmount     = l.DiscountAmount,
                NetUnitPrice       = l.NetUnitPrice,
                LineAmount         = l.LineAmount,
                TaxCategory        = l.TaxCategory,
                TaxRate            = l.TaxRate,
                TaxAmount          = l.TaxAmount,
                TotalAmount        = l.TotalAmount,
                Notes              = l.Notes,
            }).ToList(),
            Payments          = dto.Tenders.Select(t => new PosPayment
            {
                TenderType        = t.TenderType,
                Amount            = t.Amount,
                ReferenceNumber   = t.ReferenceNumber,
                AuthorizationCode = t.AuthorizationCode,
                CardScheme        = t.CardScheme,
                CardLast4         = t.CardLast4,
                IsApproved        = true,
            }).ToList(),
        };
        await _transactions.AddAsync(txn);
        await _transactions.SaveChangesAsync();

        // Link the closing transaction once the order is fully paid
        if (order.Status == SalesOrderStatus.PaidAndClosed)
        {
            order.ClosingPosTransactionId = txn.Id;
            _orders.Update(order);
            await _orders.SaveChangesAsync();
        }

        // ── 5. Update the session running totals ──────────────────────────────────
        await UpdateSessionTotalsAsync(dto.PosSessionId, order, dto.Tenders);

        // ── 6. Deduct stock (AR + payment events already fired by the central service) ──
        await PublishStockDeductionAsync(order, txn);

        _logger.LogInformation(
            "POS checkout complete: txn {TxnNumber} (receipt {Receipt}) for order {OrderNumber} — tendered {Tendered:N2}, applied {Applied:N2}, change {Change:N2}, balance {Balance:N2}",
            txn.TransactionNumber, txn.ReceiptNumber, order.OrderNumber, tendered, applied, change, order.BalanceDue);

        return new PosCheckoutResultDto
        {
            Transaction   = MapToDto(txn),
            SalesOrderId  = order.Id,
            OrderNumber   = order.OrderNumber,
            InvoiceId     = invoice.Id,
            InvoiceNumber = invoice.InvoiceNumber,
            ReceiptNumber = txn.ReceiptNumber!,
            TotalAmount   = order.TotalAmount,
            TenderedAmount = tendered,
            ChangeAmount  = change,
            BalanceDue    = Math.Max(0, order.BalanceDue),
            IsFullyPaid   = order.BalanceDue <= 0,
        };
    }

    // ── Invoice: reuse open / create + post ────────────────────────────────────

    private async Task<SalesInvoice> GetOrCreateInvoiceAsync(SalesOrder order, PosCheckoutDto dto)
    {
        var allInvoices = await _invoices.GetByOrderAsync(order.Id);

        // Reuse an existing payable invoice (lay-away / second payment).
        var payable = allInvoices
            .FirstOrDefault(i => i.Status is InvoiceStatus.Issued or InvoiceStatus.PartiallyPaid && i.BalanceDue > 0);
        if (payable != null)
            return (await _invoices.GetWithLinesAsync(payable.Id))!;

        // Resume a Draft invoice left from a prior checkout attempt that failed after
        // CreateInvoiceFromOrderAsync but before/during PostAsync. At that point
        // InvoicedQuantity on the order lines is already exhausted, so creating a second
        // invoice would throw "No uninvoiced quantities remaining". Just re-post the draft.
        var staleDraft = allInvoices.FirstOrDefault(i => i.Status == InvoiceStatus.Draft);
        if (staleDraft != null)
        {
            _logger.LogInformation(
                "POS checkout resuming stale draft invoice {InvoiceId} for order {OrderId}",
                staleDraft.Id, order.Id);
            await _invoiceService.PostAsync(staleDraft.Id);
            return (await _invoices.GetWithLinesAsync(staleDraft.Id))
                ?? throw new InvalidOperationException("Invoice resume failed");
        }

        // Order must be in an invoiceable state — promote a Draft / Parked POS basket.
        if (order.Status is SalesOrderStatus.Draft or SalesOrderStatus.PosParked)
        {
            order.Status   = SalesOrderStatus.Confirmed;
            order.PlacedAt ??= DateTime.UtcNow;
            _orders.Update(order);
            await _orders.SaveChangesAsync();
        }

        // Build the draft invoice from the order (reuses the tested regular-invoice logic)…
        var draftDto = await _orderService.CreateInvoiceFromOrderAsync(order.Id, new CreateInvoiceFromOrderDto
        {
            InvoiceType = CreateInvoiceType.Regular,
            DueDate     = DateTime.UtcNow,
            Notes       = dto.Notes,
        });

        // …then post it through the central invoice mechanism (publishes the AR event).
        await _invoiceService.PostAsync(draftDto.Id);

        var invoice = await _invoices.GetWithLinesAsync(draftDto.Id)
            ?? throw new InvalidOperationException("Invoice creation failed");

        // For walk-in (immediate) sales no delivery will ever fire COGS — post it now.
        if (order.FulfillmentType == FulfillmentType.Immediate)
            await PublishCogsAsync(order, invoice);

        return invoice;
    }

    // ── POS-specific event publishing (COGS + stock) ───────────────────────────

    private async Task PublishCogsAsync(SalesOrder order, SalesInvoice invoice)
    {
        try
        {
            var lines = new List<SalesAccountingLine>();
            foreach (var ol in order.Lines)
            {
                var gl = await LookupGlAsync(ol.ProductId);
                var unitCost = ol.CostPrice ?? gl.UnitCost;
                lines.Add(new SalesAccountingLine
                {
                    ProductId            = ol.ProductId,
                    ProductCode          = ol.ProductCode,
                    ProductName          = ol.ProductName,
                    Quantity             = ol.OrderedQuantity,
                    UnitPrice            = ol.UnitPrice,
                    UnitCost             = unitCost,
                    LineTotal            = ol.LineAmount,
                    CogsGlAccountId      = gl.CogsAccountId,
                    InventoryGlAccountId = gl.InventoryAccountId,
                });
            }

            await _events.PublishAsync(new SalesCogsPostedEvent
            {
                DeliveryId      = Guid.Empty,
                SalesOrderId    = order.Id,
                SalesInvoiceId  = invoice.Id,
                ShippedAt       = DateTime.UtcNow,
                CurrencyCode    = order.CurrencyCode,
                Lines           = lines,
                CompanyId       = order.CompanyId,
                BranchId        = order.BranchId,
                BusinessUnitId  = order.BusinessUnitId,
                CreatedByUserId = order.CreatedByUserId,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Accounting (COGS) event failed for order {OrderId}", order.Id);
        }
    }

    /// <summary>
    /// Before taking payment, blocks the sale if any line exceeds available stock — but ONLY when the
    /// store enforces it (<see cref="PosSettings.AllowNegativeStock"/> = false). Default is allow + warn,
    /// so a paid sale is never blocked unless the operator opted into strict inventory control.
    /// Fails open: a stock line whose availability cannot be resolved is not blocked.
    /// </summary>
    private async Task EnforceStockAvailabilityAsync(SalesOrder order, PosCheckoutDto dto, PosSettings? posSettings)
    {
        // Default (no settings row, or AllowNegativeStock=true) → oversell allowed, nothing to enforce.
        if (posSettings is null || posSettings.AllowNegativeStock)
            return;

        var store = await _stores.GetByIdAsync(dto.PosStoreId);
        var defaultWarehouseId = store?.DefaultWarehouseId
            ?? order.Lines.Select(l => l.WarehouseId).FirstOrDefault(w => w.HasValue)
            ?? Guid.Empty;

        // Aggregate required quantity per (product, variant, warehouse) so repeated SKUs sum correctly.
        var required = order.Lines
            .GroupBy(l => (l.ProductId, l.VariantId, Wh: l.WarehouseId ?? defaultWarehouseId))
            .Select(g => new
            {
                g.Key.ProductId,
                g.Key.VariantId,
                g.Key.Wh,
                Qty  = g.Sum(x => x.OrderedQuantity),
                Name = g.First().ProductName,
                Uom  = g.First().UnitOfMeasure,
            });

        var shortages = new List<string>();
        foreach (var r in required)
        {
            // We are already in strict mode here. A line with no resolvable warehouse cannot be verified
            // (and the deduction handler would skip it too) — refuse rather than silently complete an
            // untracked, unenforceable sale. This is a deterministic config error, not a transient one.
            if (r.Wh == Guid.Empty)
            {
                shortages.Add($"{r.Name}: no warehouse configured for this POS store — cannot verify stock");
                continue;
            }

            var avail = await LookupStockAsync(r.ProductId, r.VariantId, r.Wh, order.CompanyId);
            if (!avail.Resolved) continue; // fail-open on a transient lookup error / missing handler

            if (avail.QuantityOnHand < r.Qty)
                shortages.Add($"{r.Name} (need {r.Qty:N0} {r.Uom}, {avail.QuantityOnHand:N0} on hand)");
        }

        if (shortages.Count > 0)
            throw new InvalidOperationException(
                "Insufficient stock to complete this sale: " + string.Join("; ", shortages) +
                ". Enable 'Allow negative stock' for this POS to override.");
    }

    /// <summary>Asks Inventory for current on-hand via the TCS request/response event. Fails open.</summary>
    private async Task<StockAvailabilityData> LookupStockAsync(Guid productId, Guid? variantId, Guid warehouseId, Guid companyId)
    {
        try
        {
            var evt = new StockAvailabilityLookupEvent
            {
                ProductId   = productId,
                VariantId   = variantId,
                WarehouseId = warehouseId,
                CompanyId   = companyId,
            };
            await _events.PublishAsync(evt);
            // Handlers run synchronously inside PublishAsync; if none completed the TCS, treat as unknown.
            return evt.Result.Task.IsCompletedSuccessfully
                ? await evt.Result.Task
                : StockAvailabilityData.Unresolved;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Stock availability lookup failed for product {ProductId} — not enforcing", productId);
            return StockAvailabilityData.Unresolved;
        }
    }

    private async Task PublishStockDeductionAsync(SalesOrder order, PosTransaction txn)
    {
        try
        {
            var store = await _stores.GetByIdAsync(txn.PosStoreId);
            var defaultWarehouseId = store?.DefaultWarehouseId
                ?? order.Lines.Select(l => l.WarehouseId).FirstOrDefault(w => w.HasValue)
                ?? Guid.Empty;

            var lines = new List<StockDeductionLine>();
            foreach (var ol in order.Lines)
            {
                var gl = await LookupGlAsync(ol.ProductId);
                lines.Add(new StockDeductionLine
                {
                    ProductId     = ol.ProductId,
                    ProductCode   = ol.ProductCode,
                    VariantId     = ol.VariantId,
                    WarehouseId   = ol.WarehouseId ?? defaultWarehouseId,
                    Quantity      = ol.OrderedQuantity,
                    UnitOfMeasure = ol.UnitOfMeasure,
                    UnitCost      = ol.CostPrice ?? gl.UnitCost,
                });
            }

            await _events.PublishAsync(new PosTransactionCompletedEvent
            {
                PosTransactionId = txn.Id,
                PosStoreId       = txn.PosStoreId,
                WarehouseId      = defaultWarehouseId,
                CompanyId        = order.CompanyId,
                BranchId         = order.BranchId,
                BusinessUnitId   = order.BusinessUnitId,
                CreatedByUserId  = order.CreatedByUserId,
                Lines            = lines,
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stock deduction event failed for POS transaction {TxnId}", txn.Id);
        }
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private readonly Dictionary<Guid, ItemGlData> _glCache = [];

    private async Task<ItemGlData> LookupGlAsync(Guid? productId)
    {
        if (productId is null || productId == Guid.Empty) return ItemGlData.Empty;
        if (_glCache.TryGetValue(productId.Value, out var cached)) return cached;

        try
        {
            var lookup = new ItemGlLookupEvent { ProductId = productId.Value };
            await _events.PublishAsync(lookup);
            var data = await lookup.Result.Task;
            _glCache[productId.Value] = data;
            return data;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "GL lookup failed for product {ProductId} — using defaults", productId);
            return ItemGlData.Empty;
        }
    }

    private async Task UpdateSessionTotalsAsync(Guid sessionId, SalesOrder order, List<PosTenderDto> tenders)
    {
        var session = await _sessions.GetByIdAsync(sessionId);
        if (session is null)
        {
            _logger.LogWarning("POS session {SessionId} not found — totals not updated", sessionId);
            return;
        }

        session.TotalSalesAmount     += order.TotalAmount;
        session.TotalDiscountsAmount += order.DiscountAmount;
        session.TotalTaxAmount       += order.TaxAmount;
        session.NetSalesAmount        = session.TotalSalesAmount - session.TotalRefundsAmount;
        session.TransactionCount     += 1;

        foreach (var t in tenders)
        {
            switch (t.TenderType)
            {
                case PosTenderType.Cash:
                    session.CashCollected += t.Amount; break;
                case PosTenderType.CreditCard:
                case PosTenderType.DebitCard:
                    session.CardCollected += t.Amount; break;
                case PosTenderType.MobileWallet:
                case PosTenderType.QrCode:
                    session.WalletCollected += t.Amount; break;
                default:
                    session.OtherCollected += t.Amount; break;
            }
        }

        _sessions.Update(session);
        await _sessions.SaveChangesAsync();
    }

    private static string DescribeTenders(List<PosTenderDto> tenders) =>
        tenders.Count == 1
            ? MapTenderToMethod(tenders[0].TenderType)
            : string.Join("+", tenders.Select(t => MapTenderToMethod(t.TenderType)).Distinct());

    private static string MapTenderToMethod(PosTenderType type) => type switch
    {
        PosTenderType.Cash         => "Cash",
        PosTenderType.CreditCard   => "Card",
        PosTenderType.DebitCard    => "Card",
        PosTenderType.MobileWallet => "Wallet",
        PosTenderType.QrCode       => "Wallet",
        PosTenderType.GiftCard     => "GiftCard",
        _                          => type.ToString(),
    };

    // ── Reads ──────────────────────────────────────────────────────────────────

    public async Task<PosTransactionDto?> GetByIdAsync(Guid id)
    {
        var txn = await _transactions.GetWithLinesAsync(id);
        return txn is null ? null : MapToDto(txn);
    }

    public async Task<PosTransactionDto?> GetByNumberAsync(string transactionNumber)
    {
        var txn = await _transactions.GetByNumberAsync(transactionNumber);
        if (txn is null) return null;
        return await GetByIdAsync(txn.Id);
    }

    public async Task<List<PosTransactionDto>> GetBySessionAsync(Guid sessionId)
    {
        var list = await _transactions.GetBySessionAsync(sessionId);
        return list.Select(MapToDto).ToList();
    }

    public async Task<List<PosTransactionDto>> GetByDateRangeAsync(DateTime from, DateTime to)
    {
        var list = await _transactions.GetByDateRangeAsync(from, to, Guid.Empty);
        return list.Select(MapToDto).ToList();
    }

    // ── Mapping ────────────────────────────────────────────────────────────────

    private static PosTransactionDto MapToDto(PosTransaction t) => new()
    {
        Id                = t.Id,
        TransactionNumber = t.TransactionNumber,
        ReceiptNumber     = t.ReceiptNumber,
        PosSessionId      = t.PosSessionId,
        PosTerminalId     = t.PosTerminalId,
        PosStoreId        = t.PosStoreId,
        PosCashierId      = t.PosCashierId,
        ContactId         = t.ContactId,
        SalesOrderId      = t.SalesOrderId,
        TransactionType   = t.TransactionType,
        Status            = t.Status,
        TransactionDate   = t.TransactionDate,
        SubtotalAmount    = t.SubtotalAmount,
        DiscountAmount    = t.DiscountAmount,
        TaxAmount         = t.TaxAmount,
        RoundingAmount    = t.RoundingAmount,
        TotalAmount       = t.TotalAmount,
        TenderedAmount    = t.TenderedAmount,
        ChangeAmount      = t.ChangeAmount,
        Notes             = t.Notes,
        Lines = t.Lines.OrderBy(l => l.LineNumber).Select(l => new PosTransactionLineDto
        {
            Id            = l.Id,
            LineNumber    = l.LineNumber,
            ProductId     = l.ProductId,
            ProductCode   = l.ProductCode,
            ProductName   = l.ProductName,
            VariantId     = l.VariantId,
            VariantName   = l.VariantName,
            Quantity      = l.Quantity,
            UnitOfMeasure = l.UnitOfMeasure,
            UnitPrice     = l.UnitPrice,
            DiscountAmount = l.DiscountAmount,
            NetUnitPrice  = l.NetUnitPrice,
            LineAmount    = l.LineAmount,
            TaxCategory   = l.TaxCategory,
            TaxRate       = l.TaxRate,
            TaxAmount     = l.TaxAmount,
            TotalAmount   = l.TotalAmount,
        }).ToList(),
        Payments = t.Payments.Select(p => new PosPaymentDto
        {
            Id                = p.Id,
            TenderType        = p.TenderType,
            Amount            = p.Amount,
            ReferenceNumber   = p.ReferenceNumber,
            AuthorizationCode = p.AuthorizationCode,
            CardScheme        = p.CardScheme,
            CardLast4         = p.CardLast4,
            IsApproved        = p.IsApproved,
        }).ToList(),
    };
}
