using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Supplier
/// Links items to their suppliers with pricing and lead-time details.
/// Supports multiple suppliers per item (primary + alternatives).
/// Used in purchasing, Cash &amp; Carry replenishment, and marketplace procurement.
/// </summary>
public class ItemSupplier : BaseEntity
{
    public Guid ItemId { get; set; }

    /// <summary>Supplier ID (references Supplier master in Purchasing module)</summary>
    public Guid SupplierId { get; set; }

    /// <summary>Supplier's own code / part number for this item</summary>
    public string? SupplierItemCode { get; set; }

    /// <summary>Supplier's item description (may differ from internal Name)</summary>
    public string? SupplierItemName { get; set; }

    /// <summary>Last known purchase price from this supplier</summary>
    public decimal? LastPurchasePrice { get; set; }

    /// <summary>Currency of the purchase price (e.g., USD, CNY, PKR)</summary>
    public string CurrencyCode { get; set; } = "USD";

    /// <summary>Minimum order quantity this supplier accepts</summary>
    public decimal? MinOrderQuantity { get; set; }

    /// <summary>Lead time in days from order to delivery</summary>
    public int? LeadTimeDays { get; set; }

    /// <summary>Whether this is the primary / preferred supplier for this item</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Date of last purchase from this supplier</summary>
    public DateTime? LastPurchaseDate { get; set; }

    // Navigation property
    public Item? Item { get; set; }
}
