using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Enums;
using Nexcore.SharedKernel.Events;

namespace Accounting.Infrastructure.Events;

/// <summary>
/// Posts GL journal entries for all sales-side financial events. Posting rules:
///
///   SalesInvoicePostedEvent (credit sale):
///     DR AR (1121)  |  CR Item.SalesAccountId per line (revenue)  |  CR Tax Payable (2141)
///   SalesCogsPostedEvent (delivery shipped):
///     DR Item.CogsAccountId per line (expense)  |  CR Item.InventoryAccountId per line (asset)
///   SalesPaymentReceivedEvent (cash / card / transfer):
///     DR Cash/Bank (1111/1112)  |  CR AR (1121)
///   PosTransactionAccountingEvent (POS immediate sale):
///     DR Cash/Bank (gross)  |  CR Item.SalesAccountId per line (revenue)  |  CR Tax Payable (2141)
///     DR Item.CogsAccountId per line (COGS)  |  CR Item.InventoryAccountId per line (inventory)
///
/// Item-level GL accounts come from the Inventory master (Item.SalesAccountId /
/// CogsAccountId / InventoryAccountId); falls back to standard account numbers when null.
/// </summary>
public class SalesAccountingEventHandler :
    IEventHandler<SalesInvoicePostedEvent>,
    IEventHandler<SalesCogsPostedEvent>,
    IEventHandler<SalesPaymentReceivedEvent>,
    IEventHandler<PosTransactionAccountingEvent>
{
    private readonly AccountingDbContext _ctx;
    private readonly ILogger<SalesAccountingEventHandler> _logger;

    public SalesAccountingEventHandler(
        AccountingDbContext ctx,
        ILogger<SalesAccountingEventHandler> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    // Credit invoice - AR posting

    public async Task HandleAsync(SalesInvoicePostedEvent e, CancellationToken ct = default)
    {
        _logger.LogInformation("Accounting: posting Sales Invoice {InvoiceNumber}", e.InvoiceNumber);

        var ledger = await DefaultLedgerAsync(e.CompanyId, ct);
        if (ledger is null) return;

        // Fallback accounts
        var arAccountId  = await ResolveAccountAsync(e.CompanyId, null, "1121", ct);   // AR Trade
        var taxAccountId = await ResolveAccountAsync(e.CompanyId, null, "2141", ct);   // Tax Payable
        var fallbackRevenueId = await FallbackRevenueAccountAsync(e.CompanyId, ct);

        if (arAccountId == Guid.Empty)
        {
            _logger.LogWarning("Accounting: AR account not found for Company:{CompanyId} - skipping Invoice {InvoiceNumber}", e.CompanyId, e.InvoiceNumber);
            return;
        }

        var journalNumber = $"INV-{e.InvoiceDate:yyyyMMdd}-{e.InvoiceId.ToString("N")[..8].ToUpper()}";
        var entry = BuildHeader(ledger.Id, journalNumber, "SalesInvoice",
            $"Sales Invoice {e.InvoiceNumber} - {e.ContactName ?? e.ContactId?.ToString() ?? "Walk-in"}",
            e.InvoiceId.ToString(), e.CompanyId, e.BranchId, e.BusinessUnitId, e.CreatedByUserId,
            e.InvoiceDate, e.TotalAmount, e.CurrencyCode, e.ExchangeRate);

        int seq = 1;

        // DR Accounts Receivable = gross total (one line)
        entry.Lines.Add(Line(entry.Id, seq++, arAccountId,
            debit: e.TotalAmount, credit: 0m,
            $"AR - Invoice {e.InvoiceNumber}",
            ModuleType.Sales, EntityType.SalesInvoice, e.InvoiceId,
            e.CompanyId, e.BranchId, e.BusinessUnitId,
            customerId: e.ContactId, dueDate: e.DueDate));

        // CR Sales Revenue per product line ? item-specific account
        foreach (var l in e.Lines)
        {
            var revenueAccountId = await ResolveAccountAsync(e.CompanyId, l.SalesGlAccountId, "4111", ct, fallbackRevenueId);
            entry.Lines.Add(Line(entry.Id, seq++, revenueAccountId,
                debit: 0m, credit: l.LineTotal,
                $"Revenue - {l.ProductCode} - {l.Quantity}",
                ModuleType.Sales, EntityType.SalesInvoice, e.InvoiceId,
                e.CompanyId, e.BranchId, e.BusinessUnitId));
        }

        // CR Tax Payable = total tax (one line, only when tax exists)
        if (e.TaxAmount > 0m && taxAccountId != Guid.Empty)
        {
            entry.Lines.Add(Line(entry.Id, seq++, taxAccountId,
                debit: 0m, credit: e.TaxAmount,
                $"Tax - Invoice {e.InvoiceNumber}",
                ModuleType.Sales, EntityType.SalesInvoice, e.InvoiceId,
                e.CompanyId, e.BranchId, e.BusinessUnitId));
        }

        // CR Freight / Delivery Income = shipping charged to the customer (account 4305)
        if (e.ShippingAmount > 0m)
        {
            var freightAccountId = await ResolveAccountAsync(e.CompanyId, null, "4305", ct, fallbackRevenueId);
            if (freightAccountId != Guid.Empty)
                entry.Lines.Add(Line(entry.Id, seq, freightAccountId,
                    debit: 0m, credit: e.ShippingAmount,
                    $"Freight / delivery - Invoice {e.InvoiceNumber}",
                    ModuleType.Sales, EntityType.SalesInvoice, e.InvoiceId,
                    e.CompanyId, e.BranchId, e.BusinessUnitId));
        }

        _ctx.JournalEntries.Add(entry);
        await _ctx.SaveChangesAsync(ct);

        _logger.LogInformation("Accounting: Journal {JournalNumber} posted for Invoice {InvoiceNumber}", journalNumber, e.InvoiceNumber);
    }

    // COGS entry on delivery shipment

    public async Task HandleAsync(SalesCogsPostedEvent e, CancellationToken ct = default)
    {
        _logger.LogInformation("Accounting: posting COGS for Delivery {DeliveryId}", e.DeliveryId);

        if (!e.Lines.Any())
        {
            _logger.LogWarning("Accounting: COGS event for Delivery {DeliveryId} has no lines - skipping", e.DeliveryId);
            return;
        }

        var ledger = await DefaultLedgerAsync(e.CompanyId, ct);
        if (ledger is null) return;

        var fallbackCogsId      = await AccountByNumberAsync(e.CompanyId, "5104", ct); // Cost of Inventory Sold
        var fallbackInventoryId = await AccountByNumberAsync(e.CompanyId, "1143", ct); // Finished Goods

        decimal totalCost = e.Lines.Sum(l => l.Quantity * l.UnitCost);

        var journalNumber = $"COGS-{e.ShippedAt:yyyyMMdd}-{e.DeliveryId.ToString("N")[..8].ToUpper()}";
        var entry = BuildHeader(ledger.Id, journalNumber, "SalesCOGS",
            $"COGS - Delivery {e.DeliveryId.ToString("N")[..8]}",
            e.DeliveryId.ToString(), e.CompanyId, e.BranchId, e.BusinessUnitId, e.CreatedByUserId,
            e.ShippedAt, totalCost, e.CurrencyCode);

        int seq = 1;
        foreach (var l in e.Lines)
        {
            var lineCost = l.Quantity * l.UnitCost;
            if (lineCost == 0m) continue;

            var cogsAccountId      = await ResolveAccountAsync(e.CompanyId, l.CogsGlAccountId,      "5104", ct, fallbackCogsId);
            var inventoryAccountId = await ResolveAccountAsync(e.CompanyId, l.InventoryGlAccountId, "1143", ct, fallbackInventoryId);

            // DR COGS
            entry.Lines.Add(Line(entry.Id, seq++, cogsAccountId,
                debit: lineCost, credit: 0m,
                $"COGS - {l.ProductCode} - {l.Quantity}",
                ModuleType.Sales, EntityType.SalesInvoice, e.DeliveryId,
                e.CompanyId, e.BranchId, e.BusinessUnitId));

            // CR Inventory
            entry.Lines.Add(Line(entry.Id, seq++, inventoryAccountId,
                debit: 0m, credit: lineCost,
                $"Inventory relief - {l.ProductCode} - {l.Quantity}",
                ModuleType.Sales, EntityType.GoodsReceipt, e.DeliveryId,
                e.CompanyId, e.BranchId, e.BusinessUnitId));
        }

        if (!entry.Lines.Any())
        {
            _logger.LogWarning("Accounting: No postable COGS lines for Delivery {DeliveryId}", e.DeliveryId);
            return;
        }

        _ctx.JournalEntries.Add(entry);
        await _ctx.SaveChangesAsync(ct);

        _logger.LogInformation("Accounting: Journal {JournalNumber} posted for COGS Delivery {DeliveryId}", journalNumber, e.DeliveryId);
    }

    // Payment received - AR clearing

    public async Task HandleAsync(SalesPaymentReceivedEvent e, CancellationToken ct = default)
    {
        _logger.LogInformation("Accounting: posting Payment {PaymentNumber}", e.PaymentNumber);

        var ledger = await DefaultLedgerAsync(e.CompanyId, ct);
        if (ledger is null) return;

        // Cash methods debit Cash-on-Hand; card/bank/mobile methods debit Bank Account
        bool isCash = e.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)
                   || e.PaymentMethod.Equals("COD",  StringComparison.OrdinalIgnoreCase);

        var profile = await ProfileAsync(e.CompanyId, "Sales", "SalesPayment", ct);

        // Resolve debit account: event overrides > PostingProfile > hardcoded fallback
        // Always differentiate cash vs non-cash regardless of PostingProfile.
        Guid debitAccountId;
        var cashAcctNum = e.CashAccountNumber ?? (isCash ? "1111" : null);
        var bankAcctNum = e.BankAccountNumber ?? "1112";

        if (isCash)
        {
            debitAccountId = cashAcctNum is not null
                ? await AccountByNumberAsync(e.CompanyId, cashAcctNum, ct)
                : Guid.Empty;
            if (debitAccountId == Guid.Empty && profile is not null)
                debitAccountId = profile.DebitAccountId;
            if (debitAccountId == Guid.Empty)
                debitAccountId = await AccountByNumberAsync(e.CompanyId, "1111", ct);
        }
        else
        {
            debitAccountId = await AccountByNumberAsync(e.CompanyId, bankAcctNum, ct);
            if (debitAccountId == Guid.Empty && profile is not null)
                debitAccountId = profile.DebitAccountId;
            if (debitAccountId == Guid.Empty)
                debitAccountId = await AccountByNumberAsync(e.CompanyId, "1112", ct);
        }

        var arAccountId = profile?.CreditAccountId != null && profile.CreditAccountId != Guid.Empty
            ? profile.CreditAccountId
            : await AccountByNumberAsync(e.CompanyId, "1121", ct);

        if (debitAccountId == Guid.Empty || arAccountId == Guid.Empty)
        {
            _logger.LogWarning("Accounting: GL accounts not found for Payment {PaymentNumber} - skipping", e.PaymentNumber);
            return;
        }

        var journalNumber = $"PMT-{e.PaymentDate:yyyyMMdd}-{e.PaymentId.ToString("N")[..8].ToUpper()}";
        var entry = BuildHeader(ledger.Id, journalNumber, "SalesPayment",
            $"Customer Payment {e.PaymentNumber} via {e.PaymentMethod}",
            e.PaymentId.ToString(), e.CompanyId, e.BranchId, e.BusinessUnitId, e.CreatedByUserId,
            e.PaymentDate, e.AmountPaid, e.CurrencyCode, e.ExchangeRate);

        entry.Lines.Add(Line(entry.Id, 1, debitAccountId,
            debit: e.AmountPaid, credit: 0m,
            $"Cash receipt - {e.PaymentNumber}",
            ModuleType.Sales, EntityType.PaymentReceipt, e.PaymentId,
            e.CompanyId, e.BranchId, e.BusinessUnitId, customerId: e.ContactId));

        entry.Lines.Add(Line(entry.Id, 2, arAccountId,
            debit: 0m, credit: e.AmountPaid,
            $"AR clearing - {e.PaymentNumber}",
            ModuleType.Sales, EntityType.PaymentReceipt, e.PaymentId,
            e.CompanyId, e.BranchId, e.BusinessUnitId, customerId: e.ContactId));

        _ctx.JournalEntries.Add(entry);
        await _ctx.SaveChangesAsync(ct);

        _logger.LogInformation("Accounting: Journal {JournalNumber} posted for Payment {PaymentNumber}", journalNumber, e.PaymentNumber);
    }

    // POS immediate sale - compound entry (revenue + COGS)

    public async Task HandleAsync(PosTransactionAccountingEvent e, CancellationToken ct = default)
    {
        _logger.LogInformation("Accounting: posting POS Transaction {TxId}", e.PosTransactionId);

        var ledger = await DefaultLedgerAsync(e.CompanyId, ct);
        if (ledger is null) return;

        bool isCash = e.PaymentMethod.Equals("Cash", StringComparison.OrdinalIgnoreCase)
                   || e.PaymentMethod.Equals("COD",  StringComparison.OrdinalIgnoreCase);

        var cashAccountId   = isCash
            ? await AccountByNumberAsync(e.CompanyId, "1111", ct)
            : await AccountByNumberAsync(e.CompanyId, "1112", ct);
        var taxAccountId    = await AccountByNumberAsync(e.CompanyId, "2141", ct);
        var fallbackRevenueId   = await FallbackRevenueAccountAsync(e.CompanyId, ct);
        var fallbackCogsId      = await AccountByNumberAsync(e.CompanyId, "5104", ct);
        var fallbackInventoryId = await AccountByNumberAsync(e.CompanyId, "1143", ct);

        if (cashAccountId == Guid.Empty)
        {
            _logger.LogWarning("Accounting: Cash account not found for POS Tx {TxId} - skipping", e.PosTransactionId);
            return;
        }

        var journalNumber = $"POS-{e.CompletedAt:yyyyMMdd}-{e.PosTransactionId.ToString("N")[..8].ToUpper()}";
        var entry = BuildHeader(ledger.Id, journalNumber, "POSSale",
            $"POS Sale - {e.PosTransactionId}",
            e.PosTransactionId.ToString(), e.CompanyId, e.BranchId, e.BusinessUnitId, e.CreatedByUserId,
            e.CompletedAt, e.TotalAmount, e.CurrencyCode);

        int seq = 1;

        // Revenue side
        // DR Cash / Bank = gross total
        entry.Lines.Add(Line(entry.Id, seq++, cashAccountId,
            debit: e.TotalAmount, credit: 0m,
            "POS cash / card receipt",
            ModuleType.Sales, EntityType.General, e.PosTransactionId,
            e.CompanyId, e.BranchId, e.BusinessUnitId, customerId: e.ContactId));

        // CR Revenue per item line
        foreach (var l in e.Lines)
        {
            var revenueAccountId = await ResolveAccountAsync(e.CompanyId, l.SalesGlAccountId, "4111", ct, fallbackRevenueId);
            entry.Lines.Add(Line(entry.Id, seq++, revenueAccountId,
                debit: 0m, credit: l.LineTotal,
                $"POS Revenue - {l.ProductCode} - {l.Quantity}",
                ModuleType.Sales, EntityType.General, e.PosTransactionId,
                e.CompanyId, e.BranchId, e.BusinessUnitId));
        }

        // CR Tax Payable
        if (e.TaxAmount > 0m && taxAccountId != Guid.Empty)
        {
            entry.Lines.Add(Line(entry.Id, seq++, taxAccountId,
                debit: 0m, credit: e.TaxAmount,
                "POS tax collected",
                ModuleType.Sales, EntityType.General, e.PosTransactionId,
                e.CompanyId, e.BranchId, e.BusinessUnitId));
        }

        // COGS side
        foreach (var l in e.Lines)
        {
            var lineCost = l.Quantity * l.UnitCost;
            if (lineCost == 0m) continue;

            var cogsAccountId      = await ResolveAccountAsync(e.CompanyId, l.CogsGlAccountId,      "5104", ct, fallbackCogsId);
            var inventoryAccountId = await ResolveAccountAsync(e.CompanyId, l.InventoryGlAccountId, "1143", ct, fallbackInventoryId);

            entry.Lines.Add(Line(entry.Id, seq++, cogsAccountId,
                debit: lineCost, credit: 0m,
                $"POS COGS - {l.ProductCode} - {l.Quantity}",
                ModuleType.Sales, EntityType.General, e.PosTransactionId,
                e.CompanyId, e.BranchId, e.BusinessUnitId));

            entry.Lines.Add(Line(entry.Id, seq++, inventoryAccountId,
                debit: 0m, credit: lineCost,
                $"POS Inventory relief - {l.ProductCode} - {l.Quantity}",
                ModuleType.Sales, EntityType.General, e.PosTransactionId,
                e.CompanyId, e.BranchId, e.BusinessUnitId));
        }

        _ctx.JournalEntries.Add(entry);
        await _ctx.SaveChangesAsync(ct);

        _logger.LogInformation("Accounting: Journal {JournalNumber} posted for POS Tx {TxId}", journalNumber, e.PosTransactionId);
    }

    // Private helpers

    private async Task<Ledger?> DefaultLedgerAsync(Guid companyId, CancellationToken ct)
    {
        var ledger = await _ctx.Ledgers
            .FirstOrDefaultAsync(l => l.CompanyId == companyId && l.IsDefault, ct);

        if (ledger is null)
            _logger.LogWarning("Accounting: No default ledger for Company:{CompanyId} - skipping posting", companyId);

        return ledger;
    }

    private Task<PostingProfile?> ProfileAsync(Guid companyId, string module, string txType, CancellationToken ct) =>
        _ctx.PostingProfiles.FirstOrDefaultAsync(
            p => p.CompanyId == companyId && p.ModuleName == module
              && p.TransactionType == txType && p.IsActive, ct);

    /// <summary>
    /// Returns <paramref name="preferredId"/> when it is non-null and belongs to an active
    /// account in this company. Falls back to account-number lookup then to
    /// <paramref name="secondaryFallback"/>.
    /// </summary>
    private async Task<Guid> ResolveAccountAsync(
        Guid companyId,
        Guid? preferredId,
        string fallbackAccountNumber,
        CancellationToken ct,
        Guid secondaryFallback = default)
    {
        if (preferredId.HasValue && preferredId.Value != Guid.Empty)
        {
            // Validate the account still exists, belongs to this company, and is a posting-allowed
            // leaf — never post to a non-posting summary/parent account even if an item/category
            // links to one. Otherwise fall through to the standard posting fallback.
            var usable = await _ctx.LedgerAccounts
                .AnyAsync(a => a.Id == preferredId.Value && a.CompanyId == companyId
                            && a.IsActive && a.IsPostingAllowed, ct);
            if (usable) return preferredId.Value;
        }

        var byNumber = await AccountByNumberAsync(companyId, fallbackAccountNumber, ct);
        if (byNumber != Guid.Empty) return byNumber;

        return secondaryFallback;
    }

    private async Task<Guid> AccountByNumberAsync(Guid companyId, string number, CancellationToken ct)
    {
        var id = await _ctx.LedgerAccounts
            .Where(a => a.CompanyId == companyId && a.AccountNumber == number && a.IsActive)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);
        return id ?? Guid.Empty;
    }

    /// <summary>
    /// Finds the first active posting-allowed revenue account for the company.
    /// Used as the ultimate fallback when an item has no SalesAccountId and
    /// account "4111" does not exist.
    /// </summary>
    private async Task<Guid> FallbackRevenueAccountAsync(Guid companyId, CancellationToken ct)
    {
        var id = await _ctx.LedgerAccounts
            .Where(a => a.CompanyId == companyId
                     && a.IsActive
                     && a.IsPostingAllowed
                     && a.AccountNumber.StartsWith("4"))
            .OrderBy(a => a.AccountNumber)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);
        return id ?? Guid.Empty;
    }

    // Journal entry builders

    private static JournalEntry BuildHeader(
        Guid ledgerId, string journalNumber, string documentType,
        string description, string sourceRef,
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId,
        DateTime postingDate, decimal total,
        string currencyCode, decimal exchangeRate = 1m) => new()
    {
        Id                = Guid.NewGuid(),
        CompanyId         = companyId,
        BranchId          = branchId,
        BusinessUnitId    = businessUnitId,
        CreatedByUserId   = userId,
        CreatedAt         = DateTime.UtcNow,
        LedgerId          = ledgerId,
        JournalNumber     = journalNumber,
        DocumentType      = documentType,
        Description       = description,
        SourceSystem      = "Sales",
        SourceReferenceId = sourceRef,
        PostingDate       = postingDate,
        DocumentDate      = postingDate,
        CurrencyCode      = currencyCode,
        ExchangeRate      = exchangeRate,
        TotalDebit        = total,
        TotalCredit       = total,
        Status            = JournalEntryStatus.Posted,
        PostedByUserId    = userId,
        PostedAt          = DateTime.UtcNow,
        Lines             = new List<JournalLine>(),
    };

    private static JournalLine Line(
        Guid journalEntryId, int lineNumber, Guid ledgerAccountId,
        decimal debit, decimal credit, string description,
        ModuleType sourceModule, EntityType sourceEntityType, Guid? sourceEntityId,
        Guid companyId, Guid branchId, Guid businessUnitId,
        Guid? customerId = null, DateTime? dueDate = null) => new()
    {
        Id               = Guid.NewGuid(),
        CompanyId        = companyId,
        BranchId         = branchId,
        BusinessUnitId   = businessUnitId,
        CreatedAt        = DateTime.UtcNow,
        JournalEntryId   = journalEntryId,
        LedgerAccountId  = ledgerAccountId,
        LineNumber       = lineNumber,
        Description      = description,
        DebitAmount      = debit,
        CreditAmount     = credit,
        BaseDebitAmount  = debit,
        BaseCreditAmount = credit,
        CurrencyCode     = "USD",
        ExchangeRate     = 1m,
        SourceModuleType = sourceModule,
        SourceEntityType = sourceEntityType,
        SourceEntityId   = sourceEntityId,
        CustomerId       = customerId,
        DueDate          = dueDate,
    };
}
