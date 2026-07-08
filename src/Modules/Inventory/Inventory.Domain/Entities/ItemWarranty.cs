using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Warranty
/// Warranty terms and conditions for an item.
/// Shown on Daraz/AliExpress product pages and printed on POS receipts.
/// </summary>
public class ItemWarranty : BaseEntity
{
    public Guid ItemId { get; set; }

    /// <summary>
    /// Warranty type: Seller | Brand | NoWarranty | International | Local
    /// </summary>
    public string WarrantyType { get; set; } = "Seller";

    /// <summary>Duration in months (0 = no warranty)</summary>
    public int DurationMonths { get; set; }

    /// <summary>Warranty policy description (displayed on marketplace and POS receipt)</summary>
    public string? PolicyDescription { get; set; }

    /// <summary>Warranty provider name (e.g., "Samsung Official", "Seller Warranty")</summary>
    public string? ProviderName { get; set; }

    /// <summary>Warranty provider contact or URL</summary>
    public string? ProviderContact { get; set; }

    // Navigation property
    public Item? Item { get; set; }
}
