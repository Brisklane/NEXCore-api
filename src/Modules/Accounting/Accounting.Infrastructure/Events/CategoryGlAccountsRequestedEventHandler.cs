using Accounting.Domain.Entities;
using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Accounting.Infrastructure.Events;

/// <summary>
/// Handles <see cref="CategoryGlAccountsRequestedEvent"/> published by the Inventory module
/// whenever a new item category is created.
///
/// For each of the four posting types (Inventory asset, COGS, Purchase clearing, Sales revenue)
/// this handler:
///   1. Resolves the parent GL account by its standard account number.
///   2. Derives a unique sub-account number  (e.g., parent "1300" + suffix "0010" ? "1300-0010").
///   3. Creates the sub-account (if it doesn't already exist) under that parent.
///   4. Returns all four new account IDs via the event's TCS so the Inventory caller
///      can store them on the ItemCategory without a direct reference to Accounting.
///
/// Sub-account numbering:
///   The suffix is the 1-based ordinal of existing sub-accounts padded to 4 digits,
///   giving room for up to 9,999 categories per parent account per company.
/// </summary>
public class CategoryGlAccountsRequestedEventHandler
    : IEventHandler<CategoryGlAccountsRequestedEvent>
{
    private readonly AccountingDbContext _ctx;
    private readonly ILogger<CategoryGlAccountsRequestedEventHandler> _logger;

    public CategoryGlAccountsRequestedEventHandler(
        AccountingDbContext ctx,
        ILogger<CategoryGlAccountsRequestedEventHandler> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    public async Task HandleAsync(
        CategoryGlAccountsRequestedEvent ev,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var parentNos = new[]
            {
                ev.ParentInventoryAccountNo,
                ev.ParentCogsAccountNo,
                ev.ParentPurchaseAccountNo,
                ev.ParentSalesAccountNo,
            };

            // Load the parent accounts by equality, one number at a time.
            //
            // We deliberately AVOID `parentNos.Contains(a.AccountNumber)` here. Under the
            // LINQ expression interpreter, EF Core 9's primitive-collection translation of
            // array.Contains tries to instantiate a generic delegate whose type argument is
            // ReadOnlySpan<string> (a ref struct), which throws:
            //   TypeLoadException: 'System.ReadOnlySpan`1[System.String]' ... violates the
            //   constraint of type parameter 'TRet'  (at MakeGenericType / FuncCallInstruction)
            // That exception was silently swallowed by the catch below, leaving every category
            // without GL accounts. A simple equality predicate per number translates cleanly.
            //
            // We also omit the IsActive filter — parent/header accounts are often inactive
            // (non-posting) but are still valid structural parents.
            var parents = new List<LedgerAccount>();
            foreach (var no in parentNos.Where(n => !string.IsNullOrWhiteSpace(n)).Distinct())
            {
                var acct = await _ctx.LedgerAccounts.FirstOrDefaultAsync(
                    a => a.CompanyId == ev.CompanyId && !a.IsDeleted && a.AccountNumber == no,
                    cancellationToken);
                if (acct != null) parents.Add(acct);
            }

            var inv  = await EnsureSubAccountAsync(ev, parents, ev.ParentInventoryAccountNo,
                           "Inventory",       cancellationToken);
            var cogs = await EnsureSubAccountAsync(ev, parents, ev.ParentCogsAccountNo,
                           "COGS",            cancellationToken);
            var pur  = await EnsureSubAccountAsync(ev, parents, ev.ParentPurchaseAccountNo,
                           "Purchase",        cancellationToken);
            var sal  = await EnsureSubAccountAsync(ev, parents, ev.ParentSalesAccountNo,
                           "Sales",           cancellationToken);

            await _ctx.SaveChangesAsync(cancellationToken);

            var result = new CategoryGlAccountSet(
                Inventory: inv,
                Cogs:      cogs,
                Purchase:  pur,
                Sales:     sal);

            _logger.LogInformation(
                "GL sub-accounts created/resolved for Category:{Code} Company:{CompanyId} " +
                "Inv:{Inv} COGS:{Cogs} Pur:{Pur} Sal:{Sal}",
                ev.CategoryCode, ev.CompanyId,
                result.Inventory, result.Cogs, result.Purchase, result.Sales);

            ev.Result.TrySetResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to create GL sub-accounts for Category:{Code} Company:{CompanyId}",
                ev.CategoryCode, ev.CompanyId);

            // Signal the requester with an empty set so the await never hangs.
            ev.Result.TrySetResult(CategoryGlAccountSet.Empty);
        }
    }

    // ?? helpers ??????????????????????????????????????????????????????????????

    private async Task<Guid?> EnsureSubAccountAsync(
        CategoryGlAccountsRequestedEvent ev,
        List<LedgerAccount> parents,
        string parentAccountNo,
        string postingTypeLabel,
        CancellationToken ct)
    {
        var parent = parents.FirstOrDefault(p => p.AccountNumber == parentAccountNo);
        if (parent == null)
        {
            _logger.LogWarning(
                "Parent GL account {No} not found for Company:{CompanyId} Branch:{BranchId}. " +
                "Skipping {Label} sub-account creation.",
                parentAccountNo, ev.CompanyId, ev.BranchId, postingTypeLabel);
            return null;
        }

        // Deterministic sub-account number: "<parent>-<CATCODE>" (max 20 chars total).
        // Truncate category code if needed so the combined number fits the column length.
        var suffix = ev.CategoryCode.Length > 8 ? ev.CategoryCode[..8] : ev.CategoryCode;
        var subNo  = $"{parentAccountNo}-{suffix}".ToUpperInvariant();

        // Idempotent - return existing if already created (e.g., retry scenario).
        // Scope the lookup to the caller's exact branch + business unit so two branches
        // can each have their own sub-account under the same parent number.
        var existing = await _ctx.LedgerAccounts
            .Where(a => a.CompanyId      == ev.CompanyId
                     && a.BranchId       == ev.BranchId
                     && !a.IsDeleted
                     && a.AccountNumber  == subNo)
            .Select(a => (Guid?)a.Id)
            .FirstOrDefaultAsync(ct);

        if (existing.HasValue)
        {
            _logger.LogDebug(
                "Sub-account {No} already exists (Id:{Id}) for Branch:{BranchId} - reusing.",
                subNo, existing.Value, ev.BranchId);
            return existing;
        }

        var now     = DateTime.UtcNow;
        var subName = $"{ev.CategoryName} - {postingTypeLabel}";

        var sub = new LedgerAccount
        {
            Id               = Guid.NewGuid(),
            CompanyId        = ev.CompanyId,
            BranchId         = ev.BranchId,                        // from caller
            BusinessUnitId   = ev.BusinessUnitId ?? parent.BusinessUnitId,  // caller first, parent fallback
            CreatedByUserId  = ev.UserId,
            CreatedAt        = now,
            LedgerId         = parent.LedgerId,
            AccountNumber    = subNo,
            AccountName      = subName.Length > 255 ? subName[..255] : subName,
            CategoryId       = parent.CategoryId,
            ParentAccountId  = parent.Id,
            IsPostingAllowed = true,
            IsControlAccount = false,
            AllowManualEntry = true,
            IsActive         = true,
            Description      =
                $"Auto-created GL sub-account for inventory category '{ev.CategoryName}' " +
                $"({postingTypeLabel}). Parent: {parentAccountNo}. " +
                $"Branch: {ev.BranchId}.",
        };

        _ctx.LedgerAccounts.Add(sub);
        return sub.Id;
    }
}
