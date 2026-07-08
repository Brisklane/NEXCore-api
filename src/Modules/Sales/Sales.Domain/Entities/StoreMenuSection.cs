using Nexcore.SharedKernel;

namespace Sales.Domain.Entities;

/// <summary>
/// A section within a StoreMenu that groups items by Inventory category.
///
/// Cross-module rule: references Inventory.ItemCategory by ID only - never navigates
/// into Inventory. The category name/image are resolved by the app at read time
/// from Inventory via <c>ItemCategoryId</c>.
///
/// One StoreMenu can have many sections, each pointing to a different ItemCategory.
/// Display order and active flag are per-section-per-menu (same category can appear
/// in different menus with different ordering).
/// </summary>
public class StoreMenuSection : BaseEntity
{
    public Guid StoreMenuId { get; set; }
    public StoreMenu StoreMenu { get; set; } = null!;

    // ? Cross-module FK (Inventory.ItemCategory)
    /// <summary>
    /// FK to Inventory.ItemCategory - ID reference only.
    /// Never navigate across module boundaries.
    /// </summary>
    public Guid ItemCategoryId { get; set; }

    public int DisplayOrder { get; set; }
}
