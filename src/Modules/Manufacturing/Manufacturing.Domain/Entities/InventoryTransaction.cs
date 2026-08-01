using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Manufacturing's own journal of shop-floor material movements, surfaced by the
/// manufacturing inventory-transactions screen.
/// <para>
/// This is <b>not</b> the stock ledger. <c>Inventory.Domain.Entities.InventoryTransaction</c> is
/// the source of truth for on-hand quantities and valuation: completing a production order
/// publishes <see cref="Nexcore.SharedKernel.Events.ProductionCompletedEvent"/>, which
/// Inventory's <c>ProductionStockHandler</c> posts as real MFGISS/MFGRCP documents with
/// moving-average costing. Rows here do not move stock — do not treat them as a balance source.
/// </para>
/// </summary>
public class InventoryTransaction : BaseEntity
{
    /// <summary>
    /// Product reference (External - from Inventory module)
    /// </summary>
    public Guid ProductId { get; set; }

    /// <summary>
    /// Quantity moved (+ for receipt, - for issue)
    /// </summary>
    public decimal Quantity { get; set; }

    /// <summary>
    /// Type of transaction
    /// </summary>
    public required string TransactionType { get; set; } // Issue, Receipt, Transfer, Adjustment, Scrap

    /// <summary>
    /// Reference ID (ProductionOrder, PurchaseOrder, etc.)
    /// </summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>
    /// Reference type
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Transaction timestamp
    /// </summary>
    public DateTime TransactionDate { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }
}
