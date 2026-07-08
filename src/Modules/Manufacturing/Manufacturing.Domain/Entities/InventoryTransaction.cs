using Nexcore.SharedKernel;

namespace Manufacturing.Domain.Entities;

/// <summary>
/// Inventory Transaction - unified ledger of all stock movements.
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
