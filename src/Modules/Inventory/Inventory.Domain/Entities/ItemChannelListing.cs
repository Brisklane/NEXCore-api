using Nexcore.SharedKernel;

namespace Inventory.Domain.Entities;

/// <summary>
/// Item Channel Listing
/// Controls how and where an item is listed across sales channels.
/// One row per item per channel (AliExpress, Daraz, POS, B2B Portal, Cash &amp; Carry, etc.)
/// Each channel can have its own listing status, title, price offset, and visibility rules.
/// </summary>
public class ItemChannelListing : BaseEntity
{
    public Guid ItemId { get; set; }

    /// <summary>
    /// Channel code: POS | DARAZ | ALIEXPRESS | CASH_CARRY | B2B | WEBSITE | MOBILE_APP
    /// </summary>
    public string Channel { get; set; } = null!;

    /// <summary>
    /// Listing status on this channel:
    /// Draft | Active | Paused | Rejected | OutOfStock | Discontinued
    /// </summary>
    public string ListingStatus { get; set; } = "Draft";

    /// <summary>
    /// Channel-specific product title (overrides item Name for this channel's listing)
    /// </summary>
    public string? ChannelTitle { get; set; }

    /// <summary>Channel-specific description (HTML supported for Daraz/AliExpress)</summary>
    public string? ChannelDescription { get; set; }

    /// <summary>
    /// Channel price override.
    /// Null = use item-level price. Set when this channel has a different price.
    /// </summary>
    public decimal? ChannelPrice { get; set; }

    /// <summary>Percentage or fixed discount applied on this channel (0 = no discount)</summary>
    public decimal? ChannelDiscount { get; set; }

    /// <summary>
    /// Discount type: Percentage | FixedAmount
    /// </summary>
    public string? DiscountType { get; set; }

    /// <summary>External product ID on this channel (e.g., Daraz item ID, AliExpress product ID)</summary>
    public string? ExternalProductId { get; set; }

    /// <summary>External SKU on this channel (may differ from internal Code)</summary>
    public string? ExternalSku { get; set; }

    /// <summary>URL of the listing on this channel</summary>
    public string? ListingUrl { get; set; }

    /// <summary>Date when listing was activated on channel</summary>
    public DateTime? ListedAt { get; set; }

    /// <summary>Date when listing was last synced with the channel</summary>
    public DateTime? LastSyncedAt { get; set; }

    /// <summary>Whether stock is synchronized automatically to this channel</summary>
    public bool AutoSyncStock { get; set; } = true;

    /// <summary>Whether price is synchronized automatically to this channel</summary>
    public bool AutoSyncPrice { get; set; } = true;

    /// <summary>
    /// Mark this item as temporarily sold out on this channel.
    /// Staff can toggle from the store dashboard without unpublishing the item.
    /// </summary>
    public bool IsSoldOut { get; set; }

    /// <summary>Max quantity a single customer can order in one order on this channel.</summary>
    public int? MaxQuantityPerOrder { get; set; }

    /// <summary>Display order within the category on this channel.</summary>
    public int DisplayOrder { get; set; }

    // Navigation property
    public Item? Item { get; set; }
}
