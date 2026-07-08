using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// A published menu for a store's online ordering channel.
///
/// A store can have multiple menus (e.g., "Breakfast", "Lunch", "All Day").
/// Each menu is active during a time window defined by AvailableFrom/AvailableTo.
/// The customer app shows the correct menu based on the current time.
///
/// Items and categories are NOT stored here - they live in Inventory.
/// This entity only defines which Inventory categories (via StoreMenuSection)
/// are included in this menu and in what display order.
/// Per-item availability and online price are controlled by Inventory.ItemChannelListing
/// (Channel = "MOBILE_APP").
/// </summary>
public class StoreMenu : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public int DisplayOrder { get; set; }
    /// <summary>Time of day this menu becomes available (e.g., 06:00). Null = always.</summary>
    public TimeOnly? AvailableFrom { get; set; }

    /// <summary>Time of day this menu stops being available (e.g., 11:00). Null = always.</summary>
    public TimeOnly? AvailableTo { get; set; }

    /// <summary>
    /// Sections of this menu - each section maps to one Inventory.ItemCategory.
    /// Items within each category are resolved from Inventory at query time.
    /// </summary>
    public ICollection<StoreMenuSection> Sections { get; set; } = new List<StoreMenuSection>();
}
