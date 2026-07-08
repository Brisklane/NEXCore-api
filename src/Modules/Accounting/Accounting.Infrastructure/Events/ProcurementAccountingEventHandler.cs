using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Enums;
using Nexcore.SharedKernel.Events;

namespace Accounting.Infrastructure.Events;

/// <summary>
/// Posts GL journal entries for procurement (accounts-payable) financial events — the vendor-side
/// mirror of <see cref="SalesAccountingEventHandler"/>.
///
/// ┌──────────────────────────────┬──────────────────────────────────────────────────┐
/// │ Event                        │ Journal (simple model)                           │
/// ├──────────────────────────────┼──────────────────────────────────────────────────┤
/// │ PurchaseInvoicePostedEvent   │ DR Expense/Inventory (per line, tax-inclusive)    │
/// │  (vendor bill posted)        │   CR Accounts Payable 2111 (total, vendor-tagged) │
/// ├──────────────────────────────┼──────────────────────────────────────────────────┤
/// │ VendorPaymentClearedEvent    │ DR Accounts Payable 2111 (vendor-tagged)          │
/// │  (payment cleared)           │   CR Cash 1111 / Bank 1112                        │
/// └──────────────────────────────┴──────────────────────────────────────────────────┘
///
/// Standard COA numbers seeded by AccountingInitializationService:
///   2111 Accounts Payable – Trade · 1111 Cash on Hand · 1112 Bank – Primary · 1143 Finished Goods.
/// The invoice line's chosen GL account wins when present; otherwise it falls back to Inventory.
/// </summary>
public class ProcurementAccountingEventHandler :
    IEventHandler<PurchaseInvoicePostedEvent>,
    IEventHandler<VendorPaymentClearedEvent>
{
    private const string ApAccount   = "2111"; // Accounts Payable – Trade
    private const string CashAccount  = "1111";
    private const string BankAccount  = "1112";
    private const string InventoryFallback = "1143"; // Finished Goods (debit fallback for goods bills)

    private readonly AccountingDbContext _ctx;
    private readonly ILogger<ProcurementAccountingEventHandler> _logger;

    public ProcurementAccountingEventHandler(AccountingDbContext ctx, ILogger<ProcurementAccountingEventHandler> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    // ── Vendor invoice posted → recognise the payable ─────────────────────────

    public async Task HandleAsync(PurchaseInvoicePostedEvent e, CancellationToken ct = default)
    {
        _logger.LogInformation("Accounting: posting Purchase Invoice {InvoiceNumber}", e.InvoiceNumber);

        var ledger = await DefaultLedgerAsync(e.CompanyId, ct);
        if (ledger is null) return;

        var apAccountId = await AccountByNumberAsync(e.CompanyId, ApAccount, ct);
        if (apAccountId == Guid.Empty)
        {
            _logger.LogWarning("Accounting: AP account {Ap} not found for Company {CompanyId} — skipping Invoice {InvoiceNumber}",
                ApAccount, e.CompanyId, e.InvoiceNumber);
            return;
        }
        var inventoryFallbackId = await AccountByNumberAsync(e.CompanyId, InventoryFallback, ct);

        var journalNumber = $"PINV-{e.InvoiceDate:yyyyMMdd}-{e.InvoiceId.ToString("N")[..8].ToUpper()}";
        var entry = BuildHeader(ledger.Id, journalNumber, "PurchaseInvoice",
            $"Vendor Invoice {e.InvoiceNumber} — {e.VendorName ?? e.VendorId.ToString()}",
            e.InvoiceId.ToString(), e.CompanyId, e.BranchId, e.BusinessUnitId, e.CreatedByUserId,
            e.InvoiceDate, e.TotalAmount, e.CurrencyCode, e.ExchangeRate);

        int seq = 1;

        // DR Expense/Inventory per line (tax folded into the line amount in the simple model).
        foreach (var l in e.Lines)
        {
            if (l.Amount == 0m) continue;
            var debitAccountId = await ResolveAccountAsync(e.CompanyId, l.ExpenseAccountId, InventoryFallback, ct, inventoryFallbackId);
            if (debitAccountId == Guid.Empty) continue;

            entry.Lines.Add(Line(entry.Id, seq++, debitAccountId,
                debit: l.Amount, credit: 0m,
                string.IsNullOrWhiteSpace(l.Description) ? $"Purchase — Invoice {e.InvoiceNumber}" : l.Description,
                ModuleType.Purchasing, EntityType.General, e.InvoiceId,
                e.CompanyId, e.BranchId, e.BusinessUnitId));
        }

        if (!entry.Lines.Any())
        {
            _logger.LogWarning("Accounting: no postable lines for Purchase Invoice {InvoiceNumber}", e.InvoiceNumber);
            return;
        }

        // CR Accounts Payable = invoice total (vendor-tagged for AP aging).
        entry.Lines.Add(Line(entry.Id, seq, apAccountId,
            debit: 0m, credit: e.TotalAmount,
            $"Accounts Payable — Invoice {e.InvoiceNumber}",
            ModuleType.Purchasing, EntityType.General, e.InvoiceId,
            e.CompanyId, e.BranchId, e.BusinessUnitId, vendorId: e.VendorId, dueDate: e.DueDate));

        _ctx.JournalEntries.Add(entry);
        await _ctx.SaveChangesAsync(ct);
        _logger.LogInformation("Accounting: Journal {JournalNumber} posted for Invoice {InvoiceNumber}", journalNumber, e.InvoiceNumber);
    }

    // ── Vendor payment cleared → settle the payable ───────────────────────────

    public async Task HandleAsync(VendorPaymentClearedEvent e, CancellationToken ct = default)
    {
        _logger.LogInformation("Accounting: posting Vendor Payment {PaymentNumber}", e.PaymentNumber);

        if (e.AmountPaid <= 0m)
        {
            _logger.LogWarning("Accounting: Vendor Payment {PaymentNumber} has no allocated amount — skipping", e.PaymentNumber);
            return;
        }

        var ledger = await DefaultLedgerAsync(e.CompanyId, ct);
        if (ledger is null) return;

        var apAccountId   = await AccountByNumberAsync(e.CompanyId, ApAccount, ct);
        var creditAccountId = await AccountByNumberAsync(e.CompanyId, e.IsCash ? CashAccount : BankAccount, ct);

        if (apAccountId == Guid.Empty || creditAccountId == Guid.Empty)
        {
            _logger.LogWarning("Accounting: GL accounts not found for Vendor Payment {PaymentNumber} — skipping", e.PaymentNumber);
            return;
        }

        var journalNumber = $"VPMT-{e.PaymentDate:yyyyMMdd}-{e.PaymentId.ToString("N")[..8].ToUpper()}";
        var entry = BuildHeader(ledger.Id, journalNumber, "VendorPayment",
            $"Vendor Payment {e.PaymentNumber}",
            e.PaymentId.ToString(), e.CompanyId, e.BranchId, e.BusinessUnitId, e.CreatedByUserId,
            e.PaymentDate, e.AmountPaid, e.CurrencyCode, e.ExchangeRate);

        // DR Accounts Payable (clear the liability, vendor-tagged)
        entry.Lines.Add(Line(entry.Id, 1, apAccountId,
            debit: e.AmountPaid, credit: 0m,
            $"AP settlement — {e.PaymentNumber}",
            ModuleType.Purchasing, EntityType.VendorPayment, e.PaymentId,
            e.CompanyId, e.BranchId, e.BusinessUnitId, vendorId: e.VendorId));

        // CR Cash / Bank
        entry.Lines.Add(Line(entry.Id, 2, creditAccountId,
            debit: 0m, credit: e.AmountPaid,
            $"Payment to vendor — {e.PaymentNumber}",
            ModuleType.Purchasing, EntityType.VendorPayment, e.PaymentId,
            e.CompanyId, e.BranchId, e.BusinessUnitId, vendorId: e.VendorId));

        _ctx.JournalEntries.Add(entry);
        await _ctx.SaveChangesAsync(ct);
        _logger.LogInformation("Accounting: Journal {JournalNumber} posted for Payment {PaymentNumber}", journalNumber, e.PaymentNumber);
    }

    // ── Helpers (mirror SalesAccountingEventHandler) ──────────────────────────

    private async Task<Ledger?> DefaultLedgerAsync(Guid companyId, CancellationToken ct)
    {
        var ledger = await _ctx.Ledgers.FirstOrDefaultAsync(l => l.CompanyId == companyId && l.IsDefault, ct);
        if (ledger is null)
            _logger.LogWarning("Accounting: No default ledger for Company {CompanyId} — skipping posting", companyId);
        return ledger;
    }

    private async Task<Guid> ResolveAccountAsync(Guid companyId, Guid? preferredId, string fallbackAccountNumber,
        CancellationToken ct, Guid secondaryFallback = default)
    {
        if (preferredId.HasValue && preferredId.Value != Guid.Empty)
        {
            var usable = await _ctx.LedgerAccounts.AnyAsync(
                a => a.Id == preferredId.Value && a.CompanyId == companyId && a.IsActive && a.IsPostingAllowed, ct);
            if (usable) return preferredId.Value;
        }
        var byNumber = await AccountByNumberAsync(companyId, fallbackAccountNumber, ct);
        return byNumber != Guid.Empty ? byNumber : secondaryFallback;
    }

    private async Task<Guid> AccountByNumberAsync(Guid companyId, string number, CancellationToken ct)
    {
        var id = await _ctx.LedgerAccounts
            .Where(a => a.CompanyId == companyId && a.AccountNumber == number && a.IsActive)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);
        return id ?? Guid.Empty;
    }

    private static JournalEntry BuildHeader(
        Guid ledgerId, string journalNumber, string documentType, string description, string sourceRef,
        Guid companyId, Guid branchId, Guid businessUnitId, Guid userId,
        DateTime postingDate, decimal total, string currencyCode, decimal exchangeRate = 1m) => new()
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
        SourceSystem      = "Procurement",
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
        Guid? vendorId = null, DateTime? dueDate = null) => new()
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
        VendorId         = vendorId,
        DueDate          = dueDate,
    };
}
