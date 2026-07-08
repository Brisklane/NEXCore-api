using Accounting.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nexcore.SharedKernel.Events;

namespace Accounting.Infrastructure.Events;

/// <summary>
/// Handles <see cref="GlAccountsRequestedEvent"/> published by the Inventory module.
///
/// Looks up the four standard GL accounts for the requesting company and
/// completes the event's <see cref="GlAccountsRequestedEvent.Result"/> TCS so
/// that the Inventory caller receives them without polling and without a direct
/// compile-time reference to Accounting.Infrastructure.
///
/// Standard COA numbers (seeded by AccountingInitializationService):
///   1300 – Inventory asset
///   5100 – Cost of Goods Sold
///   2100 – Accounts Payable (purchase clearing)
///   4100 – Product Sales revenue
/// </summary>
public class GlAccountsRequestedEventHandler : IEventHandler<GlAccountsRequestedEvent>
{
    private readonly AccountingDbContext _ctx;
    private readonly ILogger<GlAccountsRequestedEventHandler> _logger;

    public GlAccountsRequestedEventHandler(
        AccountingDbContext ctx,
        ILogger<GlAccountsRequestedEventHandler> logger)
    {
        _ctx    = ctx;
        _logger = logger;
    }

    public async Task HandleAsync(
        GlAccountsRequestedEvent domainEvent,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Account numbers come from the event — never hardcoded here.
            var targetNumbers = new[]
            {
                domainEvent.InventoryAccountNo,
                domainEvent.CogsAccountNo,
                domainEvent.PurchaseAccountNo,
                domainEvent.SalesAccountNo,
            };

            var accounts = await _ctx.LedgerAccounts
                .Where(a => a.CompanyId == domainEvent.CompanyId
                         && a.BranchId  == domainEvent.BranchId
                         && (domainEvent.BusinessUnitId == null || a.BusinessUnitId == domainEvent.BusinessUnitId)
                         && !a.IsDeleted
                         && a.IsActive
                         && targetNumbers.Contains(a.AccountNumber))
                .Select(a => new { a.AccountNumber, a.Id })
                .ToListAsync(cancellationToken);

            Guid? Get(string no) =>
                accounts.FirstOrDefault(a => a.AccountNumber == no)?.Id;

            var result = new GlAccountSet(
                Inventory: Get(domainEvent.InventoryAccountNo),
                Cogs:      Get(domainEvent.CogsAccountNo),
                Purchase:  Get(domainEvent.PurchaseAccountNo),
                Sales:     Get(domainEvent.SalesAccountNo));

            _logger.LogDebug(
                "Resolved GL accounts for Company:{CompanyId} Branch:{BranchId} " +
                "[{InvNo}={Inv}, {CogsNo}={Cogs}, {PurNo}={Pur}, {SalNo}={Sal}]",
                domainEvent.CompanyId,
                domainEvent.BranchId,
                domainEvent.InventoryAccountNo, result.Inventory,
                domainEvent.CogsAccountNo,      result.Cogs,
                domainEvent.PurchaseAccountNo,  result.Purchase,
                domainEvent.SalesAccountNo,     result.Sales);

            domainEvent.Result.TrySetResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to resolve GL accounts for Company:{CompanyId}",
                domainEvent.CompanyId);

            domainEvent.Result.TrySetResult(GlAccountSet.Empty);
        }
    }
}
