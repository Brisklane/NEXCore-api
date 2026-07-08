namespace Nexcore.SharedKernel.Events;

// Shared GL account value types
// These types live in SharedKernel so any module can publish or handle GL account
// events without creating a cross-module project reference.

/// <summary>
/// The four GL account IDs needed for inventory item posting.
/// Resolved by the Accounting module via <see cref="GlAccountsRequestedEvent"/>.
/// </summary>
/// <param name="Inventory">Balance-sheet inventory asset account</param>
/// <param name="Cogs">Cost of Goods Sold expense account</param>
/// <param name="Purchase">Accounts-payable / purchase clearing account</param>
/// <param name="Sales">Sales revenue account</param>
public record GlAccountSet(Guid? Inventory, Guid? Cogs, Guid? Purchase, Guid? Sales)
{
    public static readonly GlAccountSet Empty = new(null, null, null, null);
}

/// <summary>
/// COA account numbers used to look up the four GL accounts.
/// Callers supply this when a company uses a numbering scheme different from
/// the seeded defaults.
/// </summary>
/// These MUST be posting-allowed leaf accounts (not summary parents), so item/category GL links
/// resolve to accounts the GL can actually post to.
/// <param name="Inventory">Inventory asset account number — Finished Goods (default: 1143)</param>
/// <param name="Cogs">Cost of Goods Sold account number — Cost of Inventory Sold (default: 5104)</param>
/// <param name="Purchase">Purchase / AP clearing number — Accounts Payable - Trade (default: 2111)</param>
/// <param name="Sales">Sales revenue account number — Product Sales - Retail (default: 4111)</param>
public record GlAccountNumbers(
    string Inventory = "1143",
    string Cogs      = "5104",
    string Purchase  = "2111",
    string Sales     = "4111")
{
    /// <summary>Standard posting COA numbers seeded by AccountingInitializationService.</summary>
    public static readonly GlAccountNumbers Default = new();
}

/// <summary>
/// Request-event published by any module to ask the Accounting module for the
/// four GL account IDs for a company.  The handler completes <see cref="Result"/>
/// so the publisher can await the reply without polling.
///
/// The caller supplies the COA account numbers so the handler never needs to
/// hard-code them - different companies / locales can use different numbering.
/// </summary>
public class GlAccountsRequestedEvent
{
    public Guid EventId     { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    public Guid  CompanyId      { get; init; }
    public Guid  BranchId       { get; init; }
    public Guid? BusinessUnitId { get; init; }

    /// <summary>COA number for the inventory asset account (Balance Sheet). Default: 1300.</summary>
    public string InventoryAccountNo { get; init; } = "1300";

    /// <summary>COA number for the Cost of Goods Sold account (Income Statement). Default: 5100.</summary>
    public string CogsAccountNo { get; init; } = "5100";

    /// <summary>COA number for the Accounts Payable / purchase clearing account. Default: 2100.</summary>
    public string PurchaseAccountNo { get; init; } = "2100";

    /// <summary>COA number for the Sales revenue account. Default: 4100.</summary>
    public string SalesAccountNo { get; init; } = "4100";

    /// <summary>Completed by the Accounting handler with the resolved account IDs.</summary>
    public TaskCompletionSource<GlAccountSet> Result { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}

// Per-category GL account creation

/// <summary>
/// The four GL account IDs created (or resolved) by the Accounting module
/// specifically for one inventory category.
/// </summary>
public record CategoryGlAccountSet(
    Guid? Inventory,
    Guid? Cogs,
    Guid? Purchase,
    Guid? Sales)
{
    public static readonly CategoryGlAccountSet Empty = new(null, null, null, null);

    /// <summary>True when all four accounts were successfully created / resolved.</summary>
    public bool IsComplete =>
        Inventory.HasValue && Cogs.HasValue && Purchase.HasValue && Sales.HasValue;
}

/// <summary>
/// Published by any module when a new item category is created.
/// The Accounting handler creates four dedicated GL sub-accounts
/// (one per posting type) under the standard parent accounts and
/// completes <see cref="Result"/> with their IDs.
///
/// The caller awaits <see cref="Result"/> within the same request -
/// the in-process TCS pattern guarantees no deadlock
/// (TaskCreationOptions.RunContinuationsAsynchronously).
/// </summary>
public class CategoryGlAccountsRequestedEvent
{
    public Guid EventId    { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;

    // Tenant context - stamped on every new GL sub-account
    public Guid  CompanyId      { get; init; }
    public Guid  BranchId       { get; init; }
    public Guid? BusinessUnitId { get; init; }

    /// <summary>User who triggered the creation - for GL account audit trail.</summary>
    public Guid UserId { get; init; }

    // Category identity
    /// <summary>Category code - used as the sub-account suffix (e.g., "1300-ELECTRONICS").</summary>
    public string CategoryCode { get; init; } = null!;

    /// <summary>Category name - used as the sub-account display name.</summary>
    public string CategoryName { get; init; } = null!;

    // Parent COA account numbers
    // Leave at defaults unless the company uses a different numbering scheme.
    public string ParentInventoryAccountNo { get; init; } = "1300";
    public string ParentCogsAccountNo      { get; init; } = "5100";
    public string ParentPurchaseAccountNo  { get; init; } = "2100";
    public string ParentSalesAccountNo     { get; init; } = "4100";

    /// <summary>Completed by the Accounting handler with the four new account IDs.</summary>
    public TaskCompletionSource<CategoryGlAccountSet> Result { get; } =
        new(TaskCreationOptions.RunContinuationsAsynchronously);
}
