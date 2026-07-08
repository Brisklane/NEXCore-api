using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Inventory Document (Header)
/// Main document for inventory transactions
/// Examples: GRN, Delivery, Transfer, Adjustment
/// </summary>
public class InventoryDocument : BaseEntity
{
    /// <summary>
    /// Document number (auto-generated)
    /// </summary>
    public string DocumentNumber { get; set; } = null!;

    /// <summary>
    /// Document type (GRN, Issue, Transfer, Adjustment)
    /// </summary>
    public string DocumentType { get; set; } = null!;

    /// <summary>
    /// Document date
    /// </summary>
    public DateTime DocumentDate { get; set; }

    /// <summary>
    /// Document status (Draft, Posted, Cancelled)
    /// </summary>
    public string Status { get; set; } = "Draft";

    /// <summary>
    /// Reference document type (PO, SO, etc.)
    /// </summary>
    public string? ReferenceType { get; set; }

    /// <summary>
    /// Reference document ID
    /// </summary>
    public Guid? ReferenceId { get; set; }

    /// <summary>
    /// From warehouse ID (for transfers)
    /// </summary>
    public Guid? FromWarehouseId { get; set; }

    /// <summary>
    /// To warehouse ID (for receipts/transfers)
    /// </summary>
    public Guid? ToWarehouseId { get; set; }

    /// <summary>
    /// Posting date (when document was posted)
    /// </summary>
    public DateTime? PostingDate { get; set; }

    /// <summary>
    /// Posted by user ID
    /// </summary>
    public Guid? PostedByUserId { get; set; }

    /// <summary>
    /// Total quantity in document
    /// </summary>
    public decimal TotalQuantity { get; set; }

    /// <summary>
    /// Total cost of document
    /// </summary>
    public decimal TotalCost { get; set; }

    // Navigation properties
    public ICollection<InventoryDocumentLine> Lines { get; set; } = new List<InventoryDocumentLine>();
    public Warehouse? FromWarehouse { get; set; }
    public Warehouse? ToWarehouse { get; set; }
}
